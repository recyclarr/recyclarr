using System.IO.Abstractions;
using Recyclarr.Config.Models;
using YamlDotNet.Core;

namespace Recyclarr.Config.Parsing;

internal static class ConfigYamlExtensions
{
    private static AssignScoresToConfig ToAssignScoresToConfig(this QualityScoreConfigYaml yaml)
    {
        return new AssignScoresToConfig
        {
            TrashId = yaml.TrashId,
            Name = yaml.Name ?? "",
            Score = yaml.Score,
        };
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

    private static CfGroupAssignScoresToConfig ToCfGroupAssignScoresToConfig(
        this CfGroupAssignScoresToConfigYaml yaml
    )
    {
        return new CfGroupAssignScoresToConfig { TrashId = yaml.TrashId, Name = yaml.Name };
    }

    private static CustomFormatGroupConfig ToCustomFormatGroupConfig(
        this CustomFormatGroupConfigYaml yaml
    )
    {
        return new CustomFormatGroupConfig
        {
            TrashId = yaml.TrashId ?? "",
            AssignScoresTo =
                yaml.AssignScoresTo?.Select(x => x.ToCfGroupAssignScoresToConfig()).ToList() ?? [],
            Select = yaml.Select?.ToList() ?? [],
            Exclude = yaml.Exclude?.ToList() ?? [],
        };
    }

    private static CustomFormatGroupsConfig ToCustomFormatGroupsConfig(
        this CustomFormatGroupsConfigYaml? yaml
    )
    {
        return new CustomFormatGroupsConfig
        {
            Skip = yaml?.Skip?.ToList() ?? [],
            Add = yaml?.Add?.Select(x => x.ToCustomFormatGroupConfig()).ToList() ?? [],
        };
    }

    private static QualityDefinitionItemConfig ToQualityDefinitionItemConfig(
        this QualitySizeItemConfigYaml yaml
    )
    {
        return new QualityDefinitionItemConfig
        {
            Name = yaml.Name ?? "",
            Min = QualitySizeValue.Parse(yaml.Min),
            Max = QualitySizeValue.Parse(yaml.Max),
            Preferred = QualitySizeValue.Parse(yaml.Preferred),
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
            Qualities =
                yaml.Qualities?.Select(x => x.ToQualityDefinitionItemConfig()).ToList() ?? [],
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
            TrashId = yaml.TrashId,
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

    private static PropersAndRepacksMode? ParsePropersAndRepacks(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "prefer_and_upgrade" => PropersAndRepacksMode.PreferAndUpgrade,
            "do_not_upgrade" => PropersAndRepacksMode.DoNotUpgrade,
            "do_not_prefer" => PropersAndRepacksMode.DoNotPrefer,
            null => null,
            _ => throw new YamlException($"Invalid propers_and_repacks value: '{value}'"),
        };
    }

    private static MediaManagementConfig ToMediaManagementConfig(
        this MediaManagementConfigYaml? yaml
    )
    {
        return new MediaManagementConfig
        {
            PropersAndRepacks = ParsePropersAndRepacks(yaml?.PropersAndRepacks),
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
            CustomFormats =
                yaml.CustomFormats?.Select(x => x.ToCustomFormatConfig()).ToList() ?? [],
            CustomFormatGroups = yaml.CustomFormatGroups.ToCustomFormatGroupsConfig(),
            QualityDefinition = yaml.QualityDefinition?.ToQualityDefinitionConfig(),
            QualityProfiles =
                yaml.QualityProfiles?.Select(x => x.ToQualityProfileConfig()).ToList() ?? [],
            MediaNaming = yaml.MediaNaming.ToRadarrMediaNamingConfig(),
            MediaManagement = yaml.MediaManagement.ToMediaManagementConfig(),
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
            CustomFormats =
                yaml.CustomFormats?.Select(x => x.ToCustomFormatConfig()).ToList() ?? [],
            CustomFormatGroups = yaml.CustomFormatGroups.ToCustomFormatGroupsConfig(),
            QualityDefinition = yaml.QualityDefinition?.ToQualityDefinitionConfig(),
            QualityProfiles =
                yaml.QualityProfiles?.Select(x => x.ToQualityProfileConfig()).ToList() ?? [],
            MediaNaming = yaml.MediaNaming.ToSonarrMediaNamingConfig(),
            MediaManagement = yaml.MediaManagement.ToMediaManagementConfig(),
        };
    }

    private static Uri ParseUri(string? baseUrl)
    {
        return string.IsNullOrWhiteSpace(baseUrl) ? new Uri("about:empty") : new Uri(baseUrl);
    }
}
