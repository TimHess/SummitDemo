using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Logging;

namespace Libraries.BootstrapLogger.AppExtensions;

public static class AspireBootstrapLogger
{
    /// <summary>
    /// Creates a <see cref="BootstrapLoggerFactory" /> that writes to the console and the Aspire dashboard. Use this to log before the service container has
    /// been built.
    /// </summary>
    /// <param name="minimumLevel">
    /// The minimum level. Defaults to <see cref="LogLevel.Information" />.
    /// </param>
    /// <param name="configure">
    /// An optional delegate to further configure the <see cref="ILoggingBuilder" />.
    /// </param>
    /// <returns>
    /// The configured <see cref="BootstrapLoggerFactory" /> from which <see cref="ILogger" /> instances can be created.
    /// </returns>
    public static BootstrapLoggerFactory CreateLoggerFactory([DisallowNull] LogLevel? minimumLevel = LogLevel.Information,
        Action<ILoggingBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(minimumLevel);

        return BootstrapLoggerFactory.CreateEmpty(loggingBuilder =>
        {
            loggingBuilder.SetMinimumLevel(minimumLevel.Value);
            loggingBuilder.AddConsole(consoleOptions => consoleOptions.MaxQueueLength = 1);

            loggingBuilder.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            configure?.Invoke(loggingBuilder);
        });
    }
}
