namespace OpenSourceInitiative.LicenseApi.Exceptions;

/// <summary>
///     Exception thrown when the client is not properly initialized.
/// </summary>
public sealed class OsiInitializationException : OsiException
{
    public OsiInitializationException(string message) : base(message)
    {
    }

    public OsiInitializationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
