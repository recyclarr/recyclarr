using Recyclarr.Config.File;
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

        // Set up mock to return a Scalar when Consume<Scalar>() is called
        var parser = Substitute.For<IParser>();
        parser.Current.Returns(new Scalar(filePath));
        parser.MoveNext().Returns(true);

        sut.Deserialize(parser, typeof(FileTag), null!, out var value, null!);

        value.Should().Be("abc123");
    }
}
