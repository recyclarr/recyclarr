using Recyclarr.Client.V1;

namespace Recyclarr.Cli.Processors.Sync.Results;

internal static class PipelineNames
{
    public const string CustomFormats = "Custom Formats";
    public const string QualityProfiles = "Quality Profiles";
    public const string QualitySizes = "Quality Sizes";
    public const string MediaNaming = "Media Naming";
    public const string MediaManagement = "Media Management";

    public static string Of(BlockingPipeline pipeline) =>
        pipeline switch
        {
            BlockingPipeline.CustomFormat => CustomFormats,
            BlockingPipeline.QualityProfile => QualityProfiles,
            BlockingPipeline.QualitySize => QualitySizes,
            BlockingPipeline.MediaNaming => MediaNaming,
            BlockingPipeline.MediaManagement => MediaManagement,
            _ => throw new ArgumentOutOfRangeException(nameof(pipeline), pipeline, null),
        };
}
