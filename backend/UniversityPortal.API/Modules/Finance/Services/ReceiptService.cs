using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using UniversityPortal.API.Common.Auditing;
using UniversityPortal.API.Common.Constants;
using UniversityPortal.API.Common.Exceptions;
using UniversityPortal.API.Common.Security;
using UniversityPortal.API.Infrastructure.Persistence;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;
using UniversityPortal.API.Integrations.BusinessCentral.DTOs;
using UniversityPortal.API.Modules.Finance.Configuration;
using UniversityPortal.API.Modules.Finance.DTOs;
using UniversityPortal.API.Modules.Finance.Models;

namespace UniversityPortal.API.Modules.Finance.Services;

public interface IReceiptService
{
    Task<ReceiptDto> GetForStudentAsync(string receiptNumber, CancellationToken ct);
    Task<ReceiptDto> PrintForStudentAsync(string receiptNumber, CancellationToken ct);
    Task<ReceiptDto> GetForOfficerAsync(string studentNumber, string receiptNumber, CancellationToken ct);
    Task<ReceiptDto> PrintForOfficerAsync(string studentNumber, string receiptNumber, CancellationToken ct);
}

public class ReceiptService : IReceiptService
{
    private static readonly string[] PaymentTypes = ["Payment"];
    private static readonly string[] InvoiceTypes = ["Invoice"];

    private readonly PortalDbContext _db;
    private readonly IBusinessCentralClient _bc;
    private readonly ICurrentUser _user;
    private readonly TimeProvider _time;
    private readonly ReceiptOptions _options;
    private readonly IAuditLog _audit;
    private readonly ILogger<ReceiptService> _logger;

    public ReceiptService(
        PortalDbContext db,
        IBusinessCentralClient bc,
        ICurrentUser user,
        TimeProvider time,
        IOptions<ReceiptOptions> options,
        IAuditLog audit,
        ILogger<ReceiptService> logger)
    {
        _db = db;
        _bc = bc;
        _user = user;
        _time = time;
        _options = options.Value;
        _audit = audit;
        _logger = logger;
    }

    public Task<ReceiptDto> GetForStudentAsync(string receiptNumber, CancellationToken ct) =>
        BuildAsync(RequireStudentNumber(), NormalizeReceiptNumber(receiptNumber), ct);

    public Task<ReceiptDto> PrintForStudentAsync(string receiptNumber, CancellationToken ct) =>
        PrintAsync(RequireStudentNumber(), NormalizeReceiptNumber(receiptNumber), ct);

    public Task<ReceiptDto> GetForOfficerAsync(
        string studentNumber,
        string receiptNumber,
        CancellationToken ct)
    {
        RequireOfficer();
        return BuildAsync(NormalizeStudentNumber(studentNumber), NormalizeReceiptNumber(receiptNumber), ct);
    }

    public Task<ReceiptDto> PrintForOfficerAsync(
        string studentNumber,
        string receiptNumber,
        CancellationToken ct)
    {
        RequireOfficer();
        return PrintAsync(NormalizeStudentNumber(studentNumber), NormalizeReceiptNumber(receiptNumber), ct);
    }

    private async Task<ReceiptDto> PrintAsync(
        string studentNumber,
        string receiptNumber,
        CancellationToken ct)
    {
        var receipt = await BuildAsync(studentNumber, receiptNumber, ct);
        if (receipt.Status == ReceiptStatus.Reversed)
            throw new PortalException(
                StatusCodes.Status409Conflict,
                "Receipt reversed",
                "That payment was reversed, so its receipt cannot be printed.");

        var copyNumber = await RecordPrintAsync(studentNumber, receiptNumber, ct);
        await AuditAsync(studentNumber, receiptNumber, copyNumber, ct);
        return receipt with { CopyNumber = copyNumber, PrintCount = copyNumber + 1 };
    }

    private async Task<int> RecordPrintAsync(
        string studentNumber,
        string receiptNumber,
        CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var copyNumber = await _db.ReceiptIssues
                .AsNoTracking()
                .CountAsync(issue =>
                    issue.ReceiptNumber == receiptNumber && issue.StudentNumber == studentNumber, ct);

            var issue = new ReceiptIssue
            {
                ReceiptNumber = receiptNumber,
                StudentNumber = studentNumber,
                CopyNumber = copyNumber,
                IssuedByUserId = _user.UserId ?? throw new NotAuthenticatedException(),
                IssuedAtUtc = _time.GetUtcNow().UtcDateTime
            };
            _db.ReceiptIssues.Add(issue);

            try
            {
                await _db.SaveChangesAsync(ct);
                return copyNumber;
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation
                })
            {
                _db.Entry(issue).State = EntityState.Detached;
            }
        }

        throw new PortalException(
            StatusCodes.Status409Conflict,
            "Please try again",
            "The print could not be recorded. Try again.");
    }

    private async Task<ReceiptDto> BuildAsync(
        string studentNumber,
        string receiptNumber,
        CancellationToken ct)
    {
        var customerTask = _bc.GetCustomerByNumberAsync(studentNumber, ct);
        var entriesTask = _bc.QueryCustomerLedgerEntriesAsync(
            new BcLedgerEntryQuery(
                studentNumber,
                PaymentTypes,
                null,
                null,
                0,
                50,
                DocumentNumber: receiptNumber),
            ct);
        var accountDetailsTask = _bc.GetCustomerFinancialDetailsAsync(studentNumber, ct);
        await Task.WhenAll(customerTask, entriesTask, accountDetailsTask);

        var customer = await customerTask
            ?? throw new NotFoundException($"No BC customer for student {studentNumber}");

        var entries = (await entriesTask).Items;
        if (entries.Count == 0)
            throw new NotFoundException(
                $"No payment with receipt number {receiptNumber} for student {studentNumber}");

        var first = entries.OrderBy(entry => entry.EntryNumber).First();
        var amountReceived = -entries.Where(entry => entry.Amount < 0).Sum(entry => entry.Amount);
        var accountDetails = await accountDetailsTask;

        var portalPayment = await _db.PaymentRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(payment =>
                payment.ReceiptNumber == receiptNumber &&
                payment.StudentNumber == customer.Number &&
                payment.Status == PaymentRequestStatus.Posted, ct);

        IReadOnlyList<string> settledInvoices = portalPayment is not null
            ? portalPayment.InvoiceNumber is null
                ? Array.Empty<string>()
                : [portalPayment.InvoiceNumber]
            : await InferSettledInvoicesAsync(entries, ct);

        var invoiceBalanceTasks = settledInvoices.Select(async invoiceNumber =>
        {
            var invoice = await _bc.GetPostedSalesInvoiceByNumberAsync(
                customer.Number,
                invoiceNumber,
                ct);
            return invoice is null
                ? null
                : new ReceiptInvoiceBalanceDto(
                    invoice.Number,
                    invoice.RemainingAmount,
                    string.IsNullOrEmpty(invoice.CurrencyCode)
                        ? customer.CurrencyCode
                        : invoice.CurrencyCode);
        });
        var invoiceBalances = (await Task.WhenAll(invoiceBalanceTasks))
            .OfType<ReceiptInvoiceBalanceDto>()
            .ToList();

        var receivedBy = portalPayment is null
            ? null
            : await _db.Users
                .AsNoTracking()
                .Where(user => user.Id == portalPayment.CreatedByUserId)
                .Select(user => user.FullName)
                .FirstOrDefaultAsync(ct);

        var printCount = await _db.ReceiptIssues
            .AsNoTracking()
            .CountAsync(issue =>
                issue.ReceiptNumber == receiptNumber && issue.StudentNumber == customer.Number, ct);

        return new ReceiptDto(
            ReceiptNumber: receiptNumber,
            PaymentDate: first.PostingDate,
            Status: entries.Any(entry => entry.Reversed) ? ReceiptStatus.Reversed : ReceiptStatus.Valid,
            Issuer: new ReceiptIssuerDto(
                _options.UniversityName,
                _options.AddressLines,
                _options.Phone,
                _options.Email,
                _options.FooterNote),
            Student: new StudentInfoDto(customer.Number, customer.DisplayName, customer.Email),
            AmountReceived: amountReceived,
            CurrencyCode: string.IsNullOrEmpty(first.CurrencyCode) ? customer.CurrencyCode : first.CurrencyCode,
            CurrentAccountBalance: accountDetails?.Balance,
            AccountBalanceCurrencyCode: customer.CurrencyCode,
            InvoiceBalances: invoiceBalances,
            Method: portalPayment?.Method,
            Reference: portalPayment?.Reference,
            ReceivedBy: receivedBy,
            SettledInvoices: settledInvoices,
            PrintCount: printCount,
            CopyNumber: null);
    }

    private async Task<IReadOnlyList<string>> InferSettledInvoicesAsync(
        IReadOnlyList<BcCustomerLedgerEntryDto> payments,
        CancellationToken ct)
    {
        var invoiceNumbers = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var payment in payments)
        {
            var closedEntries = await _bc.QueryCustomerLedgerEntriesAsync(
                new BcLedgerEntryQuery(
                    payment.CustomerNumber,
                    InvoiceTypes,
                    null,
                    null,
                    0,
                    50,
                    ClosedByEntryNumber: payment.EntryNumber),
                ct);

            foreach (var invoice in closedEntries.Items)
                invoiceNumbers.Add(invoice.DocumentNumber);
        }

        return invoiceNumbers.ToList();
    }

    private async Task AuditAsync(
        string studentNumber,
        string receiptNumber,
        int copyNumber,
        CancellationToken ct)
    {
        try
        {
            await _audit.WriteAsync(
                "receipt.printed",
                "Receipt",
                receiptNumber,
                true,
                new { studentNumber, copyNumber },
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Audit write failed for receipt {ReceiptNumber}, copy {CopyNumber}",
                receiptNumber,
                copyNumber);
        }
    }

    private string RequireStudentNumber() =>
        _user.StudentNumber ?? throw new NotAuthenticatedException();

    private void RequireOfficer()
    {
        if (!_user.IsAuthenticated)
            throw new NotAuthenticatedException();
        if (!_user.IsInRole(Roles.FinanceOfficer))
            throw new ForbiddenException("Caller is not in the FinanceOfficer role");
    }

    private static string NormalizeStudentNumber(string studentNumber)
    {
        if (string.IsNullOrWhiteSpace(studentNumber))
            throw new PortalException(
                StatusCodes.Status400BadRequest,
                "Student number required",
                "Provide the student's number.");
        return studentNumber.Trim();
    }

    private static string NormalizeReceiptNumber(string receiptNumber)
    {
        if (string.IsNullOrWhiteSpace(receiptNumber))
            throw new NotFoundException("No payment with that receipt number.");
        return receiptNumber.Trim();
    }
}
