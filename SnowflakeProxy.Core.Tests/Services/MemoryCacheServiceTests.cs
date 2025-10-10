using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using SnowflakeProxy.Core.Services;

namespace SnowflakeProxy.Core.Tests.Services;

public class MemoryCacheServiceTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly MemoryCacheService _cacheService;

    public MemoryCacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _cacheService = new MemoryCacheService(_memoryCache);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _cacheService.GetAsync<string>("nonexistent-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_AndGetAsync_ShouldStoreAndRetrieveValue()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithExpiration_ShouldExpireAfterTime()
    {
        // Arrange
        const string key = "expiring-key";
        const string value = "expiring-value";
        var expiration = TimeSpan.FromMilliseconds(100);

        // Act
        await _cacheService.SetAsync(key, value, expiration);
        var resultBefore = await _cacheService.GetAsync<string>(key);

        await Task.Delay(150); // Wait for expiration

        var resultAfter = await _cacheService.GetAsync<string>(key);

        // Assert
        resultBefore.Should().Be(value);
        resultAfter.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_WithoutExpiration_ShouldUseDefaultExpiration()
    {
        // Arrange
        const string key = "default-expiration-key";
        const string value = "default-expiration-value";

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithComplexObject_ShouldStoreAndRetrieve()
    {
        // Arrange
        const string key = "complex-object-key";
        var value = new TestData { Id = 1, Name = "Test", Values = new List<int> { 1, 2, 3 } };

        // Act
        await _cacheService.SetAsync(key, value);
        var result = await _cacheService.GetAsync<TestData>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(value.Id);
        result.Name.Should().Be(value.Name);
        result.Values.Should().BeEquivalentTo(value.Values);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveKey()
    {
        // Arrange
        const string key = "remove-key";
        const string value = "remove-value";
        await _cacheService.SetAsync(key, value);

        // Act
        await _cacheService.RemoveAsync(key);
        var result = await _cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveAllEntries()
    {
        // Arrange
        await _cacheService.SetAsync("key1", "value1");
        await _cacheService.SetAsync("key2", "value2");
        await _cacheService.SetAsync("key3", "value3");

        // Act
        await _cacheService.ClearAsync();

        var result1 = await _cacheService.GetAsync<string>("key1");
        var result2 = await _cacheService.GetAsync<string>("key2");
        var result3 = await _cacheService.GetAsync<string>("key3");

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
        result3.Should().BeNull();
    }

    [Fact]
    public async Task SetAsync_OverwriteExistingKey_ShouldUpdateValue()
    {
        // Arrange
        const string key = "overwrite-key";
        const string originalValue = "original";
        const string newValue = "updated";

        // Act
        await _cacheService.SetAsync(key, originalValue);
        var resultBefore = await _cacheService.GetAsync<string>(key);

        await _cacheService.SetAsync(key, newValue);
        var resultAfter = await _cacheService.GetAsync<string>(key);

        // Assert
        resultBefore.Should().Be(originalValue);
        resultAfter.Should().Be(newValue);
    }

    [Fact]
    public async Task GetAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _cacheService.GetAsync<string>("key", cts.Token));
    }

    [Fact]
    public async Task SetAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _cacheService.SetAsync("key", "value", cancellationToken: cts.Token));
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
    }

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<int> Values { get; set; } = new();
    }
}
