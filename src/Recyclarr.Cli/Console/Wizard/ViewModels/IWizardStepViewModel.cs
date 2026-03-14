namespace Recyclarr.Cli.Console.Wizard.ViewModels;

internal interface IWizardStepViewModel
{
    string SectionName { get; }
    IObservable<bool> IsValid { get; }
    bool ShouldSkip() => false;

    // Called when the step becomes the current step, for lazy data loading
    void Activate() { }
}
