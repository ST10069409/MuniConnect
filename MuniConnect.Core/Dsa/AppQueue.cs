namespace MuniConnect.Core.Dsa;

/// <summary>First-in, first-out queue. The head index keeps dequeue at the front cheap.</summary>
public sealed class AppQueue<T>
{
    private readonly List<T?> _items = new();
    private int _head;

    public int Count => _items.Count - _head;
    public bool IsEmpty => Count == 0;

    public void Enqueue(T value) => _items.Add(value);

    public T First
    {
        get
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Queue is empty");
            }

            return _items[_head]!;
        }
    }

    public T Dequeue()
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("Queue is empty");
        }

        var value = _items[_head]!;
        _items[_head] = default;
        _head++;
        if (_head > 32 && _head * 2 > _items.Count)
        {
            _items.RemoveRange(0, _head);
            _head = 0;
        }

        return value;
    }

    public List<T> ToList()
    {
        var list = new List<T>(Count);
        for (var i = _head; i < _items.Count; i++)
        {
            list.Add(_items[i]!);
        }

        return list;
    }

    public void Clear()
    {
        _items.Clear();
        _head = 0;
    }
}
