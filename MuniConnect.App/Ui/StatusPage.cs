using MuniConnect.Core.Models;
using MuniConnect.Core.Services;

namespace MuniConnect.App;

internal sealed class StatusPage : UserControl
{
    private readonly ShellForm _shell;
    private readonly CivicRepository _repository;
    private readonly PageColumn _page = new();
    private readonly WrapRow _filters = new();
    private readonly Label _notice = CivicUi.Text("", CivicFonts.Body, CivicColors.TealDark);
    private readonly RequestList _list = new();
    private readonly List<Control> _rows = new();
    private RequestStatus? _filter;

    public StatusPage(ShellForm shell, CivicRepository repository)
    {
        _shell = shell;
        _repository = repository;
        _notice.Visible = false;
        _page.Add(CivicUi.Text(
            "Requests are listed newest first. Open one to follow the timeline and reply to the civic desk.",
            CivicFonts.Body,
            CivicColors.Muted));
        _page.Add(CivicUi.Primary("Advance the most urgent request", (_, _) => Advance()));
        _page.Add(_notice);
        _page.Add(_filters);
        _page.Add(_list);
        _page.Dock = DockStyle.Fill;
        Controls.Add(_page);
        Reload();
    }

    private void Advance()
    {
        var updated = _repository.AdvanceHighestPriority();
        _notice.Text = updated == null
            ? "Nothing is waiting."
            : $"{updated.Id} is now {updated.Status.Label()}.";
        _notice.Visible = true;
        Reload();
    }

    private void Reload()
    {
        _filters.Controls.Clear();
        AddFilter("All", null);
        foreach (var status in _repository.StatusesPresent)
        {
            AddFilter(status.Label(), status);
        }

        foreach (var row in _rows)
        {
            _list.Controls.Remove(row);
            row.Dispose();
        }

        _rows.Clear();
        var reports = _repository.Reports(_filter);
        if (reports.Count == 0)
        {
            var empty = CivicUi.Text("No requests in this list yet.", CivicFonts.Body, CivicColors.Muted);
            _rows.Add(empty);
            _list.Controls.Add(empty);
        }

        foreach (var report in reports)
        {
            var row = Row(report);
            _rows.Add(row);
            _list.Controls.Add(row);
        }

        _page.Arrange();
    }

    private void AddFilter(string label, RequestStatus? status)
    {
        var selected = _filter == status;
        var button = CivicUi.Secondary(label, (_, _) =>
        {
            _filter = status;
            Reload();
        });
        if (selected)
        {
            button.BackColor = CivicColors.Navy;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = CivicColors.Navy;
        }

        _filters.Controls.Add(button);
    }

    private Control Row(IssueReport report)
    {
        var card = new StackBlock { Gap = 8 };
        card.AddRow(CivicUi.Text(
            $"{report.Id}    {report.Category.Label()}    {report.UrgencyLabel}",
            new Font("Segoe UI", 12f, FontStyle.Bold),
            CivicColors.Ink));
        card.AddRow(CivicUi.Pill(report.Status.Label(), CivicUi.StatusColor(report.Status)));
        card.AddRow(CivicUi.Text(report.Location, CivicFonts.Body, CivicColors.Navy));
        card.AddRow(CivicUi.Text(report.LatestUpdate, CivicFonts.Small, CivicColors.Muted));
        CivicUi.Clicks(card, (_, _) =>
            _shell.Push(report.Id, () => new DetailPage(_shell, _repository, report.Id)));
        return card;
    }

    private sealed class RequestList : Panel, IArranged
    {
        public RequestList()
        {
            BackColor = Color.Transparent;
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

                y += row.Height + 14;
            }

            Height = Math.Max(24, y);
        }
    }
}
