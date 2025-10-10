using System.Data;
using System.Text.Json;
using SnowflakeProxy.Core.Models;

namespace SnowflakeProxy.Core.Services;

public class DirectReportService : IReportService
{
    private readonly ISnowflakeService _snowflakeService;
    private readonly ICacheService _cacheService;
    private readonly IVisualizationRenderer _visualizationRenderer;

    public DirectReportService(
        ISnowflakeService snowflakeService,
        ICacheService cacheService,
        IVisualizationRenderer visualizationRenderer)
    {
        _snowflakeService = snowflakeService;
        _cacheService = cacheService;
        _visualizationRenderer = visualizationRenderer;
    }

    public async Task<ReportResult> GenerateReportAsync(ReportConfig config, CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(config);
        
        var cachedResult = await _cacheService.GetAsync<ReportResult>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult with { FromCache = true };
        }

        var data = await _snowflakeService.ExecuteQueryAsync(config.Query, config.Parameters, cancellationToken);
        
        var renderedOutput = await _visualizationRenderer.RenderAsync(data, config.Visualization, cancellationToken);
        
        var result = new ReportResult
        {
            Data = data,
            RenderedOutput = renderedOutput,
            Visualization = config.Visualization,
            FromCache = false
        };

        if (config.CacheTtl.HasValue)
        {
            await _cacheService.SetAsync(cacheKey, result, config.CacheTtl.Value, cancellationToken);
        }

        return result;
    }

    public async Task<ReportResult> GenerateReportAsync(string reportId, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        var config = await LoadReportConfigAsync(reportId, cancellationToken);
        
        if (parameters != null)
        {
            config = config with { Parameters = MergeParameters(config.Parameters, parameters) };
        }

        return await GenerateReportAsync(config, cancellationToken);
    }

    private async Task<ReportConfig> LoadReportConfigAsync(string reportId, CancellationToken cancellationToken)
    {
        var configPath = Path.Combine("reports", $"{reportId}.json");

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Report configuration not found: {reportId}");
        }

        var json = await File.ReadAllTextAsync(configPath, cancellationToken);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var config = JsonSerializer.Deserialize<ReportConfig>(json, options);

        if (config == null)
        {
            throw new InvalidOperationException($"Invalid report configuration: {reportId}");
        }

        // Convert JsonElement objects in parameters to their actual types
        var convertedParams = ConvertJsonElementsInDictionary(config.Parameters);
        if (convertedParams != config.Parameters)
        {
            config = config with { Parameters = convertedParams };
        }

        return config;
    }

    private Dictionary<string, object> ConvertJsonElementsInDictionary(Dictionary<string, object> parameters)
    {
        var converted = new Dictionary<string, object>();

        foreach (var kvp in parameters)
        {
            if (kvp.Value is JsonElement jsonElement)
            {
                converted[kvp.Key] = ConvertJsonElement(jsonElement);
            }
            else
            {
                converted[kvp.Key] = kvp.Value;
            }
        }

        return converted;
    }

    private object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => ConvertJsonNumber(element),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => element.ToString()!
        };
    }

    private object ConvertJsonNumber(JsonElement element)
    {
        // Try to get as integer type (prefer long for consistency)
        if (element.TryGetInt64(out var longValue))
        {
            return longValue;
        }

        // If it's a decimal/floating point number
        if (element.TryGetDouble(out var doubleValue))
        {
            return doubleValue;
        }

        // Fallback
        return element.GetRawText();
    }

    private string GenerateCacheKey(ReportConfig config)
    {
        var keyData = new
        {
            Query = config.Query,
            Parameters = config.Parameters,
            Visualization = config.Visualization
        };
        
        var json = JsonSerializer.Serialize(keyData);
        return $"report:{json.GetHashCode():X}";
    }

    private Dictionary<string, object> MergeParameters(Dictionary<string, object>? configParams, Dictionary<string, object> runtimeParams)
    {
        var merged = new Dictionary<string, object>(configParams ?? new Dictionary<string, object>());
        
        foreach (var param in runtimeParams)
        {
            merged[param.Key] = param.Value;
        }
        
        return merged;
    }
}