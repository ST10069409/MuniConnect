namespace MuniConnect.Core.Dsa;

/// <summary>AVL tree. Heights of the two children differ by at most one after every insert.</summary>
public sealed class AvlTree<TKey, TValue>
{
    private readonly Comparison<TKey> _compare;
    private AvlNode<TKey, TValue>? _root;
    private int _size;

    public AvlTree(Comparison<TKey> compare) => _compare = compare;

    public int Size => _size;
    public int Height => NodeHeight(_root);

    public void Clear()
    {
        _root = null;
        _size = 0;
    }

    public void Insert(TKey key, TValue value)
    {
        var added = false;
        _root = Insert(_root, key, value, ref added);
        if (added)
        {
            _size++;
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

    /// <summary>Returns null when every node differs in child height by at most one.</summary>
    public string? CheckBalance()
    {
        string? Walk(AvlNode<TKey, TValue>? node)
        {
            if (node == null)
            {
                return null;
            }

            var balance = NodeHeight(node.Left) - NodeHeight(node.Right);
            if (Math.Abs(balance) > 1)
            {
                return $"Balance {balance} at {node.Key}";
            }

            var expected = 1 + Math.Max(NodeHeight(node.Left), NodeHeight(node.Right));
            if (node.Height != expected)
            {
                return $"Stale height at {node.Key}";
            }

            return Walk(node.Left) ?? Walk(node.Right);
        }

        return Walk(_root);
    }

    public List<KeyValuePair<TKey, TValue>> InOrder()
    {
        var entries = new List<KeyValuePair<TKey, TValue>>();
        void Walk(AvlNode<TKey, TValue>? node)
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

    private AvlNode<TKey, TValue> Insert(AvlNode<TKey, TValue>? node, TKey key, TValue value, ref bool added)
    {
        if (node == null)
        {
            added = true;
            return new AvlNode<TKey, TValue>(key, value);
        }

        var cmp = _compare(key, node.Key);
        if (cmp < 0)
        {
            node.Left = Insert(node.Left, key, value, ref added);
        }
        else if (cmp > 0)
        {
            node.Right = Insert(node.Right, key, value, ref added);
        }
        else
        {
            node.Value = value;
            return node;
        }

        Touch(node);
        var balance = NodeHeight(node.Left) - NodeHeight(node.Right);
        if (balance > 1 && node.Left != null && _compare(key, node.Left.Key) < 0)
        {
            return RotateRight(node);
        }

        if (balance < -1 && node.Right != null && _compare(key, node.Right.Key) > 0)
        {
            return RotateLeft(node);
        }

        if (balance > 1 && node.Left != null && _compare(key, node.Left.Key) > 0)
        {
            node.Left = RotateLeft(node.Left);
            return RotateRight(node);
        }

        if (balance < -1 && node.Right != null && _compare(key, node.Right.Key) < 0)
        {
            node.Right = RotateRight(node.Right);
            return RotateLeft(node);
        }

        return node;
    }

    private static AvlNode<TKey, TValue> RotateLeft(AvlNode<TKey, TValue> x)
    {
        var y = x.Right ?? throw new InvalidOperationException("Rotate left needs a right child");
        x.Right = y.Left;
        y.Left = x;
        Touch(x);
        Touch(y);
        return y;
    }

    private static AvlNode<TKey, TValue> RotateRight(AvlNode<TKey, TValue> y)
    {
        var x = y.Left ?? throw new InvalidOperationException("Rotate right needs a left child");
        y.Left = x.Right;
        x.Right = y;
        Touch(y);
        Touch(x);
        return x;
    }

    private static void Touch(AvlNode<TKey, TValue> node)
    {
        node.Height = 1 + Math.Max(NodeHeight(node.Left), NodeHeight(node.Right));
    }

    private static int NodeHeight(AvlNode<TKey, TValue>? node) => node?.Height ?? 0;

    private sealed class AvlNode<TNodeKey, TNodeValue>
    {
        public AvlNode(TNodeKey key, TNodeValue value)
        {
            Key = key;
            Value = value;
        }

        public TNodeKey Key { get; }
        public TNodeValue Value { get; set; }
        public int Height { get; set; } = 1;
        public AvlNode<TNodeKey, TNodeValue>? Left { get; set; }
        public AvlNode<TNodeKey, TNodeValue>? Right { get; set; }
    }
}
