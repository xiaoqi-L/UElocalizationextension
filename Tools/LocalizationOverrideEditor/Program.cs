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
            MessageBox.Show($"工具启动失败。详细错误已写入：{logFile}{Environment.NewLine}{Environment.NewLine}{exception.Message}", "多语言编辑工具", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch
        {
            // The log and dialog are diagnostics only; never rethrow during startup.
        }
    }
}

internal sealed class MainForm : Form
{
    private const string AllCulturesLabel = "\u5168\u90e8";
    private const string AllTranslationStateLabel = "\u5168\u90e8";
    private const string MissingTranslationStateLabel = "\u7a7a\u7ffb\u8bd1";
    private const string TranslatedStateLabel = "\u5df2\u7ffb\u8bd1";
    private static readonly Encoding Utf16LeBom = new UnicodeEncoding(
        bigEndian: false,
        byteOrderMark: true,
        throwOnInvalidBytes: true);
    private static readonly Dictionary<string, string> CultureDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "\u82f1\u8bed",
        ["en-US"] = "\u82f1\u8bed\uff08\u7f8e\u56fd\uff09",
        ["en-GB"] = "\u82f1\u8bed\uff08\u82f1\u56fd\uff09",
        ["zh"] = "\u4e2d\u6587",
        ["zh-CN"] = "\u4e2d\u6587\uff08\u4e2d\u56fd\u5927\u9646\uff09",
        ["zh-Hans"] = "\u7b80\u4f53\u4e2d\u6587",
        ["zh-Hant"] = "\u7e41\u4f53\u4e2d\u6587",
        ["zh-TW"] = "\u4e2d\u6587\uff08\u53f0\u6e7e\uff09",
        ["zh-HK"] = "\u4e2d\u6587\uff08\u9999\u6e2f\uff09",
        ["ja"] = "\u65e5\u8bed",
        ["ko"] = "\u97e9\u8bed",
        ["fr"] = "\u6cd5\u8bed",
        ["fr-FR"] = "\u6cd5\u8bed\uff08\u6cd5\u56fd\uff09",
        ["de"] = "\u5fb7\u8bed",
        ["de-DE"] = "\u5fb7\u8bed\uff08\u5fb7\u56fd\uff09",
        ["es"] = "\u897f\u73ed\u7259\u8bed",
        ["es-ES"] = "\u897f\u73ed\u7259\u8bed\uff08\u897f\u73ed\u7259\uff09",
        ["es-MX"] = "\u897f\u73ed\u7259\u8bed\uff08\u58a8\u897f\u54e5\uff09",
        ["it"] = "\u610f\u5927\u5229\u8bed",
        ["pt"] = "\u8461\u8404\u7259\u8bed",
        ["pt-BR"] = "\u8461\u8404\u7259\u8bed\uff08\u5df4\u897f\uff09",
        ["pt-PT"] = "\u8461\u8404\u7259\u8bed\uff08\u8461\u8404\u7259\uff09",
        ["ru"] = "\u4fc4\u8bed",
        ["pl"] = "\u6ce2\u5170\u8bed",
        ["tr"] = "\u571f\u8033\u5176\u8bed",
        ["nl"] = "\u8377\u5170\u8bed",
        ["sv"] = "\u745e\u5178\u8bed",
        ["no"] = "\u632a\u5a01\u8bed",
        ["da"] = "\u4e39\u9ea6\u8bed",
        ["fi"] = "\u82ac\u5170\u8bed",
        ["ar"] = "\u963f\u62c9\u4f2f\u8bed",
        ["he"] = "\u5e0c\u4f2f\u6765\u8bed",
        ["hi"] = "\u5370\u5730\u8bed",
        ["th"] = "\u6cf0\u8bed",
        ["vi"] = "\u8d8a\u5357\u8bed",
        ["id"] = "\u5370\u5ea6\u5c3c\u897f\u4e9a\u8bed",
        ["ms"] = "\u9a6c\u6765\u8bed",
        ["uk"] = "\u4e4c\u514b\u5170\u8bed",
        ["cs"] = "\u6377\u514b\u8bed",
        ["hu"] = "\u5308\u7259\u5229\u8bed",
        ["ro"] = "\u7f57\u9a6c\u5c3c\u4e9a\u8bed"
    };

    private readonly BindingSource _bindingSource = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _searchBox = new();
    private readonly ComboBox _cultureBox = new();
    private readonly ComboBox _translationStateBox = new();
    private readonly ComboBox _defaultCultureBox = new();
    private readonly TextBox _rootBox = new();
    private readonly StatusStrip _statusStrip = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly Button _reloadButton = new();
    private readonly Button _saveButton = new();
    private readonly Button _openButton = new();
    private readonly Button _addCultureButton = new();
    private readonly Button _deleteCultureButton = new();

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

    private sealed record CellUndoChange(LocalizationEntryRow Row, string Culture, string OldValue, string NewValue);
    private sealed record JsonFileSnapshot(string Text, string Fingerprint);

    public MainForm()
    {
        Text = "\u591a\u8bed\u8a00\u7f16\u8f91\u5de5\u5177";
        Icon = LoadWindowIcon();
        Width = 1240;
        Height = 720;
        MinimumSize = new Size(980, 520);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
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
        BackColor = Color.FromArgb(245, 247, 250);

        static Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 8, 0)
            };
        }

        static void StyleButton(Button button, bool primary = false)
        {
            button.AutoSize = false;
            button.Height = 32;
            button.Width = 92;
            button.Margin = new Padding(4, 0, 0, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;

            if (primary)
            {
                button.BackColor = Color.FromArgb(37, 99, 235);
                button.ForeColor = Color.White;
                button.FlatAppearance.BorderSize = 0;
            }
            else
            {
                button.BackColor = Color.White;
                button.ForeColor = Color.FromArgb(45, 55, 72);
                button.FlatAppearance.BorderColor = Color.FromArgb(210, 216, 224);
                button.FlatAppearance.BorderSize = 1;
            }
        }

        static void StyleInput(Control control)
        {
            control.Height = 32;
            control.Margin = new Padding(0, 0, 12, 0);
        }

        var rootPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(16, 12, 16, 8),
            BackColor = Color.FromArgb(245, 247, 250)
        };
        rootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rootPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _rootBox.ReadOnly = false;
        _rootBox.Dock = DockStyle.Fill;
        _rootBox.BackColor = Color.White;
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

        _searchBox.PlaceholderText = "\u641c\u7d22\u6587\u672c";
        _searchBox.Dock = DockStyle.Fill;
        StyleInput(_searchBox);
        _searchBox.TextChanged += (_, _) => ApplyFilter();

        _cultureBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _cultureBox.Dock = DockStyle.Fill;
        StyleInput(_cultureBox);
        _cultureBox.SelectedIndexChanged += (_, _) => ApplyFilter();

        _translationStateBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _translationStateBox.Dock = DockStyle.Fill;
        StyleInput(_translationStateBox);
        _translationStateBox.Items.AddRange([AllTranslationStateLabel, MissingTranslationStateLabel, TranslatedStateLabel]);
        _translationStateBox.SelectedIndex = 0;
        _translationStateBox.SelectedIndexChanged += (_, _) => ApplyFilter();

        _defaultCultureBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _defaultCultureBox.Dock = DockStyle.Fill;
        StyleInput(_defaultCultureBox);
        _defaultCultureBox.SelectedIndexChanged += (_, _) =>
        {
            if (!_isUpdatingUi && _defaultCultureBox.SelectedItem is string selected && selected != _languages.DefaultCulture)
            {
                MarkDirty("\u542f\u52a8\u8bed\u8a00\u5df2\u66f4\u6539\uff0c\u70b9\u51fb\u4fdd\u5b58\u540e\u751f\u6548\u3002");
            }
        };

        _openButton.Text = "\u6d4f\u89c8";
        StyleButton(_openButton);
        _openButton.Click += (_, _) => ChooseRootDirectory();

        _reloadButton.Text = "\u91cd\u65b0\u52a0\u8f7d";
        StyleButton(_reloadButton);
        _reloadButton.Click += (_, _) => LoadFromRoot(_rootBox.Text);

        _saveButton.Text = "\u4fdd\u5b58";
        StyleButton(_saveButton, primary: true);
        _saveButton.Click += (_, _) => SaveDocuments();

        _addCultureButton.Text = "\u6dfb\u52a0\u8bed\u7cfb";
        StyleButton(_addCultureButton);
        _addCultureButton.Click += (_, _) => AddCulture();

        _deleteCultureButton.Text = "\u5220\u9664\u8bed\u7cfb";
        StyleButton(_deleteCultureButton);
        _deleteCultureButton.Click += (_, _) => DeleteCulture();

        rootPanel.Controls.Add(CreateLabel("\u672c\u5730\u5316\u76ee\u5f55/\u9879\u76ee\u76ee\u5f55"), 0, 0);
        rootPanel.Controls.Add(_rootBox, 1, 0);
        rootPanel.Controls.Add(_openButton, 2, 0);

        var toolbarPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 92,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(16, 0, 16, 10),
            BackColor = Color.FromArgb(245, 247, 250)
        };
        toolbarPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbarPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        toolbarPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        var filterPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 8,
            RowCount = 1
        };
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 6));
        filterPanel.Controls.Add(_searchBox, 0, 0);
        filterPanel.Controls.Add(CreateLabel("\u8bed\u8a00"), 1, 0);
        filterPanel.Controls.Add(_cultureBox, 2, 0);
        filterPanel.Controls.Add(CreateLabel("\u72b6\u6001"), 3, 0);
        filterPanel.Controls.Add(_translationStateBox, 4, 0);
        filterPanel.Controls.Add(CreateLabel("\u542f\u52a8\u8bed\u8a00"), 5, 0);
        filterPanel.Controls.Add(_defaultCultureBox, 6, 0);

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        actionPanel.Controls.Add(_reloadButton);
        actionPanel.Controls.Add(_addCultureButton);
        actionPanel.Controls.Add(_deleteCultureButton);
        actionPanel.Controls.Add(_saveButton);

        toolbarPanel.Controls.Add(filterPanel, 0, 0);
        toolbarPanel.Controls.Add(actionPanel, 0, 1);

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.GridColor = Color.FromArgb(225, 230, 236);
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 41, 59);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        _grid.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F);
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 254);
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 41, 59);
        _grid.RowTemplate.Height = 38;
        _grid.ColumnHeadersHeight = 40;
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _grid.MultiSelect = true;
        _grid.DataSource = _bindingSource;
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
            if ((_translationStateBox.SelectedItem as string) != AllTranslationStateLabel)
            {
                ApplyFilter();
            }
        };
        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty)
            {
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };

        _statusStrip.Dock = DockStyle.Bottom;
        _statusStrip.BackColor = Color.White;
        _statusStrip.SizingGrip = false;
        _statusLabel.Spring = true;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusStrip.Items.Add(_statusLabel);

        Controls.Add(_grid);
        Controls.Add(_statusStrip);
        Controls.Add(toolbarPanel);
        Controls.Add(rootPanel);
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
        return CultureDisplayNames.TryGetValue(culture, out var displayName)
            ? $"{culture} - {displayName}"
            : culture;
    }

    private void LoadFromDetectedRoot()
    {
        LoadFromRoot(AppContext.BaseDirectory);
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_hasUnsavedChanges)
        {
            return;
        }

        var result = MessageBox.Show(
            this,
            "\u5f53\u524d\u6709\u672a\u4fdd\u5b58\u7684\u4fee\u6539\uff0c\u786e\u5b9a\u4e0d\u4fdd\u5b58\u76f4\u63a5\u9000\u51fa\u5417\uff1f",
            "\u786e\u8ba4\u9000\u51fa",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
        {
            e.Cancel = true;
        }
    }

    private void MarkDirty(string? message = null)
    {
        if (_isUpdatingUi || !_hasLoadedDocument)
        {
            return;
        }

        _hasUnsavedChanges = true;
        _statusLabel.Text = message ?? "\u6709\u672a\u4fdd\u5b58\u7684\u4fee\u6539\u3002";
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
            normalizedInput = AppContext.BaseDirectory;
        }

        var startDirectory = GetSearchStartDirectory(normalizedInput);
        var candidates = new List<string>();

        AddCandidate(candidates, startDirectory);
        AddCandidate(candidates, Path.Combine(startDirectory, "LocalizationOverrides"));

        foreach (var ancestor in EnumerateAncestors(startDirectory))
        {
            AddCandidate(candidates, Path.Combine(ancestor, "LocalizationOverrides"));
        }

        AddPackagedCandidates(candidates, startDirectory);
        foreach (var ancestor in EnumerateAncestors(startDirectory).Skip(1).Take(4))
        {
            AddPackagedCandidates(candidates, ancestor);
        }

        var resolved = candidates.FirstOrDefault(IsValidOverridesDirectory);
        if (!string.IsNullOrWhiteSpace(resolved))
        {
            return Path.GetFullPath(resolved);
        }

        throw new DirectoryNotFoundException(
            "\u672a\u627e\u5230\u6709\u6548\u7684 LocalizationOverrides \u76ee\u5f55\u3002\r\n\r\n" +
            "\u8bf7\u8f93\u5165\u6216\u9009\u62e9\u4ee5\u4e0b\u4efb\u4e00\u8def\u5f84\uff1a\r\n" +
            "- \u9879\u76ee\u6839\u76ee\u5f55\r\n" +
            "- \u6253\u5305\u6839\u76ee\u5f55\r\n" +
            "- Windows/\u9879\u76ee\u540d/Binaries/Win64 \u76ee\u5f55\r\n" +
            "- \u6253\u5305 exe \u6587\u4ef6\r\n" +
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

    private static IEnumerable<string> EnumerateAncestors(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);
        while (current != null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }

    private static void AddPackagedCandidates(List<string> candidates, string root)
    {
        AddCandidate(candidates, Path.Combine(root, "Binaries", "Win64", "LocalizationOverrides"));

        foreach (var packagedCandidate in EnumerateOverridesDirectories(root, maxDepth: 5)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}Binaries{Path.DirectorySeparatorChar}Win64{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)))
        {
            AddCandidate(candidates, packagedCandidate);
        }
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

    private static void AddCandidate(List<string> candidates, string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(candidate);
        }
        catch
        {
            return;
        }

        if (!candidates.Any(existing => string.Equals(existing, fullPath, StringComparison.OrdinalIgnoreCase)))
        {
            candidates.Add(fullPath);
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
            Description = "\u9009\u62e9 LocalizationOverrides \u76ee\u5f55\u3001UE \u6253\u5305\u76ee\u5f55\u6216\u9879\u76ee\u6839\u76ee\u5f55",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_overridesDirectory) ? _overridesDirectory : Directory.GetCurrentDirectory()
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            LoadFromRoot(dialog.SelectedPath);
        }
    }

    private void LoadFromRoot(string rootDirectory)
    {
        if (_hasLoadedDocument && _hasUnsavedChanges)
        {
            var discard = MessageBox.Show(
                this,
                "\u5f53\u524d\u6709\u672a\u4fdd\u5b58\u7684\u4fee\u6539\u3002\r\n\r\n\u662f\u5426\u653e\u5f03\u8fd9\u4e9b\u4fee\u6539\u5e76\u91cd\u65b0\u52a0\u8f7d\uff1f",
                "\u786e\u8ba4\u91cd\u65b0\u52a0\u8f7d",
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
                SetDocumentActionsEnabled(true);
                ClearDirty();
                _statusLabel.Text = $"\u5df2\u52a0\u8f7d {_rows.Count} \u6761\u6587\u672c\uff0c\u542f\u52a8\u8bed\u8a00\uff1a{_languages.DefaultCulture}\u3002\u8def\u5f84\uff1a{_overridesDirectory}";
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
                _statusLabel.Text = "\u672a\u52a0\u8f7d\u6709\u6548\u7684\u672c\u5730\u5316 JSON\u3002";
            }
            else
            {
                _rootBox.Text = _overridesDirectory;
                _statusLabel.Text = $"\u52a0\u8f7d\u5931\u8d25\uff0c\u4ecd\u4f7f\u7528\u4e0a\u4e00\u4e2a\u6709\u6548\u76ee\u5f55\uff1a{_overridesDirectory}";
            }
            MessageBox.Show(this, ex.Message, "\u52a0\u8f7d\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        var selectedCulture = _cultureBox.SelectedItem as string ?? AllCulturesLabel;
        var selectedTranslationState = _translationStateBox.SelectedItem as string ?? AllTranslationStateLabel;
        IEnumerable<LocalizationEntryRow> filtered = _rows;

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(row =>
                Contains(row.Namespace, query) ||
                Contains(row.Key, query) ||
                Contains(row.Source, query) ||
                row.Translations.Values.Any(value => Contains(value, query)));
        }

        if (selectedCulture != AllCulturesLabel)
        {
            filtered = filtered.Where(row => row.Translations.ContainsKey(selectedCulture));
        }

        if (selectedTranslationState == MissingTranslationStateLabel)
        {
            filtered = selectedCulture == AllCulturesLabel
                ? filtered.Where(HasAnyMissingTranslation)
                : filtered.Where(row => IsMissingTranslation(row, selectedCulture));
        }
        else if (selectedTranslationState == TranslatedStateLabel)
        {
            filtered = selectedCulture == AllCulturesLabel
                ? filtered.Where(HasAllTranslations)
                : filtered.Where(row => !IsMissingTranslation(row, selectedCulture));
        }

        _bindingSource.DataSource = filtered.ToList();
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
        var selectedFilter = _cultureBox.SelectedItem as string ?? AllCulturesLabel;
        var selectedDefault = _defaultCultureBox.SelectedItem as string ?? _languages.DefaultCulture;

        _cultureBox.Items.Clear();
        _cultureBox.Items.Add(AllCulturesLabel);
        foreach (var culture in _cultures)
        {
            _cultureBox.Items.Add(culture);
        }
        _cultureBox.SelectedItem = _cultures.Contains(selectedFilter) ? selectedFilter : AllCulturesLabel;
        if (_cultureBox.SelectedIndex < 0)
        {
            _cultureBox.SelectedIndex = 0;
        }

        _defaultCultureBox.Items.Clear();
        foreach (var culture in _cultures)
        {
            _defaultCultureBox.Items.Add(culture);
        }
        _defaultCultureBox.SelectedItem = _cultures.Contains(selectedDefault) ? selectedDefault : _languages.DefaultCulture;
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
            MessageBox.Show(this, "\u8bed\u7cfb\u4ee3\u7801\u53ea\u80fd\u5305\u542b\u5b57\u6bcd\u3001\u6570\u5b57\u3001\u77ed\u6a2a\u7ebf\u548c\u4e0b\u5212\u7ebf\u3002", "\u65e0\u6548\u8bed\u7cfb", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_cultures.Any(existing => string.Equals(existing, culture, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, $"\u8bed\u7cfb '{culture}' \u5df2\u7ecf\u5b58\u5728\u3002", "\u91cd\u590d\u8bed\u7cfb", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        MarkDirty($"\u5df2\u6dfb\u52a0\u8bed\u7cfb {culture}\uff0c\u70b9\u51fb\u4fdd\u5b58\u540e\u751f\u6548\u3002");
    }

    private void DeleteCulture()
    {
        if (_cultures.Count <= 1)
        {
            MessageBox.Show(this, "\u81f3\u5c11\u9700\u8981\u4fdd\u7559\u4e00\u4e2a\u8bed\u7cfb\uff0c\u65e0\u6cd5\u7ee7\u7eed\u5220\u9664\u3002", "\u65e0\u6cd5\u5220\u9664\u8bed\u7cfb", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var currentFilter = _cultureBox.SelectedItem as string;
        var preferredCulture = currentFilter != AllCulturesLabel ? currentFilter : _languages.DefaultCulture;
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
                $"\u8bed\u7cfb '{culture}' \u662f Game.json \u7684 nativeCulture\uff0c\u4e0d\u80fd\u5728\u6b64\u5de5\u5177\u4e2d\u5220\u9664\u3002",
                "\u65e0\u6cd5\u5220\u9664\u6e90\u8bed\u7cfb",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            $"\u786e\u5b9a\u8981\u5220\u9664\u8bed\u7cfb '{culture}' \u5417\uff1f\r\n\r\n\u8be5\u8bed\u7cfb\u4f1a\u4ece languages.json \u7684 cultures \u4e2d\u79fb\u9664\uff0c\u5e76\u4ece Game.json \u6bcf\u6761 translations \u4e2d\u79fb\u9664\u5bf9\u5e94\u5b57\u6bb5\u3002\r\n\u6b64\u64cd\u4f5c\u9700\u8981\u70b9\u51fb\u4fdd\u5b58\u540e\u624d\u4f1a\u5199\u5165\u6587\u4ef6\u3002",
            "\u786e\u8ba4\u5220\u9664\u8bed\u7cfb",
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
        MarkDirty($"\u5df2\u5220\u9664\u8bed\u7cfb {culture}\uff0c\u70b9\u51fb\u4fdd\u5b58\u540e\u751f\u6548\u3002");
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
            _statusLabel.Text = "\u6ca1\u6709\u53ef\u64a4\u9500\u7684\u4fee\u6539\u3002";
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
        MarkDirty($"\u5df2\u64a4\u9500 {batch.Count} \u4e2a\u5355\u5143\u683c\u4fee\u6539\uff0c\u6709\u672a\u4fdd\u5b58\u7684\u4fee\u6539\u3002");
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
            MarkDirty($"\u5df2\u7c98\u8d34 {changedCells} \u4e2a\u5355\u5143\u683c\uff0c\u6709\u672a\u4fdd\u5b58\u7684\u4fee\u6539\u3002");
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
            MarkDirty($"\u5df2\u7c98\u8d34 {changedCells} \u4e2a\u5355\u5143\u683c\uff0c\u6709\u672a\u4fdd\u5b58\u7684\u4fee\u6539\u3002");
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
            _statusLabel.Text = pruneWarning == null
                ? $"\u5df2\u4fdd\u5b58\uff0c\u542f\u52a8\u8bed\u8a00\uff1a{_languages.DefaultCulture}\u3002"
                : $"\u6587\u4ef6\u5df2\u4fdd\u5b58\uff0c\u4f46\u65e7\u5907\u4efd\u88c1\u526a\u5931\u8d25\uff1a{pruneWarning}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "\u4fdd\u5b58\u5931\u8d25", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    public string CultureCode => ExtractCultureCode(_cultureBox.Text);

    public AddCultureDialog()
    {
        Text = "\u6dfb\u52a0\u8bed\u7cfb";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(430, 158);

        var label = new Label
        {
            Text = "\u8bed\u7cfb\u4ee3\u7801\uff08\u4f8b\u5982 ja\u3001ko\u3001fr\u3001zh-Hant\uff09",
            AutoSize = true,
            Location = new Point(16, 16)
        };

        _cultureBox.DropDownStyle = ComboBoxStyle.DropDown;
        _cultureBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _cultureBox.AutoCompleteSource = AutoCompleteSource.ListItems;
        _cultureBox.Location = new Point(16, 48);
        _cultureBox.Width = 398;
        _cultureBox.Items.AddRange(new object[]
        {
            "en - \u82f1\u8bed",
            "en-US - \u82f1\u8bed\uff08\u7f8e\u56fd\uff09",
            "en-GB - \u82f1\u8bed\uff08\u82f1\u56fd\uff09",
            "zh - \u4e2d\u6587",
            "zh-CN - \u4e2d\u6587\uff08\u4e2d\u56fd\u5927\u9646\uff09",
            "zh-Hans - \u7b80\u4f53\u4e2d\u6587",
            "zh-Hant - \u7e41\u4f53\u4e2d\u6587",
            "zh-TW - \u4e2d\u6587\uff08\u53f0\u6e7e\uff09",
            "zh-HK - \u4e2d\u6587\uff08\u9999\u6e2f\uff09",
            "ja - \u65e5\u8bed",
            "ko - \u97e9\u8bed",
            "fr - \u6cd5\u8bed",
            "fr-FR - \u6cd5\u8bed\uff08\u6cd5\u56fd\uff09",
            "de - \u5fb7\u8bed",
            "de-DE - \u5fb7\u8bed\uff08\u5fb7\u56fd\uff09",
            "es - \u897f\u73ed\u7259\u8bed",
            "es-ES - \u897f\u73ed\u7259\u8bed\uff08\u897f\u73ed\u7259\uff09",
            "es-MX - \u897f\u73ed\u7259\u8bed\uff08\u58a8\u897f\u54e5\uff09",
            "it - \u610f\u5927\u5229\u8bed",
            "it-IT - \u610f\u5927\u5229\u8bed\uff08\u610f\u5927\u5229\uff09",
            "pt - \u8461\u8404\u7259\u8bed",
            "pt-BR - \u8461\u8404\u7259\u8bed\uff08\u5df4\u897f\uff09",
            "pt-PT - \u8461\u8404\u7259\u8bed\uff08\u8461\u8404\u7259\uff09",
            "ru - \u4fc4\u8bed",
            "pl - \u6ce2\u5170\u8bed",
            "tr - \u571f\u8033\u5176\u8bed",
            "nl - \u8377\u5170\u8bed",
            "sv - \u745e\u5178\u8bed",
            "no - \u632a\u5a01\u8bed",
            "da - \u4e39\u9ea6\u8bed",
            "fi - \u82ac\u5170\u8bed",
            "ar - \u963f\u62c9\u4f2f\u8bed",
            "he - \u5e0c\u4f2f\u6765\u8bed",
            "hi - \u5370\u5730\u8bed",
            "th - \u6cf0\u8bed",
            "vi - \u8d8a\u5357\u8bed",
            "id - \u5370\u5ea6\u5c3c\u897f\u4e9a\u8bed",
            "ms - \u9a6c\u6765\u8bed",
            "uk - \u4e4c\u514b\u5170\u8bed",
            "cs - \u6377\u514b\u8bed",
            "hu - \u5308\u7259\u5229\u8bed",
            "ro - \u7f57\u9a6c\u5c3c\u4e9a\u8bed"
        });

        var okButton = new Button
        {
            Text = "\u786e\u5b9a",
            DialogResult = DialogResult.OK,
            Width = 88,
            Height = 28
        };

        var cancelButton = new Button
        {
            Text = "\u53d6\u6d88",
            DialogResult = DialogResult.Cancel,
            Width = 88,
            Height = 28
        };

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Location = new Point(16, 102),
            Size = new Size(398, 32),
            WrapContents = false
        };
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(okButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;

        Controls.Add(label);
        Controls.Add(_cultureBox);
        Controls.Add(buttonPanel);
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

    public string SelectedCulture => _cultureBox.SelectedItem as string ?? "";

    public DeleteCultureDialog(IEnumerable<string> cultures, string? preferredCulture)
    {
        Text = "\u5220\u9664\u8bed\u7cfb";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(430, 158);

        var label = new Label
        {
            Text = "\u9009\u62e9\u8981\u5220\u9664\u7684\u8bed\u7cfb",
            AutoSize = true,
            Location = new Point(16, 16)
        };

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

        var okButton = new Button
        {
            Text = "\u786e\u5b9a",
            DialogResult = DialogResult.OK,
            Width = 88,
            Height = 28
        };

        var cancelButton = new Button
        {
            Text = "\u53d6\u6d88",
            DialogResult = DialogResult.Cancel,
            Width = 88,
            Height = 28
        };

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Location = new Point(16, 102),
            Size = new Size(398, 32),
            WrapContents = false
        };
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(okButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;

        Controls.Add(label);
        Controls.Add(_cultureBox);
        Controls.Add(buttonPanel);
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
