using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace UniversityPortal.API.Tests;

public class PaymentRecordingTests : IClassFixture<PortalApiFactory>
{
    private readonly PortalApiFactory _factory;

    public PaymentRecordingTests(PortalApiFactory factory)
    {
        _factory = factory;
        _factory.Bc.ResetPaymentState();
    }

    [Fact]
    public async Task Recording_a_payment_posts_once_and_replays_the_same_receipt()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        var requestId = Guid.NewGuid();
        var request = PaymentRequest(
            requestId,
            amount: 125,
            invoiceNumber: FakeBusinessCentralClient.InvoiceOfA);

        using var first = await officer.PostAsJsonAsync("/api/officer/finance/payments", request);
        using var firstBody = await ReadBodyAsync(first);
        using var retry = await officer.PostAsJsonAsync("/api/officer/finance/payments", request);
        using var retryBody = await ReadBodyAsync(retry);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal("Posted", firstBody.RootElement.GetProperty("status").GetString());
        Assert.Equal(125m, firstBody.RootElement.GetProperty("amount").GetDecimal());
        Assert.Equal(
            firstBody.RootElement.GetProperty("receiptNumber").GetString(),
            retryBody.RootElement.GetProperty("receiptNumber").GetString());
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(1, _factory.Bc.CreatedPaymentLineCount);
        Assert.Equal(1, _factory.Bc.PostedPaymentCount);

        using var get = await officer.GetAsync($"/api/officer/finance/payment-requests/{requestId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task A_request_id_cannot_be_reused_with_a_different_payment()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        var requestId = Guid.NewGuid();
        using var first = await officer.PostAsJsonAsync(
            "/api/officer/finance/payments",
            PaymentRequest(requestId, amount: 125));
        using var changed = await officer.PostAsJsonAsync(
            "/api/officer/finance/payments",
            PaymentRequest(requestId, amount: 126));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        Assert.Equal(1, _factory.Bc.PostedPaymentCount);
    }

    [Fact]
    public async Task A_payment_cannot_exceed_the_invoice_balance()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        using var response = await officer.PostAsJsonAsync(
            "/api/officer/finance/payments",
            PaymentRequest(Guid.NewGuid(), amount: 1000.01m, invoiceNumber: FakeBusinessCentralClient.InvoiceOfA));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, _factory.Bc.CreatedPaymentLineCount);
        Assert.Equal(0, _factory.Bc.PostedPaymentCount);
    }

    [Fact]
    public async Task A_rejected_post_removes_its_unposted_journal_line()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        _factory.Bc.SimulatePaymentPostRejection = true;

        using var response = await officer.PostAsJsonAsync(
            "/api/officer/finance/payments",
            PaymentRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(1, _factory.Bc.CreatedPaymentLineCount);
        Assert.Equal(0, _factory.Bc.PendingPaymentLineCount);
        Assert.Equal(0, _factory.Bc.PostedPaymentCount);
    }

    [Fact]
    public async Task An_uncertain_post_is_reconciled_from_the_business_central_ledger()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        _factory.Bc.SimulateUnknownPaymentOutcomeAfterPost = true;

        using var response = await officer.PostAsJsonAsync(
            "/api/officer/finance/payments",
            PaymentRequest(Guid.NewGuid()));
        using var body = await ReadBodyAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Posted", body.RootElement.GetProperty("status").GetString());
        Assert.Equal(1, _factory.Bc.PostedPaymentCount);
    }

    [Fact]
    public async Task Unknown_payment_outcome_can_be_reconciled_later()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        var requestId = Guid.NewGuid();
        _factory.Bc.SimulateUnknownPaymentOutcomeAfterPost = true;
        _factory.Bc.SimulatePaymentLedgerLookupFailureAfterPost = true;

        using var initial = await officer.PostAsJsonAsync(
            "/api/officer/finance/payments",
            PaymentRequest(requestId));
        using var initialBody = await ReadBodyAsync(initial);
        Assert.Equal(HttpStatusCode.Accepted, initial.StatusCode);
        Assert.Equal("Unknown", initialBody.RootElement.GetProperty("status").GetString());

        using var reconciled = await officer.PostAsync(
            $"/api/officer/finance/payment-requests/{requestId}/reconcile",
            content: null);
        using var reconciledBody = await ReadBodyAsync(reconciled);
        Assert.Equal(HttpStatusCode.OK, reconciled.StatusCode);
        Assert.Equal("Posted", reconciledBody.RootElement.GetProperty("status").GetString());
        Assert.Equal(1, _factory.Bc.PostedPaymentCount);
    }

    [Fact]
    public async Task A_shared_journal_with_unrelated_lines_is_not_posted()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        _factory.Bc.AddUnrelatedPaymentJournalLine();

        using var response = await officer.PostAsJsonAsync(
            "/api/officer/finance/payments",
            PaymentRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(0, _factory.Bc.CreatedPaymentLineCount);
        Assert.Equal(1, _factory.Bc.PendingPaymentLineCount);
        Assert.Equal(0, _factory.Bc.PostedPaymentCount);
    }

    private static object PaymentRequest(Guid requestId, decimal amount = 125, string? invoiceNumber = null) =>
        new
        {
            requestId,
            studentNumber = FakeBusinessCentralClient.StudentA,
            invoiceNumber,
            amount,
            paymentDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).DateTime),
            method = "Cash",
            reference = "TEST-RECEIPT"
        };

    private static async Task<JsonDocument> ReadBodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonDocument>()
        ?? throw new InvalidOperationException("The payment endpoint returned an empty response.");
}
