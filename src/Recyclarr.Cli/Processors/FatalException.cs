namespace Recyclarr.Cli.Processors;

public class FatalException(string? message, Exception? innerException = null)
    : Exception(message, innerException);
