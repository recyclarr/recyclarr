using System.Diagnostics.CodeAnalysis;
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
using JetBrains.Annotations;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Config;
using Recyclarr.TestLibrary;
using TestLibrary.AutoFixture;
using TrashLib.Config.Services;
using TrashLib.Services.Sonarr.Config;

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

    [UsedImplicitly]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("Microsoft.Design", "CA1034",
        Justification = "YamlDotNet requires this type to be public so it may access it")]
    [SuppressMessage("Performance", "CA1822", MessageId = "Mark members as static")]
    public class TestConfig : IServiceConfiguration
    {
        public string BaseUrl => "";
        public string ApiKey => "";
        public ICollection<CustomFormatConfig> CustomFormats => new List<CustomFormatConfig>();
        public bool DeleteOldCustomFormats => false;
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
}
