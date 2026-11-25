using System.Collections.Concurrent;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;
using UniversityPortal.API.Integrations.BusinessCentral.DTOs;
using UniversityPortal.API.Integrations.BusinessCentral.Exceptions;

namespace UniversityPortal.API.Tests;

public class FakeBusinessCentralClient : IBusinessCentralClient
{
    public const string StudentA = "T-1001";
    public const string StudentB = "T-1002";
    public const string InvoiceOfA = "INV-A1";
    public const string InvoiceOfB = "INV-B1";

    private static readonly string[] PostedStatuses = ["Open", "Paid", "Corrective"];

    private readonly List<BcCustomerDto> _customers =
    [
        new()
        {
            Id = Guid.NewGuid(),
            Number = StudentA,
            DisplayName = "Student A",
            Email = "a@example.test",
            CurrencyCode = "KES",
            Blocked = "_x0020_"
        },
        new()
        {
            Id = Guid.NewGuid(),
            Number = StudentB,
            DisplayName = "Student B",
            Email = "b@example.test",
            CurrencyCode = "KES",
            Blocked = "_x0020_"
        }
    ];

    private readonly List<BcSalesInvoiceDto> _invoices =
    [
        MakeInvoice(InvoiceOfA, StudentA, "Open"),
        MakeInvoice(InvoiceOfB, StudentB, "Open"),
        MakeInvoice("INV-A-DRAFT", StudentA, "Draft")
    ];

    private readonly ConcurrentQueue<string> _queried = new();
    private readonly ConcurrentDictionary<Guid, BcSalesInvoiceDto> _createdInvoices = new();
    private readonly ConcurrentDictionary<Guid, IReadOnlyList<BcSalesInvoiceLineDto>> _createdInvoiceLines = new();
    private readonly object _paymentLock = new();
    private readonly List<BcPaymentLineDto> _paymentLines = [];
    private readonly List<BcCustomerLedgerEntryDto> _paymentLedgerEntries = [];
    private readonly Guid _paymentJournalId = Guid.NewGuid();
    private int _paymentLedgerLookupFailures;

    public IReadOnlyCollection<string> CustomerNumbersQueried => _queried.ToArray();

    public int CreatedInvoiceCount => _createdInvoices.Count;

    public int CreatedPaymentLineCount { get; private set; }

    public int PostedPaymentCount
    {
        get
        {
            lock (_paymentLock)
                return _paymentLedgerEntries.Count(entry =>
                    entry.DocumentType.Equals("Payment", StringComparison.OrdinalIgnoreCase));
        }
    }

    public int PendingPaymentLineCount
    {
        get
        {
            lock (_paymentLock)
                return _paymentLines.Count;
        }
    }

    public Guid NonPortalInvoiceId => _invoices[0].Id;

    public bool SimulateUnknownOutcomeBeforeCreate { get; set; }

    public bool SimulateUnknownOutcomeAfterCreate { get; set; }

    public bool SimulateUnknownOutcomeAfterPost { get; set; }

    public bool SimulateUnknownOutcomeAfterDelete { get; set; }

    public bool SimulatePostRejection { get; set; }

    public bool SimulatePaymentPostRejection { get; set; }

    public bool SimulateUnknownPaymentOutcomeAfterPost { get; set; }

    public bool SimulatePaymentLedgerLookupFailureAfterPost { get; set; }

    public void AddUnrelatedPaymentJournalLine()
    {
        lock (_paymentLock)
        {
            _paymentLines.Add(new BcPaymentLineDto
            {
                Id = Guid.NewGuid(),
                CustomerNumber = StudentA,
                ExternalDocumentNumber = "EXTERNAL-ENTRY"
            });
        }
    }

    public void MarkPaymentReversed(string receiptNumber)
    {
        lock (_paymentLock)
        {
            var entry = _paymentLedgerEntries.FirstOrDefault(
                payment => payment.DocumentNumber == receiptNumber);
            if (entry is null)
                throw new InvalidOperationException($"Payment receipt '{receiptNumber}' was not found.");
            entry.Reversed = true;
        }
    }

    public void AddLedgerEntry(BcCustomerLedgerEntryDto entry)
    {
        lock (_paymentLock)
            _paymentLedgerEntries.Add(entry);
    }

    public void Reset()
    {
        _queried.Clear();
        ResetPaymentState();
    }

    public void ResetPaymentState()
    {
        lock (_paymentLock)
        {
            _paymentLines.Clear();
            _paymentLedgerEntries.Clear();
            CreatedPaymentLineCount = 0;
            _paymentLedgerLookupFailures = 0;
        }

        SimulatePaymentPostRejection = false;
        SimulateUnknownPaymentOutcomeAfterPost = false;
        SimulatePaymentLedgerLookupFailureAfterPost = false;
    }

    private void Record(string? number)
    {
        if (number is not null)
            _queried.Enqueue(number);
    }

    private static BcSalesInvoiceDto MakeInvoice(string number, string customer, string status) => new()
    {
        Id = Guid.NewGuid(),
        Number = number,
        CustomerNumber = customer,
        CustomerName = customer,
        Status = status,
        CurrencyCode = "KES",
        InvoiceDate = new DateOnly(2026, 3, 1),
        PostingDate = new DateOnly(2026, 3, 1),
        DueDate = new DateOnly(2026, 3, 31),
        TotalAmountExcludingTax = 1000,
        TotalTaxAmount = 0,
        TotalAmountIncludingTax = 1000,
        RemainingAmount = 1000
    };

    public Task<BcCustomerDto?> GetCustomerByNumberAsync(
        string customerNumber,
        CancellationToken ct = default)
    {
        Record(customerNumber);
        return Task.FromResult(_customers.FirstOrDefault(customer => customer.Number == customerNumber));
    }

    public Task<BcItemDto?> GetItemByNumberAsync(string number, CancellationToken ct = default) =>
        Task.FromResult<BcItemDto?>(number == "SER203"
            ? new BcItemDto { Id = Guid.NewGuid(), Number = number, DisplayName = "Service", UnitPrice = 100 }
            : null);

    public Task<BcAccountDto?> GetAccountByNumberAsync(string number, CancellationToken ct = default) =>
        Task.FromResult<BcAccountDto?>(number == "6710"
            ? new BcAccountDto { Id = Guid.NewGuid(), Number = number, DisplayName = "Fees", DirectPosting = true }
            : null);

    public Task<BcCustomerFinancialDetailsDto?> GetCustomerFinancialDetailsAsync(
        string customerNumber,
        CancellationToken ct = default)
    {
        Record(customerNumber);
        return Task.FromResult<BcCustomerFinancialDetailsDto?>(
            new BcCustomerFinancialDetailsDto { Number = customerNumber, Balance = 1000 });
    }

    public Task<IReadOnlyList<BcSalesInvoiceDto>> GetPostedSalesInvoicesAsync(
        string customerNumber,
        int top = 50,
        CancellationToken ct = default)
    {
        Record(customerNumber);
        IReadOnlyList<BcSalesInvoiceDto> result = _invoices
            .Where(invoice => invoice.CustomerNumber == customerNumber && PostedStatuses.Contains(invoice.Status))
            .Take(top)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<BcSalesInvoiceDto?> GetPostedSalesInvoiceByNumberAsync(
        string customerNumber,
        string invoiceNumber,
        CancellationToken ct = default)
    {
        Record(customerNumber);
        return Task.FromResult(_invoices.FirstOrDefault(invoice =>
            invoice.CustomerNumber == customerNumber &&
            invoice.Number == invoiceNumber &&
            PostedStatuses.Contains(invoice.Status)));
    }

    public Task<BcSalesInvoiceDto> CreateDraftSalesInvoiceAsync(
        BcCreateInvoiceRequest request,
        CancellationToken ct = default)
    {
        if (SimulateUnknownOutcomeBeforeCreate)
        {
            SimulateUnknownOutcomeBeforeCreate = false;
            throw new BcIntegrationException(BcFailureKind.Timeout, "Simulated uncertain BC outcome");
        }

        var customer = _customers.FirstOrDefault(candidate => candidate.Number == request.CustomerNumber);
        var invoiceId = Guid.NewGuid();
        var invoiceLines = request.Lines.Select((line, index) =>
        {
            var price = line.UnitPrice ?? (line.LineType == "Item" ? 100m : 0m);
            var net = line.Quantity * price;
            return new BcSalesInvoiceLineDto
            {
                Id = Guid.NewGuid(),
                DocumentId = invoiceId,
                Sequence = (index + 1) * 10000,
                LineType = line.LineType,
                LineObjectNumber = line.Number,
                Description = line.Description ?? line.Number,
                Quantity = line.Quantity,
                UnitPrice = price,
                NetAmount = net,
                NetAmountIncludingTax = net,
                AmountIncludingTax = net
            };
        }).ToList();
        var total = invoiceLines.Sum(line => line.NetAmount);
        var invoice = new BcSalesInvoiceDto
        {
            Id = invoiceId,
            Number = $"DRAFT-{Guid.NewGuid():N}"[..18],
            ExternalDocumentNumber = request.ExternalDocumentNumber,
            CustomerId = customer?.Id ?? Guid.Empty,
            CustomerNumber = request.CustomerNumber,
            CustomerName = customer?.DisplayName ?? request.CustomerNumber,
            CurrencyCode = customer?.CurrencyCode ?? "KES",
            InvoiceDate = request.InvoiceDate ?? new DateOnly(2026, 11, 17),
            DueDate = (request.InvoiceDate ?? new DateOnly(2026, 11, 17)).AddDays(30),
            TotalAmountExcludingTax = total,
            TotalAmountIncludingTax = total,
            RemainingAmount = total,
            Status = "Draft"
        };
        _createdInvoices[invoiceId] = invoice;
        _createdInvoiceLines[invoiceId] = invoiceLines;
        if (SimulateUnknownOutcomeAfterCreate)
        {
            SimulateUnknownOutcomeAfterCreate = false;
            throw new BcIntegrationException(BcFailureKind.Timeout, "Simulated uncertain BC outcome");
        }

        return Task.FromResult(invoice);
    }

    public Task PostSalesInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (!_createdInvoices.TryGetValue(invoiceId, out var invoice))
            throw new BcIntegrationException(BcFailureKind.NotFound, $"Invoice {invoiceId} not found");

        if (SimulatePostRejection)
        {
            SimulatePostRejection = false;
            throw new BcRejectedException("Posting date is not allowed.", "Application_Error", null);
        }

        if (invoice.Status != "Draft")
            throw new BcRejectedException(
                "The action can be applied to a draft invoice only.",
                "Application_Error",
                null);

        invoice.Status = "Open";
        invoice.Number = $"POST-{Guid.NewGuid():N}"[..16];
        invoice.PostingDate = new DateOnly(2026, 11, 18);

        if (SimulateUnknownOutcomeAfterPost)
        {
            SimulateUnknownOutcomeAfterPost = false;
            throw new BcIntegrationException(BcFailureKind.Timeout, "Simulated uncertain post outcome");
        }

        return Task.CompletedTask;
    }

    public Task DeleteDraftSalesInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        if (!_createdInvoices.TryGetValue(invoiceId, out var invoice))
            throw new BcIntegrationException(BcFailureKind.NotFound, $"Invoice {invoiceId} not found");

        if (invoice.Status != "Draft")
            throw new BcRejectedException("Only a draft invoice can be deleted.", "Application_Error", null);

        _createdInvoices.TryRemove(invoiceId, out _);
        _createdInvoiceLines.TryRemove(invoiceId, out _);
        if (SimulateUnknownOutcomeAfterDelete)
        {
            SimulateUnknownOutcomeAfterDelete = false;
            throw new BcIntegrationException(BcFailureKind.Timeout, "Simulated uncertain delete outcome");
        }

        return Task.CompletedTask;
    }

    public Task<BcSalesInvoiceDto?> GetSalesInvoiceByIdAsync(
        Guid invoiceId,
        CancellationToken ct = default) =>
        Task.FromResult(_createdInvoices.TryGetValue(invoiceId, out var invoice)
            ? invoice
            : _invoices.FirstOrDefault(existing => existing.Id == invoiceId));

    public Task<IReadOnlyList<BcSalesInvoiceDto>> FindSalesInvoicesByExternalDocumentNumberAsync(
        string key,
        CancellationToken ct = default)
    {
        IReadOnlyList<BcSalesInvoiceDto> matches = _invoices
            .Concat(_createdInvoices.Values)
            .Where(invoice => invoice.ExternalDocumentNumber == key)
            .Take(5)
            .ToList();
        return Task.FromResult(matches);
    }

    public Task<IReadOnlyList<BcSalesInvoiceLineDto>> GetSalesInvoiceLinesAsync(
        Guid invoiceId,
        CancellationToken ct = default) =>
        Task.FromResult(_createdInvoiceLines.TryGetValue(invoiceId, out var lines)
            ? lines
            : (IReadOnlyList<BcSalesInvoiceLineDto>)[]);

    public Task<BcPagedList<BcSalesInvoiceDto>> QuerySalesInvoicesAsync(
        BcInvoiceQuery query,
        CancellationToken ct = default)
    {
        Record(query.CustomerNumber);
        var matches = _invoices
            .Concat(_createdInvoices.Values)
            .Where(invoice => query.CustomerNumber is null || invoice.CustomerNumber == query.CustomerNumber)
            .Where(invoice => query.Statuses is not { Count: > 0 } ||
                query.Statuses.Contains(invoice.Status, StringComparer.OrdinalIgnoreCase))
            .Where(invoice => query.From is null || invoice.InvoiceDate >= query.From)
            .Where(invoice => query.To is null || invoice.InvoiceDate <= query.To)
            .ToList();
        var page = matches.Skip(query.Skip).Take(query.Top).ToList();
        return Task.FromResult(new BcPagedList<BcSalesInvoiceDto>(page, matches.Count));
    }

    public Task<BcPagedList<BcCustomerLedgerEntryDto>> QueryCustomerLedgerEntriesAsync(
        BcLedgerEntryQuery query,
        CancellationToken ct = default)
    {
        Record(query.CustomerNumber);
        lock (_paymentLock)
        {
            if (_paymentLedgerLookupFailures > 0)
            {
                _paymentLedgerLookupFailures--;
                throw new BcIntegrationException(
                    BcFailureKind.Timeout,
                    "Simulated temporary payment ledger lookup failure");
            }

            var matches = _paymentLedgerEntries
                .Where(entry => query.CustomerNumber is null || entry.CustomerNumber == query.CustomerNumber)
                .Where(entry => query.ExternalDocumentNumber is null ||
                    entry.ExternalDocumentNumber == query.ExternalDocumentNumber)
                .Where(entry => query.DocumentNumber is null ||
                    entry.DocumentNumber == query.DocumentNumber)
                .Where(entry => query.ClosedByEntryNumber is null ||
                    entry.ClosedByEntryNumber == query.ClosedByEntryNumber)
                .Where(entry => query.DocumentTypes is not { Count: > 0 } ||
                    query.DocumentTypes.Contains(entry.DocumentType, StringComparer.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult(new BcPagedList<BcCustomerLedgerEntryDto>(
                matches.Skip(query.Skip).Take(query.Top).ToList(),
                matches.Count));
        }
    }

    public Task<Guid> ResolvePaymentJournalIdAsync(
        string batchCode,
        CancellationToken ct = default) =>
        batchCode.Equals("GENERAL", StringComparison.OrdinalIgnoreCase)
            ? Task.FromResult(_paymentJournalId)
            : throw new InvalidOperationException($"Unknown payment journal batch '{batchCode}'.");

    public Task<IReadOnlyList<BcPaymentLineDto>> GetPaymentJournalLinesAsync(
        Guid batchId,
        CancellationToken ct = default)
    {
        if (batchId != _paymentJournalId)
            throw new BcIntegrationException(BcFailureKind.NotFound, "Payment journal not found.");
        lock (_paymentLock)
            return Task.FromResult<IReadOnlyList<BcPaymentLineDto>>(_paymentLines.ToList());
    }

    public Task<BcPaymentLineDto> CreatePaymentLineAsync(
        Guid batchId,
        BcPaymentLineRequest line,
        CancellationToken ct = default)
    {
        if (batchId != _paymentJournalId)
            throw new BcIntegrationException(BcFailureKind.NotFound, "Payment journal not found.");

        lock (_paymentLock)
        {
            var paymentLine = new BcPaymentLineDto
            {
                Id = Guid.NewGuid(),
                LineNumber = _paymentLines.Count + 1,
                CustomerNumber = line.CustomerNumber,
                PostingDate = line.PostingDate,
                ExternalDocumentNumber = line.ExternalDocumentNumber,
                Amount = -Math.Abs(line.ReceivedAmount),
                AppliesToInvoiceNumber = line.AppliesToInvoiceNumber ?? string.Empty,
                Description = line.Description
            };
            _paymentLines.Add(paymentLine);
            CreatedPaymentLineCount++;
            return Task.FromResult(paymentLine);
        }
    }

    public Task DeletePaymentLineAsync(
        Guid batchId,
        Guid lineId,
        CancellationToken ct = default)
    {
        if (batchId != _paymentJournalId)
            throw new BcIntegrationException(BcFailureKind.NotFound, "Payment journal not found.");
        lock (_paymentLock)
            _paymentLines.RemoveAll(line => line.Id == lineId);
        return Task.CompletedTask;
    }

    public Task PostJournalAsync(Guid batchId, CancellationToken ct = default)
    {
        if (batchId != _paymentJournalId)
            throw new BcIntegrationException(BcFailureKind.NotFound, "Payment journal not found.");

        if (SimulatePaymentPostRejection)
        {
            SimulatePaymentPostRejection = false;
            throw new BcRejectedException("Posting date is not allowed.", "Application_Error", null);
        }

        lock (_paymentLock)
        {
            foreach (var line in _paymentLines)
            {
                var entry = new BcCustomerLedgerEntryDto
                {
                    EntryNumber = _paymentLedgerEntries.Count + 1,
                    CustomerNumber = line.CustomerNumber,
                    PostingDate = line.PostingDate,
                    DocumentType = "Payment",
                    DocumentNumber = $"PAY-{Guid.NewGuid():N}"[..16],
                    ExternalDocumentNumber = line.ExternalDocumentNumber,
                    Amount = line.Amount,
                    Description = line.Description
                };
                _paymentLedgerEntries.Add(entry);

                if (!string.IsNullOrEmpty(line.AppliesToInvoiceNumber))
                {
                    var invoice = _invoices.FirstOrDefault(candidate =>
                        candidate.CustomerNumber == line.CustomerNumber &&
                        candidate.Number == line.AppliesToInvoiceNumber);
                    if (invoice is not null)
                    {
                        invoice.RemainingAmount = Math.Max(0, invoice.RemainingAmount - Math.Abs(line.Amount));
                        if (invoice.RemainingAmount == 0)
                            invoice.Status = "Paid";
                    }
                }
            }
            _paymentLines.Clear();
        }

        if (SimulateUnknownPaymentOutcomeAfterPost)
        {
            SimulateUnknownPaymentOutcomeAfterPost = false;
            if (SimulatePaymentLedgerLookupFailureAfterPost)
            {
                SimulatePaymentLedgerLookupFailureAfterPost = false;
                lock (_paymentLock)
                    _paymentLedgerLookupFailures = 1;
            }
            throw new BcIntegrationException(BcFailureKind.Timeout, "Simulated uncertain payment post outcome");
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BcCompanyDto>> GetCompaniesAsync(CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<BcCustomerDto>> GetCustomersAsync(
        int top = 50,
        int skip = 0,
        CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<BcCustomerDto>> SearchCustomersAsync(
        string search,
        int top = 10,
        CancellationToken ct = default)
    {
        IReadOnlyList<BcCustomerDto> result = _customers
            .Where(customer =>
                customer.Number.StartsWith(search, StringComparison.OrdinalIgnoreCase) ||
                customer.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Take(top)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<BcPagedList<BcCustomerFinancialDetailsDto>> QueryCustomerFinancialDetailsAsync(
        BcBalanceQuery query,
        CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<BcCustomerDto>> GetCustomersByNumbersAsync(
        IReadOnlyCollection<string> numbers,
        CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<BcAgedReceivablesRowDto?> GetAgedReceivablesTotalAsync(CancellationToken ct = default) =>
        throw new NotImplementedException();
}
