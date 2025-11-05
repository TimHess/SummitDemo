# SummitDemo

A demo application created for [.NET Foundation Summit 2025](https://dotnetfoundation.org/) to showcase [Steeltoe](https://steeltoe.io/) in the context of [.NET Aspire](https://dotnet.microsoft.com/aspire).

This application demonstrates how Steeltoe can integrate with Aspire to provide enterprise-grade microservices capabilities, including configuration management, service discovery, and observability.

## Overview

SummitDemo is a distributed application that consists of:

- **Web Frontend** - A Blazor Server application that displays weather forecasts
- **ApiService** - A .NET Web API service that provides weather forecast data
- **SpringApiService** - A Spring Boot application that also provides weather data, demonstrating polyglot microservices
- **Config Server** - Spring Cloud Config Server for centralized configuration management
- **Eureka** - Netflix Eureka service discovery server
- **Spring Boot Admin** - Management UI for monitoring and managing Spring Boot and Steeltoe applications

## Notable Features

This demo showcases several features beyond standard Aspire templates:

### Configuration Management

- **Spring Cloud Config Server** - Centralized configuration management using Steeltoe's Config Server integration
- Configuration is stored in YAML files in the `Configuration/` directory
- Both .NET and Spring Boot applications can consume configuration from the same Config Server

### Service Discovery

- **Eureka Integration** - Service discovery using Netflix Eureka
- Both .NET and Spring Boot services register with and discover services through Eureka
- Aspire extensions for Eureka resource management

### Observability & Management

- **Actuators** - management endpoints exposed on all services
- **Spring Boot Admin** - Web-based UI for monitoring and managing applications
  - Displays health, metrics, logs, and environment information
  - Supports both Spring Boot and Steeltoe applications
  - Accessible at `http://localhost:9099` when running

### Polyglot Microservices

- **Java/Spring Boot Integration** - Demonstrates running Spring Boot applications alongside .NET services
- Uses [CommunityToolkit.Aspire.Hosting.Java](https://www.nuget.org/packages/CommunityToolkit.Aspire.Hosting.Java) for Java application hosting
- Maven build integration with Aspire
- OpenTelemetry Java agent for observability

### Additional Infrastructure Components

The `Libraries/` directory contains prototypes for extensions that use Steeltoe components with Aspire:

- **Actuators** - Actuator endpoint configuration and Spring Boot Admin integration
- **Configuration** - Config Server integration
- **Service Discovery** - Eureka

## Project Structure

```text
SummitDemo/
├── ApiService/              # .NET Weather API
├── Web/                     # Blazor Server frontend
├── AppHost/                 # Aspire orchestration project
├── ServiceDefaults/         # Shared service configuration
├── SpringApiService/        # Spring Boot Weather API
├── Configuration/           # Config Server configuration files
└── Libraries/              # Custom Aspire extensions
    ├── Actuators/          # Actuator and Spring Boot Admin extensions
    ├── Configuration/     # Config Server extensions
    ├── ServiceDiscovery/  # Service discovery providers
```

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for running containerized services)
- [Java 21 JDK](https://adoptium.net/) (for building Spring Boot application)
- [Maven](https://maven.apache.org/) (for building Spring Boot application)

### Running the Application

1. **Clone the repository**

   ```bash
   git clone <repository-url>
   cd SummitDemo
   ```

2. **Run the AppHost project**

   ```bash
   dotnet run --project AppHost --launch-profile "http"
   ```

   This will:
   - Start the Config Server
   - Start Eureka
   - Start Spring Boot Admin
   - Build and start the .NET services
   - Build and start the Spring Boot service
   - Launch the Aspire dashboard

3. **Access the applications**
   - **Aspire Dashboard**: `http://localhost:15000` (or the port shown in the console)
   - **Web Frontend**: `http://localhost:5000` (or the port shown in the console)
   - **API Service**: `http://localhost:5001` (or the port shown in the console)
   - **Spring API Service**: `http://localhost:8085`
   - **Spring Boot Admin**: `http://localhost:9099`
   - **Eureka Dashboard**: `http://localhost:8761`

## Key Technologies

- [.NET Aspire](https://dotnet.microsoft.com/aspire) - Cloud-ready stack for building distributed applications
- [Steeltoe](https://steeltoe.io/) - .NET microservices framework
- [Spring Boot](https://spring.io/projects/spring-boot) - Java application framework
- [Spring Cloud Config](https://spring.io/projects/spring-cloud-config) - Configuration management
- [Spring Cloud Netflix Eureka](https://spring.io/projects/spring-cloud-netflix) - Spring Cloud Netflix Eureka
- [Spring Boot Admin](https://codecentric.github.io/spring-boot-admin/) - Application monitoring
- [OpenTelemetry](https://opentelemetry.io/) - Observability framework

## Learn More

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Steeltoe Documentation](https://steeltoe.io/docs/)
- [Spring Cloud Documentation](https://spring.io/projects/spring-cloud)
- [Spring Boot Admin Documentation](https://codecentric.github.io/spring-boot-admin/current/)

## License

See [LICENSE.txt](LICENSE.txt) for license information.
