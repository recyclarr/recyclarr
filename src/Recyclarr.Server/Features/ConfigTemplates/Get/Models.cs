namespace Recyclarr.Server.Features.ConfigTemplates.Get;

internal sealed record GetConfigTemplateRequest
{
    /// <summary>The template id from the config templates list; case-insensitive.</summary>
    public required string Id { get; init; }
}
