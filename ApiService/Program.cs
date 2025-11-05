using Libraries.BootstrapLogger.AppExtensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Logging;
using Steeltoe.Configuration.ConfigServer;
using SummitDemo.ApiService;
using SummitDemo.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

using (BootstrapLoggerFactory loggerFactory = AspireBootstrapLogger.CreateLoggerFactory(LogLevel.Debug))
{
    builder.AddConfigServer(loggerFactory);
}

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.Services.AddOptions<WeatherOptions>().BindConfiguration("WeatherOptions");

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
