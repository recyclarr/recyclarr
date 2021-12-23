using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Autofac;
using AutoFixture.NUnit3;
using Common;
using Common.Extensions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using JetBrains.Annotations;
using NSubstitute;
using NUnit.Framework;
using TestLibrary;
using TestLibrary.AutoFixture;
using TestLibrary.NSubstitute;
using Trash.Config;
using TrashLib.Config;
using TrashLib.Config.Services;
using TrashLib.Sonarr.Config;
using TrashLib.Sonarr.ReleaseProfile;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Trash.Tests.Config.Services;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigurationLoaderTest
{
    private static TextReader GetResourceData(string file)
    {
        var testData = new ResourceDataReader(typeof(ConfigurationLoaderTest), "Data");
        return new StringReader(testData.ReadData(file));
    }

    private static IContainer BuildContainer()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<DefaultObjectFactory>().As<IObjectFactory>();
        builder.RegisterType<YamlSerializerFactory>().As<IYamlSerializerFactory>();
        return builder.Build();
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
    }

    [Test, AutoMockData(typeof(ConfigurationLoaderTest), nameof(BuildContainer))]
    public void Load_many_iterations_of_config(
        [Frozen] IFileSystem fs,
        [Frozen] IConfigurationProvider provider,
        ConfigurationLoader<SonarrConfiguration> loader)
    {
        static StreamReader MockYaml(params object[] args)
        {
            var str = new StringBuilder("sonarr:");
            const string templateYaml = "\n  - base_url: {0}\n    api_key: abc";
            str.Append(args.Aggregate("", (current, p) => current + templateYaml.FormatWith(p)));
            return StreamBuilder.FromString(str.ToString());
        }

        fs.File.OpenText(Arg.Any<string>()).Returns(MockYaml(1, 2), MockYaml(3));

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
        provider.Received(3).ActiveConfiguration =
            Verify.That<SonarrConfiguration>(x => expected.Should().ContainEquivalentOf(x));
    }

    [Test, AutoMockData(typeof(ConfigurationLoaderTest), nameof(BuildContainer))]
    public void Parse_using_stream(ConfigurationLoader<SonarrConfiguration> configLoader)
    {
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

        var testYml = @"
fubar:
- api_key: abc
";
        Action act = () => configLoader.LoadFromStream(new StringReader(testYml), "fubar");

        act.Should().Throw<ConfigurationException>();
    }

    [Test, AutoMockData]
    public void Validation_success_does_not_throw(ConfigurationLoader<TestConfig> configLoader)
    {
        var testYml = @"
fubar:
- api_key: abc
";
        Action act = () => configLoader.LoadFromStream(new StringReader(testYml), "fubar");
        act.Should().NotThrow();
    }
}
