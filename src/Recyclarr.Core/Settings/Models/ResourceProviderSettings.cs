using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Settings.Models;

public record ResourceProviderSettings
{
    public IReadOnlyCollection<IUnderlyingResourceProvider> TrashGuides { get; init; } = [];
    public IReadOnlyCollection<IUnderlyingResourceProvider> ConfigTemplates { get; init; } = [];

    /*
     * REMOVED COMPONENTS (for future reference based on user feedback):
     *
     * The following components were removed from ResourceProviderSettings to simplify the initial implementation:
     *
     * 1. CustomFormats (ServiceSpecificResourceProviders): Service-specific custom format resource providers
     *    - Would have allowed different resource providers for Radarr vs Sonarr custom formats
     *    - Example: Different git repos or local paths for each service's custom formats
     *
     * 2. MediaNaming (ServiceSpecificResourceProviders): Service-specific media naming resource providers
     *    - Would have allowed different resource providers for Radarr vs Sonarr media naming
     *    - Example: Different git repos or local paths for each service's naming conventions
     *
     * 3. ServiceSpecificResourceProviders record: Container for service-specific providers
     *    - Had Radarr and Sonarr collections of IUnderlyingResourceProvider
     *    - Enabled per-service resource provider configuration
     *
     * 4. LocalPathSource record: File system-based resource provider
     *    - Allowed specifying local file paths as resource sources
     *    - Had Path and Service properties for local resource discovery
     *
     * These were designed for flexibility and should be reconsidered if users need:
     * - Service-specific resource configuration (different providers per service)
     * - Local file system resource sources (non-git based resources)
     * - Granular control over resource provider selection
     */
}

[SuppressMessage("Design", "CA1040:Avoid empty interfaces")]
public interface IUnderlyingResourceProvider;

public record GitRepositorySource : IUnderlyingResourceProvider
{
    public string Name { get; init; } = "";
    public Uri CloneUrl { get; init; } = new("about:blank");
    public string Reference { get; init; } = "master";
}

public record LocalPathSource : IUnderlyingResourceProvider
{
    public string Path { get; init; } = "";
}
