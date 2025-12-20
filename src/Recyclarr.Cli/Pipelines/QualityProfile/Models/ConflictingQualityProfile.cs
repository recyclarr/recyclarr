using Recyclarr.Cli.Pipelines.Plan;

namespace Recyclarr.Cli.Pipelines.QualityProfile.Models;

internal record ConflictingQualityProfile(PlannedQualityProfile PlannedProfile, int ConflictingId);
