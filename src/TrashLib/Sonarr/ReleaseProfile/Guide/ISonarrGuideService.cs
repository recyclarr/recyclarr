namespace TrashLib.Sonarr.ReleaseProfile.Guide;

public interface ISonarrGuideService
{
    IReadOnlyCollection<ReleaseProfileData> GetReleaseProfileData();
}
