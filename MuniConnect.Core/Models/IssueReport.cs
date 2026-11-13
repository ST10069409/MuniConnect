using System.Text.Json.Serialization;

namespace MuniConnect.Core.Models;

public enum IssueCategory
{
    roadWorks,
    sanitary,
    utilities,
    parks,
    publicSafety,
}

public enum RequestStatus
{
    received,
    inReview,
    assigned,
    inProgress,
    resolved,
}

public static class CategoryText
{
    public static string Label(this IssueCategory category) => category switch
    {
        IssueCategory.roadWorks => "Road works",
        IssueCategory.sanitary => "Sanitary",
        IssueCategory.utilities => "Utilities",
        IssueCategory.parks => "Parks",
        IssueCategory.publicSafety => "Public safety",
        _ => category.ToString(),
    };

    public static string Label(this RequestStatus status) => status switch
    {
        RequestStatus.received => "Received",
        RequestStatus.inReview => "In review",
        RequestStatus.assigned => "Assigned",
        RequestStatus.inProgress => "In progress",
        RequestStatus.resolved => "Resolved",
        _ => status.ToString(),
    };

    public static bool IsOpen(this RequestStatus status) => status != RequestStatus.resolved;

    public static RequestStatus? Next(this RequestStatus status) => status switch
    {
        RequestStatus.received => RequestStatus.inReview,
        RequestStatus.inReview => RequestStatus.assigned,
        RequestStatus.assigned => RequestStatus.inProgress,
        RequestStatus.inProgress => RequestStatus.resolved,
        _ => null,
    };

    public static string LabelForUrgency(int urgency) => urgency switch
    {
        >= 5 => "Critical",
        4 => "High",
        3 => "Standard",
        _ => "Routine",
    };
}

public sealed class StatusMark
{
    public RequestStatus Status { get; set; }
    public DateTime At { get; set; }
}

public sealed class FeedbackMessage
{
    public string Id { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime At { get; set; }
    public bool FromResident { get; set; }
}

public sealed class IssueReport
{
    public string Id { get; set; } = "";
    public string Location { get; set; } = "";
    public IssueCategory Category { get; set; }
    public string Description { get; set; } = "";
    public List<string> ImagePaths { get; set; } = new();
    public DateTime SubmittedAt { get; set; }
    public RequestStatus Status { get; set; }
    public int Urgency { get; set; }
    public List<StatusMark> History { get; set; } = new();
    public List<FeedbackMessage> Thread { get; set; } = new();

    [JsonIgnore]
    public string UrgencyLabel => CategoryText.LabelForUrgency(Urgency);

    /// <summary>Higher values are handled first: severity, then time already waited.</summary>
    [JsonIgnore]
    public int DeskPriority
    {
        get
        {
            var waitingMinutes = (int)DateTime.Now.Subtract(SubmittedAt).TotalMinutes;
            if (waitingMinutes < 0)
            {
                waitingMinutes = 0;
            }

            return Urgency * 1_000_000 + waitingMinutes;
        }
    }

    [JsonIgnore]
    public string LatestUpdate => Thread.Count == 0
        ? "Waiting for the civic desk"
        : Thread[^1].Body;
}
