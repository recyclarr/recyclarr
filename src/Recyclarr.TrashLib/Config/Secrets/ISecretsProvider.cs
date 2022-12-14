using System.Collections.Immutable;

namespace Recyclarr.TrashLib.Config.Secrets;

public interface ISecretsProvider
{
    IImmutableDictionary<string, string> Secrets { get; }
}
