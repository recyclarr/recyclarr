# ResourceRegistry Architecture Analysis

## Overview

The ResourceRegistry system provides a generic, extensible infrastructure for:
1. **Registering resources** from various provider types (TRaSH Guides, Config Templates, Custom Formats)
2. **Querying resources** by service type (Radarr/Sonarr)
3. **Loading resource metadata** from JSON files via `JsonResourceLoader`
4. **Supporting new resource types** (CfGroupResource, QualityProfileResource)

## Core Components

### 1. ResourceRegistry<TMetadata> - Infrastructure/ResourceRegistry.cs

**Responsibilities:**
- Generic, type-keyed registry for storing metadata collections
- Maps resource types → metadata lists
- Thread-safe dictionary-based storage

**Key Methods:**
- `Register<TResource>(IEnumerable<TMetadata> metadata)` - Register metadata for a resource type
- `Get<TResource>() → IReadOnlyCollection<TMetadata>` - Retrieve all metadata for a resource type

**Design Pattern:**
- Generic over metadata type (IFileInfo, TemplateMetadata, etc.)
- Uses Type as dictionary key for polymorphism
- SingleInstance lifecycle in Autofac

**Limitations:**
- Only one metadata type per registry instance (IFileInfo for ResourceProviders, TemplateMetadata for ConfigTemplates)
- Requires separate Registry instances for different metadata types
- No built-in support for cross-resource queries

### 2. JsonResourceLoader - Domain/JsonResourceLoader.cs

**Responsibilities:**
- Deserialize generic JSON resources from files
- Return (Resource, SourceFile) tuples for linked metadata

**Key Method:**
```csharp
Load<TResource>(IEnumerable<IFileInfo> files)
  → IEnumerable<(TResource Resource, IFileInfo SourceFile)>
```

**Design:**
- Generic over resource type (enforced: class constraint)
- Uses GlobalJsonSerializerSettings.Guide for deserialization
- Filters out null deserializations silently

### 3. Resource Query Services

**Pattern:** Each resource type has a dedicated query service that:
1. Fetches file metadata from registry
2. Loads resources via JsonResourceLoader
3. Post-processes (grouping, deduplication, category assignment)
4. Returns Radarr/Sonarr-specific collections

**Examples:**

#### CustomFormatResourceQuery (Domain)
- Registry type: `ResourceRegistry<IFileInfo>`
- Metadata type: `IFileInfo`
- Resource types: `RadarrCustomFormatResource`, `SonarrCustomFormatResource`
- Post-processing: Groups by TrashId, assigns category metadata

#### QualitySizeResourceQuery (Domain)
- Registry type: `ResourceRegistry<IFileInfo>`
- Metadata type: `IFileInfo`
- Resource types: `RadarrQualitySizeResource`, `SonarrQualitySizeResource`
- Post-processing: Groups by Type (case-insensitive)

#### ConfigTemplatesResourceQuery (ConfigTemplates)
- Registry type: `ResourceRegistry<TemplateMetadata>`
- Metadata type: `TemplateMetadata` (custom, contains Id, TemplateFile, Hidden)
- Resource types: `RadarrConfigTemplateResource`, `SonarrConfigTemplateResource`
- Post-processing: Transforms TemplateMetadata → Resource, groups by Id

#### ConfigIncludesResourceQuery (ConfigTemplates)
- Registry type: `ResourceRegistry<TemplateMetadata>`
- Metadata type: `TemplateMetadata`
- Resource types: `RadarrConfigIncludeResource`, `SonarrConfigIncludeResource`
- Post-processing: Same as ConfigTemplatesResourceQuery

### 4. Provider Type Strategies

**Interface:** `IProviderTypeStrategy` - handles provider initialization and resource mapping

**Implementations:**

#### TrashGuidesStrategy
- Maps TRaSH Guides provider to registry
- Registers 6 resource types: CF, QualitySize, MediaNaming (Radarr/Sonarr each)
- Also registers CategoryMarkdownResource (2 types)
- Uses metadata.json to locate JSON directories
- Flow: metadata.json → relative paths → directory globs → file enumeration

#### CustomFormatsStrategy
- Maps Custom Formats provider to registry
- Registers 2 resource types: RadarrCustomFormatResource, SonarrCustomFormatResource
- All JSON files in root directory (recursive)
- Service-based dispatch (radarr vs sonarr)

#### ConfigTemplatesStrategy
- Maps Config Templates provider to registry
- Registers 4 resource types: ConfigTemplate, ConfigInclude (Radarr/Sonarr each)
- Reads templates.json and includes.json metadata files
- Creates TemplateMetadata objects with file references

**Resource Path Registration Pattern:**
```csharp
// TrashGuidesStrategy - IFileInfo registry
registry.Register<RadarrCustomFormatResource>(
  GlobJsonFiles(metadata.JsonPaths.Radarr.CustomFormats, rootPath)
);

// ConfigTemplatesStrategy - TemplateMetadata registry
registry.Register<RadarrConfigTemplateResource>(
  templatesData.Radarr.Select(e => TemplateMetadata.From(e, rootPath))
);

// CustomFormatsStrategy - IFileInfo registry
registry.Register<RadarrCustomFormatResource>(files);
```

### 5. Autofac Registration - ResourceProviderAutofacModule.cs

**Structure:**
- `RegisterInfrastructure()`: ResourceRegistry<T> (generic), Strategies, Factory
- `RegisterStorageLayer()`: Provider location handlers
- `RegisterDomainLayer()`: All query services

**Key Registration:**
```csharp
builder.RegisterGeneric(typeof(ResourceRegistry<>)).AsSelf().SingleInstance();
```

This creates separate instances for each TMetadata type:
- `ResourceRegistry<IFileInfo>` for TrashGuides/CustomFormats
- `ResourceRegistry<TemplateMetadata>` for ConfigTemplates/ConfigIncludes

## Resource Type Definitions

### File-Based Resources (IFileInfo registry)
- `CustomFormatResource` - extends with Category property
- `QualitySizeResource` - Type + Qualities collection
- `MediaNamingResource` - Radarr/Sonarr variants
- `CategoryMarkdownResource` - Radarr/Sonarr variants

### Metadata-Based Resources (TemplateMetadata registry)
- `ConfigTemplateResource` - record with Id, TemplateFile, Hidden
- `ConfigIncludeResource` - record with Id, TemplateFile, Hidden
- Custom types defined in ConfigTemplates folder

## Adding a New Resource Type - Current Pattern

To add QualityProfileResource:

1. **Define resource class** in Domain folder:
   ```csharp
   public record RadarrQualityProfileResource { ... }
   public record SonarrQualityProfileResource { ... }
   ```

2. **Create query service** (QualityProfileResourceQuery.cs):
   ```csharp
   public class QualityProfileResourceQuery(
       ResourceRegistry<IFileInfo> registry,
       JsonResourceLoader loader,
       ILogger log
   )
   {
       public IReadOnlyList<RadarrQualityProfileResource> GetRadarr() { ... }
       public IReadOnlyList<SonarrQualityProfileResource> GetSonarr() { ... }
   }
   ```

3. **Register in provider strategy** (TrashGuidesStrategy.MapResourcePaths):
   ```csharp
   registry.Register<RadarrQualityProfileResource>(
       GlobJsonFiles(metadata.JsonPaths.Radarr.QualityProfiles, rootPath)
   );
   registry.Register<SonarrQualityProfileResource>(
       GlobJsonFiles(metadata.JsonPaths.Sonarr.QualityProfiles, rootPath)
   );
   ```

4. **Update metadata.json** with new JSON paths

5. **Register query service** in ResourceProviderAutofacModule:
   ```csharp
   builder.RegisterType<QualityProfileResourceQuery>().AsSelf().SingleInstance();
   ```

## Limitations & Generalization Needs for CfGroupResource & QualityProfileResource

### Current Limitations

1. **Metadata Type Rigidity:**
   - TrashGuides uses IFileInfo registry
   - ConfigTemplates uses TemplateMetadata registry
   - Can't easily add new metadata types without new registry instance

2. **Query Service Duplication:**
   - Every resource type needs GetRadarr/GetSonarr methods
   - Post-processing logic (grouping, filtering) duplicated across services
   - No base class or interface to enforce pattern

3. **Provider Strategy Coupling:**
   - Each resource type requires explicit registration call
   - New resource types require modifying TrashGuidesStrategy.MapResourcePaths
   - No convention-based or reflection-based registration

4. **Category Assignment:**
   - Only CustomFormatResourceQuery handles category assignment
   - Pattern not reusable for other resources needing metadata enrichment

5. **No Cross-Registry Queries:**
   - Can't efficiently query across multiple metadata sources
   - No way to correlate CFs with CF Groups or Profiles with QualityProfiles

### Recommended Generalization Approach

1. **Base Query Service Class:**
   ```csharp
   public abstract class ResourceQueryService<TResource, TMetadata>(
       ResourceRegistry<TMetadata> registry,
       ILogger log
   )
   where TResource : class
   {
       protected IReadOnlyList<TResource> GetResources<TSpecific>()
           where TSpecific : TResource
       { ... }

       protected abstract TSpecific PostProcess(TSpecific resource);
   }
   ```

2. **Convention-Based Strategy Registration:**
   - Use reflection to auto-register resource types
   - Deduce metadata.json paths from type names

3. **Cross-Resource Queries:**
   - Add secondary registry for correlation metadata
   - Support queries like "which CFs are in group X"

4. **Strategy Base Class:**
   - Reduce duplication in MapResourcePaths
   - Standardize post-processing callback pattern

## Critical Files for Modification

- `/Users/robert/code/recyclarr/src/Recyclarr.Core/ResourceProviders/Infrastructure/ResourceRegistry.cs`
- `/Users/robert/code/recyclarr/src/Recyclarr.Core/ResourceProviders/Domain/JsonResourceLoader.cs`
- `/Users/robert/code/recyclarr/src/Recyclarr.Core/ResourceProviders/Infrastructure/TrashGuidesStrategy.cs`
- `/Users/robert/code/recyclarr/src/Recyclarr.Core/ResourceProviders/ResourceProviderAutofacModule.cs`
- New: `QualityProfileResourceQuery.cs`
- New: `CfGroupResourceQuery.cs`
- New: Quality profile and CF group resource classes
