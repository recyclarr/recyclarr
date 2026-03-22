using System.Diagnostics.CodeAnalysis;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard.Widgets;

// Read-only sidebar showing wizard progress. Sections are highlighted
// based on which one contains the current step. Not interactive.
[SuppressMessage(
    "Reliability",
    "CA2000",
    Justification = "Terminal.Gui manages disposal via Add()"
)]
internal sealed class WizardProgressBar : FrameView
{
    private readonly List<Label> _labels = [];
    private readonly IReadOnlyList<string> _sectionNames;

    public WizardProgressBar(IReadOnlyList<string> sectionNames)
    {
        _sectionNames = sectionNames;
        Title = "Progress";
        Width = 25;
        Height = Dim.Fill();
        CanFocus = false;
        TabStop = TabBehavior.NoStop;
        BorderStyle = LineStyle.Rounded;
        SchemeName = WizardSchemes.PanelInactive;

        // Vertical padding from top border
        for (var i = 0; i < sectionNames.Count; i++)
        {
            var label = new Label
            {
                Y = i + 1,
                Width = Dim.Fill(),
                CanFocus = false,
            };
            _labels.Add(label);
            Add(label);
        }

        Update(currentSection: _sectionNames[0], completedSections: []);
    }

    public void Update(string currentSection, HashSet<string> completedSections)
    {
        for (var i = 0; i < _sectionNames.Count; i++)
        {
            var name = _sectionNames[i];
            var label = _labels[i];

            if (name == currentSection)
            {
                label.Text = $"  \u25cf  {name}";
                label.SchemeName = WizardSchemes.ProgressCurrent;
            }
            else if (completedSections.Contains(name))
            {
                label.Text = $"  \u2713  {name}";
                label.SchemeName = WizardSchemes.ProgressDone;
            }
            else
            {
                label.Text = $"  \u25cb  {name}";
                label.SchemeName = WizardSchemes.ProgressFuture;
            }
        }
    }
}
