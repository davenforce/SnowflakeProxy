using System.Data;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnowflakeProxy.Core.Models;

namespace SnowflakeProxy.Core.Services;

public class VegaLiteRenderer : IVisualizationRenderer
{
    private readonly HashSet<string> _supportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "bar", "line", "scatter", "area", "pie", "table", "point"
    };

    public bool SupportsType(string visualizationType)
    {
        return _supportedTypes.Contains(visualizationType);
    }

    public Task<string> RenderAsync(DataTable data, VisualizationConfig config, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Handle table type
        if (config.Type.Equals("table", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(RenderHtmlTable(data));
        }

        // ADVANCED: If a custom Vega-Lite spec is provided, use it directly
        if (config.Spec != null)
        {
            var customSpec = InjectDataIntoCustomSpec(data, config.Spec);
            var customHtml = GenerateVegaLiteHtml(customSpec, config);
            return Task.FromResult(customHtml);
        }

        // SIMPLE: Use built-in type shortcuts
        if (!SupportsType(config.Type))
        {
            throw new NotSupportedException(
                $"Visualization type '{config.Type}' is not supported. " +
                $"Use Type='custom' with a Spec property for advanced VegaLite features.");
        }

        var vegaLiteSpec = GenerateVegaLiteSpec(data, config);
        var renderedHtml = GenerateVegaLiteHtml(vegaLiteSpec, config);

        return Task.FromResult(renderedHtml);
    }

    private object InjectDataIntoCustomSpec(DataTable data, object customSpec)
    {
        // Convert custom spec to dictionary for manipulation
        var specJson = JsonSerializer.Serialize(customSpec);
        var specDict = JsonSerializer.Deserialize<Dictionary<string, object>>(specJson);

        if (specDict == null)
        {
            throw new InvalidOperationException("Custom Vega-Lite spec could not be deserialized");
        }

        // Inject the data from the query result
        var dataValues = ConvertDataTableToObjects(data);
        specDict["data"] = JsonSerializer.Deserialize<object>(
            JsonSerializer.Serialize(new { values = dataValues }))!;

        return specDict;
    }

    private object GenerateVegaLiteSpec(DataTable data, VisualizationConfig config)
    {
        var dataValues = ConvertDataTableToObjects(data);

        var spec = new
        {
            schema = "https://vega.github.io/schema/vega-lite/v5.json",
            title = config.Title ?? "Chart",
            width = config.Width ?? 400,
            height = config.Height ?? 300,
            data = new { values = dataValues },
            mark = GetMarkConfig(config.Type),
            encoding = GetEncodingConfig(data, config)
        };

        return spec;
    }

    private object GetMarkConfig(string chartType)
    {
        return chartType.ToLowerInvariant() switch
        {
            "bar" => new { type = "bar" },
            "line" => new { type = "line", point = true },
            "scatter" or "point" => new { type = "point" },
            "area" => new { type = "area" },
            "pie" => new { type = "arc", innerRadius = 0 },
            _ => new { type = "point" }
        };
    }

    private object GetEncodingConfig(DataTable data, VisualizationConfig config)
    {
        var columns = data.Columns.Cast<DataColumn>().ToArray();
        
        if (config.Type.Equals("pie", StringComparison.OrdinalIgnoreCase))
        {
            return new
            {
                theta = new { field = columns.Length > 1 ? columns[1].ColumnName : columns[0].ColumnName, type = "quantitative" },
                color = new { field = columns[0].ColumnName, type = "nominal" }
            };
        }

        var encoding = new Dictionary<string, object>();

        // X-axis - typically first column or specified
        var xColumn = config.XAxis ?? columns[0].ColumnName;
        var xType = GetFieldType(data.Columns[xColumn]!.DataType);
        encoding["x"] = new { field = xColumn, type = xType };

        // Y-axis - typically second column or specified
        if (columns.Length > 1)
        {
            var yColumn = config.YAxis ?? columns[1].ColumnName;
            var yType = GetFieldType(data.Columns[yColumn]!.DataType);
            encoding["y"] = new { field = yColumn, type = yType };
        }

        // Color encoding for grouping if specified
        if (!string.IsNullOrEmpty(config.ColorField) && data.Columns.Contains(config.ColorField))
        {
            encoding["color"] = new { field = config.ColorField, type = "nominal" };
        }

        return encoding;
    }

    private string GetFieldType(Type dataType)
    {
        if (dataType == typeof(DateTime) || dataType == typeof(DateOnly) || dataType == typeof(DateTimeOffset))
            return "temporal";
        
        if (dataType == typeof(int) || dataType == typeof(long) || dataType == typeof(decimal) || 
            dataType == typeof(double) || dataType == typeof(float))
            return "quantitative";
        
        return "nominal";
    }

    private List<Dictionary<string, object?>> ConvertDataTableToObjects(DataTable dataTable)
    {
        var result = new List<Dictionary<string, object?>>();
        
        foreach (DataRow row in dataTable.Rows)
        {
            var dict = new Dictionary<string, object?>();
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                var columnName = dataTable.Columns[i].ColumnName;
                var value = row[i] == DBNull.Value ? null : row[i];
                dict[columnName] = value;
            }
            result.Add(dict);
        }
        
        return result;
    }

    private string GenerateVegaLiteHtml(object vegaLiteSpec, VisualizationConfig config)
    {
        var specJson = JsonSerializer.Serialize(vegaLiteSpec, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var containerId = $"vis-{Guid.NewGuid():N}";

        return $$"""
            <div id="{{containerId}}"></div>
            <script type="text/javascript">
              (function() {
                if (typeof vegaEmbed === 'undefined') {
                  console.error('Vega-Lite not loaded. Please include vega-embed script.');
                  return;
                }

                var spec = {{specJson}};
                vegaEmbed('#{{containerId}}', spec, {
                  actions: false,
                  renderer: 'svg'
                }).catch(console.error);
              })();
            </script>
            """;
    }

    private string RenderHtmlTable(DataTable data)
    {
        var html = new System.Text.StringBuilder();
        html.AppendLine("<table class=\"table table-striped\">");
        
        // Header
        html.AppendLine("<thead><tr>");
        foreach (DataColumn column in data.Columns)
        {
            html.AppendLine($"<th>{HtmlEncoder.Default.Encode(column.ColumnName)}</th>");
        }
        html.AppendLine("</tr></thead>");
        
        // Body
        html.AppendLine("<tbody>");
        foreach (DataRow row in data.Rows)
        {
            html.AppendLine("<tr>");
            for (int i = 0; i < data.Columns.Count; i++)
            {
                var value = row[i] == DBNull.Value ? "" : row[i]?.ToString() ?? "";
                html.AppendLine($"<td>{HtmlEncoder.Default.Encode(value)}</td>");
            }
            html.AppendLine("</tr>");
        }
        html.AppendLine("</tbody></table>");
        
        return html.ToString();
    }
}