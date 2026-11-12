namespace UniversityPortal.API.Common.Exceptions;

public class PortalException : Exception
{
    public int StatusCode { get; }

    public string Title { get; }

    public string PublicDetail { get; }

    public PortalException(
        int statusCode,
        string title,
        string publicDetail,
        string? logMessage = null,
        Exception? inner = null)
        : base(logMessage ?? publicDetail, inner)
    {
        StatusCode = statusCode;
        Title = title;
        PublicDetail = publicDetail;
    }
}
