---
name: mapperly
description: >-
  Use when writing or modifying Mapperly mapper classes, debugging null-handling
  in generated mapping code, or adding new DTO-to-domain mappings
---

# Mapperly

Conventions and null-handling semantics for Mapperly source-generated mappers.

## Mapper Conventions

- `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]` on every mapper. Generated DTOs
  have many properties we don't map; this silences unmapped-property warnings.
- One mapper class per service (Sonarr/Radarr). Generated DTO types share names but are distinct
  types from separate packages.
- Namespace aliases disambiguate: `using SonarrApi = Recyclarr.Api.Sonarr`.
- Mappers are `internal static partial class`.

## Null Handling (nullable DTO -> non-nullable domain)

Generated Refit DTOs mark all properties as nullable (`T?`). Domain models use non-nullable types
for fields that should always have a value. Mapperly handles the mismatch differently depending on
the target shape.

### Constructor parameters (positional records)

Mapperly substitutes type-appropriate defaults when the source is null:

- `string?` -> `string`: `""` (empty string)
- `T?` where `T` has a parameterless constructor: `new T()`
- `T?` value type: `default(T)`

### Init properties without initializers

Mapperly generates `throw ArgumentNullException` when the source is null. This is hardcoded for
`required init` properties regardless of mapper settings.

### Init/setter properties with initializers (e.g. `= ""`, `= []`)

Mapperly skips the assignment when the source is null. The initializer value is preserved. This is
the safest pattern for "optional with sensible default" semantics.

### Setter properties without initializers

Mapperly skips the assignment. The property stays at `default(T)`, which for reference types is
`null`. This silently violates the non-nullable contract. Avoid this shape for non-nullable
reference types.

### Nullable target properties

Assigned directly, including null. No special handling.

## RMG089 Diagnostic

"Mapping nullable source property X to target property Y which is not nullable." Default severity:
Info. This diagnostic is informational; the generated code handles the mismatch using the rules
above. No suppression or severity promotion is needed.

## Inspecting Generated Code

To see what Mapperly actually generates, temporarily add to `Directory.Build.props`:

```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
```

Output appears in `obj/Debug/<tfm>/generated/Riok.Mapperly/Riok.Mapperly.MapperGenerator/`. Remove
the property when done.
