namespace UniversityPortal.API.Common.Exceptions;

public sealed class NotAuthenticatedException : PortalException
{
    public NotAuthenticatedException()
        : base(401, "Authentication required", "You must be signed in to use this endpoint.")
    {
    }
}
