using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveUI;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Recyclarr.Cli.Console.Wizard.Widgets;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard.Steps;

internal enum MediaNamingChoice
{
    Yes,
    No,
}

[SuppressMessage(
    "Reliability",
    "CA2000",
    Justification = "Terminal.Gui manages disposal via Add()"
)]
internal sealed class MediaNamingStepView : WizardStepViewBase<MediaNamingViewModel>
{
    public MediaNamingStepView(MediaNamingViewModel viewModel, WizardOptionSelector optionSelector)
    {
        ViewModel = viewModel;

        var question = CreateQuestion("Sync media naming from TRaSH Guides?");

        var namingSelector = optionSelector.Create<MediaNamingChoice>(Orientation.Horizontal);
        namingSelector.Y = Pos.Bottom(question) + 1;

        // Media server selector (conditionally visible)
        var serverLabel = new Label
        {
            Text = "Media server",
            Y = Pos.Bottom(namingSelector) + 1,
            Visible = false,
            SchemeName = WizardSchemes.HintText,
        };

        var serverSelector = optionSelector.Create<MediaServer>(Orientation.Horizontal);
        serverSelector.Y = Pos.Bottom(serverLabel);
        serverSelector.Visible = false;

        // ID type selector (conditionally visible)
        var idLabel = new Label
        {
            Text = "ID type for folder matching",
            Y = Pos.Bottom(serverSelector) + 1,
            Visible = false,
            SchemeName = WizardSchemes.HintText,
        };

        // Non-generic because available options are a dynamic subset of
        // NamingIdType (OptionSelector<TEnum> blocks setting Values).
        var idSelector = optionSelector.Create(Orientation.Horizontal);
        idSelector.Y = Pos.Bottom(idLabel);
        idSelector.Visible = false;

        var hint = CreateHint(
            "Sets file and folder naming conventions. Guide defaults are recommended.",
            Pos.Bottom(idSelector) + 1
        );

        Add(question, namingSelector, serverLabel, serverSelector, idLabel, idSelector, hint);

        // VM -> View: restore Yes/No selection
        viewModel
            .WhenAnyValue(x => x.UseNaming)
            .Select(v => v is true ? MediaNamingChoice.Yes : MediaNamingChoice.No)
            .Subscribe(v => namingSelector.Value = v)
            .DisposeWith(Disposables);

        // View -> VM: Yes/No
        namingSelector
            .ObserveValue()
            .Select(v => v is MediaNamingChoice.Yes)
            .BindTo(viewModel, x => x.UseNaming)
            .DisposeWith(Disposables);

        // VM -> View: toggle server selector visibility and restore selection.
        // Combined into one subscription so value restore fires when visibility
        // changes (e.g. navigating back sets SelectedServer before ShowServerSelector).
        viewModel
            .WhenAnyValue(x => x.ShowServerSelector, x => x.SelectedServer)
            .Subscribe(t =>
            {
                var (show, server) = t;
                serverLabel.Visible = show;
                serverSelector.Visible = show;
                if (show && server.HasValue)
                {
                    serverSelector.Value = server;
                }
            })
            .DisposeWith(Disposables);

        // View -> VM: server (skip when hidden to preserve selection for restore)
        serverSelector
            .ObserveValue(o => o.Where(_ => serverSelector.Visible))
            .Select(v => v ?? MediaServer.None)
            .BindTo(viewModel, x => x.SelectedServer)
            .DisposeWith(Disposables);

        // VM -> View: update ID type options (labels + values) when available types change
        viewModel
            .WhenAnyValue(x => x.AvailableIdTypes, x => x.RecommendedIdType)
            .Where(t => t.Item1.Count > 0)
            .Subscribe(t =>
            {
                var (types, recommended) = t;
                idSelector.Labels = types
                    .Select(id =>
                        id == recommended ? $"{id.DisplayName} (Recommended)" : id.DisplayName
                    )
                    .ToArray();
                idSelector.Values = types.Select(id => (int)(object)id).ToArray();
            })
            .DisposeWith(Disposables);

        // VM -> View: toggle ID type selector visibility and restore selection.
        // Combined so value restore fires when visibility changes.
        viewModel
            .WhenAnyValue(x => x.ShowIdTypeSelector, x => x.SelectedIdType)
            .Subscribe(t =>
            {
                var (show, idType) = t;
                idLabel.Visible = show;
                idSelector.Visible = show;
                if (show && idType.HasValue)
                {
                    idSelector.Value = (int)(object)idType.Value;
                }
            })
            .DisposeWith(Disposables);

        // View -> VM: ID type (skip when hidden to preserve selection for restore).
        // Manual FromEventPattern: non-generic OptionSelector uses a different event
        // type (ValueChangedEventArgs<int?>) than the generic variant, and
        // ReactiveMarbles can't generate Events() for it either (see
        // https://github.com/reactivemarbles/ObservableEvents/issues/117).
        Observable
            .FromEventPattern<
                EventHandler<ValueChangedEventArgs<int?>>,
                ValueChangedEventArgs<int?>
            >(h => idSelector.ValueChanged += h, h => idSelector.ValueChanged -= h)
            .Where(_ => idSelector.Visible)
            .Select(e =>
                e.EventArgs.NewValue.HasValue
                    ? (NamingIdType)e.EventArgs.NewValue.Value
                    : (NamingIdType?)null
            )
            .DistinctUntilChanged()
            .BindTo(viewModel, x => x.SelectedIdType)
            .DisposeWith(Disposables);
    }
}
