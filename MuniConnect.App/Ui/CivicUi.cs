using MuniConnect.Core.Models;

namespace MuniConnect.App;

internal static class CivicColors
{
    public static readonly Color Navy = Color.FromArgb(0x17, 0x4A, 0x7E);
    public static readonly Color Teal = Color.FromArgb(0x20, 0xA3, 0x9E);
    public static readonly Color TealDark = Color.FromArgb(0x14, 0x75, 0x6F);
    public static readonly Color Background = Color.FromArgb(0xF4, 0xF6, 0xF8);
    public static readonly Color Ink = Color.FromArgb(0x1A, 0x2B, 0x3C);
    public static readonly Color Muted = Color.FromArgb(0x5E, 0x6E, 0x7E);
    public static readonly Color Line = Color.FromArgb(0xD5, 0xDE, 0xE7);
    public static readonly Color Card = Color.White;
    public static readonly Color SoftTeal = Color.FromArgb(0xE5, 0xF6, 0xF5);
}

internal static class CivicFonts
{
    public static readonly Font Body = new("Segoe UI", 10.5f);
    public static readonly Font Small = new("Segoe UI", 9.5f);
    public static readonly Font Section = new("Segoe UI", 14f, FontStyle.Bold);
    public static readonly Font Title = new("Segoe UI", 24f, FontStyle.Bold);
    public static readonly Font Eyebrow = new("Segoe UI", 9f, FontStyle.Bold);
}

internal interface IArranged
{
    void Arrange(int width);
}

internal static class CivicUi
{
    public static Label Text(string text, Font font, Color color)
    {
        return new Label
        {
            Text = text,
            Font = font,
            ForeColor = color,
            AutoSize = true,
            BackColor = Color.Transparent,
            UseMnemonic = false,
        };
    }

    public static void FitLabel(Label label, int width)
    {
        var wrap = Math.Max(40, width);
        var size = TextRenderer.MeasureText(
            label.Text,
            label.Font,
            new Size(wrap, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        label.AutoSize = false;
        label.MaximumSize = new Size(wrap, 0);
        label.Width = wrap;
        label.Height = Math.Max(24, size.Height + 2);
    }

    public static Button Primary(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            Tag = "fill",
            Height = 46,
            FlatStyle = FlatStyle.Flat,
            BackColor = CivicColors.Navy,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += onClick;
        return button;
    }

    public static Button Secondary(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            Tag = "hug",
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = CivicColors.Card,
            ForeColor = CivicColors.Navy,
            Font = CivicFonts.Body,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };
        button.FlatAppearance.BorderColor = CivicColors.Teal;
        button.FlatAppearance.BorderSize = 1;
        var size = TextRenderer.MeasureText(text, button.Font);
        button.Width = size.Width + 32;
        button.Click += onClick;
        return button;
    }

    public static Label Pill(string text, Color back)
    {
        var pill = new Label
        {
            Text = text,
            Tag = "hug",
            AutoSize = true,
            ForeColor = Color.White,
            BackColor = back,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Padding = new Padding(10, 5, 10, 5),
            UseMnemonic = false,
        };
        var size = pill.GetPreferredSize(Size.Empty);
        pill.Width = size.Width;
        pill.Height = Math.Max(28, size.Height);
        return pill;
    }

    public static Color StatusColor(RequestStatus status) => status switch
    {
        RequestStatus.received => CivicColors.Muted,
        RequestStatus.inReview => CivicColors.Navy,
        RequestStatus.assigned => CivicColors.Teal,
        RequestStatus.inProgress => CivicColors.TealDark,
        RequestStatus.resolved => Color.FromArgb(0x0E, 0x3A, 0x66),
        _ => CivicColors.Muted,
    };

    public static string When(DateTime at) => at.ToString("ddd d MMM yyyy, HH:mm");

    public static void Clicks(Control root, EventHandler handler)
    {
        root.Cursor = Cursors.Hand;
        root.Click += handler;
        foreach (Control child in root.Controls)
        {
            Clicks(child, handler);
        }
    }
}

internal sealed class PageColumn : UserControl
{
    private readonly Panel _scroll = new();
    private readonly List<Control> _items = new();

    public PageColumn()
    {
        BackColor = CivicColors.Background;
        _scroll.Dock = DockStyle.Fill;
        _scroll.AutoScroll = true;
        _scroll.BackColor = CivicColors.Background;
        _scroll.Resize += (_, _) => Arrange();
        Controls.Add(_scroll);
    }

    public void Add(Control item)
    {
        _items.Add(item);
        _scroll.Controls.Add(item);
    }

    public int ContentWidth
    {
        get
        {
            var available = _scroll.ClientSize.Width - 72;
            if (available < 200)
            {
                available = 720;
            }

            return Math.Clamp(available, 420, 860);
        }
    }

    public void Arrange()
    {
        var width = ContentWidth;
        var left = Math.Max(28, (_scroll.ClientSize.Width - width) / 2);
        var y = 28;
        foreach (var item in _items)
        {
            if (!item.Visible)
            {
                continue;
            }

            item.Left = left;
            if (item is IArranged arranged)
            {
                arranged.Arrange(width);
            }
            else if (item is Label label)
            {
                if (label.Tag as string == "hug")
                {
                    var preferred = label.GetPreferredSize(Size.Empty);
                    label.Width = Math.Max(label.Width, preferred.Width);
                    label.Height = Math.Max(label.Height, preferred.Height);
                }
                else
                {
                    CivicUi.FitLabel(label, width);
                }
            }
            else if (item.Tag as string != "hug")
            {
                item.Width = width;
            }

            item.Top = y;
            y += item.Height + 18;
        }

        _scroll.AutoScrollMinSize = new Size(0, y + 24);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Arrange();
    }
}

internal sealed class StackBlock : Panel, IArranged
{
    private readonly List<Control> _rows = new();

    public StackBlock()
    {
        BackColor = CivicColors.Card;
        Padding = new Padding(20, 16, 20, 16);
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw,
            true);
    }

    public Color Accent = Color.Empty;
    public int Gap = 8;

    public void AddRow(Control row)
    {
        _rows.Add(row);
        Controls.Add(row);
    }

    public void ClearRows()
    {
        foreach (var row in _rows)
        {
            Controls.Remove(row);
            row.Dispose();
        }

        _rows.Clear();
    }

    public void Arrange(int width)
    {
        Width = width;
        var inner = Math.Max(80, width - 40);
        var y = 18;
        foreach (var row in _rows)
        {
            if (!row.Visible)
            {
                continue;
            }

            row.Left = 20;
            row.Top = y;
            if (row is IArranged arranged)
            {
                arranged.Arrange(inner);
            }
            else if (row.Tag as string == "hug")
            {
                var preferred = row.GetPreferredSize(Size.Empty);
                row.Width = Math.Max(row.Width, preferred.Width);
                row.Height = Math.Max(row.Height, preferred.Height);
            }
            else if (row is Label label)
            {
                CivicUi.FitLabel(label, inner);
            }
            else if (row.Tag as string == "fill")
            {
                row.Width = inner;
                if (row.Height < 40)
                {
                    row.Height = 46;
                }
            }
            else
            {
                row.Width = inner;
            }

            y += row.Height + Gap;
        }

        Height = Math.Max(56, y + 10);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        using var border = new Pen(CivicColors.Line);
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        if (Accent != Color.Empty)
        {
            using var brush = new SolidBrush(Accent);
            e.Graphics.FillRectangle(brush, 0, 0, 6, Height);
        }
    }
}

internal sealed class WrapRow : Panel, IArranged
{
    public WrapRow()
    {
        BackColor = Color.Transparent;
    }

    public void Arrange(int width)
    {
        Width = width;
        var x = 0;
        var y = 0;
        var rowHeight = 0;
        foreach (Control child in Controls)
        {
            if (x > 0 && x + child.Width > width)
            {
                x = 0;
                y += rowHeight + 10;
                rowHeight = 0;
            }

            child.Left = x;
            child.Top = y;
            x += child.Width + 10;
            rowHeight = Math.Max(rowHeight, child.Height);
        }

        Height = Math.Max(38, y + rowHeight);
    }
}

internal sealed class SearchBar : Panel, IArranged
{
    public SearchBar(TextBox query, Button search)
    {
        Query = query;
        Search = search;
        Height = 46;
        BackColor = Color.Transparent;
        query.Font = CivicFonts.Body;
        query.BorderStyle = BorderStyle.FixedSingle;
        Controls.Add(query);
        Controls.Add(search);
    }

    public TextBox Query { get; }
    public Button Search { get; }

    public void Arrange(int width)
    {
        Width = width;
        Height = 46;
        Search.SetBounds(width - 140, 0, 140, 46);
        Query.SetBounds(0, 6, Math.Max(160, width - 156), 34);
    }
}

internal sealed class CardStrip : Panel, IArranged
{
    public CardStrip()
    {
        AutoScroll = true;
        BackColor = Color.Transparent;
        Height = 220;
    }

    public void Arrange(int width)
    {
        Width = width;
        var cards = Controls.Cast<Control>().ToList();
        if (cards.Count == 0)
        {
            Height = 8;
            return;
        }

        const int gap = 16;
        var cardWidth = Math.Max(210, (width - gap * (cards.Count - 1)) / cards.Count);
        var x = 0;
        var tallest = 0;
        foreach (var card in cards)
        {
            if (card is IArranged arranged)
            {
                arranged.Arrange(cardWidth);
            }

            card.Left = x;
            card.Top = 0;
            tallest = Math.Max(tallest, card.Height);
            x += cardWidth + gap;
        }

        AutoScroll = false;
        AutoScrollMinSize = Size.Empty;
        Height = tallest + 6;
    }
}
