using Recyclarr.Cli.Console.Commands;
using Recyclarr.Platform;
using Spectre.Console;

namespace Recyclarr.Cli.Logging;

internal class RecyclarrConsoleSettings(IEnvironment env, IAnsiConsole console, ILogger log)
{
    public ConsoleOutputSettings GetOutputSettings(BaseCommandSettings settings)
    {
        var rawConfig = new
        {
            RawOption = settings.Raw,
            NoColor = !string.IsNullOrEmpty(env.GetEnvironmentVariable("NO_COLOR")),
            console.Profile.Out.IsTerminal,
        };

        log.Debug("Console Output Settings {@Settings}", rawConfig);
        return new ConsoleOutputSettings
        {
            IsRawOutputEnabled =
                rawConfig.RawOption ?? (rawConfig.NoColor || !rawConfig.IsTerminal),
        };
    }
}

internal record ConsoleOutputSettings : IConsoleOutputSettings
{
    public required bool IsRawOutputEnabled { get; init; }
}
