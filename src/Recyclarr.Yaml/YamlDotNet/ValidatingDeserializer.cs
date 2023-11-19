using System.ComponentModel.DataAnnotations;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Recyclarr.Yaml.YamlDotNet;

internal class ValidatingDeserializer(INodeDeserializer nodeDeserializer) : INodeDeserializer
{
    public bool Deserialize(
        IParser reader,
        Type expectedType,
        Func<IParser, Type, object?> nestedObjectDeserializer,
        out object? value)
    {
        if (!nodeDeserializer.Deserialize(reader, expectedType, nestedObjectDeserializer, out value) ||
            value == null)
        {
            return false;
        }

        var context = new ValidationContext(value, null, null);

        try
        {
            Validator.ValidateObject(value, context, true);
        }
        catch (ValidationException e)
        {
            if (reader.Current == null)
            {
                throw;
            }

            throw new YamlException(reader.Current.Start, reader.Current.End, e.Message);
        }

        return true;
    }
}
