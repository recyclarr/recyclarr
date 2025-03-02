using Recyclarr.Common.Extensions;

namespace Recyclarr.Core.Tests.Common.Extensions;

internal sealed class StringExtensionsTest
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

    [Test]
    public void Snake_case_works()
    {
        "UpperCamelCase".ToSnakeCase().Should().Be("upper_camel_case");
        "lowerCamelCase".ToSnakeCase().Should().Be("lower_camel_case");
    }
}
