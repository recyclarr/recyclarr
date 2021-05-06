using System;

namespace Trash.Config
{
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string propertyName, Type deserializableType)
        {
            PropertyName = propertyName;
            DeserializableType = deserializableType;
        }

        public string PropertyName { get; }
        public Type DeserializableType { get; }
    }
}
