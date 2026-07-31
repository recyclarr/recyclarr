using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Logging;
using YamlDotNet.Core;

namespace Recyclarr.ErrorHandling;

internal class YamlExceptionStrategy : IExceptionStrategy
{
    public Task<HandledInstanceFailure?> HandleAsync(Exception exception)
    {
        HandledInstanceFailure? failure = exception switch
        {
            ConfigParsingException e => new ConfigParsingFailure(
                e.FilePath?.Name,
                e.Line,
                e.Message
            ),
            YamlException ye when ye.FindInnerException<ConfigParsingException>() is { } inner =>
                new YamlErrorFailure((int)ye.Start.Line, inner.Message),
            YamlException ye => new YamlParseFailure((int)ye.Start.Line),
            _ => null,
        };

        return Task.FromResult(failure);
    }
}
