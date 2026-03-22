using System.Reactive.Disposables;
using ReactiveUI;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard.Steps;

// Base class for wizard step views providing layout defaults,
// IViewFor<T> boilerplate, and helper methods for styled labels and error display.
internal abstract class WizardStepViewBase<TViewModel> : View, IViewFor<TViewModel>
    where TViewModel : class
{
    protected readonly CompositeDisposable Disposables = [];

    public TViewModel? ViewModel { get; set; }
    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (TViewModel?)value;
    }

    protected WizardStepViewBase()
    {
        CanFocus = true;
        Width = Dim.Fill();
        Height = Dim.Fill();

        // Inset content from the panel border
        Margin!.Thickness = new Thickness(2, 1, 2, 0);
    }

    protected static Label CreateQuestion(string text)
    {
        return new Label
        {
            Text = $"\u203a {text}",
            Width = Dim.Fill(),
            SchemeName = WizardSchemes.Question,
        };
    }

    protected static Label CreateHint(string text, Pos y)
    {
        return new Label
        {
            Text = text,
            Y = y,
            Width = Dim.Fill(),
            SchemeName = WizardSchemes.HintText,
        };
    }

    protected static Label CreateErrorLabel(Pos y)
    {
        return new Label
        {
            Y = y,
            Width = Dim.Fill(),
            Height = 0,
            Visible = false,
            SchemeName = "Error",
        };
    }

    protected static void ShowError(Label label, string message)
    {
        label.Text = message;
        label.Height = 1;
        label.Visible = true;
    }

    protected static void HideError(Label label)
    {
        label.Visible = false;
        label.Height = 0;
        label.Text = "";
    }

    protected override void Dispose(bool disposing)
    {
        Disposables.Dispose();
        base.Dispose(disposing);
    }
}
