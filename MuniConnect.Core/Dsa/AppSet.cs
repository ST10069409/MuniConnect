namespace MuniConnect.Core.Dsa;

/// <summary>Unique values, backed by the hash table so membership tests stay direct.</summary>
public sealed class AppSet<T> where T : notnull
{
    private readonly HashTable<T, bool> _table = new();

    public int Count => _table.Count;
    public bool IsEmpty => _table.IsEmpty;

    public bool Add(T value)
    {
        if (_table.ContainsKey(value))
        {
            return false;
        }

        _table.Put(value, true);
        return true;
    }

    public void AddAll(IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            Add(value);
        }
    }

    public bool Contains(T value) => _table.ContainsKey(value);

    public bool Remove(T value) => _table.Remove(value, out _);

    public List<T> ToList() => _table.Keys.ToList();

    public AppSet<T> Intersection(AppSet<T> other)
    {
        var result = new AppSet<T>();
        foreach (var value in ToList())
        {
            if (other.Contains(value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    public void Clear() => _table.Clear();
}
