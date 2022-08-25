namespace TrashLib.Services.Sonarr.ReleaseProfile;

public class ScopedState<T>
{
    private readonly T _defaultValue;
    private readonly Stack<Node> _scopeStack = new();

    public ScopedState(T defaultValue = default!)
    {
        _defaultValue = defaultValue;
    }

    public T Value => _scopeStack.Count > 0 ? _scopeStack.Peek().Value : _defaultValue;

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

    private class Node
    {
        public Node(T value, int scope)
        {
            Value = value;
            Scope = scope;
        }

        public T Value { get; set; }
        public int Scope { get; }
    }
}
