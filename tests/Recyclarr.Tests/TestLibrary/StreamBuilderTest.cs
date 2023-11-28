using Recyclarr.TestLibrary;

namespace Recyclarr.Tests.TestLibrary;

[TestFixture]
public class StreamBuilderTest
{
    [Test]
    public void FromString_UsingString_ShouldOutputSameString()
    {
        var stream = StreamBuilder.FromString("test");
        stream.ReadToEnd().Should().Be("test");
    }
}
