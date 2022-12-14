namespace TrashLib.Config.EnvironmentVariables;

public class EnvironmentVariableNotDefinedException : Exception
{
    public int Line { get; }
    public string EnvironmentVariableName { get; }

    public EnvironmentVariableNotDefinedException(int line, string environmentVariableName)
        : base($"Line {line} refers to undefined environment variable {environmentVariableName} and no default is specified.")
    {
        Line = line;
        EnvironmentVariableName = environmentVariableName;
    }
}
