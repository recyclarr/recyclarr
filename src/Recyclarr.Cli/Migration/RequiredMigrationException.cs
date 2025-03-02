namespace Recyclarr.Cli.Migration;

internal class RequiredMigrationException() : Exception("Some REQUIRED migrations did not pass");
