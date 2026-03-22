using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Recyclarr.TrashGuide;

namespace Recyclarr.Cli.Console.Wizard.ViewModels;

internal partial class MediaNamingViewModel : WizardStepViewModel
{
    private readonly WizardViewModel _wizard;
    private readonly SerialDisposable _syncSubscription = new();

    [Reactive]
    private bool? _useNaming;

    [Reactive]
    private MediaServer? _selectedServer;

    [Reactive]
    private NamingIdType? _selectedIdType;

    // Controls visibility of the server and ID type selectors in the view
    [Reactive]
    private bool _showServerSelector;

    [Reactive]
    private bool _showIdTypeSelector;

    // Available ID types for the current server + service combination
    [Reactive]
    private IReadOnlyList<NamingIdType> _availableIdTypes = [];

    // The recommended (pre-selected) ID type
    [Reactive]
    private NamingIdType? _recommendedIdType;

    public override string SectionName => "Media Naming";

    public override IObservable<bool> IsValid =>
        this.WhenAnyValue(x => x.UseNaming).Select(v => v is not null);

    public MediaNamingViewModel(WizardViewModel wizard)
    {
        _wizard = wizard;
        Disposables.Add(_syncSubscription);
    }

    public override void Activate()
    {
        // Dispose old subscriptions before restoring state
        _syncSubscription.Disposable = null;

        UseNaming = _wizard.UseMediaNaming;
        SelectedServer = _wizard.MediaServer;
        SelectedIdType = _wizard.NamingIdType;

        UpdateDerivedState();

        var disposables = new CompositeDisposable();

        // Sync Yes/No to wizard
        this.WhenAnyValue(x => x.UseNaming)
            .Where(v => v.HasValue)
            .Subscribe(v =>
            {
                _wizard.UseMediaNaming = v!.Value;
                ShowServerSelector = v.Value;
                if (!v.Value)
                {
                    ShowIdTypeSelector = false;
                }
            })
            .DisposeWith(disposables);

        // Sync server selection to wizard and update ID type options
        this.WhenAnyValue(x => x.SelectedServer)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .DistinctUntilChanged()
            .Subscribe(server =>
            {
                _wizard.MediaServer = server;
                UpdateIdTypeOptions(server);
            })
            .DisposeWith(disposables);

        // Sync ID type to wizard
        this.WhenAnyValue(x => x.SelectedIdType)
            .DistinctUntilChanged()
            .Subscribe(id => _wizard.NamingIdType = id)
            .DisposeWith(disposables);

        _syncSubscription.Disposable = disposables;
    }

    private void UpdateDerivedState()
    {
        ShowServerSelector = UseNaming is true;
        if (SelectedServer is { } server && server != MediaServer.None)
        {
            UpdateIdTypeOptions(server);
        }
        else
        {
            ShowIdTypeSelector = false;
        }
    }

    private void UpdateIdTypeOptions(MediaServer server)
    {
        if (server == MediaServer.None)
        {
            ShowIdTypeSelector = false;
            SelectedIdType = null;
            return;
        }

        var (types, recommended) = GetIdTypes(_wizard.ServiceType, server);
        AvailableIdTypes = types;
        RecommendedIdType = recommended;

        if (types.Count <= 1)
        {
            // Only one option (or none); auto-select and hide
            ShowIdTypeSelector = false;
            SelectedIdType = types.Count == 1 ? types[0] : null;
        }
        else
        {
            ShowIdTypeSelector = true;
            // Pre-select the recommended option if no prior selection
            // or if the prior selection isn't in the new list
            if (SelectedIdType is null || !types.Contains(SelectedIdType.Value))
            {
                SelectedIdType = recommended;
            }
        }
    }

    // Returns available ID types and the recommended default for a given
    // service + media server combination, based on TRaSH Guides naming keys.
    private static (IReadOnlyList<NamingIdType> Types, NamingIdType Recommended) GetIdTypes(
        SupportedServices service,
        MediaServer server
    )
    {
        return (service, server) switch
        {
            // Radarr: all servers have IMDb + TMDb; IMDb recommended
            (SupportedServices.Radarr, MediaServer.Plex) => (
                [NamingIdType.Imdb, NamingIdType.Tmdb],
                NamingIdType.Imdb
            ),
            (SupportedServices.Radarr, MediaServer.Emby) => (
                [NamingIdType.Imdb, NamingIdType.Tmdb],
                NamingIdType.Imdb
            ),
            (SupportedServices.Radarr, MediaServer.Jellyfin) => (
                [NamingIdType.Imdb, NamingIdType.Tmdb],
                NamingIdType.Imdb
            ),

            // Sonarr Plex/Emby: IMDb + TVDb; TVDb recommended
            (SupportedServices.Sonarr, MediaServer.Plex) => (
                [NamingIdType.Imdb, NamingIdType.Tvdb],
                NamingIdType.Tvdb
            ),
            (SupportedServices.Sonarr, MediaServer.Emby) => (
                [NamingIdType.Imdb, NamingIdType.Tvdb],
                NamingIdType.Tvdb
            ),

            // Sonarr Jellyfin: TVDb only
            (SupportedServices.Sonarr, MediaServer.Jellyfin) => (
                [NamingIdType.Tvdb],
                NamingIdType.Tvdb
            ),

            _ => ([], NamingIdType.Imdb),
        };
    }

    // Resolves the naming preset keys for YAML generation based on wizard selections.
    // Returns null values for fields that should use defaults or be omitted.
    public static NamingPresetKeys ResolvePresetKeys(
        SupportedServices service,
        bool useNaming,
        MediaServer server,
        NamingIdType? idType,
        GuideCategory category
    )
    {
        if (!useNaming)
        {
            return NamingPresetKeys.Empty;
        }

        if (server == MediaServer.None)
        {
            return service switch
            {
                SupportedServices.Radarr => new NamingPresetKeys("default", "standard"),
                SupportedServices.Sonarr => new NamingPresetKeys("default", "default"),
                _ => NamingPresetKeys.Empty,
            };
        }

        var serverPrefix = server.ToString().ToLowerInvariant();
        var idSuffix = idType switch
        {
            NamingIdType.Imdb => "imdb",
            NamingIdType.Tvdb => "tvdb",
            NamingIdType.Tmdb => "tmdb",
            _ => "imdb",
        };

        var folderKey = $"{serverPrefix}-{idSuffix}";

        // File key includes anime suffix for Radarr anime category
        var fileKey = service switch
        {
            SupportedServices.Radarr when category == GuideCategory.Anime =>
                $"{serverPrefix}-anime-{idSuffix}",
            SupportedServices.Radarr => $"{serverPrefix}-{idSuffix}",
            // Sonarr episode formats have no server-specific keys
            SupportedServices.Sonarr => "default",
            _ => "default",
        };

        // Sonarr series folder uses server-specific key; episode formats are always "default"
        return service switch
        {
            SupportedServices.Radarr => new NamingPresetKeys(folderKey, fileKey),
            SupportedServices.Sonarr => new NamingPresetKeys(folderKey, "default"),
            _ => NamingPresetKeys.Empty,
        };
    }
}

internal record NamingPresetKeys(string? FolderKey, string? FileKey)
{
    public static readonly NamingPresetKeys Empty = new(null, null);
}
