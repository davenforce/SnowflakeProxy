using System.Data;
using FluentAssertions;
using Moq;
using SnowflakeProxy.Core.Models;
using SnowflakeProxy.Core.Services;

namespace SnowflakeProxy.Core.Tests.Services;

public class DirectReportServiceTests
{
    private readonly Mock<ISnowflakeService> _mockSnowflakeService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IVisualizationRenderer> _mockRenderer;
    private readonly DirectReportService _reportService;

    public DirectReportServiceTests()
    {
        _mockSnowflakeService = new Mock<ISnowflakeService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockRenderer = new Mock<IVisualizationRenderer>();

        _reportService = new DirectReportService(
            _mockSnowflakeService.Object,
            _mockCacheService.Object,
            _mockRenderer.Object);
    }

    [Fact]
    public async Task GenerateReportAsync_WithConfig_ShouldExecuteQueryAndRender()
    {
        // Arrange
        var config = new ReportConfig
        {
            ReportId = "test-report",
            Query = "SELECT * FROM test",
            Parameters = new Dictionary<string, object>(),
            Visualization = new VisualizationConfig { Type = "bar" }
        };

        var dataTable = CreateSampleDataTable();
        var renderedOutput = "<div>Chart</div>";

        _mockCacheService
            .Setup(x => x.GetAsync<ReportResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportResult?)null);

        _mockSnowflakeService
            .Setup(x => x.ExecuteQueryAsync(config.Query, config.Parameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTable);

        _mockRenderer
            .Setup(x => x.RenderAsync(dataTable, config.Visualization, It.IsAny<CancellationToken>()))
            .ReturnsAsync(renderedOutput);

        // Act
        var result = await _reportService.GenerateReportAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().BeSameAs(dataTable);
        result.RenderedOutput.Should().Be(renderedOutput);
        result.FromCache.Should().BeFalse();

        _mockSnowflakeService.Verify(
            x => x.ExecuteQueryAsync(config.Query, config.Parameters, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockRenderer.Verify(
            x => x.RenderAsync(dataTable, config.Visualization, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateReportAsync_WithCacheTtl_ShouldCacheResult()
    {
        // Arrange
        var config = new ReportConfig
        {
            ReportId = "test-report",
            Query = "SELECT * FROM test",
            CacheTtl = TimeSpan.FromMinutes(10)
        };

        var dataTable = CreateSampleDataTable();
        var renderedOutput = "<div>Chart</div>";

        _mockCacheService
            .Setup(x => x.GetAsync<ReportResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportResult?)null);

        _mockSnowflakeService
            .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTable);

        _mockRenderer
            .Setup(x => x.RenderAsync(It.IsAny<DataTable>(), It.IsAny<VisualizationConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(renderedOutput);

        // Act
        await _reportService.GenerateReportAsync(config);

        // Assert
        _mockCacheService.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<ReportResult>(),
                config.CacheTtl,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateReportAsync_WithoutCacheTtl_ShouldNotCacheResult()
    {
        // Arrange
        var config = new ReportConfig
        {
            ReportId = "test-report",
            Query = "SELECT * FROM test",
            CacheTtl = null
        };

        var dataTable = CreateSampleDataTable();

        _mockCacheService
            .Setup(x => x.GetAsync<ReportResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportResult?)null);

        _mockSnowflakeService
            .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTable);

        _mockRenderer
            .Setup(x => x.RenderAsync(It.IsAny<DataTable>(), It.IsAny<VisualizationConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<div>Chart</div>");

        // Act
        await _reportService.GenerateReportAsync(config);

        // Assert
        _mockCacheService.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<ReportResult>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateReportAsync_WhenCacheHit_ShouldReturnCachedResult()
    {
        // Arrange
        var config = new ReportConfig
        {
            ReportId = "test-report",
            Query = "SELECT * FROM test"
        };

        var cachedResult = new ReportResult
        {
            Data = CreateSampleDataTable(),
            RenderedOutput = "<div>Cached Chart</div>",
            FromCache = false
        };

        _mockCacheService
            .Setup(x => x.GetAsync<ReportResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        // Act
        var result = await _reportService.GenerateReportAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.FromCache.Should().BeTrue();
        result.RenderedOutput.Should().Be(cachedResult.RenderedOutput);

        _mockSnowflakeService.Verify(
            x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _mockRenderer.Verify(
            x => x.RenderAsync(It.IsAny<DataTable>(), It.IsAny<VisualizationConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateReportAsync_WithParameters_ShouldPassParametersToQuery()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            { "year", 2024 },
            { "region", "North" }
        };

        var config = new ReportConfig
        {
            ReportId = "test-report",
            Query = "SELECT * FROM sales WHERE year = @year AND region = @region",
            Parameters = parameters
        };

        var dataTable = CreateSampleDataTable();

        _mockCacheService
            .Setup(x => x.GetAsync<ReportResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportResult?)null);

        _mockSnowflakeService
            .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTable);

        _mockRenderer
            .Setup(x => x.RenderAsync(It.IsAny<DataTable>(), It.IsAny<VisualizationConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<div>Chart</div>");

        // Act
        await _reportService.GenerateReportAsync(config);

        // Assert
        _mockSnowflakeService.Verify(
            x => x.ExecuteQueryAsync(
                config.Query,
                It.Is<Dictionary<string, object>>(p =>
                    p.ContainsKey("year") &&
                    p.ContainsKey("region") &&
                    (int)p["year"] == 2024 &&
                    (string)p["region"] == "North"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateReportAsync_WithReportId_ShouldLoadConfigFromFile()
    {
        // Arrange
        var reportId = "sales-dashboard";
        var parameters = new Dictionary<string, object> { { "year", 2024 } };

        // Create a test config file
        var configPath = Path.Combine("reports", $"{reportId}.json");
        Directory.CreateDirectory("reports");

        var configJson = @"{
            ""reportId"": ""sales-dashboard"",
            ""query"": ""SELECT * FROM sales"",
            ""parameters"": {},
            ""visualization"": {
                ""type"": ""bar""
            },
            ""cacheTtl"": ""00:15:00""
        }";

        await File.WriteAllTextAsync(configPath, configJson);

        var dataTable = CreateSampleDataTable();

        _mockCacheService
            .Setup(x => x.GetAsync<ReportResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportResult?)null);

        _mockSnowflakeService
            .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTable);

        _mockRenderer
            .Setup(x => x.RenderAsync(It.IsAny<DataTable>(), It.IsAny<VisualizationConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<div>Chart</div>");

        try
        {
            // Act
            var result = await _reportService.GenerateReportAsync(reportId, parameters);

            // Assert
            result.Should().NotBeNull();
            _mockSnowflakeService.Verify(
                x => x.ExecuteQueryAsync("SELECT * FROM sales", It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            // Cleanup
            if (File.Exists(configPath))
                File.Delete(configPath);
            if (Directory.Exists("reports") && !Directory.EnumerateFileSystemEntries("reports").Any())
                Directory.Delete("reports");
        }
    }

    [Fact]
    public async Task GenerateReportAsync_WithReportIdNotFound_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var reportId = "nonexistent-report";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _reportService.GenerateReportAsync(reportId));
    }

    [Fact]
    public async Task GenerateReportAsync_WithRuntimeParameters_ShouldMergeWithConfigParameters()
    {
        // Arrange
        var reportId = "test-report";
        var runtimeParams = new Dictionary<string, object> { { "region", "South" } };

        // Create test config with default parameters
        var configPath = Path.Combine("reports", $"{reportId}.json");
        Directory.CreateDirectory("reports");

        var configJson = @"{
            ""reportId"": ""test-report"",
            ""query"": ""SELECT * FROM sales"",
            ""parameters"": {
                ""year"": 2023,
                ""region"": ""North""
            },
            ""visualization"": {
                ""type"": ""bar""
            }
        }";

        await File.WriteAllTextAsync(configPath, configJson);

        var dataTable = CreateSampleDataTable();

        _mockCacheService
            .Setup(x => x.GetAsync<ReportResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportResult?)null);

        _mockSnowflakeService
            .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTable);

        _mockRenderer
            .Setup(x => x.RenderAsync(It.IsAny<DataTable>(), It.IsAny<VisualizationConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<div>Chart</div>");

        try
        {
            // Act
            await _reportService.GenerateReportAsync(reportId, runtimeParams);

            // Assert - Runtime parameters should override config parameters
            _mockSnowflakeService.Verify(
                x => x.ExecuteQueryAsync(
                    It.IsAny<string>(),
                    It.Is<Dictionary<string, object>>(p =>
                        (string)p["region"] == "South" && // Runtime override
                        (long)p["year"] == 2023), // From config
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            // Cleanup
            if (File.Exists(configPath))
                File.Delete(configPath);
            if (Directory.Exists("reports") && !Directory.EnumerateFileSystemEntries("reports").Any())
                Directory.Delete("reports");
        }
    }

    private DataTable CreateSampleDataTable()
    {
        var table = new DataTable();
        table.Columns.Add("Region", typeof(string));
        table.Columns.Add("Sales", typeof(int));
        table.Rows.Add("North", 100);
        table.Rows.Add("South", 150);
        return table;
    }
}
