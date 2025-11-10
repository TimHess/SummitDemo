namespace Libraries.Actuators.HostExtensions;

public static class ActuatorDistributedApplicationBuilderExtensions
{
    /// <summary>
    /// Configures Actuator endpoints and Spring Boot Admin client integration for a project resource or Java app.
    /// </summary>
    /// <typeparam name="T">
    /// The resource type.
    /// </typeparam>
    /// <param name="builder">
    /// The <see cref="IResourceBuilder{T}" /> to configure.
    /// </param>
    /// <param name="hostnameForSpringBootAdmin">
    /// The hostname to use for Spring Boot Admin client registration. Defaults to "host.docker.internal".
    /// </param>
    /// <returns>
    /// The configured <see cref="IResourceBuilder{T}" /> instance.
    /// </returns>
    public static IResourceBuilder<T> WithActuators<T>(this IResourceBuilder<T> builder,
        string? hostnameForSpringBootAdmin = "host.docker.internal")
        where T : IResourceWithEnvironment, IResourceWithEndpoints
    {
        if (builder.Resource is ProjectResource)
        {
            builder.WithEnvironment("MANAGEMENT__ENDPOINTS__ACTUATOR__EXPOSURE__INCLUDE__0", "*");
            builder.WithEnvironment("MANAGEMENT__ENDPOINTS__HEALTH__SHOWCOMPONENTS", "Always");
            builder.WithEnvironment("MANAGEMENT__ENDPOINTS__HEALTH__SHOWDETAILS", "Always");
            builder.WithEnvironment("MANAGEMENT__ENDPOINTS__HEALTH__LIVENESS__ENABLED", "true");
            builder.WithEnvironment("MANAGEMENT__ENDPOINTS__HEALTH__READINESS__ENABLED", "true");

            builder.WithEnvironment("SPRING__BOOT__ADMIN__CLIENT__BASEHOST", hostnameForSpringBootAdmin);
            builder.WithEnvironment("SPRING__BOOT__ADMIN__CLIENT__BASESCHEME", "http"); // SSL certificate is not trusted
            builder.WithEnvironment("SPRING__BOOT__ADMIN__CLIENT__URL", "http://localhost:9099");
        }
        else if (builder.Resource.GetType().Name == "JavaAppExecutableResource" || 
                 builder.Resource.GetType().Name == "JavaAppContainerResource")
        {
            // Java app configuration - Spring Boot Admin client settings
            builder.WithEnvironment("MANAGEMENT_ENDPOINTS_WEB_EXPOSURE_INCLUDE", "*");
            builder.WithEnvironment("MANAGEMENT_ENDPOINT_HEALTH_SHOW-COMPONENTS", "always");
            builder.WithEnvironment("MANAGEMENT_ENDPOINT_HEALTH_SHOW-DETAILS", "always");
            builder.WithEnvironment("SPRING_BOOT_ADMIN_CLIENT_URL", "http://localhost:9099");
            var port = builder.Resource.Annotations.OfType<EndpointAnnotation>().First().Port;
            if (port != null)
            {
                builder.WithEnvironment("SPRING_BOOT_ADMIN_CLIENT_INSTANCE_MANAGEMENT-BASE-URL", $"http://{hostnameForSpringBootAdmin}:{port.ToString()}");
            }
        }
        builder.WithUrlForEndpoint("https", _ => new ResourceUrlAnnotation
        {
            Url = "/actuator",
            DisplayText = "List Actuators"
        });

        builder.WithHttpCommand("/actuator/refresh", "Refresh Configuration", commandOptions: new HttpCommandOptions
        {
            IconName = "ArrowSync"
        });

        return builder;
    }
}
