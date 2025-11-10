namespace Libraries.ServiceDiscovery.Eureka.HostExtensions;

public static class EurekaResourceBuilderExtensions
{
    /// <summary>
    /// Configures the hostname and app name used when this app registers itself with Netflix Eureka.
    /// </summary>
    /// <typeparam name="T">
    /// The resource type.
    /// </typeparam>
    /// <param name="builder">
    /// The resource builder.
    /// </param>
    /// <param name="hostName">
    /// The host name to register this resource with in Eureka. Defaults to "localhost".
    /// </param>
    /// <param name="resourceNameAsAppName"></param>
    /// <returns>
    /// The <see cref="IResourceBuilder{T}" />.
    /// </returns>
    public static IResourceBuilder<T> WithEurekaRegistration<T>(this IResourceBuilder<T> builder, string? hostName = "localhost", bool resourceNameAsAppName = true)
        where T : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Resource is ProjectResource)
        {
            builder.WithEnvironment("Eureka__Instance__HostName", hostName);

            if (resourceNameAsAppName)
            {
                builder.WithEnvironment("Eureka__Instance__AppName", builder.Resource.Name);
            }

            // Make Eureka forget unresponsive instances a bit faster, to work around the limitation
            // that stopping an instance from the Aspire dashboard does not unregister in Eureka.
            builder.WithEnvironment("Eureka__Instance__LeaseRenewalIntervalInSeconds", "3");
            builder.WithEnvironment("Eureka__Instance__LeaseExpirationDurationInSeconds", "9");
        }
        else if (builder.Resource.GetType().Name == "JavaAppExecutableResource" ||
                 builder.Resource.GetType().Name == "JavaAppContainerResource")
        {
            builder.WithEnvironment("Eureka_Instance_HostName", hostName);

            if (resourceNameAsAppName)
            {
                builder.WithEnvironment("Eureka_Instance_AppName", builder.Resource.Name);
            }

            // Make Eureka forget unresponsive instances a bit faster, to work around the limitation
            // that stopping an instance from the Aspire dashboard does not unregister in Eureka.
            builder.WithEnvironment("Eureka_Instance_LeaseRenewalIntervalInSeconds", "3");
            builder.WithEnvironment("Eureka_Instance_LeaseExpirationDurationInSeconds", "9");
        }

        return builder;
    }

    /// <summary>
    /// Replacement for the built-in
    /// <c>
    /// WithReference
    /// </c>
    /// extension method, which configures the specified resource to be discovered from Netflix Eureka (instead of from environment variables).
    /// </summary>
    /// <typeparam name="TDestination">
    /// The destination resource.
    /// </typeparam>
    /// <param name="builder">
    /// The resource where the service discovery information will be injected.
    /// </param>
    /// <param name="source">
    /// The resource to discover using Netflix Eureka.
    /// </param>
    /// <returns>
    /// The <see cref="IResourceBuilder{T}" />.
    /// </returns>
    public static IResourceBuilder<TDestination> WithEurekaReference<TDestination>(this IResourceBuilder<TDestination> builder,
        IResourceBuilder<IResourceWithServiceDiscovery> source)
        where TDestination : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        builder.WithReference(source);

        // Make Eureka forget unresponsive instances a bit faster, to work around the limitation
        // that stopping an instance from the Aspire dashboard does not unregister in Eureka.
        builder.WithEnvironment("Eureka__Client__RegistryFetchIntervalSeconds", "3");

        Action<EnvironmentCallbackContext> callback = RemoveServiceDiscoveryReferenceEnvironmentVariables(source.Resource.Name);
        builder.WithEnvironment(callback);

        return builder;
    }

    private static Action<EnvironmentCallbackContext> RemoveServiceDiscoveryReferenceEnvironmentVariables(string serviceName)
    {
        return context =>
        {
            // Undo the effects from Aspire's ResourceBuilderExtensions.CreateEndpointReferenceEnvironmentPopulationCallback,
            // so that the built-in ConfigurationServiceEndpointProvider is not used to resolve instances for this service.

            foreach (string key in context.EnvironmentVariables.Keys.Where(key => key.StartsWith($"services__{serviceName}__", StringComparison.Ordinal)))
            {
                context.EnvironmentVariables.Remove(key);
            }
        };
    }
}
