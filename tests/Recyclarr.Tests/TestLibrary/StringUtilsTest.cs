using Recyclarr.TestLibrary;

namespace Recyclarr.Tests.TestLibrary;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class StringUtilsTest
{
    [Test]
    public void TrimmedString_Newlines_AreStripped()
    {
        var testStr = "\r\ntest\r\n";
        StringUtils.TrimmedString(testStr).Should().Be("test");
    }
}
