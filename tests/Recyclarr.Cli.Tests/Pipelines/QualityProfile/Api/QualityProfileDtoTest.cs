using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile.Api;

internal sealed class QualityProfileDtoTest
{
    [Test]
    [Arguments(null, false)]
    [Arguments(true, true)]
    public void Upgrade_allowed_set_behavior(bool? value, bool? expected)
    {
        var dto = new QualityProfileDto { UpgradeAllowed = false };

        var result = dto with { UpgradeAllowed = value };

        result.UpgradeAllowed.Should().Be(expected);
    }

    [Test]
    [Arguments(null, 10)]
    [Arguments(20, 20)]
    public void Min_format_score_set_behavior(int? value, int? expected)
    {
        var dto = new QualityProfileDto { MinFormatScore = 10 };

        var result = dto with { MinFormatScore = value };

        result.MinFormatScore.Should().Be(expected);
    }

    [Test]
    [Arguments(null, 10)]
    [Arguments(20, 20)]
    public void Min_format_upgrade_score_set_behavior(int? value, int? expected)
    {
        var dto = new QualityProfileDto { MinUpgradeFormatScore = 10 };

        var result = dto with { MinUpgradeFormatScore = value };

        result.MinUpgradeFormatScore.Should().Be(expected);
    }

    [Test]
    [Arguments(null, 10)]
    [Arguments(20, 20)]
    public void Cutoff_set_behavior(int? value, int? expected)
    {
        var dto = new QualityProfileDto { Cutoff = 10 };

        var result = dto with { Cutoff = value };

        result.Cutoff.Should().Be(expected);
    }

    [Test]
    [Arguments(null, 10)]
    [Arguments(20, 20)]
    public void Cutoff_format_score_set_behavior(int? value, int? expected)
    {
        var dto = new QualityProfileDto { CutoffFormatScore = 10 };

        var result = dto with { CutoffFormatScore = value };

        result.CutoffFormatScore.Should().Be(expected);
    }

    [Test]
    public void Items_no_change_when_assigning_empty_collection()
    {
        var dto = new QualityProfileDto
        {
            Items = [NewQp.QualityDto(1, "one", true), NewQp.QualityDto(2, "two", true)],
        };

        var result = dto with { Items = [] };

        result.Items.Should().BeEquivalentTo(dto.Items);
    }
}
