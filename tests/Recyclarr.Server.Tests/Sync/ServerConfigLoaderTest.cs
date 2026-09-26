using System.IO.Abstractions;
using Recyclarr.Server.Sync;
using Recyclarr.Server.Tests.Reusable;

namespace Recyclarr.Server.Tests.Sync;

internal sealed class ServerConfigLoaderTest : ServerIntegrationFixture
{
    private void AddConfig(IFileInfo file, string instanceName)
    {
        Fs.AddFile(
            file,
            new MockFileData(
                $"""
                radarr:
                  {instanceName}:
                    base_url: http://localhost:7878
                    api_key: asdf
                """
            )
        );
    }

    private static ServerSyncSettings Settings()
    {
        return new ServerSyncSettings(Service: null, Instances: [], Preview: false);
    }

    [Test]
    public void Configs_are_loaded_from_the_default_locations()
    {
        AddConfig(Paths.ConfigDirectory.File("recyclarr.yml"), "from-default-location");

        var result = Resolve<ServerConfigLoader>().LoadConfigs(Settings());

        result.Configs.Select(x => x.InstanceName).Should().Equal("from-default-location");
    }
}
