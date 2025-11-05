//namespace Libraries.Configuration.ConfigServer.HostExtensions;

//public static class ConfigServerResourceBuilderExtensions
//{
//    /// <summary>
//    /// Similar to the built-in
//    /// <see cref="ResourceBuilderExtensions.WithReference{TDestination}(IResourceBuilder{TDestination},IResourceBuilder{IResourceWithServiceDiscovery})" />
//    /// extension method, but configures the resource to use Netflix Eureka for service discovery.
//    /// </summary>
//    /// <typeparam name="TDestination">
//    /// The destination resource.
//    /// </typeparam>
//    /// <param name="builder">
//    /// The resource where the service discovery information will be injected.
//    /// </param>
//    /// <param name="source">
//    /// The resource from which to extract service discovery information.
//    /// </param>
//    /// <returns>
//    /// The <see cref="IResourceBuilder{T}" />.
//    /// </returns>
//    public static IResourceBuilder<TDestination> WithConfigServerReference<TDestination>(this IResourceBuilder<TDestination> builder,
//        IResourceBuilder<IResourceWithServiceDiscovery> source)
//        where TDestination : IResourceWithEnvironment
//    {
//        ArgumentNullException.ThrowIfNull(builder);
//        ArgumentNullException.ThrowIfNull(source);

//        builder.WithReference(source);
//        builder.WithEnvironment("Spring__Cloud__Config__Enabled", "true");

//        return builder;
//    }
//}
