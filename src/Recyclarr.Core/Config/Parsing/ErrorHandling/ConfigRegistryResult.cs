using Recyclarr.Config.Models;

namespace Recyclarr.Config.Parsing.ErrorHandling;

public record ConfigRegistryResult(
    IReadOnlyCollection<IServiceConfiguration> Configs,
    IReadOnlyCollection<ConfigParsingException> Failures
);
