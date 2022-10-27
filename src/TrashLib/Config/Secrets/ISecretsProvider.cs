using System.Collections.Immutable;

namespace TrashLib.Config.Secrets;

public interface ISecretsProvider
{
    IImmutableDictionary<string, string> Secrets { get; }
}
