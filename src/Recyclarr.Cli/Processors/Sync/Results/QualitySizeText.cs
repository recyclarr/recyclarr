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

    /// <summary>
    /// The size fields a quality changes, labeled with <paramref name="prefix"/> (empty for
    /// instance-wide sizes, the quality name for sizes listed under a profile). A created quality
    /// lists every value it sets.
    /// </summary>
    public static IEnumerable<FieldChange> Fields(QualitySizeItemResponse size, string prefix)
    {
        (string Label, PlannedValueResponseOfQualitySizeValueResponse Value)[] values =
        [
            ("Min", size.Minimum),
            ("Preferred", size.Preferred),
            ("Max", size.Maximum),
        ];

        return values
            .Where(x => size.Action == ResourceAction.Create || x.Value.Changed)
            .Select(x => new FieldChange(
                prefix + x.Label,
                Of(x.Value.Current),
                Of(x.Value.Desired)
            ));
    }
}
