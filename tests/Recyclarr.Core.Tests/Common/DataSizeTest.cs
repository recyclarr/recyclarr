using Recyclarr.Common;

namespace Recyclarr.Core.Tests.Common;

internal sealed class DataSizeTest
{
    [Test]
    [Arguments("100MB", 100L * 1024 * 1024)]
    [Arguments("1GB", 1L * 1024 * 1024 * 1024)]
    [Arguments("512KB", 512L * 1024)]
    [Arguments("0MB", 0L)]
    public void Parse_valid_sizes(string input, long expectedBytes)
    {
        var result = DataSize.Parse(input);

        result.Bytes.Should().Be(expectedBytes);
    }

    [Test]
    [Arguments("100mb")]
    [Arguments("1gb")]
    [Arguments("512Kb")]
    [Arguments("256kB")]
    public void Parse_is_case_insensitive(string input)
    {
        var act = () => DataSize.Parse(input);

        act.Should().NotThrow();
    }

    [Test]
    [Arguments("100")]
    [Arguments("MB")]
    [Arguments("100 MB")]
    [Arguments("100TB")]
    [Arguments("")]
    [Arguments("abc")]
    [Arguments("100mb ")]
    public void Parse_rejects_invalid_formats(string input)
    {
        var act = () => DataSize.Parse(input);

        act.Should().Throw<FormatException>();
    }

    [Test]
    public void Parse_rejects_overflow_value()
    {
        var act = () => DataSize.Parse("99999999999999999999MB");

        act.Should().Throw<FormatException>().WithMessage("*too large*");
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
