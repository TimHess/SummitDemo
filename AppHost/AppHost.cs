using Libraries.Actuators.HostExtensions;
using Libraries.Configuration.ConfigServer.HostExtensions;
using Libraries.ServiceDiscovery.Eureka.HostExtensions;

var builder = DistributedApplication.CreateBuilder(args);

// Spin up a Steeltoe-dev Config Server container
var configServer = builder.AddConfigServer("config-server", "../Configuration")
    .WithLifetime(ContainerLifetime.Persistent);

// Spin up a Steeltoe-dev Eureka Server container
var eureka = builder.AddEureka("eureka")
    .WithLifetime(ContainerLifetime.Persistent);

// Spin up a Steeltoe-dev Spring Boot Admin Server container
var springBootAdmin = builder.AddSpringBootAdmin("sba");

// .NET Weather Service
var apiService = builder.AddProject<Projects.SummitDemo_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithEurekaRegistration(resourceNameAsAppName: false)
    .WaitFor(configServer)
    .WaitFor(eureka)
    .WaitFor(springBootAdmin)
    .WithActuators();

// Spring Weather Service
var springApp = builder.AddSpringApp("springapp",
        workingDirectory: "../SpringApiService",
        new JavaAppExecutableResourceOptions
        {
            ApplicationName = "target/SpringApiService-0.0.1-SNAPSHOT.jar",
            Port = 8081,
            OtelAgentPath = "../agents",
        })
    .WithMavenBuild()
    .PublishAsDockerFile(c =>
    {
        c.WithBuildArg("JAR_NAME", "SpringApiService-0.0.1-SNAPSHOT.jar")
            .WithBuildArg("AGENT_PATH", "/agents")
            .WithBuildArg("SERVER_PORT", "8081");
    })
    .WithActuators()
    .WaitFor(configServer)
    .WaitFor(eureka)
    .WaitFor(springBootAdmin)
    .WithUrlForEndpoint("http", endpoint => endpoint.Url += "/weatherforecast");

builder.AddProject<Projects.SummitDemo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithEurekaReference(apiService)
    .WithEurekaReference(springApp)
    .WithActuators();

builder.Build().Run();
