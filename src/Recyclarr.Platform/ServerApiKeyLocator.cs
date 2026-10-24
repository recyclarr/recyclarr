using System.IO.Abstractions;

namespace Recyclarr.Platform;

/// <summary>
/// Resolves the server API key from the environment: <c>RECYCLARR_API_KEY</c>, or the file named
/// by <c>RECYCLARR_API_KEY_FILE</c> (ADR-027).
/// </summary>
/// <remarks>
/// The server and the CLI in one container must agree on the key, so both resolve it through this
/// type. The two variables are exclusive, empty values count as unset, and trailing line breaks in
/// the file are ignored. Each call reads the file again, so a rotated file takes effect on the next
/// call. Throws <see cref="EnvironmentException"/> when both variables are set or the file cannot
/// be read. Key strength is the server's rule, not this type's.
/// </remarks>
public class ServerApiKeyLocator(IEnvironment env, IFileSystem fs)
{
    public const string Variable = "RECYCLARR_API_KEY";
    public const string FileVariable = "RECYCLARR_API_KEY_FILE";

    /// <summary>
    /// Returns the key, or null when neither variable is set.
    /// </summary>
    public string? Locate()
    {
        var key = NonEmpty(Variable);
        var path = NonEmpty(FileVariable);

        if (key is not null && path is not null)
        {
            throw new EnvironmentException(
                $"{Variable} and {FileVariable} are both set; set only one of them"
            );
        }

        return path is null ? key : ReadFile(path);
    }

    private string? NonEmpty(string variable)
    {
        var value = env.GetEnvironmentVariable(variable);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private string ReadFile(string path)
    {
        try
        {
            return fs.File.ReadAllText(path).TrimEnd('\r', '\n');
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new EnvironmentException(
                $"Cannot read the API key file named by {FileVariable} ({path}): {e.Message}"
            );
        }
    }
}
