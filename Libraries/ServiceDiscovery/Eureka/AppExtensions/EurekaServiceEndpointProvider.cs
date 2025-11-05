using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Eureka;

namespace Libraries.ServiceDiscovery.Eureka.AppExtensions;

internal sealed class EurekaServiceEndpointProvider : IServiceEndpointProvider, IHostNameFeature
{
    private readonly string _serviceName;
    private readonly EurekaDiscoveryClient _discoveryClient;
    private readonly ILogger<EurekaServiceEndpointProvider> _logger;
    private readonly IReadOnlyList<string> _schemes;

    string IHostNameFeature.HostName => _serviceName;

    public EurekaServiceEndpointProvider(ServiceEndpointQuery query, EurekaDiscoveryClient discoveryClient,
        IOptionsMonitor<ServiceDiscoveryOptions> optionsMonitor, ILogger<EurekaServiceEndpointProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(discoveryClient);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceName = query.ServiceName;
        _discoveryClient = discoveryClient;
        _logger = logger;

        ServiceDiscoveryOptions options = optionsMonitor.CurrentValue;
        _schemes = options.ApplyAllowedSchemes(query.IncludedSchemes);
    }

    public async ValueTask PopulateAsync(IServiceEndpointBuilder endpoints, CancellationToken cancellationToken)
    {
        IList<IServiceInstance> eurekaInstances = await _discoveryClient.GetInstancesAsync(_serviceName, cancellationToken);
        List<ServiceEndpoint> eurekaEndpoints = SelectEndpoints(eurekaInstances);

        _logger.LogInformation("Instances for service '{ServiceName}' found in Eureka: {Instances}.", _serviceName,
            string.Join(", ", eurekaEndpoints.Select(serviceEndpoint => serviceEndpoint.EndPoint.ToString())));

        foreach (ServiceEndpoint serviceEndpoint in eurekaEndpoints)
        {
            endpoints.Endpoints.Add(serviceEndpoint);
        }
    }

    private List<ServiceEndpoint> SelectEndpoints(IList<IServiceInstance> instances)
    {
        // Scheme selection is documented at
        // https://learn.microsoft.com/en-us/dotnet/core/extensions/service-discovery?tabs=dotnet-cli#scheme-selection-when-resolving-https-endpoints:
        //
        //  It is common to use HTTP while developing and testing a service locally and HTTPS when the service is deployed. Service Discovery supports
        //  this by allowing for a priority list of URI schemes to be specified in the input string given to Service Discovery. Service Discovery will
        //  attempt to resolve the services for the schemes in order and will stop after an endpoint is found. URI schemes are separated by a +
        //  character, for example: "https+http://basket". Service Discovery will first try to find HTTPS endpoints for the "basket" service and will
        //  then fall back to HTTP endpoints. If any HTTPS endpoint is found, Service Discovery will not include HTTP endpoints.
        //
        //  Schemes can be filtered by configuring the AllowedSchemes and AllowAllSchemes properties on ServiceDiscoveryOptions. The AllowAllSchemes
        //  property is used to indicate that all schemes are allowed. By default, AllowAllSchemes is true and all schemes are allowed. Schemes can be
        //  restricted by setting AllowAllSchemes to false and adding allowed schemes to the AllowedSchemes property.

        List<ServiceEndpoint> endpoints = [];

        if (instances.Count > 0)
        {
            foreach (string scheme in _schemes)
            {
                foreach (IServiceInstance instance in instances)
                {
                    Uri? uri = scheme.ToLowerInvariant() switch
                    {
                        "https" => instance.SecureUri,
                        "http" => instance.NonSecureUri,
                        _ => null
                    };

                    if (uri != null && ServiceEndpoint.TryParse(uri.ToString(), out ServiceEndpoint? serviceEndpoint))
                    {
                        serviceEndpoint.Features.Set<IServiceEndpointProvider>(this);
                        endpoints.Add(serviceEndpoint);
                    }
                }

                if (endpoints.Count > 0)
                {
                    break;
                }
            }
        }

        return endpoints;
    }

    public override string ToString()
    {
        return "Eureka";
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
