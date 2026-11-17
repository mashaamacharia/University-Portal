using System.Collections.Concurrent;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;
using UniversityPortal.API.Integrations.BusinessCentral.DTOs;

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

    public IReadOnlyCollection<string> CustomerNumbersQueried => _queried.ToArray();

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
        CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task PostSalesInvoiceAsync(Guid invoiceId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task DeleteDraftSalesInvoiceAsync(Guid invoiceId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<BcSalesInvoiceDto?> GetSalesInvoiceByIdAsync(
        Guid invoiceId,
        CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<BcSalesInvoiceDto>> FindSalesInvoicesByExternalDocumentNumberAsync(
        string key,
        CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<BcSalesInvoiceLineDto>> GetSalesInvoiceLinesAsync(
        Guid invoiceId,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BcSalesInvoiceLineDto>>([]);

    public Task<BcPagedList<BcSalesInvoiceDto>> QuerySalesInvoicesAsync(
        BcInvoiceQuery query,
        CancellationToken ct = default)
    {
        Record(query.CustomerNumber);
        var matches = _invoices
            .Where(invoice => query.CustomerNumber is null || invoice.CustomerNumber == query.CustomerNumber)
            .ToList();
        return Task.FromResult(new BcPagedList<BcSalesInvoiceDto>(matches, matches.Count));
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
