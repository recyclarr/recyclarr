using System.Reactive.Concurrency;
using ReactiveUI.Builder;
using Recyclarr.Cli.Console.Wizard.Steps;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace Recyclarr.Cli.Console.Wizard;

// Uses Func<> delegates to defer ViewModel resolution until after
// ReactiveUI is initialized via RxAppBuilder (ReactiveObject subclasses
// cannot be constructed before RxApp initialization).
internal sealed class WizardApplication(
    Func<WizardViewModel> wizardViewModelFactory,
    Func<IEnumerable<IWizardStepViewModel>> stepsFactory
)
{
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

        WizardSchemes.Register();

        // Remap quit to Ctrl+C; Esc is used for back navigation.
        app.Keyboard.QuitKey = Key.C.WithCtrl;

        // Intercept navigation keys at the app level, before any widget
        // can consume them. This prevents Enter from toggling checkboxes
        // instead of advancing to the next step.
        app.Keyboard.KeyDown += OnKeyboardOnKeyDown;

        var stepViews = CreateStepViews(wizardViewModel);
        var wizard = new WizardMainView(wizardViewModel, stepViews);
        app.Run(wizard);
        wizard.Dispose();
    }

    // Create one View per step ViewModel, in matching order
    private static List<View> CreateStepViews(WizardViewModel vm)
    {
        return vm
            .Steps.Select<IWizardStepViewModel, View>(step =>
                step switch
                {
                    ServiceTypeViewModel s => new ServiceTypeStepView(s),
                    ConnectionViewModel c => new ConnectionStepView(c),
                    QualityProfileViewModel q => new QualityProfileStepView(q),
                    CfGroupViewModel g => new CfGroupStepView(g),
                    PlaceholderViewModel p => new PlaceholderStepView(p),
                    _ => throw new InvalidOperationException(
                        $"No view registered for step ViewModel type {step.GetType().Name}"
                    ),
                }
            )
            .ToList();
    }

    private static void OnKeyboardOnKeyDown(object? sender, Key key)
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
