using Common.Extensions;

namespace TestLibrary;

public static class StringUtils
{
    public static string TrimmedString(string value) => value.TrimNewlines();
}
