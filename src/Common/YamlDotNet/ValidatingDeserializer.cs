using System;
using System.ComponentModel.DataAnnotations;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Common.YamlDotNet
{
    internal class ValidatingDeserializer : INodeDeserializer
    {
        private readonly INodeDeserializer _nodeDeserializer;

        public ValidatingDeserializer(INodeDeserializer nodeDeserializer)
        {
            _nodeDeserializer = nodeDeserializer;
        }

        public bool Deserialize(IParser reader, Type expectedType,
            Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
        {
            if (!_nodeDeserializer.Deserialize(reader, expectedType, nestedObjectDeserializer, out value) ||
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
}
