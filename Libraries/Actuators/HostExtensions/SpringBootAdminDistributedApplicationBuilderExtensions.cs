namespace Libraries.Actuators.HostExtensions;

// @formatter:wrap_chained_method_calls chop_always

public static class SpringBootAdminDistributedApplicationBuilderExtensions
{
    /// <summary>
    /// Adds a Spring Boot Admin resource to the distributed application.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IDistributedApplicationBuilder" /> to add the resource to.
    /// </param>
    /// <param name="name">
    /// The name of the resource.
    /// </param>
    /// <returns>
    /// The configured <see cref="IResourceBuilder{SpringBootAdminResource}" /> instance.
    /// </returns>
    public static IResourceBuilder<SpringBootAdminResource> AddSpringBootAdmin(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var springBootAdmin = new SpringBootAdminResource(name);

        return builder.AddResource(springBootAdmin)
            .WithHttpEndpoint(9099, isProxied: false)
            .WithHttpHealthCheck("/")
            .WithImage(SpringBootAdminContainerImageTags.Image, SpringBootAdminContainerImageTags.Tag)
            .WithImageRegistry(SpringBootAdminContainerImageTags.Registry);
    }
}
