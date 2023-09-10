using FluentValidation.TestHelper;
using Recyclarr.TrashLib.Config.Parsing;

namespace Recyclarr.TrashLib.Config.Tests.Parsing;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigYamlDataObjectsValidationTest
{
    [Test]
    public void Quality_profile_format_upgrade_allowed_required()
    {
        var data = new QualityProfileConfigYaml
        {
            Name = "My QP",
            Upgrade = new QualityProfileFormatUpgradeYaml()
        };

        var validator = new QualityProfileFormatUpgradeYamlValidator(data);
        var result = validator.TestValidate(data.Upgrade);

        result.ShouldHaveValidationErrorFor(x => x.Allowed).WithErrorMessage(
            $"For profile {data.Name}, 'allowed' under 'upgrade' is required. " +
            $"If you don't want Recyclarr to manage upgrades, delete the whole 'upgrade' block.");
    }

    [Test]
    public void Quality_profile_format_upgrade_until_quality_required()
    {
        var data = new QualityProfileConfigYaml
        {
            Name = "My QP",
            Upgrade = new QualityProfileFormatUpgradeYaml
            {
                Allowed = true
            },
            Qualities = new List<QualityProfileQualityConfigYaml>()
        };

        var validator = new QualityProfileFormatUpgradeYamlValidator(data);
        var result = validator.TestValidate(data.Upgrade);

        result.ShouldHaveValidationErrorFor(x => x.UntilQuality).WithErrorMessage(
            $"For profile {data.Name}, 'until_quality' is required when 'allowed' is set to 'true' and " +
            $"an explicit 'qualities' list is provided.");
    }

    [Test]
    public void Quality_profile_name_required()
    {
        var data = new QualityProfileConfigYaml();

        var validator = new QualityProfileConfigYamlValidator();
        var result = validator.TestValidate(data);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Quality_profile_qualities_must_have_cutoff_quality()
    {
        var data = new QualityProfileConfigYaml
        {
            Name = "My QP",
            Upgrade = new QualityProfileFormatUpgradeYaml
            {
                Allowed = true,
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
            $"which is '{data.Upgrade!.UntilQuality}'");
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
                new QualityProfileQualityConfigYaml {Name = "Dupe Quality 3"},
                new QualityProfileQualityConfigYaml
                {
                    Name = "Dupe Quality 2",
                    Qualities = new[] {"Dupe Quality 3"}
                },
                new QualityProfileQualityConfigYaml
                {
                    Name = "Dupe Quality 4",
                    Qualities = new[] {"Dupe Quality 5"}
                },
                new QualityProfileQualityConfigYaml
                {
                    Name = "Dupe Quality 4",
                    Qualities = new[] {"Dupe Quality 5"}
                }
            }
        };

        var validator = new QualityProfileConfigYamlValidator();
        var result = validator.TestValidate(data);

        result.ShouldHaveValidationErrorFor(x => x.Qualities);

        result.Errors.Select(x => x.ErrorMessage).Should().BeEquivalentTo(
            $"For profile {data.Name}, 'qualities' contains duplicates for quality 'Dupe Quality'",
            $"For profile {data.Name}, 'qualities' contains duplicates for quality 'Dupe Quality 3'",
            $"For profile {data.Name}, 'qualities' contains duplicates for quality group 'Dupe Quality 4'",
            $"For profile {data.Name}, 'qualities' contains duplicates for quality 'Dupe Quality 5'");
    }

    [Test]
    public void Quality_profile_qualities_must_have_at_least_one_enabled()
    {
        var data = new QualityProfileConfigYaml
        {
            Name = "My QP",
            Qualities = new[]
            {
                new QualityProfileQualityConfigYaml {Name = "Quality 1", Enabled = false},
                new QualityProfileQualityConfigYaml {Name = "Quality 2", Enabled = false}
            }
        };

        var validator = new QualityProfileConfigYamlValidator();
        var result = validator.TestValidate(data);

        result.ShouldHaveValidationErrorFor(x => x.Qualities);

        result.Errors.Select(x => x.ErrorMessage).Should().BeEquivalentTo(
            $"For profile {data.Name}, at least one explicitly listed quality under 'qualities' must be enabled.");
    }

    [Test]
    public void Quality_profile_cutoff_quality_should_not_refer_to_disabled_qualities()
    {
        var data = new QualityProfileConfigYaml
        {
            Name = "My QP",
            Upgrade = new QualityProfileFormatUpgradeYaml
            {
                Allowed = true,
                UntilQuality = "Disabled Quality"
            },
            Qualities = new[]
            {
                new QualityProfileQualityConfigYaml {Name = "Enabled Quality"},
                new QualityProfileQualityConfigYaml {Name = "Disabled Quality", Enabled = false}
            }
        };

        var validator = new QualityProfileConfigYamlValidator();
        var result = validator.TestValidate(data);

        result.ShouldHaveValidationErrorFor(x => x.Qualities);

        result.Errors.Select(x => x.ErrorMessage).Should().BeEquivalentTo(
            $"For profile {data.Name}, 'until_quality' must not refer to explicitly disabled qualities");
    }
}
