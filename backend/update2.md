
1. The exception types

Common/Exceptions/PortalException.cs

csharp
namespace UniversityPortal.API.Common.Exceptions;

/// 






public class PortalException : Exception
{
    public int StatusCode { get; }
    public string Title { get; }
    public string PublicDetail { get; }

    public PortalException(
        int statusCode, string title, string publicDetail,
        string? logMessage = null, Exception? inner = null)
        : base(logMessage ?? publicDetail, inner)
    {
        StatusCode = statusCode;
        Title = title;
        PublicDetail = publicDetail;
    }
}

Common/Exceptions/GlobalExceptionHandler.cs

csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace UniversityPortal.API.Common.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<GlobalExceptionHandler></globalexceptionhandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler></globalexceptionhandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool></bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        // The caller disconnected. Nothing to respond to, nothing to log as an error.
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            return true;

        int status;
        string title;
        string detail;

        if (exception is PortalException pe)
        {
            status = pe.StatusCode;
            title = pe.Title;
            detail = pe.PublicDetail;
        }
        else
        {
            status = StatusCodes.Status500InternalServerError;
            title = "Unexpected error";
            detail = "An unexpected error occurred. Quote the traceId when reporting this.";
        }

        if (status >= 500)
            _logger.LogError(exception, "Request failed with {Status}: {Title}", status, title);
        else
            _logger.LogWarning("Request failed with {Status}: {Message}", status, exception.Message);

        context.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }
}

The built-in problem details service adds a traceId to every response, which lets you match what a caller saw to a log entry.

Integrations/BusinessCentral/Exceptions/BcIntegrationException.cs (a small new folder; we’ll note it in the docs)

csharp
using System.Net;
using UniversityPortal.API.Common.Exceptions;

namespace UniversityPortal.API.Integrations.BusinessCentral.Exceptions;

public enum BcFailureKind
{
    Unreachable, Timeout, NotFound, AccessDenied, Rejected, ServerError, InvalidResponse
}

public class BcIntegrationException : PortalException
{
    public BcFailureKind Kind { get; }
    public HttpStatusCode? BcStatusCode { get; }

    public BcIntegrationException(
        BcFailureKind kind, string logMessage,
        HttpStatusCode? bcStatusCode = null, Exception? inner = null)
        : base(ToStatus(kind), ToTitle(kind), ToDetail(kind), logMessage, inner)
    {
        Kind = kind;
        BcStatusCode = bcStatusCode;
    }

    private static int ToStatus(BcFailureKind kind) => kind switch
    {
        BcFailureKind.NotFound => 404,
        BcFailureKind.Timeout => 504,
        _ => 502
    };

    private static string ToTitle(BcFailureKind kind) => kind switch
    {
        BcFailureKind.NotFound => "Record not found",
        BcFailureKind.Timeout => "Business Central timed out",
        BcFailureKind.AccessDenied => "Business Central access denied",
        BcFailureKind.Rejected => "Business Central rejected the request",
        BcFailureKind.InvalidResponse => "Unexpected Business Central response",
        _ => "Business Central unavailable"
    };

    private static string ToDetail(BcFailureKind kind) => kind switch
    {
        BcFailureKind.NotFound => "The requested record does not exist.",
        BcFailureKind.Timeout => "Business Central did not respond in time. Try again shortly.",
        _ => "The portal could not complete this request with Business Central. Try again, and contact support if it continues."
    };
}

Callers see only the generic text. The specifics (status, BC’s message) stay in the logs.

2. Logging every BC call

Integrations/BusinessCentral/Clients/BusinessCentralLoggingHandler.cs

csharp
using System.Diagnostics;

namespace UniversityPortal.API.Integrations.BusinessCentral.Clients;

public class BusinessCentralLoggingHandler : DelegatingHandler
{
    private readonly ILogger<BusinessCentralLoggingHandler></businesscentrallogginghandler> _logger;

    public BusinessCentralLoggingHandler(ILogger<BusinessCentralLoggingHandler></businesscentrallogginghandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage></httpresponsemessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, ct);
            _logger.LogInformation("BC {Method} {Path} -> {Status} in {ElapsedMs} ms",
                request.Method, request.RequestUri?.AbsolutePath,
                (int)response.StatusCode, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "BC {Method} {Path} failed after {ElapsedMs} ms",
                request.Method, request.RequestUri?.AbsolutePath, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

It logs the path only, never the query string. The query is where student numbers appear in our filters, so personal identifiers stay out of the logs.

Register it in ServiceCollectionExtensions, by adding one line before AddHttpClient and one in the chain:

csharp
services.AddTransient<BusinessCentralLoggingHandler></businesscentrallogginghandler>();

services.AddHttpClient<IBusinessCentralClient, BusinessCentralClient>(client =>
{
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
})
.AddHttpMessageHandler<BusinessCentralLoggingHandler></businesscentrallogginghandler>()
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    UseDefaultCredentials = options.AuthMode == "Windows"
});

You’ll also need using UniversityPortal.API.Integrations.BusinessCentral.Clients; (already there) and nothing else. In appsettings.Development.json, add this under Logging:LogLevel to silence the framework’s own chatty HttpClient lines, since we now produce our own:

json
"System.Net.Http.HttpClient": "Warning"
3. The client translates failures

Replace BusinessCentralClient with this version. All requests now go through one GetAsync<T></t> helper that turns every failure into a BcIntegrationException:

csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UniversityPortal.API.Integrations.BusinessCentral.Configuration;
using UniversityPortal.API.Integrations.BusinessCentral.DTOs;
using UniversityPortal.API.Integrations.BusinessCentral.Exceptions;

namespace UniversityPortal.API.Integrations.BusinessCentral.Clients;

public class BusinessCentralClient : IBusinessCentralClient
{
    // Only these statuses are posted. Draft and In Review must never reach students.
    private static readonly string[] PostedStatuses = { "Open", "Paid", "Corrective" };

    private readonly HttpClient _http;
    private readonly BusinessCentralOptions _options;

    public BusinessCentralClient(HttpClient http, IOptions<BusinessCentralOptions></businesscentraloptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    private string CompanyPath(string resource) =>
        $"companies({_options.CompanyId})/{resource}";

    public async Task<IReadOnlyList<BcCompanyDto></bccompanydto>> GetCompaniesAsync(CancellationToken ct = default)
    {
        var response = await GetAsync<BcListResponse<BcCompanyDto></bccompanydto>>("companies", ct);
        return response?.Value ?? new List<BcCompanyDto></bccompanydto>();
    }

    public async Task<IReadOnlyList<BcCustomerDto></bccustomerdto>> GetCustomersAsync(
        int top = 50, int skip = 0, CancellationToken ct = default)
    {
        var url = CompanyPath($"customers?$top={top}&$skip={skip}");
        var response = await GetAsync<BcListResponse<BcCustomerDto></bccustomerdto>>(url, ct);
        return response?.Value ?? new List<BcCustomerDto></bccustomerdto>();
    }

    public async Task<BcCustomerFinancialDetailsDto?> GetCustomerFinancialDetailsAsync(
        string customerNumber, CancellationToken ct = default)
    {
        var safe = customerNumber.Replace("'", "''");
        var filter = Uri.EscapeDataString($"number eq '{safe}'");
        var url = CompanyPath($"customerFinancialDetails?$filter={filter}");

        var response = await GetAsync<BcListResponse<BcCustomerFinancialDetailsDto></bccustomerfinancialdetailsdto>>(url, ct);
        return response?.Value.FirstOrDefault();
    }

    public async Task<IReadOnlyList<BcSalesInvoiceDto></bcsalesinvoicedto>> GetPostedSalesInvoicesAsync(
        string customerNumber, int top = 50, CancellationToken ct = default)
    {
        var safe = customerNumber.Replace("'", "''");
        var statusFilter = string.Join(" or ", PostedStatuses.Select(s => $"status eq '{s}'"));
        var filter = Uri.EscapeDataString($"customerNumber eq '{safe}' and ({statusFilter})");
        var orderBy = Uri.EscapeDataString("postingDate desc");

        var url = CompanyPath($"salesInvoices?$filter={filter}&$orderby={orderBy}&$top={top}");
        var response = await GetAsync<BcListResponse<BcSalesInvoiceDto></bcsalesinvoicedto>>(url, ct);
        return response?.Value ?? new List<BcSalesInvoiceDto></bcsalesinvoicedto>();
    }

    public async Task<IReadOnlyList<BcSalesInvoiceLineDto></bcsalesinvoicelinedto>> GetSalesInvoiceLinesAsync(
        Guid invoiceId, CancellationToken ct = default)
    {
        var url = CompanyPath($"salesInvoices({invoiceId})/salesInvoiceLines");
        var response = await GetAsync<BcListResponse<BcSalesInvoiceLineDto></bcsalesinvoicelinedto>>(url, ct);
        return response?.Value ?? new List<BcSalesInvoiceLineDto></bcsalesinvoicelinedto>();
    }

    // Every read goes through here so failures are translated in exactly one place.
    private async Task<T?> GetAsync<T></t>(string url, CancellationToken ct)
    {
        var path = url.Split('?')[0]; // never put the query (student numbers) in messages
        try
        {
            using var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var snippet = body.Length > 300 ? body[..300] : body;
                throw new BcIntegrationException(
                    MapKind(response.StatusCode),
                    $"BC returned {(int)response.StatusCode} for GET {path}. Body: {snippet}",
                    response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<T></t>(ct);
        }
        catch (HttpRequestException ex)
        {
            throw new BcIntegrationException(
                BcFailureKind.Unreachable, $"Could not reach BC for GET {path}", null, ex);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // Not the caller disconnecting, so this is the HttpClient timeout
            throw new BcIntegrationException(
                BcFailureKind.Timeout, $"BC timed out for GET {path}", null, ex);
        }
        catch (JsonException ex)
        {
            throw new BcIntegrationException(
                BcFailureKind.InvalidResponse, $"BC returned unreadable JSON for GET {path}", null, ex);
        }
    }

    private static BcFailureKind MapKind(HttpStatusCode code) => code switch
    {
        HttpStatusCode.NotFound => BcFailureKind.NotFound,
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => BcFailureKind.AccessDenied,
        >= HttpStatusCode.InternalServerError => BcFailureKind.ServerError,
        _ => BcFailureKind.Rejected
    };
}
4. Simplify the diagnostics controller

The try/catch blocks are no longer needed, because the handler produces the error response:

csharp
using Microsoft.AspNetCore.Mvc;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;

namespace UniversityPortal.API.Common.Health;

[ApiController]
[Route("api/diagnostics/business-central")]
public class BusinessCentralDiagnosticsController : ControllerBase
{
    private readonly IBusinessCentralClient _bc;

    public BusinessCentralDiagnosticsController(IBusinessCentralClient bc)
    {
        _bc = bc;
    }

    [HttpGet("companies")]
    public async Task<IActionResult></iactionresult> GetCompanies(CancellationToken ct) =>
        Ok(await _bc.GetCompaniesAsync(ct));

    [HttpGet("customers")]
    public async Task<IActionResult></iactionresult> GetCustomers([FromQuery] int top = 10, CancellationToken ct = default) =>
        Ok(await _bc.GetCustomersAsync(top, 0, ct));

    [HttpGet("customers/{number}/financial-details")]
    public async Task<IActionResult></iactionresult> GetFinancialDetails(string number, CancellationToken ct)
    {
        var details = await _bc.GetCustomerFinancialDetailsAsync(number, ct);
        return details is null ? NotFound() : Ok(details);
    }

    [HttpGet("customers/{number}/invoices")]
    public async Task<IActionResult></iactionresult> GetInvoices(string number, [FromQuery] int top = 20, CancellationToken ct = default) =>
        Ok(await _bc.GetPostedSalesInvoicesAsync(number, top, ct));

    [HttpGet("invoices/{invoiceId:guid}/lines")]
    public async Task<IActionResult></iactionresult> GetInvoiceLines(Guid invoiceId, CancellationToken ct) =>
        Ok(await _bc.GetSalesInvoiceLinesAsync(invoiceId, ct));
}
5. Wire it into Program.cs
csharp
using UniversityPortal.API.Common.Exceptions;
using UniversityPortal.API.Integrations.BusinessCentral.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler></globalexceptionhandler>();
builder.Services.AddBusinessCentralIntegration(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();   // must come first so it wraps everything below

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
