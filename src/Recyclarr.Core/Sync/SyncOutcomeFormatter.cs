using System.Net;
using Recyclarr.ErrorHandling;
using Recyclarr.Pipelines.Plan;

namespace Recyclarr.Sync;

public static class SyncOutcomeFormatter
{
    public static IReadOnlyList<string> Format(SyncOutcome outcome)
    {
        return outcome switch
        {
            InvalidNamingFormatOutcome x =>
            [
                $"Invalid {x.FormatType} naming format: {x.ConfigValue}",
            ],
            QualityDefinitionNotFoundOutcome x =>
            [
                $"The specified quality definition type does not exist: {x.Type}",
            ],
            QualityNotFoundOutcome x =>
            [
                $"Quality '{x.Quality}' does not exist in the guide for type '{x.Type}'",
            ],
            PreferredRatioClampedOutcome x =>
            [
                $"preferred_ratio of {x.Original} is out of range (0.0-1.0), "
                    + $"clamped to {x.Clamped}",
            ],
            MinGreaterThanPreferredOutcome x =>
            [
                $"Quality '{x.Quality}': min ({x.Min}) cannot be greater than preferred "
                    + $"({x.Preferred})",
            ],
            UnlimitedPreferredGreaterThanMaxOutcome x =>
            [
                $"Quality '{x.Quality}': preferred (unlimited) cannot be greater than max "
                    + $"({x.Max})",
            ],
            PreferredGreaterThanMaxOutcome x =>
            [
                $"Quality '{x.Quality}': preferred ({x.Preferred}) cannot be greater than "
                    + $"max ({x.Max})",
            ],
            DuplicateQualityProfileNameOutcome x =>
            [
                $"Duplicate quality profile name '{x.Name}'. "
                    + "Each quality profile must have a unique name.",
            ],
            CustomFormatServiceIdCollisionOutcome x =>
            [
                $"Custom formats '{x.ExistingName}' ({x.ExistingTrashId}) and "
                    + $"'{x.NewName}' ({x.NewTrashId}) both resolve to the same custom format "
                    + $"in the service (ID {x.ServiceId}). Remove one from your config or "
                    + "resource provider.",
            ],
            InvalidQualityProfileTrashIdOutcome x =>
            [
                $"Invalid quality profile trash_id: {x.TrashId}",
            ],
            InvalidCustomFormatTrashIdOutcome x => [$"Invalid trash_id: {x.TrashId}"],
            InvalidCfGroupSkipIdOutcome x =>
            [
                $"Skip ID '{x.TrashId}' does not match any known CF group",
            ],
            IncompatibleCfGroupOutcome x =>
            [
                $"CF group '{x.Name}' ({x.TrashId}) was skipped because none of your quality "
                    + "profiles are in its compatibility list. Add `assign_scores_to` to "
                    + "explicitly target a profile.",
            ],
            EmptyCfGroupOutcome x =>
            [
                $"CF group '{x.Name}' ({x.TrashId}) has no custom formats after applying "
                    + "opt-in semantics. All CFs in this group are optional; use `select` to "
                    + "pick specific CFs to include.",
            ],
            AmbiguousProfileReferenceOutcome x =>
            [
                $"[{x.Context}] trash_id '{x.TrashId}' matches multiple profiles: "
                    + $"{string.Join(", ", x.ProfileNames.Select(name => $"'{name}'"))}. "
                    + "Use 'name' to specify which profile to target.",
            ],
            RuleValidationOutcome x => [x.Message],
            HandledInstanceFailure x => FormatHandledFailure(x),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };
    }

    private static List<string> FormatHandledFailure(HandledInstanceFailure failure)
    {
        return failure switch
        {
            NoConfigurationFilesFailure => ["No configuration files found"],
            InvalidInstancesFailure x =>
            [
                $"Invalid instances: {string.Join(", ", x.InstanceNames)}",
            ],
            DuplicateInstancesFailure x =>
            [
                $"Duplicate instance names: {string.Join(", ", x.InstanceNames)}",
            ],
            SplitInstancesFailure x =>
            [
                $"Configs sharing base_url not allowed: {string.Join(", ", x.InstanceNames)}",
            ],
            InvalidConfigurationFilesFailure x =>
            [
                $"Config files not found: {string.Join(", ", x.FileNames)}",
            ],
            InvalidConfigurationFailure => ["One or more invalid configurations found"],
            PostProcessingFailure x => [x.Message],
            EnvironmentFailure x => [x.Message],
            ServiceFailure x => [x.Message],
            GitFailure x => [$"Git command failed with exit code {x.ExitCode}"],
            HttpConnectionFailure => ["Connection failed - check your base_url"],
            HttpApiFailure x => FormatHttpFailure(x),
            MigrationFailure x =>
            [
                $"Migration step failed: {x.OperationDescription}",
                $"Reason: {x.Reason}",
                .. x.Remediation.Select(item => $"  - {item}"),
            ],
            ContextualValidationFailure x =>
            [
                .. x.Failures.Select(detail =>
                    !string.IsNullOrEmpty(x.ErrorPrefix)
                        ? $"[{x.ErrorPrefix}] {detail.Message}"
                        : detail.Message
                ),
                $"{x.Context} failed with {x.Failures.Count} error(s)",
            ],
            ConfigParsingFailure x =>
            [
                x.FileName is not null
                    ? $"{x.FileName} line {x.Line}: {x.Message}"
                    : $"Line {x.Line}: {x.Message}",
            ],
            YamlErrorFailure x => [$"YAML error at line {x.Line}: {x.Message}"],
            YamlParseFailure x => [$"YAML parse error at line {x.Line}"],
            _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, null),
        };
    }

    private static List<string> FormatHttpFailure(HttpApiFailure failure)
    {
        var status = $"HTTP {failure.StatusCode}";
        var messages = new List<string>();

        if (failure.StatusCode == (int)HttpStatusCode.Unauthorized)
        {
            messages.Add($"{status}: Unauthorized - check your api_key");
        }
        else if (failure.ResponseMessages.Count == 0)
        {
            messages.Add(status);
        }
        else
        {
            messages.AddRange(
                failure.ResponseMessages.Select(message =>
                    message switch
                    {
                        HttpApiResponseMessage x => $"{status}: {x.Message}",
                        HttpApiFieldError x => $"{x.Field}: {x.Message}",
                        _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, null),
                    }
                )
            );
        }

        if (failure.HasRequestContent)
        {
            messages.Add($"Request body: {failure.RequestBody}");
        }

        return messages;
    }
}
