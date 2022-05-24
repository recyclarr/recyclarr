namespace Recyclarr.Command;

public interface ISonarrCommand : IServiceCommand
{
    bool ListReleaseProfiles { get; }
    string? ListTerms { get; }
}
