using Libraries.ExtensionSharedCode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Libraries.ServiceDiscovery.Eureka.AppExtensions;

public static partial class EurekaCloudFoundryServiceDiscoveryExtensions
{
    /// <summary>
    /// Configures <see cref="EurekaClientOptions" /> to use credentials originating from the Cloud Foundry VCAP_SERVICES environment variable when running
    /// on Cloud Foundry.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> that provides access to the application's configuration.
    /// </param>
    /// <param name="serviceBindingName">
    /// The name of the service binding in VCAP_SERVICES. If not specified, this method attempts to select a single compatible service binding.
    /// </param>
    /// <param name="configureEurekaClient">
    /// An optional callback to further configure <see cref="EurekaClientOptions" /> using the selected Cloud Foundry service binding.
    /// </param>
    /// <param name="loggerFactory">
    /// When specified, logs are written here before the service container has been built.
    /// </param>
    /// <returns>
    /// The configured <see cref="IHostApplicationBuilder" /> instance.
    /// </returns>
    public static IHostApplicationBuilder ConfigureEurekaOnCloudFoundry(this IHostApplicationBuilder builder, string? serviceBindingName = null,
        Action<EurekaClientOptions, CloudFoundryService>? configureEurekaClient = null, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        AssertEurekaIsConfigured(builder.Services);

        builder.AddCloudFoundryConfiguration(loggerFactory ?? NullLoggerFactory.Instance);

        builder.Services.AddOptions<EurekaClientOptions>().Configure<IServiceProvider>((eurekaClientOptions, serviceProvider) =>
            ConfigureEurekaClientOptionsFromName(serviceProvider, eurekaClientOptions, builder, serviceBindingName, configureEurekaClient));

        return builder;
    }

    private static void ConfigureEurekaClientOptionsFromName(IServiceProvider serviceProvider, EurekaClientOptions options, IHostApplicationBuilder builder,
        string? serviceBindingName, Action<EurekaClientOptions, CloudFoundryService>? configureEurekaClient)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger(typeof(EurekaCloudFoundryServiceDiscoveryExtensions).FullName!);

        builder.ApplyCloudFoundryServiceBindingFromName(serviceBindingName, IsEurekaService,
            service => ApplyServiceBinding(service, options, configureEurekaClient, logger), logger, loggerFactory);
    }

    /// <summary>
    /// Configures <see cref="EurekaClientOptions" /> to use credentials originating from the Cloud Foundry VCAP_SERVICES environment variable when running
    /// on Cloud Foundry.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> that provides access to the application's configuration.
    /// </param>
    /// <param name="selectServiceBinding">
    /// Returns the service binding from VCAP_SERVICES to use credentials from.
    /// </param>
    /// <param name="configureEurekaClient">
    /// An optional callback to further configure <see cref="EurekaClientOptions" /> using the selected Cloud Foundry service binding.
    /// </param>
    /// <param name="loggerFactory">
    /// When specified, logs are written here before the service container has been built.
    /// </param>
    /// <returns>
    /// The configured <see cref="IHostApplicationBuilder" /> instance.
    /// </returns>
    public static IHostApplicationBuilder ConfigureEurekaOnCloudFoundry(this IHostApplicationBuilder builder,
        Func<CloudFoundryServicesOptions, CloudFoundryService> selectServiceBinding,
        Action<EurekaClientOptions, CloudFoundryService>? configureEurekaClient = null, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        AssertEurekaIsConfigured(builder.Services);

        builder.AddCloudFoundryConfiguration(loggerFactory ?? NullLoggerFactory.Instance);

        builder.Services.AddOptions<EurekaClientOptions>().Configure<IServiceProvider>((eurekaClientOptions, serviceProvider) =>
            ConfigureEurekaClientOptionsFromSelector(serviceProvider, eurekaClientOptions, builder, selectServiceBinding, configureEurekaClient));

        return builder;
    }

    private static void ConfigureEurekaClientOptionsFromSelector(IServiceProvider serviceProvider, EurekaClientOptions options, IHostApplicationBuilder builder,
        Func<CloudFoundryServicesOptions, CloudFoundryService> selectServiceBinding, Action<EurekaClientOptions, CloudFoundryService>? configureEurekaClient)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger(typeof(EurekaCloudFoundryServiceDiscoveryExtensions).FullName!);

        builder.ApplyCloudFoundryServiceBindingFromSelector(selectServiceBinding,
            service => ApplyServiceBinding(service, options, configureEurekaClient, logger), logger, loggerFactory);
    }

    private static void AssertEurekaIsConfigured(IServiceCollection services)
    {
        if (!IsEurekaConfigured(services))
        {
            throw new InvalidOperationException($"Call {nameof(EurekaServiceDiscoveryExtensions.AddEurekaServiceDiscovery)} first.");
        }
    }

    private static bool IsEurekaConfigured(IServiceCollection services)
    {
        return services.Any(descriptor => descriptor.ImplementationType == typeof(EurekaDiscoveryClient));
    }

    private static bool IsEurekaService(CloudFoundryService service)
    {
        return service.Tags.Contains("eureka", StringComparer.OrdinalIgnoreCase);
    }

    private static void ApplyServiceBinding(CloudFoundryService service, EurekaClientOptions options,
        Action<EurekaClientOptions, CloudFoundryService>? configureEurekaClientOptions, ILogger logger)
    {
        UpdateFromServiceBinding(options, service, logger);
        configureEurekaClientOptions?.Invoke(options, service);
    }

    private static void UpdateFromServiceBinding(EurekaClientOptions options, CloudFoundryService service, ILogger logger)
    {
        // There is no official documentation on the credential parameters.

        if (service.Credentials.TryGetValue("uri", out CloudFoundryCredentials? uri) && uri.Value != null)
        {
            options.EurekaServerServiceUrls = uri.Value + "/eureka/";
            LogPropertyAssigned(logger, nameof(options.EurekaServerServiceUrls));
        }

        if (service.Credentials.TryGetValue("client_id", out CloudFoundryCredentials? clientId) && clientId.Value != null)
        {
            options.ClientId = clientId.Value;
            LogPropertyAssigned(logger, nameof(options.ClientId));
        }

        if (service.Credentials.TryGetValue("client_secret", out CloudFoundryCredentials? clientSecret) && clientSecret.Value != null)
        {
            options.ClientSecret = clientSecret.Value;
            LogPropertyAssigned(logger, nameof(options.ClientSecret));
        }

        if (service.Credentials.TryGetValue("access_token_uri", out CloudFoundryCredentials? accessTokenUri) && accessTokenUri.Value != null)
        {
            options.AccessTokenUri = accessTokenUri.Value;
            LogPropertyAssigned(logger, nameof(options.AccessTokenUri));
        }
    }

    [LoggerMessage(LogLevel.Trace, "Assigned '{PropertyName}' in connection string from service binding.")]
    private static partial void LogPropertyAssigned(ILogger logger, string propertyName);
}
