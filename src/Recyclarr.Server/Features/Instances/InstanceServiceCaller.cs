using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Server.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Server.Features.Instances;

// The outcome of one call to a configured Sonarr or Radarr instance outside a sync job. Value is
// set only when the instance exists and the call succeeded.
internal sealed record InstanceCallResult<T>(
    bool InstanceFound,
    T? Value = default,
    OperationalFailure? Failure = null
);

// Runs a service call inside the named instance's scope, the same scope a sync uses, and
// classifies expected service failures with the classifier sync results use.
internal sealed class InstanceServiceCaller(
    ILogger log,
    ServerConfigurationStore configuration,
    InstanceScopeFactory scopes
)
{
    public async Task<InstanceCallResult<T>> Call<TService, T>(
        string instanceName,
        Func<TService, Task<T>> call
    )
        where TService : notnull
    {
        var config = Find(instanceName);
        if (config is null)
        {
            return new InstanceCallResult<T>(InstanceFound: false);
        }

        using var scope = scopes.Start<TService>(config);
        try
        {
            return new InstanceCallResult<T>(InstanceFound: true, Value: await call(scope.Entry));
        }
        catch (Exception e) when (OperationalFailureClassifier.Classify(e) is { } failure)
        {
            log.Warning(e, "Call to instance {Instance} failed", config.InstanceName);
            return new InstanceCallResult<T>(InstanceFound: true, Failure: failure);
        }
    }

    private IServiceConfiguration? Find(string instanceName) =>
        configuration.Current.Instances.FirstOrDefault(x =>
            x.InstanceName.Equals(instanceName, StringComparison.OrdinalIgnoreCase)
        );
}
