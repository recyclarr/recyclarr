using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Terminal.Gui.ViewBase;

namespace Recyclarr.Cli.Console.Wizard.Steps;

[SuppressMessage(
    "Reliability",
    "CA2000",
    Justification = "Terminal.Gui manages disposal via Add()"
)]
internal sealed class PlaceholderStepView : WizardStepViewBase<PlaceholderViewModel>
{
    public PlaceholderStepView(PlaceholderViewModel viewModel)
    {
        ViewModel = viewModel;

        var question = CreateQuestion(viewModel.Description);

        var hint = CreateHint(
            "This step is not yet implemented. Press Next to continue.",
            Pos.Bottom(question) + 1
        );

        Add(question, hint);
    }
}
