using Recyclarr.Settings.Models;

namespace Recyclarr.Settings.Deprecations;

public interface ISettingsDeprecationCheck
{
    RecyclarrSettings Transform(RecyclarrSettings settings);
    bool CheckIfNeeded(RecyclarrSettings settings);
}
