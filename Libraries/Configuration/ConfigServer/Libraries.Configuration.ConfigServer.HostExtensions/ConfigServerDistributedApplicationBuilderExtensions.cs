using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Libraries.Configuration.ConfigServer.HostExtensions;

// @formatter:wrap_chained_method_calls chop_always

public static class ConfigServerDistributedApplicationBuilderExtensions
{
    /// <summary>
    /// Adds a Spring Cloud Config Server resource to the distributed application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IDistributedApplicationBuilder" /> to add the resource to.
    /// </param>
    /// <param name="name">
    /// The name of the resource.
    /// </param>
    /// <param name="localConfigDirectory">
    /// Optional path to a local directory containing configuration files. When provided, the Config Server will use the native profile
    /// and bind mount this directory to /config. Mutually exclusive with <paramref name="gitUri" />.
    /// </param>
    /// <param name="gitUri">
    /// Optional Git repository URI for the configuration source (e.g., "https://github.com/myorg/myrepo.git").
    /// When provided, the Config Server will use this Git repository.
    /// Mutually exclusive with <paramref name="localConfigDirectory" />.
    /// </param>
    /// <returns>
    /// The configured <see cref="IResourceBuilder{ConfigServerResource}" /> instance.
    /// </returns>
    /// <remarks>
    /// If neither <paramref name="localConfigDirectory" /> nor
    /// <paramref name="gitUri" /> is provided, defaults to "https://github.com/spring-cloud-samples/config-repo".
    ///  </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when both <paramref name="localConfigDirectory" /> and <paramref name="gitUri" /> are provided.
    /// </exception>
    public static IResourceBuilder<ConfigServerResource> AddConfigServer(this IDistributedApplicationBuilder builder, [ResourceName] string name, string? localConfigDirectory = null, string? gitUri = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        if (!string.IsNullOrWhiteSpace(localConfigDirectory) && !string.IsNullOrWhiteSpace(gitUri))
        {
            throw new ArgumentException($"Cannot specify both {nameof(localConfigDirectory)} and {nameof(gitUri)}. Only one configuration source can be used at a time.");
        }

        var configServer = new ConfigServerResource(name);

        Stopwatch stopwatch = new();

        builder.Eventing.Subscribe<BeforeResourceStartedEvent>(configServer, (_, _) =>
        {
            stopwatch.Start();
            return Task.CompletedTask;
        });

        builder.Eventing.Subscribe<ResourceReadyEvent>(configServer, (resourceReadyEvent, _) =>
        {
            stopwatch.Stop();

            var loggerFactory = resourceReadyEvent.Services.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger("AppHost");

            logger.LogInformation("ConfigServer startup time for tag {Tag}: {Duration}", ConfigServerContainerImageTags.Tag, stopwatch.Elapsed);
            return Task.CompletedTask;
        });

        var resourceBuilder = builder.AddResource(configServer)
            .WithEndpoint(8888, 8888, "http")
            .WithImage(ConfigServerContainerImageTags.Image, ConfigServerContainerImageTags.Tag)
            .WithImageRegistry(ConfigServerContainerImageTags.Registry)
            .WithHttpHealthCheck("http://localhost:8888/actuator/health");

        if (!string.IsNullOrWhiteSpace(localConfigDirectory))
        {
            resourceBuilder = resourceBuilder
                .WithBindMount(localConfigDirectory, "/config")
                .WithEnvironment("spring.profiles.active", "native")
                .WithEnvironment("spring.cloud.config.server.native.searchLocations", "file:/config");
        }
        else if (!string.IsNullOrWhiteSpace(gitUri))
        {
            resourceBuilder = resourceBuilder
                .WithEnvironment("spring.cloud.config.server.git.uri", gitUri);
        }

        return resourceBuilder
            .WithUrl("http://localhost:8888/application/default", "Default Config");
    }
}
