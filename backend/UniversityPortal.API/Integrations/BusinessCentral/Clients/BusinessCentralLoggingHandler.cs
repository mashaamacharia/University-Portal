using System.Diagnostics;

namespace UniversityPortal.API.Integrations.BusinessCentral.Clients;

public class BusinessCentralLoggingHandler(
    ILogger<BusinessCentralLoggingHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = request.RequestUri?.AbsolutePath;

        try
        {
            var response = await base.SendAsync(request, ct);
            logger.LogInformation(
                "BC {Method} {Path} -> {Status} in {ElapsedMs} ms",
                request.Method,
                path,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning(
                "BC {Method} {Path} cancelled or timed out after {ElapsedMs} ms",
                request.Method,
                path,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "BC {Method} {Path} failed after {ElapsedMs} ms",
                request.Method,
                path,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
