using FluentValidation.TestHelper;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Tests.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class YamlConfigValidatorTest : IntegrationFixture
{
    [Test]
    public void Validation_succeeds()
    {
        var config = new ServiceConfigYamlLatest
        {
            ApiKey = "valid",
            BaseUrl = "http://valid",
            CustomFormats = new List<CustomFormatConfigYamlLatest>
            {
                new()
                {
                    TrashIds = new List<string> {"01234567890123456789012345678901"},
                    QualityProfiles = new List<QualityScoreConfigYamlLatest>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualitySizeConfigYamlLatest
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigYamlValidatorLatest>();
        var result = validator.TestValidate(config);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validation_failure_when_api_key_missing()
    {
        var config = new ServiceConfigYamlLatest
        {
            ApiKey = "", // Must not be empty
            BaseUrl = "http://valid",
            CustomFormats = new List<CustomFormatConfigYamlLatest>
            {
                new()
                {
                    TrashIds = new[] {"valid"},
                    QualityProfiles = new List<QualityScoreConfigYamlLatest>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualitySizeConfigYamlLatest
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigYamlValidatorLatest>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.ApiKey);
    }

    [Test]
    public void Validation_failure_when_base_url_empty()
    {
        var config = new ServiceConfigYamlLatest
        {
            ApiKey = "valid",
            BaseUrl = "about:empty",
            CustomFormats = new List<CustomFormatConfigYamlLatest>
            {
                new()
                {
                    TrashIds = new[] {"valid"},
                    QualityProfiles = new List<QualityScoreConfigYamlLatest>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualitySizeConfigYamlLatest
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigYamlValidatorLatest>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.BaseUrl);
    }

    public static string FirstCf { get; } = $"{nameof(ServiceConfigYamlLatest.CustomFormats)}[0].";

    [Test]
    public void Validation_failure_when_cf_trash_ids_empty()
    {
        var config = new ServiceConfigYamlLatest
        {
            ApiKey = "valid",
            BaseUrl = "http://valid",
            CustomFormats = new List<CustomFormatConfigYamlLatest>
            {
                new()
                {
                    TrashIds = Array.Empty<string>(),
                    QualityProfiles = new List<QualityScoreConfigYamlLatest>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualitySizeConfigYamlLatest
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigYamlValidatorLatest>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(FirstCf + nameof(CustomFormatConfig.TrashIds));
    }

    [Test]
    public void Validation_failure_when_quality_definition_type_empty()
    {
        var config = new ServiceConfigYamlLatest
        {
            ApiKey = "valid",
            BaseUrl = "http://valid",
            CustomFormats = new List<CustomFormatConfigYamlLatest>
            {
                new()
                {
                    TrashIds = new List<string> {"valid"},
                    QualityProfiles = new List<QualityScoreConfigYamlLatest>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualitySizeConfigYamlLatest
            {
                Type = ""
            }
        };

        var validator = Resolve<ServiceConfigYamlValidatorLatest>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.QualityDefinition!.Type);
    }

    [Test]
    public void Validation_failure_when_quality_profile_name_empty()
    {
        var config = new ServiceConfigYamlLatest
        {
            ApiKey = "valid",
            BaseUrl = "http://valid",
            CustomFormats = new List<CustomFormatConfigYamlLatest>
            {
                new()
                {
                    TrashIds = new List<string> {"valid"},
                    QualityProfiles = new List<QualityScoreConfigYamlLatest>
                    {
                        new()
                        {
                            Name = ""
                        }
                    }
                }
            },
            QualityDefinition = new QualitySizeConfigYamlLatest
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigYamlValidatorLatest>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(FirstCf +
            $"{nameof(CustomFormatConfig.QualityProfiles)}[0].{nameof(QualityProfileScoreConfig.Name)}");
    }
}
