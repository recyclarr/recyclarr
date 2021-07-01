using System;
using System.Linq;
using Fluxor;
using TrashLib.Config;

namespace Recyclarr.Code.Radarr.Fluxor
{
    internal record ActiveConfig<T>(T? Config) where T : IServiceConfiguration;

    internal class ActiveConfigFeature<T> : Feature<ActiveConfig<T>>
        where T : IServiceConfiguration
    {
        private readonly IConfigRepository<T> _configRepo;
        public ActiveConfigFeature(IConfigRepository<T> configRepo) => _configRepo = configRepo;
        public override string GetName() => nameof(ActiveConfig<T>);
        protected override ActiveConfig<T> GetInitialState() => new(_configRepo.Configs.FirstOrDefault());
    }

    internal static class Reducers
    {
        [ReducerMethod]
        public static ActiveConfig<T> SetActiveConfig<T>(ActiveConfig<T> state, ActiveConfig<T> action)
            where T : IServiceConfiguration => action;
    }

    // temporary until I know if Fluxor is the right tool for the job. Currently doesn't support:
    // - Awaitable/Synchronous Dispatch for Events (required for LocalStorage usage in an Event from OnAfterRenderAsync())
    // - Open generics for Action, State, Feature (DI probably fails?)
    // public interface IActiveConfig<T> where T : IServiceConfiguration
    // {
    //     T? Config { get; set; }
    //     event Action<T> ConfigChanged;
    // }
}
