namespace TestLibrary;

public static class StringUtils
{
    public static string TrimmedString(string value) => value.Trim('\r', '\n');
}
