using System.Reflection;
using Serilog.Events;

namespace Recyclarr.Server.Hosting;

/// <summary>
/// Process-level options read from the command line and host configuration before settings.yml
/// loads. They decide which services and middleware the server registers.
/// </summary>
internal sealed record ServerLaunchOptions
{
    /// <summary>
    /// The pid of the CLI that launched this process; the server stops when that CLI does
    /// (ADR-010). Null for standalone invocations, which stop on SIGTERM. Ownership is separate
    /// from <see cref="Mode"/>: <c>recyclarr serve</c> launches a persistent server it still owns.
    /// </summary>
    public required int? ParentPid { get; init; }

    public required ServerMode Mode { get; init; }

    public required LogEventLevel LogLevel { get; init; }

    /// <summary>
    /// True when build-time OpenAPI generation runs this entry point. It starts the host, including
    /// hosted services, against a no-op server, and Main cannot exit early because generation
    /// needs a built host. Services that touch disk, the database, or the config directory are
    /// therefore not registered at all.
    /// https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi
    /// </summary>
    public required bool IsDocumentGeneration { get; init; }

    public static ServerLaunchOptions From(IConfiguration configuration)
    {
        return new ServerLaunchOptions
        {
            ParentPid = int.TryParse(configuration["parent-pid"], out var pid) ? pid : null,
            Mode = configuration.GetValue<bool>("ephemeral")
                ? ServerMode.Ephemeral
                : ServerMode.Persistent,
            LogLevel = Enum.TryParse<LogEventLevel>(
                configuration["log-level"],
                ignoreCase: true,
                out var level
            )
                ? level
                : LogEventLevel.Information,
            IsDocumentGeneration =
                Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider",
        };
    }
}
