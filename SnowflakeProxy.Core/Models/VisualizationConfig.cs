namespace SnowflakeProxy.Core.Models;

/// <summary>
/// Configuration for rendering visualizations from query results.
/// Supports both simple built-in chart types and advanced custom Vega-Lite specs.
/// </summary>
public record VisualizationConfig
{
    /// <summary>
    /// Type of visualization to render.
    /// Built-in types: "table", "bar", "line", "scatter", "pie", "area", "point"
    /// For advanced use: "custom" with a Spec property containing full Vega-Lite JSON
    /// </summary>
    public string Type { get; init; } = "table";

    /// <summary>
    /// Custom Vega-Lite specification (JSON object).
    /// When provided, this takes precedence over Type.
    /// The library will automatically inject your query data into the spec's "data.values" field.
    /// Example:
    /// <code>
    /// Spec = new {
    ///     mark = "circle",
    ///     encoding = new {
    ///         x = new { field = "column1", type = "quantitative", scale = new { zero = false } },
    ///         y = new { field = "column2", type = "quantitative" },
    ///         size = new { field = "column3", type = "quantitative" },
    ///         color = new { field = "category", type = "nominal" }
    ///     }
    /// }
    /// </code>
    /// </summary>
    public object? Spec { get; init; }

    /// <summary>
    /// Chart title (used for built-in types)
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Chart width in pixels (used for built-in types)
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Chart height in pixels (used for built-in types)
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// Column name for X axis (used for built-in types)
    /// </summary>
    public string? XAxis { get; init; }

    /// <summary>
    /// Column name for Y axis (used for built-in types)
    /// </summary>
    public string? YAxis { get; init; }

    /// <summary>
    /// Column name for color encoding/grouping (used for built-in types)
    /// </summary>
    public string? ColorField { get; init; }
}