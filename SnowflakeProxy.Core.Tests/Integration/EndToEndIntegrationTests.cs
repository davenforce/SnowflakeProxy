using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SnowflakeProxy.Core.Models;
using SnowflakeProxy.Core.Services;

namespace SnowflakeProxy.Core.Tests.Integration;

/// <summary>
/// End-to-end integration tests that verify the entire report generation pipeline
/// without requiring actual Snowflake credentials.
/// </summary>
public class EndToEndIntegrationTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ICacheService _cacheService;
    private readonly ISnowflakeService _snowflakeService;
    private readonly IVisualizationRenderer _renderer;
    private readonly IReportService _reportService;

    public EndToEndIntegrationTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cacheService = new MemoryCacheService(_memoryCache);
        _snowflakeService = new MockSnowflakeService(NullLogger<MockSnowflakeService>.Instance);
        _renderer = new VegaLiteRenderer();
        _reportService = new DirectReportService(_snowflakeService, _cacheService, _renderer);
    }

    [Fact]
    public async Task GenerateReport_SalesData_WithBarChart_ShouldRenderSuccessfully()
    {
        // Arrange
        var config = new ReportConfig
        {
            ReportId = "sales-by-region",
            Query = "SELECT region, revenue FROM sales",
            Visualization = new VisualizationConfig
            {
                Type = "bar",
                Title = "Sales by Region",
                Width = 600,
                Height = 400
            }
        };

        // Act
        var result = await _reportService.GenerateReportAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data.Rows.Count.Should().BeGreaterThan(0);
        result.RenderedOutput.Should().Contain("vegaEmbed");
        result.RenderedOutput.Should().Contain("Sales by Region");
        result.FromCache.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateReport_TimeSeriesData_WithLineChart_ShouldRenderSuccessfully()
    {
        // Arrange
        var config = new ReportConfig
        {
            ReportId = "daily-metrics",
            Query = "SELECT date, value FROM metrics WHERE date > '2024-01-01'",
            Visualization = new VisualizationConfig
            {
                Type = "line",
                Title = "Daily Metrics Trend",
                Width = 800,
                Height = 300
            }
        };

        // Act
        var result = await _reportService.GenerateReportAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Data.Columns.Cast<System.Data.DataColumn>().Should().Contain(c => c.ColumnName == "Date");
        result.RenderedOutput.Should().Contain("\"type\":\"line\"");
        result.RenderedOutput.Should().Contain("\"point\":true");
    }

    [Fact]
    public async Task GenerateReport_WithCaching_SecondCallShouldReturnFromCache()
    {
        // Arrange
        var config = new ReportConfig
        {
            ReportId = "cached-report",
            Query = "SELECT * FROM products",
            Visualization = new VisualizationConfig { Type = "table" },
            CacheTtl = TimeSpan.FromMinutes(5)
        };

        // Act
        var firstResult = await _reportService.GenerateReportAsync(config);
        var secondResult = await _reportService.GenerateReportAsync(config);

        // Assert
        firstResult.FromCache.Should().BeFalse();
        secondResult.FromCache.Should().BeTrue();
        secondResult.RenderedOutput.Should().Be(firstResult.RenderedOutput);
    }

    [Fact]
    public async Task GenerateReport_WithParameters_ShouldPassParametersToQuery()
    {
        // Arrange
        var config = new ReportConfig
        {
            ReportId = "parameterized-report",
            Query = "SELECT * FROM sales WHERE year = @year LIMIT @limit",
            Parameters = new Dictionary<string, object>
            {
                { "year", 2024 },
                { "limit", 10 }
            },
            Visualization = new VisualizationConfig { Type = "table" }
        };

        // Act
        var result = await _reportService.GenerateReportAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Data.Rows.Count.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task GenerateReport_TableVisualization_ShouldRenderHtmlTable()
    {
        // Arrange
        var config = new ReportConfig
        {
            ReportId = "table-report",
            Query = "SELECT * FROM users",
            Visualization = new VisualizationConfig { Type = "table" }
        };

        // Act
        var result = await _reportService.GenerateReportAsync(config);

        // Assert
        result.RenderedOutput.Should().Contain("<table");
        result.RenderedOutput.Should().Contain("<thead>");
        result.RenderedOutput.Should().Contain("<tbody>");
        result.RenderedOutput.Should().NotContain("vegaEmbed");
    }

    [Fact]
    public async Task GenerateReport_PieChart_ShouldUseThetaEncoding()
    {
        // Arrange
        var config = new ReportConfig
        {
            ReportId = "category-distribution",
            Query = "SELECT product, revenue FROM sales",
            Visualization = new VisualizationConfig
            {
                Type = "pie",
                Title = "Revenue Distribution by Product"
            }
        };

        // Act
        var result = await _reportService.GenerateReportAsync(config);

        // Assert
        result.RenderedOutput.Should().Contain("\"theta\"");
        result.RenderedOutput.Should().Contain("\"type\":\"arc\"");
    }

    [Fact]
    public async Task GenerateReport_MultipleDifferentQueries_ShouldAllSucceed()
    {
        // Arrange & Act
        var salesReport = await _reportService.GenerateReportAsync(new ReportConfig
        {
            Query = "SELECT * FROM sales",
            Visualization = new VisualizationConfig { Type = "bar" }
        });

        var userReport = await _reportService.GenerateReportAsync(new ReportConfig
        {
            Query = "SELECT * FROM users",
            Visualization = new VisualizationConfig { Type = "table" }
        });

        var productReport = await _reportService.GenerateReportAsync(new ReportConfig
        {
            Query = "SELECT * FROM products",
            Visualization = new VisualizationConfig { Type = "line" }
        });

        // Assert
        salesReport.Should().NotBeNull();
        salesReport.Data.Columns.Cast<System.Data.DataColumn>()
            .Should().Contain(c => c.ColumnName == "Revenue" || c.ColumnName == "Region");

        userReport.Should().NotBeNull();
        userReport.Data.Columns.Cast<System.Data.DataColumn>()
            .Should().Contain(c => c.ColumnName.Contains("User"));

        productReport.Should().NotBeNull();
        productReport.Data.Columns.Cast<System.Data.DataColumn>()
            .Should().Contain(c => c.ColumnName.Contains("Product"));
    }

    [Fact]
    public async Task GenerateReport_WithCustomDimensions_ShouldApplyWidthAndHeight()
    {
        // Arrange
        var config = new ReportConfig
        {
            Query = "SELECT region, sales FROM sales_data",
            Visualization = new VisualizationConfig
            {
                Type = "bar",
                Width = 1000,
                Height = 600
            }
        };

        // Act
        var result = await _reportService.GenerateReportAsync(config);

        // Assert
        result.RenderedOutput.Should().Contain("\"width\":1000");
        result.RenderedOutput.Should().Contain("\"height\":600");
    }

    [Fact]
    public async Task GenerateReport_InvalidQuery_ShouldThrowException()
    {
        // Arrange
        var config = new ReportConfig
        {
            Query = "",
            Visualization = new VisualizationConfig { Type = "bar" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _reportService.GenerateReportAsync(config));
    }

    [Fact]
    public async Task CacheService_ExpiredCache_ShouldNotReturnStaleData()
    {
        // Arrange
        var config = new ReportConfig
        {
            ReportId = "expiring-report",
            Query = "SELECT * FROM sales",
            Visualization = new VisualizationConfig { Type = "bar" },
            CacheTtl = TimeSpan.FromMilliseconds(100)
        };

        // Act
        var firstResult = await _reportService.GenerateReportAsync(config);
        await Task.Delay(150); // Wait for cache to expire
        var secondResult = await _reportService.GenerateReportAsync(config);

        // Assert
        firstResult.FromCache.Should().BeFalse();
        secondResult.FromCache.Should().BeFalse();
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
    }
}
