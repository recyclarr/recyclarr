using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Common;
using Common.Extensions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using NUnit.Framework;
using TestLibrary;
using Trash.Config;
using TrashLib.Config;
using TrashLib.Sonarr.Config;
using TrashLib.Sonarr.ReleaseProfile;
using YamlDotNet.Serialization.ObjectFactories;

namespace Trash.Tests.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigurationLoaderTest
{
    private static TextReader GetResourceData(string file)
    {
        var testData = new ResourceDataReader(typeof(ConfigurationLoaderTest), "Data");
        return new StringReader(testData.ReadData(file));
    }

    [SuppressMessage("Microsoft.Design", "CA1034",
        Justification = "YamlDotNet requires this type to be public so it may access it")]
    public class TestConfig : IServiceConfiguration
    {
        public string BaseUrl => "";
        public string ApiKey => "";
    }

    [Test]
    public void Load_many_iterations_of_config()
    {
        static StreamReader MockYaml(params object[] args)
        {
            var str = new StringBuilder("sonarr:");
            const string templateYaml = "\n  - base_url: {0}\n    api_key: abc";
            str.Append(args.Aggregate("", (current, p) => current + templateYaml.FormatWith(p)));
            return StreamBuilder.FromString(str.ToString());
        }

        var fs = Substitute.For<IFileSystem>();
        fs.File.OpenText(Arg.Any<string>())
            .Returns(MockYaml(1, 2), MockYaml(3));

        var provider = Substitute.For<IConfigurationProvider>();
        // var objectFactory = Substitute.For<IObjectFactory>();
        // objectFactory.Create(Arg.Any<Type>())
        // .Returns(t => Substitute.For(new[] {(Type)t[0]}, Array.Empty<object>()));

        var actualActiveConfigs = new List<SonarrConfiguration>();
#pragma warning disable NS1004
        provider.ActiveConfiguration = Arg.Do<SonarrConfiguration>(a => actualActiveConfigs.Add(a));
#pragma warning restore NS1004

        var validator = Substitute.For<IValidator<SonarrConfiguration>>();
        var loader =
            new ConfigurationLoader<SonarrConfiguration>(provider, fs, new DefaultObjectFactory(), validator);

        var fakeFiles = new List<string>
        {
            "config1.yml",
            "config2.yml"
        };

        var expected = new List<SonarrConfiguration>
        {
            new() {ApiKey = "abc", BaseUrl = "1"},
            new() {ApiKey = "abc", BaseUrl = "2"},
            new() {ApiKey = "abc", BaseUrl = "3"}
        };

        var actual = loader.LoadMany(fakeFiles, "sonarr").ToList();

        actual.Should().BeEquivalentTo(expected);
        actualActiveConfigs.Should().BeEquivalentTo(expected, op => op.WithoutStrictOrdering());
    }

    [Test]
    public void Parse_using_stream()
    {
        var validator = Substitute.For<IValidator<SonarrConfiguration>>();
        var configLoader = new ConfigurationLoader<SonarrConfiguration>(
            Substitute.For<IConfigurationProvider>(),
            Substitute.For<IFileSystem>(),
            new DefaultObjectFactory(),
            validator);

        var configs = configLoader.LoadFromStream(GetResourceData("Load_UsingStream_CorrectParsing.yml"), "sonarr");

        configs.Should()
            .BeEquivalentTo(new List<SonarrConfiguration>
            {
                new()
                {
                    ApiKey = "95283e6b156c42f3af8a9b16173f876b",
                    BaseUrl = "http://localhost:8989",
                    ReleaseProfiles = new List<ReleaseProfileConfig>
                    {
                        new()
                        {
                            Type = ReleaseProfileType.Anime,
                            StrictNegativeScores = true,
                            Tags = new List<string> {"anime"}
                        },
                        new()
                        {
                            Type = ReleaseProfileType.Series,
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
    public void Throw_when_validation_fails()
    {
        var validator = Substitute.For<IValidator<TestConfig>>();
        var configLoader = new ConfigurationLoader<TestConfig>(
            Substitute.For<IConfigurationProvider>(),
            Substitute.For<IFileSystem>(),
            new DefaultObjectFactory(),
            validator);

        // force the validator to return a validation error
        validator.Validate(Arg.Any<TestConfig>()).Returns(new ValidationResult
        {
            Errors = {new ValidationFailure("PropertyName", "Test Validation Failure")}
        });

        var testYml = @"
fubar:
- api_key: abc
";
        Action act = () => configLoader.LoadFromStream(new StringReader(testYml), "fubar");

        act.Should().Throw<ConfigurationException>();
    }

    [Test]
    public void Validation_success_does_not_throw()
    {
        var validator = Substitute.For<IValidator<TestConfig>>();
        var configLoader = new ConfigurationLoader<TestConfig>(
            Substitute.For<IConfigurationProvider>(),
            Substitute.For<IFileSystem>(),
            new DefaultObjectFactory(),
            validator);

        var testYml = @"
fubar:
- api_key: abc
";
        Action act = () => configLoader.LoadFromStream(new StringReader(testYml), "fubar");
        act.Should().NotThrow();
    }
}
