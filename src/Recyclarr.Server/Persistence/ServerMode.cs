namespace Recyclarr.Server.Persistence;

/// <summary>
/// How this server process was launched (ADR-010). A persistent server (<c>recyclarr serve</c>)
/// owns durable state and the sync schedule; an ephemeral server lives only for one CLI command,
/// so it keeps no state on disk and never schedules work.
/// </summary>
internal enum ServerMode
{
    Persistent,
    Ephemeral,
}
