namespace OpenSourceInitiative.LicenseApi.Exceptions;

/// <summary>
///     Exception thrown when the client is not properly initialized.
/// </summary>
public sealed class OsiInitializationException(string message, Exception innerException)
    : OsiException(message, innerException);

/// <summary>
///     Base exception for all OpenSourceInitiative.LicenseApi related errors.
/// </summary>
public abstract class OsiException(string message, Exception innerException)
    : Exception(message, innerException);