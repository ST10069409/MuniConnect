namespace MuniConnect.Core.Dsa;

/// <summary>Last-in, first-out stack.</summary>
public sealed class AppStack<T>
{
    private readonly List<T> _items = new();

    public int Count => _items.Count;
    public bool IsEmpty => _items.Count == 0;

    public void Push(T value) => _items.Add(value);

    public T Peek
    {
        get
        {
            if (_items.Count == 0)
            {
                throw new InvalidOperationException("Stack is empty");
            }

            return _items[^1];
        }
    }

    public T Pop()
    {
        if (_items.Count == 0)
        {
            throw new InvalidOperationException("Stack is empty");
        }

        var value = _items[^1];
        _items.RemoveAt(_items.Count - 1);
        return value;
    }

    public void RemoveOldest()
    {
        if (_items.Count > 0)
        {
            _items.RemoveAt(0);
        }
    }

    public List<T> ToListNewestFirst()
    {
        var copy = _items.ToList();
        copy.Reverse();
        return copy;
    }

    public void Clear() => _items.Clear();
}
