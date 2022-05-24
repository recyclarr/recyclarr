namespace Recyclarr.Migration;

public interface IMigrationExecutor
{
    void PerformAllMigrationSteps();
    void CheckNeededMigrations();
}
