using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using NUnit.Framework;
using Recyclarr.TestLibrary;
using TrashLib.Config.Settings;
using TrashLib.Startup;

namespace Recyclarr.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ServiceCompatibilityIntegrationTest : IntegrationFixture
{
    [Test]
    public void Load_data_correctly_when_file_exists()
    {
        var sut = Resolve<ISettingsProvider>();
        var paths = Resolve<IAppPaths>();

        // For this test, it doesn't really matter if the YAML data matches what SettingsValue expects;
        // this test only ensures that the data deserialized is from the actual correct file.
        const string yamlData = @"
repository:
  clone_url: http://the_url.com
";

        Fs.AddFile(paths.SettingsPath.FullName, new MockFileData(yamlData));

        var settings = sut.Settings;

        settings.Repository.CloneUrl.Should().Be("http://the_url.com");
    }
}
