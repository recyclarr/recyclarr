using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile;

internal sealed class UpdatedQualityProfileTest
{
    [Test]
    public void Profile_name_uses_dto_first()
    {
        var profile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto { Name = "dto_name" },
            ProfileConfig = NewPlan.Qp("config_name"),
        };

        profile.ProfileName.Should().Be("dto_name");
    }

    [Test]
    public void Profile_name_uses_config_second()
    {
        var profile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto(),
            ProfileConfig = NewPlan.Qp("config_name"),
        };

        profile.ProfileName.Should().Be("config_name");
    }

    [Test]
    public void Dto_updated_from_config_with_qualities()
    {
        var profile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto
            {
                Id = 1,
                Name = "dto_name",
                MinFormatScore = 100,
                MinUpgradeFormatScore = 100,
                CutoffFormatScore = 200,
                UpgradeAllowed = false,
                Cutoff = 1,
            },
            ProfileConfig = NewPlan.Qp(
                new QualityProfileConfig
                {
                    Name = "config_name",
                    MinFormatScore = 110,
                    MinUpgradeFormatScore = 110,
                    UpgradeAllowed = true,
                    UpgradeUntilScore = 220,
                }
            ),
            UpdatedQualities = new UpdatedQualities
            {
                NumWantedItems = 1,
                Items =
                [
                    NewQp.QualityDto(1, "Quality Item 1", true),
                    NewQp.QualityDto(2, "Quality Item 2", true),
                    NewQp.GroupDto(
                        3,
                        "Quality Item 3",
                        true,
                        NewQp.QualityDto(4, "Quality Item 4", true)
                    ),
                ],
            },
        };

        var result = profile.BuildUpdatedDto();

        result
            .Should()
            .BeEquivalentTo(
                new QualityProfileDto
                {
                    // For right now, names are used for lookups (since QPs have no cache yet). As such, two profiles with
                    // different names will never be matched and so the names should normally be identical. However, for testing
                    // purposes, I made them different to make sure it doesn't get overwritten.
                    Name = "dto_name",
                    Id = 1,
                    MinFormatScore = 110,
                    MinUpgradeFormatScore = 110,
                    CutoffFormatScore = 220,
                    UpgradeAllowed = true,
                    Items = profile.UpdatedQualities.Items,
                },
                o => o.Excluding(x => x.Cutoff)
            );
    }

    [Test]
    public void Dto_quality_items_updated_from_config_with_no_qualities()
    {
        var profile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto
            {
                Items =
                [
                    NewQp.QualityDto(8, "Quality Item 8", true),
                    NewQp.QualityDto(9, "Quality Item 9", true),
                ],
            },
            ProfileConfig = NewPlan.Qp(""),
            UpdatedQualities = new UpdatedQualities
            {
                NumWantedItems = 0,
                Items =
                [
                    NewQp.QualityDto(1, "Quality Item 1", true),
                    NewQp.QualityDto(2, "Quality Item 2", true),
                    NewQp.GroupDto(
                        3,
                        "Quality Item 3",
                        true,
                        NewQp.QualityDto(4, "Quality Item 4", true)
                    ),
                ],
            },
        };

        var result = profile.BuildUpdatedDto();

        result.Items.Should().BeEquivalentTo(profile.ProfileDto.Items);
    }

    [Test]
    public void Dto_name_is_updated_when_empty()
    {
        var profile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto { Name = "" },
            ProfileConfig = NewPlan.Qp("config_name"),
            UpdatedQualities = new UpdatedQualities { Items = [new ProfileItemDto()] },
        };

        var dto = profile.BuildUpdatedDto();

        dto.Name.Should().Be("config_name");
    }

    [TestCase(null)]
    [TestCase(8)]
    public void Cutoff_obtained_from_updated_qualities(int? originalCutoff)
    {
        var profile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto
            {
                // To verify that it gets overwritten because config specifies a cutoff
                Cutoff = originalCutoff,
                Items =
                [
                    NewQp.QualityDto(8, "Quality Item 8", true),
                    NewQp.QualityDto(9, "Quality Item 9", true),
                ],
            },
            ProfileConfig = NewPlan.Qp(
                new QualityProfileConfig { UpgradeUntilQuality = "Quality Item 2" }
            ),
            UpdatedQualities = new UpdatedQualities
            {
                NumWantedItems = 1,
                Items =
                [
                    NewQp.QualityDto(1, "Quality Item 1", true),
                    NewQp.QualityDto(2, "Quality Item 2", true),
                    NewQp.GroupDto(
                        3,
                        "Quality Item 3",
                        true,
                        NewQp.QualityDto(4, "Quality Item 4", true)
                    ),
                ],
            },
        };

        var dto = profile.BuildUpdatedDto();

        dto.Cutoff.Should().Be(2);
    }

    [TestCase(null)]
    [TestCase(8)]
    public void Cutoff_obtained_from_original_qualities(int? originalCutoff)
    {
        var profile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto
            {
                // To verify that it gets overwritten because config specifies a cutoff
                Cutoff = originalCutoff,
                Items =
                [
                    NewQp.QualityDto(8, "Quality Item 8", true),
                    NewQp.QualityDto(9, "Quality Item 9", true),
                ],
            },
            ProfileConfig = NewPlan.Qp(
                new QualityProfileConfig { UpgradeUntilQuality = "Quality Item 9" }
            ),
            UpdatedQualities = new UpdatedQualities
            {
                NumWantedItems = 0, // zero forces cutoff search to fall back to original DTO items
                Items =
                [
                    NewQp.QualityDto(1, "Quality Item 1", true),
                    NewQp.QualityDto(2, "Quality Item 2", true),
                    NewQp.GroupDto(
                        3,
                        "Quality Item 3",
                        true,
                        NewQp.QualityDto(4, "Quality Item 4", true)
                    ),
                ],
            },
        };

        var dto = profile.BuildUpdatedDto();

        dto.Cutoff.Should().Be(9);
    }

    [Test]
    public void Cutoff_fall_back_to_first()
    {
        var profile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto
            {
                Items =
                [
                    NewQp.QualityDto(8, "Quality Item 8", true),
                    NewQp.QualityDto(9, "Quality Item 9", true),
                ],
            },
            ProfileConfig = NewPlan.Qp(
                new QualityProfileConfig
                {
                    // Do not specify an `UpgradeUntilQuality` here to simulate fallback
                }
            ),
            UpdatedQualities = new UpdatedQualities
            {
                NumWantedItems = 1,
                Items =
                [
                    NewQp.QualityDto(1, "Quality Item 1", true),
                    NewQp.QualityDto(2, "Quality Item 2", true),
                    NewQp.GroupDto(
                        3,
                        "Quality Item 3",
                        true,
                        NewQp.QualityDto(4, "Quality Item 4", true)
                    ),
                ],
            },
        };

        var dto = profile.BuildUpdatedDto();

        dto.Cutoff.Should().Be(1);
    }

    [Test]
    public void Cutoff_not_modified_when_already_set_and_no_upgrade_until()
    {
        var profile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto
            {
                Cutoff = 8,
                Items =
                [
                    NewQp.QualityDto(8, "Quality Item 8", true),
                    NewQp.QualityDto(9, "Quality Item 9", true),
                ],
            },
            ProfileConfig = NewPlan.Qp(
                new QualityProfileConfig
                {
                    // Do not specify an `UpgradeUntilQuality` here to keep existing cutoff
                }
            ),
            UpdatedQualities = new UpdatedQualities { NumWantedItems = 0 },
        };

        var dto = profile.BuildUpdatedDto();

        dto.Cutoff.Should().Be(8);
    }
}
