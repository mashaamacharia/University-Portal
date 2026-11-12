using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace UniversityPortal.API.Common.Exceptions;

public class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        var (status, title, detail) = exception is PortalException portalException
            ? (portalException.StatusCode, portalException.Title, portalException.PublicDetail)
            : (
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                "An unexpected error occurred. Quote the traceId when reporting this.");

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Request failed with {Status}: {Title}", status, title);
        }
        else
        {
            logger.LogWarning(
                "Request failed with {Status}: {Message}",
                status,
                exception.Message);
        }

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail
            }
        });
    }
}
