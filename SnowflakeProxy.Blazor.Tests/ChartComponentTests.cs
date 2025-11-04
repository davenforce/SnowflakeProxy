using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SnowflakeProxy.Blazor.Components;
using SnowflakeProxy.Core.Models;
using SnowflakeProxy.Core.Services;
using System.Data;

namespace SnowflakeProxy.Blazor.Tests;

public class ChartComponentTests : TestContext
{
    private readonly Mock<IReportService> _mockReportService;

    public ChartComponentTests()
    {
        _mockReportService = new Mock<IReportService>();
        Services.AddSingleton(_mockReportService.Object);

        // Setup JS runtime for child chart components
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void ChartComponent_WithValidQuery_RendersSuccessfully()
    {
        // Arrange
        var query = "SELECT * FROM test";
        var dataTable = CreateTestDataTable();
        var queryResult = new QueryResult
        {
            Data = dataTable,
            FromCache = false,
            ExecutedAt = DateTime.UtcNow
        };

        _mockReportService
            .Setup(x => x.ExecuteQueryAsync(query, null, null, default))
            .ReturnsAsync(queryResult);

        var config = new VisualizationConfig
        {
            Spec = new { mark = "bar", encoding = new { x = new { field = "name" }, y = new { field = "value" } } }
        };

        // Act
        var cut = RenderComponent<ChartComponent>(parameters => parameters
            .Add(p => p.Query, query)
            .Add(p => p.Config, config)
            .Add(p => p.Renderer, "vega-lite"));

        // Wait for async rendering
        cut.WaitForState(() => cut.Find(".chart-content") != null, timeout: TimeSpan.FromSeconds(5));

        // Assert
        cut.Find(".chart-content").Should().NotBeNull();
        _mockReportService.Verify(x => x.ExecuteQueryAsync(query, null, null, default), Times.Once);
    }

    [Fact]
    public void ChartComponent_WithEmptyQuery_ShowsError()
    {
        // Arrange
        var config = new VisualizationConfig { Spec = new { mark = "bar" } };

        // Act
        var cut = RenderComponent<ChartComponent>(parameters => parameters
            .Add(p => p.Query, "")
            .Add(p => p.Config, config));

        // Assert
        cut.Markup.Should().Contain("Query parameter is required");
        _mockReportService.Verify(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan?>(), default), Times.Never);
    }

    [Fact]
    public void ChartComponent_WithQueryTooLong_ShowsError()
    {
        // Arrange
        var longQuery = new string('A', 50001); // Exceeds MaxQueryLength
        var config = new VisualizationConfig { Spec = new { mark = "bar" } };

        // Act
        var cut = RenderComponent<ChartComponent>(parameters => parameters
            .Add(p => p.Query, longQuery)
            .Add(p => p.Config, config));

        // Assert
        cut.Markup.Should().Contain("Query exceeds maximum length");
        _mockReportService.Verify(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan?>(), default), Times.Never);
    }

    [Fact]
    public void ChartComponent_WithInvalidRenderer_ShowsError()
    {
        // Arrange
        var query = "SELECT * FROM test";
        var config = new VisualizationConfig { Spec = new { mark = "bar" } };

        // Act
        var cut = RenderComponent<ChartComponent>(parameters => parameters
            .Add(p => p.Query, query)
            .Add(p => p.Config, config)
            .Add(p => p.Renderer, "invalid-renderer"));

        // Assert
        cut.Markup.Should().Contain("Invalid renderer");
        _mockReportService.Verify(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan?>(), default), Times.Never);
    }

    [Fact]
    public void ChartComponent_WithParametersTooLong_ShowsError()
    {
        // Arrange
        var query = "SELECT * FROM test WHERE id = :id";
        var queryParameters = new Dictionary<string, object>
        {
            { new string('A', 101), "value" } // Parameter name too long
        };
        var config = new VisualizationConfig { Spec = new { mark = "bar" } };

        // Act
        var cut = RenderComponent<ChartComponent>(parameters => parameters
            .Add(p => p.Query, query)
            .Add(p => p.Parameters, queryParameters)
            .Add(p => p.Config, config));

        // Assert
        cut.Markup.Should().Contain("exceeds maximum length");
        _mockReportService.Verify(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan?>(), default), Times.Never);
    }

    [Fact]
    public void ChartComponent_WithValidParameters_PassesParameters()
    {
        // Arrange
        var query = "SELECT * FROM test WHERE id = :id";
        var queryParameters = new Dictionary<string, object> { { "id", 123 } };
        var dataTable = CreateTestDataTable();
        var queryResult = new QueryResult { Data = dataTable, FromCache = false, ExecutedAt = DateTime.UtcNow };

        _mockReportService
            .Setup(x => x.ExecuteQueryAsync(query, queryParameters, null, default))
            .ReturnsAsync(queryResult);

        var config = new VisualizationConfig { Spec = new { mark = "bar" } };

        // Act
        var cut = RenderComponent<ChartComponent>(parameters => parameters
            .Add(p => p.Query, query)
            .Add(p => p.Parameters, queryParameters)
            .Add(p => p.Config, config)
            .Add(p => p.Renderer, "vega-lite"));

        // Wait for async rendering
        cut.WaitForState(() => cut.Find(".chart-content") != null, timeout: TimeSpan.FromSeconds(5));

        // Assert
        _mockReportService.Verify(x => x.ExecuteQueryAsync(query, queryParameters, null, default), Times.Once);
    }

    [Fact]
    public void ChartComponent_WithCacheTtl_PassesCacheTtl()
    {
        // Arrange
        var query = "SELECT * FROM test";
        var cacheTtl = TimeSpan.FromMinutes(10);
        var dataTable = CreateTestDataTable();
        var queryResult = new QueryResult { Data = dataTable, FromCache = false, ExecutedAt = DateTime.UtcNow };

        _mockReportService
            .Setup(x => x.ExecuteQueryAsync(query, null, cacheTtl, default))
            .ReturnsAsync(queryResult);

        var config = new VisualizationConfig { Spec = new { mark = "bar" } };

        // Act
        var cut = RenderComponent<ChartComponent>(parameters => parameters
            .Add(p => p.Query, query)
            .Add(p => p.Config, config)
            .Add(p => p.CacheTtl, cacheTtl)
            .Add(p => p.Renderer, "vega-lite"));

        // Wait for async rendering
        cut.WaitForState(() => cut.Find(".chart-content") != null, timeout: TimeSpan.FromSeconds(5));

        // Assert
        _mockReportService.Verify(x => x.ExecuteQueryAsync(query, null, cacheTtl, default), Times.Once);
    }

    [Fact]
    public void ChartComponent_ShowsMetadata_WhenShowMetadataTrue()
    {
        // Arrange
        var query = "SELECT * FROM test";
        var dataTable = CreateTestDataTable();
        dataTable.Rows.Add("item3", 30);
        dataTable.Rows.Add("item4", 40);

        var queryResult = new QueryResult { Data = dataTable, FromCache = true, ExecutedAt = DateTime.UtcNow };

        _mockReportService
            .Setup(x => x.ExecuteQueryAsync(query, null, null, default))
            .ReturnsAsync(queryResult);

        var config = new VisualizationConfig { Spec = new { mark = "bar" } };

        // Act
        var cut = RenderComponent<ChartComponent>(parameters => parameters
            .Add(p => p.Query, query)
            .Add(p => p.Config, config)
            .Add(p => p.ShowMetadata, true)
            .Add(p => p.Renderer, "vega-lite"));

        // Wait for async rendering
        cut.WaitForState(() => cut.Markup.Contains("Rows:"), timeout: TimeSpan.FromSeconds(5));

        // Assert
        cut.Markup.Should().Contain("Rows: 4");
        cut.Markup.Should().Contain("From Cache");
    }

    [Fact]
    public void ChartComponent_OnError_ShowsErrorMessage()
    {
        // Arrange
        var query = "SELECT * FROM test";
        var config = new VisualizationConfig { Spec = new { mark = "bar" } };
        var errorMessage = "Database connection failed";

        _mockReportService
            .Setup(x => x.ExecuteQueryAsync(query, null, null, default))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var cut = RenderComponent<ChartComponent>(parameters => parameters
            .Add(p => p.Query, query)
            .Add(p => p.Config, config)
            .Add(p => p.Renderer, "vega-lite"));

        // Wait for error to display
        cut.WaitForState(() => cut.Markup.Contains("Error"), timeout: TimeSpan.FromSeconds(5));

        // Assert
        cut.Markup.Should().Contain(errorMessage);
        cut.Find(".alert-danger").Should().NotBeNull();
    }

    [Fact]
    public void ChartComponent_WithTitle_DisplaysTitle()
    {
        // Arrange
        var query = "SELECT * FROM test";
        var title = "My Test Chart";
        var dataTable = CreateTestDataTable();
        var queryResult = new QueryResult { Data = dataTable, FromCache = false, ExecutedAt = DateTime.UtcNow };

        _mockReportService
            .Setup(x => x.ExecuteQueryAsync(query, null, null, default))
            .ReturnsAsync(queryResult);

        var config = new VisualizationConfig { Spec = new { mark = "bar" } };

        // Act
        var cut = RenderComponent<ChartComponent>(parameters => parameters
            .Add(p => p.Query, query)
            .Add(p => p.Config, config)
            .Add(p => p.Title, title)
            .Add(p => p.Renderer, "vega-lite"));

        // Wait for async rendering
        cut.WaitForState(() => cut.Markup.Contains(title), timeout: TimeSpan.FromSeconds(5));

        // Assert
        cut.Markup.Should().Contain(title);
        cut.Find(".chart-title").TextContent.Should().Be(title);
    }

    private DataTable CreateTestDataTable()
    {
        var table = new DataTable();
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("value", typeof(int));
        table.Rows.Add("item1", 10);
        table.Rows.Add("item2", 20);
        return table;
    }
}
