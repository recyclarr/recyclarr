using System;
using TrashLib.Config;

namespace Recyclarr.Code
{
    public class ConfigProvider : IConfigProvider
    {
        private IServiceConfiguration? _activeConfiguration;

        public IServiceConfiguration Active
        {
            get => _activeConfiguration ?? throw new NullReferenceException("Active configuration has not been set");
            set => _activeConfiguration = value;
        }
    }
}
