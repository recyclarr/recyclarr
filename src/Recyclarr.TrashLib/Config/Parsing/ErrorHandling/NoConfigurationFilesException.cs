namespace Recyclarr.TrashLib.Config.Parsing.ErrorHandling;

public class NoConfigurationFilesException : Exception
{
    public NoConfigurationFilesException()
        : base("No configuration YAML files found")
    {
    }
}
