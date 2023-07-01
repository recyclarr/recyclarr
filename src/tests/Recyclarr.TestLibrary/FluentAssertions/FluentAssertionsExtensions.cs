using FluentAssertions.Collections;
using FluentAssertions.Execution;

namespace Recyclarr.TestLibrary.FluentAssertions;

public static class FluentAssertionsExtensions
{
    public static AndWhichConstraint<TAssertions, string> ContainRegexMatch<TCollection, TAssertions>(
        this StringCollectionAssertions<TCollection, TAssertions> assert,
        string regexPattern,
        string because = "",
        params object[] becauseArgs
    )
        where TCollection : IEnumerable<string>
        where TAssertions : StringCollectionAssertions<TCollection, TAssertions>
    {
        bool ContainsRegexMatch()
        {
            return assert.Subject.Any(item =>
            {
                using var scope = new AssertionScope();
                item.Should().MatchRegex(regexPattern);
                return !scope.Discard().Any();
            });
        }

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(ContainsRegexMatch())
            .FailWith("Expected {context:collection} {0} to contain a regex match of {1}{reason}.", assert.Subject,
                regexPattern);

        var matched = assert.Subject.Where(item =>
        {
            using var scope = new AssertionScope();
            item.Should().MatchRegex(regexPattern);
            return !scope.Discard().Any();
        });

        return new AndWhichConstraint<TAssertions, string>((TAssertions) assert, matched);
    }

    public static AndWhichConstraint<TAssertions, string> NotContainRegexMatch<TCollection, TAssertions>(
        this StringCollectionAssertions<TCollection, TAssertions> assert,
        string regexPattern,
        string because = "",
        params object[] becauseArgs
    )
        where TCollection : IEnumerable<string>
        where TAssertions : StringCollectionAssertions<TCollection, TAssertions>
    {
        bool NotContainsRegexMatch()
        {
            return assert.Subject.Any(item =>
            {
                using var scope = new AssertionScope();
                item.Should().NotMatchRegex(regexPattern);
                return !scope.Discard().Any();
            });
        }

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(NotContainsRegexMatch())
            .FailWith("Expected {context:collection} {0} to not contain a regex match of {1}{reason}.", assert.Subject,
                regexPattern);

        var matched = assert.Subject.Where(item =>
        {
            using var scope = new AssertionScope();
            item.Should().NotMatchRegex(regexPattern);
            return !scope.Discard().Any();
        });

        return new AndWhichConstraint<TAssertions, string>((TAssertions) assert, matched);
    }
}
