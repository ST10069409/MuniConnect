using MuniConnect.Core.Services;

namespace MuniConnect.App;

internal sealed class ShellForm : Form
{
    private readonly CivicRepository _repository;
    private readonly Label _title = new();
    private readonly Button _back = new();
    private readonly Panel _body = new();
    private readonly Stack<Frame> _history = new();
    private Frame? _current;

    public ShellForm(CivicRepository repository)
    {
        _repository = repository;
        Text = "MuniConnect";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1180, 820);
        MinimumSize = new Size(980, 680);
        BackColor = CivicColors.Background;
        Font = CivicFonts.Body;

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = CivicColors.Navy,
        };
        _back.Text = "Back";
        _back.Visible = false;
        _back.FlatStyle = FlatStyle.Flat;
        _back.ForeColor = Color.White;
        _back.BackColor = CivicColors.Navy;
        _back.Font = CivicFonts.Body;
        _back.Size = new Size(88, 38);
        _back.Location = new Point(20, 19);
        _back.Cursor = Cursors.Hand;
        _back.UseVisualStyleBackColor = false;
        _back.FlatAppearance.BorderColor = CivicColors.Teal;
        _back.FlatAppearance.BorderSize = 1;
        _back.Click += (_, _) => GoBack();

        _title.ForeColor = Color.White;
        _title.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
        _title.AutoSize = false;
        _title.TextAlign = ContentAlignment.MiddleLeft;
        _title.Dock = DockStyle.Fill;
        _title.Padding = new Padding(124, 0, 24, 0);
        _title.BackColor = CivicColors.Navy;

        header.Controls.Add(_title);
        header.Controls.Add(_back);
        _back.BringToFront();

        _body.Dock = DockStyle.Fill;
        _body.BackColor = CivicColors.Background;

        Controls.Add(_body);
        Controls.Add(header);
        ShowRoot("MuniConnect", () => new HomePage(this, _repository));
    }

    public CivicRepository Repository => _repository;

    public void ShowRoot(string title, Func<UserControl> create)
    {
        _history.Clear();
        Show(new Frame(title, create));
    }

    public void Push(string title, Func<UserControl> create)
    {
        if (_current != null)
        {
            _history.Push(_current.Value);
        }

        Show(new Frame(title, create));
    }

    public void Replace(string title, Func<UserControl> create) => Show(new Frame(title, create));

    public void GoBack()
    {
        if (_history.Count == 0)
        {
            return;
        }

        Show(_history.Pop());
    }

    private void Show(Frame frame)
    {
        _current = frame;
        _title.Text = frame.Title;
        _back.Visible = _history.Count > 0;
        while (_body.Controls.Count > 0)
        {
            var old = _body.Controls[0];
            _body.Controls.RemoveAt(0);
            old.Dispose();
        }

        var page = frame.Create();
        page.Dock = DockStyle.Fill;
        _body.Controls.Add(page);
    }

    private readonly record struct Frame(string Title, Func<UserControl> Create);
}
