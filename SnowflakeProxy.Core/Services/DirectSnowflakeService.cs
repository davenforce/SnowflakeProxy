using System.Data;
using Microsoft.Extensions.Logging;
using Snowflake.Data.Client;
using SnowflakeProxy.Core.Models;

namespace SnowflakeProxy.Core.Services;

/// <summary>
/// Direct Snowflake connectivity implementation using Snowflake.Data client with private key authentication.
/// </summary>
public class DirectSnowflakeService : ISnowflakeService
{
    private readonly SnowflakeConfiguration _config;
    private readonly ILogger<DirectSnowflakeService> _logger;

    /// <summary>
    /// Initializes a new instance of DirectSnowflakeService.
    /// </summary>
    /// <param name="config">Snowflake connection configuration including private key authentication details.</param>
    /// <param name="logger">Logger for diagnostic and error information.</param>
    public DirectSnowflakeService(SnowflakeConfiguration config, ILogger<DirectSnowflakeService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));

        _logger.LogDebug("Executing Snowflake query. Parameters: {ParameterCount}", parameters?.Count ?? 0);

        var connectionString = BuildConnectionString();

        try
        {
            using var connection = new SnowflakeDbConnection(connectionString);

            _logger.LogDebug("Opening Snowflake connection to {Account}/{Database}.{Schema}",
                _config.Account, _config.Database, _config.Schema);

            await connection.OpenAsync(cancellationToken);

            using var command = new SnowflakeDbCommand(connection, query);
            command.CommandTimeout = _config.CommandTimeout;

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = param.Key;
                    parameter.Value = param.Value ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }

                _logger.LogDebug("Added {Count} parameters to query", parameters.Count);
            }

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var dataTable = new DataTable();
            dataTable.Load(reader);

            _logger.LogInformation("Query executed successfully. Rows returned: {RowCount}", dataTable.Rows.Count);

            return dataTable;
        }
        catch (SnowflakeDbException ex)
        {
            _logger.LogError(ex, "Snowflake query failed. ErrorCode: {ErrorCode}, QueryId: {QueryId}",
                ex.ErrorCode, ex.QueryId);
            throw new InvalidOperationException(
                $"Snowflake query failed: {ex.Message} (ErrorCode: {ex.ErrorCode}, QueryId: {ex.QueryId})", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute Snowflake query: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to execute Snowflake query: {ex.Message}", ex);
        }
    }

    private string BuildConnectionString()
    {
        _logger.LogDebug("Building Snowflake connection string");
        _logger.LogDebug("Account: {Account}, User: {User}, Warehouse: {Warehouse}, Database: {Database}, Schema: {Schema}, Role: {Role}",
            _config.Account, _config.User, _config.Warehouse, _config.Database, _config.Schema, _config.Role);

        // Check if PrivateKey is a file path or actual key content
        bool isFilePath = File.Exists(_config.PrivateKey);

        if (isFilePath)
        {
            // Use private_key_file parameter - much simpler!
            _logger.LogDebug("Using private_key_file parameter with path: {Path}", _config.PrivateKey);

            return $"account={_config.Account};" +
                   $"user={_config.User};" +
                   $"authenticator=snowflake_jwt;" +
                   $"private_key_file={_config.PrivateKey};" +
                   $"private_key_pwd={_config.PrivateKeyPassword};" +
                   $"warehouse={_config.Warehouse};" +
                   $"db={_config.Database};" +
                   $"schema={_config.Schema};" +
                   $"role={_config.Role};" +
                   $"application={_config.Application};" +
                   $"connection_timeout={_config.ConnectionTimeout};" +
                   $"retry_timeout={_config.RetryTimeout};" +
                   $"maxPoolSize={_config.MaxPoolSize};" +
                   $"minPoolSize={_config.MinPoolSize};";
        }
        else
        {
            // Use private_key parameter with content
            _logger.LogDebug("Using private_key parameter with content. Length: {Length} characters", _config.PrivateKey?.Length ?? 0);

            return $"account={_config.Account};" +
                   $"user={_config.User};" +
                   $"authenticator=snowflake_jwt;" +
                   $"private_key={_config.PrivateKey};" +
                   $"private_key_pwd={_config.PrivateKeyPassword};" +
                   $"warehouse={_config.Warehouse};" +
                   $"db={_config.Database};" +
                   $"schema={_config.Schema};" +
                   $"role={_config.Role};" +
                   $"application={_config.Application};" +
                   $"connection_timeout={_config.ConnectionTimeout};" +
                   $"retry_timeout={_config.RetryTimeout};" +
                   $"maxPoolSize={_config.MaxPoolSize};" +
                   $"minPoolSize={_config.MinPoolSize};";
        }
    }
}