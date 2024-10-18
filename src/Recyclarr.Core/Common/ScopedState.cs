namespace Recyclarr.Common;

public class ScopedState<T>(T defaultValue = default!)
{
    private readonly Stack<Node> _scopeStack = new();

    public T Value => _scopeStack.Count > 0 ? _scopeStack.Peek().Value : defaultValue;

    public int? ActiveScope => _scopeStack.Count > 0 ? _scopeStack.Peek().Scope : null;

    public int StackSize => _scopeStack.Count;

    public void PushValue(T value, int scope)
    {
        if (_scopeStack.Count == 0 || _scopeStack.Peek().Scope < scope)
        {
            _scopeStack.Push(new Node(value, scope));
        }
        else if (_scopeStack.Peek().Scope == scope)
        {
            _scopeStack.Peek().Value = value;
        }
    }

    public bool Reset(int scope)
    {
        if (_scopeStack.Count == 0)
        {
            return false;
        }

        var prevCount = StackSize;
        while (_scopeStack.Count > 0 && _scopeStack.Peek().Scope >= scope)
        {
            _scopeStack.Pop();
        }

        return prevCount != StackSize;
    }

    private sealed class Node(T value, int scope)
    {
        public T Value { get; set; } = value;
        public int Scope { get; } = scope;
    }
}
