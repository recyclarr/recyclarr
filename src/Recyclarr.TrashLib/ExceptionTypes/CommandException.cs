namespace Recyclarr.TrashLib.ExceptionTypes;

public class CommandException : Exception
{
    public CommandException(string? message)
        : base(message)
    {
    }
}
