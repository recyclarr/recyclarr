using System.IO.Abstractions;

namespace Recyclarr.Config.Parsing.ErrorHandling;

public class ConfigParsingException(
    string message,
    int line,
    Exception inner,
    string? contextualMessage = null
) : Exception(message, inner)
{
    public int Line { get; } = line;
    public string? ContextualMessage { get; } = contextualMessage;
    public IFileInfo? FilePath { get; set; }
}
