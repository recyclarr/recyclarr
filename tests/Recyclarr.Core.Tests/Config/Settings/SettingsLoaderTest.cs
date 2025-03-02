using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.Settings;
using Recyclarr.Yaml;

namespace Recyclarr.Core.Tests.Config.Settings;

internal sealed class SettingsLoaderTest
{
    [Test, AutoMockData]
    public void Load_should_create_settings_file_if_not_exists(
        [Frozen] MockFileSystem fileSystem,
        [Frozen] IAppPaths paths,
        SettingsLoader sut
    )
    {
        sut.LoadAndOptionallyCreate();

        fileSystem
            .AllFiles.Should()
            .ContainSingle(paths.AppDataDirectory.File("settings.yml").FullName);
    }

    [Test, AutoMockData]
    public void Load_defaults_when_file_does_not_exist(
        [Frozen(Matching.ImplementedInterfaces)] YamlSerializerFactory serializerFactory,
        [Frozen] IAppPaths paths,
        SettingsLoader sut
    )
    {
        var expectedSettings = new RecyclarrSettings();
        var settings = sut.LoadAndOptionallyCreate();

        settings.Should().BeEquivalentTo(expectedSettings);
    }
}
