using System.Collections.ObjectModel;
using Autofac;
using FluentAssertions;
using FluentValidation;
using NUnit.Framework;
using Recyclarr.TestLibrary;
using TrashLib.Config.Services;
using TrashLib.Services.Radarr.Config;

namespace TrashLib.Tests.Radarr;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class RadarrConfigurationTest : IntegrationFixture
{
    [Test]
    public void Custom_format_is_valid_with_trash_id()
    {
        var config = new RadarrConfiguration
        {
            ApiKey = "required value",
            BaseUrl = "required value",
            CustomFormats = new List<CustomFormatConfig>
            {
                new() {TrashIds = new Collection<string> {"trash_id"}}
            }
        };

        var validator = Container.Resolve<IValidator<RadarrConfiguration>>();
        var result = validator.Validate(config);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validation_fails_for_all_missing_required_properties()
    {
        // default construct which should yield default values (invalid) for all required properties
        var config = new RadarrConfiguration();
        var validator = Container.Resolve<IValidator<RadarrConfiguration>>();

        var result = validator.Validate(config);

        var expectedErrorMessageSubstrings = new[]
        {
            "Property 'base_url' is required",
            "Property 'api_key' is required",
            "'custom_formats' elements must contain at least one element under 'trash_ids'",
            "'name' is required for elements under 'quality_profiles'",
            "'type' is required for 'quality_definition'"
        };

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should()
            .OnlyContain(x => expectedErrorMessageSubstrings.Any(x.Contains));
    }

    [Test]
    public void Validation_succeeds_when_no_missing_required_properties()
    {
        var config = new RadarrConfiguration
        {
            ApiKey = "required value",
            BaseUrl = "required value",
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
                {
                    TrashIds = new List<string> {"required value"},
                    QualityProfiles = new List<QualityProfileScoreConfig>
                    {
                        new() {Name = "required value"}
                    }
                }
            },
            QualityDefinition = new QualityDefinitionConfig
            {
                Type = "movie"
            }
        };

        var validator = Container.Resolve<IValidator<RadarrConfiguration>>();
        var result = validator.Validate(config);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
