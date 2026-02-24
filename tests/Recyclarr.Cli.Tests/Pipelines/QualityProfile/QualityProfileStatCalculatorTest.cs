using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.QualityProfile;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile;

internal sealed class QualityProfileStatCalculatorTest
{
    private static UpdatedQualityProfile CreateProfile(
        QualityProfileDto serviceDto,
        QualityProfileConfig? config = null
    )
    {
        config ??= new QualityProfileConfig { Name = serviceDto.Name };
        return new UpdatedQualityProfile
        {
            ProfileDto = serviceDto,
            ProfileConfig = NewPlan.Qp(config),
        };
    }

    [Test, AutoMockData]
    public void Name_change_only_is_detected(QualityProfileStatCalculator sut)
    {
        var profile = CreateProfile(
            new QualityProfileDto { Id = 1, Name = "Old Name" },
            new QualityProfileConfig { Name = "New Name" }
        );

        var result = sut.Calculate(profile);

        result.ProfileChanged.Should().BeTrue();
    }

    [Test, AutoMockData]
    public void No_changes_detected_when_all_fields_match(QualityProfileStatCalculator sut)
    {
        var profile = CreateProfile(
            new QualityProfileDto
            {
                Id = 1,
                Name = "Same Name",
                UpgradeAllowed = true,
                MinFormatScore = 0,
                MinUpgradeFormatScore = 1,
                CutoffFormatScore = 10000,
            },
            new QualityProfileConfig
            {
                Name = "Same Name",
                UpgradeAllowed = true,
                MinFormatScore = 0,
                MinUpgradeFormatScore = 1,
                UpgradeUntilScore = 10000,
            }
        );

        var result = sut.Calculate(profile);

        result.ProfileChanged.Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Upgrade_allowed_change_is_detected(QualityProfileStatCalculator sut)
    {
        var profile = CreateProfile(
            new QualityProfileDto
            {
                Id = 1,
                Name = "Profile",
                UpgradeAllowed = false,
            },
            new QualityProfileConfig { Name = "Profile", UpgradeAllowed = true }
        );

        var result = sut.Calculate(profile);

        result.ProfileChanged.Should().BeTrue();
    }

    [Test]
    public void Build_updated_dto_applies_name_change()
    {
        var profile = CreateProfile(
            new QualityProfileDto { Id = 42, Name = "Old Name" },
            new QualityProfileConfig { Name = "New Name" }
        );

        var updatedDto = profile.BuildUpdatedDto();

        updatedDto.Name.Should().Be("New Name");
    }
}
