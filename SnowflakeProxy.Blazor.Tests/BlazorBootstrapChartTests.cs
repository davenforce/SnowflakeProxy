using Bunit;
using FluentAssertions;
using SnowflakeProxy.Blazor.Components;
using System.Data;

namespace SnowflakeProxy.Blazor.Tests;

public class BlazorBootstrapChartTests : TestContext
{
    public BlazorBootstrapChartTests()
    {
        // Setup JS runtime for bUnit (Blazor Bootstrap uses JSInterop)
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void BlazorBootstrapChart_WithValidBarChartSpec_Renders()
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
        var cut = RenderComponent<BlazorBootstrapChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        // Assert
        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void BlazorBootstrapChart_WithLineChartSpec_Renders()
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
                        backgroundColor = new[] { "rgba(75, 192, 192, 0.2)" },
                        borderColor = new[] { "rgba(75, 192, 192, 1)" },
                        borderWidth = new[] { 1.0 },
                        fill = true,
                        tension = 0.4
                    }
                }
            }
        };

        // Act
        var cut = RenderComponent<BlazorBootstrapChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        // Assert
        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void BlazorBootstrapChart_WithPieChartSpec_Renders()
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

        // Act
        var cut = RenderComponent<BlazorBootstrapChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        // Assert
        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void BlazorBootstrapChart_WithNullData_DoesNotThrow()
    {
        // Arrange
        var spec = new
        {
            type = "bar",
            data = new { labelField = "name" }
        };

        // Act & Assert
        var act = () => RenderComponent<BlazorBootstrapChart>(parameters => parameters
            .Add(p => p.Data, null as DataTable)
            .Add(p => p.Spec, spec));

        act.Should().NotThrow();
    }

    [Fact]
    public void BlazorBootstrapChart_WithNullSpec_DoesNotThrow()
    {
        // Arrange
        var dataTable = CreateTestDataTable();

        // Act & Assert
        var act = () => RenderComponent<BlazorBootstrapChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, null));

        act.Should().NotThrow();
    }

    [Fact]
    public void BlazorBootstrapChart_WithInvalidChartType_ThrowsNotSupportedException()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var spec = new
        {
            type = "invalid-type",
            data = new
            {
                labelField = "name",
                datasets = new[] { new { label = "Values", dataField = "value" } }
            }
        };

        // Act & Assert
        var act = () => RenderComponent<BlazorBootstrapChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Unsupported chart type*");
    }

    [Fact]
    public void BlazorBootstrapChart_HandlesDBNullValues()
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
                    new
                    {
                        label = "Values",
                        dataField = "value",
                        backgroundColor = new[] { "rgba(75, 192, 192, 0.2)" }
                    }
                }
            }
        };

        // Act & Assert
        var act = () => RenderComponent<BlazorBootstrapChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        act.Should().NotThrow();
    }

    [Fact]
    public void BlazorBootstrapChart_WithDoughnutChartSpec_Renders()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var spec = new
        {
            type = "doughnut",
            data = new
            {
                labelField = "name",
                datasets = new[]
                {
                    new
                    {
                        label = "Values",
                        dataField = "value",
                        backgroundColor = new[]
                        {
                            "rgba(255, 99, 132, 0.8)",
                            "rgba(54, 162, 235, 0.8)"
                        }
                    }
                }
            }
        };

        // Act
        var cut = RenderComponent<BlazorBootstrapChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        // Assert
        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void BlazorBootstrapChart_WithPolarAreaChartSpec_Renders()
    {
        // Arrange
        var dataTable = CreateTestDataTable();
        var spec = new
        {
            type = "polararea",
            data = new
            {
                labelField = "name",
                datasets = new[]
                {
                    new
                    {
                        label = "Values",
                        dataField = "value",
                        backgroundColor = new[]
                        {
                            "rgba(255, 99, 132, 0.8)",
                            "rgba(54, 162, 235, 0.8)"
                        }
                    }
                }
            }
        };

        // Act
        var cut = RenderComponent<BlazorBootstrapChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        // Assert
        cut.Markup.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void BlazorBootstrapChart_WithOptionsInSpec_UsesOptions()
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
                        backgroundColor = new[] { "rgba(75, 192, 192, 0.2)" }
                    }
                }
            },
            options = new
            {
                responsive = false, // Override default
                plugins = new
                {
                    title = new
                    {
                        display = true,
                        text = "My Custom Chart"
                    }
                }
            }
        };

        // Act & Assert
        var act = () => RenderComponent<BlazorBootstrapChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        act.Should().NotThrow();
    }

    [Fact]
    public void BlazorBootstrapChart_WithMultipleDatasets_Renders()
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
        var act = () => RenderComponent<BlazorBootstrapChart>(parameters => parameters
            .Add(p => p.Data, dataTable)
            .Add(p => p.Spec, spec));

        act.Should().NotThrow();
    }

    [Fact]
    public void BlazorBootstrapChart_WithMissingDataField_UsesFallbackColumn()
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
                        label = "Values"
                        // No dataField specified - should use second column
                    }
                }
            }
        };

        // Act & Assert
        var act = () => RenderComponent<BlazorBootstrapChart>(parameters => parameters
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
