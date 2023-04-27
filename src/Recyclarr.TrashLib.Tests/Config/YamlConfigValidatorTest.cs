using FluentValidation.TestHelper;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class YamlConfigValidatorTest : TrashLibIntegrationFixture
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
                    TrashIds = new List<string> {"01234567890123456789012345678901"},
                    QualityProfiles = new List<QualityScoreConfigYaml>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualitySizeConfigYaml
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigYamlValidator>();
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
                    TrashIds = new[] {"valid"},
                    QualityProfiles = new List<QualityScoreConfigYaml>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualitySizeConfigYaml
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigYamlValidator>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.ApiKey);
    }

    [Test]
    public void Validation_failure_when_base_url_empty()
    {
        var config = new ServiceConfigYaml
        {
            ApiKey = "valid",
            BaseUrl = "about:empty",
            CustomFormats = new List<CustomFormatConfigYaml>
            {
                new()
                {
                    TrashIds = new[] {"valid"},
                    QualityProfiles = new List<QualityScoreConfigYaml>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualitySizeConfigYaml
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigYamlValidator>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.BaseUrl);
    }

    public static string FirstCf { get; } = $"{nameof(ServiceConfigYaml.CustomFormats)}[0].";

    [Test]
    public void Validation_failure_when_cf_trash_ids_empty()
    {
        var config = new ServiceConfigYaml
        {
            ApiKey = "valid",
            BaseUrl = "http://valid",
            CustomFormats = new List<CustomFormatConfigYaml>
            {
                new()
                {
                    TrashIds = Array.Empty<string>(),
                    QualityProfiles = new List<QualityScoreConfigYaml>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualitySizeConfigYaml
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigYamlValidator>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(FirstCf + nameof(CustomFormatConfig.TrashIds));
    }

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
                    TrashIds = new List<string> {"valid"},
                    QualityProfiles = new List<QualityScoreConfigYaml>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualitySizeConfigYaml
            {
                Type = ""
            }
        };

        var validator = Resolve<ServiceConfigYamlValidator>();
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
                    TrashIds = new List<string> {"valid"},
                    QualityProfiles = new List<QualityScoreConfigYaml>
                    {
                        new()
                        {
                            Name = ""
                        }
                    }
                }
            },
            QualityDefinition = new QualitySizeConfigYaml
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigYamlValidator>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(FirstCf +
            $"{nameof(CustomFormatConfig.QualityProfiles)}[0].{nameof(QualityProfileScoreConfig.Name)}");
    }
}
