# Config Wizard

## Goal

Sequential wizard-style TUI tool that walks users through questions, collects answers, and generates
YAML config files. Modern TUI aesthetic (bubbletea/charm-inspired) with intuitive keyboard controls
and no mouse dependency.

## Reference Repos

+ **Terminal.Gui source**: `~/code/terminal.gui` (use local exploration only, never gh-scout)
+ **ReactiveUI example**: `~/code/terminal.gui/Examples/ReactiveExample/` (Login form showing the
  full ViewModel/View/binding pattern with Terminal.Gui v2)
+ **Config wizard reference**: `~/code/recyclarr-workspace/config-wizard` (Spectre.Console-based
  wizard showing the question flow, options, and wizard behavior)
+ **TRaSH Guides data**: `~/code/recyclarr-workspace/guides/docs/json/` (groups.json, quality
  profiles, CF groups)

## Instructions / Constraints

+ **Keyboard model** (user-specified, non-negotiable):
  + `Enter` = advance to next step
  + `Esc` = go back to previous step
  + `Space` = select/toggle within widgets (radio buttons, checkboxes)
  + `Arrow keys` = navigate within a widget
  + `Tab/Shift+Tab` = move between widget groups on the page
  + `Ctrl+C` = quit
+ No mouse support concern; everything must be keyboard-accessible
+ **Modern TUI aesthetic**: Rounded corners (`LineStyle.Rounded`), Catppuccin-inspired color palette,
  accent-colored borders on focus, dim borders on inactive panels, colored question text, faint nav
  hints, generous padding
+ **API key field**: No validation on format/length, no password masking; plain text input, only
  required to be non-empty

## Architecture: ReactiveUI MVVM

The wizard follows the ReactiveUI MVVM pattern with Terminal.Gui v2. This is a deliberate
architectural choice based on industry consensus that views should receive pre-computed data, not
services. Services are injected into ViewModels; Views are passive presentation layers.

### Layer responsibilities

```txt
View (Terminal.Gui)
  Receives: observable properties from ViewModel via reactive bindings
  Does: display, capture user input, fire commands
  Does NOT: call services, run queries, hold business logic

ViewModel (ReactiveUI)
  Receives: services via constructor DI (Autofac)
  Does: call services, transform results into display-ready state,
        expose reactive properties and commands
  Does NOT: know about Terminal.Gui view internals

Services / Queries (Recyclarr.Core)
  Receives: resource registry, file system
  Does: data retrieval, deserialization
  Does NOT: know about Views or ViewModels
```

### NuGet packages required

Added to `Recyclarr.Cli.csproj`:

+ `ReactiveUI` - Core MVVM framework (ReactiveObject, ReactiveCommand, WhenAnyValue)
+ `ReactiveUI.SourceGenerators` - `[Reactive]`, `[ObservableAsProperty]`, `[ReactiveCommand]`
  source generators (PrivateAssets=all)
+ `ReactiveMarbles.ObservableEvents.SourceGenerator` - `.Events()` extension for converting
  Terminal.Gui events to observables (PrivateAssets=all)

### Key patterns (from Terminal.Gui ReactiveExample)

**ViewModel** inherits `ReactiveObject`:

```csharp
public partial class StepViewModel : ReactiveObject
{
    [Reactive] private string _name = "";           // Two-way bindable property
    [ObservableAsProperty] private bool _isValid;   // Computed from observables
    [ReactiveCommand] public void Clear() { }       // Generates ClearCommand
}
```

**View** implements `IViewFor<TViewModel>`, receives ViewModel via constructor:

```csharp
public class StepView : View, IViewFor<StepViewModel>
{
    private readonly CompositeDisposable _disposable = [];

    public StepView(StepViewModel viewModel)
    {
        ViewModel = viewModel;

        // VM -> View binding
        ViewModel.WhenAnyValue(x => x.Name)
            .BindTo(textField, x => x.Text)
            .DisposeWith(_disposable);

        // View -> VM binding
        textField.Events().TextChanged
            .Select(_ => textField.Text)
            .DistinctUntilChanged()
            .BindTo(ViewModel, x => x.Name)
            .DisposeWith(_disposable);
    }

    public StepViewModel ViewModel { get; set; }
    object IViewFor.ViewModel { get => ViewModel; set => ViewModel = (StepViewModel)value; }

    protected override void Dispose(bool disposing)
    {
        _disposable.Dispose();
        base.Dispose(disposing);
    }
}
```

**TerminalScheduler** is required for ReactiveUI to post updates back to Terminal.Gui's main loop.
Copy from `~/code/terminal.gui/Examples/ReactiveExample/TerminalScheduler.cs` and adapt namespace.

**App initialization** in `WizardApplication.Run()`:

```csharp
using var app = Application.Create();
app.Init();
var rxApp = RxAppBuilder.CreateReactiveUIBuilder();
rxApp.WithMainThreadScheduler(new TerminalScheduler(app));
rxApp.WithTaskPoolScheduler(TaskPoolScheduler.Default);
```

### DI integration

ViewModels are DI-constructed by Autofac with services injected. Views receive their ViewModel via
constructor. `WizardApplication` (DI-constructed) creates views by passing DI-resolved ViewModels.
The `app.Run(IRunnable)` non-generic overload is used (not `Run<T>()`) so views can have constructor
parameters.

Step ordering uses `Autofac.Extras.Ordering` with `.OrderByRegistration()` (same pattern as config
filters, config post processors, and global setup tasks in the existing codebase). The
`OrderedRegistrationSource` is already registered in `CompositionRoot`.

### What this replaces

The current code uses a manual pattern: `WizardStepView` base class with `LoadFromState()` /
`ApplyToState()` / `IsValid()` methods, and a static `WizardServices` service locator bridging DI
to Terminal.Gui views. Step views directly call resource queries. The MVVM refactor eliminates:

+ `WizardServices.cs` (static service locator) - replaced by DI into ViewModels
+ `WizardState.cs` manual state bag - replaced by reactive properties on ViewModels
+ `LoadFromState()` / `ApplyToState()` plumbing - replaced by reactive bindings
+ `StripEnterFromAll` / `UseSpaceNotEnter` workarounds - already removed; Enter/Esc handled at
  `app.Keyboard.KeyDown` level in `WizardApplication`

## Key Discoveries

+ **Terminal.Gui v2 key dispatch**: `app.Keyboard.KeyDown` fires before any view sees the key. This
  is the correct place to intercept wizard navigation keys (Enter, Esc). Previous approach of
  stripping Enter bindings from all descendant views was fragile (broke when widgets rebuilt their
  children dynamically, e.g. `FlagSelector.CreateSubViews()`).
+ **Terminal.Gui built-in Wizard class exists** (`Terminal.Gui/Views/Wizard/Wizard.cs`) but is too
  constrained (fixed help panel, no progress sidebar, centered dialog layout). Custom sequential
  flow was built instead.
+ **`Key.Empty` crashes** when set as `QuitKey`; throws `ArgumentException`. Solution: remap
  `QuitKey` to `Key.C.WithCtrl`.
+ **TRaSH Guides categories** come from `groups.json` files: Standard, Anime, French, German (and
  SQP for Radarr, excluded from wizard). These map to the `GuideCategory` enum. Note: TRaSH refers
  to "Standard" as "Main" in their docs; the data says "Standard".
+ **Color palette** uses Catppuccin-inspired values: Accent `#5FAFAF`, Secondary `#5F87AF`,
  Foreground `#CDD6F4`, Muted `#7F849C`, Faint `#585B70`, BorderDim `#45475A`, Success `#A6E3A1`,
  Error `#F38BA8`.
+ **Quality profile groups** (`groups.json`) are JSON arrays, not single objects per file. The
  `QualityProfileGroupResourceQuery` deserializes directly (not via `JsonResourceLoader`) and
  flattens with `SelectMany`.
+ **FlagSelector** works for multi-select with dynamically assigned power-of-2 values. Labels and
  Values can be updated at any time (triggers `CreateSubViews()`). Max practical limit ~31 items
  (int bitmask), well above the largest category (8 profiles in French Radarr).
+ **`app.Run(IRunnable)` non-generic overload** allows pre-constructed views with constructor
  parameters. The generic `Run<T>()` requires `new()` constraint. Terminal.Gui disposes the view
  only in the generic overload; caller owns disposal with the non-generic one.
+ **`app.TopRunnableView`** provides access to the running view from `app.Keyboard.KeyDown` handler
  (set by `Begin()` before the event loop starts).

## Current State of Code

### What exists and works

The wizard currently runs end-to-end with manual state management. Steps 1-3 are functional,
CF group steps are implemented but use the old pattern. All code compiles with zero warnings and
all 485 tests pass.

### Files on disk

#### Wizard core (`src/Recyclarr.Cli/Console/Wizard/`)

+ `WizardApplication.cs` - Entry point. Sets up Terminal.Gui, keyboard handlers, `app.Run()`.
+ `WizardMainView.cs` - Runnable root view. Layout (progress bar, content panel, nav hints).
  Navigation via `GoNext()`/`GoBack()` with `ShouldSkip()` support.
+ `WizardProgressBar.cs` - Read-only sidebar showing section progress.
+ `WizardSchemes.cs` - Catppuccin-inspired color palette and scheme registration.
+ `WizardState.cs` - Manual state bag (enums, InstanceState, SelectedProfile, SelectedCfGroup).
+ `WizardServices.cs` - Static service locator (TO BE REMOVED in MVVM refactor).
+ `WizardAutofacModule.cs` - DI registration (WizardApplication, WizardState).
+ `WizardStatusBar.cs` - Unused, can delete.

#### Step views (`src/Recyclarr.Cli/Console/Wizard/Steps/`)

+ `WizardStepView.cs` - Abstract base class with `SectionName`, `IsValid()`, `ApplyToState()`,
  `LoadFromState()`, `ShouldSkip()`, and helper methods for creating labels/errors.
+ `ServiceTypeStep.cs` - Radarr/Sonarr OptionSelector.
+ `ConnectionStep.cs` - Instance name, base URL, API key, content category (GuideCategory).
+ `QualityProfileStep.cs` - Multi-select FlagSelector from guide profile groups. Calls
  `WizardServices.ProfileGroupQuery` and `WizardServices.ProfileQuery` directly.
+ `CfGroupStep.cs` - Parameterized with `CfGroupMode` (SkipDefaults/AddOptional). FlagSelector
  filtered by selected profiles' trash_ids. Calls `WizardServices.CfGroupQuery` directly.
+ `PlaceholderStep.cs` - Generic placeholder with section name and description text.

#### Resource providers (`src/Recyclarr.Core/ResourceProviders/Domain/`)

+ `QualityProfileGroupResource.cs` - Domain record for groups.json (name + profiles dictionary).
+ `QualityProfileGroupResourceQuery.cs` - Loads quality profile group data.
+ `CfGroupResource.cs` - Domain record for CF group JSON (name, trash_id, default, custom_formats,
  quality_profiles.include).
+ `CfGroupResourceQuery.cs` - Loads CF group data.
+ `QualityProfileResource.cs` - Domain record for individual quality profile JSON.
+ `QualityProfileResourceQuery.cs` - Loads individual quality profiles.

#### Command entry point

+ `src/Recyclarr.Cli/Console/Commands/ConfigWizardCommand.cs` - CLI command. Initializes resource
  providers before launching Terminal.Gui.

## Design Decisions

### v1 philosophy

Keep it simple: add the components you want, guide defaults for everything. Customization branches
(quality items, profile settings, cutoff) are deferred to v2+ based on user feedback. The YAML is
still the first-class citizen for customizations.

### Quality profile flow (v1)

Single multi-select screen per instance. User picks one or more profiles from the category they
selected in step 2. No per-profile customization (name, quality items, settings, cutoff). Guide
defaults are used for everything.

### Custom format groups flow

Two screens, both optional (auto-skipped if the filtered category is empty):

1. **Skip default groups** - Shows default groups (where `default: "true"` and the group's
   `quality_profiles.include` contains at least one selected profile's trash_id). Nothing selected
   by default. User selects groups to opt out of. Selected groups go to
   `custom_format_groups.skip`.
2. **Add optional groups** - Shows non-default groups that match the selected profiles (same
   `quality_profiles.include` filtering). Nothing selected by default. User selects groups to opt
   in to. Selected groups go to `custom_format_groups.add`.

Groups not linked to any selected profile are excluded entirely (no "additional/incompatible" flow).

Per-CF customization within groups (required/default/optional CFs) is deferred; the current design
operates at the group level only. This is a planned follow-up after the group selection flow is
working.

### Screen structure (v1)

1. **Service Type** (done) - Radarr or Sonarr
2. **Instance Details** (done) - Name, base URL, API key, category
3. **Quality Profile Selection** (done) - Multi-select from guide profiles filtered by category
4. **Skip Default CF Groups** (done) - Opt out of default groups (skipped if none exist)
5. **Add Optional CF Groups** (done) - Opt in to non-default groups (skipped if none exist)
6. **Quality Sizes** - Yes/No: use guide defaults?
7. **Media Naming** - Yes/No: use guide defaults?
8. **Review & Generate** - Summary of selections, write YAML

## Refactor Plan: ReactiveUI MVVM

### Phase 1: Infrastructure

+ Add NuGet packages (ReactiveUI, ReactiveUI.SourceGenerators,
  ReactiveMarbles.ObservableEvents.SourceGenerator)
+ Copy and adapt `TerminalScheduler.cs` from the ReactiveExample
+ Update `WizardApplication.Run()` to initialize ReactiveUI builder with `TerminalScheduler`
+ Switch from `app.Run<WizardMainView>()` to `app.Run(wizard)` with pre-constructed view

### Phase 2: ViewModel layer

Create ViewModels for each step. Each ViewModel:

+ Inherits `ReactiveObject`
+ Gets services injected via Autofac constructor DI
+ Exposes `[Reactive]` properties for user inputs
+ Exposes `[ObservableAsProperty]` for computed/display state
+ Exposes validation as observables (replaces `IsValid()`)

**WizardViewModel** (replaces WizardState + navigation logic in WizardMainView):

+ Owns the ordered list of step ViewModels
+ Manages current step index, navigation (GoNext/GoBack with skip logic)
+ Holds shared state (service type, category, selected profiles) as reactive properties
+ Step ViewModels observe shared state to react to changes (e.g., category change refreshes
  profile list)

**Per-step ViewModels** (examples):

+ `ServiceTypeViewModel` - exposes `SelectedServiceType` reactive property
+ `ConnectionViewModel` - exposes `Name`, `BaseUrl`, `ApiKey`, `Category` reactive properties,
  plus validation observables
+ `QualityProfileViewModel` - takes `QualityProfileGroupResourceQuery` +
  `QualityProfileResourceQuery` via DI. Exposes `AvailableProfiles` (computed from service type +
  category), `SelectedProfiles`. Recomputes when upstream state changes.
+ `CfGroupViewModel` - takes `CfGroupResourceQuery` via DI. Exposes `AvailableGroups` (filtered
  by selected profiles + default/optional mode), `SelectedGroups`. Two instances: one for skip
  defaults, one for add optional. Exposes `ShouldSkip` observable.

### Phase 3: View layer refactor

Refactor each step view to:

+ Implement `IViewFor<TViewModel>`
+ Receive ViewModel via constructor
+ Set up reactive bindings in constructor (ViewModel -> View and View -> ViewModel)
+ Use `CompositeDisposable` for binding cleanup
+ Remove all direct service/query calls
+ Remove `LoadFromState()` / `ApplyToState()` / `IsValid()` overrides

`WizardMainView` becomes a pure layout container:

+ Receives `WizardViewModel` via constructor
+ Binds to `WizardViewModel.CurrentStep` to swap content panel
+ Binds to `WizardViewModel.ProgressState` to update sidebar
+ No navigation logic of its own (delegates to WizardViewModel)

### Phase 4: Cleanup

+ Delete `WizardServices.cs` (static service locator)
+ Delete `WizardState.cs` (replaced by reactive properties on ViewModels)
+ Delete `WizardStepView.cs` base class (replaced by `IViewFor<T>` pattern)
+ Update `WizardAutofacModule.cs` with ViewModel registrations
+ Update `ConfigWizardCommand.cs` if initialization flow changes

## Remaining TODO

+ ReactiveUI MVVM refactor (phases 1-4 above)
+ Per-CF customization within groups (follow-up after group selection works)
+ Implement Quality Sizes / Definitions step
+ Implement Media Naming step
+ Implement Review & Generate final step
+ Implement "add another instance" loop
