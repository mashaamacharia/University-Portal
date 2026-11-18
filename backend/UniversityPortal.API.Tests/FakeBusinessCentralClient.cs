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

    public IReadOnlyCollection<string> CustomerNumbersQueried => _queried.ToArray();

    public int CreatedInvoiceCount => _createdInvoices.Count;

    public Guid NonPortalInvoiceId => _invoices[0].Id;

    public bool SimulateUnknownOutcomeBeforeCreate { get; set; }

    public bool SimulateUnknownOutcomeAfterCreate { get; set; }

    public bool SimulateUnknownOutcomeAfterPost { get; set; }

    public bool SimulateUnknownOutcomeAfterDelete { get; set; }

    public bool SimulatePostRejection { get; set; }

    public void Reset() => _queried.Clear();

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
        return Task.FromResult(new BcPagedList<BcCustomerLedgerEntryDto>([], 0));
    }

    public Task<IReadOnlyList<BcCompanyDto>> GetCompaniesAsync(CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<BcCustomerDto>> GetCustomersAsync(
        int top = 50,
        int skip = 0,
        CancellationToken ct = default) =>
        throw new NotImplementedException();

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
