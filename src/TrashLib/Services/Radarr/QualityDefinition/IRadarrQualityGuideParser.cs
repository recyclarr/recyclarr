namespace TrashLib.Services.Radarr.QualityDefinition;

public interface IRadarrQualityGuideParser
{
    ICollection<RadarrQualityData> GetQualities();
}
