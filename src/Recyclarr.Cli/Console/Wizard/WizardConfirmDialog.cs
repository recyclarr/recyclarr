using System.Diagnostics.CodeAnalysis;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard;

internal static class WizardConfirmDialog
{
    [SuppressMessage(
        "Reliability",
        "CA2000",
        Justification = "Dialog.Dispose() disposes all children added via Add/AddButton"
    )]
    public static bool Query(IApplication app, string title, string message)
    {
        using var dialog = new Dialog
        {
            Title = title,
            BorderStyle = LineStyle.Rounded,
            ShadowStyle = ShadowStyle.None,
            ButtonAlignment = Alignment.Center,
            SchemeName = WizardSchemes.ConfirmDialog,
        };

        dialog.Add(
            new Label
            {
                Text = message,
                X = 1,
                Y = 1,
                Width = Dim.Fill(1),
                TextAlignment = Alignment.Center,
            }
        );

        dialog.AddButton(new Button { Title = "_Yes" });
        dialog.AddButton(new Button { Title = "_No" });

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
