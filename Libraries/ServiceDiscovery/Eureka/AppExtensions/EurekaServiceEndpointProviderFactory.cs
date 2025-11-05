using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka;

namespace Libraries.ServiceDiscovery.Eureka.AppExtensions;

public sealed class EurekaServiceEndpointProviderFactory(
    IServiceProvider serviceProvider, IOptionsMonitor<ServiceDiscoveryOptions> optionsMonitor, ILoggerFactory loggerFactory) : IServiceEndpointProviderFactory
{
    /// <summary>
    /// Attempts to create an Eureka-based service endpoint provider for the specified query.
    /// </summary>
    /// <param name="query">
    /// The service endpoint query to resolve.
    /// </param>
    /// <param name="provider">
    /// When this method returns, contains the <see cref="IServiceEndpointProvider" /> instance if one was created,
    /// or <see langword="null" /> if no provider could be created.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if a provider was created; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryCreateProvider(ServiceEndpointQuery query, [NotNullWhen(true)] out IServiceEndpointProvider? provider)
    {
        // TODO: Participate in change tracking?

        // CAUTION: We can't inject IEnumerable<IDiscoveryClient>, because it leads to infinite recursion.
        // This factory is constructed from Aspire's IHttpClientBuilder.AddServiceDiscovery() extension method, which hooks up
        // a HttpMessageHandler that results in creation of EurekaDiscoveryClient, which immediately needs an HttpClient, which triggers
        // creation of the handler chain. Therefore, we postpone taking a dependency on EurekaDiscoveryClient until we actually need it.
        EurekaDiscoveryClient? discoveryClient = serviceProvider.GetServices<IDiscoveryClient>().OfType<EurekaDiscoveryClient>().FirstOrDefault();

        if (discoveryClient != null)
        {
            ILogger<EurekaServiceEndpointProvider> logger = loggerFactory.CreateLogger<EurekaServiceEndpointProvider>();
            provider = new EurekaServiceEndpointProvider(query, discoveryClient, optionsMonitor, logger);
            return true;
        }

        provider = null;
        return false;
    }
}
