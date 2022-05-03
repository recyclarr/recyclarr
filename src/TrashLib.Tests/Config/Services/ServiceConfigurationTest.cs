using Autofac;
using FluentAssertions;
using FluentValidation;
using NUnit.Framework;
using Recyclarr.TestLibrary;
using TrashLib.Config.Services;
using TrashLib.TestLibrary;

namespace TrashLib.Tests.Config.Services;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ServiceConfigurationTest : IntegrationFixture
{
    [Test]
    public void Validation_fails_for_all_missing_required_properties()
    {
        // default construct which should yield default values (invalid) for all required properties
        var config = new TestConfig();

        var validator = Container.Resolve<IValidator<ServiceConfiguration>>();

        var result = validator.Validate(config);

        var messages = new ServiceValidationMessages();
        var expectedErrorMessageSubstrings = new[]
        {
            messages.ApiKey,
            messages.BaseUrl
        };

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage)
            .Should().BeEquivalentTo(expectedErrorMessageSubstrings);
    }

    [Test]
    public void Fail_when_trash_ids_missing()
    {
        var config = new TestConfig
        {
            BaseUrl = "valid",
            ApiKey = "valid",
            CustomFormats = new List<CustomFormatConfig>
            {
                new() // Empty to force validation failure
            }
        };

        var validator = Container.Resolve<IValidator<ServiceConfiguration>>();

        var result = validator.Validate(config);

        var messages = new ServiceValidationMessages();
        var expectedErrorMessageSubstrings = new[]
        {
            messages.CustomFormatTrashIds
        };

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage)
            .Should().BeEquivalentTo(expectedErrorMessageSubstrings);
    }
}
