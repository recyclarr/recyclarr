using System;
using TrashLib.Config;

namespace Trash.Config
{
    internal class ConfigProvider : IConfigProvider
    {
        private IServiceConfiguration? _activeConfiguration;

        public IServiceConfiguration Active
        {
            get => _activeConfiguration ?? throw new NullReferenceException("Active configuration has not been set");
            set => _activeConfiguration = value;
        }
    }
}
