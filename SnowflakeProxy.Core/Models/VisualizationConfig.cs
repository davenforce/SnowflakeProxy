namespace SnowflakeProxy.Core.Models;

/// <summary>
/// Configuration for rendering visualizations from query results.
/// This config is passed to chart renderers in the Blazor layer.
/// </summary>
public record VisualizationConfig
{
    /// <summary>
    /// Type of visualization to render.
    /// Common types: "table", "bar", "line", "scatter", "pie", "area"
    /// Specific renderers may support additional types (e.g., "doughnut", "polarArea")
    /// </summary>
    public string Type { get; init; } = "table";

    /// <summary>
    /// Renderer-specific advanced configuration (optional).
    /// Interpretation depends on the chart renderer:
    ///
    /// - Vega-Lite renderer: Expects a Vega-Lite specification object
    /// - Blazor Bootstrap renderer: Expects ChartOptions-derived objects
    ///
    /// When provided, this overrides the simple Type property and gives full control.
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