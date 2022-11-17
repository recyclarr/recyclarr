namespace TrashLib.Repo.VersionControl;

public class GitCmdException : Exception
{
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public string Error { get; }

    public int ExitCode { get; }
    // ReSharper restore UnusedAutoPropertyAccessor.Global

    public GitCmdException(int exitCode, string error)
        : base("Git command failed with a non-zero exit code")
    {
        Error = error;
        ExitCode = exitCode;
    }
}
