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

    private static ServerSyncSettings Settings(params string[] configs)
    {
        return new ServerSyncSettings(
            Service: null,
            Instances: [],
            Preview: false,
            Configs: configs
        );
    }

    [Test]
    public void Explicit_paths_are_used_instead_of_the_default_locations()
    {
        AddConfig(Paths.ConfigDirectory.File("recyclarr.yml"), "from-default-location");

        var explicitConfig = Fs.CurrentDirectory().SubDirectory("elsewhere").File("other.yml");
        AddConfig(explicitConfig, "from-explicit-path");

        var result = Resolve<ServerConfigLoader>().LoadConfigs(Settings(explicitConfig.FullName));

        result.Configs.Select(x => x.InstanceName).Should().Equal("from-explicit-path");
        result.MissingConfigFiles.Should().BeEmpty();
    }

    [Test]
    public void Default_locations_are_used_when_no_paths_are_given()
    {
        AddConfig(Paths.ConfigDirectory.File("recyclarr.yml"), "from-default-location");

        var result = Resolve<ServerConfigLoader>().LoadConfigs(Settings());

        result.Configs.Select(x => x.InstanceName).Should().Equal("from-default-location");
    }

    [Test]
    public void Paths_that_do_not_exist_are_reported_without_failing_the_rest()
    {
        var existing = Fs.CurrentDirectory().SubDirectory("elsewhere").File("other.yml");
        AddConfig(existing, "from-explicit-path");

        var missing = Fs.CurrentDirectory().SubDirectory("elsewhere").File("gone.yml");

        var result = Resolve<ServerConfigLoader>()
            .LoadConfigs(Settings(existing.FullName, missing.FullName));

        result.Configs.Select(x => x.InstanceName).Should().Equal("from-explicit-path");
        result.MissingConfigFiles.Should().Equal(missing.FullName);
    }
}
