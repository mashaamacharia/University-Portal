using UniversityPortal.API.Common.Exceptions;

namespace UniversityPortal.API.Integrations.BusinessCentral.Exceptions;

public class BcRejectedException : PortalException
{
    public string? BcCode { get; }

    public string? CorrelationId { get; }

    public BcRejectedException(string officerMessage, string? bcCode, string? correlationId)
        : base(
            422,
            "Business Central rejected the request",
            officerMessage,
            $"BC rejected a write: [{bcCode}] {officerMessage} (BC correlation {correlationId})")
    {
        BcCode = bcCode;
        CorrelationId = correlationId;
    }
}
