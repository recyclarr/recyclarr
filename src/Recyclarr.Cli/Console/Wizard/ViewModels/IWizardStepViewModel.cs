namespace Recyclarr.Cli.Console.Wizard.ViewModels;

internal interface IWizardStepViewModel
{
    string SectionName { get; }
    IObservable<bool> IsValid { get; }
    bool ShouldSkip() => false;

    void Activate();
    void ForceValidation();
}
