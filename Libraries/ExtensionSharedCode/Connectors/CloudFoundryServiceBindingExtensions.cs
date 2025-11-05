using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Configuration.CloudFoundry;

namespace Libraries.ExtensionSharedCode;

/// <summary>
/// Provides driver-independent methods to read and apply VCAP_SERVICES credentials to a connection string.
/// </summary>
internal static partial class CloudFoundryServiceBindingExtensions
{
    public static void ApplyCloudFoundryServiceBindingFromName(this IHostApplicationBuilder builder, string? serviceBindingName,
        Func<CloudFoundryService, bool> selectCompatibleService, Action<CloudFoundryService> applyServiceBinding, ILogger logger, ILoggerFactory loggerFactory)
    {
        builder.Configuration.AddCloudFoundry(null, loggerFactory);

        if (Platform.IsCloudFoundry)
        {
            AssertHasVcapServices(true, builder.Environment, logger);

            CloudFoundryServicesOptions options = LoadServiceBindings(builder);

            CloudFoundryService service = serviceBindingName != null
                ? SelectNamedService(options, serviceBindingName)
                : SelectSingleCompatibleService(options, selectCompatibleService);

            LogSelectedServiceBindingName(logger, service.Name);
            applyServiceBinding(service);
        }
    }

    public static void ApplyCloudFoundryServiceBindingFromSelector(this IHostApplicationBuilder builder,
        Func<CloudFoundryServicesOptions, CloudFoundryService> selectServiceBinding, Action<CloudFoundryService> applyServiceBinding, ILogger logger,
        ILoggerFactory loggerFactory)
    {
        builder.Configuration.AddCloudFoundry(null, loggerFactory);

        if (Platform.IsCloudFoundry)
        {
            AssertHasVcapServices(false, builder.Environment, logger);

            CloudFoundryServicesOptions options = LoadServiceBindings(builder);
            CloudFoundryService? service = selectServiceBinding(options);

            if (service == null)
            {
                throw new InvalidOperationException("Service binding is unavailable.");
            }

            LogSelectedServiceBindingName(logger, service.Name);
            applyServiceBinding(service);
        }
    }

    private static void AssertHasVcapServices(bool isRequired, IHostEnvironment environment, ILogger logger)
    {
        string? vcapServices = Environment.GetEnvironmentVariable("VCAP_SERVICES");

        if (string.IsNullOrEmpty(vcapServices) || vcapServices == "{}")
        {
            if (isRequired)
            {
                throw new InvalidOperationException(
                    "The VCAP_SERVICES environment variable is unavailable or empty. Bind the dependent services to your app first.");
            }
        }
        else
        {
            LogVcapServices(logger, environment.IsDevelopment() ? vcapServices : "[REDACTED]");
        }
    }

    private static CloudFoundryServicesOptions LoadServiceBindings(IHostApplicationBuilder builder)
    {
        var options = new CloudFoundryServicesOptions();
        builder.Configuration.GetSection("vcap").Bind(options);
        return options;
    }

    private static CloudFoundryService SelectNamedService(CloudFoundryServicesOptions options, string serviceBindingName)
    {
        CloudFoundryService? service = options.Services.SelectMany(pair => pair.Value).SingleOrDefault(binding => binding.Name == serviceBindingName);

        if (service == null)
        {
            throw new InvalidOperationException($"Service binding '{serviceBindingName}' not found in VCAP_SERVICES.");
        }

        return service;
    }

    private static CloudFoundryService SelectSingleCompatibleService(CloudFoundryServicesOptions options, Func<CloudFoundryService, bool> predicate)
    {
        List<CloudFoundryService> candidates = options.Services.SelectMany(pair => pair.Value).Where(predicate).ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No compatible service binding found in VCAP_SERVICES.");
        }

        if (candidates.Count > 1)
        {
            throw new InvalidOperationException("Multiple compatible service bindings found in VCAP_SERVICES. Please specify the service binding name.");
        }

        return candidates[0];
    }

    [LoggerMessage(LogLevel.Debug, "Found VCAP_SERVICES environment variable with contents: {Contents}")]
    private static partial void LogVcapServices(ILogger logger, string contents);

    [LoggerMessage(LogLevel.Information, "Using service binding named '{ServiceName}' from VCAP_SERVICES.")]
    private static partial void LogSelectedServiceBindingName(ILogger logger, string? serviceName);
}
