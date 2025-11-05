namespace Libraries.Actuators.HostExtensions;

public static class ActuatorDistributedApplicationBuilderExtensions
{
    /// <summary>
    /// Configures Actuator endpoints and Spring Boot Admin client integration for a project resource.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IResourceBuilder{ProjectResource}" /> to configure.
    /// </param>
    /// <param name="hostnameForSpringBootAdmin">
    /// The hostname to use for Spring Boot Admin client registration. Defaults to "host.docker.internal".
    /// </param>
    /// <returns>
    /// The configured <see cref="IResourceBuilder{ProjectResource}" /> instance.
    /// </returns>
    public static IResourceBuilder<ProjectResource> WithActuators(this IResourceBuilder<ProjectResource> builder,
        string? hostnameForSpringBootAdmin = "host.docker.internal")
    {
        builder.WithEnvironment("MANAGEMENT__ENDPOINTS__ACTUATOR__EXPOSURE__INCLUDE__0", "*");
        builder.WithEnvironment("MANAGEMENT__ENDPOINTS__HEALTH__SHOWCOMPONENTS", "Always");
        builder.WithEnvironment("MANAGEMENT__ENDPOINTS__HEALTH__SHOWDETAILS", "Always");
        builder.WithEnvironment("MANAGEMENT__ENDPOINTS__HEALTH__LIVENESS__ENABLED", "true");
        builder.WithEnvironment("MANAGEMENT__ENDPOINTS__HEALTH__READINESS__ENABLED", "true");

        builder.WithEnvironment("SPRING__BOOT__ADMIN__CLIENT__BASEHOST", hostnameForSpringBootAdmin);
        builder.WithEnvironment("SPRING__BOOT__ADMIN__CLIENT__BASESCHEME", "http"); // SSL certificate is not trusted
        builder.WithEnvironment("SPRING__BOOT__ADMIN__CLIENT__URL", "http://localhost:9099");

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
