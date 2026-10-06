---
name: ef-core
description: >-
  Use when writing, editing, or reviewing Entity Framework Core code: DbContext classes, entity
  types, model configuration (conventions, attributes, fluent API), persistence ports and their EF
  adapters, LINQ queries, SaveChanges or ExecuteUpdate/ExecuteDelete, migrations and seeding,
  domain-to-entity mapping, or tests that touch a database. Triggers on phrases like "add an
  entity", "add a migration", "DbContext", "EF Core query", "persistence layer", "repository".
---

# EF Core

## Architecture

- `DbContext` is adapter code, never a port. Its `IQueryable` semantics belong to the provider.
- Callers depend on a port the application owns, shaped by use-case operations ("mark instance
  finished", "list active jobs"). No generic repositories or per-table CRUD; `DbContext` already is
  the repository and unit of work.
- Business rules live in callers of the port. The adapter only translates operations into queries
  and transactions. Exception: rules that must run atomically in the database (check-and-update)
  stay in the adapter.
- Domain types are separate from persistence entities. Keys, foreign keys, and other storage-only
  state stay on entities. EF's mapping constraints (bindable constructors, mutable navigations,
  reference identity) must not shape domain types.
- Map with a source-generated mapper that fails the build on unmapped members (e.g. Mapperly).
  Hand-write only conversions it cannot express (serialization, derived values).

## Modeling

Configuration precedence: conventions, then EF attributes, then fluent API. Use the first that
expresses the intent; never restate a convention.

- Know the conventions:
  - Key: `Id` or `<Type>Id`.
  - Foreign key: `<Navigation><Key>`, `<Navigation>Id`, `<Principal><Key>`, `<Principal>Id`. Name FK
    properties to match instead of configuring `HasForeignKey`.
  - Table: the `DbSet` property name.
  - Required relationships cascade on delete; optional ones do not.
  - Nullable reference types set column nullability; do not add `[Required]` or `IsRequired()`.
- Attributes on persistence entities: `[PrimaryKey]`, `[Index]`, `[EntityTypeConfiguration]`.
- Fluent API only for what attributes cannot express (relationship details, `AutoInclude`). Split
  into `IEntityTypeConfiguration<T>` when `OnModelCreating` grows; do not rely on
  `ApplyConfigurationsFromAssembly` ordering.
- Type-wide rules (enum storage, converters) go in `ConfigureConventions`. Write a custom convention
  only when pre-convention configuration cannot express the rule.
- Value objects and JSON columns: complex types (`ComplexProperty`, `[ComplexType]`, `ToJson()`),
  not owned types. Keep them immutable.
- A mutable collection behind a value converter needs a `ValueComparer`; prefer immutable types.
- Do not use records as entity types; change tracking depends on reference identity.

## DbContext lifetime

- One short-lived context per unit of work; dispose it. Never share one across threads or run
  parallel operations on it. Await each call before the next.
- Use `IDbContextFactory<T>` when no DI scope matches the unit of work (background services,
  multiple units per scope). Use pooling only after measuring.
- Seal `DbContext` classes.
- After an EF `InvalidOperationException`, discard the context.

## Querying and saving

- Reads: project with `Select` into the exact shape needed. Projections fetch only needed columns,
  are untracked, and avoid cartesian explosion from multiple collection `Include`s. Mapperly
  `IQueryable` projections qualify; check its documented projection limits.
- When loading entities read-only, use `AsNoTracking`.
- No lazy loading. Load related data explicitly; use `AsSplitQuery` when including sibling
  collections.
- Paginate with a fully unique ordering; prefer keyset pagination over `Skip`/`Take`.
- Keep translation in the database. Switch to client evaluation explicitly (`AsEnumerable`) only
  after filtering.
- Batch changes into one `SaveChanges`; do not save inside loops.
- `ExecuteUpdate`/`ExecuteDelete` bypass the change tracker and run immediately in their own
  transaction. Do not mix them with tracked changes in one unit of work; wrap several in an explicit
  transaction when they must be atomic.
- Use concurrency tokens where concurrent writers can conflict; handle
  `DbUpdateConcurrencyException`.
- Escalate warnings that indicate bugs with `ConfigureWarnings(w => w.Throw(...))`.

## Migrations

- Commit migrations; review every generated one. Renames scaffold as drop and add; replace with
  `RenameColumn`/`RenameTable`. Never reference entity types or the context inside a migration.
- Never combine `EnsureCreated` with migrations.
- Never remove a migration applied outside a local database; add a corrective one.
- Multiple contexts in one database: separate migration sets and `MigrationsHistoryTable` names.
  Multiple providers: one migration set per provider, each in its own assembly.
- Seed with `UseSeeding` and `UseAsyncSeeding`, both implemented and idempotent. `HasData` only for
  static reference data with fixed keys.
- CI runs `dotnet ef migrations has-pending-model-changes`; since EF9, migrating with an outdated
  model throws.
- Write expand/contract migrations: each must work with the previous app version. Additive changes
  ship first; destructive changes ship in a later release.

### Where migrations run

Default: the app migrates at startup (`MigrateAsync`) before serving. It behaves the same in every
environment, deploys schema and code together, and suits apps operated by other people. EF9+ takes a
database-wide lock, so concurrent instances are safe.

Also expose a separate migrate command, so operators can run migrations as a deploy step (e.g. a
Kubernetes init container) and give the app credentials without schema rights.

Move to pre-deploy and post-deploy migrations when DDL outgrows startup (long index builds, large
tables, many instances). Run data backfills as batched, throttled, idempotent background jobs in the
app, never as migrations.

## Testing

- Unit tests never touch `DbContext`. They test port callers; stub the port with a hand-written
  in-memory implementation. Mock it only when a stub cannot express the behavior.
- Never fake `DbContext` or `DbSet`. LINQ-to-objects does not reproduce provider translation, and
  async queries fail without a custom query provider.
- Never use the EF InMemory provider.
- Integration tests run the adapter against the production engine: in-memory SQLite when production
  is SQLite (keep the connection open for the test's lifetime), Testcontainers for server databases.
- Isolate write tests: roll back a per-test transaction, or give each fixture its own database.

## SQLite provider

- Async APIs run synchronously in Microsoft.Data.Sqlite.
- `DateTimeOffset`, `decimal`, `TimeSpan`, and `ulong` cannot be compared or ordered in SQL; convert
  (e.g. `DateTimeOffsetToBinaryConverter`) or store UTC `DateTime`.
- No database-generated concurrency tokens; the app sets the token on each save.
- Many schema changes rebuild the table; review migrations touching large tables.
- A crashed migration can leave a row in `__EFMigrationsLock` that blocks later migrations; clear
  the table to recover.
- EF10 changed `DateTime`/`DateTimeOffset` time-zone handling in Microsoft.Data.Sqlite; check stored
  values when upgrading.
