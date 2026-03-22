using Terminal.Gui.Input;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard.Widgets;

// TextField binds Ctrl+C to Command.Copy at the view level, which fires
// before the app-level QuitKey binding. When text is selected (Terminal.Gui
// auto-selects on keyboard focus), Copy consumes the key and prevents quit.
// Removing the binding lets Ctrl+C fall through to the app-level QuitKey.
internal sealed class WizardTextField : TextField
{
    public WizardTextField()
    {
        KeyBindings.Remove(Key.C.WithCtrl);
    }
}
