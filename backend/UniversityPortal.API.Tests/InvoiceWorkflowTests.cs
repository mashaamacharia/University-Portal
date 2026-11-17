using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace UniversityPortal.API.Tests;

public class InvoiceWorkflowTests : IClassFixture<PortalApiFactory>
{
    private readonly PortalApiFactory _factory;

    public InvoiceWorkflowTests(PortalApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Repeating_a_draft_request_returns_the_same_invoice_without_creating_another()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        var key = $"fee-{Guid.NewGuid():N}"[..20];
        var request = new
        {
            studentNumber = FakeBusinessCentralClient.StudentA,
            lines = new object[]
            {
                new { lineType = "Item", number = "SER203", quantity = 1, unitPrice = 100 },
                new { lineType = "Account", number = "6710", quantity = 2, unitPrice = 50 }
            }
        };

        using var first = await PostInvoiceAsync(officer, request, key);
        using var firstBody = await first.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException("The invoice endpoint returned an empty response.");

        using var retry = await PostInvoiceAsync(officer, request, key);
        using var retryBody = await retry.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException("The invoice endpoint returned an empty response.");

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(
            firstBody.RootElement.GetProperty("bcInvoiceId").GetGuid(),
            retryBody.RootElement.GetProperty("bcInvoiceId").GetGuid());
        Assert.Equal("Draft", firstBody.RootElement.GetProperty("status").GetString());
        Assert.Equal(2, firstBody.RootElement.GetProperty("lines").GetArrayLength());
        Assert.Equal(1, _factory.Bc.CreatedInvoiceCount);

        var invoiceId = firstBody.RootElement.GetProperty("bcInvoiceId").GetGuid();
        using var review = await officer.GetAsync($"/api/officer/finance/invoices/{invoiceId}");
        using var reviewBody = await review.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException("The invoice review endpoint returned an empty response.");
        using var drafts = await officer.GetAsync("/api/officer/finance/invoices?status=Draft");
        using var draftsBody = await drafts.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException("The invoice list endpoint returned an empty response.");

        Assert.Equal(HttpStatusCode.OK, review.StatusCode);
        Assert.Equal(invoiceId, reviewBody.RootElement.GetProperty("bcInvoiceId").GetGuid());
        Assert.Equal(HttpStatusCode.OK, drafts.StatusCode);
        Assert.Contains(
            draftsBody.RootElement.GetProperty("items").EnumerateArray(),
            invoice => invoice.GetProperty("invoiceNumber").GetString() == firstBody.RootElement.GetProperty("invoiceNumber").GetString());
    }

    [Fact]
    public async Task Reusing_an_idempotency_key_for_a_different_request_returns_conflict()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        var key = $"fee-{Guid.NewGuid():N}"[..20];

        using var first = await PostInvoiceAsync(
            officer,
            new
            {
                studentNumber = FakeBusinessCentralClient.StudentA,
                lines = new[] { new { lineType = "Item", number = "SER203", quantity = 1, unitPrice = 100 } }
            },
            key);

        using var changed = await PostInvoiceAsync(
            officer,
            new
            {
                studentNumber = FakeBusinessCentralClient.StudentA,
                lines = new[] { new { lineType = "Item", number = "SER203", quantity = 2, unitPrice = 100 } }
            },
            key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
    }

    [Fact]
    public async Task An_uncertain_write_is_reconciled_by_external_document_number()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        var key = $"fee-{Guid.NewGuid():N}"[..20];
        var before = _factory.Bc.CreatedInvoiceCount;
        var request = new
        {
            studentNumber = FakeBusinessCentralClient.StudentA,
            lines = new[] { new { lineType = "Item", number = "SER203", quantity = 1, unitPrice = 100 } }
        };
        _factory.Bc.SimulateUnknownOutcomeAfterCreate = true;

        using var response = await PostInvoiceAsync(officer, request, key);
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException("The invoice endpoint returned an empty response.");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Draft", body.RootElement.GetProperty("status").GetString());
        Assert.Equal(before + 1, _factory.Bc.CreatedInvoiceCount);
    }

    [Fact]
    public async Task An_unconfirmed_pending_request_cannot_be_retried_before_the_grace_period()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        var key = $"fee-{Guid.NewGuid():N}"[..20];
        var request = new
        {
            studentNumber = FakeBusinessCentralClient.StudentA,
            lines = new[] { new { lineType = "Item", number = "SER203", quantity = 1 } }
        };
        _factory.Bc.SimulateUnknownOutcomeBeforeCreate = true;

        using var uncertain = await PostInvoiceAsync(officer, request, key);
        using var retry = await PostInvoiceAsync(officer, request, key);

        Assert.Equal(HttpStatusCode.GatewayTimeout, uncertain.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
    }

    [Fact]
    public async Task Catalog_validation_rejects_unknown_student_or_invoice_lines_before_creation()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        var countBefore = _factory.Bc.CreatedInvoiceCount;

        using var unknownStudent = await PostInvoiceAsync(
            officer,
            new
            {
                studentNumber = "UNKNOWN",
                lines = new[] { new { lineType = "Item", number = "SER203", quantity = 1 } }
            },
            $"chk-{Guid.NewGuid():N}"[..20]);

        using var unknownLines = await PostInvoiceAsync(
            officer,
            new
            {
                studentNumber = FakeBusinessCentralClient.StudentA,
                lines = new object[]
                {
                    new { lineType = "Item", number = "SER203", quantity = 1 },
                    new { lineType = "Item", number = "NO-SUCH-ITEM", quantity = 1 },
                    new { lineType = "Account", number = "00000", quantity = 1 }
                }
            },
            $"chk-{Guid.NewGuid():N}"[..20]);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownStudent.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownLines.StatusCode);
        var detail = await unknownLines.Content.ReadAsStringAsync();
        Assert.Contains("Line 2", detail, StringComparison.Ordinal);
        Assert.Contains("Line 3", detail, StringComparison.Ordinal);
        Assert.Equal(countBefore, _factory.Bc.CreatedInvoiceCount);
    }

    [Fact]
    public async Task Invoice_creation_requires_a_valid_key_and_finance_officer_role()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        var request = new
        {
            studentNumber = FakeBusinessCentralClient.StudentA,
            lines = new[] { new { lineType = "Item", number = "SER203", quantity = 1 } }
        };
        using var missingKey = await PostInvoiceAsync(officer, request);
        using var shortKey = await PostInvoiceAsync(officer, request, "abc");
        using var student = await _factory.ClientForRoleAsync("Student");
        using var forbidden = await PostInvoiceAsync(student, request, $"chk-{Guid.NewGuid():N}"[..20]);
        using var administrator = await _factory.ClientForRoleAsync("Administrator");
        using var adminForbidden = await PostInvoiceAsync(
            administrator,
            request,
            $"chk-{Guid.NewGuid():N}"[..20]);
        using var anonymous = await _factory.CreateClient().PostAsJsonAsync(
            "/api/officer/finance/invoices",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, missingKey.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, shortKey.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, adminForbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Invoice_creation_rejects_empty_lines_invalid_line_types_and_nonpositive_quantity()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");

        using var emptyLines = await PostInvoiceAsync(
            officer,
            new { studentNumber = FakeBusinessCentralClient.StudentA, lines = Array.Empty<object>() },
            $"chk-{Guid.NewGuid():N}"[..20]);
        using var badQuantity = await PostInvoiceAsync(
            officer,
            new
            {
                studentNumber = FakeBusinessCentralClient.StudentA,
                lines = new[] { new { lineType = "Item", number = "SER203", quantity = 0 } }
            },
            $"chk-{Guid.NewGuid():N}"[..20]);
        using var invalidLineType = await PostInvoiceAsync(
            officer,
            new
            {
                studentNumber = FakeBusinessCentralClient.StudentA,
                lines = new[] { new { lineType = "Resource", number = "X", quantity = 1 } }
            },
            $"chk-{Guid.NewGuid():N}"[..20]);

        Assert.Equal(HttpStatusCode.BadRequest, emptyLines.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badQuantity.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidLineType.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostInvoiceAsync(HttpClient client, object body, string? key = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/officer/finance/invoices")
        {
            Content = JsonContent.Create(body)
        };
        if (key is not null)
            request.Headers.Add("Idempotency-Key", key);
        return await client.SendAsync(request);
    }
}
