using System.IO.Abstractions;

namespace Recyclarr.Config.Parsing.ErrorHandling;

public class ConfigParsingException(string message, int line, Exception inner)
    : Exception(message, inner)
{
    public int Line { get; } = line;
    public IFileInfo? FilePath { get; set; }
}
