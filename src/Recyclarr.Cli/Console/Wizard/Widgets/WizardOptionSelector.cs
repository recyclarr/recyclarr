using System.Diagnostics.CodeAnalysis;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard.Widgets;

// Factory for OptionSelector instances with wizard-standard configuration:
// - TabBehavior.NoStop (arrow keys navigate items; Tab moves between groups)
// Space handling is intercepted at the app level in WizardApplication to prevent
// Terminal.Gui's default cycling behavior (see SelectFocusedOption).
[SuppressMessage("Performance", "CA1822", Justification = "Instance methods for DI resolution")]
internal sealed class WizardOptionSelector
{
    public OptionSelector<TEnum> Create<TEnum>(Orientation orientation = Orientation.Vertical)
        where TEnum : struct, Enum
    {
        return CreateCore<OptionSelector<TEnum>>(orientation);
    }

    public OptionSelector Create(Orientation orientation = Orientation.Vertical)
    {
        return CreateCore<OptionSelector>(orientation);
    }

    private static T CreateCore<T>(Orientation orientation)
        where T : OptionSelector, new()
    {
        return new T { Orientation = orientation, TabBehavior = TabBehavior.NoStop };
    }
}
