using System.Diagnostics.CodeAnalysis;
using System.Reactive.Concurrency;
using ReactiveUI.Builder;
using Recyclarr.Cli.Console.Wizard.Steps;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Recyclarr.Cli.Console.Wizard.Widgets;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Recyclarr.Cli.Console.Wizard;

// Uses Func<> delegates to defer ViewModel resolution until after
// ReactiveUI is initialized via RxAppBuilder (ReactiveObject subclasses
// cannot be constructed before RxApp initialization).
internal sealed class WizardApplication(
    ILogger logger,
    Func<WizardViewModel> wizardViewModelFactory,
    Func<IEnumerable<IWizardStepViewModel>> stepsFactory,
    WizardConfirmDialog confirmDialog,
    WizardOptionSelector optionSelector,
    WizardFlagSelector flagSelector
)
{
    [SuppressMessage(
        "ReSharper",
        "AccessToDisposedClosure",
        Justification = "Lambda is only invoked during app.Run(), before app is disposed"
    )]
    public void Run()
    {
        using var app = Application.Create();
        app.Init();

        RxAppBuilder
            .CreateReactiveUIBuilder()
            .WithMainThreadScheduler(new TerminalScheduler(app))
            .WithTaskPoolScheduler(TaskPoolScheduler.Default)
            .WithCoreServices()
            .BuildApp();

        // Resolve ViewModels after ReactiveUI initialization
        var wizardViewModel = wizardViewModelFactory();
        var steps = stepsFactory();
        wizardViewModel.Initialize(steps);

        // Wire confirmation dialog so VMs can prompt the user without
        // depending on Terminal.Gui directly.
        wizardViewModel.ShowConfirmation = (title, message) =>
            confirmDialog.Query(app, title, message);

        WizardSchemes.Register();

        // Remap quit to Ctrl+C; Esc is used for back navigation.
        Application.SetDefaultKeyBinding(Command.Quit, Bind.All(Key.C.WithCtrl));

        // Intercept navigation keys at the app level, before any widget
        // can consume them. This prevents Enter from toggling checkboxes
        // instead of advancing to the next step.
        app.Keyboard.KeyDown += OnKeyDown;

        var stepViews = CreateStepViews(wizardViewModel);
        using var wizard = new WizardMainView(logger, wizardViewModel, stepViews);

        try
        {
            app.Run(wizard);
        }
        catch (Exception ex)
        {
            // Re-throw with full details; the `using var app` disposes Terminal.Gui
            // so the stack trace prints to a normal terminal after cleanup.
            throw new InvalidOperationException(
                $"Wizard crashed: {ex.Message}\n{ex.StackTrace}",
                ex
            );
        }
    }

    // Create one View per step ViewModel, in matching order
    private List<View> CreateStepViews(WizardViewModel vm)
    {
        return vm
            .Steps.Select<IWizardStepViewModel, View>(step =>
                step switch
                {
                    ServiceTypeViewModel s => new ServiceTypeStepView(s, optionSelector),
                    ConnectionViewModel c => new ConnectionStepView(c, optionSelector),
                    QualityProfileViewModel q => new QualityProfileStepView(q, flagSelector),
                    CfGroupViewModel g => new CfGroupStepView(g, flagSelector),
                    QualitySizeViewModel q => new QualitySizeStepView(q, optionSelector),
                    MediaNamingViewModel m => new MediaNamingStepView(m, optionSelector),
                    ReviewViewModel r => new ReviewStepView(r),
                    _ => throw new InvalidOperationException(
                        $"No view registered for step ViewModel type {step.GetType().Name}"
                    ),
                }
            )
            .ToList();
    }

    private void OnKeyDown(object? sender, Key key)
    {
        if (sender is not IKeyboard { App.TopRunnableView: WizardMainView wizard })
        {
            return;
        }

        var vm = wizard.ViewModel;
        if (vm is null)
        {
            return;
        }

        if (key == Key.Enter)
        {
            vm.GoNext();
            key.Handled = true;
        }
        else if (key == Key.Esc)
        {
            vm.GoBack();
            key.Handled = true;
        }
    }
}
