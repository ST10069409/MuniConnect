namespace MuniConnect.Core.Models;

public enum ListingKind
{
    Event,
    Announcement,
}

public sealed class LocalEvent
{
    public LocalEvent(
        string id,
        string title,
        string summary,
        string venue,
        DateTime start,
        string category,
        string serviceArea,
        ListingKind kind,
        IReadOnlyList<string> tags)
    {
        Id = id;
        Title = title;
        Summary = summary;
        Venue = venue;
        Start = start;
        Category = category;
        ServiceArea = serviceArea;
        Kind = kind;
        Tags = tags;
    }

    public string Id { get; }
    public string Title { get; }
    public string Summary { get; }
    public string Venue { get; }
    public DateTime Start { get; }
    public string Category { get; }
    public string ServiceArea { get; }
    public ListingKind Kind { get; }
    public IReadOnlyList<string> Tags { get; }
}

public sealed class EventKey : IComparable<EventKey>
{
    public EventKey(DateTime start, string id)
    {
        Start = start;
        Id = id;
    }

    public DateTime Start { get; }
    public string Id { get; }

    public int CompareTo(EventKey? other)
    {
        if (other == null)
        {
            return 1;
        }

        var byTime = Start.CompareTo(other.Start);
        if (byTime != 0)
        {
            return byTime;
        }

        return string.Compare(Id, other.Id, StringComparison.Ordinal);
    }
}
