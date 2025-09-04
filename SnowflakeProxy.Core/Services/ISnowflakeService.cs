using System.Data;

public interface ISnowflakeService
{
    Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default);
}