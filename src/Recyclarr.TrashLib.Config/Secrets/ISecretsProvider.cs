namespace Recyclarr.TrashLib.Config.Secrets;

public interface ISecretsProvider
{
    IReadOnlyDictionary<string, string> Secrets { get; }
}
