using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using UniversityPortal.API.Infrastructure.Persistence;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;

namespace UniversityPortal.API.Tests;

public record Account(Guid Id, string Email, string Password);

public class PortalApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@example.test";
    public const string AdminPassword = "Admin-Test-Pass1";
    public const string TestPassword = "Test-Password-1";

    private readonly string _dbName;
    private readonly string _testConnection;
    private readonly string _adminConnection;
    private readonly ConcurrentDictionary<string, Lazy<Task<Account>>> _accounts = new();
    private bool _databaseCreated;

    public FakeBusinessCentralClient Bc { get; } = new();

    public PortalApiFactory()
    {
        var baseConnection = Environment.GetEnvironmentVariable("PORTAL_TEST_DB")
            ?? throw new InvalidOperationException(
                "Set PORTAL_TEST_DB to a PostgreSQL server connection string, e.g. " +
                "'Host=localhost;Port=5433;Username=postgres;Password=your-password'.");

        _dbName = "university_portal_test_" + Guid.NewGuid().ToString("N");
        _testConnection = new NpgsqlConnectionStringBuilder(baseConnection) { Database = _dbName }.ConnectionString;
        _adminConnection = new NpgsqlConnectionStringBuilder(baseConnection) { Database = "postgres" }.ConnectionString;

        CreateTestDatabase();
        try
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>()
                .UseNpgsql(_testConnection)
                .Options;
            using var db = new PortalDbContext(options);
            db.Database.Migrate();
        }
        catch
        {
            DropTestDatabase();
            throw;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:PortalDb", _testConnection);
        builder.UseSetting("Jwt:Issuer", "test-issuer");
        builder.UseSetting("Jwt:Audience", "test-audience");
        builder.UseSetting("Jwt:SigningKey", "test-signing-key-test-signing-key-123456");
        builder.UseSetting("BusinessCentral:BaseUrl", "http://localhost:1/api/v2.0");
        builder.UseSetting("BusinessCentral:AuthMode", "Windows");
        builder.UseSetting("BusinessCentral:CompanyId", Guid.NewGuid().ToString());
        builder.UseSetting("Seed:AdminEmail", AdminEmail);
        builder.UseSetting("Seed:AdminPassword", AdminPassword);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBusinessCentralClient>();
            services.AddSingleton<IBusinessCentralClient>(Bc);
        });
    }

    public async Task<string> LoginTokenAsync(string email, string password)
    {
        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException("The login endpoint returned an empty response.");
        return body.RootElement.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("The login response did not contain an access token.");
    }

    public async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await LoginTokenAsync(email, password));
        return client;
    }

    public async Task<Account> CreateAccountAsync(string role, string? studentNumber = null)
    {
        using var admin = await LoginAsync(AdminEmail, AdminPassword);
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.test";
        using var response = await admin.PostAsJsonAsync("/api/admin/users", new
        {
            email,
            fullName = $"Test {role}",
            password = TestPassword,
            role,
            studentNumber
        });
        response.EnsureSuccessStatusCode();
        using var body = await response.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException("The user creation endpoint returned an empty response.");
        return new Account(body.RootElement.GetProperty("id").GetGuid(), email, TestPassword);
    }

    public async Task<HttpClient> ClientForRoleAsync(string role)
    {
        if (role == "Administrator")
            return await LoginAsync(AdminEmail, AdminPassword);

        var account = await _accounts.GetOrAdd(
            role,
            selectedRole => new Lazy<Task<Account>>(() => CreateAccountAsync(
                selectedRole,
                selectedRole == "Student" ? FakeBusinessCentralClient.StudentA : null))).Value;
        return await LoginAsync(account.Email, account.Password);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            DropTestDatabase();
    }

    private void CreateTestDatabase()
    {
        using var connection = new NpgsqlConnection(_adminConnection);
        connection.Open();
        using var command = new NpgsqlCommand($"CREATE DATABASE \"{_dbName}\"", connection);
        command.ExecuteNonQuery();
        _databaseCreated = true;
    }

    private void DropTestDatabase()
    {
        if (!_databaseCreated)
            return;

        using (var pooledConnection = new NpgsqlConnection(_testConnection))
            NpgsqlConnection.ClearPool(pooledConnection);

        using var connection = new NpgsqlConnection(_adminConnection);
        connection.Open();
        using var command = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_dbName}\" WITH (FORCE)", connection);
        command.ExecuteNonQuery();
        _databaseCreated = false;
    }
}
