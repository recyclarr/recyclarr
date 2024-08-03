using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Pipelines.QualitySize.Models;

public record ProcessedQualitySizeData(string Type, IReadOnlyCollection<QualityItemWithLimits> Qualities);
