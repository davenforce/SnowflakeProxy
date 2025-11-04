namespace SnowflakeProxy.Core.Models;

/// <summary>
/// Configuration for rendering visualizations from query results.
/// Contains a renderer-specific specification that is JSON-serializable.
/// </summary>
public record VisualizationConfig
{
    /// <summary>
    /// Renderer-specific visualization specification (JSON-serializable).
    ///
    /// The structure depends on the renderer:
    /// - Vega-Lite: Vega-Lite specification object (mark, encoding, etc.)
    /// - Chart.js (both direct and Blazor Bootstrap): Chart.js config (type, data, options)
    ///
    /// This object will be serialized to JSON and can be stored in a database for dynamic report definitions.
    /// </summary>
    public object Spec { get; init; } = new { };
}