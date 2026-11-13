using MuniConnect.Core.Dsa;
using MuniConnect.Core.Models;
using MuniConnect.Core.Services;

namespace MuniConnect.Tests;

public class DsaTests
{
    [Fact]
    public void Hash_table_stores_overwrites_and_resizes()
    {
        var table = new HashTable<string, int>();
        for (var i = 0; i < 50; i++)
        {
            table.Put("k" + i, i);
        }

        table.Put("k3", 99);
        Assert.True(table.TryGet("k3", out var value));
        Assert.Equal(99, value);
        Assert.Equal(50, table.Count);
        Assert.True(table.Remove("k3", out _));
        Assert.False(table.ContainsKey("k3"));
    }

    [Fact]
    public void Set_keeps_unique_categories()
    {
        var set = new AppSet<string>();
        Assert.True(set.Add("Sanitary"));
        Assert.False(set.Add("Sanitary"));
        set.Add("Utilities");
        Assert.Equal(2, set.Count);
        Assert.True(set.Contains("Utilities"));
    }

    [Fact]
    public void Queue_is_first_in_first_out()
    {
        var queue = new AppQueue<string>();
        queue.Enqueue("received");
        queue.Enqueue("review");
        Assert.Equal("received", queue.Dequeue());
        Assert.Equal("review", queue.First);
    }

    [Fact]
    public void Stack_is_last_in_first_out()
    {
        var stack = new AppStack<string>();
        stack.Push("market");
        stack.Push("water");
        Assert.Equal("water", stack.Peek);
        Assert.Equal("water", stack.Pop());
        Assert.Equal(new[] { "market" }, stack.ToListNewestFirst());
    }

    [Fact]
    public void Heap_and_priority_queue_release_the_highest_item_first()
    {
        var heap = new BinaryHeap<int>((a, b) => a.CompareTo(b));
        foreach (var value in new[] { 3, 1, 4, 2 })
        {
            heap.Add(value);
        }

        Assert.Equal(new[] { 4, 3, 2, 1 }, new[]
        {
            heap.RemoveFirst(), heap.RemoveFirst(), heap.RemoveFirst(), heap.RemoveFirst(),
        });

        var queue = new AppPriorityQueue<string>((a, b) => a.Length.CompareTo(b.Length));
        queue.Add("road");
        queue.Add("a");
        queue.Add("utilities");
        Assert.Equal("utilities", queue.RemoveFirst());
    }

    [Fact]
    public void Binary_tree_walks_in_preorder_inorder_and_postorder()
    {
        var tree = new BinaryTree<int>(
            1,
            new BinaryTree<int>(2, new BinaryTree<int>(4)),
            new BinaryTree<int>(3));
        Assert.Equal(new[] { 1, 2, 4, 3 }, tree.Preorder());
        Assert.Equal(new[] { 4, 2, 1, 3 }, tree.Inorder());
        Assert.Equal(new[] { 4, 2, 3, 1 }, tree.Postorder());
    }

    [Fact]
    public void Binary_search_tree_finds_an_inserted_title()
    {
        var tree = new BinarySearchTree<string, string>(string.CompareOrdinal);
        tree.Insert("night food market", "e-night");
        tree.Insert("harbourford saturday market", "e-market");
        Assert.True(tree.TrySearch("harbourford saturday market", out var id));
        Assert.Equal("e-market", id);
        Assert.Equal(
            new[] { "harbourford saturday market", "night food market" },
            tree.InOrder().Select(entry => entry.Key).ToArray());
    }

    [Fact]
    public void Avl_tree_stays_balanced_and_sorted()
    {
        var tree = new AvlTree<int, int>((a, b) => a.CompareTo(b));
        for (var i = 1; i <= 40; i++)
        {
            tree.Insert(i, i);
        }

        Assert.True(tree.Height < 10);
        Assert.Null(tree.CheckBalance());
        Assert.Equal(Enumerable.Range(1, 40), tree.InOrder().Select(entry => entry.Key));
        tree.Insert(20, 99);
        Assert.True(tree.TrySearch(20, out var value));
        Assert.Equal(99, value);
        Assert.Equal(40, tree.Size);
    }

    [Fact]
    public void Sorted_dictionary_returns_keys_from_a_start_point()
    {
        var dictionary = new AppSortedDictionary<int, string>((a, b) => a.CompareTo(b));
        dictionary.Put(3, "c");
        dictionary.Put(1, "a");
        dictionary.Put(2, "b");
        Assert.Equal(new[] { "a", "b", "c" }, dictionary.Entries.Select(entry => entry.Value).ToArray());
        Assert.Equal(new[] { 2, 3 }, dictionary.From(2).Select(entry => entry.Key).ToArray());
    }

    [Fact]
    public void Red_black_tree_keeps_its_invariants()
    {
        var tree = new RedBlackTree<int, string>((a, b) => a.CompareTo(b));
        for (var i = 1; i <= 50; i++)
        {
            tree.Insert(i, "v" + i);
        }

        tree.Insert(10, "updated");
        Assert.Null(tree.CheckInvariants());
        Assert.Equal(50, tree.Size);
        Assert.True(tree.TrySearch(10, out var value));
        Assert.Equal("updated", value);
        var keys = tree.InOrder().Select(entry => entry.Key).ToList();
        Assert.Equal(1, keys[0]);
        Assert.Equal(50, keys[^1]);
    }

    [Fact]
    public void Graph_search_and_minimum_spanning_tree()
    {
        var graph = new Graph<string>();
        graph.Link("A", "B", 1);
        graph.Link("A", "C", 4);
        graph.Link("B", "C", 2);
        graph.Link("B", "D", 6);
        graph.Link("C", "D", 3);

        Assert.Equal(new[] { "A", "B", "C", "D" }, graph.BreadthFirst("A"));
        Assert.Equal(4, graph.DepthFirst("A").Distinct().Count());
        var tree = graph.MinimumSpanningTree();
        Assert.Equal(3, tree.Count);
        Assert.Equal(6, tree.Sum(edge => edge.Weight));
    }

    [Fact]
    public void Urgency_tree_scores_hazard_and_place()
    {
        var scorer = new UrgencyScorer();
        Assert.Contains("Hazard words?", scorer.Labels);
        Assert.Equal(5, scorer.Score(new IssueFacts(
            IssueCategory.utilities,
            "School gate",
            "There is a water leak outside the school gate")));
        Assert.Equal(3, scorer.Score(new IssueFacts(
            IssueCategory.roadWorks,
            "Harbour Road, outside the clinic",
            "Deep pothole on Harbour Road")));
        Assert.Equal(2, scorer.Score(new IssueFacts(
            IssueCategory.parks,
            "North Green",
            "The bench in North Green needs a repair")));
    }
}
