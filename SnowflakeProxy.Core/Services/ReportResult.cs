using System.Data;
using SnowflakeProxy.Core.Models;

namespace SnowflakeProxy.Core.Services;

public record ReportResult
{
    public DataTable Data { get; init; } = new();
    public string RenderedOutput { get; init; } = string.Empty;
    public VisualizationConfig Visualization { get; init; } = new();
    public bool FromCache { get; init; }
}