using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Autofac;
using FluentAssertions;
using FluentValidation;
using NUnit.Framework;
using TrashLib.Config;
using TrashLib.Radarr;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.QualityDefinition;

namespace TrashLib.Tests.Radarr
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class RadarrConfigurationTest
    {
        private IContainer _container = default!;

        [OneTimeSetUp]
        public void Setup()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<ConfigAutofacModule>();
            builder.RegisterModule<RadarrAutofacModule>();
            _container = builder.Build();
        }

        private static readonly TestCaseData[] NameOrIdsTestData =
        {
            new(new Collection<string> {"name"}, new Collection<string>()),
            new(new Collection<string>(), new Collection<string> {"trash_id"})
        };

        [TestCaseSource(nameof(NameOrIdsTestData))]
        public void Custom_format_is_valid_with_one_of_either_names_or_trash_id(Collection<string> namesList,
            Collection<string> trashIdsList)
        {
            var config = new RadarrConfiguration
            {
                ApiKey = "required value",
                BaseUrl = "required value",
                CustomFormats = new List<CustomFormatConfig>
                {
                    new() {Names = namesList, TrashIds = trashIdsList}
                }
            };

            var validator = _container.Resolve<IValidator<RadarrConfiguration>>();
            var result = validator.Validate(config);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public void Validation_fails_for_all_missing_required_properties()
        {
            // default construct which should yield default values (invalid) for all required properties
            var config = new RadarrConfiguration();
            var validator = _container.Resolve<IValidator<RadarrConfiguration>>();

            var result = validator.Validate(config);

            var expectedErrorMessageSubstrings = new[]
            {
                "Property 'base_url' is required",
                "Property 'api_key' is required",
                "'custom_formats' elements must contain at least one element in either 'names' or 'trash_ids'",
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
                        Names = new List<string> {"required value"},
                        QualityProfiles = new List<QualityProfileConfig>
                        {
                            new() {Name = "required value"}
                        }
                    }
                },
                QualityDefinition = new QualityDefinitionConfig
                {
                    Type = RadarrQualityDefinitionType.Movie
                }
            };

            var validator = _container.Resolve<IValidator<RadarrConfiguration>>();
            var result = validator.Validate(config);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
    }
}
