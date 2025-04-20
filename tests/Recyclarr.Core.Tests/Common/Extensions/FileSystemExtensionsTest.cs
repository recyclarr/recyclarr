using System.IO.Abstractions;
using Recyclarr.Common;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Core.Tests.Common.Extensions;

internal sealed class FileSystemExtensionsTest
{
    [Test]
    public void Return_null_when_no_yaml_files_exist()
    {
        var fs = new MockFileSystem();
        var result = fs.CurrentDirectory().YamlFile("test");
        result.Should().BeNull();
    }

    [TestCase("test.yml")]
    [TestCase("test.yaml")]
    public void Return_non_null_when_single_yaml_file_exists(string yamlFilename)
    {
        var fs = new MockFileSystem();
        fs.AddEmptyFile(yamlFilename);

        var result = fs.CurrentDirectory().YamlFile("test");
        result.Should().NotBeNull();
        result.Name.Should().Be(yamlFilename);
    }

    [Test]
    public void Throw_when_both_files_exist()
    {
        var fs = new MockFileSystem();
        fs.AddEmptyFile("test.yml");
        fs.AddEmptyFile("test.yaml");

        var act = () => fs.CurrentDirectory().YamlFile("test");
        act.Should().Throw<ConflictingYamlFilesException>();
    }
}
