using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.Text;
using Autofac;
using FluentValidation;
using Recyclarr.Common;
using Recyclarr.Common.Extensions;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Config.Parsing;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigurationLoaderTest : TrashLibIntegrationFixture
{
    private static Func<TextReader> GetResourceData(string file)
    {
        var testData = new ResourceDataReader(typeof(ConfigurationLoaderTest), "Data");
        return () => new StringReader(testData.ReadData(file));
    }

    protected override void RegisterTypes(ContainerBuilder builder)
    {
        base.RegisterTypes(builder);
        builder.RegisterMockFor<IValidator<RadarrConfigYaml>>();
        builder.RegisterMockFor<IValidator<SonarrConfigYaml>>();
    }

    [Test]
    public void Load_many_iterations_of_config()
    {
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

        LoadMany(SupportedServices.Sonarr).Should().BeEquivalentTo(expectedSonarr);
        LoadMany(SupportedServices.Radarr).Should().BeEquivalentTo(expectedRadarr);

        return;

        static string MockYaml(string sectionName, params object[] args)
        {
            var str = new StringBuilder($"{sectionName}:");
            const string templateYaml =
                """
                  instance{1}:
                    base_url: http://{0}
                    api_key: abc
                """;

            for (var i = 0; i < args.Length; ++i)
            {
                str.Append($"\n{templateYaml.FormatWith(args[i], i)}\n");
            }

            return str.ToString();
        }

        IEnumerable<IServiceConfiguration> LoadMany(SupportedServices service)
            => fileData.SelectMany(x => loader.Load(x.Item1)).GetConfigsOfType(service);
    }

    [Test]
    public void Parse_using_stream()
    {
        var configLoader = Resolve<ConfigurationLoader>();
        configLoader.Load(GetResourceData("Load_UsingStream_CorrectParsing.yml"))
            .GetConfigsOfType(SupportedServices.Sonarr)
            .Should().BeEquivalentTo(new List<SonarrConfiguration>
            {
                new()
                {
                    ApiKey = "95283e6b156c42f3af8a9b16173f876b",
                    BaseUrl = new Uri("http://localhost:8989"),
                    InstanceName = "name",
                    ReplaceExistingCustomFormats = false,
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
    public void No_log_when_file_not_empty_but_has_no_desired_sections()
    {
        var sut = Resolve<ConfigurationLoader>();
        const string testYml =
            """
            not_wanted:
              instance:
                base_url: abc
                api_key: xyz
            """;

        sut.Load(testYml).GetConfigsOfType(SupportedServices.Sonarr);

        Logger.Messages.Should().NotContain("Configuration is empty");
    }
}
