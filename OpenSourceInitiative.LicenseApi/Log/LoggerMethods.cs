using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace OpenSourceInitiative.LicenseApi.Log;

[ExcludeFromCodeCoverage]
internal static partial class LoggerMethods
{
    [LoggerMessage(LogLevel.Debug, "Acquiring initialization lock")]
    internal static partial void LogAcquiringInitializationLock(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Already initialized, skipping")]
    internal static partial void LogAlreadyInitializedSkipping(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Initializing OsiLicensesClient")]
    internal static partial void LogInitializingOsilicensesclient(ILogger logger);

    [LoggerMessage(LogLevel.Information, "OsiLicensesClient initialization completed successfully")]
    internal static partial void LogOsilicensesclientInitializationCompletedSuccessfully(ILogger logger);

    [LoggerMessage(LogLevel.Error, "Failed to initialize OsiLicensesClient")]
    internal static partial void LogFailedToInitializeOsilicensesclient(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Starting synchronous initialization")]
    internal static partial void LogStartingSynchronousInitialization(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Returning cached snapshot of {count} licenses")]
    internal static partial void LogReturningCachedSnapshotOfCountLicenses(ILogger logger, int count);

    [LoggerMessage(LogLevel.Information, "Fetching all licenses from OSI API")]
    internal static partial void LogFetchingAllLicensesFromOsiApi(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Starting streaming deserialization from {path}")]
    internal static partial void LogStartingStreamingDeserializationFromPath(ILogger logger, string path);

    [LoggerMessage(LogLevel.Debug, "Streaming deserialization completed, loaded {count} licenses")]
    internal static partial void LogStreamingDeserializationCompletedLoadedCountLicenses(ILogger logger, int count);

    [LoggerMessage(LogLevel.Warning, "Streaming deserialization failed, falling back to array deserialization")]
    internal static partial void LogStreamingDeserializationFailedFallingBackToArrayDeserialization(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Attempting fallback array deserialization")]
    internal static partial void LogAttemptingFallbackArrayDeserialization(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Fallback deserialization successful, processing {count} licenses")]
    internal static partial void LogFallbackDeserializationSuccessfulProcessingCountLicenses(ILogger logger, int count);

    [LoggerMessage(LogLevel.Error, "Fallback deserialization failed")]
    internal static partial void LogFallbackDeserializationFailed(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Fetching license texts with max parallelism of {maxParallelism}")]
    internal static partial void LogFetchingLicenseTextsWithMaxParallelismOfMaxparallelism(ILogger logger,
        int maxParallelism);

    [LoggerMessage(LogLevel.Warning, "Failed to fetch license text for {licenseName} (SPDX: {spdxId})")]
    internal static partial void LogFailedToFetchLicenseTextForLicensenameSpdxSpdxid(ILogger logger, string licenseName,
        string? spdxId);

    [LoggerMessage(LogLevel.Debug, "Initiated {count} license text fetch operations")]
    internal static partial void LogInitiatedCountLicenseTextFetchOperations(ILogger logger, int count);

    [LoggerMessage(LogLevel.Debug, "All license text fetch operations completed")]
    internal static partial void LogAllLicenseTextFetchOperationsCompleted(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Some license text fetch operations failed")]
    internal static partial void LogSomeLicenseTextFetchOperationsFailed(ILogger logger);

    [LoggerMessage(LogLevel.Information, "Successfully loaded {count} licenses in {elapsedMs} ms")]
    internal static partial void LogSuccessfullyLoadedCountLicensesInElapsedmsMs(ILogger logger, int count,
        long elapsedMs);

    [LoggerMessage(LogLevel.Debug, "Search called with empty query, returning empty result")]
    internal static partial void LogSearchCalledWithEmptyQueryReturningEmptyResult(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Searching for licenses matching query: '{query}'")]
    internal static partial void LogSearchingForLicensesMatchingQueryQuery(ILogger logger, string query);

    [LoggerMessage(LogLevel.Information, "Search for '{query}' returned {count} result(s)")]
    internal static partial void LogSearchForQueryReturnedCountResultS(ILogger logger, string query, int count);

    [LoggerMessage(LogLevel.Debug, "GetBySpdx called with empty SPDX ID")]
    internal static partial void LogGetbyspdxCalledWithEmptySpdxId(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Looking up license by SPDX ID: '{spdxId}'")]
    internal static partial void LogLookingUpLicenseBySpdxIdSpdxid(ILogger logger, string spdxId);

    [LoggerMessage(LogLevel.Debug, "Found license '{licenseName}' via dictionary lookup")]
    internal static partial void LogFoundLicenseLicensenameViaDictionaryLookup(ILogger logger, string licenseName);

    [LoggerMessage(LogLevel.Debug, "Found license '{licenseName}' via fallback scan")]
    internal static partial void LogFoundLicenseLicensenameViaFallbackScan(ILogger logger, string licenseName);

    [LoggerMessage(LogLevel.Information, "License with SPDX ID '{spdxId}' not found")]
    internal static partial void LogLicenseWithSpdxIdSpdxidNotFound(ILogger logger, string spdxId);

    [LoggerMessage(LogLevel.Debug, "FetchFiltered called with empty value for parameter '{paramName}'")]
    internal static partial void LogFetchfilteredCalledWithEmptyValueForParameterParamname(ILogger logger,
        string paramName);

    [LoggerMessage(LogLevel.Debug, "Fetching licenses filtered by {paramName}='{paramValue}'")]
    internal static partial void LogFetchingLicensesFilteredByParamnameParamvalue(ILogger logger, string paramName,
        string paramValue);

    [LoggerMessage(LogLevel.Debug, "Request URL: {requestUrl}")]
    internal static partial void LogRequestUrlRequesturl(ILogger logger, string requestUrl);

    [LoggerMessage(LogLevel.Debug, "Deserialized {count} filtered licenses")]
    internal static partial void LogDeserializedCountFilteredLicenses(ILogger logger, int count);

    [LoggerMessage(LogLevel.Error, "Failed to fetch filtered licenses by {paramName}='{paramValue}'")]
    internal static partial void LogFailedToFetchFilteredLicensesByParamnameParamvalue(ILogger logger, string paramName,
        string paramValue);

    [LoggerMessage(LogLevel.Information, "No licenses found for {paramName}='{paramValue}'")]
    internal static partial void LogNoLicensesFoundForParamnameParamvalue(ILogger logger, string paramName,
        string paramValue);

    [LoggerMessage(LogLevel.Debug, "Enriching {count} filtered licenses with text (max parallelism: {maxParallelism})")]
    internal static partial void LogEnrichingCountFilteredLicensesWithTextMaxParallelismMaxparallelism(ILogger logger,
        int count, int maxParallelism);

    [LoggerMessage(LogLevel.Debug, "Initiated {count} license text fetch operations for filtered results")]
    internal static partial void LogInitiatedCountLicenseTextFetchOperationsForFilteredResults(ILogger logger,
        int count);

    [LoggerMessage(LogLevel.Debug, "All license text fetch operations completed for filtered results")]
    internal static partial void LogAllLicenseTextFetchOperationsCompletedForFilteredResults(ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Some license text fetch operations failed for filtered results")]
    internal static partial void LogSomeLicenseTextFetchOperationsFailedForFilteredResults(ILogger logger);

    [LoggerMessage(LogLevel.Information,
        "Fetched and enriched {count} licenses filtered by {paramName}='{paramValue}' in {elapsedMs} ms")]
    internal static partial void LogFetchedAndEnrichedCountLicensesFilteredByParamnameParamvalueInElapsedmsMs(
        ILogger logger, int count, string paramName, string paramValue, long elapsedMs);

    [LoggerMessage(LogLevel.Debug, "Disposing OsiLicensesClient")]
    internal static partial void LogDisposingOsilicensesclient(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Disposed owned HttpClient")]
    internal static partial void LogDisposedOwnedHttpclient(ILogger logger);

    [LoggerMessage(LogLevel.Debug, "Disposing OsiLicensesClient asynchronously")]
    internal static partial void LogDisposingOsilicensesclientAsynchronously(ILogger logger);
}