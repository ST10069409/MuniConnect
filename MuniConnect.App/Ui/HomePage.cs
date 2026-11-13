using MuniConnect.Core.Services;

namespace MuniConnect.App;

internal sealed class HomePage : UserControl
{
    public HomePage(ShellForm shell, CivicRepository repository)
    {
        var page = new PageColumn();
        var open = repository.OpenCount;
        page.Add(CivicUi.Text("HARBOURFORD", CivicFonts.Eyebrow, CivicColors.Teal));
        page.Add(CivicUi.Text("What do you want to do?", CivicFonts.Title, CivicColors.Navy));
        page.Add(CivicUi.Text(
            "Report an issue, see what is happening nearby, and follow the reply from the civic desk. You can answer on the same request.",
            CivicFonts.Body,
            CivicColors.Muted));
        page.Add(Task(
            "Report an issue",
            "Location, category, photos, and a desk reply.",
            CivicColors.Teal,
            (_, _) => shell.Push("Report an issue", () => new ReportPage(shell, repository))));
        page.Add(Task(
            "Events and announcements",
            "Upcoming local listings, search, and suggestions.",
            CivicColors.Navy,
            (_, _) => shell.Push("Events and announcements", () => new EventsPage(shell, repository))));
        page.Add(Task(
            "Service request status",
            open == 0
                ? "None open. Track a report after you send one."
                : $"{open} open. Track progress and reply to the desk.",
            CivicColors.TealDark,
            (_, _) => shell.Push("Service request status", () => new StatusPage(shell, repository))));
        page.Add(CivicUi.Secondary("How data is organised", (_, _) =>
            shell.Push("How data is organised", () => new StructuresPage(repository))));
        page.Dock = DockStyle.Fill;
        Controls.Add(page);
    }

    private static StackBlock Task(string title, string subtitle, Color accent, EventHandler click)
    {
        var card = new StackBlock { Accent = accent, Gap = 6 };
        card.AddRow(CivicUi.Text(title, CivicFonts.Section, CivicColors.Ink));
        card.AddRow(CivicUi.Text(subtitle, CivicFonts.Body, CivicColors.Muted));
        CivicUi.Clicks(card, click);
        return card;
    }
}
