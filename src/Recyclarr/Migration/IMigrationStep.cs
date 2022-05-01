using Serilog;

namespace Recyclarr.Migration;

public interface IMigrationStep
{
    int Order { get; }
    string Description { get; }
    bool CheckIfNeeded();
    void Execute(ILogger log);
}
