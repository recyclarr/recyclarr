using Recyclarr.Config.Models;
using Recyclarr.Pipelines.QualityProfile;
using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Core.Tests.Pipelines.QualityProfile;

internal sealed class QualityProfileStatCalculatorTest
{
    private static UpdatedQualityProfile CreateProfile(
        QualityProfileData serviceDto,
        QualityProfileConfig? config = null
    )
    {
        config ??= new QualityProfileConfig { Name = serviceDto.Name };
        return new UpdatedQualityProfile
        {
            Profile = serviceDto,
            ProfileConfig = NewPlan.Qp(config),
        };
    }

    [Test, AutoMockData]
    public void Name_change_only_is_detected(QualityProfileStatCalculator sut)
    {
        var profile = CreateProfile(
            new QualityProfileData { Id = 1, Name = "Old Name" },
            new QualityProfileConfig { Name = "New Name" }
        );

        var result = sut.Calculate(profile);

        result.ProfileChanged.Should().BeTrue();
    }

    [Test, AutoMockData]
    public void No_changes_detected_when_all_fields_match(QualityProfileStatCalculator sut)
    {
        var profile = CreateProfile(
            new QualityProfileData
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
            new QualityProfileData
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

    [Test, AutoMockData]
    public void Language_change_is_detected(QualityProfileStatCalculator sut)
    {
        var french = new ProfileLanguage { Id = 2, Name = "French" };
        var profile = new UpdatedQualityProfile
        {
            Profile = new QualityProfileData
            {
                Id = 1,
                Name = "Profile",
                Language = new ProfileLanguage { Id = 1, Name = "English" },
            },
            ProfileConfig = NewPlan.Qp(
                new QualityProfileConfig { Name = "Profile" },
                NewPlan.QpResource("trash-id", "Profile") with
                {
                    Language = "French",
                }
            ),
            Languages = [french],
        };

        var result = sut.Calculate(profile);

        result.ProfileChanged.Should().BeTrue();
    }

    [Test]
    public void Build_updated_dto_applies_name_change()
    {
        var profile = CreateProfile(
            new QualityProfileData { Id = 42, Name = "Old Name" },
            new QualityProfileConfig { Name = "New Name" }
        );

        var updatedDto = profile.BuildMergedProfile();

        updatedDto.Name.Should().Be("New Name");
    }
}
