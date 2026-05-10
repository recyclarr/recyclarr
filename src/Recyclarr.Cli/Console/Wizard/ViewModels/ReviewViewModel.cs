using ReactiveUI.SourceGenerators;

namespace Recyclarr.Cli.Console.Wizard.ViewModels;

// Structured data for the review step's colored multi-widget layout
internal record ReviewSection(string Header, IReadOnlyList<ReviewItem> Items);

internal record ReviewItem
{
    // Key-value row: "Name:" in default color, "movies" in accent
    public static ReviewItem KeyValue(string label, string value) =>
        new() { Label = label, Value = value };

    // Value-only row: entire text in accent
    public static ReviewItem ValueOnly(string value) => new() { Value = value };

    // Sub-header row: dimmed text for grouping within a section
    public static ReviewItem SubHeader(string text) => new() { Value = text, IsSubHeader = true };

    public string? Label { get; private init; }
    public required string Value { get; init; }
    public bool IsSubHeader { get; private init; }
}

internal partial class ReviewViewModel(WizardViewModel wizard) : WizardStepViewModel
{
    [Reactive]
    private IReadOnlyList<ReviewSection> _sections = [];

    public override string SectionName => "Review & Generate";

    public override void Activate()
    {
        Sections = BuildSections();
    }

    private List<ReviewSection> BuildSections()
    {
        var sections = new List<ReviewSection>
        {
            new(
                "Service",
                [
                    ReviewItem.KeyValue("Name:", wizard.InstanceName),
                    ReviewItem.KeyValue("URL:", wizard.BaseUrl),
                ]
            ),
            new("Category", [ReviewItem.ValueOnly($"{wizard.Category}")]),
            BuildProfilesSection(),
            BuildCfGroupsSection(),
            BuildQualitySizesSection(),
            BuildMediaNamingSection(),
        };

        return sections;
    }

    private ReviewSection BuildProfilesSection()
    {
        var profiles = wizard.SelectedProfiles;
        var items =
            profiles.Count > 0
                ? profiles.Select(p => ReviewItem.ValueOnly(p.Label)).ToList()
                : [ReviewItem.ValueOnly("None selected")];

        return new ReviewSection("Quality Profiles", items);
    }

    private ReviewSection BuildCfGroupsSection()
    {
        var items = new List<ReviewItem>();

        var skippedGroups = wizard.SkippedCfGroups;
        items.Add(ReviewItem.SubHeader("Skipped (defaults):"));
        if (skippedGroups.Count > 0)
        {
            items.AddRange(skippedGroups.Select(g => ReviewItem.ValueOnly(g.Label)));
        }
        else
        {
            items.Add(ReviewItem.ValueOnly("None"));
        }

        var addedGroups = wizard.AddedCfGroups;
        items.Add(ReviewItem.SubHeader("Added (optional):"));
        if (addedGroups.Count > 0)
        {
            items.AddRange(addedGroups.Select(g => ReviewItem.ValueOnly(g.Label)));
        }
        else
        {
            items.Add(ReviewItem.ValueOnly("None"));
        }

        return new ReviewSection("Custom Format Groups", items);
    }

    private ReviewSection BuildQualitySizesSection()
    {
        var enabled = wizard.UseQualitySizes;
        var items = new List<ReviewItem>
        {
            ReviewItem.KeyValue("Enabled:", enabled ? "Yes" : "No"),
        };

        if (enabled)
        {
            var qualitySizeType = WizardViewModel.QualitySizeType(
                wizard.ServiceType,
                wizard.Category
            );
            items.Add(ReviewItem.KeyValue("Type:", qualitySizeType));
        }

        return new ReviewSection("Quality Sizes", items);
    }

    private ReviewSection BuildMediaNamingSection()
    {
        var enabled = wizard.UseMediaNaming;
        var items = new List<ReviewItem>
        {
            ReviewItem.KeyValue("Enabled:", enabled ? "Yes" : "No"),
        };

        if (enabled)
        {
            var server = wizard.MediaServer;
            items.Add(ReviewItem.KeyValue("Server:", $"{server}"));

            if (server != MediaServer.None && wizard.NamingIdType is { } idType)
            {
                items.Add(ReviewItem.KeyValue("ID Type:", idType.DisplayName));
            }
        }

        return new ReviewSection("Media Naming", items);
    }
}
