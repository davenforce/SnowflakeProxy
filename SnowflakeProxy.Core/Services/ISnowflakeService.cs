using System.Data;

namespace SnowflakeProxy.Core.Services;

public interface ISnowflakeService
{
    Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
}