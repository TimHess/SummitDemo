using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using Steeltoe.Discovery.Eureka;

namespace Libraries.ServiceDiscovery.Eureka.AppExtensions;

public static class EurekaServiceCollectionExtensions
{
    /// <summary>
    /// Configures a service discovery endpoint provider which uses Netflix Eureka to resolve endpoints.
    /// </summary>
    /// <param name="services">
    /// The service collection.
    /// </param>
    /// <returns>
    /// The service collection.
    /// </returns>
    public static IServiceCollection AddEurekaServiceEndpointProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEurekaDiscoveryClient();

        services.AddServiceDiscoveryCore();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IServiceEndpointProviderFactory, EurekaServiceEndpointProviderFactory>());

        SuppressEurekaHttpClientLogging(services);
        DisableEurekaHttpClientResilience(services);

        return services;
    }

    private static void SuppressEurekaHttpClientLogging(this IServiceCollection services)
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
}
