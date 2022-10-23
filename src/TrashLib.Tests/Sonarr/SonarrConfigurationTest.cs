using FluentAssertions;
using FluentValidation;
using NUnit.Framework;
using Recyclarr.TestLibrary;
using TrashLib.Services.Sonarr.Config;

namespace TrashLib.Tests.Sonarr;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrConfigurationTest : IntegrationFixture
{
    [Test]
    public void Validation_fails_for_all_missing_required_properties()
    {
        // default construct which should yield default values (invalid) for all required properties
        var config = new SonarrConfiguration
        {
            BaseUrl = "valid",
            ApiKey = "valid",
            // validation is only applied to actual release profile elements. Not if it's empty.
            ReleaseProfiles = new[] {new ReleaseProfileConfig()}
        };

        var validator = ServiceLocator.Resolve<IValidator<SonarrConfiguration>>();

        var result = validator.Validate(config);

        var messages = new SonarrValidationMessages();
        var expectedErrorMessageSubstrings = new[]
        {
            messages.ReleaseProfileTrashIds
        };

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage)
            .Should().BeEquivalentTo(expectedErrorMessageSubstrings);
    }

    [Test]
    public void Validation_succeeds_when_no_missing_required_properties()
    {
        var config = new SonarrConfiguration
        {
            ApiKey = "required value",
            BaseUrl = "required value",
            ReleaseProfiles = new List<ReleaseProfileConfig>
            {
                new() {TrashIds = new[] {"123"}}
            }
        };

        var validator = ServiceLocator.Resolve<IValidator<SonarrConfiguration>>();
        var result = validator.Validate(config);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
