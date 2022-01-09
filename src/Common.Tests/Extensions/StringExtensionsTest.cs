using Common.Extensions;
using FluentAssertions;
using NUnit.Framework;

namespace Common.Tests.Extensions;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class StringExtensionsTest
{
    [Test]
    public void Carriage_returns_and_newlines_are_stripped_from_front_and_back()
    {
        "\r\ntest\n\r".TrimNewlines().Should().Be("test");
    }

    [Test]
    public void Spaces_are_ignored_when_stripping_newlines()
    {
        "\n test \r".TrimNewlines().Should().Be(" test ");
    }
}
