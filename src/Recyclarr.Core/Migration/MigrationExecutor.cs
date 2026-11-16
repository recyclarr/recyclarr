using Recyclarr.Migration.Steps;

namespace Recyclarr.Migration;

internal class MigrationExecutor(IOrderedEnumerable<IMigrationStep> steps, ILogger log)
    : IMigrationExecutor
{
    public void RunAll()
    {
        foreach (var step in steps)
        {
            try
            {
                step.Run(log);
            }
            catch (Exception e)
            {
                throw new MigrationException(step.Description, step.Remediation, e);
            }
        }
    }
}
