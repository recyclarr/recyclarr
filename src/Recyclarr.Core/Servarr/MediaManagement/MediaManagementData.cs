using Recyclarr.Config.Models;

namespace Recyclarr.Servarr.MediaManagement;

public record MediaManagementData
{
    public int Id { get; init; }
    public PropersAndRepacksMode? PropersAndRepacks { get; init; }

    public IReadOnlyCollection<string> GetDifferences(MediaManagementData other)
    {
        List<string> differences = [];

        if (PropersAndRepacks != other.PropersAndRepacks)
        {
            differences.Add(
                $"DownloadPropersAndRepacks: {PropersAndRepacks} -> {other.PropersAndRepacks}"
            );
        }

        return differences;
    }
}
