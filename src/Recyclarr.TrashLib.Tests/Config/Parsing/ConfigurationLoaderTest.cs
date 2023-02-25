using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.Text;
using Autofac;
using FluentValidation;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.Common;
using Recyclarr.Common.Extensions;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.Config.Services.Sonarr;
using Recyclarr.TrashLib.Config.Yaml;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Config.Parsing;

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
        static string MockYaml(string sectionName, params object[] args)
        {
            var str = new StringBuilder($"{sectionName}:");
            const string templateYaml = @"
  instance{1}:
    base_url: http://{0}
    api_key: abc";

            var counter = 0;
            str.Append(args.Aggregate("", (current, p) => current + templateYaml.FormatWith(p, counter++)));
            return str.ToString();
        }

        var baseDir = Fs.CurrentDirectory();
        var fileData = new[]
        {
            (baseDir.File("config1.yml"), MockYaml("sonarr", "one", "two")),
            (baseDir.File("config2.yml"), MockYaml("sonarr", "three")),
            (baseDir.File("config3.yml"), "bad yaml"),
            (baseDir.File("config4.yml"), MockYaml("radarr", "four"))
        };

        foreach (var (file, data) in fileData)
        {
            Fs.AddFile(file.FullName, new MockFileData(data));
        }

        var expectedSonarr = new[]
        {
            new {ApiKey = "abc", BaseUrl = new Uri("http://one")},
            new {ApiKey = "abc", BaseUrl = new Uri("http://two")},
            new {ApiKey = "abc", BaseUrl = new Uri("http://three")}
        };

        var expectedRadarr = new[]
        {
            new {ApiKey = "abc", BaseUrl = new Uri("http://four")}
        };

        var loader = Resolve<IConfigurationLoader>();
        var actual = loader.LoadMany(fileData.Select(x => x.Item1));

        actual.GetConfigsBasedOnSettings(MockSyncSettings.Sonarr())
            .Should().BeEquivalentTo(expectedSonarr);

        actual.GetConfigsBasedOnSettings(MockSyncSettings.Radarr())
            .Should().BeEquivalentTo(expectedRadarr);
    }

    [Test]
    public void Parse_using_stream()
    {
        var configLoader = Resolve<ConfigurationLoader>();
        var configs = configLoader.LoadFromStream(GetResourceData("Load_UsingStream_CorrectParsing.yml"), "sonarr");

        configs.GetConfigsBasedOnSettings(MockSyncSettings.Sonarr())
            .Should().BeEquivalentTo(new List<SonarrConfiguration>
            {
                new()
                {
                    ApiKey = "95283e6b156c42f3af8a9b16173f876b",
                    BaseUrl = new Uri("http://localhost:8989"),
                    InstanceName = "name",
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
            }, o => o.Excluding(x => x.LineNumber));
    }

    [Test, AutoMockData]
    public void Throw_when_yaml_file_only_has_comment(ConfigurationLoader sut)
    {
        const string testYml = "# YAML with nothing but this comment";

        var act = () => sut.LoadFromStream(new StringReader(testYml), "fubar");

        act.Should().Throw<EmptyYamlException>();
    }

    [Test, AutoMockData]
    public void Throw_when_yaml_file_is_empty(ConfigurationLoader sut)
    {
        const string testYml = "";

        var act = () => sut.LoadFromStream(new StringReader(testYml), "fubar");

        act.Should().Throw<EmptyYamlException>();
    }

    [Test, AutoMockData]
    public void No_throw_when_file_not_empty_but_has_no_desired_sections(ConfigurationLoader sut)
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
