using FluentAssertions;
using NUnit.Framework;

namespace Recyclarr.TestLibrary.Tests;

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
