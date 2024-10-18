namespace Recyclarr.Config.Secrets;

public interface ISecretsProvider
{
    IReadOnlyDictionary<string, string> Secrets { get; }
}
