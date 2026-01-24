namespace Recyclarr.ServarrApi.MediaManagement;

public static class MediaManagementDtoExtensions
{
    public static IReadOnlyCollection<string> GetDifferences(
        this MediaManagementDto oldDto,
        MediaManagementDto newDto
    )
    {
        var differences = new List<string>();

        if (oldDto.DownloadPropersAndRepacks != newDto.DownloadPropersAndRepacks)
        {
            differences.Add(
                $"DownloadPropersAndRepacks: {oldDto.DownloadPropersAndRepacks} -> {newDto.DownloadPropersAndRepacks}"
            );
        }

        return differences;
    }
}
