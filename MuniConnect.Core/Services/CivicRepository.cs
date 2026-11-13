using System.Text.Json;
using System.Text.Json.Serialization;
using MuniConnect.Core.Dsa;
using MuniConnect.Core.Models;

namespace MuniConnect.Core.Services;

public sealed class StructureSnapshot
{
    public int ReportCount { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();
    public int IntakeWaiting { get; init; }
    public int IntakeProcessed { get; init; }
    public int AvlHeight { get; init; }
    public int OpenCount { get; init; }
    public string? NextReference { get; init; }
    public string? NextSummary { get; init; }
    public int EventCount { get; init; }
    public int EventIndexHeight { get; init; }
    public int RecordedSearches { get; init; }
    public string? TopSearch { get; init; }
    public IReadOnlyList<string> RecentSearches { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> BreadthFirst { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DepthFirst { get; init; } = Array.Empty<string>();
    public int TrailStops { get; init; }
    public double TrailWeight { get; init; }
}

public sealed class CivicRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string? _storageDirectory;
    private readonly UrgencyScorer _scorer = new();
    private string? _directory;
    private string? _file;
    private List<IssueReport> _reports = new();
    private int _nextNumber = 1000;
    private int _messageSerial;

    private HashTable<string, IssueReport> _byId = new();
    private AppSet<string> _categories = new();
    private AvlTree<TimeKey, string> _byTime = new(CompareTime);
    private RedBlackTree<PriorityKey, string> _byPriority = new(ComparePriority);
    private AppQueue<string> _intake = new();

    public CivicRepository(string? storageDirectory = null, EventCatalog? events = null)
    {
        _storageDirectory = storageDirectory;
        Events = events ?? new EventCatalog();
    }

    public EventCatalog Events { get; }

    public void Init()
    {
        try
        {
            if (_storageDirectory != null)
            {
                _directory = _storageDirectory;
            }
            else
            {
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                _directory = Path.Combine(docs, "MuniConnect");
            }

            Directory.CreateDirectory(_directory);
            _file = Path.Combine(_directory, "muni_connect.json");
            if (File.Exists(_file))
            {
                Restore(File.ReadAllText(_file));
            }
            else
            {
                Seed();
                Save();
            }
        }
        catch
        {
            _directory = null;
            _file = null;
            if (_reports.Count == 0)
            {
                Seed();
            }
        }

        RebuildIndexes();
    }

    public int OpenCount => _reports.Count(report => report.Status.IsOpen());

    public int IntakeWaiting => _intake.Count;

    public int IntakeProcessed => _reports.Count(report => report.Status != RequestStatus.received);

    public IReadOnlyList<IssueCategory> Categories =>
        Enum.GetValues<IssueCategory>().Where(category => _categories.Contains(category.Label())).ToList();

    public IReadOnlyList<RequestStatus> StatusesPresent
    {
        get
        {
            var present = new AppSet<RequestStatus>();
            foreach (var report in _byId.Values)
            {
                present.Add(report.Status);
            }

            return Enum.GetValues<RequestStatus>().Where(present.Contains).ToList();
        }
    }

    public IReadOnlyList<string> RecentSearches => Events.RecentSearches;

    public IReadOnlyList<string> UrgencyChecks => _scorer.Labels;

    public string? MostFrequentSearch => Events.MostFrequentSearch;

    public bool TryGetReport(string id, out IssueReport report) => _byId.TryGet(id, out report);

    public List<IssueReport> Reports(RequestStatus? status = null)
    {
        var ordered = new List<IssueReport>();
        foreach (var entry in _byTime.InOrder())
        {
            if (!_byId.TryGet(entry.Value, out var report))
            {
                continue;
            }

            if (status != null && report.Status != status)
            {
                continue;
            }

            ordered.Add(report);
        }

        return ordered;
    }

    public List<string> RankedOpenIds() => _byPriority.InOrder().Select(entry => entry.Value).ToList();

    public int PreviewUrgency(string location, IssueCategory? category, string description)
    {
        if (category == null)
        {
            return 0;
        }

        return _scorer.Score(new IssueFacts(category.Value, location, description));
    }

    public IssueReport Submit(string location, IssueCategory category, string description, IReadOnlyList<string> imagePaths)
    {
        var now = DateTime.Now;
        var report = new IssueReport
        {
            Id = TakeId(),
            Location = location.Trim(),
            Category = category,
            Description = description.Trim(),
            ImagePaths = CopyImages(imagePaths),
            SubmittedAt = now,
            Status = RequestStatus.received,
            Urgency = _scorer.Score(new IssueFacts(category, location, description)),
            History = [new StatusMark { Status = RequestStatus.received, At = now }],
            Thread = new List<FeedbackMessage>(),
        };
        _reports.Add(report);
        ProcessIntake();
        Save();
        return report;
    }

    public void SendFeedback(string id, string message)
    {
        var body = message.Trim();
        if (body.Length == 0 || !_byId.TryGet(id, out var report))
        {
            return;
        }

        report.Thread.Add(new FeedbackMessage
        {
            Id = MessageId(),
            Body = body,
            At = DateTime.Now,
            FromResident = true,
        });
        if (report.Status == RequestStatus.resolved)
        {
            Reopen(report, body);
        }
        else
        {
            Advance(report, body);
        }

        RebuildIndexes();
        Save();
    }

    public IssueReport? CheckProgress(string id)
    {
        if (!_byId.TryGet(id, out var report) || !report.Status.IsOpen())
        {
            return report;
        }

        if (report.Status == RequestStatus.received)
        {
            ProcessIntake();
        }
        else
        {
            Advance(report, null);
            RebuildIndexes();
        }

        Save();
        _byId.TryGet(id, out report);
        return report;
    }

    public IssueReport? AdvanceHighestPriority()
    {
        var open = _reports.Where(report => report.Status.IsOpen()).ToList();
        if (open.Count == 0)
        {
            return null;
        }

        var queue = new AppPriorityQueue<IssueReport>(CompareReports);
        foreach (var report in open)
        {
            queue.Add(report);
        }

        var chosen = queue.RemoveFirst();
        if (chosen.Status == RequestStatus.received)
        {
            ProcessIntake();
        }
        else
        {
            Advance(chosen, null);
            RebuildIndexes();
        }

        Save();
        _byId.TryGet(chosen.Id, out var updated);
        return updated;
    }

    public void RememberSearch(string query)
    {
        Events.RememberSearch(query);
        Save();
    }

    public StructureSnapshot Snapshot()
    {
        var ranked = RankedOpenIds();
        IssueReport? next = null;
        if (ranked.Count > 0)
        {
            _byId.TryGet(ranked[0], out next);
        }

        var stops = Events.Trail();
        var categories = _categories.ToList();
        categories.Sort(StringComparer.Ordinal);
        return new StructureSnapshot
        {
            ReportCount = _byId.Count,
            Categories = categories,
            IntakeWaiting = _intake.Count,
            IntakeProcessed = IntakeProcessed,
            AvlHeight = _byTime.Height,
            OpenCount = _byPriority.Size,
            NextReference = next?.Id,
            NextSummary = next == null ? null : $"{next.UrgencyLabel} · {next.Category.Label()} · {next.Location}",
            EventCount = Events.EventCount,
            EventIndexHeight = Events.IndexHeight,
            RecordedSearches = Events.RecordedSearches,
            TopSearch = Events.MostFrequentSearch,
            RecentSearches = Events.RecentSearches,
            BreadthFirst = Events.ServiceBreadthFirst(),
            DepthFirst = Events.ServiceDepthFirst(),
            TrailStops = stops.Count,
            TrailWeight = Events.TrailWeight,
        };
    }

    private void ProcessIntake()
    {
        RebuildIndexes();
        while (!_intake.IsEmpty)
        {
            var id = _intake.Dequeue();
            if (!_byId.TryGet(id, out var report) || report.Status != RequestStatus.received)
            {
                continue;
            }

            Advance(report, null);
        }

        RebuildIndexes();
    }

    private void Advance(IssueReport report, string? residentNote)
    {
        var next = report.Status.Next();
        if (next == null)
        {
            return;
        }

        report.Status = next.Value;
        var now = DateTime.Now;
        report.History.Add(new StatusMark { Status = next.Value, At = now });
        report.Thread.Add(new FeedbackMessage
        {
            Id = MessageId(),
            Body = DeskCopy(report, residentNote),
            At = now,
            FromResident = false,
        });
    }

    private void Reopen(IssueReport report, string note)
    {
        report.Status = RequestStatus.inReview;
        var now = DateTime.Now;
        report.History.Add(new StatusMark { Status = RequestStatus.inReview, At = now });
        report.Thread.Add(new FeedbackMessage
        {
            Id = MessageId(),
            Body = $"We reopened {report.Id} because of your reply: \"{Snippet(note)}\". A reviewer will look at {report.Location} again.",
            At = now,
            FromResident = false,
        });
    }

    private static string DeskCopy(IssueReport report, string? note)
    {
        var quote = string.IsNullOrWhiteSpace(note) ? "" : $" We noted your message: \"{Snippet(note)}\".";
        return report.Status switch
        {
            RequestStatus.inReview =>
                $"Civic desk received {report.Id} for {report.Category.Label()} at {report.Location}. A reviewer is checking it.{quote}",
            RequestStatus.assigned =>
                $"A {report.Category.Label().ToLowerInvariant()} crew has been assigned to {report.Location}. Reference {report.Id}.{quote}",
            RequestStatus.inProgress =>
                $"Work is underway at {report.Location}. We will update {report.Id} when the job is finished.{quote}",
            RequestStatus.resolved =>
                $"{report.Id} is marked resolved. Reply if {report.Location} still needs attention.{quote}",
            _ => $"{report.Id} is waiting in the intake queue.",
        };
    }

    private static string Snippet(string note)
    {
        var trimmed = note.Trim();
        return trimmed.Length <= 80 ? trimmed : $"{trimmed[..77]}...";
    }

    private void RebuildIndexes()
    {
        _byId = new HashTable<string, IssueReport>();
        _categories = new AppSet<string>();
        foreach (var category in Enum.GetValues<IssueCategory>())
        {
            _categories.Add(category.Label());
        }

        _byTime = new AvlTree<TimeKey, string>(CompareTime);
        _byPriority = new RedBlackTree<PriorityKey, string>(ComparePriority);
        _intake = new AppQueue<string>();

        var waiting = new List<IssueReport>();
        foreach (var report in _reports)
        {
            _byId.Put(report.Id, report);
            _categories.Add(report.Category.Label());
            _byTime.Insert(new TimeKey(report.SubmittedAt, report.Id), report.Id);
            if (report.Status.IsOpen())
            {
                _byPriority.Insert(new PriorityKey(report.DeskPriority, report.Id), report.Id);
            }

            if (report.Status == RequestStatus.received)
            {
                waiting.Add(report);
            }
        }

        waiting.Sort((a, b) => a.SubmittedAt.CompareTo(b.SubmittedAt));
        foreach (var report in waiting)
        {
            _intake.Enqueue(report.Id);
        }
    }

    private void Seed()
    {
        var now = DateTime.Now;
        _nextNumber = 1003;
        _reports =
        [
            Sample(
                "HD-1000",
                "Quay Lane",
                IssueCategory.utilities,
                "There was a power outage on Quay Lane and the street lights stayed off.",
                now.AddDays(-8),
                RequestStatus.resolved,
                [
                    Mark(RequestStatus.received, now.AddDays(-8)),
                    Mark(RequestStatus.inReview, now.AddDays(-8).AddHours(1)),
                    Mark(RequestStatus.assigned, now.AddDays(-7)),
                    Mark(RequestStatus.inProgress, now.AddDays(-6)),
                    Mark(RequestStatus.resolved, now.AddDays(-5)),
                ],
                [
                    Desk("m1000a", "Civic desk received HD-1000 for Utilities at Quay Lane. A reviewer is checking it.", now.AddDays(-8).AddHours(1)),
                    Resident("m1000b", "The lights came back on this morning. Thank you.", now.AddDays(-5).AddHours(-2)),
                    Desk("m1000c", "HD-1000 is marked resolved. Reply if Quay Lane still needs attention. We noted your message: \"The lights came back on this morning. Thank you.\".", now.AddDays(-5)),
                ]),
            Sample(
                "HD-1001",
                "Harbour Road, outside the clinic",
                IssueCategory.roadWorks,
                "Deep pothole on Harbour Road outside the clinic. Cars are swerving into the opposite lane.",
                now.AddDays(-3).AddHours(-2),
                RequestStatus.inProgress,
                [
                    Mark(RequestStatus.received, now.AddDays(-3).AddHours(-2)),
                    Mark(RequestStatus.inReview, now.AddDays(-3).AddHours(-1)),
                    Mark(RequestStatus.assigned, now.AddDays(-2).AddHours(-3)),
                    Mark(RequestStatus.inProgress, now.AddDays(-1).AddHours(-5)),
                ],
                [
                    Desk("m1001a", "Civic desk received HD-1001 for Road works at Harbour Road, outside the clinic. A reviewer is checking it.", now.AddDays(-3).AddHours(-1)),
                    Resident("m1001b", "The hole got wider after the rain last night.", now.AddDays(-2).AddHours(-4)),
                    Desk("m1001c", "A road works crew has been assigned to Harbour Road, outside the clinic. Reference HD-1001. We noted your message: \"The hole got wider after the rain last night.\".", now.AddDays(-2).AddHours(-3)),
                    Desk("m1001d", "Work is underway at Harbour Road, outside the clinic. We will update HD-1001 when the job is finished.", now.AddDays(-1).AddHours(-5)),
                ]),
            Sample(
                "HD-1002",
                "Market Square",
                IssueCategory.sanitary,
                "Bins at Market Square are full and blocking the walkway beside the flower stall.",
                now.AddDays(-1).AddHours(-3),
                RequestStatus.inReview,
                [
                    Mark(RequestStatus.received, now.AddDays(-1).AddHours(-3)),
                    Mark(RequestStatus.inReview, now.AddHours(-20)),
                ],
                [
                    Desk("m1002a", "Civic desk received HD-1002 for Sanitary at Market Square. A reviewer is checking it.", now.AddHours(-20)),
                ]),
        ];
    }

    private IssueReport Sample(
        string id,
        string location,
        IssueCategory category,
        string description,
        DateTime submittedAt,
        RequestStatus status,
        List<StatusMark> history,
        List<FeedbackMessage> thread)
    {
        return new IssueReport
        {
            Id = id,
            Location = location,
            Category = category,
            Description = description,
            ImagePaths = new List<string>(),
            SubmittedAt = submittedAt,
            Status = status,
            Urgency = _scorer.Score(new IssueFacts(category, location, description)),
            History = history,
            Thread = thread,
        };
    }

    private static StatusMark Mark(RequestStatus status, DateTime at) => new() { Status = status, At = at };

    private static FeedbackMessage Desk(string id, string body, DateTime at) =>
        new() { Id = id, Body = body, At = at, FromResident = false };

    private static FeedbackMessage Resident(string id, string body, DateTime at) =>
        new() { Id = id, Body = body, At = at, FromResident = true };

    private string TakeId()
    {
        var id = $"HD-{_nextNumber:0000}";
        _nextNumber++;
        return id;
    }

    private string MessageId() => $"m{DateTime.UtcNow.Ticks}-{_messageSerial++}";

    private List<string> CopyImages(IReadOnlyList<string> sourcePaths)
    {
        if (_directory == null || sourcePaths.Count == 0)
        {
            return sourcePaths.ToList();
        }

        var folder = Path.Combine(_directory, "photos");
        Directory.CreateDirectory(folder);
        var stored = new List<string>();
        foreach (var sourcePath in sourcePaths)
        {
            if (!File.Exists(sourcePath))
            {
                continue;
            }

            var name = $"{DateTime.UtcNow.Ticks}-{Path.GetFileName(sourcePath)}";
            var dest = Path.Combine(folder, name);
            File.Copy(sourcePath, dest, overwrite: false);
            stored.Add(dest);
        }

        return stored;
    }

    private void Save()
    {
        if (_file == null)
        {
            return;
        }

        try
        {
            var payload = new StoreDocument
            {
                NextNumber = _nextNumber,
                Searches = Events.SearchLog.ToList(),
                Reports = _reports,
            };
            File.WriteAllText(_file, JsonSerializer.Serialize(payload, JsonOptions));
        }
        catch
        {
            // A failed save leaves the in-memory desk usable for this session.
        }
    }

    private void Restore(string raw)
    {
        StoreDocument? decoded;
        try
        {
            decoded = JsonSerializer.Deserialize<StoreDocument>(raw, JsonOptions);
        }
        catch
        {
            Seed();
            return;
        }

        if (decoded == null)
        {
            Seed();
            return;
        }

        _nextNumber = decoded.NextNumber == 0 ? 1003 : decoded.NextNumber;
        _reports = decoded.Reports ?? new List<IssueReport>();
        if (_reports.Count == 0)
        {
            Seed();
            return;
        }

        Events.RestoreSearches(decoded.Searches ?? new List<string>());
    }

    private static int CompareTime(TimeKey a, TimeKey b)
    {
        var byTime = b.At.CompareTo(a.At);
        if (byTime != 0)
        {
            return byTime;
        }

        return string.Compare(a.Id, b.Id, StringComparison.Ordinal);
    }

    private static int ComparePriority(PriorityKey a, PriorityKey b)
    {
        var byPriority = b.Priority.CompareTo(a.Priority);
        if (byPriority != 0)
        {
            return byPriority;
        }

        return string.Compare(a.Id, b.Id, StringComparison.Ordinal);
    }

    private static int CompareReports(IssueReport a, IssueReport b)
    {
        var byPriority = a.DeskPriority.CompareTo(b.DeskPriority);
        if (byPriority != 0)
        {
            return byPriority;
        }

        return string.Compare(b.Id, a.Id, StringComparison.Ordinal);
    }

    private sealed class TimeKey
    {
        public TimeKey(DateTime at, string id)
        {
            At = at;
            Id = id;
        }

        public DateTime At { get; }
        public string Id { get; }
    }

    private sealed class PriorityKey
    {
        public PriorityKey(int priority, string id)
        {
            Priority = priority;
            Id = id;
        }

        public int Priority { get; }
        public string Id { get; }
    }

    private sealed class StoreDocument
    {
        public int NextNumber { get; set; }
        public List<string>? Searches { get; set; }
        public List<IssueReport>? Reports { get; set; }
    }
}
