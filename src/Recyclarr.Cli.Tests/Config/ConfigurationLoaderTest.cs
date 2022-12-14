using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Autofac;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Cli.Config;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.Common;
using Recyclarr.Common.Extensions;
using Recyclarr.TestLibrary;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TrashLib.Config.Secrets;
using Recyclarr.TrashLib.Services.Sonarr.Config;
using Recyclarr.TrashLib.TestLibrary;
using Serilog.Sinks.TestCorrelator;
using YamlDotNet.Core;

namespace Recyclarr.Cli.Tests.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigurationLoaderTest : IntegrationFixture
{
    private static TextReader GetResourceData(string file)
    {
        var testData = new ResourceDataReader(typeof(ConfigurationLoaderTest), "Data");
        return new StringReader(testData.ReadData(file));
    }

    protected override void RegisterExtraTypes(ContainerBuilder builder)
    {
        base.RegisterExtraTypes(builder);
        builder.RegisterMockFor<IValidator<TestConfig>>();
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
            (baseDir.File("config2.yml").FullName, MockYaml(3)),
            (baseDir.File("config3.yml").FullName, "bad yaml")
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

    [Test]
    public void Skip_when_validation_fails()
        // [Frozen] IValidator<TestConfig> validator,
        // ConfigurationLoader<TestConfig> configLoader)
    {
        var validator = Resolve<IValidator<TestConfig>>();
        var sut = Resolve<ConfigurationLoader<TestConfig>>();

        // force the validator to return a validation error
        validator.Validate(Arg.Any<TestConfig>()).Returns(new ValidationResult
        {
            Errors =
            {
                new ValidationFailure("PropertyName", "Test Validation Failure"),
                new ValidationFailure("Another", "This is yet another failure")
            }
        });

        const string testYml = @"
fubar:
  instance1:
    api_key: abc
";
        var result = sut.LoadFromStream(new StringReader(testYml), "fubar");

        result.Should().BeEmpty();
    }

    [Test]
    public void Validation_success_does_not_throw()
    {
        var configLoader = Resolve<ConfigurationLoader<TestConfig>>();

        const string testYml = @"
fubar:
  instanceA:
    api_key: abc
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
  instance1:
    api_key: !secret api_key
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
                Name = "instance1",
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
        using var logContext = TestCorrelator.CreateContext();
        var configLoader = Resolve<ConfigurationLoader<SonarrConfiguration>>();

        const string testYml = @"
sonarr:
  instance2:
    api_key: !secret api_key
    base_url: fake_url
";

        const string secretsYml = "no_api_key: 95283e6b156c42f3af8a9b16173f876b";

        Fs.AddFile(Paths.SecretsPath.FullName, new MockFileData(secretsYml));

        var act = () => configLoader.LoadFromStream(new StringReader(testYml), "sonarr");

        act.Should().Throw<YamlException>()
            .WithInnerException<SecretNotFoundException>()
            .WithMessage("*api_key is not defined in secrets.yml");
    }

    [Test]
    public void Throw_when_referencing_secret_without_secrets_file()
    {
        var configLoader = Resolve<ConfigurationLoader<TestConfig>>();

        const string testYml = @"
sonarr:
  instance3:
    api_key: !secret api_key
    base_url: fake_url
";

        Action act = () => configLoader.LoadFromStream(new StringReader(testYml), "sonarr");
        act.Should().Throw<YamlException>()
            .WithInnerException<SecretNotFoundException>()
            .WithMessage("*api_key is not defined in secrets.yml");
    }

    [Test]
    public void Throw_when_secret_value_is_not_scalar()
    {
        var configLoader = Resolve<ConfigurationLoader<TestConfig>>();

        const string testYml = @"
sonarr:
  instance4:
    api_key: !secret { property: value }
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
  instance5:
    api_key: fake_key
    base_url: fake_url
    release_profiles: !secret bogus_profile
";

        const string secretsYml = @"bogus_profile: 95283e6b156c42f3af8a9b16173f876b";

        Fs.AddFile(Paths.SecretsPath.FullName, new MockFileData(secretsYml));
        Action act = () => configLoader.LoadFromStream(new StringReader(testYml), "sonarr");
        act.Should().Throw<YamlException>().WithMessage("Exception during deserialization");
    }

    [Test, AutoMockData]
    public void Throw_when_yaml_file_only_has_comment(ConfigurationLoader<TestConfig> sut)
    {
        const string testYml = "# YAML with nothing but this comment";

        var act = () => sut.LoadFromStream(new StringReader(testYml), "fubar");

        act.Should().Throw<EmptyYamlException>();
    }

    [Test, AutoMockData]
    public void Throw_when_yaml_file_is_empty(ConfigurationLoader<TestConfig> sut)
    {
        const string testYml = "";

        var act = () => sut.LoadFromStream(new StringReader(testYml), "fubar");

        act.Should().Throw<EmptyYamlException>();
    }

    [Test, AutoMockData]
    public void Do_not_throw_when_file_not_empty_but_has_no_desired_sections(ConfigurationLoader<TestConfig> sut)
    {
        const string testYml = @"
not_wanted:
  instance:
    base_url: abc
    api_key: xyz
";

        var act = () => sut.LoadFromStream(new StringReader(testYml), "fubar");

        act.Should().NotThrow();
    }
}
