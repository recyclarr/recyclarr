---
name: mapperly
description: >-
  Use when writing, editing, or reviewing Mapperly (`Riok.Mapperly`) mapper
  classes; adding or changing a `[Mapper]`-attributed partial class;
  configuring `RequiredMappingStrategy`, `[MapProperty]`, `[MapperIgnore]`,
  or other Mapperly attributes; mapping between nullable Refit/OpenAPI DTOs
  and non-nullable domain records; debugging null-handling in generated
  mapping code (positional records, `required init`, initializer defaults);
  investigating Mapperly diagnostics such as RMG023, RMG060, or RMG089;
  inspecting source-generated output under
  `obj/*/*/generated/Riok.Mapperly/`. Triggers on phrases like "add a
  Mapperly mapper", "Mapperly null handling", "DTO to domain mapping",
  "generated mapper", or edits to files ending in `*Mapper.cs` under
  `ServarrApi/`. Do NOT use for hand-written mapping code or AutoMapper.
---

# Mapperly

Repository conventions and verified null-handling behavior for Mapperly mappers. Behavior below was
observed in generated output from `Riok.Mapperly` 4.3.1 (pinned in `Directory.Packages.props`).
Re-verify against generated code after a Mapperly upgrade.

## Mapper Conventions

- `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]` on every mapper. Generated DTOs
  have many properties we don't map; this silences unmapped-member warnings (RMG012, RMG020).
- Mappers are `internal static partial class`.
- One mapper class per service (Sonarr/Radarr). Generated DTO types share names but are distinct
  types from separate packages; alias namespaces: `using SonarrApi = Recyclarr.Api.Sonarr`.
- Domain-to-DTO writes use an existing-target method
  (`void UpdateDto(TDomain, [MappingTarget] TDto)`) so DTO fields the domain does not model keep
  their fetched values.

## Null Handling

Refit DTOs declare every property nullable. Mapper settings stay at defaults
(`ThrowOnMappingNullMismatch = true`, `AllowNullPropertyAssignment = true`). The generated code for
a null source value depends on the target member shape:

| Target member                                           | Null source value                      |
| ------------------------------------------------------- | -------------------------------------- |
| Nullable (`T?`)                                         | Assigned, including null               |
| Non-nullable constructor parameter                      | `?? throw ArgumentNullException`       |
| Non-nullable `init` or `required` (with or without `=`) | `?? throw ArgumentNullException`       |
| Non-nullable settable (`set`)                           | Assignment skipped; prior value stays  |

Consequences:

- Property initializers never act as fallbacks for constructor or `init` targets; the generated
  object initializer overwrites them or throws. `public string Name { get; init; } = "";` still
  throws when the DTO field is null.
- A field that Sonarr/Radarr may omit needs a nullable domain property, or a user-implemented method
  that supplies the fallback (see `ItemToDomain` in `SonarrQualityProfileMapper`).
- A non-nullable settable property with no initializer stays `null` when the source is null,
  silently violating its contract. Avoid this shape.
- In `UpdateDto`, a null domain value overwrites the DTO field with null, because DTO targets are
  nullable.
- `ThrowOnMappingNullMismatch = false` replaces the throw with `""`, `new T()`, or `default`. It is
  unused in this repository; collections such as `List<T>` still throw and report RMG002.

## Diagnostics

- RMG089 (Info): "Mapping the nullable source property ... to the target property ... which is not
  nullable". Expected for every nullable DTO to non-nullable domain member; it marks a member that
  throws or skips per the table above. Check that outcome is intended rather than suppressing it.
- RMG023 (Error): a `required` target member has no matching source member.
  `RequiredMappingStrategy .None` does not suppress it; add `[MapProperty]` or a user-implemented
  method.
- RMG060 (Warning): multiple user-implemented methods for the same type pair.

## User-Implemented Methods

Mapperly discovers any non-partial method in the mapper with a mapping signature and uses it for
that type pair, including nested members and collection elements (`MapFieldValue` in
`SonarrCustomFormatMapper`). Parameter and return types must match the mapped types exactly,
including nullability.

## Verifying Generated Code

`Directory.Build.props` enables `EmitCompilerGeneratedFiles` for the whole repository. After a
build, find the output with `rg --files --no-ignore -g '<Mapper>.g.cs' src/<Project>/obj`; it lives
under `obj/<Configuration>/<tfm>/generated/Riok.Mapperly/Riok.Mapperly.MapperGenerator/`. Confirm
null handling there instead of inferring it from attributes.
