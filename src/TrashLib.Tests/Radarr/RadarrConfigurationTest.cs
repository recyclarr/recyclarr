using System;
using System.Collections;
using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Trash.Config;
using TrashLib.Config;
using TrashLib.Radarr;
using YamlDotNet.Core;
using YamlDotNet.Serialization.ObjectFactories;

namespace TrashLib.Tests.Radarr
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class RadarrConfigurationTest
    {
        public static IEnumerable GetTrashIdsOrNamesEmptyTestData()
        {
            yield return new TestCaseData(@"
radarr:
  - api_key: abc
    base_url: xyz
    custom_formats:
      - names: [foo]
        quality_profiles:
          - name: MyProfile
")
                .SetName("{m} (without_trash_ids)");

            yield return new TestCaseData(@"
radarr:
  - api_key: abc
    base_url: xyz
    custom_formats:
      - trash_ids: [abc123]
        quality_profiles:
          - name: MyProfile
")
                .SetName("{m} (without_names)");
        }

        [TestCaseSource(nameof(GetTrashIdsOrNamesEmptyTestData))]
        public void Custom_format_either_names_or_trash_id_not_empty_is_ok(string testYaml)
        {
            var configLoader = new ConfigurationLoader<RadarrConfiguration>(
                Substitute.For<IConfigurationProvider>(),
                Substitute.For<IFileSystem>(), new DefaultObjectFactory());

            Action act = () => configLoader.LoadFromStream(new StringReader(testYaml), "radarr");

            act.Should().NotThrow();
        }

        [Test]
        public void Custom_format_names_and_trash_ids_lists_must_not_both_be_empty()
        {
            var testYaml = @"
radarr:
  - api_key: abc
    base_url: xyz
    custom_formats:
      - quality_profiles:
          - name: MyProfile
";
            var configLoader = new ConfigurationLoader<RadarrConfiguration>(
                Substitute.For<IConfigurationProvider>(),
                Substitute.For<IFileSystem>(), new DefaultObjectFactory());

            Action act = () => configLoader.LoadFromStream(new StringReader(testYaml), "radarr");

            act.Should().Throw<ConfigurationException>()
                .WithMessage("*must contain at least one element in either 'names' or 'trash_ids'.");
        }

        [Test]
        public void Quality_definition_type_is_required()
        {
            const string yaml = @"
radarr:
- base_url: a
  api_key: b
  quality_definition:
    preferred_ratio: 0.5
";
            var loader = new ConfigurationLoader<RadarrConfiguration>(
                Substitute.For<IConfigurationProvider>(),
                Substitute.For<IFileSystem>(),
                new DefaultObjectFactory());

            Action act = () => loader.LoadFromStream(new StringReader(yaml), "radarr");

            act.Should().Throw<YamlException>()
                .WithMessage("*'type' is required for 'quality_definition'");
        }

        [Test]
        public void Quality_profile_name_is_required()
        {
            const string testYaml = @"
radarr:
  - api_key: abc
    base_url: xyz
    custom_formats:
      - names: [one, two]
        quality_profiles:
          - score: 100
";

            var configLoader = new ConfigurationLoader<RadarrConfiguration>(
                Substitute.For<IConfigurationProvider>(),
                Substitute.For<IFileSystem>(), new DefaultObjectFactory());

            Action act = () => configLoader.LoadFromStream(new StringReader(testYaml), "radarr");

            act.Should().Throw<YamlException>();
        }
    }
}
