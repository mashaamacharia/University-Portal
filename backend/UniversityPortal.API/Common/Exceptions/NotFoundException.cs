namespace UniversityPortal.API.Common.Exceptions;

public sealed class NotFoundException : PortalException
{
    public NotFoundException(string logMessage)
        : base(404, "Record not found", "The requested record does not exist.", logMessage)
    {
    }
}
