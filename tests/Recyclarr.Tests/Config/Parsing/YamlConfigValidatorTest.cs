using FluentValidation.TestHelper;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;

namespace Recyclarr.Tests.Config.Parsing;

[TestFixture]
public class YamlConfigValidatorTest
{
    [Test]
    public void Validation_succeeds()
    {
        var config = new ServiceConfigYaml
        {
            ApiKey = "valid",
            BaseUrl = "http://valid",
            CustomFormats = new List<CustomFormatConfigYaml>
            {
                new()
                {
                    TrashIds = new List<string> { "01234567890123456789012345678901" },
                    AssignScoresTo = new List<QualityScoreConfigYaml> { new() { Name = "valid" } },
                },
            },
            QualityDefinition = new QualitySizeConfigYaml { Type = "valid" },
        };

        var validator = new ServiceConfigYamlValidator();
        var result = validator.TestValidate(config);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validation_failure_when_api_key_missing()
    {
        var config = new ServiceConfigYaml
        {
            ApiKey = "", // Must not be empty
            BaseUrl = "http://valid",
            CustomFormats = new List<CustomFormatConfigYaml>
            {
                new()
                {
                    TrashIds = ["valid"],
                    AssignScoresTo = new List<QualityScoreConfigYaml> { new() { Name = "valid" } },
                },
            },
            QualityDefinition = new QualitySizeConfigYaml { Type = "valid" },
        };

        var validator = new ServiceConfigYamlValidator();
        var result = validator.TestValidate(
            config,
            o => o.IncludeRuleSets(YamlValidatorRuleSets.RootConfig)
        );

        result.ShouldHaveValidationErrorFor(x => x.ApiKey);
    }

    [Test]
    public void Validation_failure_when_base_url_empty()
    {
        var config = new ServiceConfigYaml
        {
            ApiKey = "valid",
            BaseUrl = "",
            CustomFormats = new List<CustomFormatConfigYaml>
            {
                new()
                {
                    TrashIds = ["valid"],
                    AssignScoresTo = new List<QualityScoreConfigYaml> { new() { Name = "valid" } },
                },
            },
            QualityDefinition = new QualitySizeConfigYaml { Type = "valid" },
        };

        var validator = new ServiceConfigYamlValidator();
        var result = validator.TestValidate(
            config,
            o => o.IncludeRuleSets(YamlValidatorRuleSets.RootConfig)
        );

        result
            .ShouldHaveValidationErrorFor(x => x.BaseUrl)
            .WithErrorMessage("'base_url' must not be empty.");
    }

    [Test]
    public void Validation_failure_when_base_url_not_start_with_http()
    {
        var config = new ServiceConfigYaml
        {
            ApiKey = "valid",
            BaseUrl = "ftp://foo.com",
            CustomFormats = new List<CustomFormatConfigYaml>
            {
                new()
                {
                    TrashIds = ["valid"],
                    AssignScoresTo = new List<QualityScoreConfigYaml> { new() { Name = "valid" } },
                },
            },
            QualityDefinition = new QualitySizeConfigYaml { Type = "valid" },
        };

        var validator = new ServiceConfigYamlValidator();
        var result = validator.TestValidate(
            config,
            o => o.IncludeRuleSets(YamlValidatorRuleSets.RootConfig)
        );

        result
            .ShouldHaveValidationErrorFor(x => x.BaseUrl)
            .WithErrorMessage("base_url must start with 'http' or 'https'");
    }

    private static string FirstCf => $"{nameof(ServiceConfigYaml.CustomFormats)}[0].";

    [Test]
    public void Validation_failure_when_quality_definition_type_empty()
    {
        var config = new ServiceConfigYaml
        {
            ApiKey = "valid",
            BaseUrl = "http://valid",
            CustomFormats = new List<CustomFormatConfigYaml>
            {
                new()
                {
                    TrashIds = new List<string> { "valid" },
                    AssignScoresTo = new List<QualityScoreConfigYaml> { new() { Name = "valid" } },
                },
            },
            QualityDefinition = new QualitySizeConfigYaml { Type = "" },
        };

        var validator = new ServiceConfigYamlValidator();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.QualityDefinition!.Type);
    }

    [Test]
    public void Validation_failure_when_quality_profile_name_empty()
    {
        var config = new ServiceConfigYaml
        {
            ApiKey = "valid",
            BaseUrl = "http://valid",
            CustomFormats = new List<CustomFormatConfigYaml>
            {
                new()
                {
                    TrashIds = new List<string> { "valid" },
                    AssignScoresTo = new List<QualityScoreConfigYaml> { new() { Name = "" } },
                },
            },
            QualityDefinition = new QualitySizeConfigYaml { Type = "valid" },
        };

        var validator = new ServiceConfigYamlValidator();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(
            FirstCf
                + $"{nameof(CustomFormatConfig.AssignScoresTo)}[0].{nameof(AssignScoresToConfig.Name)}"
        );
    }

    [Test]
    public void Validation_failure_when_base_url_invalid()
    {
        var config = new ServiceConfigYaml { BaseUrl = "http:/invalid" };

        var validator = new ServiceConfigYamlValidator();
        var result = validator.TestValidate(
            config,
            s => s.IncludeRuleSets(YamlValidatorRuleSets.RootConfig)
        );

        result
            .ShouldHaveValidationErrorFor(x => x.BaseUrl)
            .WithErrorMessage("base_url must be a valid URL");
    }
}
