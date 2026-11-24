using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using UniversityPortal.API.Common.Auditing;
using UniversityPortal.API.Common.Constants;
using UniversityPortal.API.Common.Exceptions;
using UniversityPortal.API.Common.Security;
using UniversityPortal.API.Infrastructure.Persistence;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;
using UniversityPortal.API.Integrations.BusinessCentral.DTOs;
using UniversityPortal.API.Integrations.BusinessCentral.Exceptions;
using UniversityPortal.API.Modules.Finance.Configuration;
using UniversityPortal.API.Modules.Finance.DTOs;
using UniversityPortal.API.Modules.Finance.Models;

namespace UniversityPortal.API.Modules.Finance.Services;

public interface IPaymentRecordingService
{
    Task<PaymentResultDto> RecordAsync(RecordPaymentRequest request, CancellationToken ct);
    Task<PaymentResultDto> GetAsync(Guid requestId, CancellationToken ct);
    Task<PaymentResultDto> ReconcileAsync(Guid requestId, CancellationToken ct);
}

public class PaymentRecordingService : IPaymentRecordingService
{
    private static readonly string[] PaymentTypes = ["Payment"];

    private readonly PortalDbContext _db;
    private readonly IBusinessCentralClient _bc;
    private readonly ICurrentUser _user;
    private readonly IAuditLog _audit;
    private readonly TimeProvider _time;
    private readonly PaymentOptions _options;
    private readonly ILogger<PaymentRecordingService> _logger;

    public PaymentRecordingService(
        PortalDbContext db,
        IBusinessCentralClient bc,
        ICurrentUser user,
        IAuditLog audit,
        TimeProvider time,
        IOptions<PaymentOptions> options,
        ILogger<PaymentRecordingService> logger)
    {
        _db = db;
        _bc = bc;
        _user = user;
        _audit = audit;
        _time = time;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<PaymentResultDto> RecordAsync(RecordPaymentRequest request, CancellationToken ct)
    {
        var userId = RequireOfficer();
        var requestId = request.RequestId!.Value;
        var studentNumber = request.StudentNumber.Trim();
        var invoiceNumber = string.IsNullOrWhiteSpace(request.InvoiceNumber)
            ? null
            : request.InvoiceNumber.Trim();
        var amount = request.Amount!.Value;
        var date = request.PaymentDate!.Value;
        var method = request.Method.Trim();
        var reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim();
        var payloadHash = HashOf(studentNumber, invoiceNumber, amount, date, method, reference);

        var existing = await _db.PaymentRequests.FindAsync([requestId], ct);
        if (existing is not null)
            return ReplayOf(existing, payloadHash);

        var batchCode = ResolveBatchCode(method);
        ValidateDate(date);

        var customer = await _bc.GetCustomerByNumberAsync(studentNumber, ct)
            ?? throw new PortalException(
                StatusCodes.Status422UnprocessableEntity,
                "Unknown student",
                "No Business Central customer has that student number.");

        if (invoiceNumber is not null)
            await ValidateInvoicePaymentAsync(customer.Number, invoiceNumber, amount, ct);

        var row = new PaymentRequest
        {
            Id = requestId,
            StudentNumber = customer.Number,
            InvoiceNumber = invoiceNumber,
            Amount = amount,
            PaymentDate = date,
            Method = method,
            Reference = reference,
            PayloadHash = payloadHash,
            Status = PaymentRequestStatus.Pending,
            CreatedByUserId = userId,
            CreatedAtUtc = _time.GetUtcNow().UtcDateTime
        };

        _db.PaymentRequests.Add(row);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            _db.Entry(row).State = EntityState.Detached;
            var winner = await _db.PaymentRequests.AsNoTracking()
                .FirstOrDefaultAsync(payment => payment.Id == requestId, ct);
            if (winner is null)
                throw;

            return ReplayOf(winner, payloadHash);
        }

        await AuditAsync(
            "payment.requested",
            row,
            true,
            new
            {
                row.StudentNumber,
                row.InvoiceNumber,
                row.Amount,
                row.PaymentDate,
                row.Method,
                row.Reference
            },
            ct);

        await ExecutePostingAsync(row, batchCode, ct);
        await AuditOutcomeAsync(row, ct);
        return ToResult(row);
    }

    public async Task<PaymentResultDto> GetAsync(Guid requestId, CancellationToken ct)
    {
        RequireOfficer();
        var row = await _db.PaymentRequests.AsNoTracking()
            .FirstOrDefaultAsync(payment => payment.Id == requestId, ct)
            ?? throw new NotFoundException($"Payment request {requestId} not found");
        return ToResult(row);
    }

    public async Task<PaymentResultDto> ReconcileAsync(Guid requestId, CancellationToken ct)
    {
        RequireOfficer();
        var row = await _db.PaymentRequests.FindAsync([requestId], ct)
            ?? throw new NotFoundException($"Payment request {requestId} not found");

        if (row.Status is PaymentRequestStatus.Posted or PaymentRequestStatus.Failed)
            return ToResult(row);

        if (row.Status == PaymentRequestStatus.Pending &&
            _time.GetUtcNow().UtcDateTime - row.CreatedAtUtc < TimeSpan.FromMinutes(2))
        {
            throw new PortalException(
                StatusCodes.Status409Conflict,
                "Still in progress",
                "This payment was submitted moments ago. Check again in a couple of minutes.");
        }

        var batchCode = ResolveBatchCode(row.Method);
        var externalDocumentNumber = ReferenceOf(row.Id);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await AcquirePaymentLocksAsync(row, batchCode, ct);

        var posted = await FindLedgerEntryAsync(externalDocumentNumber, ct);
        if (posted is not null)
        {
            MarkPosted(row, posted);
        }
        else
        {
            var batchId = await _bc.ResolvePaymentJournalIdAsync(batchCode, ct);
            var lines = await _bc.GetPaymentJournalLinesAsync(batchId, ct);
            foreach (var line in lines.Where(line =>
                         line.ExternalDocumentNumber == externalDocumentNumber))
            {
                await _bc.DeletePaymentLineAsync(batchId, line.Id, ct);
            }

            Fail(row, "The payment was not posted to Business Central. Record it again.");
        }

        await SaveAndCommitAsync(transaction, ct);
        await AuditAsync(
            row.Status == PaymentRequestStatus.Posted ? "payment.reconciled" : "payment.failed",
            row,
            row.Status == PaymentRequestStatus.Posted,
            new { row.Status, row.ReceiptNumber, row.LedgerEntryNumber, row.FailureReason },
            ct);
        return ToResult(row);
    }

    private async Task ExecutePostingAsync(PaymentRequest row, string batchCode, CancellationToken ct)
    {
        var externalDocumentNumber = ReferenceOf(row.Id);
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await AcquirePaymentLocksAsync(row, batchCode, ct);

        Guid? batchId = null;
        try
        {
            try
            {
                batchId = await _bc.ResolvePaymentJournalIdAsync(batchCode, ct);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Payment journal batch {BatchCode} is unavailable", batchCode);
                Fail(row, "The configured payment journal is unavailable. Contact an administrator.");
                await SaveAndCommitAsync(transaction, ct);
                return;
            }

            var posted = await FindLedgerEntryAsync(externalDocumentNumber, ct);
            if (posted is not null)
            {
                MarkPosted(row, posted);
            }
            else
            {
                if (row.InvoiceNumber is not null)
                {
                    try
                    {
                        await ValidateInvoicePaymentAsync(
                            row.StudentNumber,
                            row.InvoiceNumber,
                            row.Amount,
                            ct);
                    }
                    catch (PortalException ex)
                    {
                        Fail(row, ex.PublicDetail);
                        await SaveAndCommitAsync(transaction, ct);
                        return;
                    }
                }

                var lines = await _bc.GetPaymentJournalLinesAsync(batchId.Value, ct);
                if (lines.Any(line => line.ExternalDocumentNumber != externalDocumentNumber))
                {
                    Fail(
                        row,
                        "The payment journal holds unposted lines from someone else. " +
                        "Clear them in Business Central, then record the payment again.");
                    await SaveAndCommitAsync(transaction, ct);
                    return;
                }

                if (lines.Count == 0)
                {
                    await _bc.CreatePaymentLineAsync(
                        batchId.Value,
                        new BcPaymentLineRequest(
                            row.StudentNumber,
                            row.PaymentDate,
                            row.Amount,
                            row.InvoiceNumber,
                            externalDocumentNumber,
                            DescriptionFor(row)),
                        ct);
                }

                await _bc.PostJournalAsync(batchId.Value, ct);
                posted = await FindLedgerEntryAsync(externalDocumentNumber, ct);

                if (posted is not null)
                    MarkPosted(row, posted);
                else
                    MarkUnknown(
                        row,
                        "Business Central accepted the post, but the payment is not in the ledger yet. Reconcile it shortly.");
            }
        }
        catch (BcRejectedException ex)
        {
            await ResolveAfterRejectionAsync(row, batchId, externalDocumentNumber, ex, ct);
        }
        catch (BcIntegrationException ex)
        {
            _logger.LogError(
                ex,
                "Payment {RequestId} hit a Business Central problem ({Kind})",
                row.Id,
                ex.Kind);
            await ResolveAfterErrorAsync(row, batchId, externalDocumentNumber, ex, ct);
        }
        await SaveAndCommitAsync(transaction, ct);
    }

    private async Task ResolveAfterErrorAsync(
        PaymentRequest row,
        Guid? batchId,
        string externalDocumentNumber,
        BcIntegrationException exception,
        CancellationToken ct)
    {
        try
        {
            var posted = await FindLedgerEntryAsync(externalDocumentNumber, ct);
            if (posted is not null)
            {
                MarkPosted(row, posted);
                return;
            }
        }
        catch (BcIntegrationException lookupException)
        {
            _logger.LogWarning(
                lookupException,
                "Could not check the ledger for payment {RequestId} after a Business Central error",
                row.Id);
            MarkUnknown(
                row,
                "Business Central could not be reached, so the outcome is not known yet. Reconcile it shortly.");
            return;
        }

        if (exception.OutcomeUnknown)
        {
            MarkUnknown(
                row,
                "Business Central did not confirm the payment outcome. Reconcile this payment before retrying.");
            return;
        }

        if (batchId is not null)
        {
            try
            {
                await RemovePaymentLinesAsync(batchId.Value, externalDocumentNumber, ct);
            }
            catch (BcIntegrationException cleanupException)
            {
                _logger.LogWarning(
                    cleanupException,
                    "Could not remove the journal line for payment {RequestId}",
                    row.Id);
                MarkUnknown(row, "The payment failed and its journal line could not be removed. Reconcile it.");
                return;
            }
        }

        Fail(row, "Business Central rejected the payment.");
    }

    private async Task ResolveAfterRejectionAsync(
        PaymentRequest row,
        Guid? batchId,
        string externalDocumentNumber,
        BcRejectedException exception,
        CancellationToken ct)
    {
        try
        {
            var posted = await FindLedgerEntryAsync(externalDocumentNumber, ct);
            if (posted is not null)
            {
                MarkPosted(row, posted);
                return;
            }
        }
        catch (BcIntegrationException lookupException)
        {
            _logger.LogWarning(
                lookupException,
                "Could not check the ledger for rejected payment {RequestId}",
                row.Id);
            MarkUnknown(
                row,
                "Business Central could not confirm whether the payment was posted. Reconcile it shortly.");
            return;
        }

        if (batchId is not null)
        {
            try
            {
                await RemovePaymentLinesAsync(batchId.Value, externalDocumentNumber, ct);
            }
            catch (BcIntegrationException cleanupException)
            {
                _logger.LogWarning(
                    cleanupException,
                    "Could not remove the rejected journal line for payment {RequestId}",
                    row.Id);
                MarkUnknown(row, "The payment was rejected and its journal line could not be removed. Reconcile it.");
                return;
            }
        }

        Fail(row, exception.PublicDetail);
    }

    private async Task RemovePaymentLinesAsync(
        Guid batchId,
        string externalDocumentNumber,
        CancellationToken ct)
    {
        var lines = await _bc.GetPaymentJournalLinesAsync(batchId, ct);
        foreach (var line in lines.Where(line =>
                     line.ExternalDocumentNumber == externalDocumentNumber))
        {
            await _bc.DeletePaymentLineAsync(batchId, line.Id, ct);
        }
    }

    private async Task ValidateInvoicePaymentAsync(
        string studentNumber,
        string invoiceNumber,
        decimal amount,
        CancellationToken ct)
    {
        var invoice = await _bc.GetPostedSalesInvoiceByNumberAsync(studentNumber, invoiceNumber, ct)
            ?? throw new PortalException(
                StatusCodes.Status422UnprocessableEntity,
                "Unknown invoice",
                "That student has no posted invoice with that number.");

        if (invoice.RemainingAmount <= 0)
        {
            throw new PortalException(
                StatusCodes.Status409Conflict,
                "Invoice already settled",
                "That invoice has nothing left to pay.");
        }

        if (amount > invoice.RemainingAmount)
        {
            throw new PortalException(
                StatusCodes.Status422UnprocessableEntity,
                "Amount too high",
                $"The amount is more than the invoice's outstanding {invoice.RemainingAmount:0.00}. " +
                "Record the extra as a separate on-account payment.");
        }
    }

    private async Task<BcCustomerLedgerEntryDto?> FindLedgerEntryAsync(
        string externalDocumentNumber,
        CancellationToken ct)
    {
        var page = await _bc.QueryCustomerLedgerEntriesAsync(
            new BcLedgerEntryQuery(null, PaymentTypes, null, null, 0, 1, externalDocumentNumber),
            ct);
        return page.Items.FirstOrDefault();
    }

    private async Task AcquirePaymentLocksAsync(
        PaymentRequest row,
        string batchCode,
        CancellationToken ct)
    {
        var lockKeys = new List<long> { LockKeyFor("batch:" + batchCode) };
        if (row.InvoiceNumber is not null)
            lockKeys.Add(LockKeyFor($"invoice:{row.StudentNumber}:{row.InvoiceNumber}"));

        foreach (var lockKey in lockKeys.Distinct().Order())
            await _db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({lockKey})", ct);
    }

    private void MarkPosted(PaymentRequest row, BcCustomerLedgerEntryDto entry)
    {
        row.Status = PaymentRequestStatus.Posted;
        row.ReceiptNumber = entry.DocumentNumber;
        row.LedgerEntryNumber = entry.EntryNumber;
        row.FailureReason = null;
        row.CompletedAtUtc = _time.GetUtcNow().UtcDateTime;
    }

    private void Fail(PaymentRequest row, string reason)
    {
        row.Status = PaymentRequestStatus.Failed;
        row.FailureReason = reason;
        row.CompletedAtUtc = _time.GetUtcNow().UtcDateTime;
    }

    private void MarkUnknown(PaymentRequest row, string reason)
    {
        row.Status = PaymentRequestStatus.Unknown;
        row.FailureReason = reason;
    }

    private async Task SaveAndCommitAsync(IDbContextTransaction transaction, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task AuditOutcomeAsync(PaymentRequest row, CancellationToken ct)
    {
        var (action, success) = row.Status switch
        {
            PaymentRequestStatus.Posted => ("payment.posted", true),
            PaymentRequestStatus.Failed => ("payment.failed", false),
            PaymentRequestStatus.Unknown => ("payment.outcome_unknown", false),
            _ => ("payment.pending", false)
        };

        await AuditAsync(
            action,
            row,
            success,
            new { row.Status, row.ReceiptNumber, row.LedgerEntryNumber, row.FailureReason },
            ct);
    }

    private async Task AuditAsync(
        string action,
        PaymentRequest row,
        bool success,
        object details,
        CancellationToken ct)
    {
        try
        {
            await _audit.WriteAsync(action, "PaymentRequest", row.Id.ToString(), success, details, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit write failed for {Action} on payment {RequestId}", action, row.Id);
        }
    }

    private Guid RequireOfficer()
    {
        if (!_user.IsAuthenticated)
            throw new NotAuthenticatedException();
        if (!_user.IsInRole(Roles.FinanceOfficer))
            throw new ForbiddenException("Caller is not in the FinanceOfficer role");

        return _user.UserId ?? throw new NotAuthenticatedException();
    }

    private PaymentResultDto ReplayOf(PaymentRequest existing, string payloadHash) =>
        existing.PayloadHash != payloadHash
            ? throw new PortalException(
                StatusCodes.Status409Conflict,
                "Request id already used",
                "That request id was already used for a different payment. Generate a new one.")
            : ToResult(existing);

    private static PaymentResultDto ToResult(PaymentRequest payment) => new(
        payment.Id,
        payment.Status,
        payment.ReceiptNumber,
        payment.StudentNumber,
        payment.InvoiceNumber,
        payment.Amount,
        payment.PaymentDate,
        payment.Method,
        payment.Status switch
        {
            PaymentRequestStatus.Posted => null,
            PaymentRequestStatus.Pending => "The payment is being recorded.",
            _ => payment.FailureReason
        });

    private string ResolveBatchCode(string method)
    {
        var batches = new Dictionary<string, string>(_options.MethodBatches, StringComparer.OrdinalIgnoreCase);
        return batches.TryGetValue(method, out var batchCode)
            ? batchCode.Trim()
            : throw new PortalException(
                StatusCodes.Status400BadRequest,
                "Unknown payment method",
                $"Payment method must be one of: {string.Join(", ", batches.Keys)}.");
    }

    private void ValidateDate(DateOnly date)
    {
        var today = DateOnly.FromDateTime(
            _time.GetUtcNow().ToOffset(TimeSpan.FromHours(3)).DateTime);

        if (date > today)
        {
            throw new PortalException(
                StatusCodes.Status400BadRequest,
                "Invalid payment date",
                "The payment date cannot be in the future.");
        }

        if (date < today.AddDays(-_options.MaxBackdateDays))
        {
            throw new PortalException(
                StatusCodes.Status400BadRequest,
                "Invalid payment date",
                $"The payment date cannot be more than {_options.MaxBackdateDays} days in the past.");
        }
    }

    private static string ReferenceOf(Guid id) => id.ToString("N");

    private static string DescriptionFor(PaymentRequest row)
    {
        var description = $"{row.Method} payment" +
            (row.Reference is null ? string.Empty : $" ref {row.Reference}");
        return description.Length > 100 ? description[..100] : description;
    }

    private static long LockKeyFor(string resource)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("payment-lock:" + resource.ToUpperInvariant()));
        return BinaryPrimitives.ReadInt64LittleEndian(hash);
    }

    private static string HashOf(
        string student,
        string? invoice,
        decimal amount,
        DateOnly date,
        string method,
        string? reference) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(FormattableString.Invariant(
            $"{student}|{invoice}|{amount:F2}|{date:yyyy-MM-dd}|{method.ToUpperInvariant()}|{reference}"))));
}
