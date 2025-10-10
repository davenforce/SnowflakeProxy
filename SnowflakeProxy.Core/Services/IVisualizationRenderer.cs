using System.Data;
using SnowflakeProxy.Core.Models;

namespace SnowflakeProxy.Core.Services;

public interface IVisualizationRenderer
{
    Task<string> RenderAsync(DataTable data, VisualizationConfig config, CancellationToken cancellationToken = default);
    bool SupportsType(string visualizationType);
}