public record ReportConfig
{
    public string ReportId { get; init; } = string.Empty;
    public string Query { get; init; } = string.Empty;
    public Dictionary<string, object> Parameters { get; init; } = new();
    public VisualizationConfig Visualization { get; init; } = new();
    public TimeSpan? CacheDuration { get; init; }
}
