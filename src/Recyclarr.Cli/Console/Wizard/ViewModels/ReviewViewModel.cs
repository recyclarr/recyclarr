using ReactiveUI.SourceGenerators;

namespace Recyclarr.Cli.Console.Wizard.ViewModels;

internal partial class ReviewViewModel(WizardViewModel wizard) : WizardStepViewModel
{
    [Reactive]
    private string _summary = "";

    public override string SectionName => "Review & Generate";

    public override void Activate()
    {
        Summary = BuildSummary();
    }

    private string BuildSummary()
    {
        var lines = new List<string>
        {
            "Configuration Summary",
            "",
            "Service:",
            $"  Type: {wizard.ServiceType}",
            $"  Name: {wizard.InstanceName}",
            $"  URL: {wizard.BaseUrl}",
            "",
            $"Category: {wizard.Category}",
            "",
            "Quality Profiles:",
        };

        var profiles = wizard.SelectedProfiles;
        if (profiles.Count > 0)
        {
            foreach (var profile in profiles)
            {
                lines.Add($"  - {profile.Label}");
            }
        }
        else
        {
            lines.Add("  None selected");
        }
        lines.Add("");

        lines.Add("Custom Format Groups:");

        var skippedGroups = wizard.SkippedCfGroups;
        lines.Add("  Skipped (defaults):");
        if (skippedGroups.Count > 0)
        {
            foreach (var group in skippedGroups)
            {
                lines.Add($"    - {group.Label}");
            }
        }
        else
        {
            lines.Add("    None");
        }

        var addedGroups = wizard.AddedCfGroups;
        lines.Add("  Added (optional):");
        if (addedGroups.Count > 0)
        {
            foreach (var group in addedGroups)
            {
                lines.Add($"    - {group.Label}");
            }
        }
        else
        {
            lines.Add("    None");
        }
        lines.Add("");

        var qualitySizeType = WizardViewModel.QualitySizeType(wizard.ServiceType, wizard.Category);
        lines.Add($"Quality Sizes: {(wizard.UseQualitySizes ? "Yes" : "No")}");
        if (wizard.UseQualitySizes)
        {
            lines.Add($"  Type: {qualitySizeType}");
        }
        lines.Add("");

        lines.Add($"Media Naming: {(wizard.UseMediaNaming ? "Yes" : "No")}");
        if (wizard.UseMediaNaming)
        {
            var server = wizard.MediaServer;
            lines.Add($"  Server: {server}");

            if (server != MediaServer.None && wizard.NamingIdType is { } idType)
            {
                lines.Add($"  ID Type: {idType.DisplayName}");
            }
        }

        return string.Join("\n", lines);
    }
}
