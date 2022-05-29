namespace Recyclarr.Migration;

public interface IMigrationExecutor
{
    void PerformAllMigrationSteps(bool withDiagnostics);
    void CheckNeededMigrations();
}
