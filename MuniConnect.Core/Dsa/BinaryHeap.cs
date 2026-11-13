namespace MuniConnect.Core.Dsa;

/// <summary>
/// Binary heap. <paramref name="compare"/> returns a positive number when the first item should sit above the second.
/// </summary>
public sealed class BinaryHeap<T>
{
    private readonly List<T> _items = new();
    private readonly Comparison<T> _compare;

    public BinaryHeap(Comparison<T> compare) => _compare = compare;

    public int Count => _items.Count;
    public bool IsEmpty => _items.Count == 0;
    public T? Peek => _items.Count == 0 ? default : _items[0];

    public void Add(T value)
    {
        _items.Add(value);
        SiftUp(_items.Count - 1);
    }

    public T RemoveFirst()
    {
        if (_items.Count == 0)
        {
            throw new InvalidOperationException("Heap is empty");
        }

        var top = _items[0];
        var last = _items[^1];
        _items.RemoveAt(_items.Count - 1);
        if (_items.Count > 0)
        {
            _items[0] = last;
            SiftDown(0);
        }

        return top;
    }

    private void SiftUp(int index)
    {
        var i = index;
        while (i > 0)
        {
            var parent = (i - 1) / 2;
            if (_compare(_items[i], _items[parent]) <= 0)
            {
                break;
            }

            Swap(i, parent);
            i = parent;
        }
    }

    private void SiftDown(int index)
    {
        var i = index;
        while (true)
        {
            var left = i * 2 + 1;
            var right = left + 1;
            var best = i;
            if (left < _items.Count && _compare(_items[left], _items[best]) > 0)
            {
                best = left;
            }

            if (right < _items.Count && _compare(_items[right], _items[best]) > 0)
            {
                best = right;
            }

            if (best == i)
            {
                break;
            }

            Swap(i, best);
            i = best;
        }
    }

    private void Swap(int a, int b)
    {
        (_items[a], _items[b]) = (_items[b], _items[a]);
    }
}

/// <summary>Priority queue. The highest-priority item leaves first. Storage is a binary heap.</summary>
public sealed class AppPriorityQueue<T>
{
    private readonly BinaryHeap<T> _heap;

    public AppPriorityQueue(Comparison<T> compare) => _heap = new BinaryHeap<T>(compare);

    public int Count => _heap.Count;
    public bool IsEmpty => _heap.IsEmpty;
    public T? Peek => _heap.Peek;

    public void Add(T value) => _heap.Add(value);

    public T RemoveFirst() => _heap.RemoveFirst();
}
