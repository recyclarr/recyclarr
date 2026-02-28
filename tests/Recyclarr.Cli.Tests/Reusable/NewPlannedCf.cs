using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Tests.Reusable;

// Separate from NewCf (Core.TestLibrary) because PlannedCustomFormat and PlannedCfScore
// are Cli types. See REC-90 for consolidation when Cli/Core boundary is revisited.
internal static class NewPlannedCf
{
    public static PlannedCustomFormat Planned(string name, string trashId, int serviceId = 0)
    {
        return new PlannedCustomFormat(
            new CustomFormatResource
            {
                Name = name,
                TrashId = trashId,
                Id = serviceId,
            }
        );
    }

    public static PlannedCfScore Score(string trashId, int serviceId, int score)
    {
        return new PlannedCfScore(Planned("", trashId, serviceId), score);
    }

    public static PlannedCfScore Score(string name, string trashId, int serviceId, int score)
    {
        return new PlannedCfScore(Planned(name, trashId, serviceId), score);
    }
}
