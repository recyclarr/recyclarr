using FluentValidation;
using JetBrains.Annotations;
using Recyclarr.Common.Extensions;
using Recyclarr.Common.FluentValidation;

namespace Recyclarr.Config.Data.V1;

[UsedImplicitly]
public class RootConfigYamlValidator : CustomValidator<RootConfigYaml>
{
    public RootConfigYamlValidator()
    {
        RuleFor(x => x).Must(x => x.Radarr.IsEmpty() && x.Sonarr.IsEmpty())
            .WithSeverity(Severity.Warning)
            .WithMessage(
                "Found array-style list of instances instead of named-style. " +
                "Array-style lists of Sonarr/Radarr instances are deprecated. " +
                "See: https://recyclarr.dev/wiki/upgrade-guide/v5.0#instances-must-now-be-named");
    }
}
