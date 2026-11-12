using System.Net;
using UniversityPortal.API.Common.Exceptions;

namespace UniversityPortal.API.Integrations.BusinessCentral.Exceptions;

public enum BcFailureKind
{
    Unreachable,
    Timeout,
    NotFound,
    AccessDenied,
    Rejected,
    ServerError,
    InvalidResponse
}

public class BcIntegrationException : PortalException
{
    public BcFailureKind Kind { get; }

    public HttpStatusCode? BcStatusCode { get; }

    public BcIntegrationException(
        BcFailureKind kind,
        string logMessage,
        HttpStatusCode? bcStatusCode = null,
        Exception? inner = null)
        : base(ToStatus(kind), ToTitle(kind), ToDetail(kind), logMessage, inner)
    {
        Kind = kind;
        BcStatusCode = bcStatusCode;
    }

    private static int ToStatus(BcFailureKind kind) => kind switch
    {
        BcFailureKind.NotFound => StatusCodes.Status404NotFound,
        BcFailureKind.Timeout => StatusCodes.Status504GatewayTimeout,
        _ => StatusCodes.Status502BadGateway
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
