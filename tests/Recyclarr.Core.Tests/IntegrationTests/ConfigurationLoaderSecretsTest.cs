using System.IO.Abstractions;
using Recyclarr.Config.Parsing;
using Recyclarr.Core.TestLibrary;
using Recyclarr.TrashGuide;

namespace Recyclarr.Core.Tests.IntegrationTests;

internal sealed class ConfigurationLoaderSecretsTest : IntegrationTestFixture
{
    [Test]
    public void Test_secret_loading()
    {
        var configLoader = Resolve<ConfigurationLoader>();

        const string testYml = """
            sonarr:
              instance1:
                api_key: !secret api_key
                base_url: !secret 123GARBAGE_
                custom_formats:
                  - trash_ids:
                      - !secret secret_rp
            """;

        const string secretsYml = """
            api_key: 95283e6b156c42f3af8a9b16173f876b
            123GARBAGE_: 'https://radarr:7878'
            secret_rp: 1234567
            """;

        Fs.AddFile(
            Paths.AppDataDirectory.File("secrets.yml").FullName,
            new MockFileData(secretsYml)
        );

        var results = configLoader.Load(() => new StringReader(testYml));

        results
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new LoadedConfigYaml(
                    "instance1",
                    SupportedServices.Sonarr,
                    new SonarrConfigYaml
                    {
                        ApiKey = "95283e6b156c42f3af8a9b16173f876b",
                        BaseUrl = "https://radarr:7878",
                        CustomFormats = [new CustomFormatConfigYaml { TrashIds = ["1234567"] }],
                    }
                )
            );
    }

    [Test]
    public void Throw_when_referencing_invalid_secret()
    {
        var configLoader = Resolve<ConfigurationLoader>();

        const string testYml = """
            sonarr:
              instance2:
                api_key: !secret api_key
                base_url: fake_url
            """;

        const string secretsYml = "no_api_key: 95283e6b156c42f3af8a9b16173f876b";

        Fs.AddFile(
            Paths.AppDataDirectory.File("recyclarr.yml").FullName,
            new MockFileData(secretsYml)
        );

        configLoader
            .Load(() => new StringReader(testYml))
            .Should()
            .NotContain(x => x.ServiceType == SupportedServices.Sonarr);
    }

    [Test]
    public void Throw_when_referencing_secret_without_secrets_file()
    {
        var configLoader = Resolve<ConfigurationLoader>();

        const string testYml = """
            sonarr:
              instance3:
                api_key: !secret api_key
                base_url: fake_url
            """;

        configLoader
            .Load(() => new StringReader(testYml))
            .Should()
            .NotContain(x => x.ServiceType == SupportedServices.Sonarr);
    }

    [Test]
    public void No_config_loaded_when_secret_value_is_not_scalar()
    {
        var configLoader = Resolve<ConfigurationLoader>();

        const string testYml = """
            sonarr:
              instance4:
                api_key: !secret { property: value }
                base_url: fake_url
            """;

        configLoader
            .Load(() => new StringReader(testYml))
            .Should()
            .NotContain(x => x.ServiceType == SupportedServices.Sonarr);
    }

    [Test]
    public void No_config_loaded_when_resolved_value_is_not_correct()
    {
        var configLoader = Resolve<ConfigurationLoader>();

        const string testYml = """
            sonarr:
              instance5:
                api_key: fake_key
                base_url: fake_url
                custom_formats: !secret bogus_profile
            """;

        const string secretsYml = "bogus_profile: 95283e6b156c42f3af8a9b16173f876b";

        Fs.AddFile(
            Paths.AppDataDirectory.File("recyclarr.yml").FullName,
            new MockFileData(secretsYml)
        );
        configLoader
            .Load(() => new StringReader(testYml))
            .Should()
            .NotContain(x => x.ServiceType == SupportedServices.Sonarr);
    }
}
