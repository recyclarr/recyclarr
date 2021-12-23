using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TrashLib.Config;
using TrashLib.Config.Settings;
using TrashLib.Radarr.Config;
using YamlDotNet.Serialization;

namespace TrashLib.Tests.Config.Settings;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SettingsPersisterTest
{
    [Test, AutoMockData]
    public void Load_should_create_settings_file_if_not_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fileSystem,
        [Frozen] IResourcePaths paths,
        SettingsPersister sut)
    {
        paths.SettingsPath.Returns("test_path");

        sut.Load();

        fileSystem.AllFiles.Should().ContainSingle(x => x.EndsWith(paths.SettingsPath));
    }

    [Test, AutoMockData]
    public void Load_defaults_when_file_does_not_exist(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fileSystem,
        [Frozen(Matching.ImplementedInterfaces)] YamlSerializerFactory serializerFactory,
        [Frozen(Matching.ImplementedInterfaces)] SettingsProvider settingsProvider,
        [Frozen] IResourcePaths paths,
        SettingsPersister sut)
    {
        paths.SettingsPath.Returns("test_path");

        sut.Load();

        var expectedSettings = new SettingsValues();
        settingsProvider.Settings.Should().BeEquivalentTo(expectedSettings);
    }

    [Test, AutoMockData]
    public void Load_data_correctly_when_file_exists(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fileSystem,
        [Frozen] IYamlSerializerFactory serializerFactory,
        [Frozen] IResourcePaths paths,
        SettingsPersister sut)
    {
        // For this test, it doesn't really matter if the YAML data matches what SettingsValue expects;
        // this test only ensures that the data deserialized is from the actual correct file.
        var expectedYamlData = @"
repository:
  clone_url: http://the_url.com
";
        var deserializer = Substitute.For<IDeserializer>();
        serializerFactory.CreateDeserializer().Returns(deserializer);
        paths.SettingsPath.Returns("test_path");
        fileSystem.AddFile(paths.SettingsPath, new MockFileData(expectedYamlData));

        sut.Load();

        deserializer.Received().Deserialize<SettingsValues>(expectedYamlData);
    }
}
