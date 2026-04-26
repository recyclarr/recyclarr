using System.IO.Abstractions;
using Recyclarr.Config.File;
using Recyclarr.Platform;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Recyclarr.Core.Tests.Config.File;

internal sealed class FileDeserializerTest
{
    [Test, AutoMockData]
    public void Returns_false_for_non_file_tag_type(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        FileDeserializer sut
    )
    {
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

    [Test, AutoMockData]
    public void Trims_trailing_whitespace_from_file_content(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        FileDeserializer sut
    )
    {
        const string filePath = "/secrets/api_key";
        fs.AddFile(filePath, "abc123\n");

        var parser = CreateParser(filePath);

        sut.Deserialize(parser, typeof(FileTag), null!, out var value, null!);

        value.Should().Be("abc123");
    }

    [Test, AutoMockData]
    public void Absolute_path_is_used_as_is(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        FileDeserializer sut
    )
    {
        fs.AddFile("/absolute/secrets/key", "my-secret");

        var parser = CreateParser("/absolute/secrets/key");

        sut.Deserialize(parser, typeof(FileTag), null!, out var value, null!);

        value.Should().Be("my-secret");
    }

    [Test, AutoMockData]
    public void Relative_path_resolves_against_config_directory(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths paths,
        FileDeserializer sut
    )
    {
        var configDir = fs.CurrentDirectory().SubDirectory("config");
        paths.ConfigDirectory.Returns(configDir);
        fs.AddFile(configDir.File("secrets/api_key").FullName, "resolved-secret");

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
