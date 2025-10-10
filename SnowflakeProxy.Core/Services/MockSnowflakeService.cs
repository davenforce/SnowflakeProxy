using System.Data;
using Microsoft.Extensions.Logging;

namespace SnowflakeProxy.Core.Services;

/// <summary>
/// Mock Snowflake service for testing and development without requiring actual Snowflake credentials.
/// Generates realistic sample data based on query patterns.
/// </summary>
public class MockSnowflakeService : ISnowflakeService
{
    private readonly ILogger<MockSnowflakeService> _logger;
    private readonly Random _random = new();

    /// <summary>
    /// Initializes a new instance of MockSnowflakeService.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    public MockSnowflakeService(ILogger<MockSnowflakeService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Query cannot be null or empty.", nameof(query));

        _logger.LogInformation("MockSnowflakeService executing query. Parameters: {Count}", parameters?.Count ?? 0);

        // Simulate query execution delay
        Task.Delay(100, cancellationToken).Wait(cancellationToken);

        var dataTable = GenerateMockData(query, parameters);

        _logger.LogInformation("Mock query executed. Returned {RowCount} rows", dataTable.Rows.Count);

        return Task.FromResult(dataTable);
    }

    private DataTable GenerateMockData(string query, Dictionary<string, object>? parameters)
    {
        var queryLower = query.ToLowerInvariant();

        // Detect query patterns and generate appropriate data
        if (queryLower.Contains("sales") || queryLower.Contains("revenue"))
        {
            return GenerateSalesData(parameters);
        }

        if (queryLower.Contains("user") || queryLower.Contains("customer"))
        {
            return GenerateUserData(parameters);
        }

        if (queryLower.Contains("product"))
        {
            return GenerateProductData(parameters);
        }

        if (queryLower.Contains("time") || queryLower.Contains("date"))
        {
            return GenerateTimeSeriesData(parameters);
        }

        // Default: simple two-column data
        return GenerateGenericData(parameters);
    }

    private DataTable GenerateSalesData(Dictionary<string, object>? parameters)
    {
        var table = new DataTable();
        table.Columns.Add("Region", typeof(string));
        table.Columns.Add("Product", typeof(string));
        table.Columns.Add("Revenue", typeof(decimal));
        table.Columns.Add("Units", typeof(int));
        table.Columns.Add("Date", typeof(DateTime));

        var regions = new[] { "North", "South", "East", "West", "Central" };
        var products = new[] { "Widget A", "Widget B", "Gadget X", "Gadget Y", "Tool Z" };

        var rowCount = parameters?.ContainsKey("limit") == true
            ? Convert.ToInt32(parameters["limit"])
            : 20;

        for (int i = 0; i < rowCount; i++)
        {
            table.Rows.Add(
                regions[_random.Next(regions.Length)],
                products[_random.Next(products.Length)],
                _random.Next(1000, 50000) + (_random.NextDouble() * 100),
                _random.Next(10, 500),
                DateTime.Now.AddDays(-_random.Next(0, 365))
            );
        }

        return table;
    }

    private DataTable GenerateUserData(Dictionary<string, object>? parameters)
    {
        var table = new DataTable();
        table.Columns.Add("UserId", typeof(int));
        table.Columns.Add("UserName", typeof(string));
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("SignupDate", typeof(DateTime));
        table.Columns.Add("IsActive", typeof(bool));

        var firstNames = new[] { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Henry" };
        var lastNames = new[] { "Smith", "Johnson", "Williams", "Jones", "Brown", "Davis", "Miller", "Wilson" };

        var rowCount = parameters?.ContainsKey("limit") == true
            ? Convert.ToInt32(parameters["limit"])
            : 15;

        for (int i = 0; i < rowCount; i++)
        {
            var firstName = firstNames[_random.Next(firstNames.Length)];
            var lastName = lastNames[_random.Next(lastNames.Length)];
            table.Rows.Add(
                i + 1,
                $"{firstName} {lastName}",
                $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
                DateTime.Now.AddDays(-_random.Next(0, 730)),
                _random.Next(0, 10) > 2 // 80% active
            );
        }

        return table;
    }

    private DataTable GenerateProductData(Dictionary<string, object>? parameters)
    {
        var table = new DataTable();
        table.Columns.Add("ProductId", typeof(int));
        table.Columns.Add("ProductName", typeof(string));
        table.Columns.Add("Category", typeof(string));
        table.Columns.Add("Price", typeof(decimal));
        table.Columns.Add("InStock", typeof(int));

        var categories = new[] { "Electronics", "Clothing", "Home & Garden", "Sports", "Books" };
        var products = new[] { "Premium", "Standard", "Basic", "Deluxe", "Pro" };

        var rowCount = parameters?.ContainsKey("limit") == true
            ? Convert.ToInt32(parameters["limit"])
            : 12;

        for (int i = 0; i < rowCount; i++)
        {
            var category = categories[_random.Next(categories.Length)];
            table.Rows.Add(
                i + 100,
                $"{products[_random.Next(products.Length)]} {category.TrimEnd('s')}",
                category,
                _random.Next(10, 1000) + (_random.NextDouble() * 100),
                _random.Next(0, 500)
            );
        }

        return table;
    }

    private DataTable GenerateTimeSeriesData(Dictionary<string, object>? parameters)
    {
        var table = new DataTable();
        table.Columns.Add("Date", typeof(DateTime));
        table.Columns.Add("Value", typeof(double));
        table.Columns.Add("Metric", typeof(string));

        var metrics = new[] { "PageViews", "Sessions", "Users", "Conversions" };

        var rowCount = parameters?.ContainsKey("limit") == true
            ? Convert.ToInt32(parameters["limit"])
            : 30;

        var baseDate = DateTime.Now.AddDays(-rowCount);

        for (int i = 0; i < rowCount; i++)
        {
            table.Rows.Add(
                baseDate.AddDays(i),
                _random.Next(100, 10000) + (_random.NextDouble() * 1000),
                metrics[_random.Next(metrics.Length)]
            );
        }

        return table;
    }

    private DataTable GenerateGenericData(Dictionary<string, object>? parameters)
    {
        var table = new DataTable();
        table.Columns.Add("Category", typeof(string));
        table.Columns.Add("Value", typeof(int));

        var categories = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon" };

        foreach (var category in categories)
        {
            table.Rows.Add(category, _random.Next(50, 500));
        }

        return table;
    }
}
