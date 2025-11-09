using System.IO.Abstractions;
using Recyclarr.Config.Models;

namespace Recyclarr.Config.Parsing;

internal static class ConfigYamlExtensions
{
    private static AssignScoresToConfig ToAssignScoresToConfig(this QualityScoreConfigYaml yaml)
    {
        return new AssignScoresToConfig { Name = yaml.Name ?? "", Score = yaml.Score };
    }

    private static CustomFormatConfig ToCustomFormatConfig(this CustomFormatConfigYaml yaml)
    {
        return new CustomFormatConfig
        {
            TrashIds = yaml.TrashIds?.ToList() ?? [],
            AssignScoresTo =
                yaml.AssignScoresTo?.Select(x => x.ToAssignScoresToConfig()).ToList() ?? [],
        };
    }

    private static QualityDefinitionConfig ToQualityDefinitionConfig(
        this QualitySizeConfigYaml yaml
    )
    {
        return new QualityDefinitionConfig
        {
            Type = yaml.Type ?? "",
            PreferredRatio = yaml.PreferredRatio,
        };
    }

    private static ResetUnmatchedScoresConfig ToResetUnmatchedScoresConfig(
        this ResetUnmatchedScoresConfigYaml? yaml
    )
    {
        return new ResetUnmatchedScoresConfig
        {
            Enabled = yaml?.Enabled ?? false,
            Except = yaml?.Except?.ToList() ?? [],
        };
    }

    public static QualityProfileQualityConfig ToQualityProfileQualityConfig(
        this QualityProfileQualityConfigYaml yaml
    )
    {
        return new QualityProfileQualityConfig
        {
            Name = yaml.Name ?? "",
            Enabled = yaml.Enabled ?? true,
            Qualities = yaml.Qualities?.ToList() ?? [],
        };
    }

    public static QualityProfileConfig ToQualityProfileConfig(this QualityProfileConfigYaml yaml)
    {
        return new QualityProfileConfig
        {
            Name = yaml.Name ?? "",
            UpgradeAllowed = yaml.Upgrade?.Allowed,
            UpgradeUntilQuality = yaml.Upgrade?.UntilQuality,
            UpgradeUntilScore = yaml.Upgrade?.UntilScore,
            MinFormatScore = yaml.MinFormatScore,
            MinUpgradeFormatScore = yaml.MinUpgradeFormatScore,
            ScoreSet = yaml.ScoreSet,
            QualitySort = yaml.QualitySort ?? QualitySortAlgorithm.Top,
            ResetUnmatchedScores = yaml.ResetUnmatchedScores.ToResetUnmatchedScoresConfig(),
            Qualities =
                yaml.Qualities?.Select(x => x.ToQualityProfileQualityConfig()).ToList() ?? [],
        };
    }

    private static RadarrMovieNamingConfig ToRadarrMovieNamingConfig(
        this RadarrMovieNamingConfigYaml yaml
    )
    {
        return new RadarrMovieNamingConfig { Rename = yaml.Rename, Standard = yaml.Standard };
    }

    private static RadarrMediaNamingConfig ToRadarrMediaNamingConfig(
        this RadarrMediaNamingConfigYaml? yaml
    )
    {
        return new RadarrMediaNamingConfig
        {
            Folder = yaml?.Folder,
            Movie = yaml?.Movie?.ToRadarrMovieNamingConfig(),
        };
    }

    private static SonarrEpisodeNamingConfig ToSonarrEpisodeNamingConfig(
        this SonarrEpisodeNamingConfigYaml yaml
    )
    {
        return new SonarrEpisodeNamingConfig
        {
            Rename = yaml.Rename,
            Standard = yaml.Standard,
            Daily = yaml.Daily,
            Anime = yaml.Anime,
        };
    }

    private static SonarrMediaNamingConfig ToSonarrMediaNamingConfig(
        this SonarrMediaNamingConfigYaml? yaml
    )
    {
        return new SonarrMediaNamingConfig
        {
            Season = yaml?.Season,
            Series = yaml?.Series,
            Episodes = yaml?.Episodes?.ToSonarrEpisodeNamingConfig(),
        };
    }

    public static IServiceConfiguration ToRadarrConfiguration(
        this RadarrConfigYaml yaml,
        string instanceName,
        IFileInfo? yamlPath
    )
    {
        return new RadarrConfiguration
        {
            InstanceName = instanceName,
            YamlPath = yamlPath,
            BaseUrl = ParseUri(yaml.BaseUrl),
            ApiKey = yaml.ApiKey ?? "",
            DeleteOldCustomFormats = yaml.DeleteOldCustomFormats ?? false,
            ReplaceExistingCustomFormats = yaml.ReplaceExistingCustomFormats ?? false,
            CustomFormats =
                yaml.CustomFormats?.Select(x => x.ToCustomFormatConfig()).ToList() ?? [],
            QualityDefinition = yaml.QualityDefinition?.ToQualityDefinitionConfig(),
            QualityProfiles =
                yaml.QualityProfiles?.Select(x => x.ToQualityProfileConfig()).ToList() ?? [],
            MediaNaming = yaml.MediaNaming.ToRadarrMediaNamingConfig(),
        };
    }

    public static IServiceConfiguration ToSonarrConfiguration(
        this SonarrConfigYaml yaml,
        string instanceName,
        IFileInfo? yamlPath
    )
    {
        return new SonarrConfiguration
        {
            InstanceName = instanceName,
            YamlPath = yamlPath,
            BaseUrl = ParseUri(yaml.BaseUrl),
            ApiKey = yaml.ApiKey ?? "",
            DeleteOldCustomFormats = yaml.DeleteOldCustomFormats ?? false,
            ReplaceExistingCustomFormats = yaml.ReplaceExistingCustomFormats ?? false,
            CustomFormats =
                yaml.CustomFormats?.Select(x => x.ToCustomFormatConfig()).ToList() ?? [],
            QualityDefinition = yaml.QualityDefinition?.ToQualityDefinitionConfig(),
            QualityProfiles =
                yaml.QualityProfiles?.Select(x => x.ToQualityProfileConfig()).ToList() ?? [],
            MediaNaming = yaml.MediaNaming.ToSonarrMediaNamingConfig(),
        };
    }

    private static Uri ParseUri(string? baseUrl)
    {
        return string.IsNullOrWhiteSpace(baseUrl) ? new Uri("about:empty") : new Uri(baseUrl);
    }
}
