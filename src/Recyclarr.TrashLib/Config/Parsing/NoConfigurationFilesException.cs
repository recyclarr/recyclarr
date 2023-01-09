namespace Recyclarr.TrashLib.Config.Parsing;

public class NoConfigurationFilesException : Exception
{
    public NoConfigurationFilesException()
        : base("No configuration YAML files found")
    {
    }
}
