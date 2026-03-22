using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Recyclarr.Cli.Console.Wizard.Widgets;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard.Steps;

[SuppressMessage(
    "Reliability",
    "CA2000",
    Justification = "Terminal.Gui manages disposal via Add()"
)]
[SuppressMessage(
    "Reliability",
    "CA2213",
    Justification = "Terminal.Gui manages disposal via Add()"
)]
internal sealed class CfGroupStepView : WizardStepViewBase<CfGroupViewModel>
{
    private readonly WizardFlagSelector _flagSelector;

    public CfGroupStepView(CfGroupViewModel viewModel, WizardFlagSelector flagSelector)
    {
        _flagSelector = flagSelector;
        ViewModel = viewModel;

        var question = CreateQuestion("Configure CF group selections");

        // Side-by-side columns for skip and add panels
        var skipFrame = CreateGroupFrame("Skip Default Groups", X = 0, Dim.Percent(50) - 1);

        var addFrame = CreateGroupFrame("Add Optional Groups", Pos.Percent(50) + 1, Dim.Fill());

        skipFrame.Y = Pos.Bottom(question) + 1;
        skipFrame.Height = Dim.Fill();
        addFrame.Y = Pos.Bottom(question) + 1;
        addFrame.Height = Dim.Fill();

        var skipSelector = (FlagSelector)skipFrame.SubViews.First();
        var addSelector = (FlagSelector)addFrame.SubViews.First();

        Add(question, skipFrame, addFrame);

        // Skip panel bindings: VM -> View
        viewModel
            .WhenAnyValue(x => x.SkipLabels, x => x.SkipValues)
            .Subscribe(tuple =>
            {
                var (labels, values) = tuple;
                skipSelector.Labels = labels;
                skipSelector.Values = values;
            })
            .DisposeWith(Disposables);

        viewModel
            .WhenAnyValue(x => x.SkipSelectedFlagValue)
            .Subscribe(v => skipSelector.Value = v)
            .DisposeWith(Disposables);

        // Skip panel bindings: View -> VM
        skipSelector
            .Events()
            .ValueChanged.Select(e => e.NewValue)
            .DistinctUntilChanged()
            .BindTo(viewModel, x => x.SkipSelectedFlagValue)
            .DisposeWith(Disposables);

        // Add panel bindings: VM -> View
        viewModel
            .WhenAnyValue(x => x.AddLabels, x => x.AddValues)
            .Subscribe(tuple =>
            {
                var (labels, values) = tuple;
                addSelector.Labels = labels;
                addSelector.Values = values;
            })
            .DisposeWith(Disposables);

        viewModel
            .WhenAnyValue(x => x.AddSelectedFlagValue)
            .Subscribe(v => addSelector.Value = v)
            .DisposeWith(Disposables);

        // Add panel bindings: View -> VM
        addSelector
            .Events()
            .ValueChanged.Select(e => e.NewValue)
            .DistinctUntilChanged()
            .BindTo(viewModel, x => x.AddSelectedFlagValue)
            .DisposeWith(Disposables);

        // Track focus to highlight the active panel's border
        WireFocusHighlight(skipFrame);
        WireFocusHighlight(addFrame);
    }

    private FrameView CreateGroupFrame(string title, Pos x, Dim width)
    {
        var selector = _flagSelector.Create();

        var frame = new FrameView
        {
            Title = title,
            X = x,
            Width = width,
            BorderStyle = LineStyle.Rounded,
            SchemeName = WizardSchemes.PanelInactive,
            ViewportSettings = ViewportSettingsFlags.HasVerticalScrollBar,

            // TabStop (not TabGroup) so Tab cycles between the two panels
            TabStop = TabBehavior.TabStop,
        };

        frame.Add(selector);
        return frame;
    }

    // Switch border scheme when the frame (or any child) gains/loses focus
    private static void WireFocusHighlight(FrameView frame)
    {
        frame.HasFocusChanged += (_, args) =>
        {
            frame.SchemeName = args.NewValue ? WizardSchemes.Panel : WizardSchemes.PanelInactive;
        };
    }
}
