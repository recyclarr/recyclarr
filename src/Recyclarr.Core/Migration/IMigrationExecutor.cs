namespace Recyclarr.Migration;

/// <summary>
/// Runs every migration step in order. A failing step throws <see cref="MigrationException"/>,
/// which stops server startup.
/// </summary>
public interface IMigrationExecutor
{
    void RunAll();
}
