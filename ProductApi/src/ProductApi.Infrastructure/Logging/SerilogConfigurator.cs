using Microsoft.Extensions.Configuration;
using Serilog;

namespace ProductApi.Infrastructure.Logging;

/// <summary>
/// Centralizes Serilog structured-logging setup (console + rolling file sinks),
/// satisfying the "Logging Framework of your choice for structured logging" requirement.
/// </summary>
public static class SerilogConfigurator
{
    public static LoggerConfiguration BuildConfiguration(IConfiguration configuration) =>
        new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/productapi-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}");
}
