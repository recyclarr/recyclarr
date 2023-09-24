namespace Recyclarr.Config.Parsing.ErrorHandling;

public class PostProcessingException : Exception
{
    public PostProcessingException(string? message)
        : base(message)
    {
    }
}
