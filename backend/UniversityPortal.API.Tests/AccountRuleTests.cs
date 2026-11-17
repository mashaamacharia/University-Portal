using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace UniversityPortal.API.Tests;

public class AccountRuleTests : IClassFixture<PortalApiFactory>
{
    private readonly PortalApiFactory _factory;

    public AccountRuleTests(PortalApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<string?> DetailAsync(HttpResponseMessage response)
    {
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException("The error response body was empty.");
        return body.RootElement.GetProperty("detail").GetString();
    }

    private Task<HttpResponseMessage> LoginRawAsync(string email, string password) =>
        _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password });

    private static object NewUser(
        string email,
        string role,
        string? studentNumber = null,
        string password = PortalApiFactory.TestPassword) =>
        new { email, fullName = "Test User", password, role, studentNumber };

    [Fact]
    public async Task Wrong_password_and_unknown_email_look_identical()
    {
        var account = await _factory.CreateAccountAsync("FinanceOfficer");
        using var wrongPassword = await LoginRawAsync(account.Email, "Wrong-Password-1");
        using var unknownEmail = await LoginRawAsync($"nobody-{Guid.NewGuid():N}@example.test", "Wrong-Password-1");

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(await DetailAsync(wrongPassword), await DetailAsync(unknownEmail));
    }

    [Fact]
    public async Task An_account_locks_after_five_wrong_passwords_even_for_the_right_one()
    {
        var account = await _factory.CreateAccountAsync("FinanceOfficer");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await LoginRawAsync(account.Email, "Wrong-Password-1");
        }

        using var correct = await LoginRawAsync(account.Email, account.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, correct.StatusCode);
    }

    [Fact]
    public async Task A_disabled_account_cannot_sign_in()
    {
        var account = await _factory.CreateAccountAsync("FinanceOfficer");
        using var admin = await _factory.LoginAsync(PortalApiFactory.AdminEmail, PortalApiFactory.AdminPassword);
        using var disable = await admin.PutAsJsonAsync(
            $"/api/admin/users/{account.Id}/active",
            new { isActive = false });

        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);
        using var attempt = await LoginRawAsync(account.Email, account.Password);
        Assert.Equal(HttpStatusCode.Unauthorized, attempt.StatusCode);
    }

    [Fact]
    public async Task An_admin_cannot_disable_their_own_account()
    {
        using var admin = await _factory.LoginAsync(PortalApiFactory.AdminEmail, PortalApiFactory.AdminPassword);
        using var me = await admin.GetAsync("/api/auth/me");
        using var meBody = await me.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException("The current-user endpoint returned an empty response.");
        var myId = meBody.RootElement.GetProperty("userId").GetGuid();

        using var response = await admin.PutAsJsonAsync(
            $"/api/admin/users/{myId}/active",
            new { isActive = false });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_student_account_for_an_unknown_customer_is_rejected()
    {
        using var admin = await _factory.LoginAsync(PortalApiFactory.AdminEmail, PortalApiFactory.AdminPassword);
        using var response = await admin.PostAsJsonAsync(
            "/api/admin/users",
            NewUser($"s-{Guid.NewGuid():N}@example.test", "Student", "NO-SUCH-CUSTOMER"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_student_number_can_be_linked_to_only_one_account()
    {
        await _factory.CreateAccountAsync("Student", FakeBusinessCentralClient.StudentB);
        using var admin = await _factory.LoginAsync(PortalApiFactory.AdminEmail, PortalApiFactory.AdminPassword);
        using var second = await admin.PostAsJsonAsync(
            "/api/admin/users",
            NewUser($"s-{Guid.NewGuid():N}@example.test", "Student", FakeBusinessCentralClient.StudentB));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task A_student_account_needs_a_student_number_and_staff_must_not_have_one()
    {
        using var admin = await _factory.LoginAsync(PortalApiFactory.AdminEmail, PortalApiFactory.AdminPassword);
        using var missing = await admin.PostAsJsonAsync(
            "/api/admin/users",
            NewUser($"s-{Guid.NewGuid():N}@example.test", "Student"));
        using var unexpected = await admin.PostAsJsonAsync(
            "/api/admin/users",
            NewUser($"o-{Guid.NewGuid():N}@example.test", "FinanceOfficer", FakeBusinessCentralClient.StudentA));

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, unexpected.StatusCode);
    }

    [Fact]
    public async Task Duplicate_emails_and_weak_passwords_are_rejected()
    {
        var existing = await _factory.CreateAccountAsync("FinanceOfficer");
        using var admin = await _factory.LoginAsync(PortalApiFactory.AdminEmail, PortalApiFactory.AdminPassword);
        using var duplicate = await admin.PostAsJsonAsync(
            "/api/admin/users",
            NewUser(existing.Email, "FinanceOfficer"));
        using var weak = await admin.PostAsJsonAsync(
            "/api/admin/users",
            NewUser($"w-{Guid.NewGuid():N}@example.test", "FinanceOfficer", password: "short"));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
    }
}
