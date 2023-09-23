namespace Recyclarr.Config.EnvironmentVariables;

public class EnvironmentVariableNotDefinedException : Exception
{
    public int Line { get; }
    public string EnvironmentVariableName { get; }

    public EnvironmentVariableNotDefinedException(int line, string envVarName)
        : base($"Line {line} refers to undefined environment variable {envVarName} and no default is specified.")
    {
        Line = line;
        EnvironmentVariableName = envVarName;
    }
}
