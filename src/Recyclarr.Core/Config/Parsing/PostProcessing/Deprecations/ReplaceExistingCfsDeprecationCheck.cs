using Recyclarr.Sync.Events;

namespace Recyclarr.Config.Parsing.PostProcessing.Deprecations;

public class ReplaceExistingCfsDeprecationCheck(ISyncEventPublisher eventPublisher)
    : IConfigDeprecationCheck
{
    public bool CheckIfNeeded(ServiceConfigYaml include)
    {
        return include.ReplaceExistingCustomFormats is true;
    }

    public ServiceConfigYaml Transform(ServiceConfigYaml include)
    {
        eventPublisher.AddDeprecation(
            "The `replace_existing_custom_formats` option is deprecated and no longer has any effect. "
                + "To adopt existing CFs, run: recyclarr cache rebuild --adopt. "
                + "See: <https://recyclarr.dev/guide/upgrade-guide/v8.0/#replace-existing-removed>"
        );

        // Option is now a no-op; return config unchanged
        return include;
    }
}
