namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public class YamlIncludeException : Exception
{
    public YamlIncludeException(string? message)
        : base(message)
    {
    }
}
