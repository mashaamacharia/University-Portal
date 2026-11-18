using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UniversityPortal.API.Common.Auditing;
using UniversityPortal.API.Common.Constants;
using UniversityPortal.API.Common.Exceptions;
using UniversityPortal.API.Common.Security;
using UniversityPortal.API.Infrastructure.Persistence;
using UniversityPortal.API.Integrations.BusinessCentral;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;
using UniversityPortal.API.Integrations.BusinessCentral.DTOs;
using UniversityPortal.API.Integrations.BusinessCentral.Exceptions;
using UniversityPortal.API.Modules.Finance.DTOs;
using UniversityPortal.API.Modules.Finance.Configuration;
using UniversityPortal.API.Modules.Finance.Models;
using Microsoft.Extensions.Options;

namespace UniversityPortal.API.Modules.Finance.Services;

public record InvoiceCreationResult(OfficerInvoiceDetailDto Invoice, bool Created);

public interface IInvoiceWorkflowService
{
    Task<InvoiceCreationResult> CreateDraftAsync(
        CreateInvoiceRequest request,
        string? idempotencyKey,
        CancellationToken ct);

    Task<OfficerInvoiceDetailDto> GetInvoiceAsync(Guid bcInvoiceId, CancellationToken ct);

    Task<OfficerInvoiceDetailDto> PostAsync(Guid bcInvoiceId, CancellationToken ct);

    Task DiscardAsync(Guid bcInvoiceId, CancellationToken ct);
}

public class InvoiceWorkflowService : IInvoiceWorkflowService
{
    private static readonly Regex KeyPattern = new("^[A-Za-z0-9_-]{8,35}$", RegexOptions.Compiled);
    private static readonly TimeSpan PendingGrace = TimeSpan.FromMinutes(3);

    private sealed record Line(string Type, string Number, decimal Quantity, decimal? UnitPrice, string? Description);

    private readonly IBusinessCentralClient _bc;
    private readonly PortalDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IAuditLog _audit;
    private readonly TimeProvider _time;
    private readonly FinanceOptions _finance;
    private readonly ILogger<InvoiceWorkflowService> _logger;

    public InvoiceWorkflowService(
        IBusinessCentralClient bc,
        PortalDbContext db,
        ICurrentUser user,
        IAuditLog audit,
        TimeProvider time,
        IOptions<FinanceOptions> finance,
        ILogger<InvoiceWorkflowService> logger)
    {
        _bc = bc;
        _db = db;
        _user = user;
        _audit = audit;
        _time = time;
        _finance = finance.Value;
        _logger = logger;
    }

    public async Task<OfficerInvoiceDetailDto> GetInvoiceAsync(Guid bcInvoiceId, CancellationToken ct)
    {
        RequireOfficer();
        return await LoadDetailAsync(bcInvoiceId, ct);
    }

    public async Task<OfficerInvoiceDetailDto> PostAsync(Guid bcInvoiceId, CancellationToken ct)
    {
        RequireOfficer();
        var userId = _user.UserId ?? throw new NotAuthenticatedException();
        var record = await RequirePortalInvoiceAsync(bcInvoiceId, ct);

        var invoice = await GetInvoiceByIdOrKeyAsync(record, ct);
        if (invoice is null)
        {
            await SetStateAsync(
                record,
                InvoiceRequestStatus.Discarded,
                "Invoice no longer exists in Business Central",
                ct);
            throw new NotFoundException($"BC invoice {bcInvoiceId} no longer exists");
        }

        var status = FinanceMapper.ParseStatus(invoice.Status);
        if (IsPosted(status))
        {
            await ReconcilePostedAsync(record, invoice, ct);
            return await BuildDetailAsync(invoice, ct);
        }

        if (record.Status == InvoiceRequestStatus.Discarded || status != InvoiceStatus.Draft)
        {
            throw new PortalException(
                StatusCodes.Status422UnprocessableEntity,
                "Invoice cannot be posted",
                record.Status == InvoiceRequestStatus.Discarded
                    ? "This invoice was discarded and cannot be posted."
                    : $"The invoice is '{status}' in Business Central, and only drafts can be posted.");
        }

        if (_finance.PostingRequiresDifferentOfficer && record.CreatedByUserId == userId)
        {
            throw new PortalException(
                StatusCodes.Status403Forbidden,
                "Second officer required",
                "This invoice must be posted by a different officer than the one who created it.");
        }

        try
        {
            await _bc.PostSalesInvoiceAsync(bcInvoiceId, ct);
        }
        catch (BcRejectedException ex)
        {
            var current = await GetInvoiceByIdOrKeyAsync(record, ct);
            if (current is not null && IsPosted(FinanceMapper.ParseStatus(current.Status)))
            {
                await ReconcilePostedAsync(record, current, ct);
                return await BuildDetailAsync(current, ct);
            }

            await SetStateAsync(record, record.Status, ex.PublicDetail, ct);
            await AuditAsync(
                "invoice.post.rejected",
                bcInvoiceId,
                false,
                new
                {
                    draftNumber = record.BcInvoiceNumber,
                    reason = ex.PublicDetail,
                    bcCorrelation = ex.CorrelationId
                },
                ct);
            throw;
        }
        catch (BcIntegrationException ex) when (ex.OutcomeUnknown)
        {
            BcSalesInvoiceDto? current;
            try
            {
                current = await GetInvoiceByIdOrKeyAsync(record, ct);
            }
            catch (BcIntegrationException lookupException)
            {
                _logger.LogWarning(
                    lookupException,
                    "Could not reconcile invoice {BcInvoiceId} after an uncertain post",
                    bcInvoiceId);
                current = null;
            }

            if (current is null || !IsPosted(FinanceMapper.ParseStatus(current.Status)))
            {
                await AuditAsync(
                    "invoice.post.outcome_unknown",
                    bcInvoiceId,
                    false,
                    new { draftNumber = record.BcInvoiceNumber, kind = ex.Kind.ToString() },
                    ct);
                throw;
            }

            await MarkPostedAsync(record, current, userId, ct);
            return await BuildDetailAsync(current, ct);
        }

        var posted = await GetInvoiceByIdOrKeyAsync(record, ct);
        if (posted is null || !IsPosted(FinanceMapper.ParseStatus(posted.Status)))
        {
            throw new BcIntegrationException(
                BcFailureKind.InvalidResponse,
                $"BC accepted posting invoice {bcInvoiceId}, but its posted state could not be confirmed.");
        }

        await MarkPostedAsync(record, posted, userId, ct);
        return await BuildDetailAsync(posted, ct);
    }

    public async Task DiscardAsync(Guid bcInvoiceId, CancellationToken ct)
    {
        RequireOfficer();
        var record = await RequirePortalInvoiceAsync(bcInvoiceId, ct);
        if (record.Status == InvoiceRequestStatus.Discarded)
            return;

        var invoice = await GetInvoiceByIdOrKeyAsync(record, ct);
        if (invoice is not null)
        {
            if (IsPosted(FinanceMapper.ParseStatus(invoice.Status)))
            {
                await ReconcilePostedAsync(record, invoice, ct);
                throw new PortalException(
                    StatusCodes.Status422UnprocessableEntity,
                    "Invoice already posted",
                    "A posted invoice cannot be discarded. Issue a credit memo in Business Central instead.");
            }

            try
            {
                await _bc.DeleteDraftSalesInvoiceAsync(bcInvoiceId, ct);
            }
            catch (BcRejectedException ex)
            {
                var current = await GetInvoiceByIdOrKeyAsync(record, ct);
                if (current is not null && IsPosted(FinanceMapper.ParseStatus(current.Status)))
                {
                    await ReconcilePostedAsync(record, current, ct);
                    throw new PortalException(
                        StatusCodes.Status422UnprocessableEntity,
                        "Invoice already posted",
                        "A posted invoice cannot be discarded. Issue a credit memo in Business Central instead.");
                }

                await AuditAsync(
                    "invoice.draft.discard_rejected",
                    bcInvoiceId,
                    false,
                    new
                    {
                        draftNumber = record.BcInvoiceNumber,
                        reason = ex.PublicDetail,
                        bcCorrelation = ex.CorrelationId
                    },
                    ct);
                throw;
            }
            catch (BcIntegrationException ex) when (ex.Kind == BcFailureKind.NotFound)
            {
                _logger.LogInformation(
                    "Draft invoice {BcInvoiceId} was already absent from Business Central",
                    bcInvoiceId);
            }
            catch (BcIntegrationException ex) when (ex.OutcomeUnknown)
            {
                var lookedUp = false;
                BcSalesInvoiceDto? stillThere = null;
                try
                {
                    stillThere = await GetInvoiceByIdOrKeyAsync(record, ct);
                    lookedUp = true;
                }
                catch (BcIntegrationException lookupException)
                {
                    _logger.LogWarning(
                        lookupException,
                        "Could not reconcile invoice {BcInvoiceId} after an uncertain discard",
                        bcInvoiceId);
                }

                if (!lookedUp || stillThere is not null)
                {
                    await AuditAsync(
                        "invoice.draft.discard_outcome_unknown",
                        bcInvoiceId,
                        false,
                        new { draftNumber = record.BcInvoiceNumber, kind = ex.Kind.ToString() },
                        ct);
                    throw;
                }
            }
        }

        await SetStateAsync(record, InvoiceRequestStatus.Discarded, null, ct);
        await AuditAsync(
            "invoice.draft.discarded",
            bcInvoiceId,
            true,
            new { draftNumber = record.BcInvoiceNumber, studentNumber = record.StudentNumber },
            ct);
    }

    public async Task<InvoiceCreationResult> CreateDraftAsync(
        CreateInvoiceRequest request,
        string? idempotencyKey,
        CancellationToken ct)
    {
        RequireOfficer();
        var userId = _user.UserId ?? throw new NotAuthenticatedException();

        if (idempotencyKey is null || !KeyPattern.IsMatch(idempotencyKey))
        {
            throw new PortalException(
                StatusCodes.Status400BadRequest,
                "Invalid idempotency key",
                "Send an Idempotency-Key header of 8 to 35 characters (letters, digits, '-' or '_'). " +
                "Reuse the same key to retry the same request.");
        }

        var studentNumber = request.StudentNumber.Trim();
        var lines = request.Lines.Select(line => new Line(
            line.LineType!.Value.ToString(),
            line.Number.Trim(),
            line.Quantity,
            line.UnitPrice,
            string.IsNullOrWhiteSpace(line.Description) ? null : line.Description.Trim())).ToList();
        var hash = ComputeHash(studentNumber, request.InvoiceDate, lines);
        var now = _time.GetUtcNow().UtcDateTime;
        var record = await _db.InvoiceRequests.FirstOrDefaultAsync(
            entry => entry.IdempotencyKey == idempotencyKey,
            ct);

        if (record is not null)
        {
            if (record.RequestHash != hash)
            {
                throw new PortalException(
                    StatusCodes.Status409Conflict,
                    "Idempotency key already used",
                    "This key was already used for a different invoice request. Use a new key for a new invoice.");
            }

            if (record.Status is InvoiceRequestStatus.Draft or InvoiceRequestStatus.Posted)
            {
                if (record.BcInvoiceId is not { } invoiceId)
                {
                    throw new BcIntegrationException(
                        BcFailureKind.InvalidResponse,
                        $"Invoice request {record.Id} is {record.Status} but has no Business Central invoice ID.");
                }

                return new InvoiceCreationResult(await LoadDetailAsync(invoiceId, ct), Created: false);
            }

            if (record.Status == InvoiceRequestStatus.Pending)
            {
                var found = await FindByKeyAsync(idempotencyKey, ct);
                if (found is not null)
                {
                    await AdoptAsync(record, found, recovered: true, ct);
                    return new InvoiceCreationResult(await BuildDetailAsync(found, ct), Created: false);
                }

                if (now - record.UpdatedAtUtc < PendingGrace)
                    throw InProgress();
            }
        }

        await ValidateAgainstBcAsync(studentNumber, lines, ct);

        if (record is null)
        {
            record = new InvoiceRequest
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = idempotencyKey,
                RequestHash = hash,
                StudentNumber = studentNumber,
                Status = InvoiceRequestStatus.Pending,
                CreatedByUserId = userId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            _db.InvoiceRequests.Add(record);
        }
        else
        {
            record.Status = InvoiceRequestStatus.Pending;
            record.LastError = null;
            record.UpdatedAtUtc = now;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (
            ex is DbUpdateConcurrencyException ||
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw InProgress();
        }

        BcSalesInvoiceDto created;
        var recovered = false;
        try
        {
            created = await _bc.CreateDraftSalesInvoiceAsync(new BcCreateInvoiceRequest
            {
                CustomerNumber = studentNumber,
                ExternalDocumentNumber = idempotencyKey,
                InvoiceDate = request.InvoiceDate,
                Lines = lines.Select(line => new BcCreateInvoiceLine
                {
                    LineType = line.Type,
                    Number = line.Number,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice,
                    Description = line.Description
                }).ToList()
            }, ct);
        }
        catch (BcRejectedException ex)
        {
            await SetStateAsync(record, InvoiceRequestStatus.Failed, ex.PublicDetail, ct);
            await AuditAsync(
                "invoice.draft.rejected",
                null,
                false,
                new { key = idempotencyKey, studentNumber, reason = ex.PublicDetail, bcCorrelation = ex.CorrelationId },
                ct);
            throw;
        }
        catch (BcIntegrationException ex) when (ex.OutcomeUnknown)
        {
            BcSalesInvoiceDto? found;
            try
            {
                found = await FindByKeyAsync(idempotencyKey, ct);
            }
            catch (BcIntegrationException lookupException)
            {
                _logger.LogWarning(
                    lookupException,
                    "Could not reconcile invoice request {RequestId} after an uncertain BC write",
                    record.Id);
                found = null;
            }

            if (found is null)
            {
                await SetStateAsync(record, InvoiceRequestStatus.Pending, $"Outcome unknown ({ex.Kind})", ct);
                await AuditAsync(
                    "invoice.draft.outcome_unknown",
                    null,
                    false,
                    new { key = idempotencyKey, studentNumber, kind = ex.Kind.ToString() },
                    ct);
                throw;
            }

            created = found;
            recovered = true;
        }
        catch (BcIntegrationException ex)
        {
            await SetStateAsync(record, InvoiceRequestStatus.Failed, ex.PublicDetail, ct);
            await AuditAsync(
                "invoice.draft.failed",
                null,
                false,
                new { key = idempotencyKey, studentNumber, kind = ex.Kind.ToString() },
                ct);
            throw;
        }

        await AdoptAsync(record, created, recovered, ct);
        return new InvoiceCreationResult(await BuildDetailAsync(created, ct), Created: !recovered);
    }

    private async Task ValidateAgainstBcAsync(string studentNumber, IReadOnlyList<Line> lines, CancellationToken ct)
    {
        var customerTask = _bc.GetCustomerByNumberAsync(studentNumber, ct);
        var itemsTask = LookupAsync(
            lines.Where(line => line.Type == nameof(InvoiceLineType.Item)).Select(line => line.Number),
            number => _bc.GetItemByNumberAsync(number, ct));
        var accountsTask = LookupAsync(
            lines.Where(line => line.Type == nameof(InvoiceLineType.Account)).Select(line => line.Number),
            number => _bc.GetAccountByNumberAsync(number, ct));

        await Task.WhenAll(customerTask, itemsTask, accountsTask);
        var errors = new List<string>();
        var customer = await customerTask;
        if (customer is null)
            errors.Add($"No student with number '{studentNumber}' exists in Business Central.");
        else if (IsInvoicingBlocked(customer))
            errors.Add($"Student '{studentNumber}' is blocked for invoicing in Business Central.");

        var items = await itemsTask;
        var accounts = await accountsTask;
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var lineNumber = index + 1;
            if (line.Type == nameof(InvoiceLineType.Item))
            {
                var item = items[line.Number];
                if (item is null)
                    errors.Add($"Line {lineNumber}: item '{line.Number}' does not exist in Business Central.");
                else if (item.Blocked)
                    errors.Add($"Line {lineNumber}: item '{line.Number}' is blocked.");
            }
            else
            {
                var account = accounts[line.Number];
                if (account is null)
                    errors.Add($"Line {lineNumber}: account '{line.Number}' does not exist in Business Central.");
                else if (account.Blocked)
                    errors.Add($"Line {lineNumber}: account '{line.Number}' is blocked.");
                else if (!account.DirectPosting)
                    errors.Add($"Line {lineNumber}: account '{line.Number}' does not allow direct posting.");
            }
        }

        if (errors.Count > 0)
            throw new PortalException(422, "Invoice request not valid", string.Join(" ", errors));
    }

    private static async Task<Dictionary<string, T?>> LookupAsync<T>(
        IEnumerable<string> numbers,
        Func<string, Task<T?>> lookup)
    {
        var distinct = numbers.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var results = await Task.WhenAll(distinct.Select(lookup));
        return distinct.Zip(results).ToDictionary(pair => pair.First, pair => pair.Second, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsInvoicingBlocked(BcCustomerDto customer)
    {
        var blocked = BcValue.Decode(customer.Blocked).Trim();
        return blocked.Equals("Invoice", StringComparison.OrdinalIgnoreCase) ||
               blocked.Equals("All", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<BcSalesInvoiceDto?> FindByKeyAsync(string key, CancellationToken ct)
    {
        var found = await _bc.FindSalesInvoicesByExternalDocumentNumberAsync(key, ct);
        return found.Count switch
        {
            0 => null,
            1 => found[0],
            _ => throw new PortalException(
                StatusCodes.Status409Conflict,
                "Duplicate invoices found",
                "Business Central holds more than one invoice with this key. Do not retry; ask an administrator to review.",
                $"{found.Count} BC invoices carry the key {key}")
        };
    }

    private async Task<InvoiceRequest> RequirePortalInvoiceAsync(Guid bcInvoiceId, CancellationToken ct)
    {
        var record = await _db.InvoiceRequests.FirstOrDefaultAsync(
            request => request.BcInvoiceId == bcInvoiceId,
            ct);
        if (record is not null)
            return record;

        var existsInBc = await _bc.GetSalesInvoiceByIdAsync(bcInvoiceId, ct) is not null;
        throw existsInBc
            ? new PortalException(
                StatusCodes.Status422UnprocessableEntity,
                "Not a portal invoice",
                "This invoice was not created through the portal, so it cannot be posted or discarded here.")
            : new NotFoundException($"No invoice {bcInvoiceId} in the portal or in BC");
    }

    private async Task<BcSalesInvoiceDto?> GetInvoiceByIdOrKeyAsync(InvoiceRequest record, CancellationToken ct)
    {
        var invoice = record.BcInvoiceId is { } id
            ? await _bc.GetSalesInvoiceByIdAsync(id, ct)
            : null;

        if (invoice is not null || string.IsNullOrWhiteSpace(record.IdempotencyKey))
            return invoice;

        return await FindByKeyAsync(record.IdempotencyKey, ct);
    }

    private static bool IsPosted(InvoiceStatus status) =>
        status is InvoiceStatus.Open or InvoiceStatus.Paid or InvoiceStatus.Corrective;

    private async Task MarkPostedAsync(
        InvoiceRequest record,
        BcSalesInvoiceDto posted,
        Guid postedBy,
        CancellationToken ct)
    {
        var draftNumber = record.BcInvoiceNumber;
        var now = _time.GetUtcNow().UtcDateTime;
        record.Status = InvoiceRequestStatus.Posted;
        record.BcInvoiceNumber = posted.Number;
        record.PostedByUserId = postedBy;
        record.PostedAtUtc = now;
        record.UpdatedAtUtc = now;
        record.LastError = null;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not record the posting of invoice {BcInvoiceId}", posted.Id);
        }

        await AuditAsync(
            "invoice.posted",
            posted.Id,
            true,
            new
            {
                studentNumber = record.StudentNumber,
                draftNumber,
                postedNumber = posted.Number,
                total = posted.TotalAmountIncludingTax
            },
            ct);
    }

    private async Task ReconcilePostedAsync(InvoiceRequest record, BcSalesInvoiceDto posted, CancellationToken ct)
    {
        await _db.Entry(record).ReloadAsync(ct);
        if (record.Status == InvoiceRequestStatus.Posted)
            return;

        record.Status = InvoiceRequestStatus.Posted;
        record.BcInvoiceNumber = posted.Number;
        record.UpdatedAtUtc = _time.GetUtcNow().UtcDateTime;
        record.LastError = null;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not reconcile invoice request {RequestId}", record.Id);
        }

        await AuditAsync(
            "invoice.post.reconciled",
            posted.Id,
            true,
            new
            {
                postedNumber = posted.Number,
                note = "BC showed the invoice as posted before the portal's record did"
            },
            ct);
    }

    private async Task AdoptAsync(
        InvoiceRequest record,
        BcSalesInvoiceDto bc,
        bool recovered,
        CancellationToken ct)
    {
        var status = FinanceMapper.ParseStatus(bc.Status);
        record.Status = status is InvoiceStatus.Open or InvoiceStatus.Paid or InvoiceStatus.Corrective
            ? InvoiceRequestStatus.Posted
            : InvoiceRequestStatus.Draft;
        record.BcInvoiceId = bc.Id;
        record.BcInvoiceNumber = bc.Number;
        record.LastError = null;
        record.UpdatedAtUtc = _time.GetUtcNow().UtcDateTime;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not record invoice {BcInvoiceId} on request {RequestId}", bc.Id, record.Id);
        }

        await AuditAsync(
            recovered ? "invoice.draft.recovered" : "invoice.draft.created",
            bc.Id,
            true,
            new
            {
                key = record.IdempotencyKey,
                studentNumber = record.StudentNumber,
                bcNumber = bc.Number,
                total = bc.TotalAmountIncludingTax
            },
            ct);
    }

    private async Task SetStateAsync(
        InvoiceRequest record,
        InvoiceRequestStatus status,
        string? error,
        CancellationToken ct)
    {
        record.Status = status;
        record.LastError = error is { Length: > 1000 } ? error[..1000] : error;
        record.UpdatedAtUtc = _time.GetUtcNow().UtcDateTime;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not update invoice request {RequestId}", record.Id);
        }
    }

    private async Task AuditAsync(string action, Guid? invoiceId, bool success, object details, CancellationToken ct)
    {
        try
        {
            await _audit.WriteAsync(action, "Invoice", invoiceId?.ToString(), success, details, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit write failed for {Action}", action);
        }
    }

    private async Task<OfficerInvoiceDetailDto> LoadDetailAsync(Guid bcInvoiceId, CancellationToken ct)
    {
        var invoice = await _bc.GetSalesInvoiceByIdAsync(bcInvoiceId, ct)
            ?? throw new NotFoundException($"BC invoice {bcInvoiceId} not found");
        return await BuildDetailAsync(invoice, ct);
    }

    private async Task<OfficerInvoiceDetailDto> BuildDetailAsync(BcSalesInvoiceDto invoice, CancellationToken ct)
    {
        var lines = await _bc.GetSalesInvoiceLinesAsync(invoice.Id, ct);
        return FinanceMapper.ToOfficerDetail(invoice, lines, Today());
    }

    private void RequireOfficer()
    {
        if (!_user.IsAuthenticated)
            throw new NotAuthenticatedException();
        if (!_user.IsInRole(Roles.FinanceOfficer))
            throw new ForbiddenException("Caller is not in the FinanceOfficer role");
    }

    private DateOnly Today() => DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);

    private static PortalException InProgress() => new(
        StatusCodes.Status409Conflict,
        "Request in progress",
        "A request with this key is already running, or an earlier attempt has not been confirmed yet. " +
        "Wait a few minutes, then retry with the same key. The portal checks Business Central before doing anything.");

    private static string ComputeHash(string studentNumber, DateOnly? invoiceDate, IEnumerable<Line> lines)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            StudentNumber = studentNumber.ToUpperInvariant(),
            InvoiceDate = invoiceDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Lines = lines.Select(line => new
            {
                line.Type,
                Number = line.Number.ToUpperInvariant(),
                Quantity = line.Quantity.ToString("0.#############", CultureInfo.InvariantCulture),
                UnitPrice = line.UnitPrice?.ToString("0.#############", CultureInfo.InvariantCulture),
                line.Description
            })
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
