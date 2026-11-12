namespace UniversityPortal.API.Common.Exceptions;

public sealed class ForbiddenException : PortalException
{
    public ForbiddenException(string logMessage)
        : base(403, "Forbidden", "You do not have permission to perform this action.", logMessage)
    {
    }
}
