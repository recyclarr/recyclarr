using Recyclarr.TrashLib.Repo;

namespace Recyclarr.TrashLib.Services.ReleaseProfile.Guide;

public class ReleaseProfileGuideService : IReleaseProfileGuideService
{
    private readonly IRepoMetadataBuilder _metadataBuilder;
    private readonly ReleaseProfileGuideParser _parser;

    public ReleaseProfileGuideService(
        IRepoMetadataBuilder metadataBuilder,
        ReleaseProfileGuideParser parser)
    {
        _metadataBuilder = metadataBuilder;
        _parser = parser;
    }

    private ReleaseProfilePaths GetPaths()
    {
        var metadata = _metadataBuilder.GetMetadata();
        return new ReleaseProfilePaths(
            _metadataBuilder.ToDirectoryInfoList(metadata.JsonPaths.Sonarr.ReleaseProfiles)
        );
    }

    public IReadOnlyList<ReleaseProfileData> GetReleaseProfileData()
    {
        var paths = GetPaths();
        return _parser.GetReleaseProfileData(paths.ReleaseProfileDirectories).ToList();
    }
}
