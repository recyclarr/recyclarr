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

        var configuration = Resolve<ServerConfiguration>();

        configuration
            .InstanceNames.Should()
            .BeEquivalentTo("from-main-file", "from-configs-directory");
    }
}
