namespace MuniConnect.Core.Dsa;

/// <summary>Unbalanced binary search tree. Keys to the left compare lower.</summary>
public sealed class BinarySearchTree<TKey, TValue>
{
    private readonly Comparison<TKey> _compare;
    private BstNode<TKey, TValue>? _root;
    private int _size;

    public BinarySearchTree(Comparison<TKey> compare) => _compare = compare;

    public int Size => _size;
    public int Height => NodeHeight(_root);

    public void Clear()
    {
        _root = null;
        _size = 0;
    }

    public void Insert(TKey key, TValue value)
    {
        if (_root == null)
        {
            _root = new BstNode<TKey, TValue>(key, value);
            _size++;
            return;
        }

        var node = _root;
        while (true)
        {
            var cmp = _compare(key, node.Key);
            if (cmp == 0)
            {
                node.Value = value;
                return;
            }

            if (cmp < 0)
            {
                if (node.Left == null)
                {
                    node.Left = new BstNode<TKey, TValue>(key, value);
                    _size++;
                    Refresh(_root);
                    return;
                }

                node = node.Left;
            }
            else
            {
                if (node.Right == null)
                {
                    node.Right = new BstNode<TKey, TValue>(key, value);
                    _size++;
                    Refresh(_root);
                    return;
                }

                node = node.Right;
            }
        }
    }

    public bool TrySearch(TKey key, out TValue value)
    {
        var node = _root;
        while (node != null)
        {
            var cmp = _compare(key, node.Key);
            if (cmp == 0)
            {
                value = node.Value;
                return true;
            }

            node = cmp < 0 ? node.Left : node.Right;
        }

        value = default!;
        return false;
    }

    public List<KeyValuePair<TKey, TValue>> InOrder()
    {
        var entries = new List<KeyValuePair<TKey, TValue>>();
        void Walk(BstNode<TKey, TValue>? node)
        {
            if (node == null)
            {
                return;
            }

            Walk(node.Left);
            entries.Add(new KeyValuePair<TKey, TValue>(node.Key, node.Value));
            Walk(node.Right);
        }

        Walk(_root);
        return entries;
    }

    private void Refresh(BstNode<TKey, TValue>? node)
    {
        if (node == null)
        {
            return;
        }

        Refresh(node.Left);
        Refresh(node.Right);
        node.Height = 1 + Math.Max(NodeHeight(node.Left), NodeHeight(node.Right));
    }

    private static int NodeHeight(BstNode<TKey, TValue>? node) => node?.Height ?? 0;

    private sealed class BstNode<TNodeKey, TNodeValue>
    {
        public BstNode(TNodeKey key, TNodeValue value)
        {
            Key = key;
            Value = value;
        }

        public TNodeKey Key { get; }
        public TNodeValue Value { get; set; }
        public int Height { get; set; } = 1;
        public BstNode<TNodeKey, TNodeValue>? Left { get; set; }
        public BstNode<TNodeKey, TNodeValue>? Right { get; set; }
    }
}
