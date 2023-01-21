using FluentValidation.TestHelper;
using NUnit.Framework;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ServiceConfigurationValidatorTest : IntegrationFixture
{
    [Test]
    public void Validation_succeeds()
    {
        var config = new TestConfig
        {
            ApiKey = "valid",
            BaseUrl = new Uri("http://valid"),
            InstanceName = "valid",
            LineNumber = 1,
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
                {
                    TrashIds = new List<string> {"valid"},
                    QualityProfiles = new List<QualityProfileScoreConfig>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualityDefinitionConfig
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigurationValidator>();
        var result = validator.TestValidate(config);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validation_failure_when_api_key_missing()
    {
        var config = new TestConfig
        {
            ApiKey = "", // Must not be empty
            BaseUrl = new Uri("http://valid"),
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
                {
                    TrashIds = new[] {"valid"},
                    QualityProfiles = new List<QualityProfileScoreConfig>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualityDefinitionConfig
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigurationValidator>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.ApiKey);
    }

    [Test]
    public void Validation_failure_when_base_url_empty()
    {
        var config = new TestConfig
        {
            ApiKey = "valid",
            BaseUrl = new Uri("about:empty"),
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
                {
                    TrashIds = new[] {"valid"},
                    QualityProfiles = new List<QualityProfileScoreConfig>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualityDefinitionConfig
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigurationValidator>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.BaseUrl);
    }

    public static string FirstCf { get; } = $"{nameof(TestConfig.CustomFormats)}[0].";

    [Test]
    public void Validation_failure_when_cf_trash_ids_empty()
    {
        var config = new TestConfig
        {
            ApiKey = "valid",
            BaseUrl = new Uri("http://valid"),
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
                {
                    TrashIds = Array.Empty<string>(),
                    QualityProfiles = new List<QualityProfileScoreConfig>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualityDefinitionConfig
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigurationValidator>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(FirstCf + nameof(CustomFormatConfig.TrashIds));
    }

    [Test]
    public void Validation_failure_when_quality_definition_type_empty()
    {
        var config = new TestConfig
        {
            ApiKey = "valid",
            BaseUrl = new Uri("http://valid"),
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
                {
                    TrashIds = new List<string> {"valid"},
                    QualityProfiles = new List<QualityProfileScoreConfig>
                    {
                        new()
                        {
                            Name = "valid"
                        }
                    }
                }
            },
            QualityDefinition = new QualityDefinitionConfig
            {
                Type = ""
            }
        };

        var validator = Resolve<ServiceConfigurationValidator>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.QualityDefinition!.Type);
    }

    [Test]
    public void Validation_failure_when_quality_profile_name_empty()
    {
        var config = new TestConfig
        {
            ApiKey = "valid",
            BaseUrl = new Uri("http://valid"),
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
                {
                    TrashIds = new List<string> {"valid"},
                    QualityProfiles = new List<QualityProfileScoreConfig>
                    {
                        new()
                        {
                            Name = ""
                        }
                    }
                }
            },
            QualityDefinition = new QualityDefinitionConfig
            {
                Type = "valid"
            }
        };

        var validator = Resolve<ServiceConfigurationValidator>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(FirstCf +
            $"{nameof(CustomFormatConfig.QualityProfiles)}[0].{nameof(QualityProfileScoreConfig.Name)}");
    }

    [Test]
    public void Validation_failure_when_instance_name_empty()
    {
        var config = new TestConfig
        {
            InstanceName = ""
        };

        var validator = Resolve<ServiceConfigurationValidator>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.InstanceName);
    }

    [Test]
    public void Validation_failure_when_line_number_equals_zero()
    {
        var config = new TestConfig
        {
            LineNumber = 0
        };

        var validator = Resolve<ServiceConfigurationValidator>();
        var result = validator.TestValidate(config);

        result.ShouldHaveValidationErrorFor(x => x.LineNumber);
    }
}
