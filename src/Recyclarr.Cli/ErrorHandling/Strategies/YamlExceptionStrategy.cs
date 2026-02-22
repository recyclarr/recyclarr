using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Logging;
using YamlDotNet.Core;

namespace Recyclarr.Cli.ErrorHandling.Strategies;

internal class YamlExceptionStrategy : IExceptionStrategy
{
    public Task<IReadOnlyList<string>?> HandleAsync(Exception exception)
    {
        var message = exception switch
        {
            // Unwrapped by SettingsLoader/ConfigParser with file and line context
            ConfigParsingException cpe => FormatConfigParsingMessage(cpe),

            // Raw YamlException (no unwrapping happened); check for layer-1 inner
            YamlException ye when ye.FindInnerException<ConfigParsingException>() is { } inner =>
                $"YAML error at line {(int)ye.Start.Line}: {inner.Message}",

            YamlException ye => $"YAML parse error at line {(int)ye.Start.Line}",

            _ => null,
        };

        return Task.FromResult<IReadOnlyList<string>?>(message is not null ? [message] : null);
    }

    private static string FormatConfigParsingMessage(ConfigParsingException e)
    {
        var file = e.FilePath?.Name;
        var prefix = file is not null ? $"{file} line {e.Line}" : $"Line {e.Line}";
        return $"{prefix}: {e.Message}";
    }
}
