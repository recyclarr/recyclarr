using Recyclarr.ErrorHandling;
using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;

namespace Recyclarr.Core.Tests.Sync;

internal sealed class SyncOutcomeFormatterTest
{
    [TestCaseSource(nameof(Cases))]
    public void Formats_compatibility_messages(SyncOutcome outcome, string[] expected)
    {
        SyncOutcomeFormatter.Format(outcome).Should().Equal(expected);
    }

    [Test]
    public void Cases_cover_every_concrete_outcome()
    {
        var outcomeTypes = typeof(SyncOutcome)
            .Assembly.GetTypes()
            .Where(type => !type.IsAbstract && type.IsAssignableTo(typeof(SyncOutcome)));
        var testedTypes = Cases()
            .SelectMany(test => test.Arguments)
            .OfType<SyncOutcome>()
            .Select(outcome => outcome.GetType());

        testedTypes.Should().BeEquivalentTo(outcomeTypes);
    }

    private static IEnumerable<TestCaseData> Cases()
    {
        yield return Case(
            new InvalidNamingFormatOutcome("Movie Folder Format", "missing"),
            "Invalid Movie Folder Format naming format: missing"
        );
        yield return Case(
            new QualityDefinitionNotFoundOutcome("movie"),
            "The specified quality definition type does not exist: movie"
        );
        yield return Case(
            new QualityNotFoundOutcome("Bluray", "movie"),
            "Quality 'Bluray' does not exist in the guide for type 'movie'"
        );
        yield return Case(
            new PreferredRatioClampedOutcome(2, 1),
            "preferred_ratio of 2 is out of range (0.0-1.0), clamped to 1"
        );
        yield return Case(
            new MinGreaterThanPreferredOutcome("Bluray", 10, 5),
            "Quality 'Bluray': min (10) cannot be greater than preferred (5)"
        );
        yield return Case(
            new UnlimitedPreferredGreaterThanMaxOutcome("Bluray", 100),
            "Quality 'Bluray': preferred (unlimited) cannot be greater than max (100)"
        );
        yield return Case(
            new PreferredGreaterThanMaxOutcome("Bluray", 100, 50),
            "Quality 'Bluray': preferred (100) cannot be greater than max (50)"
        );
        yield return Case(
            new DuplicateQualityProfileNameOutcome("WEB"),
            "Duplicate quality profile name 'WEB'. Each quality profile must have a unique name."
        );
        yield return Case(
            new CustomFormatServiceIdCollisionOutcome("One", "id-1", "Two", "id-2", 3),
            "Custom formats 'One' (id-1) and 'Two' (id-2) both resolve to the same custom "
                + "format in the service (ID 3). Remove one from your config or resource provider."
        );
        yield return Case(
            new InvalidQualityProfileTrashIdOutcome("bad-qp"),
            "Invalid quality profile trash_id: bad-qp"
        );
        yield return Case(
            new InvalidCustomFormatTrashIdOutcome("bad-cf"),
            "Invalid trash_id: bad-cf"
        );
        yield return Case(
            new InvalidCfGroupSkipIdOutcome("bad-group"),
            "Skip ID 'bad-group' does not match any known CF group"
        );
        yield return Case(
            new IncompatibleCfGroupOutcome("Audio", "audio-id"),
            "CF group 'Audio' (audio-id) was skipped because none of your quality profiles are "
                + "in its compatibility list. Add `assign_scores_to` to explicitly target a profile."
        );
        yield return Case(
            new EmptyCfGroupOutcome("Audio", "audio-id"),
            "CF group 'Audio' (audio-id) has no custom formats after applying opt-in semantics. "
                + "All CFs in this group are optional; use `select` to pick specific CFs to include."
        );
        yield return Case(
            new AmbiguousProfileReferenceOutcome("custom_formats", "qp-id", ["One", "Two"]),
            "[custom_formats] trash_id 'qp-id' matches multiple profiles: 'One', 'Two'. "
                + "Use 'name' to specify which profile to target."
        );
        yield return Case(
            new RuleValidationOutcome(
                SyncDiagnosticLevel.Error,
                "trash_id",
                "Invalid CF trash_id",
                "bad",
                "NotEmpty"
            ),
            "Invalid CF trash_id"
        );
        yield return Case(new NoConfigurationFilesFailure(), "No configuration files found");
        yield return Case(
            new InvalidInstancesFailure(["one", "two"]),
            "Invalid instances: one, two"
        );
        yield return Case(
            new DuplicateInstancesFailure(["one", "two"]),
            "Duplicate instance names: one, two"
        );
        yield return Case(
            new SplitInstancesFailure(["one", "two"]),
            "Configs sharing base_url not allowed: one, two"
        );
        yield return Case(
            new InvalidConfigurationFilesFailure(["one.yml", "two.yml"]),
            "Config files not found: one.yml, two.yml"
        );
        yield return Case(
            new InvalidConfigurationFailure(),
            "One or more invalid configurations found"
        );
        yield return Case(new PostProcessingFailure("post failed"), "post failed");
        yield return Case(new EnvironmentFailure("environment failed"), "environment failed");
        yield return Case(new ServiceFailure("service failed"), "service failed");
        yield return Case(new GitFailure(2), "Git command failed with exit code 2");
        yield return Case(new HttpConnectionFailure(), "Connection failed - check your base_url");
        yield return Case(
            new HttpApiFailure(
                400,
                [new HttpApiResponseMessage("Bad request")],
                HasRequestContent: true,
                "{}"
            ),
            "HTTP 400: Bad request",
            "Request body: {}"
        );
        yield return Case(
            new MigrationFailure("move state", "disk full", ["free space"]),
            "Migration step failed: move state",
            "Reason: disk full",
            "  - free space"
        );
        yield return Case(
            new ContextualValidationFailure(
                "Profile validation",
                "WEB",
                [new ValidationFailureDetail("Cutoff", "Invalid cutoff", "bad", "Rule")]
            ),
            "[WEB] Invalid cutoff",
            "Profile validation failed with 1 error(s)"
        );
        yield return Case(
            new ConfigParsingFailure("recyclarr.yml", 5, "invalid value"),
            "recyclarr.yml line 5: invalid value"
        );
        yield return Case(
            new YamlErrorFailure(5, "invalid value"),
            "YAML error at line 5: invalid value"
        );
        yield return Case(new YamlParseFailure(5), "YAML parse error at line 5");
    }

    private static TestCaseData Case(SyncOutcome outcome, params string[] messages)
    {
        return new TestCaseData(outcome, messages).SetName(outcome.GetType().Name);
    }
}
