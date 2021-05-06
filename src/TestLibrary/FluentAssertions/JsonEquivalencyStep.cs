using FluentAssertions.Equivalency;
using FluentAssertions.Json;
using Newtonsoft.Json.Linq;

namespace TestLibrary.FluentAssertions
{
    public class JsonEquivalencyStep : IEquivalencyStep
    {
        public bool CanHandle(IEquivalencyValidationContext context, IEquivalencyAssertionOptions config)
        {
            return context.Subject?.GetType().IsAssignableTo(typeof(JToken)) ?? false;
        }

        public bool Handle(IEquivalencyValidationContext context, IEquivalencyValidator parent,
            IEquivalencyAssertionOptions config)
        {
            ((JToken) context.Subject).Should()
                .BeEquivalentTo((JToken) context.Expectation, context.Because, context.BecauseArgs);
            return true;
        }
    }
}
