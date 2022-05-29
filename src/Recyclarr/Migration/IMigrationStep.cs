using CliFx.Infrastructure;

namespace Recyclarr.Migration;

public interface IMigrationStep
{
    int Order { get; }
    string Description { get; }
    IReadOnlyCollection<string> Remediation { get; }
    bool Required { get; }
    bool CheckIfNeeded();
    void Execute(IConsole? console);
}
