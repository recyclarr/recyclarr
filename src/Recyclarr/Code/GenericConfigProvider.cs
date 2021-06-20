using System;
using System.Collections.Generic;
using Recyclarr.Code.Settings.Persisters;
using TrashLib.Config;

namespace Recyclarr.Code
{
    internal sealed class GenericConfigProvider<T> : IConfigProvider<T>
        where T : IServiceConfiguration
    {
        private readonly IConfigProvider _generalServiceProvider;
        private T? _active;

        public GenericConfigProvider(IConfigPersister<T> configPersister, IConfigProvider generalServiceProvider)
        {
            Configs = configPersister.Load();
            _generalServiceProvider = generalServiceProvider;
        }

        public ICollection<T> Configs { get; }

        public T Active
        {
            get => _active ?? throw new NullReferenceException("Active configuration has not been set");
            set
            {
                _active = value;
                _generalServiceProvider.Active = value;
                OnActiveChanged();
            }
        }

        public event Action<T?>? ActiveChanged;

        private void OnActiveChanged()
        {
            ActiveChanged?.Invoke(_active);
        }
    }
}
