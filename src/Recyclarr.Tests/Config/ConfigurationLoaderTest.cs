using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using AutoFixture.NUnit3;
using Common;
using Common.Extensions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Config;
using Recyclarr.TestLibrary;
using TestLibrary.AutoFixture;
using TrashLib.Services.Sonarr.Config;
using TrashLib.TestLibrary;
using YamlDotNet.Core;

namespace Recyclarr.Tests.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigurationLoaderTest : IntegrationFixture
{
    private static TextReader GetResourceData(string file)
    {
        var testData = new ResourceDataReader(typeof(ConfigurationLoaderTest), "Data");
        return new StringReader(testData.ReadData(file));
    }

    [Test]
    public void Load_many_iterations_of_config()
    {
        static string MockYaml(params object[] args)
        {
            var str = new StringBuilder("sonarr:");
            const string templateYaml = "\n  - base_url: {0}\n    api_key: abc";
            str.Append(args.Aggregate("", (current, p) => current + templateYaml.FormatWith(p)));
            return str.ToString();
        }

        var baseDir = Fs.CurrentDirectory();
        var fileData = new (string, string)[]
        {
            (baseDir.File("config1.yml").FullName, MockYaml(1, 2)),
            (baseDir.File("config2.yml").FullName, MockYaml(3))
        };

        foreach (var (file, data) in fileData)
        {
            Fs.AddFile(file, new MockFileData(data));
        }

        var expected = new List<SonarrConfiguration>
        {
            new() {ApiKey = "abc", BaseUrl = "1"},
            new() {ApiKey = "abc", BaseUrl = "2"},
            new() {ApiKey = "abc", BaseUrl = "3"}
        };

        var loader = Resolve<IConfigurationLoader<SonarrConfiguration>>();
        var actual = loader.LoadMany(fileData.Select(x => x.Item1), "sonarr").ToList();

        actual.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void Parse_using_stream()
    {
        var configLoader = Resolve<ConfigurationLoader<SonarrConfiguration>>();
        var configs = configLoader.LoadFromStream(GetResourceData("Load_UsingStream_CorrectParsing.yml"), "sonarr");

        configs.Should().BeEquivalentTo(new List<SonarrConfiguration>
        {
            new()
            {
                ApiKey = "95283e6b156c42f3af8a9b16173f876b",
                BaseUrl = "http://localhost:8989",
                Name = "name",
                ReleaseProfiles = new List<ReleaseProfileConfig>
                {
                    new()
                    {
                        TrashIds = new[] {"123"},
                        StrictNegativeScores = true,
                        Tags = new List<string> {"anime"}
                    },
                    new()
                    {
                        TrashIds = new[] {"456"},
                        StrictNegativeScores = false,
                        Tags = new List<string>
                        {
                            "tv",
                            "series"
                        }
                    }
                }
            }
        });
    }

    [Test, AutoMockData]
    public void Throw_when_validation_fails(
        [Frozen] IValidator<TestConfig> validator,
        ConfigurationLoader<TestConfig> configLoader)
    {
        // force the validator to return a validation error
        validator.Validate(Arg.Any<TestConfig>()).Returns(new ValidationResult
        {
            Errors = {new ValidationFailure("PropertyName", "Test Validation Failure")}
        });

        const string testYml = @"
fubar:
- api_key: abc
";
        Action act = () => configLoader.LoadFromStream(new StringReader(testYml), "fubar");

        act.Should().Throw<ConfigurationException>();
    }

    [Test, AutoMockData]
    public void Validation_success_does_not_throw(ConfigurationLoader<TestConfig> configLoader)
    {
        const string testYml = @"
fubar:
- api_key: abc
";
        Action act = () => configLoader.LoadFromStream(new StringReader(testYml), "fubar");
        act.Should().NotThrow();
    }

    [Test]
    public void Test_secret_loading()
    {
        var configLoader = Resolve<ConfigurationLoader<SonarrConfiguration>>();

        const string testYml = @"
sonarr:
- api_key: !secret api_key
  base_url: !secret 123GARBAGE_
  release_profiles:
      - trash_ids:
          - !secret secret_rp
";

        const string secretsYml = @"
api_key: 95283e6b156c42f3af8a9b16173f876b
123GARBAGE_: 'https://radarr:7878'
secret_rp: 1234567
";

        Fs.AddFile(Paths.SecretsPath.FullName, new MockFileData(secretsYml));
        var expected = new List<SonarrConfiguration>
        {
            new()
            {
                ApiKey = "95283e6b156c42f3af8a9b16173f876b",
                BaseUrl = "https://radarr:7878",
                ReleaseProfiles = new List<ReleaseProfileConfig>
                {
                    new()
                    {
                        TrashIds = new[] {"1234567"}
                    }
                }
            }
        };

        var parsedSecret = configLoader.LoadFromStream(new StringReader(testYml), "sonarr");
        parsedSecret.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void Throw_when_referencing_invalid_secret()
    {
        var configLoader = Resolve<ConfigurationLoader<SonarrConfiguration>>();

        const string testYml = @"
sonarr:
- api_key: !secret api_key
  base_url: fake_url
";

        const string secretsYml = @"
no_api_key: 95283e6b156c42f3af8a9b16173f876b
";

        Fs.AddFile(Paths.SecretsPath.FullName, new MockFileData(secretsYml));

        Action act = () => configLoader.LoadFromStream(new StringReader(testYml), "sonarr");
        act.Should().Throw<YamlException>().WithMessage("api_key is not defined in secrets.yml.");
    }

    [Test]
    public void Throw_when_referencing_secret_without_secrets_file()
    {
        var configLoader = Resolve<ConfigurationLoader<SonarrConfiguration>>();

        const string testYml = @"
sonarr:
- api_key: !secret api_key
  base_url: fake_url
";

        Action act = () => configLoader.LoadFromStream(new StringReader(testYml), "sonarr");
        act.Should().Throw<YamlException>().WithMessage("api_key is not defined in secrets.yml.");
    }

    [Test]
    public void Throw_when_secret_value_is_not_scalar()
    {
        var configLoader = Resolve<ConfigurationLoader<SonarrConfiguration>>();

        const string testYml = @"
sonarr:
- api_key: !secret { property: value }
  base_url: fake_url
";

        Action act = () => configLoader.LoadFromStream(new StringReader(testYml), "sonarr");
        act.Should().Throw<YamlException>().WithMessage("Expected 'Scalar'*");
    }

    [Test]
    public void Throw_when_expected_value_is_not_scalar()
    {
        var configLoader = Resolve<ConfigurationLoader<SonarrConfiguration>>();

        const string testYml = @"
sonarr:
- api_key: fake_key
  base_url: fake_url
  release_profiles: !secret bogus_profile
";

        const string secretsYml = @"
bogus_profile: 95283e6b156c42f3af8a9b16173f876b
";

        Fs.AddFile(Paths.SecretsPath.FullName, new MockFileData(secretsYml));
        Action act = () => configLoader.LoadFromStream(new StringReader(testYml), "sonarr");
        act.Should().Throw<YamlException>().WithMessage("Exception during deserialization");
    }
}
