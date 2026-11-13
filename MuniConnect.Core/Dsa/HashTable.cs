namespace MuniConnect.Core.Dsa;

/// <summary>Separate-chaining hash table. Lookups stay direct as the table grows.</summary>
public sealed class HashTable<TKey, TValue> where TKey : notnull
{
    private List<KeyValuePair<TKey, TValue>>[] _buckets;
    private int _count;
    private bool _resizing;
    private readonly IEqualityComparer<TKey> _comparer = EqualityComparer<TKey>.Default;

    public HashTable(int capacity = 16)
    {
        var size = capacity < 4 ? 4 : capacity;
        _buckets = new List<KeyValuePair<TKey, TValue>>[size];
        for (var i = 0; i < size; i++)
        {
            _buckets[i] = new List<KeyValuePair<TKey, TValue>>();
        }
    }

    public int Count => _count;
    public bool IsEmpty => _count == 0;

    public void Put(TKey key, TValue value)
    {
        var bucket = _buckets[Index(key)];
        for (var i = 0; i < bucket.Count; i++)
        {
            if (_comparer.Equals(bucket[i].Key, key))
            {
                bucket[i] = new KeyValuePair<TKey, TValue>(key, value);
                return;
            }
        }

        bucket.Add(new KeyValuePair<TKey, TValue>(key, value));
        _count++;
        if (!_resizing && _count > _buckets.Length * 3 / 4)
        {
            Resize();
        }
    }

    public bool TryGet(TKey key, out TValue value)
    {
        foreach (var entry in _buckets[Index(key)])
        {
            if (_comparer.Equals(entry.Key, key))
            {
                value = entry.Value;
                return true;
            }
        }

        value = default!;
        return false;
    }

    public bool ContainsKey(TKey key) => TryGet(key, out _);

    public bool Remove(TKey key, out TValue value)
    {
        var bucket = _buckets[Index(key)];
        for (var i = 0; i < bucket.Count; i++)
        {
            if (_comparer.Equals(bucket[i].Key, key))
            {
                value = bucket[i].Value;
                bucket.RemoveAt(i);
                _count--;
                return true;
            }
        }

        value = default!;
        return false;
    }

    public IReadOnlyList<TKey> Keys
    {
        get
        {
            var keys = new List<TKey>(_count);
            foreach (var entry in Entries)
            {
                keys.Add(entry.Key);
            }

            return keys;
        }
    }

    public IReadOnlyList<TValue> Values
    {
        get
        {
            var values = new List<TValue>(_count);
            foreach (var entry in Entries)
            {
                values.Add(entry.Value);
            }

            return values;
        }
    }

    public IReadOnlyList<KeyValuePair<TKey, TValue>> Entries
    {
        get
        {
            var all = new List<KeyValuePair<TKey, TValue>>(_count);
            foreach (var bucket in _buckets)
            {
                all.AddRange(bucket);
            }

            return all;
        }
    }

    public void Clear()
    {
        foreach (var bucket in _buckets)
        {
            bucket.Clear();
        }

        _count = 0;
    }

    private int Index(TKey key) => (_comparer.GetHashCode(key) & 0x7fffffff) % _buckets.Length;

    private void Resize()
    {
        var previous = _buckets;
        _buckets = new List<KeyValuePair<TKey, TValue>>[previous.Length * 2];
        for (var i = 0; i < _buckets.Length; i++)
        {
            _buckets[i] = new List<KeyValuePair<TKey, TValue>>();
        }

        _count = 0;
        _resizing = true;
        foreach (var bucket in previous)
        {
            foreach (var entry in bucket)
            {
                Put(entry.Key, entry.Value);
            }
        }

        _resizing = false;
    }
}
