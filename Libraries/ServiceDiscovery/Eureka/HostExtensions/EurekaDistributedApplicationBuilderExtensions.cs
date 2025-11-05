using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Libraries.ServiceDiscovery.Eureka.HostExtensions;

// @formatter:wrap_chained_method_calls chop_always

public static class EurekaDistributedApplicationBuilderExtensions
{
    /// <summary>
    /// Adds a Spring CloudNetflix Eureka service discovery resource to the distributed application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IDistributedApplicationBuilder" /> to add the resource to.
    /// </param>
    /// <param name="name">
    /// The name of the resource.
    /// </param>
    /// <returns>
    /// The configured <see cref="IResourceBuilder{EurekaResource}" /> instance.
    /// </returns>
    public static IResourceBuilder<EurekaResource> AddEureka(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var eureka = new EurekaResource(name);

        Stopwatch stopwatch = new();

        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(eureka, (_, _) =>
        {
            stopwatch.Start();
            return Task.CompletedTask;
        });

        builder.Eventing.Subscribe<ResourceReadyEvent>(eureka, (resourceReadyEvent, _) =>
        {
            stopwatch.Stop();

            var loggerFactory = resourceReadyEvent.Services.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger("AppHost");

            logger.LogInformation("Eureka startup time for tag {Tag}: {Duration}", EurekaContainerImageTags.Tag, stopwatch.Elapsed);
            return Task.CompletedTask;
        });

        return builder.AddResource(eureka)
            .WithEndpoint(8761, 8761, "http")
            .WithImage(EurekaContainerImageTags.Image, EurekaContainerImageTags.Tag)
            .WithImageRegistry(EurekaContainerImageTags.Registry)
            .WithHttpHealthCheck("http://localhost:8761")
            .WithUrl("http://localhost:8761/eureka/apps", "Apps JSON");
    }
}
