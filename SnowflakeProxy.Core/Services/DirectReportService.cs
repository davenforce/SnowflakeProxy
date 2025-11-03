using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SnowflakeProxy.Core.Models;

namespace SnowflakeProxy.Core.Services;

/// <summary>
/// Direct implementation of IReportService that executes queries with caching support.
/// </summary>
public class DirectReportService : IReportService
{
    private readonly ISnowflakeService _snowflakeService;
    private readonly ICacheService _cacheService;

    public DirectReportService(
        ISnowflakeService snowflakeService,
        ICacheService cacheService)
    {
        _snowflakeService = snowflakeService;
        _cacheService = cacheService;
    }

    public async Task<QueryResult> ExecuteQueryAsync(
        string query,
        Dictionary<string, object>? parameters = null,
        TimeSpan? cacheTtl = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be null or empty", nameof(query));
        }

        var cacheKey = GenerateCacheKey(query, parameters);

        // Check cache if TTL is specified
        if (cacheTtl.HasValue)
        {
            var cachedResult = await _cacheService.GetAsync<QueryResult>(cacheKey, cancellationToken);
            if (cachedResult != null)
            {
                return cachedResult with { FromCache = true };
            }
        }

        // Execute query
        var data = await _snowflakeService.ExecuteQueryAsync(
            query,
            parameters ?? new Dictionary<string, object>(),
            cancellationToken);

        var result = new QueryResult
        {
            Data = data,
            FromCache = false,
            ExecutedAt = DateTime.UtcNow
        };

        // Cache result if TTL is specified
        if (cacheTtl.HasValue)
        {
            await _cacheService.SetAsync(cacheKey, result, cacheTtl.Value, cancellationToken);
        }

        return result;
    }

    private static string GenerateCacheKey(string query, Dictionary<string, object>? parameters)
    {
        var keyData = new
        {
            Query = query,
            Parameters = parameters ?? new Dictionary<string, object>()
        };

        var json = JsonSerializer.Serialize(keyData);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return $"query:{Convert.ToBase64String(hash)}";
    }
}
