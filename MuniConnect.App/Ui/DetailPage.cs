using MuniConnect.Core.Models;
using MuniConnect.Core.Services;

namespace MuniConnect.App;

internal sealed class DetailPage : UserControl
{
    private readonly ShellForm _shell;
    private readonly CivicRepository _repository;
    private readonly string _id;
    private readonly PageColumn _page = new();
    private readonly TextBox _reply = new()
    {
        Multiline = true,
        Height = 96,
        Font = CivicFonts.Body,
        BorderStyle = BorderStyle.FixedSingle,
        ScrollBars = ScrollBars.Vertical,
    };
    private readonly Label _notice = CivicUi.Text("", CivicFonts.Body, CivicColors.TealDark);

    public DetailPage(ShellForm shell, CivicRepository repository, string id)
    {
        _shell = shell;
        _repository = repository;
        _id = id;
        _notice.Visible = false;
        if (!repository.TryGetReport(id, out var report))
        {
            _page.Add(CivicUi.Text("That request is no longer on file.", CivicFonts.Body, CivicColors.Muted));
            _page.Dock = DockStyle.Fill;
            Controls.Add(_page);
            return;
        }

        _page.Add(CivicUi.Text(report.Id, CivicFonts.Title, CivicColors.Navy));
        _page.Add(CivicUi.Pill(report.Status.Label(), CivicUi.StatusColor(report.Status)));
        _page.Add(CivicUi.Text(
            $"{report.UrgencyLabel}    ·    {report.Category.Label()}",
            CivicFonts.Section,
            CivicColors.Ink));
        _page.Add(CivicUi.Text(report.Location, CivicFonts.Body, CivicColors.Navy));
        _page.Add(CivicUi.Text(report.Description, CivicFonts.Body, CivicColors.Ink));
        _page.Add(CivicUi.Text($"Submitted {CivicUi.When(report.SubmittedAt)}", CivicFonts.Small, CivicColors.Muted));

        if (report.ImagePaths.Count > 0)
        {
            var photos = new WrapRow();
            foreach (var path in report.ImagePaths)
            {
                photos.Controls.Add(Photo(path));
            }

            _page.Add(photos);
        }

        _page.Add(CivicUi.Text("Progress", CivicFonts.Section, CivicColors.Navy));
        foreach (var mark in report.History)
        {
            _page.Add(CivicUi.Text($"{mark.Status.Label()}    ·    {CivicUi.When(mark.At)}", CivicFonts.Body, CivicColors.Ink));
        }

        var related = repository.Events.RelatedTo(report.Category);
        if (related.Count > 0)
        {
            _page.Add(CivicUi.Text("Related notices", CivicFonts.Section, CivicColors.Navy));
            foreach (var listing in related)
            {
                var card = new StackBlock { Gap = 4 };
                card.AddRow(CivicUi.Text(listing.Title, new Font("Segoe UI", 11f, FontStyle.Bold), CivicColors.Ink));
                card.AddRow(CivicUi.Text($"{CivicUi.When(listing.Start)}    ·    {listing.Venue}", CivicFonts.Small, CivicColors.Muted));
                _page.Add(card);
            }
        }

        _page.Add(CivicUi.Text("Conversation", CivicFonts.Section, CivicColors.Navy));
        if (report.Thread.Count == 0)
        {
            _page.Add(CivicUi.Text("Waiting for the civic desk.", CivicFonts.Body, CivicColors.Muted));
        }

        foreach (var message in report.Thread)
        {
            _page.Add(MessageBubble(message));
        }

        _page.Add(CivicUi.Text("Reply to the civic desk", CivicFonts.Section, CivicColors.Ink));
        _reply.PlaceholderText = "Add what you have seen since the last update";
        _page.Add(_reply);
        _page.Add(_notice);
        _page.Add(CivicUi.Primary("Send to the desk", (_, _) => Send()));
        var actions = new WrapRow();
        actions.Controls.Add(CivicUi.Secondary("Check progress", (_, _) => Check()));
        actions.Controls.Add(CivicUi.Secondary("Back", (_, _) => shell.GoBack()));
        _page.Add(actions);
        _page.Dock = DockStyle.Fill;
        Controls.Add(_page);
    }

    private void Send()
    {
        if (_reply.Text.Trim().Length == 0)
        {
            _notice.Text = "Write a short reply first.";
            _notice.Visible = true;
            _page.Arrange();
            return;
        }

        _repository.SendFeedback(_id, _reply.Text);
        _shell.Replace(_id, () => new DetailPage(_shell, _repository, _id));
    }

    private void Check()
    {
        var before = _repository.TryGetReport(_id, out var current) ? current.Status : RequestStatus.received;
        var updated = _repository.CheckProgress(_id);
        if (updated == null)
        {
            _notice.Text = "That request is no longer on file.";
            _notice.Visible = true;
            _page.Arrange();
            return;
        }

        if (!updated.Status.IsOpen() && before == updated.Status)
        {
            _notice.Text = "This request is resolved. Reply if it still needs attention.";
            _notice.Visible = true;
            _page.Arrange();
            return;
        }

        _shell.Replace(_id, () => new DetailPage(_shell, _repository, _id));
    }

    private static StackBlock MessageBubble(FeedbackMessage message)
    {
        var card = new StackBlock { Gap = 6 };
        card.BackColor = message.FromResident ? CivicColors.SoftTeal : CivicColors.Card;
        card.AddRow(CivicUi.Text(
            message.FromResident ? "You" : "Civic desk",
            CivicFonts.Eyebrow,
            message.FromResident ? CivicColors.TealDark : CivicColors.Navy));
        card.AddRow(CivicUi.Text(CivicUi.When(message.At), CivicFonts.Small, CivicColors.Muted));
        card.AddRow(CivicUi.Text(message.Body, CivicFonts.Body, CivicColors.Ink));
        return card;
    }

    private static Control Photo(string path)
    {
        var box = new Panel { Width = 160, Height = 120, BackColor = CivicColors.Background };
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var image = Image.FromStream(stream);
            box.BackgroundImage = new Bitmap(image);
            box.BackgroundImageLayout = ImageLayout.Zoom;
        }
        catch
        {
            box.Controls.Add(new Label
            {
                Text = Path.GetFileName(path),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
            });
        }

        return box;
    }
}
