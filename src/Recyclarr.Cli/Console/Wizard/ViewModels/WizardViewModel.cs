using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Console.Wizard.ViewModels;

[SuppressMessage(
    "Reliability",
    "CA2213",
    Justification = "Disposable fields are disposed via CompositeDisposable"
)]
internal partial class WizardViewModel : ReactiveObject, IDisposable
{
    private readonly ILogger _logger;
    private readonly CompositeDisposable _disposable = [];
    private readonly Subject<Unit> _finished = new();

    [Reactive]
    private int _currentStepIndex;

    [Reactive]
    private SupportedServices _serviceType = SupportedServices.Radarr;

    [Reactive]
    private GuideCategory _category = GuideCategory.Standard;

    [Reactive]
    private string _instanceName = "movies";

    [Reactive]
    private string _baseUrl = "http://localhost:7878";

    [Reactive]
    private string _apiKey = "";

    [Reactive]
    private IReadOnlyList<WizardSelection> _selectedProfiles = [];

    [Reactive]
    private bool _useQualitySizes = true;

    [Reactive]
    private bool _useMediaNaming = true;

    [Reactive]
    private MediaServer _mediaServer = MediaServer.None;

    [Reactive]
    private NamingIdType? _namingIdType;

    [Reactive]
    private IReadOnlyList<WizardSelection> _skippedCfGroups = [];

    [Reactive]
    private IReadOnlyList<WizardSelection> _addedCfGroups = [];

    // Not using [ObservableAsProperty] here because we need to assign
    // computed properties after Initialize() sets the step list.
    private readonly ObservableAsPropertyHelper<IWizardStepViewModel?> _currentStep;
    private readonly ObservableAsPropertyHelper<string> _currentSectionName;
    private readonly ObservableAsPropertyHelper<bool> _isFirstStep;
    private readonly ObservableAsPropertyHelper<bool> _isLastStep;

    public WizardViewModel(ILogger logger)
    {
        _logger = logger;
        // Wire up computed properties from CurrentStepIndex.
        // Safe because Steps defaults to [] and these only fire when Steps is populated.
        _currentStep = this.WhenAnyValue(x => x.CurrentStepIndex)
            .Select(i => Steps.Count > 0 ? Steps[i] : null)
            .ToProperty(this, x => x.CurrentStep)
            .DisposeWith(_disposable);

        _currentSectionName = this.WhenAnyValue(x => x.CurrentStep)
            .Select(s => s?.SectionName ?? "")
            .ToProperty(this, x => x.CurrentSectionName)
            .DisposeWith(_disposable);

        _isFirstStep = this.WhenAnyValue(x => x.CurrentStepIndex)
            .Select(i => i == 0)
            .ToProperty(this, x => x.IsFirstStep)
            .DisposeWith(_disposable);

        _isLastStep = this.WhenAnyValue(x => x.CurrentStepIndex)
            .Select(i => Steps.Count > 0 && i == Steps.Count - 1)
            .ToProperty(this, x => x.IsLastStep)
            .DisposeWith(_disposable);

        // Not using ReactiveCommand here because its output scheduler
        // (TerminalScheduler) defers execution via Application.Invoke,
        // which breaks synchronous ForceValidation + IsValid reads.

        _finished.DisposeWith(_disposable);
    }

    // Navigation is imperative (not ReactiveCommand) because validation
    // via ForceValidation + synchronous IsValid read must complete within
    // a single call stack, without scheduler deferral.

    // Signals when all steps are completed (no more steps to advance to)
    public IObservable<Unit> Finished => _finished.AsObservable();

    // Provided by the application layer to show modal confirmation dialogs.
    // Returns true if user confirms.
    public Func<string, string, bool>? ShowConfirmation { get; set; }

    public IWizardStepViewModel? CurrentStep => _currentStep.Value;
    public string CurrentSectionName => _currentSectionName.Value;
    public bool IsFirstStep => _isFirstStep.Value;
    public bool IsLastStep => _isLastStep.Value;

    // Steps are set after construction to break the circular dependency
    // (step VMs reference WizardViewModel, WizardViewModel references steps).
    public IReadOnlyList<IWizardStepViewModel> Steps { get; private set; } = [];
    public int StepCount => Steps.Count;

    public IReadOnlyList<string> SectionNames =>
        Steps.Select(s => s.SectionName).Distinct().ToList();

    // Completed sections: all sections from steps before the current one
    // (excluding the current section itself)
    public HashSet<string> CompletedSections
    {
        get
        {
            var completed = Steps.Take(CurrentStepIndex).Select(s => s.SectionName).ToHashSet();
            completed.Remove(CurrentSectionName);
            return completed;
        }
    }

    // Must be called once after construction with the ordered step list.
    // Triggers the initial step via re-assigning CurrentStepIndex.
    public void Initialize(IEnumerable<IWizardStepViewModel> steps)
    {
        Steps = steps.ToList();
        Steps[0].Activate();

        // Re-notify to trigger computed property updates now that Steps is populated
        this.RaisePropertyChanged(nameof(CurrentStepIndex));
    }

    public void GoNext()
    {
        var current = Steps[CurrentStepIndex];
        current.ForceValidation();

        // Read the latest validity synchronously (ForceValidation with
        // CurrentThreadScheduler ensures the value is already propagated).
        var isValid = false;
        using var sub = current.IsValid.Subscribe(v => isValid = v);

        if (!isValid)
        {
            _logger.Debug(
                "GoNext blocked: step {Index} ({Step}) failed validation",
                CurrentStepIndex,
                current.GetType().Name
            );
            return;
        }

        if (current.GetAdvanceConfirmation() is var (title, message))
        {
            if (ShowConfirmation?.Invoke(title, message) is not true)
            {
                return;
            }
        }

        for (var i = CurrentStepIndex + 1; i < Steps.Count; i++)
        {
            if (!Steps[i].ShouldSkip())
            {
                Steps[i].Activate();
                CurrentStepIndex = i;
                return;
            }
        }

        // All remaining steps were skipped; wizard is done
        _logger.Debug("Wizard finished (all steps complete)");
        _finished.OnNext(Unit.Default);
    }

    public void GoBack()
    {
        for (var i = CurrentStepIndex - 1; i >= 0; i--)
        {
            if (!Steps[i].ShouldSkip())
            {
                Steps[i].Activate();
                CurrentStepIndex = i;
                return;
            }
        }
    }

    public void Dispose()
    {
        _disposable.Dispose();
    }

    // Quality size type is deterministic from service type + category.
    // SQP is excluded from the wizard; all other categories map unambiguously.
    public static string QualitySizeType(SupportedServices serviceType, GuideCategory category)
    {
        return category switch
        {
            GuideCategory.Anime => "anime",
            _ => serviceType switch
            {
                SupportedServices.Radarr => "movie",
                SupportedServices.Sonarr => "series",
                _ => "movie",
            },
        };
    }

    public static string DefaultName(SupportedServices serviceType)
    {
        return serviceType switch
        {
            SupportedServices.Radarr => "movies",
            SupportedServices.Sonarr => "series",
            _ => "instance",
        };
    }

    public static string DefaultBaseUrl(SupportedServices serviceType)
    {
        return serviceType switch
        {
            SupportedServices.Radarr => "http://localhost:7878",
            SupportedServices.Sonarr => "http://localhost:8989",
            _ => "http://localhost",
        };
    }
}
