namespace Recyclarr.Cli.Console.Helpers;

internal static class StringCaseConverter
{
    public static string ToKebabCase(string value)
    {
        return string.Concat(
                value.Select(
                    (c, i) =>
                        i > 0 && char.IsUpper(c)
                            ? "-" + char.ToLowerInvariant(c)
                            : char.ToLowerInvariant(c).ToString()
                )
            )
            .TrimStart('-');
    }
}
