using System.Data;
using FluentAssertions;
using SnowflakeProxy.Core.Models;
using SnowflakeProxy.Core.Services;

namespace SnowflakeProxy.Core.Tests.Services;

public class VegaLiteRendererTests
{
    private readonly VegaLiteRenderer _renderer;

    public VegaLiteRendererTests()
    {
        _renderer = new VegaLiteRenderer();
    }

    [Theory]
    [InlineData("bar")]
    [InlineData("line")]
    [InlineData("scatter")]
    [InlineData("area")]
    [InlineData("pie")]
    [InlineData("table")]
    [InlineData("point")]
    public void SupportsType_WithSupportedTypes_ShouldReturnTrue(string chartType)
    {
        // Act
        var result = _renderer.SupportsType(chartType);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("histogram")]
    [InlineData("heatmap")]
    [InlineData("invalid")]
    public void SupportsType_WithUnsupportedTypes_ShouldReturnFalse(string chartType)
    {
        // Act
        var result = _renderer.SupportsType(chartType);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SupportsType_IsCaseInsensitive()
    {
        // Act
        var lowerResult = _renderer.SupportsType("bar");
        var upperResult = _renderer.SupportsType("BAR");
        var mixedResult = _renderer.SupportsType("Bar");

        // Assert
        lowerResult.Should().BeTrue();
        upperResult.Should().BeTrue();
        mixedResult.Should().BeTrue();
    }

    [Fact]
    public async Task RenderAsync_WithTableType_ShouldRenderHtmlTable()
    {
        // Arrange
        var data = CreateSampleDataTable();
        var config = new VisualizationConfig { Type = "table" };

        // Act
        var result = await _renderer.RenderAsync(data, config);

        // Assert
        result.Should().Contain("<table");
        result.Should().Contain("<thead>");
        result.Should().Contain("<tbody>");
        result.Should().Contain("Region");
        result.Should().Contain("Sales");
        result.Should().Contain("North");
        result.Should().Contain("100");
    }

    [Fact]
    public async Task RenderAsync_WithBarChart_ShouldGenerateVegaLiteSpec()
    {
        // Arrange
        var data = CreateSampleDataTable();
        var config = new VisualizationConfig
        {
            Type = "bar",
            Title = "Sales by Region",
            Width = 600,
            Height = 400
        };

        // Act
        var result = await _renderer.RenderAsync(data, config);

        // Assert
        result.Should().Contain("vegaEmbed");
        result.Should().Contain("\"mark\"");
        result.Should().Contain("\"encoding\"");
        result.Should().Contain("Sales by Region");
    }

    [Fact]
    public async Task RenderAsync_WithLineChart_ShouldIncludePointMarks()
    {
        // Arrange
        var data = CreateSampleDataTable();
        var config = new VisualizationConfig { Type = "line" };

        // Act
        var result = await _renderer.RenderAsync(data, config);

        // Assert
        result.Should().Contain("\"type\":\"line\"");
        result.Should().Contain("\"point\":true");
    }

    [Fact]
    public async Task RenderAsync_WithPieChart_ShouldUseThetaEncoding()
    {
        // Arrange
        var data = CreateSampleDataTable();
        var config = new VisualizationConfig { Type = "pie" };

        // Act
        var result = await _renderer.RenderAsync(data, config);

        // Assert
        result.Should().Contain("\"theta\"");
        result.Should().Contain("\"color\"");
        result.Should().Contain("\"type\":\"arc\"");
    }

    [Fact]
    public async Task RenderAsync_WithCustomDimensions_ShouldApplyWidthAndHeight()
    {
        // Arrange
        var data = CreateSampleDataTable();
        var config = new VisualizationConfig
        {
            Type = "bar",
            Width = 800,
            Height = 600
        };

        // Act
        var result = await _renderer.RenderAsync(data, config);

        // Assert
        result.Should().Contain("\"width\":800");
        result.Should().Contain("\"height\":600");
    }

    [Fact]
    public async Task RenderAsync_WithCustomAxes_ShouldUseSpecifiedColumns()
    {
        // Arrange
        var data = CreateSampleDataTable();
        var config = new VisualizationConfig
        {
            Type = "bar",
            XAxis = "Region",
            YAxis = "Sales"
        };

        // Act
        var result = await _renderer.RenderAsync(data, config);

        // Assert
        result.Should().Contain("\"field\":\"Region\"");
        result.Should().Contain("\"field\":\"Sales\"");
    }

    [Fact]
    public async Task RenderAsync_WithColorField_ShouldAddColorEncoding()
    {
        // Arrange
        var data = CreateSampleDataTableWithCategory();
        var config = new VisualizationConfig
        {
            Type = "bar",
            ColorField = "Category"
        };

        // Act
        var result = await _renderer.RenderAsync(data, config);

        // Assert
        result.Should().Contain("\"color\"");
        result.Should().Contain("\"field\":\"Category\"");
    }

    [Fact]
    public async Task RenderAsync_WithUnsupportedType_ShouldThrowNotSupportedException()
    {
        // Arrange
        var data = CreateSampleDataTable();
        var config = new VisualizationConfig { Type = "unsupported" };

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(
            async () => await _renderer.RenderAsync(data, config));
    }

    [Fact]
    public async Task RenderAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var data = CreateSampleDataTable();
        var config = new VisualizationConfig { Type = "bar" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _renderer.RenderAsync(data, config, cts.Token));
    }

    [Fact]
    public async Task RenderAsync_ShouldIncludeUniqueContainerId()
    {
        // Arrange
        var data = CreateSampleDataTable();
        var config = new VisualizationConfig { Type = "bar" };

        // Act
        var result1 = await _renderer.RenderAsync(data, config);
        var result2 = await _renderer.RenderAsync(data, config);

        // Assert
        result1.Should().Contain("id=\"vis-");
        result2.Should().Contain("id=\"vis-");
        result1.Should().NotBe(result2); // Different container IDs
    }

    [Fact]
    public async Task RenderAsync_WithTemporalData_ShouldDetectTemporalType()
    {
        // Arrange
        var data = CreateTemporalDataTable();
        var config = new VisualizationConfig { Type = "line" };

        // Act
        var result = await _renderer.RenderAsync(data, config);

        // Assert
        result.Should().Contain("\"type\":\"temporal\"");
    }

    [Fact]
    public async Task RenderAsync_WithNumericData_ShouldDetectQuantitativeType()
    {
        // Arrange
        var data = CreateSampleDataTable();
        var config = new VisualizationConfig { Type = "bar" };

        // Act
        var result = await _renderer.RenderAsync(data, config);

        // Assert
        result.Should().Contain("\"type\":\"quantitative\"");
    }

    [Fact]
    public async Task RenderAsync_ShouldEmbedDataInSpec()
    {
        // Arrange
        var data = CreateSampleDataTable();
        var config = new VisualizationConfig { Type = "bar" };

        // Act
        var result = await _renderer.RenderAsync(data, config);

        // Assert
        result.Should().Contain("\"data\"");
        result.Should().Contain("\"values\"");
        result.Should().Contain("North");
        result.Should().Contain("South");
    }

    [Fact]
    public async Task RenderAsync_TableType_ShouldHtmlEncodeValues()
    {
        // Arrange
        var data = new DataTable();
        data.Columns.Add("Name", typeof(string));
        data.Columns.Add("Description", typeof(string));
        data.Rows.Add("Test", "<script>alert('xss')</script>");

        var config = new VisualizationConfig { Type = "table" };

        // Act
        var result = await _renderer.RenderAsync(data, config);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().Contain("&lt;script&gt;");
    }

    private DataTable CreateSampleDataTable()
    {
        var table = new DataTable();
        table.Columns.Add("Region", typeof(string));
        table.Columns.Add("Sales", typeof(int));

        table.Rows.Add("North", 100);
        table.Rows.Add("South", 150);
        table.Rows.Add("East", 120);
        table.Rows.Add("West", 180);

        return table;
    }

    private DataTable CreateSampleDataTableWithCategory()
    {
        var table = new DataTable();
        table.Columns.Add("Region", typeof(string));
        table.Columns.Add("Sales", typeof(int));
        table.Columns.Add("Category", typeof(string));

        table.Rows.Add("North", 100, "A");
        table.Rows.Add("South", 150, "B");
        table.Rows.Add("East", 120, "A");
        table.Rows.Add("West", 180, "B");

        return table;
    }

    private DataTable CreateTemporalDataTable()
    {
        var table = new DataTable();
        table.Columns.Add("Date", typeof(DateTime));
        table.Columns.Add("Value", typeof(int));

        table.Rows.Add(new DateTime(2024, 1, 1), 100);
        table.Rows.Add(new DateTime(2024, 2, 1), 150);
        table.Rows.Add(new DateTime(2024, 3, 1), 120);

        return table;
    }
}
