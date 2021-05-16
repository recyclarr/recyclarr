using System;

namespace Trash.Config
{
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string propertyName, Type deserializableType, string msg)
            : base($"An exception occurred while deserializing type '{deserializableType}' " +
                   $"for YML property '{propertyName}': {msg}")
        {
            PropertyName = propertyName;
            DeserializableType = deserializableType;
        }

        public string PropertyName { get; }
        public Type DeserializableType { get; }
    }
}
