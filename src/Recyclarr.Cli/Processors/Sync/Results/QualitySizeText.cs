using System.Globalization;
using Recyclarr.Client.V1;

namespace Recyclarr.Cli.Processors.Sync.Results;

internal static class QualitySizeText
{
    public static string Of(QualitySizeValueResponse? size) =>
        size switch
        {
            null => FieldChange.Unset,
            { Kind: QualitySizeKind.Unlimited } => "Unlimited",
            { Value: { } value } => value.ToString(CultureInfo.InvariantCulture),
            _ => FieldChange.Unset,
        };
}
