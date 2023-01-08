namespace Recyclarr.TrashLib.Config.Yaml;

public class UnknownYamlException : Exception
{
    public UnknownYamlException(string msg)
        : base(msg)
    {
    }
}
