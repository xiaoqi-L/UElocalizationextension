namespace LocalizationOverrideEditor;

internal sealed class DarkComboBox : ComboBox
{
    private const int WmPaint = 0x000F;

    public DarkComboBox()
    {
        FlatStyle = FlatStyle.Flat;
        DrawMode = DrawMode.OwnerDrawFixed;
        DrawItem += OnDrawItem;
    }

    public void ApplyTheme()
    {
        var colors = EditorUiThemePalette.Get(EditorUiStrings.CurrentTheme);
        BackColor = colors.InputBackground;
        ForeColor = colors.InputText;
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg != WmPaint || EditorUiStrings.CurrentTheme != EditorUiTheme.Dark)
        {
            return;
        }

        PaintDarkChrome();
    }

    private void PaintDarkChrome()
    {
        var colors = EditorUiThemePalette.Get(EditorUiTheme.Dark);
        using var g = CreateGraphics();

        using (var borderPen = new Pen(colors.Border))
        {
            g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
        }

        var buttonWidth = Math.Max(18, SystemInformation.HorizontalScrollBarArrowWidth);
        var buttonRect = new Rectangle(Width - buttonWidth - 1, 1, buttonWidth - 1, Height - 3);
        using (var backBrush = new SolidBrush(colors.InputBackground))
        {
            g.FillRectangle(backBrush, buttonRect);
        }

        var centerX = buttonRect.Left + buttonRect.Width / 2;
        var centerY = buttonRect.Top + buttonRect.Height / 2;
        Point[] arrow =
        [
            new(centerX - 4, centerY - 2),
            new(centerX + 4, centerY - 2),
            new(centerX, centerY + 2)
        ];
        using var arrowBrush = new SolidBrush(colors.SecondaryText);
        g.FillPolygon(arrowBrush, arrow);
    }

    private static void OnDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || sender is not ComboBox comboBox)
        {
            return;
        }

        var colors = EditorUiThemePalette.Get(EditorUiStrings.CurrentTheme);
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var backColor = selected ? colors.GridSelectionBackground : colors.InputBackground;

        using (var backBrush = new SolidBrush(backColor))
        {
            e.Graphics.FillRectangle(backBrush, e.Bounds);
        }

        var text = comboBox.Items[e.Index]?.ToString() ?? string.Empty;
        TextRenderer.DrawText(
            e.Graphics,
            text,
            comboBox.Font,
            e.Bounds,
            colors.InputText,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
