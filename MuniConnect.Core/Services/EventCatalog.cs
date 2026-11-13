using System.Text.RegularExpressions;
using MuniConnect.Core.Dsa;
using MuniConnect.Core.Models;

namespace MuniConnect.Core.Services;

public sealed class EventCatalog
{
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "the", "and", "for", "with", "from", "this", "that", "are", "was",
    };

    private readonly HashTable<string, LocalEvent> _byId = new();
    private readonly AppSortedDictionary<EventKey, LocalEvent> _byTime = new((a, b) => a.CompareTo(b));
    private readonly BinarySearchTree<string, string> _titles = new(string.CompareOrdinal);
    private readonly HashTable<string, AppSet<string>> _words = new();
    private readonly Graph<string> _tagGraph = new();
    private readonly Graph<string> _serviceGraph = new();
    private readonly HashTable<string, int> _frequencies = new();
    private readonly AppStack<string> _recent = new();
    private readonly List<string> _log = new();

    public EventCatalog()
    {
        foreach (var listing in HarbourfordEvents())
        {
            _byId.Put(listing.Id, listing);
            _byTime.Put(new EventKey(listing.Start, listing.Id), listing);
            _titles.Insert(listing.Title.ToLowerInvariant(), listing.Id);
            foreach (var word in Tokens(Haystack(listing)))
            {
                if (!_words.TryGet(word, out var bucket))
                {
                    bucket = new AppSet<string>();
                    _words.Put(word, bucket);
                }

                bucket.Add(listing.Id);
            }

            var tags = listing.Tags;
            for (var i = 0; i < tags.Count; i++)
            {
                _tagGraph.AddNode(tags[i]);
                for (var j = i + 1; j < tags.Count; j++)
                {
                    _tagGraph.Link(tags[i], tags[j], 1);
                }
            }
        }

        LinkServices();
    }

    public int EventCount => _byId.Count;
    public int IndexHeight => _byTime.Height;
    public int RecordedSearches => _log.Count;
    public IReadOnlyList<string> RecentSearches => _recent.ToListNewestFirst();
    public IReadOnlyList<string> SearchLog => _log.ToList();

    public IReadOnlyList<LocalEvent> Upcoming =>
        _byTime.Entries.Select(entry => entry.Value).ToList();

    public string? MostFrequentSearch
    {
        get
        {
            string? best = null;
            var count = 0;
            foreach (var entry in _frequencies.Entries)
            {
                if (entry.Value > count)
                {
                    best = entry.Key;
                    count = entry.Value;
                }
            }

            return count < 2 ? null : best;
        }
    }

    public void RememberSearch(string raw)
    {
        var query = raw.Trim().ToLowerInvariant();
        if (query.Length < 2)
        {
            return;
        }

        _frequencies.TryGet(query, out var seen);
        _frequencies.Put(query, seen + 1);
        if (_recent.IsEmpty || _recent.Peek != query)
        {
            _recent.Push(query);
        }

        while (_recent.Count > 8)
        {
            _recent.RemoveOldest();
        }

        _log.Add(query);
        while (_log.Count > 40)
        {
            _log.RemoveAt(0);
        }
    }

    public void RestoreSearches(IEnumerable<string> queries)
    {
        _frequencies.Clear();
        _recent.Clear();
        _log.Clear();
        foreach (var query in queries)
        {
            RememberSearch(query);
        }
    }

    public List<LocalEvent> ResultsFor(string raw)
    {
        var query = raw.Trim().ToLowerInvariant();
        if (query.Length == 0)
        {
            return Upcoming.ToList();
        }

        var matched = new HashTable<string, int>();
        void Bump(string id, int amount)
        {
            matched.TryGet(id, out var current);
            matched.Put(id, current + amount);
        }

        if (_titles.TrySearch(query, out var exactTitle))
        {
            Bump(exactTitle, 8);
        }

        foreach (var token in Tokens(query))
        {
            if (_words.TryGet(token, out var hits))
            {
                foreach (var id in hits.ToList())
                {
                    Bump(id, 3);
                }
            }

            if (token.Length < 3)
            {
                continue;
            }

            foreach (var key in _words.Keys)
            {
                if (key == token || !key.StartsWith(token, StringComparison.Ordinal))
                {
                    continue;
                }

                if (_words.TryGet(key, out var prefixHits))
                {
                    foreach (var id in prefixHits.ToList())
                    {
                        Bump(id, 1);
                    }
                }
            }
        }

        foreach (var listing in Upcoming)
        {
            if (Haystack(listing).Contains(query, StringComparison.Ordinal))
            {
                Bump(listing.Id, 2);
            }
        }

        var heap = new BinaryHeap<LocalEvent>((a, b) =>
        {
            matched.TryGet(a.Id, out var scoreA);
            matched.TryGet(b.Id, out var scoreB);
            var byScore = scoreA.CompareTo(scoreB);
            if (byScore != 0)
            {
                return byScore;
            }

            return b.Start.CompareTo(a.Start);
        });
        foreach (var id in matched.Keys)
        {
            if (_byId.TryGet(id, out var listing))
            {
                heap.Add(listing);
            }
        }

        var ranked = new List<LocalEvent>();
        while (!heap.IsEmpty)
        {
            ranked.Add(heap.RemoveFirst());
        }

        return ranked;
    }

    public List<LocalEvent> Suggestions()
    {
        var upcoming = Upcoming;
        if (_log.Count == 0)
        {
            return upcoming.Take(3).ToList();
        }

        var tokenCounts = new HashTable<string, int>();
        foreach (var query in _log)
        {
            foreach (var token in Tokens(query))
            {
                tokenCounts.TryGet(token, out var count);
                tokenCounts.Put(token, count + 1);
            }
        }

        var related = new AppSet<string>();
        foreach (var token in tokenCounts.Keys)
        {
            if (!_tagGraph.HasNode(token))
            {
                continue;
            }

            foreach (var tag in _tagGraph.BreadthFirst(token).Take(6))
            {
                related.Add(tag);
            }
        }

        var heap = new BinaryHeap<LocalEvent>((a, b) =>
        {
            var byScore = SuggestionScore(a, related, tokenCounts)
                .CompareTo(SuggestionScore(b, related, tokenCounts));
            if (byScore != 0)
            {
                return byScore;
            }

            return b.Start.CompareTo(a.Start);
        });
        foreach (var listing in upcoming)
        {
            if (SuggestionScore(listing, related, tokenCounts) > 0)
            {
                heap.Add(listing);
            }
        }

        var picked = new List<LocalEvent>();
        while (!heap.IsEmpty && picked.Count < 3)
        {
            picked.Add(heap.RemoveFirst());
        }

        return picked.Count == 0 ? upcoming.Take(3).ToList() : picked;
    }

    public List<LocalEvent> RelatedTo(IssueCategory category)
    {
        var areas = new AppSet<string>();
        areas.Add(category.Label());
        foreach (var neighbor in _serviceGraph.NeighborsOf(category.Label()))
        {
            areas.Add(neighbor);
        }

        return Upcoming.Where(listing => areas.Contains(listing.ServiceArea)).Take(3).ToList();
    }

    public List<LocalEvent> Trail()
    {
        var listings = Upcoming.ToList();
        if (listings.Count < 2)
        {
            return listings;
        }

        var tree = SpanningTree(listings);
        var ordered = new List<LocalEvent>();
        foreach (var id in tree.DepthFirst(listings[0].Id))
        {
            if (_byId.TryGet(id, out var listing))
            {
                ordered.Add(listing);
            }
        }

        return ordered;
    }

    public double TrailWeight
    {
        get
        {
            var listings = Upcoming.ToList();
            if (listings.Count < 2)
            {
                return 0;
            }

            var graph = CompleteWeek(listings);
            return graph.MinimumSpanningTree().Sum(edge => edge.Weight);
        }
    }

    public List<string> ServiceBreadthFirst() =>
        _serviceGraph.BreadthFirst(IssueCategory.roadWorks.Label());

    public List<string> ServiceDepthFirst() =>
        _serviceGraph.DepthFirst(IssueCategory.roadWorks.Label());

    private static int SuggestionScore(
        LocalEvent listing,
        AppSet<string> related,
        HashTable<string, int> tokenCounts)
    {
        var score = 0;
        foreach (var tag in listing.Tags)
        {
            if (related.Contains(tag))
            {
                score += 3;
            }

            if (tokenCounts.TryGet(tag, out var tagCount))
            {
                score += tagCount;
            }
        }

        foreach (var token in Tokens(listing.Title))
        {
            if (tokenCounts.TryGet(token, out var titleCount))
            {
                score += titleCount;
            }
        }

        return score;
    }

    private static Graph<string> SpanningTree(IReadOnlyList<LocalEvent> listings)
    {
        var graph = CompleteWeek(listings);
        var tree = new Graph<string>();
        foreach (var listing in listings)
        {
            tree.AddNode(listing.Id);
        }

        foreach (var edge in graph.MinimumSpanningTree())
        {
            tree.Link(edge.From, edge.To, edge.Weight);
        }

        return tree;
    }

    private static Graph<string> CompleteWeek(IReadOnlyList<LocalEvent> listings)
    {
        var graph = new Graph<string>();
        for (var i = 0; i < listings.Count; i++)
        {
            for (var j = i + 1; j < listings.Count; j++)
            {
                var hours = Math.Abs((int)listings[j].Start.Subtract(listings[i].Start).TotalHours);
                var penalty = listings[i].Category == listings[j].Category ? 0 : 6;
                graph.Link(listings[i].Id, listings[j].Id, hours + penalty);
            }
        }

        return graph;
    }

    private void LinkServices()
    {
        (IssueCategory, IssueCategory)[] pairs =
        [
            (IssueCategory.roadWorks, IssueCategory.utilities),
            (IssueCategory.roadWorks, IssueCategory.publicSafety),
            (IssueCategory.roadWorks, IssueCategory.parks),
            (IssueCategory.utilities, IssueCategory.sanitary),
            (IssueCategory.sanitary, IssueCategory.publicSafety),
            (IssueCategory.parks, IssueCategory.sanitary),
        ];
        foreach (var (left, right) in pairs)
        {
            _serviceGraph.Link(left.Label(), right.Label(), 1);
        }
    }

    private static List<string> Tokens(string raw)
    {
        return Regex.Split(raw.ToLowerInvariant(), "[^a-z0-9]+")
            .Where(word => word.Length >= 2 && !StopWords.Contains(word))
            .ToList();
    }

    private static string Haystack(LocalEvent listing) =>
        $"{listing.Title} {listing.Summary} {listing.Venue} {listing.Category} {listing.ServiceArea} {string.Join(' ', listing.Tags)}"
            .ToLowerInvariant();

    private static DateTime Soon(int daysAhead, int hour, int minute = 0)
    {
        var now = DateTime.Now;
        var when = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0).AddDays(daysAhead);
        if (when <= now)
        {
            when = when.AddDays(1);
        }

        return when;
    }

    private static List<LocalEvent> HarbourfordEvents() =>
    [
        new(
            "e-market",
            "Harbourford Saturday market",
            "Stalls, bread, and produce on the square from morning until two.",
            "Market Square",
            Soon(1, 9),
            "Community",
            IssueCategory.publicSafety.Label(),
            ListingKind.Event,
            ["market", "community"]),
        new(
            "e-water",
            "Water maintenance on Harbour Road",
            "A planned utilities shutdown. Fill bottles before 10 pm.",
            "Harbour Road",
            Soon(1, 22),
            "Utilities",
            IssueCategory.utilities.Label(),
            ListingKind.Announcement,
            ["water", "utilities"]),
        new(
            "e-drains",
            "Storm drain inspection",
            "Crews will open the grates along the canal path for an hour.",
            "Old Canal",
            Soon(2, 11),
            "Sanitary",
            IssueCategory.sanitary.Label(),
            ListingKind.Announcement,
            ["drains", "sanitary"]),
        new(
            "e-cleanup",
            "North Green cleanup",
            "Bring gloves. Bags and tools are provided by the parks team.",
            "North Green",
            Soon(2, 8),
            "Parks",
            IssueCategory.parks.Label(),
            ListingKind.Event,
            ["parks", "volunteer"]),
        new(
            "e-council",
            "Council open meeting",
            "Ward questions are taken after the service-request report.",
            "Town Hall",
            Soon(3, 18),
            "Council",
            IssueCategory.publicSafety.Label(),
            ListingKind.Announcement,
            ["council", "community"]),
        new(
            "e-recycling",
            "Quay Lane recycling drive",
            "Glass, paper, and small electronics. No builder rubble.",
            "Quay Lane",
            Soon(3, 9),
            "Sanitary",
            IssueCategory.sanitary.Label(),
            ListingKind.Event,
            ["recycling", "sanitary"]),
        new(
            "e-library",
            "Library story hour",
            "A free reading for children in the upstairs room.",
            "Harbourford Library",
            Soon(4, 16),
            "Community",
            IssueCategory.parks.Label(),
            ListingKind.Event,
            ["library", "community"]),
        new(
            "e-night",
            "Night food market",
            "Hot food and music on the square after work.",
            "Market Square",
            Soon(5, 17),
            "Community",
            IssueCategory.publicSafety.Label(),
            ListingKind.Event,
            ["market", "food"]),
        new(
            "e-football",
            "Youth football at North Green",
            "Under-14 friendly. Spectators can stand along the east path.",
            "North Green",
            Soon(6, 10),
            "Parks",
            IssueCategory.parks.Label(),
            ListingKind.Event,
            ["sport", "parks"]),
        new(
            "e-road",
            "Clinic Hill resurfacing",
            "One lane stays open. Expect a wait near the clinic entrance.",
            "Clinic Hill",
            Soon(0, 6, 30),
            "Road works",
            IssueCategory.roadWorks.Label(),
            ListingKind.Announcement,
            ["roads", "clinic"]),
    ];
}
