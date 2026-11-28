using System.Reflection;
using Serilog.Events;

namespace Recyclarr.Server.Hosting;

/// <summary>
/// Process-level options read from the command line (<see cref="ServerCommandLine"/>). They decide
/// which services and middleware the server registers, and whether settings.yml is read at all.
/// </summary>
internal sealed record ServerLaunchOptions
{
    /// <summary>
    /// True when build-time OpenAPI generation runs this entry point. It starts the host, including
    /// hosted services, against a no-op server, and Main cannot exit early because generation
    /// needs a built host. Services that touch disk, the database, or the config directory are
    /// therefore not registered at all. Generation passes its own host arguments, such as
    /// <c>--applicationName</c>.
    /// https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi
    /// </summary>
    public static bool IsDocumentGenerationProcess { get; } =
        Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

    /// <summary>
    /// The API URLs from <c>--urls</c>. They override <c>ASPNETCORE_URLS</c>; null leaves the
    /// environment and settings.yml to decide (<see cref="ListenUrls"/>).
    /// </summary>
    public string? Urls { get; init; }

    /// <summary>
    /// The pid of the CLI that launched this process; the server stops when that CLI does
    /// (ADR-010). Null for standalone invocations, which stop on SIGTERM. Ownership is separate
    /// from <see cref="Mode"/>: <c>recyclarr serve</c> launches a persistent server it still owns.
    /// </summary>
    public int? ParentPid { get; init; }

    public ServerMode Mode { get; init; } = ServerMode.Persistent;

    public LogEventLevel LogLevel { get; init; } = LogEventLevel.Information;

    /// <inheritdoc cref="IsDocumentGenerationProcess"/>
    public bool IsDocumentGeneration { get; init; } = IsDocumentGenerationProcess;
}
