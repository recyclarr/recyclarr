using System.Collections.Generic;
using TrashLib.Config;
using TrashLib.Radarr.QualityDefinition;

// ReSharper disable ClassNeverInstantiated.Global

namespace TrashLib.Radarr.Config
{
    public class RadarrConfig : ServiceConfiguration
    {
        public int Id { get; set; }
        public QualityDefinitionConfig? QualityDefinition { get; init; }
        public ICollection<CustomFormatConfig> CustomFormats { get; set; }// = new();
        public ICollection<QualityProfileConfig> QualityProfiles { get; init; }// = new();
        public bool DeleteOldCustomFormats { get; init; }
    }

    public class CustomFormatConfig
    {
        public string Name { get; init; } = "";
        public string TrashId { get; init; } = "";
    }

    public class QualityProfileConfig
    {
        public ICollection<ScoreEntryConfig> Scores { get; init; }// = new();
        public bool ResetUnmatchedScores { get; init; }
    }

    public class ScoreEntryConfig
    {
        public string TrashId { get; init; } = "";
        public int? Score { get; init; }
    }

    public class QualityDefinitionConfig
    {
        // -1 does not map to a valid enumerator. this is to force validation to fail if it is not set from YAML.
        // All of this craziness is to avoid making the enum type nullable.
        public RadarrQualityDefinitionType Type { get; set; } = (RadarrQualityDefinitionType) (-1);

        public decimal PreferredRatio { get; set; } = 1.0m;
    }
}
