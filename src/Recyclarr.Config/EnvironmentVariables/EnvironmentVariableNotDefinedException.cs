namespace Recyclarr.Config.EnvironmentVariables;

public class EnvironmentVariableNotDefinedException(int line, string envVarName) : Exception(
    $"Line {line} refers to undefined environment variable {envVarName} and no default is specified.")
{
    public int Line { get; } = line;
    public string EnvironmentVariableName { get; } = envVarName;
}
