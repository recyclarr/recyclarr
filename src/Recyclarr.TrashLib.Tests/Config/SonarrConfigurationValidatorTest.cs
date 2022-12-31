using FluentValidation.TestHelper;
using NUnit.Framework;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.Sonarr;
using Recyclarr.TrashLib.Services.Sonarr.Config;

namespace Recyclarr.TrashLib.Tests.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrConfigurationValidatorTest
{
    [Test]
    public void Sonarr_v4_succeeds()
    {
        var config = new SonarrConfiguration
        {
            ApiKey = "valid",
            BaseUrl = "valid",
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

        var capabilities = new SonarrCapabilities
        {
            SupportsCustomFormats = true,
            SupportsNamedReleaseProfiles = true
        };
        var validator = new SonarrConfigurationValidator(capabilities);
        var result = validator.TestValidate(config);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Sonarr_v3_succeeds()
    {
        var config = new SonarrConfiguration
        {
            ApiKey = "valid",
            BaseUrl = "valid",
            ReleaseProfiles = new List<ReleaseProfileConfig>
            {
                new()
                {
                    TrashIds = new List<string> {"valid"},
                    Filter = new SonarrProfileFilterConfig {Include = new[] {"valid"}},
                    Tags = new[] {"valid"}
                }
            },
            QualityDefinition = new QualityDefinitionConfig
            {
                Type = "valid"
            }
        };

        var capabilities = new SonarrCapabilities
        {
            SupportsCustomFormats = false,
            SupportsNamedReleaseProfiles = true
        };
        var validator = new SonarrConfigurationValidator(capabilities);
        var result = validator.TestValidate(config);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Sonarr_v4_failures()
    {
        var config = new SonarrConfiguration
        {
            ReleaseProfiles = new List<ReleaseProfileConfig> {new()}
        };

        var capabilities = new SonarrCapabilities {SupportsCustomFormats = true};
        var validator = new SonarrConfigurationValidator(capabilities);
        var result = validator.TestValidate(config);

        // Release profiles not allowed in v4
        result.ShouldHaveValidationErrorFor(x => x.ReleaseProfiles);
    }

    [Test]
    public void Sonarr_v3_failures()
    {
        var config = new SonarrConfiguration
        {
            CustomFormats = new List<CustomFormatConfig> {new()},
            ReleaseProfiles = new List<ReleaseProfileConfig>
            {
                new()
                {
                    TrashIds = Array.Empty<string>(),
                    Filter = new SonarrProfileFilterConfig
                    {
                        Include = new[] {"include"},
                        Exclude = new[] {"exclude"}
                    }
                }
            }
        };

        var capabilities = new SonarrCapabilities
        {
            SupportsCustomFormats = false,
            SupportsNamedReleaseProfiles = false
        };

        var validator = new SonarrConfigurationValidator(capabilities);
        var result = validator.TestValidate(config);

        // Custom formats not allowed in v3
        result.ShouldHaveValidationErrorFor(x => x.CustomFormats);

        // Due to named release profiles not being supported (minimum version requirement not met)
        result.ShouldHaveValidationErrorFor(x => x);

        var releaseProfiles = $"{nameof(config.ReleaseProfiles)}[0].";

        // Release profile trash IDs cannot be empty
        result.ShouldHaveValidationErrorFor(releaseProfiles + nameof(ReleaseProfileConfig.TrashIds));

        // Cannot use include + exclude filters together
        result.ShouldHaveValidationErrorFor(releaseProfiles +
            $"{nameof(ReleaseProfileConfig.Filter)}.{nameof(SonarrProfileFilterConfig.Include)}");
    }
}
