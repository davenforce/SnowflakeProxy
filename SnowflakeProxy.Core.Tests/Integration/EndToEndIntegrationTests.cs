using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using SnowflakeProxy.Core.Services;

namespace SnowflakeProxy.Core.Tests.Integration;

/// <summary>
/// End-to-end integration tests that verify the entire query execution and caching pipeline
/// without requiring actual Snowflake credentials.
/// </summary>
public class EndToEndIntegrationTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ICacheService _cacheService;
    private readonly ISnowflakeService _snowflakeService;
    private readonly IReportService _reportService;

    public EndToEndIntegrationTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cacheService = new MemoryCacheService(_memoryCache);
        _snowflakeService = new MockSnowflakeService(NullLogger<MockSnowflakeService>.Instance);
        _reportService = new DirectReportService(_snowflakeService, _cacheService);
    }

    [Fact]
    public async Task ExecuteQuery_SalesData_ShouldReturnDataSuccessfully()
    {
        // Arrange
        var query = "SELECT region, revenue FROM sales";

        // Act
        var result = await _reportService.ExecuteQueryAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data.Rows.Count.Should().BeGreaterThan(0);
        result.FromCache.Should().BeFalse();
        result.ExecutedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExecuteQuery_TimeSeriesData_ShouldReturnDateColumn()
    {
        // Arrange
        var query = "SELECT date, value FROM metrics WHERE date > '2024-01-01'";

        // Act
        var result = await _reportService.ExecuteQueryAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Data.Columns.Cast<System.Data.DataColumn>()
            .Should().Contain(c => c.ColumnName.Equals("Date", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteQuery_WithCaching_SecondCallShouldReturnFromCache()
    {
        // Arrange
        var query = "SELECT * FROM products";
        var cacheTtl = TimeSpan.FromMinutes(5);

        // Act
        var firstResult = await _reportService.ExecuteQueryAsync(query, null, cacheTtl);
        var secondResult = await _reportService.ExecuteQueryAsync(query, null, cacheTtl);

        // Assert
        firstResult.FromCache.Should().BeFalse();
        secondResult.FromCache.Should().BeTrue();
        secondResult.Data.Rows.Count.Should().Be(firstResult.Data.Rows.Count);
        secondResult.ExecutedAt.Should().Be(firstResult.ExecutedAt);
    }

    [Fact]
    public async Task ExecuteQuery_WithParameters_ShouldPassParametersToQuery()
    {
        // Arrange
        var query = "SELECT * FROM sales WHERE year = @year LIMIT @limit";
        var parameters = new Dictionary<string, object>
        {
            { "year", 2024 },
            { "limit", 10 }
        };

        // Act
        var result = await _reportService.ExecuteQueryAsync(query, parameters);

        // Assert
        result.Should().NotBeNull();
        result.Data.Rows.Count.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task ExecuteQuery_UsersTable_ShouldReturnUserData()
    {
        // Arrange
        var query = "SELECT * FROM users";

        // Act
        var result = await _reportService.ExecuteQueryAsync(query);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Rows.Count.Should().BeGreaterThan(0);
        result.Data.Columns.Cast<System.Data.DataColumn>()
            .Should().Contain(c => c.ColumnName.Contains("User"));
    }

    [Fact]
    public async Task ExecuteQuery_ProductTable_ShouldReturnProductData()
    {
        // Arrange
        var query = "SELECT * FROM products";

        // Act
        var result = await _reportService.ExecuteQueryAsync(query);

        // Assert
        result.Data.Should().NotBeNull();
        result.Data.Rows.Count.Should().BeGreaterThan(0);
        result.Data.Columns.Cast<System.Data.DataColumn>()
            .Should().Contain(c => c.ColumnName.Contains("Product"));
    }

    [Fact]
    public async Task ExecuteQuery_MultipleDifferentQueries_ShouldAllSucceed()
    {
        // Arrange & Act
        var salesResult = await _reportService.ExecuteQueryAsync("SELECT * FROM sales");
        var userResult = await _reportService.ExecuteQueryAsync("SELECT * FROM users");
        var productResult = await _reportService.ExecuteQueryAsync("SELECT * FROM products");

        // Assert
        salesResult.Should().NotBeNull();
        salesResult.Data.Columns.Cast<System.Data.DataColumn>()
            .Should().Contain(c => c.ColumnName == "Revenue" || c.ColumnName == "Region");

        userResult.Should().NotBeNull();
        userResult.Data.Columns.Cast<System.Data.DataColumn>()
            .Should().Contain(c => c.ColumnName.Contains("User"));

        productResult.Should().NotBeNull();
        productResult.Data.Columns.Cast<System.Data.DataColumn>()
            .Should().Contain(c => c.ColumnName.Contains("Product"));
    }

    [Fact]
    public async Task ExecuteQuery_InvalidQuery_ShouldThrowException()
    {
        // Arrange
        var query = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _reportService.ExecuteQueryAsync(query));
    }

    [Fact]
    public async Task CacheService_ExpiredCache_ShouldNotReturnStaleData()
    {
        // Arrange
        var query = "SELECT * FROM sales";
        var cacheTtl = TimeSpan.FromMilliseconds(100);

        // Act
        var firstResult = await _reportService.ExecuteQueryAsync(query, null, cacheTtl);
        await Task.Delay(150); // Wait for cache to expire
        var secondResult = await _reportService.ExecuteQueryAsync(query, null, cacheTtl);

        // Assert
        firstResult.FromCache.Should().BeFalse();
        secondResult.FromCache.Should().BeFalse();
        secondResult.ExecutedAt.Should().BeAfter(firstResult.ExecutedAt);
    }

    [Fact]
    public async Task ExecuteQuery_WithNoCacheTtl_ShouldNotUseCache()
    {
        // Arrange
        var query = "SELECT * FROM sales";

        // Act
        var firstResult = await _reportService.ExecuteQueryAsync(query, null, null);
        var secondResult = await _reportService.ExecuteQueryAsync(query, null, null);

        // Assert
        firstResult.FromCache.Should().BeFalse();
        secondResult.FromCache.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteQuery_DifferentQueries_ShouldHaveDifferentCacheKeys()
    {
        // Arrange
        var query1 = "SELECT * FROM sales";
        var query2 = "SELECT * FROM products";
        var cacheTtl = TimeSpan.FromMinutes(5);

        // Act
        var result1 = await _reportService.ExecuteQueryAsync(query1, null, cacheTtl);
        var result2 = await _reportService.ExecuteQueryAsync(query2, null, cacheTtl);
        var result1Again = await _reportService.ExecuteQueryAsync(query1, null, cacheTtl);

        // Assert
        result1.FromCache.Should().BeFalse();
        result2.FromCache.Should().BeFalse();
        result1Again.FromCache.Should().BeTrue(); // Should get cached result from first query
    }

    [Fact]
    public async Task ExecuteQuery_SalesWithRegionFilter_ShouldReturnFilteredData()
    {
        // Arrange
        var query = "SELECT * FROM sales WHERE region = 'North'";

        // Act
        var result = await _reportService.ExecuteQueryAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Data.Rows.Count.Should().BeGreaterThan(0);
        result.Data.Columns.Cast<System.Data.DataColumn>()
            .Should().Contain(c => c.ColumnName == "Region");
    }

    [Fact]
    public async Task ExecuteQuery_TimeSeriesWithDateRange_ShouldReturnRangeData()
    {
        // Arrange
        var query = "SELECT * FROM metrics WHERE date BETWEEN '2024-01-01' AND '2024-12-31'";

        // Act
        var result = await _reportService.ExecuteQueryAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Data.Rows.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteQuery_AggregateQuery_ShouldReturnAggregatedData()
    {
        // Arrange
        var query = "SELECT region, SUM(revenue) as total FROM sales GROUP BY region";

        // Act
        var result = await _reportService.ExecuteQueryAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Data.Rows.Count.Should().BeGreaterThan(0);
        result.Data.Columns.Cast<System.Data.DataColumn>()
            .Should().Contain(c => c.ColumnName == "Region");
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
    }
}
