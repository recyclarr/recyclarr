using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using Autofac;
using FluentValidation;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Parsing;
using Recyclarr.Core.TestLibrary;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.TrashGuide;

namespace Recyclarr.Core.Tests.IntegrationTests;

internal sealed class ConfigurationLoaderTest : IntegrationTestFixture
{
    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);
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
            (baseDir.File("config4.yml"), MockYaml("radarr", "four")),
        };

        foreach (var (file, data) in fileData)
        {
            Fs.AddFile(file.FullName, new MockFileData(data));
        }

        var loader = Resolve<ConfigurationLoader>();

        var result = fileData.SelectMany(x => loader.Load(x.Item1)).ToList();

        result
            .Where(x => x.ServiceType == SupportedServices.Sonarr)
            .Select(x => x.Yaml)
            .Should()
            .BeEquivalentTo([
                new { ApiKey = "abc", BaseUrl = "http://one" },
                new { ApiKey = "abc", BaseUrl = "http://two" },
                new { ApiKey = "abc", BaseUrl = "http://three" },
            ]);

        result
            .Where(x => x.ServiceType == SupportedServices.Radarr)
            .Select(x => x.Yaml)
            .Should()
            .BeEquivalentTo([new { ApiKey = "abc", BaseUrl = "http://four" }]);

        return;

        static string MockYaml(string sectionName, params object[] args)
        {
            var str = new StringBuilder($"{sectionName}:");
            const string templateYaml = """
                  instance{1}:
                    base_url: http://{0}
                    api_key: abc
                """;

            for (var i = 0; i < args.Length; ++i)
            {
                str.Append(
                    CultureInfo.InvariantCulture,
                    $"\n{templateYaml.FormatWith(args[i], i)}\n"
                );
            }

            return str.ToString();
        }
    }

    [Test]
    public void Parse_using_stream()
    {
        var configLoader = Resolve<ConfigurationLoader>();
        var result = configLoader.Load(
            """
            sonarr:
              name:
                base_url: http://localhost:8989
                api_key: 95283e6b156c42f3af8a9b16173f876b
            """
        );

        result
            .Where(x => x.ServiceType == SupportedServices.Sonarr)
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new LoadedConfigYaml(
                    "name",
                    SupportedServices.Sonarr,
                    new SonarrConfigYaml
                    {
                        ApiKey = "95283e6b156c42f3af8a9b16173f876b",
                        BaseUrl = "http://localhost:8989",
                    }
                )
            );
    }
}
