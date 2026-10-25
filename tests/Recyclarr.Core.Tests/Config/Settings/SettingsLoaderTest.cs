using Recyclarr.Platform;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Recyclarr.Yaml;

namespace Recyclarr.Core.Tests.Config.Settings;

internal sealed class SettingsLoaderTest
{
    [Test, AutoMockData]
    public void Load_defaults_without_writing_when_file_does_not_exist(
        [Frozen] MockFileSystem fileSystem,
        [Frozen(Matching.ImplementedInterfaces)] YamlSerializerFactory serializerFactory,
        [Frozen] IAppPaths paths,
        SettingsLoader sut
    )
    {
        var settings = sut.Load();

        settings.Should().BeEquivalentTo(new RecyclarrSettings());
        fileSystem.AllFiles.Should().BeEmpty();
    }
}
