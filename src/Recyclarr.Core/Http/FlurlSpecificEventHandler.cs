using System.Diagnostics.CodeAnalysis;
using Flurl.Http;

namespace Recyclarr.Http;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification =
    "Naming convention borrowed from and determined by Flurl")]
public abstract class FlurlSpecificEventHandler : FlurlEventHandler
{
    public abstract FlurlEventType EventType { get; }
}
