using Autofac;
using Recyclarr.Compatibility;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Server.Resources;
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
    ResourceCoordinator resources,
    InstanceScopeFactory scopes
)
{
    public async Task<InstanceCallResult<T>> Call<TService, T>(
        string instanceName,
        Func<TService, Task<T>> call,
        CancellationToken ct
    )
        where TService : notnull
    {
        var config = Find(instanceName);
        if (config is null)
        {
            return new InstanceCallResult<T>(InstanceFound: false);
        }

        using var instance = scopes.Start<ILifetimeScope>(config);
        try
        {
            // Gateways are selected by the detected API version, so they resolve only from the
            // scope created after detection.
            var enforcer = instance.Entry.Resolve<ServiceAgnosticCapabilityEnforcer>();
            var detected = await enforcer.Check(config, ct);
            await using var detectedScope = instance.Entry.BeginDetectedServiceScope(detected);
            var value = await call(detectedScope.Resolve<TService>());
            return new InstanceCallResult<T>(InstanceFound: true, Value: value);
        }
        catch (Exception e) when (OperationalFailureClassifier.Classify(e) is { } failure)
        {
            log.Warning(e, "Call to instance {Instance} failed", config.InstanceName);
            return new InstanceCallResult<T>(InstanceFound: true, Failure: failure);
        }
    }

    private IServiceConfiguration? Find(string instanceName) =>
        resources.Current.Configuration.Instances.FirstOrDefault(x =>
            x.InstanceName.Equals(instanceName, StringComparison.OrdinalIgnoreCase)
        );
}
