using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace UniversityPortal.API.Tests;

public class AccessRuleTests : IClassFixture<PortalApiFactory>
{
    private readonly PortalApiFactory _factory;

    public AccessRuleTests(PortalApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/student/finance/statement")]
    [InlineData("/api/officer/finance/invoices")]
    [InlineData("/api/officer/finance/students?search=Student")]
    [InlineData("/api/admin/users")]
    [InlineData("/api/auth/me")]
    public async Task Protected_endpoints_reject_anonymous_callers(string path)
    {
        using var response = await _factory.CreateClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Health_is_public()
    {
        using var response = await _factory.CreateClient().GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("Student", "/api/student/finance/statement", 200)]
    [InlineData("Student", "/api/officer/finance/invoices", 403)]
    [InlineData("Student", "/api/officer/finance/students?search=Student", 403)]
    [InlineData("Student", "/api/admin/users", 403)]
    [InlineData("FinanceOfficer", "/api/officer/finance/invoices", 200)]
    [InlineData("FinanceOfficer", "/api/officer/finance/students?search=Student", 200)]
    [InlineData("FinanceOfficer", "/api/student/finance/statement", 403)]
    [InlineData("FinanceOfficer", "/api/admin/users", 403)]
    [InlineData("Administrator", "/api/admin/users", 200)]
    [InlineData("Administrator", "/api/student/finance/statement", 403)]
    [InlineData("Administrator", "/api/officer/finance/invoices", 403)]
    public async Task Each_role_reaches_only_its_own_endpoints(string role, string path, int expected)
    {
        using var client = await _factory.ClientForRoleAsync(role);
        using var response = await client.GetAsync(path);
        Assert.Equal(expected, (int)response.StatusCode);
    }

    [Theory]
    [InlineData("/api/officer/finance/students?search=a")]
    [InlineData("/api/officer/finance/students?search=%20%20")]
    public async Task Student_lookup_rejects_search_terms_shorter_than_two_characters(string path)
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        using var response = await officer.GetAsync(path);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_student_can_open_their_own_invoice_but_not_another_students()
    {
        using var student = await _factory.ClientForRoleAsync("Student");

        using var own = await student.GetAsync(
            $"/api/student/finance/invoices/{FakeBusinessCentralClient.InvoiceOfA}");
        using var other = await student.GetAsync(
            $"/api/student/finance/invoices/{FakeBusinessCentralClient.InvoiceOfB}");
        using var draft = await student.GetAsync("/api/student/finance/invoices/INV-A-DRAFT");

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, draft.StatusCode);
    }

    [Fact]
    public async Task A_students_requests_only_ever_ask_BC_about_their_own_number()
    {
        using var student = await _factory.ClientForRoleAsync("Student");
        _factory.Bc.Reset();

        using var statement = await student.GetAsync("/api/student/finance/statement");
        using var invoices = await student.GetAsync(
            $"/api/student/finance/invoices?customerNumber={FakeBusinessCentralClient.StudentB}");
        using var payments = await student.GetAsync("/api/student/finance/payments");

        Assert.Equal(HttpStatusCode.OK, statement.StatusCode);
        Assert.Equal(HttpStatusCode.OK, invoices.StatusCode);
        Assert.Equal(HttpStatusCode.OK, payments.StatusCode);

        Assert.NotEmpty(_factory.Bc.CustomerNumbersQueried);
        Assert.All(
            _factory.Bc.CustomerNumbersQueried,
            number => Assert.Equal(FakeBusinessCentralClient.StudentA, number));

        using var list = await invoices.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException("The invoice endpoint returned an empty response.");
        Assert.DoesNotContain(
            list.RootElement.EnumerateArray(),
            invoice => invoice.GetProperty("invoiceNumber").GetString() == FakeBusinessCentralClient.InvoiceOfB);
    }

    [Fact]
    public async Task A_tampered_token_is_rejected()
    {
        var account = await _factory.CreateAccountAsync("FinanceOfficer");
        var token = await _factory.LoginTokenAsync(account.Email, account.Password);
        var tampered = token[..^3] + (token.EndsWith("AAA", StringComparison.Ordinal) ? "BBB" : "AAA");

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);
        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
