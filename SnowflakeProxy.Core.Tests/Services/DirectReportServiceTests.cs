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
    private readonly DirectReportService _reportService;

    public DirectReportServiceTests()
    {
        _mockSnowflakeService = new Mock<ISnowflakeService>();
        _mockCacheService = new Mock<ICacheService>();

        _reportService = new DirectReportService(
            _mockSnowflakeService.Object,
            _mockCacheService.Object);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithQuery_ShouldExecuteAndReturnData()
    {
        // Arrange
        var query = "SELECT * FROM test";
        var dataTable = CreateSampleDataTable();

        _mockCacheService
            .Setup(x => x.GetAsync<QueryResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueryResult?)null);

        _mockSnowflakeService
            .Setup(x => x.ExecuteQueryAsync(query, It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTable);

        // Act
        var result = await _reportService.ExecuteQueryAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().BeSameAs(dataTable);
        result.FromCache.Should().BeFalse();

        _mockSnowflakeService.Verify(
            x => x.ExecuteQueryAsync(query, It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithCacheTtl_ShouldCacheResult()
    {
        // Arrange
        var query = "SELECT * FROM test";
        var cacheTtl = TimeSpan.FromMinutes(10);
        var dataTable = CreateSampleDataTable();

        _mockCacheService
            .Setup(x => x.GetAsync<QueryResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueryResult?)null);

        _mockSnowflakeService
            .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTable);

        // Act
        await _reportService.ExecuteQueryAsync(query, null, cacheTtl);

        // Assert
        _mockCacheService.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<QueryResult>(),
                cacheTtl,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithoutCacheTtl_ShouldNotCacheResult()
    {
        // Arrange
        var query = "SELECT * FROM test";
        var dataTable = CreateSampleDataTable();

        _mockCacheService
            .Setup(x => x.GetAsync<QueryResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueryResult?)null);

        _mockSnowflakeService
            .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTable);

        // Act
        await _reportService.ExecuteQueryAsync(query, null, null);

        // Assert
        _mockCacheService.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<QueryResult>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _mockCacheService.Verify(
            x => x.GetAsync<QueryResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WhenCacheHit_ShouldReturnCachedResult()
    {
        // Arrange
        var query = "SELECT * FROM test";
        var cacheTtl = TimeSpan.FromMinutes(5);

        var cachedResult = new QueryResult
        {
            Data = CreateSampleDataTable(),
            FromCache = false,
            ExecutedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        _mockCacheService
            .Setup(x => x.GetAsync<QueryResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        // Act
        var result = await _reportService.ExecuteQueryAsync(query, null, cacheTtl);

        // Assert
        result.Should().NotBeNull();
        result.FromCache.Should().BeTrue();
        result.Data.Should().BeSameAs(cachedResult.Data);

        _mockSnowflakeService.Verify(
            x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteQueryAsync_WithParameters_ShouldPassParametersToQuery()
    {
        // Arrange
        var query = "SELECT * FROM sales WHERE year = @year AND region = @region";
        var parameters = new Dictionary<string, object>
        {
            { "year", 2024 },
            { "region", "North" }
        };

        var dataTable = CreateSampleDataTable();

        _mockCacheService
            .Setup(x => x.GetAsync<QueryResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueryResult?)null);

        _mockSnowflakeService
            .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTable);

        // Act
        await _reportService.ExecuteQueryAsync(query, parameters);

        // Assert
        _mockSnowflakeService.Verify(
            x => x.ExecuteQueryAsync(
                query,
                It.Is<Dictionary<string, object>>(p =>
                    p.ContainsKey("year") &&
                    p.ContainsKey("region") &&
                    (int)p["year"] == 2024 &&
                    (string)p["region"] == "North"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteQueryAsync_EmptyQuery_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _reportService.ExecuteQueryAsync(""));
    }

    [Fact]
    public async Task ExecuteQueryAsync_NullQuery_ShouldThrowArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _reportService.ExecuteQueryAsync(null!));
    }

    [Fact]
    public async Task ExecuteQueryAsync_SameQueryMultipleTimes_ShouldUseSameCacheKey()
    {
        // Arrange
        var query = "SELECT * FROM test";
        var cacheTtl = TimeSpan.FromMinutes(5);
        var dataTable = CreateSampleDataTable();

        string? capturedCacheKey = null;

        _mockCacheService
            .Setup(x => x.GetAsync<QueryResult>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueryResult?)null);

        _mockSnowflakeService
            .Setup(x => x.ExecuteQueryAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataTable);

        _mockCacheService
            .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<QueryResult>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Callback<string, QueryResult, TimeSpan?, CancellationToken>((key, _, _, _) =>
            {
                if (capturedCacheKey == null)
                    capturedCacheKey = key;
                else
                    capturedCacheKey.Should().Be(key); // Same query should use same cache key
            })
            .Returns(Task.CompletedTask);

        // Act
        await _reportService.ExecuteQueryAsync(query, null, cacheTtl);
        await _reportService.ExecuteQueryAsync(query, null, cacheTtl);

        // Assert
        capturedCacheKey.Should().NotBeNull();
        _mockCacheService.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<QueryResult>(), cacheTtl, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
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
