using FluentValidation.TestHelper;
using Recyclarr.TrashLib.Config.Parsing;

namespace Recyclarr.TrashLib.Tests.Config.Parsing;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigYamlDataObjectsValidationTest
{
    [Test]
    public void Quality_profile_name_required()
    {
        var data = new QualityProfileConfigYaml();

        var validator = new QualityProfileConfigYamlValidator();
        var result = validator.TestValidate(data);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Quality_profile_until_quality_required()
    {
        var data = new QualityProfileConfigYaml
        {
            UpgradesAllowed = new QualityProfileFormatUpgradeYaml()
        };

        var validator = new QualityProfileConfigYamlValidator();
        var result = validator.TestValidate(data);

        result.ShouldHaveValidationErrorFor(x => x.UpgradesAllowed!.UntilQuality);
    }

    [Test]
    public void Quality_profile_qualities_must_have_cutoff_quality()
    {
        var data = new QualityProfileConfigYaml
        {
            Name = "My QP",
            UpgradesAllowed = new QualityProfileFormatUpgradeYaml
            {
                UntilQuality = "Test Quality"
            },
            Qualities = new[]
            {
                new QualityProfileQualityConfigYaml {Name = "Another Quality"}
            }
        };

        var validator = new QualityProfileConfigYamlValidator();
        var result = validator.TestValidate(data);

        result.ShouldHaveValidationErrorFor(x => x.Qualities);

        result.Errors.Select(x => x.ErrorMessage).Should().BeEquivalentTo(
            $"For profile {data.Name}, 'qualities' must contain the quality mentioned in 'until_quality', " +
            $"which is '{data.UpgradesAllowed!.UntilQuality}'");
    }

    [Test]
    public void Quality_profile_qualities_cutoff_required()
    {
        var data = new QualityProfileConfigYaml
        {
            Name = "My QP",
            UpgradesAllowed = new QualityProfileFormatUpgradeYaml()
        };

        var validator = new QualityProfileConfigYamlValidator();
        var result = validator.TestValidate(data);

        result.ShouldHaveValidationErrorFor(x => x.UpgradesAllowed!.UntilQuality)
            .WithErrorMessage("'until_quality' is required when allowing profile upgrades");
    }

    [Test]
    public void Quality_profile_cutoff_must_not_reference_child_qualities()
    {
        var data = new QualityProfileConfigYaml
        {
            Name = "My QP",
            UpgradesAllowed = new QualityProfileFormatUpgradeYaml
            {
                UntilQuality = "Child Quality"
            },
            Qualities = new[]
            {
                new QualityProfileQualityConfigYaml
                {
                    Name = "Parent Group",
                    Qualities = new[] {"Child Quality"}
                }
            }
        };

        var validator = new QualityProfileConfigYamlValidator();
        var result = validator.TestValidate(data);

        result.ShouldHaveValidationErrorFor(x => x.Qualities);

        result.Errors.Select(x => x.ErrorMessage).Should().BeEquivalentTo(
            $"For profile {data.Name}, 'until_quality' must not refer to qualities contained within groups");
    }

    [Test]
    public void Quality_profile_qualities_must_have_no_duplicates()
    {
        var data = new QualityProfileConfigYaml
        {
            Name = "My QP",
            Qualities = new[]
            {
                new QualityProfileQualityConfigYaml {Name = "Dupe Quality"},
                new QualityProfileQualityConfigYaml {Name = "Dupe Quality"},
                new QualityProfileQualityConfigYaml {Name = "Dupe Quality"},
                new QualityProfileQualityConfigYaml {Name = "Dupe Quality 2"},
                new QualityProfileQualityConfigYaml {Name = "Dupe Quality 2"},
                new QualityProfileQualityConfigYaml {Name = "Dupe Quality 3"},
                new QualityProfileQualityConfigYaml {Name = "Dupe Quality 4"},
                new QualityProfileQualityConfigYaml
                {
                    Name = "Dupe Quality 3",
                    Qualities = new[] {"Dupe Quality 4"}
                }
            }
        };

        var validator = new QualityProfileConfigYamlValidator();
        var result = validator.TestValidate(data);

        result.ShouldHaveValidationErrorFor(x => x.Qualities);

        result.Errors.Select(x => x.ErrorMessage).Should().BeEquivalentTo(
            $"For profile {data.Name}, 'qualities' contains duplicates for quality 'Dupe Quality'",
            $"For profile {data.Name}, 'qualities' contains duplicates for quality 'Dupe Quality 2'",
            $"For profile {data.Name}, 'qualities' contains duplicates for quality 'Dupe Quality 3'",
            $"For profile {data.Name}, 'qualities' contains duplicates for quality 'Dupe Quality 4'");
    }

    [Test]
    public void Quality_profile_cutoff_quality_should_not_refer_to_disabled_qualities()
    {
        var data = new QualityProfileConfigYaml
        {
            Name = "My QP",
            UpgradesAllowed = new QualityProfileFormatUpgradeYaml
            {
                UntilQuality = "Test Quality"
            },
            Qualities = new[]
            {
                new QualityProfileQualityConfigYaml
                {
                    Name = "Test Quality",
                    Enabled = false
                }
            }
        };

        var validator = new QualityProfileConfigYamlValidator();
        var result = validator.TestValidate(data);

        result.ShouldHaveValidationErrorFor(x => x.Qualities);

        result.Errors.Select(x => x.ErrorMessage).Should().BeEquivalentTo(
            $"For profile {data.Name}, 'until_quality' must not refer to explicitly disabled qualities");
    }
}
