namespace Recyclarr.Cli.Processors;

internal class FatalException(string? message, Exception? innerException = null)
    : Exception(message, innerException);
