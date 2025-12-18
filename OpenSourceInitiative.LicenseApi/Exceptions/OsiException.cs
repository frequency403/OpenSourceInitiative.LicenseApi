namespace OpenSourceInitiative.LicenseApi.Exceptions;

/// <summary>
///     Base exception for all OpenSourceInitiative.LicenseApi related errors.
/// </summary>
public abstract class OsiException : Exception
{
    protected OsiException(string message) : base(message)
    {
    }

    protected OsiException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
