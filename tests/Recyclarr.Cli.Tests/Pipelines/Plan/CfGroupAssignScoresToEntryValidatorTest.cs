using Recyclarr.Cli.Pipelines.Plan.Components;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Tests.Pipelines.Plan;

internal sealed class CfGroupAssignScoresToEntryValidatorTest
{
    private static Dictionary<string, QualityProfileResource> GuideProfiles(
        params (string TrashId, string Name)[] profiles
    )
    {
        return profiles.ToDictionary(
            p => p.TrashId,
            p =>
                (QualityProfileResource)
                    new RadarrQualityProfileResource { TrashId = p.TrashId, Name = p.Name },
            StringComparer.OrdinalIgnoreCase
        );
    }

    [Test]
    public void Invalid_trash_id_fails()
    {
        var sut = new CfGroupAssignScoresToEntryValidator(
            "test-group",
            GuideProfiles(("valid-qp", "Valid Profile")),
            []
        );

        var result = sut.Validate(new CfGroupAssignScoresToConfig { TrashId = "nonexistent" });

        result
            .Errors.Should()
            .ContainSingle()
            .Which.ErrorMessage.Should()
            .Contain("Invalid profile trash_id");
    }

    [Test]
    public void Valid_trash_id_passes()
    {
        var sut = new CfGroupAssignScoresToEntryValidator(
            "test-group",
            GuideProfiles(("valid-qp", "Valid Profile")),
            []
        );

        var result = sut.Validate(new CfGroupAssignScoresToConfig { TrashId = "valid-qp" });

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Name_referencing_guide_backed_profile_fails()
    {
        var sut = new CfGroupAssignScoresToEntryValidator(
            "test-group",
            GuideProfiles(("guide-qp", "Guide Profile")),
            [new QualityProfileConfig { TrashId = "guide-qp" }]
        );

        var result = sut.Validate(new CfGroupAssignScoresToConfig { Name = "Guide Profile" });

        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Contain("guide-backed");
    }

    [Test]
    public void Name_referencing_nonexistent_profile_fails()
    {
        var sut = new CfGroupAssignScoresToEntryValidator(
            "test-group",
            GuideProfiles(("guide-qp", "Guide Profile")),
            []
        );

        var result = sut.Validate(new CfGroupAssignScoresToConfig { Name = "Nonexistent Profile" });

        result
            .Errors.Should()
            .ContainSingle()
            .Which.ErrorMessage.Should()
            .Contain("No quality profile");
    }

    [Test]
    public void Name_referencing_user_defined_profile_passes()
    {
        var sut = new CfGroupAssignScoresToEntryValidator(
            "test-group",
            GuideProfiles(("guide-qp", "Guide Profile")),
            [new QualityProfileConfig { Name = "My Custom Profile" }]
        );

        var result = sut.Validate(new CfGroupAssignScoresToConfig { Name = "My Custom Profile" });

        result.IsValid.Should().BeTrue();
    }
}
