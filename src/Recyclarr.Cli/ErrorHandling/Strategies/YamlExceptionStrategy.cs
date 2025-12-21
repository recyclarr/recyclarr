using YamlDotNet.Core;

namespace Recyclarr.Cli.ErrorHandling.Strategies;

internal class YamlExceptionStrategy : IExceptionStrategy
{
    public Task<IReadOnlyList<string>?> HandleAsync(Exception exception)
    {
        if (exception is not YamlException e)
        {
            return Task.FromResult<IReadOnlyList<string>?>(null);
        }

        var message = e.Data["ContextualMessage"] is string context
            ? $"YAML parse error at line {e.Start.Line}: {context}"
            : $"YAML parse error at line {e.Start.Line}";
        return Task.FromResult<IReadOnlyList<string>?>([message]);
    }
}
