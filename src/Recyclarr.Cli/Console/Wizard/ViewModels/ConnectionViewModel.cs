using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Recyclarr.Cli.Console.Wizard.ViewModels;

internal partial class ConnectionViewModel : WizardStepViewModel
{
    private readonly WizardViewModel _wizard;
    private readonly SerialDisposable _syncSubscription = new();

    [Reactive]
    private string _name = "";

    [Reactive]
    private string _baseUrl = "";

    [Reactive]
    private string _apiKey = "";

    [Reactive]
    private GuideCategory _category = GuideCategory.Standard;

    // Validation error messages; empty string means valid
    private readonly ObservableAsPropertyHelper<string> _nameError;
    private readonly ObservableAsPropertyHelper<string> _urlError;
    private readonly ObservableAsPropertyHelper<string> _apiKeyError;

    public string NameError => _nameError.Value;
    public string UrlError => _urlError.Value;
    public string ApiKeyError => _apiKeyError.Value;

    public override string SectionName => "Instance";
    public override IObservable<bool> IsValid { get; }

    public ConnectionViewModel(WizardViewModel wizard)
    {
        _wizard = wizard;

        var nameValidation = this.WhenAnyValue(x => x.Name)
            .Select(name => string.IsNullOrWhiteSpace(name) ? "Instance name is required." : "");

        var urlValidation = this.WhenAnyValue(x => x.BaseUrl)
            .Select(url =>
            {
                if (
                    string.IsNullOrWhiteSpace(url)
                    || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    || (uri.Scheme != "http" && uri.Scheme != "https")
                )
                {
                    return "Must be a valid HTTP or HTTPS URL.";
                }

                return "";
            });

        var apiKeyValidation = this.WhenAnyValue(x => x.ApiKey)
            .Select(key => string.IsNullOrEmpty(key) ? "API key is required." : "");

        _nameError = nameValidation.ToProperty(this, x => x.NameError);
        _urlError = urlValidation.ToProperty(this, x => x.UrlError);
        _apiKeyError = apiKeyValidation.ToProperty(this, x => x.ApiKeyError);

        IsValid = Observable
            .CombineLatest(nameValidation, urlValidation, apiKeyValidation)
            .Select(errors => errors.All(string.IsNullOrEmpty));

        // Sync local state back to wizard on every change
        _syncSubscription.Disposable = new CompositeDisposable(
            this.WhenAnyValue(x => x.Name).Skip(1).Subscribe(v => wizard.InstanceName = v),
            this.WhenAnyValue(x => x.BaseUrl).Skip(1).Subscribe(v => wizard.BaseUrl = v),
            this.WhenAnyValue(x => x.ApiKey).Skip(1).Subscribe(v => wizard.ApiKey = v),
            this.WhenAnyValue(x => x.Category).Skip(1).Subscribe(v => wizard.Category = v)
        );

        Disposables.Add(_syncSubscription);
        Disposables.Add(_nameError);
        Disposables.Add(_urlError);
        Disposables.Add(_apiKeyError);
    }

    public void Activate()
    {
        Name = _wizard.InstanceName;
        BaseUrl = _wizard.BaseUrl;
        ApiKey = _wizard.ApiKey;
        Category = _wizard.Category;
    }
}
