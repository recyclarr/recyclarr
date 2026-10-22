using System.IO.Abstractions;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Tests.Reusable;

namespace Recyclarr.Server.Tests.Sync;

internal sealed class ServerConfigLoaderTest : ServerIntegrationFixture
{
    // Instances sharing a base URL are rejected as split, so each gets its own host.
    private void AddConfig(IFileInfo file, string instanceName)
    {
        Fs.AddFile(
            file,
            new MockFileData(
                $"""
                radarr:
                  {instanceName}:
                    base_url: http://{instanceName}:7878
                    api_key: asdf
                """
            )
        );
    }

    [Test]
    public void Every_config_location_is_loaded()
    {
        AddConfig(Paths.ConfigDirectory.File("recyclarr.yml"), "from-main-file");
        AddConfig(Paths.YamlConfigDirectory.File("extra.yml"), "from-configs-directory");

        var configuration = Resolve<ServerConfigLoader>().TryLoadServerConfiguration();

        configuration.Should().NotBeNull();
        configuration
            .InstanceNames.Should()
            .BeEquivalentTo("from-main-file", "from-configs-directory");
    }

    [Test]
    public void Conflicting_main_config_files_are_a_load_error()
    {
        AddConfig(Paths.ConfigDirectory.File("recyclarr.yml"), "yml");
        AddConfig(Paths.ConfigDirectory.File("recyclarr.yaml"), "yaml");

        Resolve<ServerConfigLoader>().TryLoadServerConfiguration().Should().BeNull();
    }

    private const string SizeSection =
        "quality_definition:\n      type: movie\n      qualities:\n        - name: X\n          min: ";
    private const string PropersSection = "media_management:\n      propers_and_repacks: ";

    [TestCase(SizeSection + "big", ExpectedResult = false)]
    [TestCase(SizeSection + "unlimited", ExpectedResult = true)]
    [TestCase(PropersSection + "sometimes", ExpectedResult = false)]
    [TestCase(PropersSection + "do_not_prefer", ExpectedResult = true)]
    public bool Value_fields_load_only_with_valid_values(string section)
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                $"""
                radarr:
                  movies:
                    base_url: http://movies:7878
                    api_key: asdf
                    {section}
                """
            )
        );

        return Resolve<ServerConfigLoader>().TryLoadServerConfiguration() is not null;
    }
}
