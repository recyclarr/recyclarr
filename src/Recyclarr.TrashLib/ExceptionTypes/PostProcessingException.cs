namespace Recyclarr.TrashLib.ExceptionTypes;

public class PostProcessingException : Exception
{
    public PostProcessingException(string? message)
        : base(message)
    {
    }
}
