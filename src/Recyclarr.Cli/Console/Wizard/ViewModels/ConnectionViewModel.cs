using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace Recyclarr.Cli.Console.Wizard.ViewModels;

internal partial class ConnectionViewModel : WizardStepViewModel
{
    private readonly WizardViewModel _wizard;
    private readonly SerialDisposable _syncSubscription = new();

    // CurrentThreadScheduler ensures CheckValidation() propagates synchronously,
    // so ForceValidation() + GoNextCommand.Execute() work in sequence.
    public ReactiveProperty<string> Name { get; } =
        new("", CurrentThreadScheduler.Instance, false, false);
    public ReactiveProperty<string> BaseUrl { get; } =
        new("", CurrentThreadScheduler.Instance, false, false);
    public ReactiveProperty<string> ApiKey { get; } =
        new("", CurrentThreadScheduler.Instance, false, false);

    [Reactive]
    private GuideCategory _category = GuideCategory.Standard;

    public override string SectionName => "Instance";
    public override IObservable<bool> IsValid { get; }

    public ConnectionViewModel(WizardViewModel wizard)
    {
        _wizard = wizard;

        Name.AddValidationError(
            name => string.IsNullOrWhiteSpace(name) ? "Instance name is required." : null,
            ignoreInitialError: true
        );

        BaseUrl.AddValidationError(
            url =>
            {
                if (
                    string.IsNullOrWhiteSpace(url)
                    || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    || (uri.Scheme != "http" && uri.Scheme != "https")
                )
                {
                    return "Must be a valid HTTP or HTTPS URL.";
                }

                return null;
            },
            ignoreInitialError: true
        );

        ApiKey.AddValidationError(
            key => string.IsNullOrEmpty(key) ? "API key is required." : null,
            ignoreInitialError: true
        );

        IsValid = Observable
            .CombineLatest(Name.ObserveHasErrors, BaseUrl.ObserveHasErrors, ApiKey.ObserveHasErrors)
            .Select(errors => errors.All(hasError => !hasError));

        // Sync local state back to wizard on every change
        _syncSubscription.Disposable = new CompositeDisposable(
            Name.Skip(1).Subscribe(v => wizard.InstanceName = v ?? ""),
            BaseUrl.Skip(1).Subscribe(v => wizard.BaseUrl = v ?? ""),
            ApiKey.Skip(1).Subscribe(v => wizard.ApiKey = v ?? ""),
            this.WhenAnyValue(x => x.Category).Skip(1).Subscribe(v => wizard.Category = v)
        );

        Disposables.Add(_syncSubscription);
        Name.DisposeWith(Disposables);
        BaseUrl.DisposeWith(Disposables);
        ApiKey.DisposeWith(Disposables);
    }

    public override void Activate()
    {
        Name.Value = _wizard.InstanceName;
        BaseUrl.Value = _wizard.BaseUrl;
        ApiKey.Value = _wizard.ApiKey;
        Category = _wizard.Category;
    }

    public override void ForceValidation()
    {
        Name.CheckValidation();
        BaseUrl.CheckValidation();
        ApiKey.CheckValidation();
    }
}
