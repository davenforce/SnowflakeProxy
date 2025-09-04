public record VisualizationConfig
{
    public string Type { get; init; } = "vega-lite"; // "vega-lite", "python", "html"
    public object Spec { get; init; } = new { };
}