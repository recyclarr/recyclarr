namespace Recyclarr.VersionControl;

public class GitCmdException(int exitCode, string errorMessage) : Exception(errorMessage)
{
    public int ExitCode { get; } = exitCode;
}

public class InvalidGitRepoException(string? message) : Exception(message);
