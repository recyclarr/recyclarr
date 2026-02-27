# Service Adapter Layer + Refit Migration Plan

Living document tracking the implementation of ADR-005 (service adapter layer) and ADR-006
(Refit/Refitter migration).

## Architecture Decisions

- [ADR-005](../decisions/architecture/005-service-adapter-layer.md): Port + Adapter pattern,
  hydrated resource state, shared vs split pipeline litmus test
- [ADR-006](../decisions/architecture/006-refit-refitter-api-clients.md): Refit + Refitter for
  generated API clients from OpenAPI specs

## Key Design Patterns

**Port + Adapter** (per feature, per service):
- Port: domain interface in `Servarr/` layer, speaks domain types only. Port methods use read-only
  collection types (`IReadOnlyList<T>`) for both parameters and return values; the caller doesn't
  need mutability and the adapter shouldn't modify what it receives.
- Adapter: implements port, owns HTTP client, stashes fetched DTOs for round-trip. Contains
  to/from-domain mapping as private methods (no separate translator class; the mapping is private to
  one adapter, tested through the adapter, and called from one place)

**Directory structure** (`src/Recyclarr.Core/`):
- `Servarr/{Feature}/` - port interfaces and domain types (e.g. `Servarr/QualitySize/`,
  `Servarr/SystemStatus/`). These are NOT pipeline-exclusive; system status is consumed by
  compatibility, not a pipeline. Avoid `System` as a subdirectory name (collides with BCL namespace).
- `ServarrApi/{Feature}/` - adapters alongside Flurl-backed API services and DTOs. DTO types use
  `Service` prefix (e.g. `ServiceQualityDefinitionItem`, `ServiceSystemStatus`) to avoid collision
  with domain types and namespace names.

**Domain type `required` properties**: driven by domain/pipeline invariants, not API schema. A
property is `required` only when omitting it at construction would silently mask a bug in domain
logic. If a field has a natural zero/null default that is either valid or harmless in domain context,
use a non-required `init` property with that default. The API may guarantee a field is always
present, but that does not mean the domain type needs to enforce it at the type level. Decision tree:

1. Can the caller ever reasonably not care about this field's value? If yes, non-required with
   sensible default.
2. Would a default value silently break matching, lookup, or business logic? If yes, `required`.
3. Otherwise, non-required with that safe default.

Examples: `QualityName` is required (empty string breaks matching). `Id` is not (zero is harmless;
the adapter always sets it). Nullable fields like `MaxSize` default to null (which has domain meaning
"unlimited").

**Test factory helpers (`New*` classes)**: each adapter phase MUST overhaul test helpers for
consistency. Rules:

- One `New{DomainType}` class per domain type (e.g. `NewQualityDefinition`, `NewCustomFormat`). No
  grab bags mixing unrelated types.
- Factory methods accept only parameters relevant to the test, with sensible defaults for the rest.
  This shields tests from model changes and keeps construction sites focused on what the test
  actually asserts.
- Methods return one type. If a domain type has variants (e.g. with/without scores), use overloads or
  optional parameters, not separate classes.
- Location: `Core.TestLibrary` for types defined in Core; `Cli.Tests/Reusable` for types defined in
  Cli.
- Existing helpers that violate these rules (e.g. `NewPlan` as a grab bag, `NewQp` mixing DTOs with
  domain types) get overhauled when their phase is touched. Scorched earth; don't preserve legacy
  patterns for consistency with the old code.
- `NewPlan` specifically: plan item factory methods (e.g. `Cf`, `CfScore`, `Qp`, `Qs`, `QsItem`)
  should move to the `New*` class for the type they construct. `NewPlan` retains only methods that
  construct `TestPlan` itself or `Planned*` wrapper types that don't have their own `New*` class.

**Hydrated resource pattern**: adapter holds original DTOs in an internal dictionary keyed by
resource ID. Populated during fetch, consumed during persist, scoped to one sync operation via
`InstancePerLifetimeScope`.

**DI resolution**: keyed registration per service + non-keyed lambda that reads
`IServiceConfiguration.ServiceType` at resolve time:

```csharp
// Keyed (per service)
builder.RegisterType<SonarrFooAdapter>()
    .Keyed<IFooService>(SupportedServices.Sonarr);
builder.RegisterType<RadarrFooAdapter>()
    .Keyed<IFooService>(SupportedServices.Radarr);

// Non-keyed (what pipeline phases inject)
builder.Register(c =>
        c.ResolveKeyed<IFooService>(
            c.Resolve<IServiceConfiguration>().ServiceType))
    .As<IFooService>()
    .InstancePerLifetimeScope();
```

**Shared vs split pipeline litmus test**:
- Same resource, service-specific properties -> shared pipeline + adapter
- Different resource concepts behind a shared path -> split into service-specific pipelines

**Adapter multiplicity**: every pipeline always gets one adapter per service (Sonarr adapter +
Radarr adapter), regardless of whether the pipeline is shared or split. For shared pipelines, both
adapters implement the same port interface and DI resolves the correct one via the keyed/non-keyed
pattern. For split pipelines, each pipeline has its own port and single adapter (e.g.
`ISonarrNamingService` implemented by `SonarrNamingAdapter`). There is never a single "shared"
adapter that handles both services; the adapter is always service-specific because it owns
service-specific HTTP calls and mapping logic. Even when schemas are identical today (e.g. custom
formats, system status), separate adapters per service are required because they will back different
generated Refit clients after Part B. The duplication in identical-schema adapters is mechanical and
temporary; it disappears once each adapter injects its own service-specific Refit interface.
Deferring the split to Part B was considered and rejected because it creates inconsistency during
Part A and requires a "split this adapter" refactor during the Refit migration.

## Phases

### Part A: Adapter Layer (Architecture)

Each phase introduces the port + adapter for one pipeline. Pipeline phases are updated to inject the
port and operate on domain types. Flurl remains the HTTP client inside adapters.

#### Phase 1: System pipeline adapter
- **Status:** done
- **Size:** tiny (pattern-establishing; read-only, one method, identical schemas)
- **Notes:** First adapter. Establishes project structure, DI conventions, and the keyed/non-keyed
  registration pattern. No round-trip concern (no writes).

#### Phase 2: Quality Definitions pipeline adapter
- **Status:** done
- **Size:** small
- **Notes:** Two methods (get, update). Identical schemas. First pipeline with a write path; first
  real test of the hydrated resource pattern. Established the `Servarr/` directory convention for
  ports and domain types.

#### Phase 3: Media Management pipeline adapter
- **Status:** not started
- **Size:** small
- **Notes:** Two methods, minor schema divergence between services. First pipeline where the two
  adapters differ in mapping logic.

#### Phase 4a: Media Naming pipeline split
- **Status:** not started
- **Size:** small
- **Notes:** Pure structural split. Create `SonarrNamingPipelineContext` and
  `RadarrNamingPipelineContext` with their own phase sets. Move existing code from behind switch
  statements. Delete: abstract `MediaNamingDto` base, `IServiceBasedMediaNamingConfigPhase`, keyed
  DI for config phases, all switch/pattern-match dispatch in transaction/preview/persistence phases.
  `NamingFormatLookup` stays as shared utility. No adapter layer yet; phases talk directly to
  service-specific API services (still Flurl).

#### Phase 4b: Media Naming adapters
- **Status:** not started
- **Size:** small
- **Notes:** Add port + adapter for both Sonarr and Radarr naming pipelines. Each pipeline gets its
  own port (e.g. `ISonarrNamingService`, `IRadarrNamingService`). No shared interface needed since
  these are independent domain concepts.

#### Phase 5: Custom Formats pipeline adapter
- **Status:** not started
- **Size:** medium
- **Notes:** Full CRUD (4 methods), identical schemas. Stashed state dictionary manages multiple
  resources; create returns IDs that need tracking, deletes remove entries. First adapter with real
  lifecycle complexity.

#### Phase 6: Quality Profiles pipeline adapter
- **Status:** not started
- **Size:** large
- **Depends on:** Phase 5 (CF adapter must be in place; QP depends on CF pipeline output for score
  ID hydration)
- **Notes:** Most complex. Five methods including schema and languages. Nested DTO graph. Radarr
  language divergence. Three Refitter-generated interfaces (`QualityProfile`,
  `QualityProfileSchema`, `Language`) will eventually compose behind one port. This is where every
  design decision gets stress-tested.

### Part B: Refit Migration (Tooling)

Adapters are in place from Part A. These phases swap the HTTP client inside adapters from Flurl to
Refit. Pipeline phases are untouched.

#### Phase 7: Refit infrastructure
- **Status:** not started
- **Size:** small-medium
- **Notes:** Prerequisite for phases 8-9. Rewrite Flurl event handlers as `DelegatingHandler`
  subclasses: request/response logging with URL redaction, redirect handling. Auth header
  (`X-Api-Key`) as a `DelegatingHandler`. SSL certificate bypass via
  `ConfigurePrimaryHttpMessageHandler`. HttpClientFactory registration pattern.

#### Phase 8: Sonarr Refit migration
- **Status:** not started
- **Size:** medium
- **Depends on:** Phase 7
- **Notes:** Create `Recyclarr.Api.Sonarr` project with `.refitter` config. Generate Refit
  interfaces and DTOs from Sonarr OpenAPI spec (`includeTags` scoped to consumed endpoints). Update
  all Sonarr adapter internals from Flurl to injected Refit interfaces. Update adapter mapping
  methods for generated DTOs.

#### Phase 9: Radarr Refit migration
- **Status:** not started
- **Size:** medium
- **Depends on:** Phase 7
- **Notes:** Same as phase 8 for Radarr. Independent of phase 8; can be done in parallel or either
  order.

#### Phase 10: Apprise Refit migration
- **Status:** not started
- **Size:** tiny
- **Notes:** Hand-written Refit interface (single POST endpoint, no OpenAPI spec). Replace
  `AppriseRequestBuilder` and `AppriseNotificationApiService`. Custom JSON serialization settings
  move to `RefitSettings`.

#### Phase 11: Flurl removal
- **Status:** not started
- **Size:** tiny
- **Depends on:** Phases 8, 9, 10
- **Notes:** Remove Flurl and Flurl.Http packages from `Directory.Packages.props` and project
  references. Delete `IServarrRequestBuilder`, `ServarrRequestBuilder`, `FlurlSpecificEventHandler`,
  `FlurlBeforeCallLogRedactor`, `FlurlAfterCallLogRedactor`, `FlurlRedirectPreventer`,
  `FlurlLogging`, `IAppriseRequestBuilder`, `AppriseRequestBuilder`. Delete `Flurl.Http.Testing`
  usage in tests.

## Dependency Graph

```
Phase 1 (System)
Phase 2 (Quality Definitions)
Phase 3 (Media Management)
Phase 4a (Naming split) -> Phase 4b (Naming adapters)
Phase 5 (Custom Formats) -> Phase 6 (Quality Profiles)

All of Part A -> Phase 7 (Refit infra) -> Phase 8 (Sonarr Refit)
                                       -> Phase 9 (Radarr Refit)
                                       -> Phase 10 (Apprise Refit)
                                          Phase 8 + 9 + 10 -> Phase 11 (Flurl removal)
```

Phases 1, 2, 3, 4a, and 5 have no dependencies on each other (except 4a before 4b, and 5 before 6).
They can be done in any order. Suggested order is simplest-first to establish patterns
incrementally.

## Related Issues

- [REC-88](https://linear.app/recyclarr/issue/REC-88): Explore making CF -> QP pipeline data
  dependency explicit (backlog, not blocking)

## Open Questions

- Exact domain type shapes will be defined during each phase (not designed upfront)
- Refitter `.refitter` config tuning (generated output quality) will be discovered during phases 8-9
- `DelegatingHandler` design for logging/redaction will be detailed during phase 7

## Design Discussion Notes

Captured from the initial architecture discussion. These record the reasoning, rejected
alternatives, and boundary-testing that led to the decisions above.

### Why migrate from Flurl?

The initial evaluation compared Flurl (current), Refit, RestEase, and OpenAPI code generation. Flurl
is deeply integrated (18 production files, custom event handlers, per-client serializer config). The
migration was not motivated by Flurl being broken but by two concerns:

1. **Upstream change detection.** Hand-maintained DTOs with `[JsonExtensionData]` are change-blind.
   If Sonarr renames a field, the current code silently stops sending it (deserializes as null, no
   compile error). Generated types from the OpenAPI spec surface this as a compile error.

2. **Architectural debt.** The unified "Servarr" abstraction assumes Sonarr and Radarr are the same
   API. This assumption is increasingly wrong and produces dispatch hacks at every divergence point.
   This concern is independent of HTTP client choice and arguably more important.

RestEase was rejected due to single maintainer (bus factor) and Newtonsoft default. Pure
openapi-generator was rejected because the Java toolchain is heavy for a .NET project. Kiota was
considered but has ergonomic issues (all properties nullable). Refitter generates Refit interfaces
from specs, combining the benefits of both approaches.

### The Servarr abstraction problem

The current `IServarrRequestBuilder` encodes an assumption: Sonarr and Radarr are the same API with
cosmetic differences. Each divergence becomes an exception to the rule:

- Media naming: three dispatch mechanisms (enum switch in API service, keyed DI for config phases,
  pattern matching in persistence)
- Quality profiles: `ExtraJson` silently absorbs Radarr's `language` field
- Media management: `ExtraJson` absorbs domain-specific fields

The pattern: every new service-specific feature becomes another special case bolted onto a unified
abstraction. The exceptions were starting to outnumber the rule.

### OpenAPI spec comparison

Both Sonarr and Radarr publish official OpenAPI 3.0 specs (auto-generated via Swashbuckle). A
detailed comparison of the endpoints Recyclarr uses revealed more divergence than the current
codebase acknowledges:

- **Custom Formats:** Identical schemas. Truly shareable.
- **Quality Definitions:** Identical (nested `Quality` model differs but main schema matches).
- **Language:** Identical.
- **System Status:** Near-identical (`sqliteVersion` Sonarr-only).
- **Quality Profiles:** Diverge. Radarr adds `language` field.
- **Media Management:** Diverge. Domain-specific fields, different enum values.
- **Media Naming:** Completely different. Almost no overlap beyond `id`.

The current code hides the quality profile and media management divergence behind `ExtraJson`. This
works but is change-blind (see upstream change detection above).

### Adapter boundary: where to draw the line

The initial proposal was "adapter for shared, split pipeline for divergent." Testing this against
media naming (the hardest case) revealed that the abstract `MediaNamingDto` base class is a fiction;
it has no properties and exists only to let the pipeline context hold either type. Sonarr episode
naming and Radarr movie naming are unrelated domain concepts behind a shared endpoint path.

**The litmus test:** "Is there a meaningful shared domain concept, or just a shared endpoint path?"

- Same resource with service-specific properties: shared pipeline + adapter. The domain type has
  optional properties; each adapter populates what its service supports. Example: quality profiles
  with Radarr language.
- Different resource concepts: split into service-specific pipelines. Example: media naming.

This was tested with hypotheticals:

- **Hypothetical 1:** Both services have "language" but different wire formats (Radarr: int, Sonarr:
  string). Adapter handles it; same domain concept, different serialization. No split needed.
- **Hypothetical 2:** Radarr returns a complex language object (`{name, id, region}`) that needs
  round-tripping. Adapter handles it; the complexity is in the translator and hydrated state, not
  the pipeline.

### The read-modify-write problem

Central challenge: fetch a 50-field resource, domain logic cares about 5 fields, PUT it back without
losing the other 45. Four approaches were researched:

1. **`[JsonExtensionData]` on DTOs** (current approach). Recognized pattern, not a hack. But only
   works when you control the DTO type; doesn't survive migration to generated types.
2. **Hydrated resource pair.** Carry the original DTO alongside the domain type. Adapter stashes the
   raw DTO and merges domain changes back on persist.
3. **Mutable JSON node.** Work at the JSON DOM level (`JsonNode`). Maximum flexibility, no schema
   needed. Overkill for this use case.
4. **Fetch-in-update.** Adapter re-GETs the resource during persist. Stateless but adds network
   calls and failure modes.

**Decision: Hydrated resource pattern (option 2).** The adapter holds stashed DTOs in a dictionary
keyed by resource ID. No extra GET, no `ExtraJson` on domain types, scoped to one sync operation.

The fetch-in-update alternative was rejected because it solves a staleness problem that doesn't
exist within a single sync operation. The remote state won't drift between fetch and persist phases
(seconds apart, Recyclarr is typically the only automated consumer).

### Adapter statefulness and parallelism

Concern raised: does stateful adapter (internal dictionary) break under parallelism?

Analysis: The access pattern is naturally safe. Bulk GET populates the dictionary before any persist
work starts (sequential populate, parallel reads). Even if individual resources were fetched in
parallel, `ConcurrentDictionary` handles it. Each sync operation gets its own adapter instance via
`InstancePerLifetimeScope`, so no cross-sync contention.

The parallelism concern is YAGNI for Recyclarr's workload but the pattern holds up regardless.

### DI resolution for service-specific adapters

Problem: pipeline phases inject `IQualityProfileService` directly (no factory, no lazy, no child
scope registration). Two adapters exist (Sonarr, Radarr). How does DI resolve the right one?

Rejected approaches:
- **Child scope registration:** antipattern; mutating the container at runtime
- **`ServiceAdapter<T>` wrapper with `.Value`:** lazy indirection leaks into consumers
- **`IIndex<SupportedServices, T>`:** requires consumer to know about keying

Chosen approach: keyed registrations for concrete adapters + non-keyed lambda registration that
resolves based on `IServiceConfiguration.ServiceType` at resolve time. Pipeline phases inject the
port directly; they have no idea keying is involved. Autofac supports the same interface being
registered as both keyed and non-keyed (separate registration pools).

### Phase-level code sharing for split pipelines

Concern: if media naming splits into two pipelines, how much code is duplicated?

Analysis of every media naming phase revealed almost no shared code that matters:
- Fetch: trivial (one API call each)
- Config: already split (keyed DI)
- Transaction: switch dispatching to two methods with zero shared logic
- Preview: switch dispatching; shared code is one `AddRow` helper
- Persistence: shared code is 3 lines of diff-count logging

The split removes more code (abstract base, switches, keyed DI, dispatch interface) than it
duplicates (~10 lines). Shared utilities like `NamingFormatLookup` remain as standalone helpers both
pipelines consume.

General principle: split pipelines don't mean "duplicate everything." Shared logic lives in helper
classes that both pipelines depend on, same as any two unrelated pipelines sharing a utility.

### CF -> QP pipeline data dependency

Quality Profiles cannot be migrated independently of Custom Formats. `PlannedCfScore` holds a direct
object reference to `PlannedCustomFormat`. When CF persistence creates a new format and the API
returns its ID, that ID is visible to QP automatically through the shared reference. This coupling
is implicit (no visible contract) but functional.

Tracked as [REC-88](https://linear.app/recyclarr/issue/REC-88) for future exploration. Not blocking
current work. Practical impact: CF adapter must be done before QP adapter (phase 5 before phase 6).

### Refitter interface decomposition

Both Sonarr and Radarr OpenAPI specs use tags that align with Recyclarr's domain decomposition:
`CustomFormat`, `QualityProfile`, `QualityProfileSchema`, `Language`, `QualityDefinition`,
`NamingConfig`, `MediaManagementConfig`, `System`. Refitter's `multipleInterfaces: ByTag` produces
one Refit interface per tag, matching the desired domain-scoped interfaces without post-generation
surgery.

Minor wrinkle: `QualityProfileSchema` and `Language` generate as separate interfaces from
`QualityProfile`. The adapter composes all three behind one port. Wiring detail, not a design
concern.

### Why no separate translator classes?

The original design had a separate static translator class per adapter (e.g.
`SonarrSystemTranslator`) containing pure `ToDomain`/`FromDomain` mapping functions. This was
reconsidered and rejected. The mapping logic is private to one adapter, called from one place, and
tested through the adapter's public surface. Extracting it to a separate class adds a file and
indirection with no concrete benefit:

- **Testing:** Adapter tests exercise the mapping identically. The only difference between testing
  `Translator.ToDomain(dto)` directly vs through the adapter is a single `.Returns(...)` mock setup
  line, which is trivial.
- **Reuse:** The mapping is never shared; each adapter has its own service-specific mapping even
  when schemas are identical.
- **Complexity:** Even for complex cases (Quality Profiles with nested DTO graphs and many input
  shape variations), the test setup cost of going through the adapter is negligible.

Decision: to/from-domain mapping lives as private methods on the adapter. No separate translator
classes.

### Why not fix architecture and migrate to Refit simultaneously?

Sequencing discussion concluded: fix the architecture first (adapter layer with Flurl inside), then
swap Flurl for Refit inside adapters. Reasons:

- The adapter layer is independently valuable regardless of HTTP client
- Validates the domain type design and round-trip pattern before adding framework novelty
- Each adapter phase is a clean PR with one concern (architecture OR tooling, not both)
- Natural pause point after Part A if priorities shift
