using System.Reactive.Disposables.Fluent;
using ReactiveUI;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard.Steps;

internal sealed class ReviewStepView : WizardStepViewBase<ReviewViewModel>
{
    public ReviewStepView(ReviewViewModel viewModel)
    {
        ViewModel = viewModel;

        var question = CreateQuestion("Review your configuration");

        var summaryView = new TextView
        {
            Y = Pos.Bottom(question) + 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 2,
            ReadOnly = true,
            WordWrap = true,
        };

        var hint = CreateHint(
            "YAML generation coming soon. Press Enter to finish.",
            Pos.Bottom(summaryView) + 1
        );
        Add(question, summaryView, hint);

        viewModel
            .WhenAnyValue(x => x.Summary)
            .Subscribe(text => summaryView.Text = text)
            .DisposeWith(Disposables);
    }
}
