using Libraries.BootstrapLogger.AppExtensions;
using Libraries.ServiceDiscovery.Eureka.AppExtensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Logging;
using Steeltoe.Configuration.ConfigServer;
using Steeltoe.Discovery.Eureka;
using SummitDemo.ApiService;
using SummitDemo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

using (BootstrapLoggerFactory loggerFactory = AspireBootstrapLogger.CreateLoggerFactory(LogLevel.Debug))
{
    // Add Steeltoe's Config Server Client
    builder.AddConfigServer(loggerFactory);
}

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();


// Bind our IOptions to data retrieved from Config Server
builder.Services.AddOptions<WeatherOptions>().BindConfiguration("WeatherOptions");

// add service registration/discovery with Eureka today
// builder.Services.AddEurekaDiscoveryClient();

// PROTOTYPE: Add Steeltoe's Eureka client to Microsoft's Service Discovery
builder.AddEurekaServiceDiscovery(true, false);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet(string.Empty, () => new { weatherForecast="/weatherForecast" });
app.MapGet("/weatherforecast", (IOptionsSnapshot<WeatherOptions> weatherOptions) =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            weatherOptions.Value.Summaries[Random.Shared.Next(weatherOptions.Value.Summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
