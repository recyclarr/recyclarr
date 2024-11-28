namespace Recyclarr.Config.EnvironmentVariables;

public class EnvironmentVariableNotDefinedException(long line, string envVarName)
    : Exception(
        $"Line {line} refers to undefined environment variable {envVarName} and no default is specified."
    )
{
    public long Line { get; } = line;
    public string EnvironmentVariableName { get; } = envVarName;
}
