using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LocalizationOverrideEditor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        EditorUiStrings.SetLanguage(EditorUiSettings.LoadLanguage());
        EditorUiStrings.SetTheme(EditorUiSettings.LoadTheme());
        Application.ThreadException += (_, eventArgs) => ReportStartupFailure(eventArgs.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) => ReportStartupFailure(eventArgs.ExceptionObject as Exception ?? new Exception(eventArgs.ExceptionObject?.ToString()));
        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        catch (Exception exception)
        {
            ReportStartupFailure(exception);
        }
    }

    private static void ReportStartupFailure(Exception exception)
    {
        var logFile = Path.Combine(AppContext.BaseDirectory, "UELocalizationTool-startup-error.log");
        try
        {
            File.WriteAllText(logFile, $"{DateTime.Now:O}{Environment.NewLine}{exception}");
            MessageBox.Show(
                EditorUiStrings.Format("StartupFailed", logFile, exception.Message),
                EditorUiStrings.Get("AppTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // The log and dialog are diagnostics only; never rethrow during startup.
        }
    }
}

internal sealed class MainForm : Form
{
    private enum TranslationFilter
    {
        All,
        Missing,
        Translated
    }

    private static readonly Encoding Utf16LeBom = new UnicodeEncoding(
        bigEndian: false,
        byteOrderMark: true,
        throwOnInvalidBytes: true);

    private readonly BindingSource _bindingSource = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _searchBox = new();
    private readonly DarkComboBox _cultureBox = new();
    private readonly DarkComboBox _translationStateBox = new();
    private readonly DarkComboBox _defaultCultureBox = new();
    private readonly TextBox _rootBox = new();
    private readonly StatusStrip _statusStrip = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly Button _reloadButton = new();
    private readonly Button _saveButton = new();
    private readonly Button _openButton = new();
    private readonly Button _addCultureButton = new();
    private readonly Button _deleteCultureButton = new();
    private readonly Button _themeToggleButton = new();
    private readonly Label _uiLanguageLabel = new();
    private readonly DarkComboBox _uiLanguageBox = new();
    private TableLayoutPanel _headerPanel = null!;
    private TableLayoutPanel _rootPanel = null!;
    private TableLayoutPanel _toolbarPanel = null!;
    private Label _headerTitleLabel = null!;
    private Label _rootDirectoryLabel = null!;
    private Label _filterLanguageLabel = null!;
    private Label _filterStateLabel = null!;
    private Label _defaultCultureLabel = null!;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private string _rootDirectory = "";
    private string _overridesDirectory = "";
    private string _languagesFile = "";
    private string _gameFile = "";
    private LanguagesDocument _languages = new();
    private LocalizationDocument _document = new();
    private List<LocalizationEntryRow> _rows = [];
    private List<string> _cultures = [];
    private readonly Stack<List<CellUndoChange>> _undoStack = new();
    private CellUndoChange? _pendingEdit;
    private bool _isApplyingUndo;
    private bool _isUpdatingUi;
    private bool _hasUnsavedChanges;
    private bool _hasLoadedDocument;
    private string _languagesFingerprint = "";
    private string _gameFingerprint = "";
    private TranslationFilter _translationFilter = TranslationFilter.All;
    private string? _cultureFilter;
    private string? _statusMessageKey;
    private object[] _statusMessageArgs = [];

    private sealed record CellUndoChange(LocalizationEntryRow Row, string Culture, string OldValue, string NewValue);
    private sealed record JsonFileSnapshot(string Text, string Fingerprint);

    public MainForm()
    {
        EditorUiStrings.SetLanguage(EditorUiSettings.LoadLanguage());
        EditorUiStrings.SetTheme(EditorUiSettings.LoadTheme());
        Icon = LoadWindowIcon();
        Width = 1240;
        Height = 720;
        MinimumSize = new Size(1020, 520);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        ApplyUiLanguage();
        SetDocumentActionsEnabled(false);
        Load += (_, _) => LoadFromDetectedRoot();
        FormClosing += MainForm_FormClosing;
    }

    private static Icon LoadWindowIcon()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    private void BuildLayout()
    {
        Font = new Font("Microsoft YaHei UI", 9F);

        static Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(0, 0, 4, 0)
            };
        }

        static void StyleInput(Control control)
        {
            control.Height = 32;
            control.Margin = new Padding(0, 0, 16, 0);
        }

        _rootBox.ReadOnly = false;
        _rootBox.Dock = DockStyle.Fill;
        _rootBox.BorderStyle = BorderStyle.FixedSingle;
        StyleInput(_rootBox);
        _rootBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                LoadFromRoot(_rootBox.Text);
            }
        };

        _searchBox.Dock = DockStyle.Fill;
        StyleInput(_searchBox);
        _searchBox.TextChanged += (_, _) => ApplyFilter();

        _cultureBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _cultureBox.Dock = DockStyle.Fill;
        StyleInput(_cultureBox);
        _cultureBox.SelectedIndexChanged += (_, _) =>
        {
            if (_isUpdatingUi)
            {
                return;
            }

            _cultureFilter = _cultureBox.SelectedIndex <= 0 ? null : _cultureBox.SelectedItem as string;
            ApplyFilter();
        };

        _translationStateBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _translationStateBox.Dock = DockStyle.Fill;
        StyleInput(_translationStateBox);
        _translationStateBox.SelectedIndexChanged += (_, _) =>
        {
            if (_isUpdatingUi)
            {
                return;
            }

            _translationFilter = _translationStateBox.SelectedIndex switch
            {
                1 => TranslationFilter.Missing,
                2 => TranslationFilter.Translated,
                _ => TranslationFilter.All
            };
            ApplyFilter();
        };

        _defaultCultureBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _defaultCultureBox.Dock = DockStyle.Fill;
        StyleInput(_defaultCultureBox);
        _defaultCultureBox.SelectedIndexChanged += (_, _) =>
        {
            if (!_isUpdatingUi && _defaultCultureBox.SelectedItem is string selected && selected != _languages.DefaultCulture)
            {
                MarkDirty("StatusDefaultCultureChanged");
            }
        };

        ConfigureHeaderButton(_themeToggleButton);
        _themeToggleButton.Click += (_, _) => ToggleUiTheme();

        _uiLanguageLabel.AutoSize = true;
        _uiLanguageLabel.Text = "中/En";
        _uiLanguageLabel.TextAlign = ContentAlignment.MiddleLeft;
        _uiLanguageLabel.Margin = new Padding(8, 6, 4, 0);
        _uiLanguageLabel.Font = new Font("Microsoft YaHei UI", 9F);

        _uiLanguageBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _uiLanguageBox.Width = 88;
        _uiLanguageBox.Margin = new Padding(0, 2, 0, 0);
        StyleInput(_uiLanguageBox);
        _uiLanguageBox.Items.AddRange(["中文", "English"]);
        _uiLanguageBox.SelectedIndex = EditorUiStrings.Current == EditorUiLanguage.English ? 1 : 0;
        _uiLanguageBox.SelectedIndexChanged += (_, _) =>
        {
            if (_isUpdatingUi)
            {
                return;
            }

            var nextLanguage = _uiLanguageBox.SelectedIndex == 1
                ? EditorUiLanguage.English
                : EditorUiLanguage.Chinese;
            if (nextLanguage == EditorUiStrings.Current)
            {
                return;
            }

            EditorUiStrings.SetLanguage(nextLanguage);
            EditorUiSettings.SaveLanguage(nextLanguage);
            ApplyUiLanguage();
        };

        StyleButton(_openButton);
        _openButton.Click += (_, _) => ChooseRootDirectory();

        StyleButton(_reloadButton);
        _reloadButton.Click += (_, _) => LoadFromRoot(_rootBox.Text);

        StyleButton(_saveButton, primary: true);
        _saveButton.Click += (_, _) => SaveDocuments();

        StyleButton(_addCultureButton);
        _addCultureButton.Click += (_, _) => AddCulture();

        StyleButton(_deleteCultureButton);
        _deleteCultureButton.Click += (_, _) => DeleteCulture();

        _headerPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 56,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(20, 12, 20, 12)
        };
        _headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _headerTitleLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
            Margin = new Padding(0, 0, 16, 0)
        };

        var headerActionsPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        headerActionsPanel.Controls.Add(_themeToggleButton);
        headerActionsPanel.Controls.Add(_uiLanguageLabel);
        headerActionsPanel.Controls.Add(_uiLanguageBox);

        _headerPanel.Controls.Add(_headerTitleLabel, 0, 0);
        _headerPanel.Controls.Add(headerActionsPanel, 1, 0);

        _rootDirectoryLabel = CreateLabel("");
        _rootPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 56,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(16, 14, 16, 10)
        };
        _rootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _rootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _rootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _rootPanel.Controls.Add(_rootDirectoryLabel, 0, 0);
        _rootPanel.Controls.Add(_rootBox, 1, 0);
        _rootPanel.Controls.Add(_openButton, 2, 0);

        _toolbarPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 104,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(16, 4, 16, 12)
        };
        _toolbarPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _toolbarPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        _toolbarPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var filterPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 1,
            Padding = new Padding(0, 4, 0, 4)
        };
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 136));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 136));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 136));
        filterPanel.Controls.Add(_searchBox, 0, 0);
        _filterLanguageLabel = CreateLabel("");
        filterPanel.Controls.Add(_filterLanguageLabel, 1, 0);
        filterPanel.Controls.Add(_cultureBox, 2, 0);
        _filterStateLabel = CreateLabel("");
        filterPanel.Controls.Add(_filterStateLabel, 3, 0);
        filterPanel.Controls.Add(_translationStateBox, 4, 0);
        _defaultCultureLabel = CreateLabel("");
        filterPanel.Controls.Add(_defaultCultureLabel, 5, 0);
        filterPanel.Controls.Add(_defaultCultureBox, 6, 0);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };
        actionPanel.Controls.Add(_reloadButton);
        actionPanel.Controls.Add(_addCultureButton);
        actionPanel.Controls.Add(_deleteCultureButton);
        actionPanel.Controls.Add(_saveButton);

        _toolbarPanel.Controls.Add(filterPanel, 0, 0);
        _toolbarPanel.Controls.Add(actionPanel, 0, 1);

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _grid.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F);
        _grid.RowTemplate.Height = 38;
        _grid.ColumnHeadersHeight = 40;
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _grid.MultiSelect = true;
        _grid.DataSource = _bindingSource;
        _grid.CellPainting += Grid_CellPainting;
        _grid.CurrentCellChanged += Grid_CurrentCellChanged;
        _grid.KeyDown += Grid_KeyDown;
        _grid.CellBeginEdit += Grid_CellBeginEdit;
        _grid.CellEndEdit += Grid_CellEndEdit;
        _grid.CellValueChanged += (_, _) =>
        {
            if (_isUpdatingUi)
            {
                return;
            }
            MarkDirty();
        };
        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty)
            {
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };

        _statusStrip.Dock = DockStyle.Bottom;
        _statusStrip.SizingGrip = false;
        _statusLabel.Spring = true;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusStrip.Items.Add(_statusLabel);

        Controls.Add(_grid);
        Controls.Add(_statusStrip);
        Controls.Add(_toolbarPanel);
        Controls.Add(_rootPanel);
        Controls.Add(_headerPanel);
    }

    private void ConfigureHeaderButton(Button button)
    {
        button.AutoSize = false;
        button.Height = 32;
        button.Width = 88;
        button.Margin = new Padding(8, 0, 0, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.BorderSize = 1;
    }

    private void StyleButton(Button button, bool primary = false)
    {
        var colors = EditorUiThemePalette.Get(EditorUiStrings.CurrentTheme);
        button.AutoSize = false;
        button.Height = 32;
        button.Width = 92;
        button.Margin = new Padding(4, 0, 0, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;

        if (primary)
        {
            button.BackColor = colors.PrimaryButtonBackground;
            button.ForeColor = colors.PrimaryButtonText;
            button.FlatAppearance.BorderSize = 0;
        }
        else
        {
            button.BackColor = colors.SecondaryButtonBackground;
            button.ForeColor = colors.SecondaryButtonText;
            button.FlatAppearance.BorderColor = colors.SecondaryButtonBorder;
            button.FlatAppearance.BorderSize = 1;
        }
    }

    private void StyleHeaderButton(Button button)
    {
        var colors = EditorUiThemePalette.Get(EditorUiStrings.CurrentTheme);
        button.BackColor = colors.SecondaryButtonBackground;
        button.ForeColor = colors.SecondaryButtonText;
        button.FlatAppearance.BorderColor = colors.SecondaryButtonBorder;
        button.FlatAppearance.BorderSize = 1;
    }

    private void ApplyInputTheme(Control control)
    {
        var colors = EditorUiThemePalette.Get(EditorUiStrings.CurrentTheme);
        control.BackColor = colors.InputBackground;
        control.ForeColor = colors.InputText;

        if (control is TextBox textBox)
        {
            textBox.BorderStyle = EditorUiStrings.CurrentTheme == EditorUiTheme.Dark
                ? BorderStyle.None
                : BorderStyle.FixedSingle;
        }

        if (control is DarkComboBox darkComboBox)
        {
            darkComboBox.ApplyTheme();
        }
    }

    private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (EditorUiStrings.CurrentTheme != EditorUiTheme.Dark || e.Graphics is null)
        {
            return;
        }

        var colors = EditorUiThemePalette.Get(EditorUiTheme.Dark);
        var graphics = e.Graphics;

        if (e.RowIndex == -1 && e.ColumnIndex == -1)
        {
            using var backBrush = new SolidBrush(colors.GridHeaderBackground);
            graphics.FillRectangle(backBrush, e.CellBounds);
            e.Handled = true;
            return;
        }

        if (e.RowIndex == -1 && e.ColumnIndex >= 0)
        {
            using var backBrush = new SolidBrush(colors.GridHeaderBackground);
            graphics.FillRectangle(backBrush, e.CellBounds);

            var text = e.FormattedValue?.ToString() ?? string.Empty;
            TextRenderer.DrawText(
                graphics,
                text,
                e.CellStyle?.Font ?? _grid.Font,
                e.CellBounds,
                colors.SecondaryText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            e.Handled = true;
            return;
        }

        if (e.ColumnIndex == -1 && e.RowIndex >= 0)
        {
            using var backBrush = new SolidBrush(colors.GridHeaderBackground);
            graphics.FillRectangle(backBrush, e.CellBounds);

            if (_grid.CurrentCell?.RowIndex == e.RowIndex)
            {
                DrawRowHeaderArrow(graphics, e.CellBounds, colors.PrimaryText);
            }

            e.Handled = true;
        }
    }

    private int _rowHeaderArrowRow = -1;

    private void Grid_CurrentCellChanged(object? sender, EventArgs e)
    {
        if (EditorUiStrings.CurrentTheme != EditorUiTheme.Dark)
        {
            return;
        }

        var currentRow = _grid.CurrentCell?.RowIndex ?? -1;
        if (_rowHeaderArrowRow >= 0
            && _rowHeaderArrowRow < _grid.Rows.Count
            && _rowHeaderArrowRow != currentRow)
        {
            _grid.InvalidateRow(_rowHeaderArrowRow);
        }

        if (currentRow >= 0 && currentRow < _grid.Rows.Count)
        {
            _grid.InvalidateRow(currentRow);
        }

        _rowHeaderArrowRow = currentRow;
    }

    private static void DrawRowHeaderArrow(Graphics graphics, Rectangle bounds, Color color)
    {
        var centerY = bounds.Top + bounds.Height / 2;
        var tipX = bounds.Left + Math.Max(6, bounds.Width / 2);
        Point[] arrow =
        [
            new(tipX - 5, centerY - 5),
            new(tipX + 1, centerY),
            new(tipX - 5, centerY + 5)
        ];
        using var brush = new SolidBrush(color);
        graphics.FillPolygon(brush, arrow);
    }

    private void ApplyGridBorderTheme()
    {
        var colors = EditorUiThemePalette.Get(EditorUiStrings.CurrentTheme);

        if (EditorUiStrings.CurrentTheme == EditorUiTheme.Dark)
        {
            _grid.CellBorderStyle = DataGridViewCellBorderStyle.None;
            _grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            _grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            _grid.AdvancedColumnHeadersBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
            _grid.AdvancedRowHeadersBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
            _grid.GridColor = colors.GridBackground;
            return;
        }

        _grid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
        _grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        _grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        _grid.AdvancedColumnHeadersBorderStyle.All = DataGridViewAdvancedCellBorderStyle.Single;
        _grid.AdvancedRowHeadersBorderStyle.All = DataGridViewAdvancedCellBorderStyle.Single;
        _grid.GridColor = colors.GridLine;
    }

    private const int WmSetRedraw = 0x000B;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private static void SetControlRedraw(Control control, bool redraw)
    {
        if (!control.IsHandleCreated)
        {
            return;
        }

        SendMessage(control.Handle, WmSetRedraw, redraw ? 1 : 0, 0);
        if (redraw)
        {
            control.Invalidate(true);
        }
    }

    private void ToggleUiTheme()
    {
        var nextTheme = EditorUiStrings.CurrentTheme == EditorUiTheme.Light
            ? EditorUiTheme.Dark
            : EditorUiTheme.Light;
        EditorUiStrings.SetTheme(nextTheme);
        EditorUiSettings.SaveTheme(nextTheme);
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        SuspendLayout();
        _grid.SuspendLayout();
        SetControlRedraw(this, false);
        try
        {
            ApplyThemeCore();
        }
        finally
        {
            SetControlRedraw(this, true);
            _grid.ResumeLayout(false);
            ResumeLayout(true);
            Refresh();
        }
    }

    private void ApplyThemeCore()
    {
        var colors = EditorUiThemePalette.Get(EditorUiStrings.CurrentTheme);

        BackColor = colors.FormBackground;

        _headerPanel.BackColor = colors.PanelBackground;
        _rootPanel.BackColor = colors.SurfaceBackground;
        _toolbarPanel.BackColor = colors.SurfaceBackground;

        _headerTitleLabel.ForeColor = colors.PrimaryText;
        _uiLanguageLabel.ForeColor = colors.SecondaryText;
        _rootDirectoryLabel.ForeColor = colors.SecondaryText;
        _filterLanguageLabel.ForeColor = colors.SecondaryText;
        _filterStateLabel.ForeColor = colors.SecondaryText;
        _defaultCultureLabel.ForeColor = colors.SecondaryText;

        ApplyInputTheme(_rootBox);
        ApplyInputTheme(_searchBox);
        ApplyInputTheme(_cultureBox);
        ApplyInputTheme(_translationStateBox);
        ApplyInputTheme(_defaultCultureBox);
        ApplyInputTheme(_uiLanguageBox);

        StyleButton(_openButton);
        StyleButton(_reloadButton);
        StyleButton(_addCultureButton);
        StyleButton(_deleteCultureButton);
        StyleButton(_saveButton, primary: true);
        StyleHeaderButton(_themeToggleButton);
        if (EditorUiStrings.CurrentTheme == EditorUiTheme.Dark)
        {
            _themeToggleButton.FlatAppearance.BorderColor = colors.AccentBorder;
        }

        _themeToggleButton.Text = EditorUiStrings.GetToggleThemeLabel();
        // 「中/En」标签与下拉选项固定，不随 UI 语言切换。

        _grid.BackgroundColor = colors.GridBackground;
        _grid.GridColor = colors.GridLine;
        _grid.BorderStyle = EditorUiStrings.CurrentTheme == EditorUiTheme.Dark
            ? BorderStyle.None
            : BorderStyle.FixedSingle;
        _grid.DefaultCellStyle.BackColor = colors.GridBackground;
        _grid.DefaultCellStyle.ForeColor = colors.InputText;
        _grid.DefaultCellStyle.SelectionBackColor = colors.GridSelectionBackground;
        _grid.DefaultCellStyle.SelectionForeColor = colors.GridSelectionText;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = colors.GridAlternateBackground;
        _grid.AlternatingRowsDefaultCellStyle.ForeColor = colors.InputText;
        _grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = colors.GridSelectionBackground;
        _grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = colors.GridSelectionText;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = colors.GridHeaderBackground;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = colors.SecondaryText;
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = colors.GridHeaderBackground;
        _grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = colors.SecondaryText;
        _grid.RowHeadersDefaultCellStyle.BackColor = colors.GridHeaderBackground;
        _grid.RowHeadersDefaultCellStyle.ForeColor = colors.SecondaryText;
        _grid.RowHeadersDefaultCellStyle.SelectionBackColor = colors.GridHeaderBackground;
        _grid.RowHeadersDefaultCellStyle.SelectionForeColor = colors.SecondaryText;
        ApplyGridBorderTheme();
        _grid.EnableHeadersVisualStyles = false;

        _statusStrip.BackColor = colors.PanelBackground;
        _statusLabel.ForeColor = colors.SecondaryText;
    }

    private void ConfigureGridColumns()
    {
        _grid.Columns.Clear();

        foreach (var culture in _cultures)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = $"Translation_{culture}",
                HeaderText = GetCultureHeaderText(culture),
                FillWeight = 130
            });
        }
    }

    private static string GetCultureHeaderText(string culture)
    {
        return $"{culture} - {EditorUiStrings.GetCultureDisplayName(culture)}";
    }

    private void ApplyUiLanguage()
    {
        Text = EditorUiStrings.Get("AppTitle");
        _headerTitleLabel.Text = EditorUiStrings.Get("AppTitle");
        _uiLanguageLabel.Text = "中/En";
        _rootDirectoryLabel.Text = EditorUiStrings.Get("RootDirectoryLabel");
        _filterLanguageLabel.Text = EditorUiStrings.Get("FilterLanguage");
        _filterStateLabel.Text = EditorUiStrings.Get("FilterState");
        _defaultCultureLabel.Text = EditorUiStrings.Get("DefaultCulture");
        _searchBox.PlaceholderText = EditorUiStrings.Get("SearchPlaceholder");
        _rootBox.PlaceholderText = EditorUiStrings.Get("RootPathPlaceholder");
        _openButton.Text = EditorUiStrings.Get("Browse");
        _reloadButton.Text = EditorUiStrings.Get("Reload");
        _saveButton.Text = EditorUiStrings.Get("Save");
        _addCultureButton.Text = EditorUiStrings.Get("AddCulture");
        _deleteCultureButton.Text = EditorUiStrings.Get("DeleteCulture");

        _isUpdatingUi = true;
        try
        {
            if (_uiLanguageBox.Items.Count == 0)
            {
                _uiLanguageBox.Items.AddRange(["中文", "English"]);
            }

            _uiLanguageBox.SelectedIndex = EditorUiStrings.Current == EditorUiLanguage.English ? 1 : 0;
        }
        finally
        {
            _isUpdatingUi = false;
        }

        RefreshFilterCombos();
        if (_hasLoadedDocument)
        {
            ConfigureGridColumns();
        }

        RefreshStatusMessage();
        ApplyTheme();
    }

    private void RefreshFilterCombos()
    {
        _isUpdatingUi = true;
        try
        {
            _translationStateBox.Items.Clear();
            _translationStateBox.Items.AddRange([
                EditorUiStrings.Get("FilterAll"),
                EditorUiStrings.Get("FilterMissing"),
                EditorUiStrings.Get("FilterTranslated")
            ]);
            _translationStateBox.SelectedIndex = _translationFilter switch
            {
                TranslationFilter.Missing => 1,
                TranslationFilter.Translated => 2,
                _ => 0
            };

            var selectedCulture = _cultureFilter;
            _cultureBox.Items.Clear();
            _cultureBox.Items.Add(EditorUiStrings.Get("FilterAll"));
            foreach (var culture in _cultures)
            {
                _cultureBox.Items.Add(culture);
            }

            if (!string.IsNullOrWhiteSpace(selectedCulture) && _cultures.Contains(selectedCulture))
            {
                _cultureBox.SelectedItem = selectedCulture;
            }
            else
            {
                _cultureFilter = null;
                _cultureBox.SelectedIndex = 0;
            }
        }
        finally
        {
            _isUpdatingUi = false;
        }
    }

    private void SetStatusMessage(string key, params object[] args)
    {
        _statusMessageKey = key;
        _statusMessageArgs = args;
        RefreshStatusMessage();
    }

    private void RefreshStatusMessage()
    {
        if (string.IsNullOrWhiteSpace(_statusMessageKey))
        {
            return;
        }

        _statusLabel.Text = _statusMessageArgs.Length == 0
            ? EditorUiStrings.Get(_statusMessageKey)
            : EditorUiStrings.Format(_statusMessageKey, _statusMessageArgs);
    }

    private void LoadFromDetectedRoot()
    {
        var lastRoot = EditorUiSettings.LoadLastRootDirectory();
        if (string.IsNullOrWhiteSpace(lastRoot))
        {
            ShowEmptyStartupState();
            return;
        }

        if (!IsSavedRootPathStillValid(lastRoot))
        {
            EditorUiSettings.SaveLastRootDirectory("");
            ShowEmptyStartupState();
            return;
        }

        _rootBox.Text = lastRoot;
        TryLoadFromRoot(lastRoot, showErrorOnFailure: false);
    }

    private void ShowEmptyStartupState()
    {
        _rootBox.Text = "";
        SetDocumentActionsEnabled(false);
        SetStatusMessage("StatusSelectDirectory");
    }

    private static bool IsSavedRootPathStillValid(string path)
    {
        try
        {
            var normalized = NormalizeUserPath(path);
            if (string.IsNullOrWhiteSpace(normalized) || !Directory.Exists(normalized))
            {
                return false;
            }

            var fullPath = Path.GetFullPath(normalized);
            if (IsValidOverridesDirectory(fullPath))
            {
                return true;
            }

            return IsValidOverridesDirectory(Path.Combine(fullPath, "LocalizationOverrides"));
        }
        catch
        {
            return false;
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_hasUnsavedChanges)
        {
            return;
        }

        var result = MessageBox.Show(
            this,
            EditorUiStrings.Get("ConfirmExitMessage"),
            EditorUiStrings.Get("ConfirmExit"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
        {
            e.Cancel = true;
        }
    }

    private void MarkDirty(string? messageKey = null, params object[] args)
    {
        if (_isUpdatingUi || !_hasLoadedDocument)
        {
            return;
        }

        _hasUnsavedChanges = true;
        if (string.IsNullOrWhiteSpace(messageKey))
        {
            SetStatusMessage("StatusUnsaved");
        }
        else
        {
            SetStatusMessage(messageKey, args);
        }
    }

    private void ClearDirty()
    {
        _hasUnsavedChanges = false;
    }

    private void SetDocumentActionsEnabled(bool enabled)
    {
        _saveButton.Enabled = enabled;
        _addCultureButton.Enabled = enabled;
        _deleteCultureButton.Enabled = enabled;
        _grid.Enabled = enabled;
        _cultureBox.Enabled = enabled;
        _translationStateBox.Enabled = enabled;
        _defaultCultureBox.Enabled = enabled;
        _searchBox.Enabled = enabled;
    }

    private static string ResolveOverridesDirectory(string inputPath)
    {
        var normalizedInput = NormalizeUserPath(inputPath);
        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            throw new DirectoryNotFoundException(
                "\u8bf7\u8f93\u5165\u6216\u9009\u62e9 LocalizationOverrides \u76ee\u5f55\u3001\u9879\u76ee\u6839\u76ee\u5f55\u6216\u6253\u5305\u76ee\u5f55\u3002");
        }

        var startDirectory = GetSearchStartDirectory(normalizedInput);

        if (IsValidOverridesDirectory(startDirectory))
        {
            return Path.GetFullPath(startDirectory);
        }

        var directChild = Path.Combine(startDirectory, "LocalizationOverrides");
        if (IsValidOverridesDirectory(directChild))
        {
            return Path.GetFullPath(directChild);
        }

        var resolved = EnumerateOverridesDirectories(startDirectory, maxDepth: 5)
            .FirstOrDefault(IsValidOverridesDirectory);
        if (!string.IsNullOrWhiteSpace(resolved))
        {
            return Path.GetFullPath(resolved);
        }

        throw new DirectoryNotFoundException(
            "\u672a\u5728\u8be5\u8def\u5f84\u4e0b\u627e\u5230\u5305\u542b languages.json \u4e0e Game.json \u7684 LocalizationOverrides \u76ee\u5f55\u3002\r\n\r\n" +
            "\u8bf7\u8f93\u5165\u6216\u9009\u62e9\uff1a\r\n" +
            "- \u9879\u76ee\u6839\u76ee\u5f55\r\n" +
            "- \u6253\u5305\u6839\u76ee\u5f55\r\n" +
            "- \u76f4\u63a5\u5305\u542b languages.json \u548c Game.json \u7684 LocalizationOverrides \u76ee\u5f55");
    }

    private static string NormalizeUserPath(string path)
    {
        return Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
    }

    private static string GetSearchStartDirectory(string inputPath)
    {
        if (Directory.Exists(inputPath))
        {
            return Path.GetFullPath(inputPath);
        }

        if (File.Exists(inputPath))
        {
            return Path.GetDirectoryName(Path.GetFullPath(inputPath)) ?? Directory.GetCurrentDirectory();
        }

        var extension = Path.GetExtension(inputPath);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            var parent = Path.GetDirectoryName(inputPath);
            if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
            {
                return Path.GetFullPath(parent);
            }
        }

        throw new DirectoryNotFoundException($"\u8def\u5f84\u4e0d\u5b58\u5728\uff1a{inputPath}");
    }

    private static IEnumerable<string> EnumerateOverridesDirectories(string root, int maxDepth)
    {
        if (!Directory.Exists(root) || maxDepth < 0)
        {
            yield break;
        }

        var queue = new Queue<(string Directory, int Depth)>();
        queue.Enqueue((root, 0));

        while (queue.Count > 0)
        {
            var (directory, depth) = queue.Dequeue();
            IEnumerable<string> children;
            try
            {
                children = Directory.EnumerateDirectories(directory);
            }
            catch
            {
                continue;
            }

            foreach (var child in children)
            {
                if (string.Equals(Path.GetFileName(child), "LocalizationOverrides", StringComparison.OrdinalIgnoreCase))
                {
                    yield return child;
                }

                if (depth < maxDepth)
                {
                    queue.Enqueue((child, depth + 1));
                }
            }
        }
    }

    private static bool IsValidOverridesDirectory(string directory)
    {
        return Directory.Exists(directory)
            && File.Exists(Path.Combine(directory, "languages.json"))
            && File.Exists(Path.Combine(directory, "Game.json"));
    }

    private void ChooseRootDirectory()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = EditorUiStrings.Get("ChooseDirectoryDescription"),
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_overridesDirectory) ? _overridesDirectory : Directory.GetCurrentDirectory()
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            TryLoadFromRoot(dialog.SelectedPath, showErrorOnFailure: true);
        }
    }

    private void LoadFromRoot(string rootDirectory) =>
        TryLoadFromRoot(rootDirectory, showErrorOnFailure: true);

    private void TryLoadFromRoot(string rootDirectory, bool showErrorOnFailure)
    {
        if (_hasLoadedDocument && _hasUnsavedChanges)
        {
            var discard = MessageBox.Show(
                this,
                EditorUiStrings.Get("ConfirmReloadMessage"),
                EditorUiStrings.Get("ConfirmReload"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (discard != DialogResult.Yes)
            {
                _rootBox.Text = _overridesDirectory;
                return;
            }
        }

        try
        {
            // Build the complete candidate state first. Live paths, models,
            // controls, and fingerprints change only after both files validate.
            var candidateOverridesDirectory = ResolveOverridesDirectory(rootDirectory);
            var candidateLanguagesFile = Path.Combine(candidateOverridesDirectory, "languages.json");
            var candidateGameFile = Path.Combine(candidateOverridesDirectory, "Game.json");
            var languagesSnapshot = ReadUtf16LeBomJson(candidateLanguagesFile);
            var candidateLanguages = ParseLanguagesDocument(languagesSnapshot.Text, candidateLanguagesFile);
            var gameSnapshot = ReadUtf16LeBomJson(candidateGameFile);
            var candidateDocument = ParseLocalizationDocument(gameSnapshot.Text, candidateGameFile, candidateLanguages);
            var candidateCultures = new List<string>(candidateLanguages.Cultures);
            var candidateRows = candidateDocument.Entries.Select(LocalizationEntryRow.FromEntry).ToList();

            foreach (var row in candidateRows)
            {
                foreach (var culture in candidateCultures)
                {
                    row.Translations.TryAdd(culture, "");
                }
            }

            _isUpdatingUi = true;
            try
            {
                _rootDirectory = rootDirectory;
                _overridesDirectory = candidateOverridesDirectory;
                _languagesFile = candidateLanguagesFile;
                _gameFile = candidateGameFile;
                _languages = candidateLanguages;
                _document = candidateDocument;
                _cultures = candidateCultures;
                _rows = candidateRows;
                _languagesFingerprint = languagesSnapshot.Fingerprint;
                _gameFingerprint = gameSnapshot.Fingerprint;
                _hasLoadedDocument = true;
                _undoStack.Clear();
                _pendingEdit = null;

                RefreshCultureControls();
                ConfigureGridColumns();
                ApplyFilter();
                _rootBox.Text = _overridesDirectory;
                EditorUiSettings.SaveLastRootDirectory(_overridesDirectory);
                SetDocumentActionsEnabled(true);
                ClearDirty();
                SetStatusMessage("StatusLoaded", _rows.Count, _languages.DefaultCulture, _overridesDirectory);
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }
        catch (Exception ex)
        {
            if (!_hasLoadedDocument)
            {
                SetDocumentActionsEnabled(false);
                SetStatusMessage(showErrorOnFailure ? "StatusNoValidJson" : "StatusSelectDirectory");
            }
            else
            {
                _rootBox.Text = _overridesDirectory;
                SetStatusMessage("StatusLoadFailedKeepPrevious", _overridesDirectory);
            }

            if (showErrorOnFailure)
            {
                MessageBox.Show(this, ex.Message, EditorUiStrings.Get("LoadFailed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private static JsonFileSnapshot ReadUtf16LeBomJson(string filename)
    {
        var bytes = File.ReadAllBytes(filename);
        if (bytes.Length < 2 || bytes[0] != 0xFF || bytes[1] != 0xFE)
        {
            throw new InvalidDataException($"{filename} \u5fc5\u987b\u662f UTF-16 LE BOM \u7f16\u7801\u7684 JSON \u6587\u4ef6\u3002\u8bf7\u7528 Unreal \u63a7\u5236\u53f0\u547d\u4ee4 LocalizationOverrides.Generate \u91cd\u65b0\u751f\u6210\uff0c\u6216\u7528 UTF-16 LE BOM \u683c\u5f0f\u4fdd\u5b58\u3002");
        }

        return new JsonFileSnapshot(
            Utf16LeBom.GetString(bytes, 2, bytes.Length - 2),
            Convert.ToHexString(SHA256.HashData(bytes)));
    }

    private static LanguagesDocument ParseLanguagesDocument(string json, string filename)
    {
        using var document = ParseJson(json, filename);
        var root = RequireObject(document.RootElement, filename);
        EnsureAllowedProperties(root, filename, "version", "defaultCulture", "cultures");

        var version = RequireInt32(root, "version", filename);
        if (version != 1)
        {
            throw new InvalidDataException($"{filename}: version \u5fc5\u987b\u4e3a 1\uff0c\u5f53\u524d\u4e3a {version}\u3002");
        }

        var culturesElement = RequireProperty(root, "cultures", JsonValueKind.Array, filename);
        var cultures = new List<string>();
        var seenCultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cultureIndex = 0;
        foreach (var cultureElement in culturesElement.EnumerateArray())
        {
            if (cultureElement.ValueKind != JsonValueKind.String)
            {
                throw new InvalidDataException($"{filename}: cultures[{cultureIndex}] \u5fc5\u987b\u662f\u5b57\u7b26\u4e32\u3002");
            }

            var culture = cultureElement.GetString() ?? "";
            if (!IsValidCultureCode(culture))
            {
                throw new InvalidDataException($"{filename}: cultures[{cultureIndex}] \u5305\u542b\u65e0\u6548\u8bed\u7cfb '{culture}'\u3002");
            }
            if (!seenCultures.Add(culture))
            {
                throw new InvalidDataException($"{filename}: \u8bed\u7cfb '{culture}' \u91cd\u590d\uff08\u4e0d\u533a\u5206\u5927\u5c0f\u5199\uff09\u3002");
            }

            cultures.Add(culture);
            cultureIndex++;
        }
        if (cultures.Count == 0)
        {
            throw new InvalidDataException($"{filename}: cultures \u81f3\u5c11\u9700\u8981\u5305\u542b\u4e00\u4e2a\u8bed\u7cfb\u3002");
        }

        var requestedDefaultCulture = RequireNonEmptyString(root, "defaultCulture", filename);
        var defaultCulture = FindDeclaredCulture(cultures, requestedDefaultCulture)
            ?? throw new InvalidDataException($"{filename}: defaultCulture '{requestedDefaultCulture}' \u672a\u5728 cultures \u4e2d\u58f0\u660e\u3002");

        return new LanguagesDocument
        {
            Version = version,
            DefaultCulture = defaultCulture,
            Cultures = cultures
        };
    }

    private static LocalizationDocument ParseLocalizationDocument(string json, string filename, LanguagesDocument languages)
    {
        using var document = ParseJson(json, filename);
        var root = RequireObject(document.RootElement, filename);
        EnsureAllowedProperties(root, filename, "version", "target", "nativeCulture", "entries");

        var version = RequireInt32(root, "version", filename);
        if (version != 1)
        {
            throw new InvalidDataException($"{filename}: version \u5fc5\u987b\u4e3a 1\uff0c\u5f53\u524d\u4e3a {version}\u3002");
        }

        var target = RequireNonEmptyString(root, "target", filename);
        if (!string.Equals(target, "Game", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"{filename}: \u8be5\u5de5\u5177\u53ea\u652f\u6301 target=\"Game\"\uff0c\u5f53\u524d\u4e3a '{target}'\u3002");
        }

        var requestedNativeCulture = RequireNonEmptyString(root, "nativeCulture", filename);
        var nativeCulture = FindDeclaredCulture(languages.Cultures, requestedNativeCulture)
            ?? throw new InvalidDataException($"{filename}: nativeCulture '{requestedNativeCulture}' \u672a\u5728 languages.json cultures \u4e2d\u58f0\u660e\u3002");

        var entriesElement = RequireProperty(root, "entries", JsonValueKind.Array, filename);
        var entries = new List<LocalizationEntry>();
        var identities = new HashSet<(string Namespace, string Key)>();
        var entryIndex = 0;
        foreach (var entryElement in entriesElement.EnumerateArray())
        {
            var context = $"{filename}: entries[{entryIndex}]";
            var entryObject = RequireObject(entryElement, context);
            EnsureAllowedProperties(entryObject, context, "namespace", "key", "source", "translations");

            var entryNamespace = RequireString(entryObject, "namespace", context);
            var key = RequireNonEmptyString(entryObject, "key", context);
            var source = RequireNonEmptyString(entryObject, "source", context);
            if (!identities.Add((entryNamespace, key)))
            {
                throw new InvalidDataException($"{context}: \u91cd\u590d\u7684\u6587\u672c\u8eab\u4efd Namespace='{entryNamespace}', Key='{key}'\u3002");
            }

            var translationsElement = RequireProperty(entryObject, "translations", JsonValueKind.Object, context);
            EnsureUniqueProperties(translationsElement, $"{context}.translations", StringComparer.OrdinalIgnoreCase);
            var translations = new Dictionary<string, string>();
            foreach (var translationProperty in translationsElement.EnumerateObject())
            {
                var declaredCulture = FindDeclaredCulture(languages.Cultures, translationProperty.Name)
                    ?? throw new InvalidDataException($"{context}: \u7ffb\u8bd1\u8bed\u7cfb '{translationProperty.Name}' \u672a\u5728 languages.json cultures \u4e2d\u58f0\u660e\u3002");
                if (translationProperty.Value.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidDataException($"{context}: translations.{translationProperty.Name} \u5fc5\u987b\u662f\u5b57\u7b26\u4e32\u3002");
                }
                translations.Add(declaredCulture, translationProperty.Value.GetString() ?? "");
            }

            entries.Add(new LocalizationEntry
            {
                Namespace = entryNamespace,
                Key = key,
                Source = source,
                Translations = translations
            });
            entryIndex++;
        }

        return new LocalizationDocument
        {
            Version = version,
            Target = target,
            NativeCulture = nativeCulture,
            Entries = entries
        };
    }

    private static JsonDocument ParseJson(string json, string filename)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"{filename}: JSON \u683c\u5f0f\u65e0\u6548\uff1a{exception.Message}", exception);
        }
    }

    private static JsonElement RequireObject(JsonElement element, string context)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"{context}: \u5fc5\u987b\u662f JSON \u5bf9\u8c61\u3002");
        }
        EnsureUniqueProperties(element, context, StringComparer.Ordinal);
        return element;
    }

    private static void EnsureUniqueProperties(JsonElement element, string context, StringComparer comparer)
    {
        var propertyNames = new HashSet<string>(comparer);
        foreach (var property in element.EnumerateObject())
        {
            if (!propertyNames.Add(property.Name))
            {
                throw new InvalidDataException($"{context}: \u5c5e\u6027 '{property.Name}' \u91cd\u590d\u3002");
            }
        }
    }

    private static void EnsureAllowedProperties(JsonElement element, string context, params string[] allowedProperties)
    {
        var allowed = new HashSet<string>(allowedProperties, StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                throw new InvalidDataException($"{context}: \u4e0d\u652f\u6301\u7684\u5c5e\u6027 '{property.Name}'\u3002");
            }
        }
    }

    private static JsonElement RequireProperty(JsonElement element, string propertyName, JsonValueKind expectedKind, string context)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            throw new InvalidDataException($"{context}: \u7f3a\u5c11\u5fc5\u9700\u5c5e\u6027 '{propertyName}'\u3002");
        }
        if (value.ValueKind != expectedKind)
        {
            throw new InvalidDataException($"{context}: '{propertyName}' \u5fc5\u987b\u662f {expectedKind}\u3002");
        }
        return value;
    }

    private static int RequireInt32(JsonElement element, string propertyName, string context)
    {
        var value = RequireProperty(element, propertyName, JsonValueKind.Number, context);
        if (!value.TryGetInt32(out var result))
        {
            throw new InvalidDataException($"{context}: '{propertyName}' \u5fc5\u987b\u662f 32 \u4f4d\u6574\u6570\u3002");
        }
        return result;
    }

    private static string RequireString(JsonElement element, string propertyName, string context)
    {
        return RequireProperty(element, propertyName, JsonValueKind.String, context).GetString() ?? "";
    }

    private static string RequireNonEmptyString(JsonElement element, string propertyName, string context)
    {
        var value = RequireString(element, propertyName, context);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"{context}: '{propertyName}' \u4e0d\u80fd\u4e3a\u7a7a\u3002");
        }
        return value;
    }

    private static string? FindDeclaredCulture(IEnumerable<string> cultures, string requestedCulture)
    {
        return cultures.FirstOrDefault(culture => string.Equals(culture, requestedCulture, StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyFilter()
    {
        var query = _searchBox.Text.Trim();
        IEnumerable<LocalizationEntryRow> filtered = _rows;

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(row =>
                Contains(row.Namespace, query) ||
                Contains(row.Key, query) ||
                Contains(row.Source, query) ||
                row.Translations.Values.Any(value => Contains(value, query)));
        }

        if (!string.IsNullOrWhiteSpace(_cultureFilter))
        {
            filtered = filtered.Where(row => row.Translations.ContainsKey(_cultureFilter));
        }

        if (_translationFilter == TranslationFilter.Missing)
        {
            filtered = string.IsNullOrWhiteSpace(_cultureFilter)
                ? filtered.Where(HasAnyMissingTranslation)
                : filtered.Where(row => IsMissingTranslation(row, _cultureFilter));
        }
        else if (_translationFilter == TranslationFilter.Translated)
        {
            filtered = string.IsNullOrWhiteSpace(_cultureFilter)
                ? filtered.Where(HasAllTranslations)
                : filtered.Where(row => !IsMissingTranslation(row, _cultureFilter));
        }

        var wasUpdatingUi = _isUpdatingUi;
        _isUpdatingUi = true;
        try
        {
            _bindingSource.DataSource = filtered.ToList();
        }
        finally
        {
            _isUpdatingUi = wasUpdatingUi;
        }
    }

    private static bool Contains(string? value, string query)
    {
        return value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsMissingTranslation(LocalizationEntryRow row, string culture)
    {
        return !row.Translations.TryGetValue(culture, out var translation) || string.IsNullOrWhiteSpace(translation);
    }

    private bool HasAnyMissingTranslation(LocalizationEntryRow row)
    {
        return _cultures.Any(culture => IsMissingTranslation(row, culture));
    }

    private bool HasAllTranslations(LocalizationEntryRow row)
    {
        return _cultures.All(culture => !IsMissingTranslation(row, culture));
    }

    private void RefreshCultureControls()
    {
        var selectedDefault = _defaultCultureBox.SelectedItem as string ?? _languages.DefaultCulture;

        _defaultCultureBox.Items.Clear();
        foreach (var culture in _cultures)
        {
            _defaultCultureBox.Items.Add(culture);
        }
        _defaultCultureBox.SelectedItem = _cultures.Contains(selectedDefault) ? selectedDefault : _languages.DefaultCulture;

        RefreshFilterCombos();
    }

    private void AddCulture()
    {
        using var dialog = new AddCultureDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var culture = dialog.CultureCode.Trim();
        if (!IsValidCultureCode(culture))
        {
            MessageBox.Show(this, EditorUiStrings.Get("InvalidCultureMessage"), EditorUiStrings.Get("InvalidCulture"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_cultures.Any(existing => string.Equals(existing, culture, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, EditorUiStrings.Format("DuplicateCultureMessage", culture), EditorUiStrings.Get("DuplicateCulture"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _grid.EndEdit();
        _cultures.Add(culture);
        foreach (var row in _rows)
        {
            row.Translations.TryAdd(culture, "");
        }

        RefreshCultureControls();
        ConfigureGridColumns();
        ApplyFilter();
        _undoStack.Clear();
        MarkDirty("StatusCultureAdded", culture);
    }

    private void DeleteCulture()
    {
        if (_cultures.Count <= 1)
        {
            MessageBox.Show(this, EditorUiStrings.Get("CannotDeleteLastCulture"), EditorUiStrings.Get("CannotDeleteCulture"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var preferredCulture = !string.IsNullOrWhiteSpace(_cultureFilter) ? _cultureFilter : _languages.DefaultCulture;
        using var dialog = new DeleteCultureDialog(_cultures, preferredCulture);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var culture = dialog.SelectedCulture;
        if (string.IsNullOrWhiteSpace(culture) || !_cultures.Contains(culture))
        {
            return;
        }

        if (string.Equals(_document.NativeCulture, culture, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                this,
                EditorUiStrings.Format("CannotDeleteNativeCultureMessage", culture),
                EditorUiStrings.Get("CannotDeleteNativeCulture"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            EditorUiStrings.Format("ConfirmDeleteCultureMessage", culture),
            EditorUiStrings.Get("ConfirmDeleteCulture"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        _grid.EndEdit();
        _cultures.Remove(culture);
        foreach (var row in _rows)
        {
            row.Translations.Remove(culture);
        }

        if (string.Equals(_languages.DefaultCulture, culture, StringComparison.OrdinalIgnoreCase))
        {
            _languages.DefaultCulture = _cultures[0];
        }

        RefreshCultureControls();
        ConfigureGridColumns();
        ApplyFilter();
        _undoStack.Clear();
        MarkDirty("StatusCultureDeleted", culture);
    }

    private static bool IsValidCultureCode(string culture)
    {
        return !string.IsNullOrWhiteSpace(culture)
            && Regex.IsMatch(culture, @"\A[A-Za-z0-9]+(?:[-_][A-Za-z0-9]+)*\z");
    }

    private void Grid_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        if (_isApplyingUndo)
        {
            return;
        }

        _pendingEdit = CreateUndoChange(e.RowIndex, e.ColumnIndex, "");
    }

    private void Grid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (_isApplyingUndo || _pendingEdit is not { } pending)
        {
            _pendingEdit = null;
            return;
        }

        _pendingEdit = null;
        if (pending.Row != GetRow(e.RowIndex) || pending.Culture != GetCulture(e.ColumnIndex))
        {
            return;
        }

        var newValue = GetTranslation(pending.Row, pending.Culture);
        if (pending.OldValue != newValue)
        {
            PushUndoBatch([pending with { NewValue = newValue }]);
        }
    }

    private void Grid_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.V)
        {
            PasteClipboardIntoGrid();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
        else if (e.Control && e.KeyCode == Keys.Z)
        {
            UndoLastChange();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private CellUndoChange? CreateUndoChange(int rowIndex, int columnIndex, string newValue)
    {
        var row = GetRow(rowIndex);
        var culture = GetCulture(columnIndex);
        if (row == null || string.IsNullOrWhiteSpace(culture))
        {
            return null;
        }

        return new CellUndoChange(row, culture, GetTranslation(row, culture), newValue);
    }

    private LocalizationEntryRow? GetRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _grid.Rows.Count)
        {
            return null;
        }

        return _grid.Rows[rowIndex].DataBoundItem as LocalizationEntryRow;
    }

    private string GetCulture(int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= _grid.Columns.Count)
        {
            return "";
        }

        var propertyName = _grid.Columns[columnIndex].DataPropertyName;
        const string prefix = "Translation_";
        return propertyName.StartsWith(prefix, StringComparison.Ordinal) ? propertyName[prefix.Length..] : "";
    }

    private static string GetTranslation(LocalizationEntryRow row, string culture)
    {
        return row.Translations.TryGetValue(culture, out var value) ? value : "";
    }

    private static void SetTranslation(LocalizationEntryRow row, string culture, string value)
    {
        row.Translations[culture] = value;
    }

    private void PushUndoBatch(IEnumerable<CellUndoChange?> changes)
    {
        var batch = changes
            .Where(change => change != null && change.OldValue != change.NewValue)
            .Select(change => change!)
            .ToList();
        if (batch.Count > 0)
        {
            _undoStack.Push(batch);
        }
    }

    private void UndoLastChange()
    {
        _grid.EndEdit();
        if (_undoStack.Count == 0)
        {
            SetStatusMessage("StatusNothingToUndo");
            return;
        }

        var batch = _undoStack.Pop();
        _isApplyingUndo = true;
        try
        {
            foreach (var change in batch)
            {
                SetTranslation(change.Row, change.Culture, change.OldValue);
            }
        }
        finally
        {
            _isApplyingUndo = false;
        }

        ApplyFilter();
        MarkDirty("StatusUndone", batch.Count);
    }

    private void PasteClipboardIntoGrid()
    {
        if (_grid.CurrentCell == null || !Clipboard.ContainsText())
        {
            return;
        }

        _grid.EndEdit();

        var clipboardText = Clipboard.GetText();
        if (string.IsNullOrEmpty(clipboardText))
        {
            return;
        }

        if (!clipboardText.Contains('\t'))
        {
            PasteSingleValue(clipboardText);
            return;
        }

        var startRow = _grid.CurrentCell.RowIndex;
        var startColumn = _grid.CurrentCell.ColumnIndex;
        var rows = SplitClipboardRows(clipboardText);
        var changedCells = 0;
        var undoChanges = new List<CellUndoChange?>();

        for (var rowOffset = 0; rowOffset < rows.Count; rowOffset++)
        {
            var targetRow = startRow + rowOffset;
            if (targetRow >= _grid.Rows.Count)
            {
                break;
            }

            var values = rows[rowOffset].Split('\t');
            for (var columnOffset = 0; columnOffset < values.Length; columnOffset++)
            {
                var targetColumn = startColumn + columnOffset;
                if (targetColumn >= _grid.Columns.Count)
                {
                    break;
                }

                var column = _grid.Columns[targetColumn];
                if (column.ReadOnly)
                {
                    continue;
                }

                undoChanges.Add(CreateUndoChange(targetRow, targetColumn, values[columnOffset]));
                _grid.Rows[targetRow].Cells[targetColumn].Value = values[columnOffset];
                changedCells++;
            }
        }

        if (changedCells > 0)
        {
            PushUndoBatch(undoChanges);
            _grid.NotifyCurrentCellDirty(true);
            _grid.EndEdit();
            MarkDirty("StatusPasted", changedCells);
        }
    }

    private void PasteSingleValue(string value)
    {
        var changedCells = 0;
        var undoChanges = new List<CellUndoChange?>();
        if (_grid.SelectedCells.Count > 1)
        {
            foreach (DataGridViewCell cell in _grid.SelectedCells)
            {
                if (cell.OwningColumn == null || cell.OwningColumn.ReadOnly)
                {
                    continue;
                }

                undoChanges.Add(CreateUndoChange(cell.RowIndex, cell.ColumnIndex, value));
                cell.Value = value;
                changedCells++;
            }
        }
        else if (_grid.CurrentCell is { } currentCell && currentCell.OwningColumn is { ReadOnly: false })
        {
            undoChanges.Add(CreateUndoChange(currentCell.RowIndex, currentCell.ColumnIndex, value));
            currentCell.Value = value;
            changedCells++;
        }

        if (changedCells > 0)
        {
            PushUndoBatch(undoChanges);
            _grid.NotifyCurrentCellDirty(true);
            _grid.EndEdit();
            MarkDirty("StatusPasted", changedCells);
        }
    }

    private static List<string> SplitClipboardRows(string clipboardText)
    {
        clipboardText = clipboardText.Replace("\r\n", "\n").Replace('\r', '\n');
        if (clipboardText.EndsWith('\n'))
        {
            clipboardText = clipboardText[..^1];
        }

        return clipboardText.Split('\n').ToList();
    }

    private void SaveDocuments()
    {
        try
        {
            if (!_hasLoadedDocument)
            {
                throw new InvalidOperationException("\u5c1a\u672a\u52a0\u8f7d\u6709\u6548\u7684\u672c\u5730\u5316 JSON\uff0c\u65e0\u6cd5\u4fdd\u5b58\u3002");
            }

            _grid.EndEdit();
            EnsureSourceFilesUnchanged();

            if (_defaultCultureBox.SelectedItem is not string selectedDefaultCulture)
            {
                throw new InvalidDataException("\u8bf7\u9009\u62e9\u6709\u6548\u7684\u542f\u52a8\u8bed\u8a00\u3002");
            }

            var canonicalDefaultCulture = FindDeclaredCulture(_cultures, selectedDefaultCulture)
                ?? throw new InvalidDataException($"\u542f\u52a8\u8bed\u8a00 '{selectedDefaultCulture}' \u672a\u5728 cultures \u4e2d\u58f0\u660e\u3002");
            var languagesToSave = new LanguagesDocument
            {
                Version = 1,
                DefaultCulture = canonicalDefaultCulture,
                Cultures = new List<string>(_cultures)
            };
            var documentToSave = new LocalizationDocument
            {
                Version = 1,
                Target = "Game",
                NativeCulture = _document.NativeCulture,
                Entries = _rows.Select(row => row.ToEntry(_cultures)).ToList()
            };

            var transactionId = Guid.NewGuid().ToString("N");
            var languagesTemporaryFile = $"{_languagesFile}.tmp.{transactionId}";
            var gameTemporaryFile = $"{_gameFile}.tmp.{transactionId}";
            var backupGroup = CreateUniqueBackupGroup(_overridesDirectory);
            var languagesBackupFile = $"{_languagesFile}.{backupGroup}.bak";
            var gameBackupFile = $"{_gameFile}.{backupGroup}.bak";
            var backupsComplete = false;
            var commitStarted = false;
            JsonFileSnapshot? finalLanguagesSnapshot = null;
            JsonFileSnapshot? finalGameSnapshot = null;
            LanguagesDocument? savedLanguages = null;
            LocalizationDocument? savedDocument = null;

            try
            {
                File.WriteAllText(languagesTemporaryFile, JsonSerializer.Serialize(languagesToSave, _jsonOptions), Utf16LeBom);
                File.WriteAllText(gameTemporaryFile, JsonSerializer.Serialize(documentToSave, _jsonOptions), Utf16LeBom);

                // Re-read both temporary files using exactly the same encoding
                // and schema path used during normal loading.
                var temporaryLanguagesSnapshot = ReadUtf16LeBomJson(languagesTemporaryFile);
                var validatedLanguages = ParseLanguagesDocument(temporaryLanguagesSnapshot.Text, languagesTemporaryFile);
                var temporaryGameSnapshot = ReadUtf16LeBomJson(gameTemporaryFile);
                ParseLocalizationDocument(temporaryGameSnapshot.Text, gameTemporaryFile, validatedLanguages);

                // Check again immediately before taking backups to reduce the
                // external-edit race between validation and replacement.
                EnsureSourceFilesUnchanged();
                File.Copy(_languagesFile, languagesBackupFile, overwrite: false);
                try
                {
                    File.Copy(_gameFile, gameBackupFile, overwrite: false);
                    backupsComplete = true;

                    // The backups must represent exactly the bytes that were
                    // loaded into the editor. This catches external writes that
                    // raced with backup creation before any formal file changes.
                    var backupLanguagesFingerprint = ReadUtf16LeBomJson(languagesBackupFile).Fingerprint;
                    var backupGameFingerprint = ReadUtf16LeBomJson(gameBackupFile).Fingerprint;
                    if (!string.Equals(backupLanguagesFingerprint, _languagesFingerprint, StringComparison.Ordinal)
                        || !string.Equals(backupGameFingerprint, _gameFingerprint, StringComparison.Ordinal))
                    {
                        throw new IOException(
                            "\u521b\u5efa\u5907\u4efd\u65f6\u68c0\u6d4b\u5230 JSON \u88ab\u5916\u90e8\u4fee\u6539\uff0c\u672c\u6b21\u4fdd\u5b58\u5df2\u53d6\u6d88\u3002\u8bf7\u91cd\u65b0\u52a0\u8f7d\u3002");
                    }

                    EnsureSourceFilesUnchanged();
                }
                catch
                {
                    SafeDelete(languagesBackupFile);
                    throw;
                }

                commitStarted = true;
                ReplaceFileAtomically(languagesTemporaryFile, _languagesFile);
                ReplaceFileAtomically(gameTemporaryFile, _gameFile);

                finalLanguagesSnapshot = ReadUtf16LeBomJson(_languagesFile);
                savedLanguages = ParseLanguagesDocument(finalLanguagesSnapshot.Text, _languagesFile);
                finalGameSnapshot = ReadUtf16LeBomJson(_gameFile);
                savedDocument = ParseLocalizationDocument(finalGameSnapshot.Text, _gameFile, savedLanguages);
            }
            catch (Exception transactionException)
            {
                if (commitStarted && backupsComplete)
                {
                    if (TryRestoreBackupPair(
                        languagesBackupFile,
                        _languagesFile,
                        gameBackupFile,
                        _gameFile,
                        out var rollbackError))
                    {
                        SafeDelete(languagesBackupFile);
                        SafeDelete(gameBackupFile);
                    }
                    else
                    {
                        throw new IOException(
                            $"\u4fdd\u5b58\u5931\u8d25\uff0c\u4e14\u81ea\u52a8\u56de\u6eda\u672a\u5b8c\u5168\u6210\u529f\u3002\r\n" +
                            $"\u8bf7\u4fdd\u7559\u5e76\u624b\u52a8\u6062\u590d\uff1a\r\n{languagesBackupFile}\r\n{gameBackupFile}\r\n\r\n{rollbackError}",
                            transactionException);
                    }
                }
                else
                {
                    SafeDelete(languagesBackupFile);
                    SafeDelete(gameBackupFile);
                }

                throw new IOException($"\u672c\u5730\u5316 JSON \u6210\u7ec4\u4fdd\u5b58\u5931\u8d25\uff0c\u539f\u6587\u4ef6\u5df2\u4fdd\u6301\u6216\u6062\u590d\u3002{Environment.NewLine}{transactionException.Message}", transactionException);
            }
            finally
            {
                SafeDelete(languagesTemporaryFile);
                SafeDelete(gameTemporaryFile);
            }

            _languages = savedLanguages ?? throw new InvalidOperationException("\u4fdd\u5b58\u540e languages.json \u6821\u9a8c\u72b6\u6001\u7f3a\u5931\u3002");
            _document = savedDocument ?? throw new InvalidOperationException("\u4fdd\u5b58\u540e Game.json \u6821\u9a8c\u72b6\u6001\u7f3a\u5931\u3002");
            _languagesFingerprint = finalLanguagesSnapshot?.Fingerprint
                ?? throw new InvalidOperationException("\u4fdd\u5b58\u540e languages.json \u6307\u7eb9\u7f3a\u5931\u3002");
            _gameFingerprint = finalGameSnapshot?.Fingerprint
                ?? throw new InvalidOperationException("\u4fdd\u5b58\u540e Game.json \u6307\u7eb9\u7f3a\u5931\u3002");

            string? pruneWarning = null;
            try
            {
                PruneBackupGroups(_overridesDirectory);
            }
            catch (Exception pruneException)
            {
                pruneWarning = pruneException.Message;
            }
            ClearDirty();
            if (pruneWarning == null)
            {
                SetStatusMessage("StatusSaved", _languages.DefaultCulture);
            }
            else
            {
                SetStatusMessage("StatusSavedBackupPruneFailed", pruneWarning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, EditorUiStrings.Get("SaveFailed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void EnsureSourceFilesUnchanged()
    {
        var currentLanguagesFingerprint = ReadUtf16LeBomJson(_languagesFile).Fingerprint;
        var currentGameFingerprint = ReadUtf16LeBomJson(_gameFile).Fingerprint;
        if (!string.Equals(currentLanguagesFingerprint, _languagesFingerprint, StringComparison.Ordinal)
            || !string.Equals(currentGameFingerprint, _gameFingerprint, StringComparison.Ordinal))
        {
            throw new IOException(
                "\u68c0\u6d4b\u5230 languages.json \u6216 Game.json \u5728\u52a0\u8f7d\u540e\u88ab\u5176\u4ed6\u7a0b\u5e8f\u4fee\u6539\u3002\r\n" +
                "\u4e3a\u907f\u514d\u8986\u76d6\u5916\u90e8\u66f4\u65b0\uff0c\u672c\u6b21\u4fdd\u5b58\u5df2\u53d6\u6d88\u3002\u8bf7\u70b9\u51fb\u201c\u91cd\u65b0\u52a0\u8f7d\u201d\u540e\u518d\u7f16\u8f91\u3002");
        }
    }

    private static string CreateUniqueBackupGroup(string directory)
    {
        return $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}";
    }

    private static void ReplaceFileAtomically(string sourceFile, string destinationFile)
    {
        if (File.Exists(destinationFile))
        {
            File.Replace(sourceFile, destinationFile, destinationBackupFileName: null, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(sourceFile, destinationFile);
        }
    }

    private static bool TryRestoreBackupPair(
        string languagesBackupFile,
        string languagesFile,
        string gameBackupFile,
        string gameFile,
        out string error)
    {
        var errors = new List<string>();
        TryRestoreBackup(languagesBackupFile, languagesFile, errors);
        TryRestoreBackup(gameBackupFile, gameFile, errors);
        error = string.Join(Environment.NewLine, errors);
        return errors.Count == 0;
    }

    private static void TryRestoreBackup(string backupFile, string destinationFile, List<string> errors)
    {
        var restoreTemporaryFile = $"{destinationFile}.restore.{Guid.NewGuid():N}";
        try
        {
            if (!File.Exists(backupFile))
            {
                throw new FileNotFoundException("\u56de\u6eda\u5907\u4efd\u4e0d\u5b58\u5728\u3002", backupFile);
            }

            File.Copy(backupFile, restoreTemporaryFile, overwrite: false);
            ReplaceFileAtomically(restoreTemporaryFile, destinationFile);
        }
        catch (Exception exception)
        {
            errors.Add($"{destinationFile}: {exception.Message}");
        }
        finally
        {
            SafeDelete(restoreTemporaryFile);
        }
    }

    private static void SafeDelete(string filename)
    {
        try
        {
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
        }
        catch
        {
            // Cleanup is best effort. Formal files and recovery backups are
            // never deleted through this helper.
        }
    }

    private static void PruneBackupGroups(string directory)
    {
        var backupPattern = new Regex(
            @"^(Game|languages)\.json\.(\d{8}-\d{6}(?:-\d{3})?(?:-\d{2}|-[0-9A-Fa-f]{32})?)\.bak$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var backupGroups = Directory.EnumerateFiles(directory, "*.bak")
            .Select(file => new { File = file, Match = backupPattern.Match(Path.GetFileName(file)) })
            .Where(item => item.Match.Success)
            .GroupBy(item => item.Match.Groups[2].Value)
            .Where(group => group.Select(item => item.Match.Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 2)
            .OrderByDescending(group => group.Key)
            .Skip(3);

        foreach (var group in backupGroups)
        {
            foreach (var item in group)
            {
                File.Delete(item.File);
            }
        }
    }
}

internal sealed class AddCultureDialog : Form
{
    private readonly ComboBox _cultureBox = new();
    private readonly Label _promptLabel = new();
    private readonly Button _okButton = new();
    private readonly Button _cancelButton = new();

    public string CultureCode => ExtractCultureCode(_cultureBox.Text);

    public AddCultureDialog()
    {
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(430, 158);

        _promptLabel.AutoSize = true;
        _promptLabel.Location = new Point(16, 16);

        _cultureBox.DropDownStyle = ComboBoxStyle.DropDown;
        _cultureBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cultureBox.AutoCompleteSource = AutoCompleteSource.ListItems;
        _cultureBox.Location = new Point(16, 48);
        _cultureBox.Width = 398;
        RefreshCulturePresets();

        _okButton.DialogResult = DialogResult.OK;
        _okButton.Width = 88;
        _okButton.Height = 28;

        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Width = 88;
        _cancelButton.Height = 28;

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Location = new Point(16, 102),
            Size = new Size(398, 32),
            WrapContents = false
        };
        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_okButton);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        Controls.Add(_promptLabel);
        Controls.Add(_cultureBox);
        Controls.Add(buttonPanel);

        ApplyUiLanguage();
    }

    private void ApplyUiLanguage()
    {
        Text = EditorUiStrings.Get("AddCultureDialogTitle");
        _promptLabel.Text = EditorUiStrings.Get("AddCultureDialogLabel");
        _okButton.Text = EditorUiStrings.Get("Ok");
        _cancelButton.Text = EditorUiStrings.Get("Cancel");
        RefreshCulturePresets();
    }

    private void RefreshCulturePresets()
    {
        var selected = _cultureBox.Text;
        _cultureBox.Items.Clear();
        foreach (var culture in CultureDisplayHelper.PresetCultureCodes)
        {
            _cultureBox.Items.Add(EditorUiStrings.GetCulturePresetLabel(culture));
        }

        if (!string.IsNullOrWhiteSpace(selected))
        {
            _cultureBox.Text = selected;
        }
    }

    private static string ExtractCultureCode(string value)
    {
        var text = value.Trim();
        var separatorIndex = text.IndexOf(" - ", StringComparison.Ordinal);
        return separatorIndex >= 0 ? text[..separatorIndex].Trim() : text;
    }
}

internal sealed class DeleteCultureDialog : Form
{
    private readonly ComboBox _cultureBox = new();
    private readonly Label _promptLabel = new();
    private readonly Button _okButton = new();
    private readonly Button _cancelButton = new();

    public string SelectedCulture => _cultureBox.SelectedItem as string ?? "";

    public DeleteCultureDialog(IEnumerable<string> cultures, string? preferredCulture)
    {
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(430, 158);

        _promptLabel.AutoSize = true;
        _promptLabel.Location = new Point(16, 16);

        _cultureBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _cultureBox.Location = new Point(16, 48);
        _cultureBox.Width = 398;
        foreach (var culture in cultures)
        {
            _cultureBox.Items.Add(culture);
        }

        if (!string.IsNullOrWhiteSpace(preferredCulture) && _cultureBox.Items.Contains(preferredCulture))
        {
            _cultureBox.SelectedItem = preferredCulture;
        }
        else if (_cultureBox.Items.Count > 0)
        {
            _cultureBox.SelectedIndex = 0;
        }

        _okButton.DialogResult = DialogResult.OK;
        _okButton.Width = 88;
        _okButton.Height = 28;

        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Width = 88;
        _cancelButton.Height = 28;

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Location = new Point(16, 102),
            Size = new Size(398, 32),
            WrapContents = false
        };
        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_okButton);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        Controls.Add(_promptLabel);
        Controls.Add(_cultureBox);
        Controls.Add(buttonPanel);

        ApplyUiLanguage();
    }

    private void ApplyUiLanguage()
    {
        Text = EditorUiStrings.Get("DeleteCultureDialogTitle");
        _promptLabel.Text = EditorUiStrings.Get("DeleteCultureDialogLabel");
        _okButton.Text = EditorUiStrings.Get("Ok");
        _cancelButton.Text = EditorUiStrings.Get("Cancel");
    }
}

internal sealed class LanguagesDocument
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("defaultCulture")]
    public string DefaultCulture { get; set; } = "";

    [JsonPropertyName("cultures")]
    public List<string> Cultures { get; set; } = [];
}

internal sealed class LocalizationDocument
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("target")]
    public string Target { get; set; } = "";

    [JsonPropertyName("nativeCulture")]
    public string NativeCulture { get; set; } = "";

    [JsonPropertyName("entries")]
    public List<LocalizationEntry> Entries { get; set; } = [];
}

internal sealed class LocalizationEntry
{
    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = "";

    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("source")]
    public string Source { get; set; } = "";

    [JsonPropertyName("translations")]
    public Dictionary<string, string> Translations { get; set; } = [];
}

internal sealed class LocalizationEntryRow : System.ComponentModel.ICustomTypeDescriptor
{
    public string Namespace { get; set; } = "";
    public string Key { get; set; } = "";
    public string Source { get; set; } = "";
    public Dictionary<string, string> Translations { get; set; } = [];

    public static LocalizationEntryRow FromEntry(LocalizationEntry entry)
    {
        return new LocalizationEntryRow
        {
            Namespace = entry.Namespace,
            Key = entry.Key,
            Source = entry.Source,
            Translations = new Dictionary<string, string>(entry.Translations)
        };
    }

    public LocalizationEntry ToEntry(IEnumerable<string> cultures)
    {
        foreach (var culture in cultures)
        {
            Translations.TryAdd(culture, "");
        }

        return new LocalizationEntry
        {
            Namespace = Namespace,
            Key = Key,
            Source = Source,
            Translations = Translations
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                .ToDictionary(pair => pair.Key, pair => pair.Value)
        };
    }

    public System.ComponentModel.AttributeCollection GetAttributes() => System.ComponentModel.AttributeCollection.Empty;
    public string? GetClassName() => nameof(LocalizationEntryRow);
    public string? GetComponentName() => null;
    public System.ComponentModel.TypeConverter GetConverter() => new();
    public System.ComponentModel.EventDescriptor? GetDefaultEvent() => null;
    public System.ComponentModel.PropertyDescriptor? GetDefaultProperty() => null;
    public object? GetEditor(Type editorBaseType) => null;
    public System.ComponentModel.EventDescriptorCollection GetEvents() => System.ComponentModel.EventDescriptorCollection.Empty;
    public System.ComponentModel.EventDescriptorCollection GetEvents(Attribute[]? attributes) => System.ComponentModel.EventDescriptorCollection.Empty;
    public System.ComponentModel.PropertyDescriptorCollection GetProperties() => GetProperties(null);

    public System.ComponentModel.PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
    {
        var properties = new List<System.ComponentModel.PropertyDescriptor>
        {
            new StaticPropertyDescriptor(nameof(Namespace), () => Namespace, value => Namespace = value ?? ""),
            new StaticPropertyDescriptor(nameof(Key), () => Key, value => Key = value ?? ""),
            new StaticPropertyDescriptor(nameof(Source), () => Source, value => Source = value ?? "")
        };

        foreach (var culture in Translations.Keys.Order(StringComparer.OrdinalIgnoreCase))
        {
            properties.Add(new TranslationPropertyDescriptor(culture));
        }

        return new System.ComponentModel.PropertyDescriptorCollection(properties.ToArray());
    }

    public object GetPropertyOwner(System.ComponentModel.PropertyDescriptor? pd) => this;

    private sealed class StaticPropertyDescriptor(
        string name,
        Func<string> getter,
        Action<string?> setter) : System.ComponentModel.PropertyDescriptor(name, null)
    {
        public override Type ComponentType => typeof(LocalizationEntryRow);
        public override bool IsReadOnly => false;
        public override Type PropertyType => typeof(string);
        public override bool CanResetValue(object component) => false;
        public override object GetValue(object? component) => getter();
        public override void ResetValue(object component) { }
        public override void SetValue(object? component, object? value) => setter(value?.ToString());
        public override bool ShouldSerializeValue(object component) => false;
    }

    private sealed class TranslationPropertyDescriptor(string culture)
        : System.ComponentModel.PropertyDescriptor($"Translation_{culture}", null)
    {
        public override Type ComponentType => typeof(LocalizationEntryRow);
        public override bool IsReadOnly => false;
        public override Type PropertyType => typeof(string);
        public override bool CanResetValue(object component) => false;

        public override object GetValue(object? component)
        {
            var row = (LocalizationEntryRow)component!;
            return row.Translations.TryGetValue(culture, out var value) ? value : "";
        }

        public override void ResetValue(object component) { }

        public override void SetValue(object? component, object? value)
        {
            var row = (LocalizationEntryRow)component!;
            row.Translations[culture] = value?.ToString() ?? "";
        }

        public override bool ShouldSerializeValue(object component) => false;
    }
}
