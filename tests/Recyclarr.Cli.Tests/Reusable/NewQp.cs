using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.Models;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

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
            new ProfileFormatItemDto { Name = name, Score = oldScore },
            newScore,
            reason
        );
    }

    public static ProfileItemDto GroupDto(
        int itemId,
        string itemName,
        bool enabled,
        params ProfileItemDto[] nestedItems
    )
    {
        return new ProfileItemDto
        {
            Id = itemId,
            Name = itemName,
            Allowed = enabled,
            Items = nestedItems,
        };
    }

    public static ProfileItemDto QualityDto(int itemId, string itemName, bool enabled)
    {
        return new ProfileItemDto
        {
            Allowed = enabled,
            Quality = new ProfileItemQualityDto { Id = itemId, Name = itemName },
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
        IReadOnlyList<QualityProfileDto>? profiles = null,
        QualityProfileDto? schema = null,
        IReadOnlyList<ProfileLanguageDto>? languages = null
    ) => new(profiles ?? [], schema ?? new(), languages ?? []);
}
