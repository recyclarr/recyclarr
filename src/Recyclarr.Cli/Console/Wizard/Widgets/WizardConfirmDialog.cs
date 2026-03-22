using System.Diagnostics.CodeAnalysis;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard.Widgets;

[SuppressMessage("Performance", "CA1822", Justification = "Instance method for DI resolution")]
internal sealed class WizardConfirmDialog
{
    private const int DialogWidth = 60;

    [SuppressMessage(
        "Reliability",
        "CA2000",
        Justification = "Dialog.Dispose() disposes all children added via Add/AddButton"
    )]
    public bool Query(IApplication app, string title, string message)
    {
        using var dialog = new Dialog();
        dialog.Title = title;
        dialog.Width = DialogWidth;
        dialog.BorderStyle = LineStyle.Rounded;
        dialog.ShadowStyle = ShadowStyles.None;
        dialog.ButtonAlignment = Alignment.Center;
        dialog.SchemeName = WizardSchemes.ConfirmDialog;

        dialog.Add(
            new Label
            {
                Text = message,
                X = 2,
                Y = 1,
                Width = DialogWidth - 8,
                TextAlignment = Alignment.Center,
            }
        );

        dialog.AddButton(
            new Button { Title = "_Yes", SchemeName = WizardSchemes.ConfirmDialogButton }
        );
        dialog.AddButton(
            new Button { Title = "_No", SchemeName = WizardSchemes.ConfirmDialogButton }
        );

        // Prevent the arrow chrome that Dialog auto-applies to the
        // last button via IsDefault
        foreach (var btn in dialog.Buttons)
        {
            btn.IsDefault = false;
        }

        app.Run(dialog);
        return dialog.Result == 0;
    }
}
