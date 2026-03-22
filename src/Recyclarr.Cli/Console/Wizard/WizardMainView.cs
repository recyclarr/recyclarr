using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using ReactiveUI;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard;

[SuppressMessage(
    "Reliability",
    "CA2213",
    Justification = "Terminal.Gui manages disposal via Add()"
)]
[SuppressMessage(
    "Reliability",
    "CA2000",
    Justification = "Terminal.Gui manages disposal via Add()"
)]
internal sealed class WizardMainView : Runnable, IViewFor<WizardViewModel>
{
    private readonly CompositeDisposable _disposable = [];
    private readonly WizardProgressBar _progressBar;
    private readonly FrameView _contentPanel;
    private readonly View _navHintsBar;
    private readonly IReadOnlyList<View> _stepViews;

    public WizardMainView(WizardViewModel viewModel, IReadOnlyList<View> stepViews)
    {
        ViewModel = viewModel;
        _stepViews = stepViews;

        Title = "Recyclarr Config Wizard";
        Width = Dim.Fill();
        Height = Dim.Fill();

        _progressBar = new WizardProgressBar(viewModel.SectionNames);

        _contentPanel = new FrameView
        {
            X = Pos.Right(_progressBar),
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            CanFocus = true,
            TabStop = TabBehavior.TabGroup,
            BorderStyle = LineStyle.Rounded,
            SchemeName = WizardSchemes.Panel,
        };

        _navHintsBar = new View
        {
            X = Pos.Right(_progressBar),
            Y = Pos.Bottom(_contentPanel),
            Width = Dim.Fill(),
            Height = 1,
            CanFocus = false,
        };

        Add(_progressBar, _contentPanel, _navHintsBar);

        // React to step changes from the ViewModel
        viewModel
            .WhenAnyValue(x => x.CurrentStepIndex)
            .Subscribe(ShowStep)
            .DisposeWith(_disposable);

        viewModel.Finished.Subscribe(_ => RequestStop()).DisposeWith(_disposable);
    }

    public WizardViewModel? ViewModel { get; set; }
    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = (WizardViewModel?)value;
    }

    private void ShowStep(int index)
    {
        var vm = ViewModel!; // non-null: set in constructor, never cleared
        var stepView = _stepViews[index];

        // Swap content
        _contentPanel.RemoveAll();
        _contentPanel.Title = vm.CurrentSectionName;
        _contentPanel.Add(stepView);

        // Update progress sidebar
        _progressBar.Update(vm.CurrentSectionName, vm.CompletedSections);

        UpdateNavHints(vm, index);
        _contentPanel.SetFocus();
    }

    private void UpdateNavHints(WizardViewModel vm, int index)
    {
        _navHintsBar.RemoveAll();

        var isFirst = vm.IsFirstStep;
        var isLast = vm.IsLastStep;
        var action = isLast ? "Generate" : "Next";

        List<(string Text, string Scheme)> segments =
        [
            ($"  Step {index + 1} of {vm.StepCount}", WizardSchemes.NavHint),
            ("        ", WizardSchemes.NavHint),
            ("Enter ", WizardSchemes.NavKey),
            (action, WizardSchemes.NavHint),
        ];

        if (!isFirst)
        {
            segments.Add(("   ", WizardSchemes.NavHint));
            segments.Add(("Esc ", WizardSchemes.NavKey));
            segments.Add(("Back", WizardSchemes.NavHint));
        }

        segments.Add(("   ", WizardSchemes.NavHint));
        segments.Add(("Ctrl+C ", WizardSchemes.NavKey));
        segments.Add(("Quit", WizardSchemes.NavHint));

        View? previous = null;
        foreach (var (text, scheme) in segments)
        {
            var label = new Label
            {
                Text = text,
                X = previous is null ? 0 : Pos.Right(previous),
                Width = text.Length,
                CanFocus = false,
                SchemeName = scheme,
            };

            _navHintsBar.Add(label);
            previous = label;
        }
    }

    protected override void Dispose(bool disposing)
    {
        _disposable.Dispose();
        base.Dispose(disposing);
    }
}
