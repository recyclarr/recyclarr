using Recyclarr.Servarr.QualitySize;

namespace Recyclarr.Cli.Tests.Reusable;

internal static class NewQualityDefinition
{
    public static QualityDefinitionItem Item(
        string qualityName,
        decimal minSize = 0,
        decimal? maxSize = null,
        decimal? preferredSize = null
    )
    {
        return new QualityDefinitionItem
        {
            QualityName = qualityName,
            MinSize = minSize,
            MaxSize = maxSize,
            PreferredSize = preferredSize,
        };
    }
}
