namespace MuniConnect.Core.Dsa;

/// <summary>Classic red-black tree. Insert keeps the black-height the same on every path.</summary>
public sealed class RedBlackTree<TKey, TValue>
{
    private readonly Comparison<TKey> _compare;
    private readonly RbNode<TKey, TValue> _nil;
    private RbNode<TKey, TValue> _root;
    private int _size;

    public RedBlackTree(Comparison<TKey> compare)
    {
        _compare = compare;
        _nil = RbNode<TKey, TValue>.Sentinel();
        _root = _nil;
    }

    public int Size => _size;

    public void Clear()
    {
        _root = _nil;
        _size = 0;
    }

    public void Insert(TKey key, TValue value)
    {
        var parent = _nil;
        var current = _root;
        while (!current.Nil)
        {
            parent = current;
            var cmp = _compare(key, current.Key);
            if (cmp == 0)
            {
                current.Value = value;
                return;
            }

            current = cmp < 0 ? current.Left : current.Right;
        }

        var node = new RbNode<TKey, TValue>(key, value, _nil);
        node.Parent = parent;
        if (parent.Nil)
        {
            _root = node;
        }
        else if (_compare(key, parent.Key) < 0)
        {
            parent.Left = node;
        }
        else
        {
            parent.Right = node;
        }

        _size++;
        InsertFixup(node);
    }

    public bool TrySearch(TKey key, out TValue value)
    {
        var current = _root;
        while (!current.Nil)
        {
            var cmp = _compare(key, current.Key);
            if (cmp == 0)
            {
                value = current.Value;
                return true;
            }

            current = cmp < 0 ? current.Left : current.Right;
        }

        value = default!;
        return false;
    }

    public List<KeyValuePair<TKey, TValue>> InOrder()
    {
        var entries = new List<KeyValuePair<TKey, TValue>>();
        void Walk(RbNode<TKey, TValue> node)
        {
            if (node.Nil)
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

    /// <summary>Returns null when the red-black invariants hold.</summary>
    public string? CheckInvariants()
    {
        if (_root.Color != RbColor.Black)
        {
            return "Root is red";
        }

        if (BlackHeight(_root) < 0)
        {
            return "Red parent has a red child, or black-heights differ";
        }

        var keys = InOrder().Select(entry => entry.Key).ToList();
        for (var i = 1; i < keys.Count; i++)
        {
            if (_compare(keys[i - 1], keys[i]) > 0)
            {
                return "In-order walk is not sorted";
            }
        }

        return null;
    }

    private void InsertFixup(RbNode<TKey, TValue> z)
    {
        while (z.Parent.Color == RbColor.Red)
        {
            if (ReferenceEquals(z.Parent, z.Parent.Parent.Left))
            {
                var uncle = z.Parent.Parent.Right;
                if (uncle.Color == RbColor.Red)
                {
                    z.Parent.Color = RbColor.Black;
                    uncle.Color = RbColor.Black;
                    z.Parent.Parent.Color = RbColor.Red;
                    z = z.Parent.Parent;
                }
                else
                {
                    if (ReferenceEquals(z, z.Parent.Right))
                    {
                        z = z.Parent;
                        RotateLeft(z);
                    }

                    z.Parent.Color = RbColor.Black;
                    z.Parent.Parent.Color = RbColor.Red;
                    RotateRight(z.Parent.Parent);
                }
            }
            else
            {
                var uncle = z.Parent.Parent.Left;
                if (uncle.Color == RbColor.Red)
                {
                    z.Parent.Color = RbColor.Black;
                    uncle.Color = RbColor.Black;
                    z.Parent.Parent.Color = RbColor.Red;
                    z = z.Parent.Parent;
                }
                else
                {
                    if (ReferenceEquals(z, z.Parent.Left))
                    {
                        z = z.Parent;
                        RotateRight(z);
                    }

                    z.Parent.Color = RbColor.Black;
                    z.Parent.Parent.Color = RbColor.Red;
                    RotateLeft(z.Parent.Parent);
                }
            }
        }

        _root.Color = RbColor.Black;
    }

    private void RotateLeft(RbNode<TKey, TValue> x)
    {
        var y = x.Right;
        x.Right = y.Left;
        if (!y.Left.Nil)
        {
            y.Left.Parent = x;
        }

        y.Parent = x.Parent;
        if (x.Parent.Nil)
        {
            _root = y;
        }
        else if (ReferenceEquals(x, x.Parent.Left))
        {
            x.Parent.Left = y;
        }
        else
        {
            x.Parent.Right = y;
        }

        y.Left = x;
        x.Parent = y;
    }

    private void RotateRight(RbNode<TKey, TValue> x)
    {
        var y = x.Left;
        x.Left = y.Right;
        if (!y.Right.Nil)
        {
            y.Right.Parent = x;
        }

        y.Parent = x.Parent;
        if (x.Parent.Nil)
        {
            _root = y;
        }
        else if (ReferenceEquals(x, x.Parent.Right))
        {
            x.Parent.Right = y;
        }
        else
        {
            x.Parent.Left = y;
        }

        y.Right = x;
        x.Parent = y;
    }

    private int BlackHeight(RbNode<TKey, TValue> node)
    {
        if (node.Nil)
        {
            return 1;
        }

        if (node.Color == RbColor.Red &&
            (node.Left.Color == RbColor.Red || node.Right.Color == RbColor.Red))
        {
            return -1;
        }

        var left = BlackHeight(node.Left);
        var right = BlackHeight(node.Right);
        if (left < 0 || right < 0 || left != right)
        {
            return -1;
        }

        return left + (node.Color == RbColor.Black ? 1 : 0);
    }

    private enum RbColor { Red, Black }

    private sealed class RbNode<TNodeKey, TNodeValue>
    {
        private RbNode()
        {
            Nil = true;
            Color = RbColor.Black;
        }

        public RbNode(TNodeKey key, TNodeValue value, RbNode<TNodeKey, TNodeValue> sentinel)
        {
            Key = key;
            Value = value;
            Color = RbColor.Red;
            Left = sentinel;
            Right = sentinel;
            Parent = sentinel;
        }

        public static RbNode<TNodeKey, TNodeValue> Sentinel()
        {
            var nil = new RbNode<TNodeKey, TNodeValue>();
            nil.Left = nil;
            nil.Right = nil;
            nil.Parent = nil;
            return nil;
        }

        public bool Nil { get; }
        public TNodeKey Key { get; } = default!;
        public TNodeValue Value { get; set; } = default!;
        public RbColor Color { get; set; }
        public RbNode<TNodeKey, TNodeValue> Left { get; set; } = null!;
        public RbNode<TNodeKey, TNodeValue> Right { get; set; } = null!;
        public RbNode<TNodeKey, TNodeValue> Parent { get; set; } = null!;
    }
}
