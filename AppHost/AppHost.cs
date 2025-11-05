using Libraries.Actuators.HostExtensions;
using Libraries.Configuration.ConfigServer.HostExtensions;
using Libraries.ServiceDiscovery.Eureka.HostExtensions;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ConfigServerResource> configServer = builder.AddConfigServer("config-server", "../Configuration")
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<EurekaResource> eureka = builder.AddEureka("eureka");

IResourceBuilder<SpringBootAdminResource> springBootAdmin = builder.AddSpringBootAdmin("sba");

var apiService = builder.AddProject<Projects.SummitDemo_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithEurekaRegistration()
    .WaitFor(configServer)
    .WaitFor(eureka)
    .WaitFor(springBootAdmin)
    .WithActuators();

var springApp = builder.AddSpringApp("springapp",
        workingDirectory: "../SpringApiService",
        new JavaAppExecutableResourceOptions
        {
            ApplicationName = "target/SpringApiService-0.0.1-SNAPSHOT.jar",
            Port = 8085,
            OtelAgentPath = "../agents",
        })
    .WithMavenBuild()
    .PublishAsDockerFile(c =>
    {
        c.WithBuildArg("JAR_NAME", "SpringApiService-0.0.1-SNAPSHOT.jar")
            .WithBuildArg("AGENT_PATH", "/agents")
            .WithBuildArg("SERVER_PORT", "8085");
    })
    .WaitFor(configServer)
    .WaitFor(eureka)
    .WaitFor(springBootAdmin);

builder.AddProject<Projects.SummitDemo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WithReference(springApp)
    .WaitFor(apiService)
    .WaitFor(springApp)
    .WithActuators();

builder.Build().Run();
