namespace Recyclarr.Cli.Console;

public class CommandException : Exception
{
    public CommandException(string? message)
        : base(message)
    {
    }
}
