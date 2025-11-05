using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;

namespace Libraries.Actuators.AppExtensions;

public static class SpringBootAdminExtensions
{
    /// <summary>
    /// Adds the Spring Boot Admin client services to the specified <see cref="IHostApplicationBuilder" />.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IHostApplicationBuilder" /> to configure.
    /// </param>
    /// <returns>
    /// The configured <see cref="IHostApplicationBuilder" /> instance.
    /// </returns>
    public static IHostApplicationBuilder AddSpringBootAdminClient(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSpringBootAdminClient();
        SuppressSpringBootAdminHttpClientLogging(builder.Services);

        return builder;
    }

    /// <summary>
    /// Reduce the logging noise from Spring Boot Admin HTTP Client and Polly retries.
    /// </summary>
    /// <param name="services">
    /// The service collection.
    /// </param>
    /// <returns>
    /// The service collection.
    /// </returns>
    private static void SuppressSpringBootAdminHttpClientLogging(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddFilter("System.Net.Http.HttpClient.SpringBootAdmin.LogicalHandler", LogLevel.None);
            builder.AddFilter("System.Net.Http.HttpClient.SpringBootAdmin.ClientHandler", LogLevel.None);
        });

        DisableSpringBootAdminHttpClientResilience(services);
    }

    private static void DisableSpringBootAdminHttpClientResilience(IServiceCollection services)
    {
        Action<HttpClientFactoryOptions> configure = options =>
            options.HttpMessageHandlerBuilderActions.Add(builder => RemoveResilienceHandler(builder.AdditionalHandlers));

        services.Configure("SpringBootAdmin", configure);
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
