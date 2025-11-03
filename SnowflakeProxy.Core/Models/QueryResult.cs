using System.Data;

namespace SnowflakeProxy.Core.Models;

/// <summary>
/// Result of executing a query, containing the data and metadata.
/// </summary>
public record QueryResult
{
    /// <summary>
    /// The query result data.
    /// </summary>
    public DataTable Data { get; init; } = new();

    /// <summary>
    /// Indicates whether this result was retrieved from cache.
    /// </summary>
    public bool FromCache { get; init; }

    /// <summary>
    /// Timestamp when the query was executed (or when cached result was created).
    /// </summary>
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
}
