namespace Recyclarr.Cli.Migration;

public class RequiredMigrationException : Exception
{
    public RequiredMigrationException()
        : base("Some REQUIRED migrations did not pass")
    {
    }
}
