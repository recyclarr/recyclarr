namespace Recyclarr.VersionControl;

public class GitCmdException(int exitCode, string error) : Exception("Git command failed with a non-zero exit code")
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public string Error { get; } = error;
    public int ExitCode { get; } = exitCode;
    // ReSharper restore UnusedAutoPropertyAccessor.Global
}

public class InvalidGitRepoException(string? message) : Exception(message);
