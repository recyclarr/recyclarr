namespace Recyclarr.TrashGuide.MediaNaming;

public interface IMediaNamingResourceQuery
{
    RadarrMediaNamingData GetRadarrNamingData();
    SonarrMediaNamingData GetSonarrNamingData();
}
