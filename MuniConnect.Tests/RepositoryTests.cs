using MuniConnect.Core.Models;
using MuniConnect.Core.Services;

namespace MuniConnect.Tests;

public sealed class RepositoryTests : IDisposable
{
    private readonly string _directory;

    public RepositoryTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "municonnect-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void Submitting_a_report_logs_it_and_posts_a_desk_reply()
    {
        var repo = Desk();
        var report = repo.Submit(
            "Market Square",
            IssueCategory.sanitary,
            "There is a sewage leak beside the school entrance.",
            Array.Empty<string>());

        Assert.Equal("HD-1003", report.Id);
        Assert.Equal(5, report.Urgency);
        Assert.Equal("Critical", report.UrgencyLabel);
        Assert.Equal(RequestStatus.inReview, report.Status);
        Assert.Contains("Civic desk received HD-1003", report.Thread[^1].Body);
        Assert.False(report.Thread[^1].FromResident);
    }

    [Fact]
    public void The_desk_advances_the_highest_priority_open_request()
    {
        var repo = Desk();
        var updated = repo.AdvanceHighestPriority();
        Assert.NotNull(updated);
        Assert.Equal("HD-1001", updated.Id);
        Assert.Equal(RequestStatus.resolved, updated.Status);
    }

    [Fact]
    public void A_reply_is_answered_and_a_resolved_request_can_be_reopened()
    {
        var repo = Desk();
        repo.SendFeedback("HD-1002", "The bins are still full.");
        Assert.True(repo.TryGetReport("HD-1002", out var open));
        Assert.Equal(RequestStatus.assigned, open.Status);
        Assert.Contains("assigned", open.Thread[^1].Body, StringComparison.OrdinalIgnoreCase);

        repo.SendFeedback("HD-1000", "The lights are off again.");
        Assert.True(repo.TryGetReport("HD-1000", out var reopened));
        Assert.Equal(RequestStatus.inReview, reopened.Status);
        Assert.Contains("reopened", reopened.Thread[^1].Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Search_suggestions_and_the_linked_week_use_the_indexes()
    {
        var repo = Desk();
        Assert.Null(repo.MostFrequentSearch);
        repo.RememberSearch("market");
        Assert.Null(repo.MostFrequentSearch);
        repo.RememberSearch("market");
        Assert.Equal("market", repo.MostFrequentSearch);

        var results = repo.Events.ResultsFor("harbourford saturday market");
        Assert.Equal("Harbourford Saturday market", results[0].Title);
        Assert.NotEmpty(repo.Events.Suggestions());
        Assert.True(repo.Events.Trail().Count >= 2);
        Assert.Contains("Road works", repo.Events.ServiceBreadthFirst());
    }

    [Fact]
    public void Reports_and_searches_survive_a_restart()
    {
        var repo = Desk();
        repo.RememberSearch("water");
        repo.RememberSearch("water");
        repo.Submit(
            "North Green",
            IssueCategory.parks,
            "The bench in North Green needs a repair.",
            Array.Empty<string>());

        var again = Desk();
        Assert.True(again.TryGetReport("HD-1003", out var report));
        Assert.Equal(IssueCategory.parks, report.Category);
        Assert.Equal("water", again.MostFrequentSearch);
        Assert.Contains("HD-1001", again.Reports().Select(item => item.Id));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
        catch
        {
            // Windows can keep a file handle for a moment after the test.
        }
    }

    private CivicRepository Desk()
    {
        var repo = new CivicRepository(_directory);
        repo.Init();
        return repo;
    }
}
