using Recyclarr.Common;

namespace Recyclarr.Core.Tests.Common;

internal sealed class DataSizeTest
{
    [TestCase("100MB", 100L * 1024 * 1024)]
    [TestCase("1GB", 1L * 1024 * 1024 * 1024)]
    [TestCase("512KB", 512L * 1024)]
    [TestCase("0MB", 0L)]
    public void Parse_valid_sizes(string input, long expectedBytes)
    {
        var result = DataSize.Parse(input);

        result.Bytes.Should().Be(expectedBytes);
    }

    [TestCase("100mb")]
    [TestCase("1gb")]
    [TestCase("512Kb")]
    [TestCase("256kB")]
    public void Parse_is_case_insensitive(string input)
    {
        var act = () => DataSize.Parse(input);

        act.Should().NotThrow();
    }

    [TestCase("100")]
    [TestCase("MB")]
    [TestCase("100 MB")]
    [TestCase("100TB")]
    [TestCase("")]
    [TestCase("abc")]
    [TestCase("100mb ")]
    public void Parse_rejects_invalid_formats(string input)
    {
        var act = () => DataSize.Parse(input);

        act.Should().Throw<FormatException>();
    }

    [Test]
    public void Default_is_100_megabytes()
    {
        DataSize.Default.Bytes.Should().Be(100L * 1024 * 1024);
    }

    [Test]
    public void Factory_methods_compute_correct_bytes()
    {
        DataSize.FromKilobytes(1).Bytes.Should().Be(1024);
        DataSize.FromMegabytes(1).Bytes.Should().Be(1024 * 1024);
        DataSize.FromGigabytes(1).Bytes.Should().Be(1024L * 1024 * 1024);
    }
}
