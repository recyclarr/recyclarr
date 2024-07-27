namespace Recyclarr.Config.Secrets;

public class SecretNotFoundException(long line, string secretKey)
    : Exception($"Secret used on line {line} with key {secretKey} is not defined in secrets.yml")
{
    public long Line { get; } = line;
    public string SecretKey { get; } = secretKey;
}
