using SnowflakeProxy.Core.Models;

namespace SnowflakeProxy.Core.Services;

/// <summary>
/// Service for executing Snowflake queries with caching support.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Executes a query and returns the result data.
    /// Results are cached based on the query and parameters if cacheTtl is specified.
    /// </summary>
    /// <param name="query">The SQL query to execute</param>
    /// <param name="parameters">Optional query parameters for parameterized queries</param>
    /// <param name="cacheTtl">Optional cache time-to-live. If null, result is not cached.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Query result containing data and cache metadata</returns>
    Task<QueryResult> ExecuteQueryAsync(
        string query,
        Dictionary<string, object>? parameters = null,
        TimeSpan? cacheTtl = null,
        CancellationToken cancellationToken = default);
}
