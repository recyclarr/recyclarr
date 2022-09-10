using FluentAssertions;
using FluentAssertions.Equivalency;
using FluentAssertions.Json;
using Newtonsoft.Json.Linq;

namespace TestLibrary.FluentAssertions;

public class JsonEquivalencyStep : IEquivalencyStep
{
    public EquivalencyResult Handle(Comparands comparands, IEquivalencyValidationContext context,
        IEquivalencyValidator nestedValidator)
    {
        var canHandle = comparands.Subject?.GetType().IsAssignableTo(typeof(JToken)) ?? false;
        if (!canHandle)
        {
            return EquivalencyResult.ContinueWithNext;
        }

        ((JToken) comparands.Subject!).Should().BeEquivalentTo(
            (JToken) comparands.Expectation,
            context.Reason.FormattedMessage,
            context.Reason.Arguments);

        return EquivalencyResult.AssertionCompleted;
    }
}
