namespace Recyclarr.Cli.Console.Wizard.ViewModels;

internal sealed class PlaceholderViewModel(string sectionName, string description)
    : WizardStepViewModel
{
    public override string SectionName => sectionName;
    public string Description => description;
}
