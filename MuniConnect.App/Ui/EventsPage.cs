using MuniConnect.Core.Models;
using MuniConnect.Core.Services;

namespace MuniConnect.App;

internal sealed class EventsPage : UserControl
{
    private readonly CivicRepository _repository;
    private readonly PageColumn _page = new();
    private readonly TextBox _query = new();
    private readonly Label _favourite = CivicUi.Text("", CivicFonts.Body, CivicColors.TealDark);
    private readonly WrapRow _chips = new();
    private readonly CardStrip _suggestions = new();
    private readonly Label _resultsHeading = CivicUi.Text("Coming up", CivicFonts.Section, CivicColors.Navy);
    private readonly EventList _results = new();
    private readonly EventList _trail = new();

    public EventsPage(ShellForm shell, CivicRepository repository)
    {
        _repository = repository;
        _page.Add(CivicUi.Text(
            "Upcoming events and announcements in Harbourford. Search changes the suggestions.",
            CivicFonts.Body,
            CivicColors.Muted));

        var search = CivicUi.Primary("Search", (_, _) => RunSearch());
        _query.PlaceholderText = "Search events, for example market or water";
        _query.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                RunSearch();
            }
        };
        _page.Add(new SearchBar(_query, search));
        _page.Add(_favourite);
        _page.Add(_chips);
        _page.Add(CivicUi.Text("Suggested for you", CivicFonts.Section, CivicColors.Navy));
        _page.Add(_suggestions);
        _page.Add(_resultsHeading);
        _page.Add(_results);
        _page.Add(CivicUi.Text("A linked week", CivicFonts.Section, CivicColors.Navy));
        _page.Add(_trail);
        _page.Add(CivicUi.Secondary("Back to menu", (_, _) =>
            shell.ShowRoot("MuniConnect", () => new HomePage(shell, repository))));
        _page.Dock = DockStyle.Fill;
        Controls.Add(_page);
        Bind(repository.Events.Upcoming, searching: false);
    }

    private void RunSearch()
    {
        var text = _query.Text.Trim();
        if (text.Length >= 2)
        {
            _repository.RememberSearch(text);
        }

        Bind(_repository.Events.ResultsFor(text), searching: text.Length > 0);
    }

    private void Bind(IReadOnlyList<LocalEvent> listings, bool searching)
    {
        var favourite = _repository.MostFrequentSearch;
        _favourite.Text = favourite == null
            ? "Search twice for the same word and it becomes a favourite."
            : $"You often look for \"{favourite}\".";

        _chips.Controls.Clear();
        foreach (var term in _repository.RecentSearches)
        {
            var captured = term;
            _chips.Controls.Add(CivicUi.Secondary(captured, (_, _) =>
            {
                _query.Text = captured;
                RunSearch();
            }));
        }

        _suggestions.Controls.Clear();
        foreach (var listing in _repository.Events.Suggestions())
        {
            _suggestions.Controls.Add(SuggestionCard(listing));
        }

        _resultsHeading.Text = searching ? "Matches" : "Coming up";
        _results.Fill(listings.Count == 0
            ? [CivicUi.Text("No events match that search.", CivicFonts.Body, CivicColors.Muted)]
            : listings.Select(EventCard).ToList());

        var trailItems = new List<Control>();
        var number = 1;
        foreach (var listing in _repository.Events.Trail())
        {
            trailItems.Add(CivicUi.Text(
                $"{number}.  {listing.Title}",
                new Font("Segoe UI", 11f, FontStyle.Bold),
                CivicColors.Ink));
            trailItems.Add(CivicUi.Text(
                $"{CivicUi.When(listing.Start)}   ·   {listing.Venue}",
                CivicFonts.Small,
                CivicColors.Muted));
            number++;
        }

        _trail.Fill(trailItems);
        _page.Arrange();
    }

    private static StackBlock SuggestionCard(LocalEvent listing)
    {
        var card = new StackBlock { Gap = 6, Width = 300 };
        card.AddRow(CivicUi.Text(
            listing.Kind == ListingKind.Announcement ? "Announcement" : "Event",
            CivicFonts.Eyebrow,
            CivicColors.Teal));
        card.AddRow(CivicUi.Text(listing.Title, new Font("Segoe UI", 12f, FontStyle.Bold), CivicColors.Ink));
        card.AddRow(CivicUi.Text(CivicUi.When(listing.Start), CivicFonts.Small, CivicColors.Muted));
        card.AddRow(CivicUi.Text(listing.Venue, CivicFonts.Body, CivicColors.Navy));
        card.Arrange(300);
        return card;
    }

    private static Control EventCard(LocalEvent listing)
    {
        var card = new StackBlock { Accent = CivicColors.Teal, Gap = 6 };
        var kind = listing.Kind == ListingKind.Announcement ? "Announcement" : "Event";
        card.AddRow(CivicUi.Text($"{kind}   ·   {listing.Category}", CivicFonts.Eyebrow, CivicColors.Teal));
        card.AddRow(CivicUi.Text(listing.Title, CivicFonts.Section, CivicColors.Ink));
        card.AddRow(CivicUi.Text($"{CivicUi.When(listing.Start)}    ·    {listing.Venue}", CivicFonts.Body, CivicColors.Navy));
        card.AddRow(CivicUi.Text(listing.Summary, CivicFonts.Body, CivicColors.Muted));
        return card;
    }

    private sealed class EventList : Panel, IArranged
    {
        public EventList()
        {
            BackColor = Color.Transparent;
        }

        public void Fill(IReadOnlyList<Control> items)
        {
            Controls.Clear();
            foreach (var item in items)
            {
                Controls.Add(item);
            }
        }

        public void Arrange(int width)
        {
            Width = width;
            var y = 0;
            foreach (Control row in Controls)
            {
                row.Left = 0;
                row.Top = y;
                if (row is IArranged arranged)
                {
                    arranged.Arrange(width);
                }
                else if (row is Label label)
                {
                    CivicUi.FitLabel(label, width);
                }

                y += row.Height + 12;
            }

            Height = Math.Max(24, y);
        }
    }
}
