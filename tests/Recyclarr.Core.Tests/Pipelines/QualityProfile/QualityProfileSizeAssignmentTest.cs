using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile;

internal sealed class QualityProfileSizeAssignmentTest
{
    private static readonly QualityItemSize ServerSize = new(1, 10, 5);
    private static readonly QualityItemSize AssignedSize = new(5, 50, 30);

    private static QualityProfileItem Quality(int id, string name, QualityItemSize? size) =>
        new()
        {
            Allowed = true,
            Quality = new QualityProfileItemQuality { Id = id, Name = name },
            Size = size,
        };

    private static QualityProfileData ServerProfile() =>
        new()
        {
            Id = 1,
            Name = "Profile",
            Cutoff = 9,
            Items =
            [
                Quality(9, "HDTV-1080p", ServerSize),
                new QualityProfileItem
                {
                    Id = 1001,
                    Name = "WEB 1080p",
                    Allowed = true,
                    Items = [Quality(3, "WEBDL-1080p", ServerSize)],
                },
                Quality(4, "HDTV-720p", ServerSize),
            ],
        };

    private static UpdatedQualityProfile CreateProfile(
        IReadOnlyDictionary<string, QualityItemSize> assigned
    ) =>
        new()
        {
            Profile = ServerProfile(),
            ProfileConfig = NewPlan.Qp(new QualityProfileConfig { Name = "Profile" }),
            AssignedSizes = assigned,
        };

    [Test]
    public void Assigned_sizes_land_on_top_level_and_grouped_qualities()
    {
        var profile = CreateProfile(
            new Dictionary<string, QualityItemSize>
            {
                ["HDTV-1080p"] = AssignedSize,
                ["WEBDL-1080p"] = AssignedSize,
            }
        );

        var merged = profile.BuildMergedProfile();

        merged.Items[0].Size.Should().Be(AssignedSize);
        merged.Items[1].Items[0].Size.Should().Be(AssignedSize);
        merged.Items[2].Size.Should().Be(ServerSize, "unassigned qualities keep service sizes");
    }

    [Test, AutoMockData]
    public void Size_difference_is_a_quality_change(QualityProfileStatCalculator sut)
    {
        var profile = CreateProfile(
            new Dictionary<string, QualityItemSize> { ["HDTV-1080p"] = AssignedSize }
        );

        sut.Calculate(profile).QualitiesChanged.Should().BeTrue();
    }

    // Ratio-computed sizes carry a decimal scale (70.0) that service values (70) do not
    [Test, AutoMockData]
    public void Equal_sizes_with_different_decimal_scale_are_not_a_change(
        QualityProfileStatCalculator sut
    )
    {
        var profile = CreateProfile(
            new Dictionary<string, QualityItemSize> { ["HDTV-1080p"] = new(1.0m, 10.00m, 5.0m) }
        );

        sut.Calculate(profile).QualitiesChanged.Should().BeFalse();
    }

    [Test, AutoMockData]
    public void Identical_sizes_are_not_a_change(QualityProfileStatCalculator sut)
    {
        var profile = CreateProfile(
            new Dictionary<string, QualityItemSize> { ["HDTV-1080p"] = ServerSize }
        );

        sut.Calculate(profile).QualitiesChanged.Should().BeFalse();
    }
}
