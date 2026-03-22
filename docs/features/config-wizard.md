# Config Wizard

## Goal

Sequential wizard-style TUI tool that walks users through questions, collects answers, and generates
YAML config files. Modern TUI aesthetic (bubbletea/charm-inspired) with intuitive keyboard controls
and no mouse dependency.

## Reference Repos

+ **Terminal.Gui source**: `~/code/forks/terminal.gui` (use local exploration only, never gh-scout)
+ **ReactiveUI example**: `~/code/forks/terminal.gui/Examples/ReactiveExample/` (Login form showing
  the full ViewModel/View/binding pattern with Terminal.Gui v2)
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
+ **Modern TUI aesthetic**: Rounded corners (`LineStyle.Rounded`), Catppuccin-inspired color
  palette, accent-colored borders on focus, dim borders on inactive panels, colored question text,
  faint nav hints, generous padding
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

+ `ReactiveUI` - Core MVVM framework (ReactiveObject, ReactiveProperty, WhenAnyValue)
+ `ReactiveUI.SourceGenerators` - `[Reactive]`, `[ObservableAsProperty]` source generators
  (PrivateAssets=all)
+ `ReactiveMarbles.ObservableEvents.SourceGenerator` - `.Events()` extension for converting
  Terminal.Gui events to observables (PrivateAssets=all)

### Key patterns

**ViewModel** inherits `ReactiveObject`. Uses `ReactiveProperty<T>` for two-way bindable properties
with built-in validation, and `[Reactive]` for simpler properties that don't need validation:

```csharp
public partial class ConnectionViewModel : WizardStepViewModel
{
    // ReactiveProperty for two-way binding with validation support.
    // CurrentThreadScheduler ensures ForceValidation() propagates synchronously.
    public ReactiveProperty<string> Name { get; } =
        new("", CurrentThreadScheduler.Instance, false, false);

    [Reactive] private GuideCategory _category = GuideCategory.Standard;

    public ConnectionViewModel()
    {
        Name.AddValidationError(
            name => string.IsNullOrWhiteSpace(name) ? "Required." : null,
            ignoreInitialError: true
        );
    }
}
```

**View** implements `IViewFor<TViewModel>`, receives ViewModel via constructor. Uses
`ReactiveProperty<T>.BindTwoWay()` extension for TextField bindings:

```csharp
public class ConnectionStepView : WizardStepViewBase<ConnectionViewModel>
{
    public ConnectionStepView(ConnectionViewModel viewModel)
    {
        ViewModel = viewModel;

        var nameField = new WizardTextField { Y = Pos.Bottom(nameLabel), Width = 40 };

        // Two-way binding via ReactivePropertyBindingExtensions
        viewModel.Name.BindTwoWay(nameField).DisposeWith(Disposables);

        // Validation error display
        viewModel.Name.ObserveValidationErrors()
            .Subscribe(err => ToggleError(nameError, err))
            .DisposeWith(Disposables);
    }
}
```

**`ReactivePropertyBindingExtensions`** bridges ReactiveProperty and Terminal.Gui widgets.
ReactiveUI's built-in `Bind()` requires `INotifyPropertyChanged` on both sides, which Terminal.Gui
controls don't implement. The `BindTwoWay` extensions wire VM-to-View and View-to-VM directions via
observables:

+ Generic overload: `BindTwoWay(Action<T?> writeView, IObservable<T> viewChanged)` for any widget
+ TextField overload: `BindTwoWay(TextField field)` using `field.Events().TextChanged`

**TerminalScheduler** is required for ReactiveUI to post updates back to Terminal.Gui's main loop.
Adapted from `~/code/forks/terminal.gui/Examples/ReactiveExample/TerminalScheduler.cs`.

**App initialization** in `WizardApplication.Run()`:

```csharp
using var app = Application.Create();
app.Init();
RxAppBuilder.CreateReactiveUIBuilder()
    .WithMainThreadScheduler(new TerminalScheduler(app))
    .WithTaskPoolScheduler(TaskPoolScheduler.Default)
    .WithCoreServices()
    .BuildApp();
```

### Navigation

Navigation is imperative (not `ReactiveCommand`) because validation via `ForceValidation()` +
synchronous `IsValid` read must complete within a single call stack, without scheduler deferral.
`ReactiveCommand`'s output scheduler (`TerminalScheduler`) defers execution via
`Application.Invoke`, which breaks synchronous validation reads.

Enter and Esc are intercepted at the `app.Keyboard.KeyDown` level in `WizardApplication`, before any
view sees the key. `GoNext()` calls `ForceValidation()` to reveal suppressed validation errors
(fields using `ignoreInitialError` that the user never touched), then reads `IsValid` synchronously.

After validation passes, `GoNext()` checks `GetAdvanceConfirmation()` on the current step. If
non-null, a confirmation dialog is shown via the `ShowConfirmation` callback before advancing. Steps
that return `ShouldSkip() == true` are silently bypassed during both forward and backward navigation.

### Confirmation dialogs

Steps can override `GetAdvanceConfirmation()` to return a `(Title, Message)` tuple. `GoNext()` shows
it via a `Func<string, string, bool>` callback that `WizardApplication` wires to
`MessageBox.Query()`.

ReactiveUI's idiomatic pattern for VM-to-View communication is `Interaction<TInput, TOutput>`, which
decouples the ViewModel from the UI framework and makes handlers testable by registering mock
handlers. However, `Interaction.Handle()` returns `IObservable<TOutput>` (async by design), and our
navigation is deliberately synchronous (see above). Terminal.Gui's `MessageBox.Query()` is also
synchronous (it runs a nested event loop). Bridging async Interaction into a sync call would fight
both patterns. The `Func` callback is a pragmatic choice given this constraint; if navigation ever
becomes async, `Interaction<,>` would be the right migration target.

### DI integration

ViewModels are DI-constructed by Autofac with services injected. Views receive their ViewModel via
constructor. `WizardApplication` (DI-constructed) creates views by passing DI-resolved ViewModels.
The `app.Run(IRunnable)` non-generic overload is used (not `Run<T>()`) so views can have constructor
parameters.

Step ordering relies on Autofac's registration order for `IEnumerable<IWizardStepViewModel>`.
ViewModels are registered in wizard flow order in `WizardAutofacModule`.

## Key Discoveries

+ **Terminal.Gui v2 key dispatch order** (`ApplicationKeyboard.RaiseKeyDownEvent`): (1)
  `app.Keyboard.KeyDown` event, (2) active popovers, (3) view hierarchy, (4) app-level commands
  (including QuitKey). Views fire BEFORE app-level commands; any view-level binding that handles a
  key prevents the app-level command from executing. `app.Keyboard.KeyDown` (step 1) is the correct
  place to intercept wizard navigation keys (Enter, Esc) because it fires before any view.
+ **Terminal.Gui v2 Tab vs F6 navigation**: `Tab`/`Shift+Tab` cycles between views with
  `TabStop = TabBehavior.TabStop`. `F6`/`Shift+F6` cycles between views with
  `TabStop = TabBehavior.TabGroup`. FrameView defaults to `TabGroup`, so Tab will skip FrameViews
  entirely. For Tab to cycle between FrameViews (matching the wizard keyboard model), set
  `TabStop = TabBehavior.TabStop` on each FrameView explicitly.
+ **SelectorBase.TabBehavior property** controls the `TabStop` assigned to child checkboxes when
  `CreateSubViews()` runs. Setting `FlagSelector.TabBehavior = TabBehavior.NoStop` means checkboxes
  only respond to arrow keys (not Tab). This also gates the arrow-key navigation logic in
  `SelectorBase.MoveNext`/`MovePrevious`, which check `Focused?.TabStop != NoStop` and bail out if
  the children aren't NoStop. Always use this property instead of manually iterating children to set
  `TabStop`; `CreateSubViews()` reruns whenever Labels or Values change and would overwrite manual
  settings.
+ **Ctrl+C / QuitKey conflict with TextField**: Terminal.Gui's `TextField` binds `Ctrl+C` to
  `Command.Copy` at the view level (dispatch step 3). `TextField.OnHasFocusChanged` calls
  `SelectAll()` when gaining keyboard focus with non-empty text. Once text is selected, `Copy()`
  returns true, consuming Ctrl+C before the app-level QuitKey (dispatch step 4) fires. Solution:
  `WizardTextField` subclass removes the `Key.C.WithCtrl` binding, letting Ctrl+C fall through to
  QuitKey. Use `WizardTextField` instead of `TextField` for all wizard text inputs.
+ **`Key.Empty` crashes** when set as `QuitKey`; throws `ArgumentException`. Solution: remap
  `QuitKey` to `Key.C.WithCtrl`.
+ **Terminal.Gui built-in Wizard class exists** (`Terminal.Gui/Views/Wizard/Wizard.cs`) but is too
  constrained (fixed help panel, no progress sidebar, centered dialog layout). Custom sequential
  flow was built instead.
+ **Default interface methods (DIM) don't dispatch through derived classes the way virtual methods
  do.** If a base class implements an interface without providing a method, the DIM is used and
  locked into the interface vtable. A derived class adding a method with the same signature does NOT
  override the DIM unless the derived class re-declares the interface. All step lifecycle methods
  (`ShouldSkip`, `Activate`, `ForceValidation`, `GetAdvanceConfirmation`) must be virtual methods on
  `WizardStepViewModel`, not DIMs on `IWizardStepViewModel`. The interface declares the contract;
  the base class provides the defaults.
+ **Stale subscriptions on re-activation**: When `Activate()` is called again (user navigates back),
  `PopulateSelector()` resets `SelectedFlagValue = null`. If the old sync subscription is still
  alive, it fires and wipes wizard state before `RestoreSelections()` can read it. Fix: dispose the
  old `SerialDisposable` subscription (set `.Disposable = null`) before calling populate/restore,
  then wire the new subscription after.
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
+ **`MessageBox.Query()` is synchronous.** It calls `app.Run(dialog)` internally, which runs a
  nested event loop and blocks until the dialog closes. Safe to call from key handlers.

## Current State of Code

### What exists and works

The wizard runs end-to-end with ReactiveUI MVVM. Steps 1-4 are functional (service type, connection,
quality profiles, CF group skip/add). Steps 5-7 are placeholder views. Quality profiles are optional
(confirmation dialog when skipping); the CF group step auto-skips when no profiles are selected.
Back-navigation restores prior selections. All code compiles with zero warnings.

### Files on disk

#### Wizard core (`src/Recyclarr.Cli/Console/Wizard/`)

+ `WizardApplication.cs` - Entry point. Initializes ReactiveUI, sets up Terminal.Gui, keyboard
  handlers (Enter/Esc at `app.Keyboard.KeyDown` level), QuitKey remapping, confirmation dialog
  wiring, `app.Run()`. Wraps `app.Run()` in try/catch to surface crash stack traces after
  Terminal.Gui cleanup.
+ `WizardMainView.cs` - Runnable root view implementing `IViewFor<WizardViewModel>`. Layout
  (progress bar, content panel, nav hints). Reacts to `CurrentStepIndex` changes to swap content.
+ `WizardProgressBar.cs` - Read-only sidebar showing section progress.
+ `WizardSchemes.cs` - Catppuccin-inspired color palette and scheme registration.
+ `WizardTextField.cs` - TextField subclass that removes the `Ctrl+C` Copy binding so QuitKey works.
+ `WizardTypes.cs` - Shared types (`WizardSelection`, `FlagSelectorHelper`).
+ `WizardAutofacModule.cs` - DI registration (WizardApplication, WizardViewModel, step ViewModels in
  flow order).
+ `TerminalScheduler.cs` - ReactiveUI scheduler bridging to Terminal.Gui's main loop.
+ `ReactivePropertyBindingExtensions.cs` - Two-way binding helpers for `ReactiveProperty<T>` with
  Terminal.Gui widgets.

#### ViewModels (`src/Recyclarr.Cli/Console/Wizard/ViewModels/`)

+ `IWizardStepViewModel.cs` - Interface: `SectionName`, `IsValid`, `ShouldSkip()`, `Activate()`,
  `ForceValidation()`, `GetAdvanceConfirmation()`.
+ `WizardStepViewModel.cs` - Abstract base class: `ReactiveObject` + `IWizardStepViewModel` +
  `CompositeDisposable` for cleanup. Provides virtual defaults for all lifecycle methods (not DIMs
  on the interface; see Key Discoveries).
+ `WizardViewModel.cs` - Owns step list, current index, navigation (`GoNext`/`GoBack` with skip
  logic, validation gating, and advance confirmation), `ShowConfirmation` callback, shared wizard
  state (service type, category, selected profiles, instance connection details, CF group
  selections).
+ `ServiceTypeViewModel.cs` - Exposes `SelectedServiceType` reactive property.
+ `ConnectionViewModel.cs` - Exposes `Name`, `BaseUrl`, `ApiKey` as `ReactiveProperty<string>` with
  validation, `Category` as `[Reactive]`. Syncs to `WizardViewModel` shared state.
+ `QualityProfileViewModel.cs` - Loads profile groups via DI-injected query. Exposes
  `SelectedFlagValue`, `Labels`, `Values` for FlagSelector binding. Selection is optional;
  overrides `GetAdvanceConfirmation()` to prompt when nothing is selected.
+ `CfGroupViewModel.cs` - Combined skip/add CF group step. Loads CF groups via DI-injected query.
  Filters by selected profiles. Exposes skip and add panel state (labels, values, selections).
  Overrides `ShouldSkip()` to auto-skip when no groups match.
+ `PlaceholderViewModel.cs` - Generic placeholder with section name and description.

#### Step views (`src/Recyclarr.Cli/Console/Wizard/Steps/`)

+ `WizardStepViewBase.cs` - Abstract generic base class: `View` + `IViewFor<TViewModel>`, layout
  defaults (margins, focus), helper methods for styled labels and error display.
+ `ServiceTypeStepView.cs` - Radarr/Sonarr OptionSelector.
+ `ConnectionStepView.cs` - WizardTextField inputs for name, URL, API key. OptionSelector for
  category. Two-way bindings via `BindTwoWay()`, validation error display.
+ `QualityProfileStepView.cs` - Multi-select FlagSelector from guide profile groups. Hint text
  indicates Enter-to-skip when nothing is selected.
+ `CfGroupStepView.cs` - Side-by-side FrameViews (TabStop, not TabGroup) with FlagSelectors
  (TabBehavior.NoStop) for skip/add CF groups. Focus highlight on active panel.
+ `PlaceholderStepView.cs` - Generic placeholder with section name and description text.

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

Single multi-select screen per instance. User picks profiles from the category they selected in
step 2. No per-profile customization (name, quality items, settings, cutoff). Guide defaults are
used for everything.

Selection is optional. If the user presses Enter with nothing selected, a confirmation dialog
appears explaining that CF groups will also be skipped. Confirming advances past both the QP and CF
steps.

### Custom format groups flow

Single screen with two side-by-side panels, auto-skipped if no groups match the selected profiles
(or if no profiles were selected):

+ **Left panel: Skip Default Groups** - Shows default groups (where `default: "true"` and the
  group's `quality_profiles.include` contains at least one selected profile's trash_id). Nothing
  selected by default. User selects groups to opt out of. Selected groups go to
  `custom_format_groups.skip`.
+ **Right panel: Add Optional Groups** - Shows non-default groups that match the selected profiles
  (same `quality_profiles.include` filtering). Nothing selected by default. User selects groups to
  opt in to. Selected groups go to `custom_format_groups.add`.

Each panel is a FrameView (`TabStop = TabBehavior.TabStop`) with a FlagSelector
(`TabBehavior = TabBehavior.NoStop`) inside. Arrow keys navigate within each panel; Tab/Shift+Tab
moves focus between panels. The active panel gets an accent border via `HasFocusChanged`. Vertical
scrollbars appear automatically when groups exceed the available height
(`ViewportSettingsFlags.HasVerticalScrollBar`).

Groups not linked to any selected profile are excluded entirely (no "additional/incompatible" flow).

Per-CF customization within groups (required/default/optional CFs) is deferred; the current design
operates at the group level only. This is a planned follow-up after the group selection flow is
working.

### Screen structure (v1)

1. **Service Type** (done) - Radarr or Sonarr
2. **Instance Details** (done) - Name, base URL, API key, category
3. **Quality Profile Selection** (done) - Optional multi-select from guide profiles
4. **CF Group Selection** (done) - Side-by-side: skip defaults (left), add optional (right)
5. **Quality Sizes** - Yes/No: use guide defaults?
6. **Media Naming** - Yes/No: use guide defaults?
7. **Review & Generate** - Summary of selections, write YAML

## Remaining TODO

+ Per-CF customization within groups (follow-up after group selection works)
+ Implement Quality Sizes / Definitions step
+ Implement Media Naming step
+ Implement Review & Generate final step
+ Implement "add another instance" loop
