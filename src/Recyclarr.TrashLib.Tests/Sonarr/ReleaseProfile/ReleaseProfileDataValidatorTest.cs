using FluentAssertions;
using FluentValidation.TestHelper;
using NUnit.Framework;
using Recyclarr.TrashLib.Services.ReleaseProfile;

namespace Recyclarr.TrashLib.Tests.Sonarr.ReleaseProfile;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ReleaseProfileDataValidatorTest
{
    [Test]
    public void Empty_term_collections_not_allowed()
    {
        var validator = new ReleaseProfileDataValidator();
        var data = new ReleaseProfileData();

        validator.Validate(data).IsValid.Should().BeFalse();
    }

    [Test]
    public void Allow_single_preferred_term()
    {
        var validator = new ReleaseProfileDataValidator();
        var data = new ReleaseProfileData
        {
            TrashId = "trash_id",
            Name = "name",
            Required = Array.Empty<TermData>(),
            Ignored = Array.Empty<TermData>(),
            Preferred = new[] {new PreferredTermData {Terms = new[] {new TermData()}}}
        };

        var result = validator.TestValidate(data);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Allow_single_required_term()
    {
        var validator = new ReleaseProfileDataValidator();
        var data = new ReleaseProfileData
        {
            TrashId = "trash_id",
            Name = "name",
            Required = new[] {new TermData {Term = "term"}},
            Ignored = Array.Empty<TermData>(),
            Preferred = Array.Empty<PreferredTermData>()
        };

        var result = validator.TestValidate(data);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Allow_single_ignored_term()
    {
        var validator = new ReleaseProfileDataValidator();
        var data = new ReleaseProfileData
        {
            TrashId = "trash_id",
            Name = "name",
            Required = Array.Empty<TermData>(),
            Ignored = new[] {new TermData {Term = "term"}},
            Preferred = Array.Empty<PreferredTermData>()
        };

        var result = validator.TestValidate(data);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Term_data_validate_empty()
    {
        var validator = new TermDataValidator();
        var data = new TermData();

        var result = validator.TestValidate(data);

        result.ShouldHaveValidationErrorFor(x => x.Term);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
        result.ShouldNotHaveValidationErrorFor(x => x.TrashId);
    }

    [Test]
    public void Preferred_term_data_validate_empty()
    {
        var validator = new PreferredTermDataValidator();
        var data = new PreferredTermData();

        var result = validator.TestValidate(data);

        result.ShouldHaveValidationErrorFor(x => x.Terms);
    }
}
