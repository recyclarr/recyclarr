using Recyclarr.Cli.Console.Settings;

namespace Recyclarr.Cli.Processors.Delete;

public interface IDeleteCustomFormatsProcessor
{
    Task Process(IDeleteCustomFormatSettings settings, CancellationToken ct);
}
