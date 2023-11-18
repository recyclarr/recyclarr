namespace Recyclarr.Config.Secrets;

public class SecretNotFoundException(int line, string secretKey)
    : Exception($"Secret used on line {line} with key {secretKey} is not defined in secrets.yml")
{
    public int Line { get; } = line;
    public string SecretKey { get; } = secretKey;
}
