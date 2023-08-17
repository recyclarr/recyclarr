using System.IO.Abstractions;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.TestLibrary;
using Serilog.Sinks.TestCorrelator;

namespace Recyclarr.TrashLib.Tests.Config.Parsing;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigurationLoaderSecretsTest : TrashLibIntegrationFixture
{
    [Test]
    public void Test_secret_loading()
    {
        var configLoader = Resolve<ConfigurationLoader>();

        const string testYml =
            """
            sonarr:
              instance1:
                api_key: !secret api_key
                base_url: !secret 123GARBAGE_
                release_profiles:
                  - trash_ids:
                      - !secret secret_rp
            """;

        const string secretsYml =
            """
            api_key: 95283e6b156c42f3af8a9b16173f876b
            123GARBAGE_: 'https://radarr:7878'
            secret_rp: 1234567
            """;

        Fs.AddFile(Paths.AppDataDirectory.File("secrets.yml").FullName, new MockFileData(secretsYml));
        var expected = new[]
        {
            new
            {
                InstanceName = "instance1",
                ApiKey = "95283e6b156c42f3af8a9b16173f876b",
                BaseUrl = new Uri("https://radarr:7878"),
                ReleaseProfiles = new[]
                {
                    new
                    {
                        TrashIds = new[] {"1234567"}
                    }
                }
            }
        };

        configLoader.Load(() => new StringReader(testYml))
            .GetConfigsOfType(SupportedServices.Sonarr)
            .Should().BeEquivalentTo(expected);
    }

    [Test]
    public void Throw_when_referencing_invalid_secret()
    {
        using var logContext = TestCorrelator.CreateContext();
        var configLoader = Resolve<ConfigurationLoader>();

        const string testYml =
            """
            sonarr:
              instance2:
                api_key: !secret api_key
                base_url: fake_url
            """;

        const string secretsYml = "no_api_key: 95283e6b156c42f3af8a9b16173f876b";

        Fs.AddFile(Paths.AppDataDirectory.File("recyclarr.yml").FullName, new MockFileData(secretsYml));

        configLoader.Load(() => new StringReader(testYml))
            .GetConfigsOfType(SupportedServices.Sonarr)
            .Should().BeEmpty();
    }

    [Test]
    public void Throw_when_referencing_secret_without_secrets_file()
    {
        var configLoader = Resolve<ConfigurationLoader>();

        const string testYml =
            """
            sonarr:
              instance3:
                api_key: !secret api_key
                base_url: fake_url
            """;

        configLoader.Load(() => new StringReader(testYml))
            .GetConfigsOfType(SupportedServices.Sonarr)
            .Should().BeEmpty();
    }

    [Test]
    public void No_config_loaded_when_secret_value_is_not_scalar()
    {
        var configLoader = Resolve<ConfigurationLoader>();

        const string testYml =
            """
            sonarr:
              instance4:
                api_key: !secret { property: value }
                base_url: fake_url
            """;

        configLoader.Load(() => new StringReader(testYml))
            .GetConfigsOfType(SupportedServices.Sonarr)
            .Should().BeEmpty();
    }

    [Test]
    public void No_config_loaded_when_resolved_value_is_not_correct()
    {
        var configLoader = Resolve<ConfigurationLoader>();

        const string testYml =
            """
            sonarr:
              instance5:
                api_key: fake_key
                base_url: fake_url
                release_profiles: !secret bogus_profile
            """;

        const string secretsYml = @"bogus_profile: 95283e6b156c42f3af8a9b16173f876b";

        Fs.AddFile(Paths.AppDataDirectory.File("recyclarr.yml").FullName, new MockFileData(secretsYml));
        configLoader.Load(() => new StringReader(testYml))
            .GetConfigsOfType(SupportedServices.Sonarr)
            .Should().BeEmpty();
    }
}
