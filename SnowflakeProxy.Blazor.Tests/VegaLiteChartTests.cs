using Bunit;
using FluentAssertions;
using Microsoft.JSInterop;
using SnowflakeProxy.Blazor.Components;
using System.Data;

namespace SnowflakeProxy.Blazor.Tests;

public class VegaLiteChartTests : TestContext
{
    public VegaLiteChartTests()
    {
        // Setup JS runtime for bUnit
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void VegaLiteChart_RendersContainer()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var spec = new
        {
            mark = "bar",
            encoding = new
            {
                x = new { field = "name", type = "nominal" },
                y = new { field = "value", type = "quantitative" }
            }
        };

        // Act
        var cut = RenderComponent<VegaLiteChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        // Assert
        cut.Find("div").Should().NotBeNull();
        cut.Find("div").GetAttribute("id").Should().StartWith("vis-");
    }

    [Fact]
    public void VegaLiteChart_WithNullData_DoesNotThrow()
    {
        // Arrange
        var spec = new { mark = "bar" };

        // Act & Assert
        var act = () => RenderComponent<VegaLiteChart>(parameters => parameters
            .Add(p => p.Data, null as DataTable)
            .Add(p => p.Spec, spec));

        act.Should().NotThrow();
    }

    [Fact]
    public void VegaLiteChart_WithNullSpec_DoesNotThrow()
    {
        // Arrange
        var dataTable = CreateTestDataTable();

        // Act & Assert
        var act = () => RenderComponent<VegaLiteChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, null));

        act.Should().NotThrow();
    }

    [Fact]
    public void VegaLiteChart_CallsJSInterop_OnAfterRender()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var spec = new
        {
            mark = "bar",
            encoding = new
            {
                x = new { field = "name", type = "nominal" },
                y = new { field = "value", type = "quantitative" }
            }
        };

        // Setup JS module to track calls
        JSInterop.SetupVoid("VegaLiteInterop.render", _ => true);

        // Act
        var cut = RenderComponent<VegaLiteChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        // Wait for render
        cut.WaitForState(() => JSInterop.Invocations.Any(x => x.Identifier == "VegaLiteInterop.render"), timeout: TimeSpan.FromSeconds(5));

        // Assert
        JSInterop.VerifyInvoke("VegaLiteInterop.render", calledTimes: 1);
    }

    [Fact]
    public void VegaLiteChart_GeneratesUniqueContainerId()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var spec = new { mark = "bar" };

        // Act
        var cut1 = RenderComponent<VegaLiteChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        var cut2 = RenderComponent<VegaLiteChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        // Assert
        var id1 = cut1.Find("div").GetAttribute("id");
        var id2 = cut2.Find("div").GetAttribute("id");

        id1.Should().NotBe(id2);
        id1.Should().StartWith("vis-");
        id2.Should().StartWith("vis-");
    }

    [Fact]
    public void VegaLiteChart_InjectsDataIntoSpec()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var spec = new
        {
            mark = "bar",
            encoding = new
            {
                x = new { field = "name", type = "nominal" },
                y = new { field = "value", type = "quantitative" }
            },
            width = 600,
            height = 400
        };

        string? capturedJson = null;
        JSInterop.SetupVoid("VegaLiteInterop.render", invocation =>
        {
            capturedJson = invocation.Arguments[1]?.ToString();
            return true;
        });

        // Act
        var cut = RenderComponent<VegaLiteChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        // Wait for render
        cut.WaitForState(() => capturedJson != null, timeout: TimeSpan.FromSeconds(5));

        // Assert
        capturedJson.Should().NotBeNull();
        capturedJson.Should().Contain("\"data\"");
        capturedJson.Should().Contain("\"values\"");
        capturedJson.Should().Contain("item1");
        capturedJson.Should().Contain("item2");
    }

    [Fact]
    public void VegaLiteChart_HandlesDBNullValues()
    {
        // Arrange
        var dataTable = new DataTable();
        dataTable.Columns.Add("name", typeof(string));
        dataTable.Columns.Add("value", typeof(int));
        dataTable.Rows.Add("item1", 10);
        dataTable.Rows.Add(DBNull.Value, DBNull.Value);
        dataTable.Rows.Add("item3", 30);

        var spec = new { mark = "bar" };

        // Act & Assert
        var act = () => RenderComponent<VegaLiteChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        act.Should().NotThrow();
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
