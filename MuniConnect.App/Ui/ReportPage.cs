using MuniConnect.Core.Models;
using MuniConnect.Core.Services;

namespace MuniConnect.App;

internal sealed class ReportPage : UserControl
{
    private readonly ShellForm _shell;
    private readonly CivicRepository _repository;
    private readonly PageColumn _page = new();
    private readonly TextBox _location = new();
    private readonly ComboBox _category = new();
    private readonly TextBox _description = new();
    private readonly Label _priority = CivicUi.Text("", new Font("Segoe UI", 11f, FontStyle.Bold), CivicColors.Navy);
    private readonly Label _notice = CivicUi.Text("", CivicFonts.Body, CivicColors.TealDark);
    private readonly WrapRow _photos = new();
    private readonly List<string> _paths = new();

    public ReportPage(ShellForm shell, CivicRepository repository)
    {
        _shell = shell;
        _repository = repository;
        _notice.Visible = false;
        _photos.Visible = false;
        _photos.Height = 120;

        _page.Add(CivicUi.Text(
            "Tell the civic desk where the problem is, what kind of problem it is, and what you can see.",
            CivicFonts.Body,
            CivicColors.Muted));
        _page.Add(FieldLabel("Location"));
        StyleBox(_location, "Harbour Road, outside the clinic");
        _location.TextChanged += (_, _) => RefreshPriority();
        _page.Add(_location);

        var chips = new WrapRow();
        foreach (var place in new[] { "Market Square", "Harbour Road", "North Green", "Quay Lane" })
        {
            chips.Controls.Add(CivicUi.Secondary(place, (_, _) =>
            {
                _location.Text = place;
                RefreshPriority();
            }));
        }

        _page.Add(chips);
        _page.Add(FieldLabel("Category"));
        _category.DropDownStyle = ComboBoxStyle.DropDownList;
        _category.Font = CivicFonts.Body;
        _category.Height = 36;
        _category.Items.Add("Choose a category");
        foreach (var category in repository.Categories)
        {
            _category.Items.Add(new CategoryChoice(category.Label(), category));
        }

        _category.SelectedIndex = 0;
        _category.SelectedIndexChanged += (_, _) => RefreshPriority();
        _page.Add(_category);
        _page.Add(FieldLabel("Description"));
        _description.Multiline = true;
        _description.Height = 140;
        _description.Font = CivicFonts.Body;
        _description.BorderStyle = BorderStyle.FixedSingle;
        _description.ScrollBars = ScrollBars.Vertical;
        _description.PlaceholderText = "What is wrong, and who is affected?";
        _description.TextChanged += (_, _) => RefreshPriority();
        _page.Add(_description);
        _page.Add(_priority);
        _page.Add(CivicUi.Secondary("Attach photos", (_, _) => AddPhotos()));
        _page.Add(_photos);
        _page.Add(_notice);
        _page.Add(CivicUi.Primary("Submit report", (_, _) => Submit()));
        _page.Add(CivicUi.Secondary("Back to menu", (_, _) =>
            shell.ShowRoot("MuniConnect", () => new HomePage(shell, repository))));
        _page.Dock = DockStyle.Fill;
        Controls.Add(_page);
        RefreshPriority();
    }

    private void RefreshPriority()
    {
        var choice = SelectedCategory();
        if (choice == null)
        {
            _priority.Text = "Desk priority appears after you choose a category.";
            return;
        }

        var score = _repository.PreviewUrgency(_location.Text, choice.Value, _description.Text);
        _priority.Text = $"Desk priority: {CategoryText.LabelForUrgency(score)}";
        _page.Arrange();
    }

    private IssueCategory? SelectedCategory() =>
        _category.SelectedItem is CategoryChoice choice ? choice.Category : null;

    private void AddPhotos()
    {
        var room = 4 - _paths.Count;
        if (room <= 0)
        {
            ShowNotice("You can attach up to 4 photos.");
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Title = "Attach photos",
            Filter = "Images|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp",
            Multiselect = true,
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        foreach (var path in dialog.FileNames.Take(room))
        {
            _paths.Add(path);
            _photos.Controls.Add(Thumbnail(path));
        }

        _photos.Visible = _paths.Count > 0;
        ShowNotice(dialog.FileNames.Length > room
            ? $"Only the first 4 photos were attached. {_paths.Count} of 4."
            : $"{_paths.Count} of 4 photos attached.");
    }

    private Control Thumbnail(string path)
    {
        var box = new Panel
        {
            Width = 112,
            Height = 112,
            BackColor = CivicColors.Background,
        };
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
                Text = "Photo",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
            });
        }

        var remove = new Button
        {
            Text = "×",
            Size = new Size(24, 24),
            Location = new Point(84, 4),
            FlatStyle = FlatStyle.Flat,
            BackColor = CivicColors.Navy,
            ForeColor = Color.White,
        };
        remove.FlatAppearance.BorderSize = 0;
        remove.Click += (_, _) =>
        {
            _paths.Remove(path);
            box.BackgroundImage?.Dispose();
            _photos.Controls.Remove(box);
            box.Dispose();
            _photos.Visible = _paths.Count > 0;
            ShowNotice(_paths.Count == 0 ? "" : $"{_paths.Count} of 4 photos attached.");
        };
        box.Controls.Add(remove);
        return box;
    }

    private void ShowNotice(string text)
    {
        _notice.Text = text;
        _notice.Visible = text.Length > 0;
        _page.Arrange();
    }

    private void Submit()
    {
        var location = _location.Text.Trim();
        var description = _description.Text.Trim();
        var category = SelectedCategory();
        if (location.Length < 3)
        {
            ShowNotice("Enter the place where the issue is.");
            return;
        }

        if (category == null)
        {
            ShowNotice("Choose a category.");
            return;
        }

        if (description.Length < 8)
        {
            ShowNotice("Describe the issue in a little more detail.");
            return;
        }

        var report = _repository.Submit(location, category.Value, description, _paths);
        var track = SubmitDialog.Ask(FindForm()!, report);
        if (track)
        {
            _shell.Replace(report.Id, () => new DetailPage(_shell, _repository, report.Id));
        }
        else
        {
            _shell.ShowRoot("MuniConnect", () => new HomePage(_shell, _repository));
        }
    }

    private static Label FieldLabel(string text) =>
        CivicUi.Text(text, new Font("Segoe UI", 9.5f, FontStyle.Bold), CivicColors.Ink);

    private static void StyleBox(TextBox box, string placeholder)
    {
        box.Font = CivicFonts.Body;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Height = 36;
        box.PlaceholderText = placeholder;
    }

    private sealed record CategoryChoice(string Label, IssueCategory Category)
    {
        public override string ToString() => Label;
    }
}

internal static class SubmitDialog
{
    public static bool Ask(IWin32Window owner, IssueReport report)
    {
        var reply = report.Thread.Count == 0
            ? "The civic desk has logged your report."
            : report.Thread[^1].Body;
        using var dialog = new Form
        {
            Text = "Report submitted",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = CivicColors.Background,
            Font = CivicFonts.Body,
            ClientSize = new Size(560, 420),
        };
        var band = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = CivicColors.Teal };
        var body = new Label
        {
            Text = $"Reference {report.Id}\n{report.UrgencyLabel}  ·  {report.Status.Label()}\n\nCivic desk\n{reply}",
            Font = CivicFonts.Body,
            ForeColor = CivicColors.Ink,
            Location = new Point(28, 28),
            MaximumSize = new Size(500, 0),
            AutoSize = true,
        };
        var back = CivicUi.Secondary("Back to menu", (_, _) => dialog.Close());
        var track = CivicUi.Primary("Track request", (_, _) =>
        {
            dialog.Tag = true;
            dialog.Close();
        });
        track.Width = 180;
        track.Height = 44;
        dialog.Controls.Add(body);
        dialog.Controls.Add(back);
        dialog.Controls.Add(track);
        dialog.Controls.Add(band);
        dialog.Shown += (_, _) =>
        {
            var height = body.Bottom + 88;
            dialog.ClientSize = new Size(560, Math.Max(280, height));
            back.Location = new Point(28, dialog.ClientSize.Height - 64);
            track.Location = new Point(dialog.ClientSize.Width - 208, dialog.ClientSize.Height - 68);
        };
        dialog.ShowDialog(owner);
        return dialog.Tag is true;
    }
}
