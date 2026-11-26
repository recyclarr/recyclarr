using System.IO.Abstractions;
using Recyclarr.Config.File;
using Recyclarr.Config.Parsing;
using Recyclarr.Platform;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Recyclarr.Core.Tests.Config.File;

internal sealed class FileDeserializerTest
{
    private readonly MockFileSystem _fs = new();
    private readonly IAppPaths _paths = Substitute.For<IAppPaths>();

    private FileDeserializer CreateSut() => new(_fs, _paths, new ConfigInputRecorder());

    [Test]
    public void Returns_false_for_non_file_tag_type()
    {
        var sut = CreateSut();
        var result = sut.Deserialize(
            Substitute.For<IParser>(),
            typeof(string),
            null!,
            out var value,
            null!
        );

        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Test]
    public void Trims_trailing_whitespace_from_file_content()
    {
        var sut = CreateSut();
        const string filePath = "/secrets/api_key";
        _fs.AddFile(filePath, "abc123\n");

        var parser = CreateParser(filePath);

        sut.Deserialize(parser, typeof(FileTag), null!, out var value, null!);

        value.Should().Be("abc123");
    }

    [Test]
    public void Absolute_path_is_used_as_is()
    {
        var sut = CreateSut();
        _fs.AddFile("/absolute/secrets/key", "my-secret");

        var parser = CreateParser("/absolute/secrets/key");

        sut.Deserialize(parser, typeof(FileTag), null!, out var value, null!);

        value.Should().Be("my-secret");
    }

    [Test]
    public void Relative_path_resolves_against_config_directory()
    {
        var sut = CreateSut();
        var configDir = _fs.CurrentDirectory().SubDirectory("config");
        _paths.ConfigDirectory.Returns(configDir);
        _fs.AddFile(configDir.File("secrets/api_key").FullName, "resolved-secret");

        var parser = CreateParser("secrets/api_key");

        sut.Deserialize(parser, typeof(FileTag), null!, out var value, null!);

        value.Should().Be("resolved-secret");
    }

    private static IParser CreateParser(string scalarValue)
    {
        var parser = Substitute.For<IParser>();
        parser.Current.Returns(new Scalar(scalarValue));
        parser.MoveNext().Returns(true);
        return parser;
    }
}
