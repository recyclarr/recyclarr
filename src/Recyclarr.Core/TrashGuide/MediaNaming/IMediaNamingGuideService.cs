namespace Recyclarr.TrashGuide.MediaNaming;

public interface IMediaNamingGuideService
{
    RadarrMediaNamingData GetRadarrNamingData();
    SonarrMediaNamingData GetSonarrNamingData();
}
