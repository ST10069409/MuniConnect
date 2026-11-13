using MuniConnect.Core.Services;

namespace MuniConnect.App;

internal sealed class StructuresPage : UserControl
{
    public StructuresPage(CivicRepository repository)
    {
        var snapshot = repository.Snapshot();
        var next = snapshot.NextReference == null
            ? "Nothing is waiting."
            : $"{snapshot.NextReference} ({snapshot.NextSummary}).";
        var recent = snapshot.RecentSearches.Count == 0
            ? "Recent searches sit on a stack, newest on top. None yet."
            : "Recent searches sit on a stack, newest on top: " + string.Join(", ", snapshot.RecentSearches) + ".";

        (string Title, string Body)[] notes =
        [
            ("Hash table", $"Reports are stored by reference, so opening one is a direct lookup. {snapshot.ReportCount} reports are in the table."),
            ("Set", "Issue categories are kept as a set, so a category is listed once. " + string.Join(", ", snapshot.Categories) + "."),
            ("Queue", $"New reports wait in an intake queue and leave in arrival order. {snapshot.IntakeWaiting} are waiting. {snapshot.IntakeProcessed} have already moved on."),
            ("Stack", recent),
            ("Priority queue and binary heap", $"The desk takes the highest-priority open request first. Next: {next} The queue is a binary heap."),
            ("Sorted dictionary and AVL tree", $"Events stay in date order in a sorted dictionary ({snapshot.EventCount} events, height {snapshot.EventIndexHeight}). Reports use an AVL tree keyed by time, height {snapshot.AvlHeight}, so the status list stays balanced as reports are added."),
            ("Binary search tree", "Event titles are also stored in a binary search tree for an exact title match. That tree is left unbalanced. The AVL tree is the one that stays short."),
            ("Red-black tree", $"Open requests are ranked in a red-black tree by desk priority. {snapshot.OpenCount} open requests are indexed."),
            ("Binary tree", "Urgency is decided by walking a binary tree. Yes goes left.\n" + string.Join("\n", repository.UrgencyChecks)),
            ("Graph traversal", $"Services are linked in a graph. Breadth-first from Road works: {string.Join(", ", snapshot.BreadthFirst)}. Depth-first: {string.Join(", ", snapshot.DepthFirst)}."),
            ("Minimum spanning tree", $"Upcoming events are joined with Kruskal's algorithm into one connected week ({snapshot.TrailStops} stops, total weight {snapshot.TrailWeight:0.##})."),
        ];

        var page = new PageColumn();
        page.Add(CivicUi.Text(
            "The desk, the event list, and the suggestions are built from these structures. The numbers change after you submit a report or record a search.",
            CivicFonts.Body,
            CivicColors.Muted));
        foreach (var note in notes)
        {
            var card = new StackBlock { Accent = CivicColors.Navy, Gap = 8 };
            card.AddRow(CivicUi.Text(note.Title, CivicFonts.Section, CivicColors.Navy));
            var bodyFont = note.Title == "Binary tree" ? new Font("Consolas", 10.5f) : CivicFonts.Body;
            card.AddRow(CivicUi.Text(note.Body, bodyFont, CivicColors.Ink));
            page.Add(card);
        }

        page.Dock = DockStyle.Fill;
        Controls.Add(page);
    }
}
