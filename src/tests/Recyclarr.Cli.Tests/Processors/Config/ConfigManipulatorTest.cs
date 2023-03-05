using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using Recyclarr.Cli.Processors.Config;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Cli.Tests.Processors.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigManipulatorTest : CliIntegrationFixture
{
    [Test]
    public void Create_file_when_no_file_already_exists()
    {
        var sut = Resolve<ConfigManipulator>();
        var src = Fs.CurrentDirectory().File("template.yml");
        var dst = Fs.CurrentDirectory().SubDir("one", "two", "three").File("config.yml");

        const string yamlData = @"
sonarr:
  instance1:
    base_url: http://localhost:80
    api_key: 123abc
";

        Fs.AddFile(src, new MockFileData(yamlData));

        sut.LoadAndSave(src, dst, (_, yaml) => yaml);

        Fs.AllFiles.Should().Contain(dst.FullName);
    }

    [Test]
    public void Throw_on_invalid_yaml()
    {
        var sut = Resolve<ConfigManipulator>();
        var src = Fs.CurrentDirectory().File("template.yml");
        var dst = Fs.CurrentDirectory().File("config.yml");

        const string yamlData = @"
sonarr:
  instance1:
    invalid: yaml
";

        Fs.AddFile(src, new MockFileData(yamlData));

        var act = () => sut.LoadAndSave(src, dst, (_, yaml) => yaml);

        act.Should().Throw<FileLoadException>();
    }
}
