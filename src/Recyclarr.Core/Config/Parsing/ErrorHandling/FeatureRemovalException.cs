namespace Recyclarr.Config.Parsing.ErrorHandling;

public class FeatureRemovalException(string message, string docLink)
    : Exception($"{message} See: {docLink}");
