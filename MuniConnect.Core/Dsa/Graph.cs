namespace MuniConnect.Core.Dsa;

/// <summary>
/// Undirected weighted graph with breadth-first search, depth-first search,
/// and Kruskal's minimum spanning tree.
/// </summary>
public sealed class Graph<T> where T : notnull
{
    private readonly HashTable<T, List<WeightedEdge<T>>> _adj = new();

    public int NodeCount => _adj.Count;

    public int UndirectedEdgeCount
    {
        get
        {
            var count = 0;
            foreach (var node in Nodes)
            {
                count += _adj.TryGet(node, out var edges) ? edges.Count : 0;
            }

            return count / 2;
        }
    }

    public IReadOnlyList<T> Nodes => _adj.Keys;

    public bool HasNode(T node) => _adj.ContainsKey(node);

    public void AddNode(T node)
    {
        if (!_adj.ContainsKey(node))
        {
            _adj.Put(node, new List<WeightedEdge<T>>());
        }
    }

    /// <summary>Adds an undirected edge, or increases the weight when the edge already exists.</summary>
    public void Link(T a, T b, double amount)
    {
        if (EqualityComparer<T>.Default.Equals(a, b))
        {
            return;
        }

        AddNode(a);
        AddNode(b);
        Add(a, b, amount);
        Add(b, a, amount);
    }

    public List<T> NeighborsOf(T node)
    {
        if (!_adj.TryGet(node, out var edges))
        {
            return new List<T>();
        }

        return edges.Select(edge => edge.To).ToList();
    }

    /// <summary>Visit order of a breadth-first search, nearest nodes first.</summary>
    public List<T> BreadthFirst(T start)
    {
        var distance = BreadthFirstDistances(start);
        var order = distance.Keys.ToList();
        order.Sort((a, b) =>
        {
            distance.TryGet(a, out var da);
            distance.TryGet(b, out var db);
            var byDistance = da.CompareTo(db);
            if (byDistance != 0)
            {
                return byDistance;
            }

            return string.Compare(a?.ToString(), b?.ToString(), StringComparison.Ordinal);
        });
        return order;
    }

    public HashTable<T, int> BreadthFirstDistances(T start)
    {
        var distance = new HashTable<T, int>();
        if (!_adj.ContainsKey(start))
        {
            return distance;
        }

        var queue = new AppQueue<T>();
        distance.Put(start, 0);
        queue.Enqueue(start);
        while (!queue.IsEmpty)
        {
            var node = queue.Dequeue();
            distance.TryGet(node, out var here);
            if (!_adj.TryGet(node, out var edges))
            {
                continue;
            }

            foreach (var edge in edges)
            {
                if (distance.ContainsKey(edge.To))
                {
                    continue;
                }

                distance.Put(edge.To, here + 1);
                queue.Enqueue(edge.To);
            }
        }

        return distance;
    }

    public List<T> DepthFirst(T start)
    {
        var seen = new AppSet<T>();
        var order = new List<T>();
        void Visit(T node)
        {
            if (!seen.Add(node))
            {
                return;
            }

            order.Add(node);
            if (!_adj.TryGet(node, out var edges))
            {
                return;
            }

            foreach (var edge in edges)
            {
                Visit(edge.To);
            }
        }

        if (_adj.ContainsKey(start))
        {
            Visit(start);
        }

        return order;
    }

    public List<WeightedEdge<T>> MinimumSpanningTree()
    {
        var nodeList = Nodes.ToList();
        var index = new HashTable<T, int>();
        for (var i = 0; i < nodeList.Count; i++)
        {
            index.Put(nodeList[i], i);
        }

        var edges = new List<WeightedEdge<T>>();
        var seen = new AppSet<string>();
        foreach (var node in nodeList)
        {
            if (!_adj.TryGet(node, out var adjacent))
            {
                continue;
            }

            foreach (var edge in adjacent)
            {
                index.TryGet(edge.From, out var from);
                index.TryGet(edge.To, out var to);
                var key = from < to ? $"{from}:{to}" : $"{to}:{from}";
                if (seen.Add(key))
                {
                    edges.Add(edge);
                }
            }
        }

        edges.Sort((a, b) => a.Weight.CompareTo(b.Weight));
        var sets = new UnionFind(nodeList.Count);
        var tree = new List<WeightedEdge<T>>();
        foreach (var edge in edges)
        {
            index.TryGet(edge.From, out var from);
            index.TryGet(edge.To, out var to);
            if (sets.Union(from, to))
            {
                tree.Add(edge);
            }
        }

        return tree;
    }

    private void Add(T from, T to, double amount)
    {
        if (!_adj.TryGet(from, out var edges))
        {
            return;
        }

        for (var i = 0; i < edges.Count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(edges[i].To, to))
            {
                edges[i] = new WeightedEdge<T>(from, to, edges[i].Weight + amount);
                return;
            }
        }

        edges.Add(new WeightedEdge<T>(from, to, amount));
    }

    private sealed class UnionFind
    {
        private readonly int[] _parent;
        private readonly int[] _rank;

        public UnionFind(int size)
        {
            _parent = new int[size];
            _rank = new int[size];
            for (var i = 0; i < size; i++)
            {
                _parent[i] = i;
            }
        }

        public int Find(int value)
        {
            if (_parent[value] != value)
            {
                _parent[value] = Find(_parent[value]);
            }

            return _parent[value];
        }

        public bool Union(int a, int b)
        {
            var rootA = Find(a);
            var rootB = Find(b);
            if (rootA == rootB)
            {
                return false;
            }

            if (_rank[rootA] < _rank[rootB])
            {
                _parent[rootA] = rootB;
            }
            else if (_rank[rootA] > _rank[rootB])
            {
                _parent[rootB] = rootA;
            }
            else
            {
                _parent[rootB] = rootA;
                _rank[rootA]++;
            }

            return true;
        }
    }
}

public sealed class WeightedEdge<T>
{
    public WeightedEdge(T from, T to, double weight)
    {
        From = from;
        To = to;
        Weight = weight;
    }

    public T From { get; }
    public T To { get; }
    public double Weight { get; }
}
