using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Net;
using Recyclarr.Cli.Server;
using Recyclarr.Platform;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Commands;

/// <summary>
/// Writes starter configuration files into the local configuration directory. Templates come from
/// the server, which generates them from guide data; the server never writes configuration itself
/// because its configuration directory may be read-only.
/// </summary>
[Description("Create a starter configuration file.")]
internal class ConfigCreateCommand(
    ServerConnectionFactory connections,
    ConfigDirectoryLocator configDirectory,
    IFileSystem fs,
    IAnsiConsole console,
    ILogger log
) : AsyncCommand<ConfigCreateCommand.CliSettings>
{
    [SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays",
        Justification = "Spectre.Console requires it"
    )]
    internal class CliSettings : BaseCommandSettings
    {
        [CommandOption("-p|--path")]
        [Description(
            "File to write instead of the default location. Allowed with at most one "
                + "`--template`."
        )]
        public string? Path { get; init; }

        [CommandOption("-t|--template")]
        [Description(
            "One or more templates to create in the `configs` directory. Use "
                + "`config list templates` to get a list of names accepted here."
        )]
        public string[] Templates { get; init; } = [];

        [CommandOption("-f|--force")]
        [Description("Replace any existing configuration file, if present.")]
        public bool Force { get; init; }

        public override ValidationResult Validate()
        {
            return Path is not null && Templates.Length > 1
                ? ValidationResult.Error(
                    "Cannot use --path with more than one --template; one path holds one file."
                )
                : ValidationResult.Success();
        }
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        CliSettings settings,
        CancellationToken ct
    )
    {
        var explicitFile = settings.Path is null ? null : fs.FileInfo.New(settings.Path);

        if (settings.Templates.Length == 0)
        {
            var file = explicitFile ?? configDirectory.Locate().File("recyclarr.yml");
            return (int)(
                Write(file, StarterConfig.Yaml, settings.Force)
                    ? ExitStatus.Succeeded
                    : ExitStatus.Failed
            );
        }

        await using var connection = await connections.ConnectAsync(ct);
        var configs = configDirectory.Locate().SubDirectory("configs");
        var succeeded = true;

        foreach (var id in settings.Templates)
        {
            var response = await connection.ConfigTemplates.ConfigTemplatesGet(id, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                ReportError(
                    $"No template has the id '{id}'. "
                        + "Run `recyclarr config list templates` to see the available templates."
                );
                succeeded = false;
                continue;
            }

            var yaml = response.ContentOrThrow();
            var file = explicitFile ?? configs.File($"{id}.yml");
            succeeded &= Write(file, yaml, settings.Force);
        }

        return (int)(succeeded ? ExitStatus.Succeeded : ExitStatus.Failed);
    }

    private bool Write(IFileInfo file, string content, bool force)
    {
        if (file.Exists && !force)
        {
            ReportError(
                $"File already exists: {file.FullName}. "
                    + "Use -f/--force to replace it, or choose a different path."
            );
            return false;
        }

        var replaced = file.Exists;
        file.Directory?.Create();
        fs.File.WriteAllText(file.FullName, content);

        var message = $"{(replaced ? "Replaced" : "Created")}: {file.FullName}";
        console.MarkupLineInterpolated($"[green]{message}[/]");
        log.Information(message);
        return true;
    }

    private void ReportError(string message)
    {
        console.MarkupLineInterpolated($"[red]Error:[/] {message}");
        log.Error(message);
    }
}
