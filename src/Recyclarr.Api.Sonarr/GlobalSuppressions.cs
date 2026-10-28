using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Usage",
    "CA2227:Collection properties should be read only",
    Justification = "Wire DTOs: the serializer and gateways replace collections on fetched resources",
    Scope = "namespaceanddescendants",
    Target = "~N:Recyclarr.Api.Sonarr"
)]
