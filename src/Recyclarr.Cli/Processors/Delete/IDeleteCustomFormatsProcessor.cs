using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Processors.Delete;

internal interface IDeleteCustomFormatsProcessor
{
    Task Process(IDeleteCustomFormatSettings settings, CancellationToken ct);
}
