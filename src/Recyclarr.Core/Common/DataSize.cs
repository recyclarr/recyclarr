using System.Globalization;
using System.Text.RegularExpressions;

namespace Recyclarr.Common;

/// Value type representing a data size, parsed from human-readable strings like "100MB", "1GB".
public readonly partial record struct DataSize
{
    public long Bytes { get; }

    private DataSize(long bytes) => Bytes = bytes;

    public static DataSize FromKilobytes(long kilobytes) => new(kilobytes * 1024L);

    public static DataSize FromMegabytes(long megabytes) => new(megabytes * 1024L * 1024);

    public static DataSize FromGigabytes(long gigabytes) => new(gigabytes * 1024L * 1024 * 1024);

    public static DataSize Default => FromMegabytes(100);

    [GeneratedRegex(@"^(\d+)(KB|MB|GB)$", RegexOptions.IgnoreCase)]
    private static partial Regex ParsePattern { get; }

    public static DataSize Parse(string value)
    {
        var match = ParsePattern.Match(value);
        if (!match.Success)
        {
            throw new FormatException(
                $"Invalid data size '{value}'. Use a number followed by a unit: KB, MB, or GB (e.g. 512KB, 100MB, 1GB)."
            );
        }

        var numberText = match.Groups[1].Value;
        var unit = match.Groups[2].Value.ToUpperInvariant();

        long number;
        try
        {
            number = long.Parse(numberText, CultureInfo.InvariantCulture);
        }
        catch (OverflowException)
        {
            throw new FormatException(
                $"Invalid data size '{value}'. Value is too large. Use a number followed by a unit: KB, MB, or GB."
            );
        }

        return unit switch
        {
            "KB" => FromKilobytes(number),
            "MB" => FromMegabytes(number),
            "GB" => FromGigabytes(number),
            _ => throw new InvalidOperationException($"Unhandled unit: {unit}"),
        };
    }
}
