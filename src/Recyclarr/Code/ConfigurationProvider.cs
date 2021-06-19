using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Common.Extensions;
using Recyclarr.Code.Settings.Persisters;
using TrashLib.Config;
using TrashLib.Radarr.Config;

namespace Recyclarr.Code
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        public IServiceConfiguration ActiveConfiguration { get; set; }
    }

    public interface IConfigurationProvider<T>
        where T : IServiceConfiguration
    {
        public T Active { get; }
        public IReadOnlyCollection<T> Configs { get; }
    }

    internal sealed class RadarrConfigurationProvider : IConfigurationProvider<RadarrConfiguration>, IDisposable
    {
        public void Dispose()
        {
        }

        private RadarrConfiguration? _active;

        public RadarrConfigurationProvider(IRadarrConfigPersister configPersister)
        {
            Configs = configPersister.Load().AsReadOnly();
        }

        public IReadOnlyCollection<RadarrConfiguration> Configs { get; }

        public RadarrConfiguration Active
        {
            get => _active ?? throw new NullReferenceException("Active configuration has not been set");
            set
            {
                _active = value;
                ActiveChanged?.Invoke();
            }
        }

        public event Action? ActiveChanged;
    }
}
