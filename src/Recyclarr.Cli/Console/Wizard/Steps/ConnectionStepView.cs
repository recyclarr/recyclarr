using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveUI;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Recyclarr.Cli.Console.Wizard.Widgets;
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
internal sealed class ConnectionStepView : WizardStepViewBase<ConnectionViewModel>
{
    public ConnectionStepView(ConnectionViewModel viewModel, WizardOptionSelector optionSelector)
    {
        ViewModel = viewModel;

        var question = CreateQuestion("Configure your instance connection");

        // Instance name
        var nameLabel = new Label
        {
            Text = "Instance name",
            Y = Pos.Bottom(question) + 1,
            SchemeName = WizardSchemes.HintText,
        };
        var nameField = new WizardTextField { Y = Pos.Bottom(nameLabel), Width = 40 };
        var nameError = CreateErrorLabel(Pos.Bottom(nameField));

        // Base URL
        var urlLabel = new Label
        {
            Text = "Base URL",
            Y = Pos.Bottom(nameError) + 1,
            SchemeName = WizardSchemes.HintText,
        };
        var urlField = new WizardTextField { Y = Pos.Bottom(urlLabel), Width = Dim.Fill() };
        var urlError = CreateErrorLabel(Pos.Bottom(urlField));

        // API key
        var apiKeyLabel = new Label
        {
            Text = "API key (Settings \u203a General in Sonarr/Radarr)",
            Y = Pos.Bottom(urlError) + 1,
            SchemeName = WizardSchemes.HintText,
        };
        var apiKeyField = new WizardTextField { Y = Pos.Bottom(apiKeyLabel), Width = Dim.Fill() };
        var apiKeyError = CreateErrorLabel(Pos.Bottom(apiKeyField));

        // Content category
        var categoryLabel = new Label
        {
            Text = "Content category",
            Y = Pos.Bottom(apiKeyError) + 1,
            SchemeName = WizardSchemes.HintText,
        };
        var categorySelector = optionSelector.Create<GuideCategory>(Orientation.Horizontal);
        categorySelector.Y = Pos.Bottom(categoryLabel);
        var categoryHint = CreateHint(
            "Filters quality profiles and custom formats to this category.",
            Pos.Bottom(categorySelector)
        );

        Add(
            question,
            nameLabel,
            nameField,
            nameError,
            urlLabel,
            urlField,
            urlError,
            apiKeyLabel,
            apiKeyField,
            apiKeyError,
            categoryLabel,
            categorySelector,
            categoryHint
        );

        // Two-way bindings
        viewModel.Name.BindTwoWay(nameField).DisposeWith(Disposables);
        viewModel.BaseUrl.BindTwoWay(urlField).DisposeWith(Disposables);
        viewModel.ApiKey.BindTwoWay(apiKeyField).DisposeWith(Disposables);

        viewModel
            .WhenAnyValue(x => x.Category)
            .Subscribe(v => categorySelector.Value = v)
            .DisposeWith(Disposables);

        // View -> VM
        categorySelector
            .ObserveValue()
            .Select(v => v ?? GuideCategory.Standard)
            .BindTo(viewModel, x => x.Category)
            .DisposeWith(Disposables);

        // Error display bindings
        viewModel
            .Name.ObserveValidationErrors()
            .Subscribe(err => ToggleError(nameError, err))
            .DisposeWith(Disposables);

        viewModel
            .BaseUrl.ObserveValidationErrors()
            .Subscribe(err => ToggleError(urlError, err))
            .DisposeWith(Disposables);

        viewModel
            .ApiKey.ObserveValidationErrors()
            .Subscribe(err => ToggleError(apiKeyError, err))
            .DisposeWith(Disposables);

        return;

        static void ToggleError(Label label, string? error)
        {
            if (string.IsNullOrEmpty(error))
            {
                HideError(label);
            }
            else
            {
                ShowError(label, error);
            }
        }
    }
}
