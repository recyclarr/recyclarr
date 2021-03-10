using System;

namespace Trash.Config
{
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string propertyName, Type type)
        {
            PropertyName = propertyName;
            Type = type;
        }

        public string PropertyName { get; }
        public Type Type { get; }
    }
}
