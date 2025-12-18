namespace OpenSourceInitiative.LicenseApi.Exceptions;

/// <summary>
///     Exception thrown when a request to the OSI API fails.
/// </summary>
public sealed class OsiApiException : OsiException
{
    public OsiApiException(string message) : base(message)
    {
    }

    public OsiApiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
