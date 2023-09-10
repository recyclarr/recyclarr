using Recyclarr.TrashLib.Repo;

namespace Recyclarr.TrashLib.Guide.ReleaseProfile;

public class ReleaseProfileGuideService : IReleaseProfileGuideService
{
    private readonly Lazy<IReadOnlyList<ReleaseProfileData>> _guideData;

    public ReleaseProfileGuideService(IRepoMetadataBuilder metadataBuilder, ReleaseProfileGuideParser parser)
    {
        _guideData = new Lazy<IReadOnlyList<ReleaseProfileData>>(() =>
        {
            var metadata = metadataBuilder.GetMetadata();
            var paths = metadataBuilder.ToDirectoryInfoList(metadata.JsonPaths.Sonarr.ReleaseProfiles);
            return parser.GetReleaseProfileData(paths).ToList();
        });
    }

    public IReadOnlyList<ReleaseProfileData> GetReleaseProfileData()
    {
        return _guideData.Value;
    }
}
