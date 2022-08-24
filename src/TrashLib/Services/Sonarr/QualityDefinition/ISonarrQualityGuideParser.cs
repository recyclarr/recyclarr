namespace TrashLib.Services.Sonarr.QualityDefinition;

public interface ISonarrQualityGuideParser
{
    ICollection<SonarrQualityData> GetQualities();
}
