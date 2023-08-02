using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.Api;
using Recyclarr.Cli.Pipelines.QualityProfile.PipelinePhases;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class UpdatedQualityProfileTest
{
    [Test]
    public void Profile_name_uses_dto_first()
    {
        var profile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto
            {
                Name = "dto_name"
            },
            ProfileConfig = new ProcessedQualityProfileData(new QualityProfileConfig
            {
                Name = "config_name"
            }),
            UpdateReason = QualityProfileUpdateReason.New
        };

        profile.ProfileName.Should().Be("dto_name");
    }

    [Test]
    public void Profile_name_uses_config_second()
    {
        var profile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto(),
            ProfileConfig = new ProcessedQualityProfileData(new QualityProfileConfig
            {
                Name = "config_name"
            }),
            UpdateReason = QualityProfileUpdateReason.New
        };

        profile.ProfileName.Should().Be("config_name");
    }

    [Test]
    public void Dto_updated_from_config()
    {
        var profile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto
            {
                Id = 1,
                Name = "dto_name",
                MinFormatScore = 100,
                CutoffFormatScore = 200,
                UpgradeAllowed = false,
                Cutoff = 1
            },
            ProfileConfig = new ProcessedQualityProfileData(new QualityProfileConfig
            {
                Name = "config_name",
                MinFormatScore = 110,
                UpgradeUntilScore = 220,
                UpgradeUntilQuality = "Quality Item 3"
            }),
            UpdatedQualities = new UpdatedQualities
            {
                Items = new List<ProfileItemDto>
                {
                    NewQp.QualityDto(1, "Quality Item 1", true),
                    NewQp.QualityDto(2, "Quality Item 2", true),
                    NewQp.GroupDto(3, "Quality Item 3", true,
                        NewQp.QualityDto(4, "Quality Item 4", true))
                }
            },
            UpdateReason = QualityProfileUpdateReason.New
        };

        var result = profile.BuildUpdatedDto();

        result.Should().BeEquivalentTo(new QualityProfileDto
        {
            // For right now, names are used for lookups (since QPs have no cache yet). As such, two profiles with
            // different names will never be matched and so the names should normally be identical. However, for testing
            // purposes, I made them different to make sure it doesn't get overwritten.
            Name = "dto_name",
            Id = 1,
            MinFormatScore = 110,
            CutoffFormatScore = 220,
            UpgradeAllowed = true,
            Cutoff = 3,
            Items = profile.UpdatedQualities.Items
        });
    }

    [Test]
    public void Dto_name_is_updated_when_empty()
    {
        var profile = new UpdatedQualityProfile
        {
            ProfileDto = new QualityProfileDto {Name = ""},
            ProfileConfig = new ProcessedQualityProfileData(new QualityProfileConfig {Name = "config_name"}),
            UpdatedQualities = new UpdatedQualities
            {
                Items = new[]
                {
                    new ProfileItemDto()
                }
            },
            UpdateReason = QualityProfileUpdateReason.New
        };

        var dto = profile.BuildUpdatedDto();

        dto.Name.Should().Be("config_name");
    }
}
