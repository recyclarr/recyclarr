using System.Diagnostics.CodeAnalysis;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard.Widgets;

// Factory for FlagSelector instances with wizard-standard configuration:
// - TabBehavior.NoStop (arrow keys navigate items; Tab moves between groups)
// Space is intentionally preserved: it toggles checkboxes (desired behavior).
[SuppressMessage("Performance", "CA1822", Justification = "Instance method for DI resolution")]
internal sealed class WizardFlagSelector
{
    public FlagSelector Create()
    {
        return new FlagSelector { Width = Dim.Fill(), TabBehavior = TabBehavior.NoStop };
    }
}
