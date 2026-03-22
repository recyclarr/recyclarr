namespace Recyclarr.Cli.Console.Wizard.ViewModels;

internal interface IWizardStepViewModel
{
    string SectionName { get; }
    IObservable<bool> IsValid { get; }

    bool ShouldSkip();
    void Activate();
    void ForceValidation();

    // Return non-null to prompt user for confirmation before advancing.
    // Title and message are displayed in a modal dialog; the step only
    // advances if the user confirms.
    (string Title, string Message)? GetAdvanceConfirmation();
}
