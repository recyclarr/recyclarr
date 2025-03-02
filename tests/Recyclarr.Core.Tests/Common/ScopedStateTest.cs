using Recyclarr.Common;

namespace Recyclarr.Core.Tests.Common;

internal sealed class ScopedStateTest
{
    [Test]
    public void AccessValue_MultipleScopes_ScopeValuesReturned()
    {
        var state = new ScopedState<int>(50);
        state.PushValue(100, 0);
        state.PushValue(150, 1);

        state.StackSize.Should().Be(2);
        state.ActiveScope.Should().Be(1);
        state.Value.Should().Be(150);

        state.Reset(1).Should().BeTrue();

        state.StackSize.Should().Be(1);
        state.ActiveScope.Should().Be(0);
        state.Value.Should().Be(100);

        state.Reset(0).Should().BeTrue();

        state.StackSize.Should().Be(0);
        state.ActiveScope.Should().BeNull();
        state.Value.Should().Be(50);
    }

    [Test]
    public void AccessValue_NextBlockScope_ReturnValueUntilSecondSession()
    {
        var state = new ScopedState<int>(50);
        state.PushValue(100, 0);

        state.ActiveScope.Should().Be(0);
        state.Value.Should().Be(100);

        state.Reset(0).Should().BeTrue();

        state.ActiveScope.Should().BeNull();
        state.Value.Should().Be(50);
    }

    [Test]
    public void AccessValue_NoScope_ReturnDefaultValue()
    {
        var state = new ScopedState<int>(50);
        state.ActiveScope.Should().BeNull();
        state.Value.Should().Be(50);
    }

    [Test]
    public void AccessValue_WholeSectionScope_ReturnValueAcrossMultipleResets()
    {
        var state = new ScopedState<int>(50);
        state.PushValue(100, 1);

        state.ActiveScope.Should().Be(1);
        state.Value.Should().Be(100);

        state.Reset(2).Should().BeFalse();

        state.ActiveScope.Should().Be(1);
        state.Value.Should().Be(100);
    }

    [Test]
    public void Reset_UsingGreatestScopeWithTwoScopes_ShouldRemoveAllScope()
    {
        var state = new ScopedState<int>(50);
        state.PushValue(100, 1);
        state.PushValue(150, 0);
        state.Reset(1).Should().BeTrue();

        state.ActiveScope.Should().BeNull();
        state.Value.Should().Be(50);
    }

    [Test]
    public void Reset_UsingLesserScopeWithTwoScopes_ShouldRemoveTopScope()
    {
        var state = new ScopedState<int>(50);
        state.PushValue(100, 0);
        state.PushValue(150, 1);
        state.Reset(1).Should().BeTrue();

        state.ActiveScope.Should().Be(0);
        state.Value.Should().Be(100);
    }

    [Test]
    public void Reset_WithLesserScope_ShouldDoNothing()
    {
        var state = new ScopedState<int>(50);
        state.PushValue(100, 1);
        state.Reset(2).Should().BeFalse();

        state.ActiveScope.Should().Be(1);
        state.Value.Should().Be(100);
    }

    [Test]
    public void Reset_WithScope_ShouldReset()
    {
        var state = new ScopedState<int>(50);
        state.PushValue(100, 1);
        state.Reset(1).Should().BeTrue();

        state.ActiveScope.Should().BeNull();
        state.Value.Should().Be(50);
    }
}
