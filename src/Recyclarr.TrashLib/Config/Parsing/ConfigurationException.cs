using System.Runtime.Serialization;
using System.Text;
using FluentValidation.Results;

namespace Recyclarr.TrashLib.Config.Parsing;

[Serializable]
public class ConfigurationException : Exception
{
    protected ConfigurationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    private ConfigurationException(string propertyName, Type deserializableType, IEnumerable<string> messages)
    {
        PropertyName = propertyName;
        DeserializableType = deserializableType;
        ErrorMessages = messages.ToList();
    }

    public ConfigurationException(string propertyName, Type deserializableType, string message)
        : this(propertyName, deserializableType, new[] {message})
    {
    }

    public ConfigurationException(string propertyName, Type deserializableType,
        IEnumerable<ValidationFailure> validationFailures)
        : this(propertyName, deserializableType, validationFailures.Select(e => e.ToString()))
    {
    }

    public IReadOnlyCollection<string> ErrorMessages { get; } = new List<string>();
    public string PropertyName { get; } = "";
    public Type DeserializableType { get; } = default!;

    public override string Message => BuildMessage();

    private string BuildMessage()
    {
        const string delim = "\n   - ";
        var builder = new StringBuilder(
            $"An exception occurred while deserializing type '{DeserializableType}' for YML property '{PropertyName}'");

        // ReSharper disable once InvertIf
        if (ErrorMessages.Count > 0)
        {
            builder.Append(":" + delim);
            builder.Append(string.Join(delim, ErrorMessages));
        }

        return builder.ToString();
    }
}
