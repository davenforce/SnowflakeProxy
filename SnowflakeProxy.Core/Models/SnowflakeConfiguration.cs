namespace SnowflakeProxy.Core.Models;

public class SnowflakeConfiguration
{
    public string Account { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string PrivateKeyPassword { get; set; } = string.Empty;
    public string Warehouse { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string Role { get; set; } = "PROD_READER";
    public string Application { get; set; } = "SnowflakeProxy";
    public int ConnectionTimeout { get; set; } = 60;
    public int RetryTimeout { get; set; } = 300;
    public int MaxPoolSize { get; set; } = 10;
    public int MinPoolSize { get; set; } = 2;
    public int CommandTimeout { get; set; } = 300;
}