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
+ **Modern TUI aesthetic**: Rounded corners, Catppuccin-inspired color palette, accent-colored
  borders on focus, dim borders on inactive panels, colored question text, faint nav hints, generous
  padding
+ **API key field**: No validation on format/length, no password masking; plain text input, only
  required to be non-empty

## Architecture

ReactiveUI MVVM with Terminal.Gui v2. Views receive pre-computed data, not services. Services are
injected into ViewModels; Views are passive presentation layers.

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

### Key architectural constraints

+ **Navigation is synchronous.** Validation via `ForceValidation()` + synchronous `IsValid` read
  must complete within a single call stack. `ReactiveCommand` defers via `TerminalScheduler`, which
  breaks this. Navigation uses imperative methods, not reactive commands.
+ **Confirmation dialogs use a `Func` callback, not `Interaction<,>`.** ReactiveUI's `Interaction`
  is async by design; our navigation is synchronous. The `Func` callback is pragmatic given this
  constraint; if navigation ever becomes async, `Interaction<,>` would be the migration target.
+ **Step lifecycle methods are virtual on the base class, not DIMs on the interface.** Default
  interface methods don't dispatch through derived classes the way virtual methods do (the DIM is
  locked into the interface vtable). The interface declares the contract; the base class provides
  overridable defaults.
+ **Step ordering relies on Autofac registration order** in `WizardAutofacModule`.

## Design Decisions

### v1 philosophy

Keep it simple: add the components you want, guide defaults for everything. Customization branches
(quality items, profile settings, cutoff) are deferred to v2+ based on user feedback. The YAML is
still the first-class citizen for customizations.

### Screen structure (v1)

1. **Service Type** - Radarr or Sonarr
2. **Instance Details** - Name, base URL, API key, category
3. **Quality Profile Selection** - Optional multi-select from guide profiles
4. **CF Group Selection** - Side-by-side: skip defaults (left), add optional (right)
5. **Quality Sizes** - Yes/No: sync guide quality sizes? Type auto-resolved from category
6. **Media Naming** - Yes/No with dynamic server + ID type selectors
7. **Review & Generate** - Summary of selections, write YAML

### Quality profile flow

Single multi-select screen per instance. User picks profiles from the category they selected in step
2. No per-profile customization; guide defaults for everything.

Selection is optional. If the user presses Enter with nothing selected, a confirmation dialog
appears explaining that CF groups will also be skipped. Confirming advances past both the QP and CF
steps.

### Custom format groups flow

Single screen with two side-by-side panels, auto-skipped if no groups match the selected profiles
(or if no profiles were selected):

+ **Left panel (Skip Default Groups)**: Default groups linked to selected profiles. Nothing selected
  initially. User selects groups to opt out of. Maps to `custom_format_groups.skip`.
+ **Right panel (Add Optional Groups)**: Non-default groups linked to selected profiles. Nothing
  selected initially. User selects groups to opt in to. Maps to `custom_format_groups.add`.

Groups not linked to any selected profile are excluded entirely. Per-CF customization within groups
is deferred; the current design operates at the group level only.

### Quality sizes flow

Simple opt-in/opt-out. The quality size type is auto-determined from service type + category:

+ Radarr Standard/French/German -> `movie`
+ Radarr Anime -> `anime`
+ Sonarr Standard/French/German -> `series`
+ Sonarr Anime -> `anime`

Default: Yes. If yes, generates `quality_definition: type: <resolved>` in the YAML. If no, omits the
section. No per-quality customization (power-user option via hand-editing YAML).

### Media naming flow

Single screen with up to three selectors that appear dynamically based on prior choices:

1. **Yes/No** - "Sync media naming from TRaSH Guides?" (default: Yes). If No, omits the
   `media_naming` section entirely and hides selectors 2-3.
2. **Media server** - None/Plex/Emby/Jellyfin (visible when Yes; default: None). Determines whether
   folder naming includes media server IDs for library matching. "None" uses the generic `default`
   preset keys.
3. **ID type** - Varies by server + service (visible when server != None). The recommended option is
   pre-selected so the user can press Enter to accept. Hidden when only one option exists.

ID type options per combination:

| Service | Server             | Options                     |
| ------- | ------------------ | --------------------------- |
| Radarr  | Plex/Emby/Jellyfin | IMDb (recommended), TMDb    |
| Sonarr  | Plex/Emby          | IMDb, TVDb (recommended)    |
| Sonarr  | Jellyfin           | TVDb only (selector hidden) |

Preset key resolution:

+ **Folder key**: `{server}-{id}` (e.g., `plex-imdb`) or `default` when server is None
+ **File key (Radarr)**: `{server}-{id}` or `{server}-anime-{id}` when category is Anime; or
  `standard` when server is None
+ **File key (Sonarr)**: Always `default` (no server-specific episode format keys exist)
+ **rename**: Always `true` when naming is enabled

## Remaining TODO

+ Per-CF customization within groups (follow-up after group selection works)
+ Implement Review & Generate final step
+ Implement "add another instance" loop
+ Multi-category support within a single instance (e.g., anime + series in one Sonarr). The general
  recommendation for mixed instances is to use the anime quality definition. Blocked on the wizard
  supporting cross-category profile selection.
