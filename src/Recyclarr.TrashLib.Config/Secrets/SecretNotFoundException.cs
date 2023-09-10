namespace Recyclarr.TrashLib.Config.Secrets;

public class SecretNotFoundException : Exception
{
    public int Line { get; }
    public string SecretKey { get; }

    public SecretNotFoundException(int line, string secretKey)
        : base($"Secret used on line {line} with key {secretKey} is not defined in secrets.yml")
    {
        Line = line;
        SecretKey = secretKey;
    }
}
