using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SnowflakeProxy.Core.Models;
using SnowflakeProxy.Core.Services;

namespace SnowflakeProxy.Core.Extensions;

/// <summary>
/// Extension methods for configuring SnowflakeReporting services in dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds SnowflakeReporting services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration containing Snowflake settings</param>
    /// <param name="configSectionName">The configuration section name (default: "Snowflake")</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSnowflakeReporting(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = "Snowflake")
    {
        // Bind Snowflake configuration
        var snowflakeConfig = new SnowflakeConfiguration();
        configuration.GetSection(configSectionName).Bind(snowflakeConfig);
        services.AddSingleton(snowflakeConfig);

        // Register core services
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<ISnowflakeService, DirectSnowflakeService>();
        services.AddSingleton<IVisualizationRenderer, VegaLiteRenderer>();
        services.AddScoped<IReportService, DirectReportService>();

        return services;
    }

    /// <summary>
    /// Adds SnowflakeReporting services with explicit configuration.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure Snowflake settings</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSnowflakeReporting(
        this IServiceCollection services,
        Action<SnowflakeConfiguration> configureOptions)
    {
        var config = new SnowflakeConfiguration();
        configureOptions(config);
        services.AddSingleton(config);

        // Register core services
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<ISnowflakeService, DirectSnowflakeService>();
        services.AddSingleton<IVisualizationRenderer, VegaLiteRenderer>();
        services.AddScoped<IReportService, DirectReportService>();

        return services;
    }

    /// <summary>
    /// Adds SnowflakeReporting services with custom implementations.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The Snowflake configuration</param>
    /// <param name="configureServices">Action to register custom service implementations</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSnowflakeReporting(
        this IServiceCollection services,
        SnowflakeConfiguration configuration,
        Action<IServiceCollection>? configureServices = null)
    {
        services.AddSingleton(configuration);

        // Register core services
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<ISnowflakeService, DirectSnowflakeService>();
        services.AddSingleton<IVisualizationRenderer, VegaLiteRenderer>();
        services.AddScoped<IReportService, DirectReportService>();

        // Allow custom service registration
        configureServices?.Invoke(services);

        return services;
    }

    /// <summary>
    /// Adds SnowflakeReporting services with a mock Snowflake service for testing/development.
    /// This allows you to test report generation without actual Snowflake credentials.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSnowflakeReportingWithMockData(this IServiceCollection services)
    {
        // Register core services with mock Snowflake service
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<ISnowflakeService, MockSnowflakeService>();
        services.AddSingleton<IVisualizationRenderer, VegaLiteRenderer>();
        services.AddScoped<IReportService, DirectReportService>();

        return services;
    }
}
