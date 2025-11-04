using Bunit;
using FluentAssertions;
using Microsoft.JSInterop;
using SnowflakeProxy.Blazor.Components;
using System.Data;

namespace SnowflakeProxy.Blazor.Tests;

public class ChartJsChartTests : TestContext
{
    public ChartJsChartTests()
    {
        // Setup JS runtime for bUnit
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void ChartJsChart_RendersCanvas()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var spec = new
        {
            type = "bar",
            data = new
            {
                labelField = "name",
                datasets = new[]
                {
                    new
                    {
                        label = "Values",
                        dataField = "value",
                        backgroundColor = new[] { "rgba(75, 192, 192, 0.2)" },
                        borderColor = new[] { "rgba(75, 192, 192, 1)" },
                        borderWidth = new[] { 1.0 }
                    }
                }
            }
        };

        // Act
        var cut = RenderComponent<ChartJsChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        // Assert
        cut.Find("canvas").Should().NotBeNull();
        cut.Find("canvas").GetAttribute("id").Should().StartWith("chart-");
    }

    [Fact]
    public void ChartJsChart_WithNullData_DoesNotThrow()
    {
        // Arrange
        var spec = new { type = "bar", data = new { labelField = "name" } };

        // Act & Assert
        var act = () => RenderComponent<ChartJsChart>(parameters => parameters
            .Add(p => p.Data, null as DataTable)
            .Add(p => p.Spec, spec));

        act.Should().NotThrow();
    }

    [Fact]
    public void ChartJsChart_WithNullSpec_DoesNotThrow()
    {
        // Arrange
        var dataTable = CreateTestDataTable();

        // Act & Assert
        var act = () => RenderComponent<ChartJsChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, null));

        act.Should().NotThrow();
    }

    [Fact]
    public void ChartJsChart_CallsJSInterop_OnAfterRender()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var spec = new
        {
            type = "bar",
            data = new
            {
                labelField = "name",
                datasets = new[]
                {
                    new
                    {
                        label = "Values",
                        dataField = "value"
                    }
                }
            }
        };

        JSInterop.SetupVoid("ChartJsInterop.initialize", _ => true);

        // Act
        var cut = RenderComponent<ChartJsChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        // Wait for render
        cut.WaitForState(() => JSInterop.Invocations.Any(x => x.Identifier == "ChartJsInterop.initialize"), timeout: TimeSpan.FromSeconds(5));

        // Assert
        JSInterop.VerifyInvoke("ChartJsInterop.initialize", calledTimes: 1);
    }

    [Fact]
    public void ChartJsChart_GeneratesUniqueCanvasId()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var spec = new { type = "bar", data = new { labelField = "name" } };

        // Act
        var cut1 = RenderComponent<ChartJsChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        var cut2 = RenderComponent<ChartJsChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        // Assert
        var id1 = cut1.Find("canvas").GetAttribute("id");
        var id2 = cut2.Find("canvas").GetAttribute("id");

        id1.Should().NotBe(id2);
        id1.Should().StartWith("chart-");
        id2.Should().StartWith("chart-");
    }

    [Fact]
    public void ChartJsChart_BuildsChartConfig_WithDataFromDataTable()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var spec = new
        {
            type = "line",
            data = new
            {
                labelField = "name",
                datasets = new[]
                {
                    new
                    {
                        label = "Values",
                        dataField = "value",
                        borderColor = "rgb(75, 192, 192)"
                    }
                }
            },
            options = new
            {
                responsive = true,
                plugins = new
                {
                    title = new
                    {
                        display = true,
                        text = "Test Chart"
                    }
                }
            }
        };

        object? capturedConfig = null;
        JSInterop.SetupVoid("ChartJsInterop.initialize", invocation =>
        {
            capturedConfig = invocation.Arguments[1];
            return true;
        });

        // Act
        var cut = RenderComponent<ChartJsChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        // Wait for render
        cut.WaitForState(() => capturedConfig != null, timeout: TimeSpan.FromSeconds(5));

        // Assert
        capturedConfig.Should().NotBeNull();
    }

    [Fact]
    public void ChartJsChart_HandlesMultipleDatasets()
    {
        // Arrange
        var dataTable = new DataTable();
        dataTable.Columns.Add("month", typeof(string));
        dataTable.Columns.Add("sales", typeof(int));
        dataTable.Columns.Add("expenses", typeof(int));
        dataTable.Rows.Add("Jan", 100, 80);
        dataTable.Rows.Add("Feb", 150, 90);

        var spec = new
        {
            type = "bar",
            data = new
            {
                labelField = "month",
                datasets = new[]
                {
                    new
                    {
                        label = "Sales",
                        dataField = "sales",
                        backgroundColor = new[] { "rgba(75, 192, 192, 0.2)" }
                    },
                    new
                    {
                        label = "Expenses",
                        dataField = "expenses",
                        backgroundColor = new[] { "rgba(255, 99, 132, 0.2)" }
                    }
                }
            }
        };

        // Act & Assert
        var act = () => RenderComponent<ChartJsChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        act.Should().NotThrow();
    }

    [Fact]
    public void ChartJsChart_HandlesDBNullValues()
    {
        // Arrange
        var dataTable = new DataTable();
        dataTable.Columns.Add("name", typeof(string));
        dataTable.Columns.Add("value", typeof(int));
        dataTable.Rows.Add("item1", 10);
        dataTable.Rows.Add(DBNull.Value, DBNull.Value);
        dataTable.Rows.Add("item3", 30);

        var spec = new
        {
            type = "bar",
            data = new
            {
                labelField = "name",
                datasets = new[]
                {
                    new { label = "Values", dataField = "value" }
                }
            }
        };

        // Act & Assert
        var act = () => RenderComponent<ChartJsChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        act.Should().NotThrow();
    }

    [Fact]
    public void ChartJsChart_SupportsPieChart()
    {
        // Arrange
        var dataTable = new DataTable();
        dataTable.Columns.Add("category", typeof(string));
        dataTable.Columns.Add("percentage", typeof(double));
        dataTable.Rows.Add("Category A", 35.5);
        dataTable.Rows.Add("Category B", 25.0);
        dataTable.Rows.Add("Category C", 39.5);

        var spec = new
        {
            type = "pie",
            data = new
            {
                labelField = "category",
                datasets = new[]
                {
                    new
                    {
                        label = "Distribution",
                        dataField = "percentage",
                        backgroundColor = new[]
                        {
                            "rgba(255, 99, 132, 0.8)",
                            "rgba(54, 162, 235, 0.8)",
                            "rgba(255, 206, 86, 0.8)"
                        }
                    }
                }
            }
        };

        // Act & Assert
        var act = () => RenderComponent<ChartJsChart>(parameters => parameters
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
