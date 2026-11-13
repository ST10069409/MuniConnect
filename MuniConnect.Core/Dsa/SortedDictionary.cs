namespace MuniConnect.Core.Dsa;

/// <summary>Dictionary whose keys stay in sorted order. The index is an AVL tree.</summary>
public sealed class AppSortedDictionary<TKey, TValue>
{
    private readonly Comparison<TKey> _compare;
    private readonly AvlTree<TKey, TValue> _tree;

    public AppSortedDictionary(Comparison<TKey> compare)
    {
        _compare = compare;
        _tree = new AvlTree<TKey, TValue>(compare);
    }

    public int Count => _tree.Size;
    public int Height => _tree.Height;

    public void Put(TKey key, TValue value) => _tree.Insert(key, value);

    public bool TryGet(TKey key, out TValue value) => _tree.TrySearch(key, out value);

    public IReadOnlyList<KeyValuePair<TKey, TValue>> Entries => _tree.InOrder();

    /// <summary>Entries whose keys compare greater than or equal to <paramref name="start"/>.</summary>
    public List<KeyValuePair<TKey, TValue>> From(TKey start)
    {
        var result = new List<KeyValuePair<TKey, TValue>>();
        foreach (var entry in _tree.InOrder())
        {
            if (_compare(entry.Key, start) >= 0)
            {
                result.Add(entry);
            }
        }

        return result;
    }

    public void Clear() => _tree.Clear();
}
