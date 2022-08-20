namespace TrashLib.Repo;

public record RadarrMetadata(
    IReadOnlyCollection<string> CustomFormats
);

public record SonarrMetadata(
    IReadOnlyCollection<string> ReleaseProfiles
);

public record JsonPaths(
    RadarrMetadata Radarr,
    SonarrMetadata Sonarr
);

public record RepoMetadata(
    JsonPaths JsonPaths
);
