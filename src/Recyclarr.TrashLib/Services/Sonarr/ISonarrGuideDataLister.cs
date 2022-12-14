namespace Recyclarr.TrashLib.Services.Sonarr;

public interface ISonarrGuideDataLister
{
    void ListReleaseProfiles();
    void ListTerms(string releaseProfileId);
    void ListQualities();
    void ListCustomFormats();
}
