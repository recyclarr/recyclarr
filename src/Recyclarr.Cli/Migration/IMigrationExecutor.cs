namespace Recyclarr.Cli.Migration;

public interface IMigrationExecutor
{
    void PerformAllMigrationSteps(bool withDiagnostics);
    void CheckNeededMigrations();
}
