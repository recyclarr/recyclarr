using Autofac;
using FluentAssertions;
using FluentValidation;
using NUnit.Framework;
using TrashLib.Config;
using TrashLib.Sonarr;
using TrashLib.Sonarr.Config;
using TrashLib.Sonarr.ReleaseProfile;

namespace TrashLib.Tests.Sonarr;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrConfigurationTest
{
    private IContainer _container = default!;

    [OneTimeSetUp]
    public void Setup()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<ConfigAutofacModule>();
        builder.RegisterModule<SonarrAutofacModule>();
        _container = builder.Build();
    }

    [Test]
    public void Validation_fails_for_all_missing_required_properties()
    {
        // default construct which should yield default values (invalid) for all required properties
        var config = new SonarrConfiguration();
        var validator = _container.Resolve<IValidator<SonarrConfiguration>>();

        var result = validator.Validate(config);

        var expectedErrorMessageSubstrings = new[]
        {
            "Property 'base_url' is required",
            "Property 'api_key' is required",
            "'type' is required for 'release_profiles' elements"
        };

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.ErrorMessage).Should()
            .OnlyContain(x => expectedErrorMessageSubstrings.Any(x.Contains));
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
                new() {Type = ReleaseProfileType.Anime}
            }
        };

        var validator = _container.Resolve<IValidator<SonarrConfiguration>>();
        var result = validator.Validate(config);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
