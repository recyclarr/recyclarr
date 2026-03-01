using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Config.Models;
using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Cli.Tests.Reusable;

internal static class NewQp
{
    public static UpdatedFormatScore UpdatedScore(
        string name,
        int oldScore,
        int newScore,
        FormatScoreUpdateReason reason
    )
    {
        return new UpdatedFormatScore(
            new QualityProfileFormatItem
            {
                FormatId = 0,
                Name = name,
                Score = oldScore,
            },
            newScore,
            reason
        );
    }

    public static QualityProfileItem GroupItem(
        int itemId,
        string itemName,
        bool enabled,
        params QualityProfileItem[] nestedItems
    )
    {
        return new QualityProfileItem
        {
            Id = itemId,
            Name = itemName,
            Allowed = enabled,
            Items = nestedItems,
        };
    }

    public static QualityProfileItem QualityItem(int itemId, string itemName, bool enabled)
    {
        return new QualityProfileItem
        {
            Allowed = enabled,
            Quality = new QualityProfileItemQuality { Id = itemId, Name = itemName },
        };
    }

    [SuppressMessage(
        "ReSharper",
        "IntroduceOptionalParameters.Global",
        Justification = "This is for unit test purposes and we want to be explicit sometimes"
    )]
    public static QualityProfileQualityConfig QualityConfig(string itemName)
    {
        return QualityConfig(itemName, true);
    }

    public static QualityProfileQualityConfig QualityConfig(string itemName, bool enabled)
    {
        return new QualityProfileQualityConfig { Enabled = enabled, Name = itemName };
    }

    public static QualityProfileQualityConfig GroupConfig(
        string itemName,
        params string[] nestedItems
    )
    {
        return GroupConfig(itemName, true, nestedItems);
    }

    public static QualityProfileQualityConfig GroupConfig(
        string itemName,
        bool enabled,
        params string[] nestedItems
    )
    {
        return QualityConfig(itemName, enabled) with { Qualities = nestedItems };
    }

    public static QualityProfileServiceData ServiceData(
        IReadOnlyList<QualityProfileData>? profiles = null,
        QualityProfileData? schema = null,
        IReadOnlyList<ProfileLanguage>? languages = null
    ) => new(profiles ?? [], schema ?? new QualityProfileData { Name = "" }, languages ?? []);
}
