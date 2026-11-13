using System.Text.RegularExpressions;
using MuniConnect.Core.Dsa;
using MuniConnect.Core.Models;

namespace MuniConnect.Core.Services;

public sealed class IssueFacts
{
    public IssueFacts(IssueCategory category, string location, string description)
    {
        Category = category;
        Location = location;
        Description = description;
    }

    public IssueCategory Category { get; }
    public string Location { get; }
    public string Description { get; }

    public bool ContainsAny(IReadOnlyList<string> words)
    {
        var raw = $" {Location.ToLowerInvariant()} {Description.ToLowerInvariant()} ";
        var text = Regex.Replace(raw, "[^a-z0-9]+", " ");
        return words.Any(word => text.Contains($" {word} ", StringComparison.Ordinal));
    }
}

/// <summary>Walks a binary decision tree to score how quickly a report should be handled.</summary>
public sealed class UrgencyScorer
{
    private static readonly string[] Hazards =
    [
        "leak", "sewage", "outage", "blackout", "wire", "sinkhole", "flood",
        "gas", "fire", "collapse", "injury", "exposed", "overflow",
    ];

    private static readonly string[] Places =
    [
        "school", "hospital", "clinic", "market", "station", "library", "hall",
    ];

    private readonly BinaryTree<Step> _root;

    public UrgencyScorer()
    {
        var critical = new BinaryTree<Step>(Step.Leaf("Critical", 5));
        var high = new BinaryTree<Step>(Step.Leaf("High", 4));
        var standardRoad = new BinaryTree<Step>(Step.Leaf("Standard", 3));
        var standardPlace = new BinaryTree<Step>(Step.Leaf("Standard", 3));
        var routine = new BinaryTree<Step>(Step.Leaf("Routine", 2));
        var publicPlace = new BinaryTree<Step>(
            Step.Branch("Public place?", facts => facts.ContainsAny(Places)),
            standardPlace,
            routine);
        var road = new BinaryTree<Step>(
            Step.Branch("Road works?", facts => facts.Category == IssueCategory.roadWorks),
            standardRoad,
            publicPlace);
        var criticalCategory = new BinaryTree<Step>(
            Step.Branch(
                "Utilities or sanitary?",
                facts => facts.Category is IssueCategory.utilities or IssueCategory.sanitary),
            critical,
            high);
        _root = new BinaryTree<Step>(
            Step.Branch("Hazard words?", facts => facts.ContainsAny(Hazards)),
            criticalCategory,
            road);
    }

    public int Score(IssueFacts facts) => Walk(_root, facts);

    public IReadOnlyList<string> Labels => _root.Preorder().Select(step => step.Label).ToList();

    private static int Walk(BinaryTree<Step> node, IssueFacts facts)
    {
        var step = node.Value;
        if (step.Score != null)
        {
            return step.Score.Value;
        }

        var next = step.Test!(facts) ? node.Left : node.Right;
        return Walk(next!, facts);
    }

    private sealed class Step
    {
        private Step(string label, Func<IssueFacts, bool>? test, int? score)
        {
            Label = label;
            Test = test;
            Score = score;
        }

        public string Label { get; }
        public Func<IssueFacts, bool>? Test { get; }
        public int? Score { get; }

        public static Step Branch(string label, Func<IssueFacts, bool> test) => new(label, test, null);

        public static Step Leaf(string label, int score) => new(label, null, score);
    }
}
