using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.TrashLib.Config.Services;

public interface IServiceConfiguration
{
    string ServiceName { get; }

    string? InstanceName { get; }

    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings",
        Justification = "This is not treated as a true URI until later")]
    string BaseUrl { get; }

    string ApiKey { get; }

    bool DeleteOldCustomFormats { get; }
}
