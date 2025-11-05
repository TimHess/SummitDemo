using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery;
using Steeltoe.Discovery.Eureka;
using Steeltoe.Discovery.Eureka.Configuration;

namespace Libraries.ServiceDiscovery.Eureka.AppExtensions;

public static partial class EurekaServiceDiscoveryExtensions
{
    private static readonly int DefaultRegistryFetchIntervalSeconds = new EurekaClientOptions().RegistryFetchIntervalSeconds;
    private static readonly TimeSpan DefaultDiscoveryRefreshPeriod = new ServiceDiscoveryOptions().RefreshPeriod;

    private static readonly Action<ServiceDiscoveryOptions> EmptyConfigureDiscovery = _ =>
    {
    };

    /// <summary>
    /// Activates service registration and/or discovery using Netflix Eureka.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <param name="register">
    /// Indicates whether to register the running app as a service instance in Eureka.
    /// </param>
    /// <param name="discover">
    /// Indicates whether to use Eureka for discovery services. When set to <see langword="true" />, registered apps are periodically fetched from the Eureka
    /// server.
    /// </param>
    /// <param name="configureDiscovery">
    /// The delegate used to configure service discovery options.
    /// </param>
    /// <param name="configureEurekaClient">
    /// The delegate used to configure connectivity to the Eureka server.
    /// </param>
    /// <param name="configureEurekaInstance">
    /// The delegate used to configure how to register the running app in Eureka.
    /// </param>
    /// <returns>
    /// The configured <see cref="IHostApplicationBuilder" /> instance.
    /// </returns>
    public static IHostApplicationBuilder AddEurekaServiceDiscovery(this IHostApplicationBuilder builder, bool register, bool discover,
        Action<ServiceDiscoveryOptions>? configureDiscovery = null, Action<EurekaClientOptions>? configureEurekaClient = null,
        Action<EurekaInstanceOptions>? configureEurekaInstance = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (register || discover)
        {
            builder.Services.AddEurekaDiscoveryClient();
            SuppressEurekaHttpClientLogging(builder.Services);
            DisableEurekaHttpClientResilience(builder.Services);

            builder.Services.AddOptions<EurekaClientOptions>().Configure<IServiceProvider>((clientOptions, serviceProvider) =>
                ConfigureEurekaClientOptions(serviceProvider, clientOptions, register, discover, configureEurekaClient));

            if (configureEurekaInstance != null)
            {
                builder.Services.AddOptions<EurekaInstanceOptions>().Configure(configureEurekaInstance);
            }
        }

        if (discover)
        {
            builder.Services.AddServiceDiscoveryCore(configureDiscovery ?? EmptyConfigureDiscovery);
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IServiceEndpointProviderFactory, EurekaServiceEndpointProviderFactory>());
        }

        return builder;
    }

    private static void ConfigureEurekaClientOptions(IServiceProvider serviceProvider, EurekaClientOptions clientOptions, bool register, bool discover,
        Action<EurekaClientOptions>? configureEurekaClient)
    {
        ServiceDiscoveryOptions discoveryOptions = serviceProvider.GetRequiredService<IOptionsMonitor<ServiceDiscoveryOptions>>().CurrentValue;
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger(typeof(EurekaServiceDiscoveryExtensions).FullName!);

        clientOptions.ShouldRegisterWithEureka = register;
        clientOptions.ShouldFetchRegistry = discover;

        LogEurekaConfigured(logger, register, discover);

        if (clientOptions.RegistryFetchIntervalSeconds != DefaultRegistryFetchIntervalSeconds &&
            discoveryOptions.RefreshPeriod == DefaultDiscoveryRefreshPeriod)
        {
            // Preserve override from AppHost extension method that makes Eureka more responsive locally.
        }
        else
        {
            clientOptions.RegistryFetchIntervalSeconds = (int)discoveryOptions.RefreshPeriod.TotalSeconds;
        }

        configureEurekaClient?.Invoke(clientOptions);
    }

    private static void SuppressEurekaHttpClientLogging(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddFilter("System.Net.Http.HttpClient.Eureka.ClientHandler", LogLevel.None);
            builder.AddFilter("System.Net.Http.HttpClient.Eureka.LogicalHandler", LogLevel.None);
            builder.AddFilter("System.Net.Http.HttpClient.AccessTokenForEureka.ClientHandler", LogLevel.None);
            builder.AddFilter("System.Net.Http.HttpClient.AccessTokenForEureka.LogicalHandler", LogLevel.None);
        });
    }

    private static void DisableEurekaHttpClientResilience(IServiceCollection services)
    {
        Action<HttpClientFactoryOptions> configure = options =>
            options.HttpMessageHandlerBuilderActions.Add(builder => RemoveResilienceHandler(builder.AdditionalHandlers));

        services.Configure("Eureka", configure);
        services.Configure("AccessTokenForEureka", configure);
    }

    private static void RemoveResilienceHandler(IList<DelegatingHandler> defaultHandlers)
    {
        // Similar to https://learn.microsoft.com/dotnet/api/microsoft.extensions.dependencyinjection.resiliencehttpclientbuilderextensions.removeallresiliencehandlers,
        // but without the need for an IHttpClientBuilder instance.

        for (int index = defaultHandlers.Count - 1; index >= 0; --index)
        {
            if (defaultHandlers[index] is ResilienceHandler)
            {
                defaultHandlers.RemoveAt(index);
            }
        }
    }

    [LoggerMessage(LogLevel.Trace, "Configured Eureka with Register = {Register}, Discover = {Discover}.")]
    private static partial void LogEurekaConfigured(ILogger logger, bool register, bool discover);
}
