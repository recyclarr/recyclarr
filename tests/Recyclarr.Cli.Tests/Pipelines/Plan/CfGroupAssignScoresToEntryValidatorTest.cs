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
            GuideProfiles(("valid-qp", "Valid Profile"))
        );

        var result = sut.Validate(new AssignScoresToConfig { TrashId = "nonexistent" });

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
            GuideProfiles(("valid-qp", "Valid Profile"))
        );

        var result = sut.Validate(new AssignScoresToConfig { TrashId = "valid-qp" });

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Name_referencing_guide_backed_profile_passes()
    {
        var sut = new CfGroupAssignScoresToEntryValidator(
            "test-group",
            GuideProfiles(("guide-qp", "Guide Profile"))
        );

        var result = sut.Validate(new AssignScoresToConfig { Name = "Guide Profile" });

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Name_not_in_guide_passes_validation()
    {
        var sut = new CfGroupAssignScoresToEntryValidator(
            "test-group",
            GuideProfiles(("guide-qp", "Guide Profile"))
        );

        var result = sut.Validate(new AssignScoresToConfig { Name = "Any" });

        result.IsValid.Should().BeTrue();
    }
}
