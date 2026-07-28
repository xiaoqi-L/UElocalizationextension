using System.Globalization;
using System.Text.Json;

namespace LocalizationOverrideEditor;

internal enum EditorUiLanguage
{
    Chinese,
    English
}

internal enum EditorUiTheme
{
    Light,
    Dark
}

internal sealed class EditorUiThemeColors
{
    public required Color FormBackground { get; init; }
    public required Color PanelBackground { get; init; }
    public required Color SurfaceBackground { get; init; }
    public required Color PrimaryText { get; init; }
    public required Color SecondaryText { get; init; }
    public required Color InputBackground { get; init; }
    public required Color InputText { get; init; }
    public required Color Border { get; init; }
    public required Color GridLine { get; init; }
    public required Color GridBackground { get; init; }
    public required Color GridAlternateBackground { get; init; }
    public required Color GridHeaderBackground { get; init; }
    public required Color GridSelectionBackground { get; init; }
    public required Color GridSelectionText { get; init; }
    public required Color AccentBorder { get; init; }
    public required Color PrimaryButtonBackground { get; init; }
    public required Color PrimaryButtonText { get; init; }
    public required Color SecondaryButtonBackground { get; init; }
    public required Color SecondaryButtonText { get; init; }
    public required Color SecondaryButtonBorder { get; init; }
}

internal static class EditorUiThemePalette
{
    private static readonly EditorUiThemeColors Light = new()
    {
        FormBackground = Color.FromArgb(245, 247, 250),
        PanelBackground = Color.White,
        SurfaceBackground = Color.FromArgb(245, 247, 250),
        PrimaryText = Color.FromArgb(30, 41, 59),
        SecondaryText = Color.FromArgb(45, 55, 72),
        InputBackground = Color.White,
        InputText = Color.FromArgb(30, 41, 59),
        Border = Color.FromArgb(210, 216, 224),
        GridLine = Color.FromArgb(225, 230, 236),
        GridBackground = Color.White,
        GridAlternateBackground = Color.FromArgb(248, 250, 252),
        GridHeaderBackground = Color.FromArgb(248, 250, 252),
        GridSelectionBackground = Color.FromArgb(232, 240, 254),
        GridSelectionText = Color.FromArgb(30, 41, 59),
        AccentBorder = Color.FromArgb(37, 99, 235),
        PrimaryButtonBackground = Color.FromArgb(37, 99, 235),
        PrimaryButtonText = Color.White,
        SecondaryButtonBackground = Color.White,
        SecondaryButtonText = Color.FromArgb(45, 55, 72),
        SecondaryButtonBorder = Color.FromArgb(210, 216, 224)
    };

    private static readonly EditorUiThemeColors Dark = new()
    {
        FormBackground = Color.FromArgb(24, 24, 24),
        PanelBackground = Color.FromArgb(30, 30, 30),
        SurfaceBackground = Color.FromArgb(24, 24, 24),
        PrimaryText = Color.FromArgb(212, 212, 212),
        SecondaryText = Color.FromArgb(136, 136, 136),
        InputBackground = Color.FromArgb(37, 37, 38),
        InputText = Color.FromArgb(212, 212, 212),
        Border = Color.FromArgb(60, 60, 60),
        GridLine = Color.FromArgb(37, 37, 38),
        GridBackground = Color.FromArgb(24, 24, 24),
        GridAlternateBackground = Color.FromArgb(30, 30, 30),
        GridHeaderBackground = Color.FromArgb(30, 30, 30),
        GridSelectionBackground = Color.FromArgb(30, 58, 95),
        GridSelectionText = Color.FromArgb(212, 212, 212),
        AccentBorder = Color.FromArgb(43, 87, 151),
        PrimaryButtonBackground = Color.FromArgb(31, 78, 121),
        PrimaryButtonText = Color.FromArgb(224, 224, 224),
        SecondaryButtonBackground = Color.FromArgb(37, 37, 38),
        SecondaryButtonText = Color.FromArgb(212, 212, 212),
        SecondaryButtonBorder = Color.FromArgb(60, 60, 60)
    };

    public static EditorUiThemeColors Get(EditorUiTheme theme) =>
        theme == EditorUiTheme.Dark ? Dark : Light;
}

internal static class EditorUiStrings
{
    private static readonly Dictionary<string, (string Zh, string En)> Strings = new(StringComparer.Ordinal)
    {
        ["AppTitle"] = ("多语言编辑工具", "Localization Editor"),
        ["RootDirectoryLabel"] = ("本地化目录/项目目录", "Localization / Project Directory"),
        ["Browse"] = ("浏览", "Browse"),
        ["SearchPlaceholder"] = ("搜索文本", "Search text"),
        ["FilterLanguage"] = ("语言", "Language"),
        ["FilterState"] = ("状态", "Status"),
        ["DefaultCulture"] = ("启动语言", "Startup Language"),
        ["FilterAll"] = ("全部", "All"),
        ["FilterMissing"] = ("空翻译", "Missing"),
        ["FilterTranslated"] = ("已翻译", "Translated"),
        ["Reload"] = ("重新加载", "Reload"),
        ["Save"] = ("保存", "Save"),
        ["AddCulture"] = ("添加语系", "Add Culture"),
        ["DeleteCulture"] = ("删除语系", "Delete Culture"),
        ["ToggleLanguage"] = ("English", "中文"),
        ["ToggleThemeDark"] = ("深色", "Dark"),
        ["ToggleThemeLight"] = ("浅色", "Light"),
        ["Ok"] = ("确定", "OK"),
        ["Cancel"] = ("取消", "Cancel"),
        ["ConfirmExit"] = ("确认退出", "Confirm Exit"),
        ["ConfirmExitMessage"] = ("当前有未保存的修改，确定不保存直接退出吗？", "You have unsaved changes. Exit without saving?"),
        ["ConfirmReload"] = ("确认重新加载", "Confirm Reload"),
        ["ConfirmReloadMessage"] = ("当前有未保存的修改。\r\n\r\n是否放弃这些修改并重新加载？", "You have unsaved changes.\r\n\r\nDiscard them and reload?"),
        ["ChooseDirectoryDescription"] = ("选择 LocalizationOverrides 目录、UE 打包目录或项目根目录", "Select LocalizationOverrides folder, packaged build root, or project root"),
        ["LoadFailed"] = ("加载失败", "Load Failed"),
        ["SaveFailed"] = ("保存失败", "Save Failed"),
        ["StartupFailed"] = ("工具启动失败。详细错误已写入：{0}\r\n\r\n{1}", "Failed to start the tool. Details written to: {0}\r\n\r\n{1}"),
        ["InvalidCulture"] = ("无效语系", "Invalid Culture"),
        ["InvalidCultureMessage"] = ("语系代码只能包含字母、数字、短横线和下划线。", "Culture codes may only contain letters, digits, hyphens, and underscores."),
        ["DuplicateCulture"] = ("重复语系", "Duplicate Culture"),
        ["DuplicateCultureMessage"] = ("语系 '{0}' 已经存在。", "Culture '{0}' already exists."),
        ["CannotDeleteCulture"] = ("无法删除语系", "Cannot Delete Culture"),
        ["CannotDeleteLastCulture"] = ("至少需要保留一个语系，无法继续删除。", "At least one culture must remain."),
        ["CannotDeleteNativeCulture"] = ("无法删除源语系", "Cannot Delete Native Culture"),
        ["CannotDeleteNativeCultureMessage"] = ("语系 '{0}' 是 Game.json 的 nativeCulture，不能在此工具中删除。", "Culture '{0}' is the nativeCulture in Game.json and cannot be deleted here."),
        ["ConfirmDeleteCulture"] = ("确认删除语系", "Confirm Delete Culture"),
        ["ConfirmDeleteCultureMessage"] = ("确定要删除语系 '{0}' 吗？\r\n\r\n该语系会从 languages.json 的 cultures 中移除，并从 Game.json 每条 translations 中移除对应字段。\r\n此操作需要点击保存后才会写入文件。", "Delete culture '{0}'?\r\n\r\nIt will be removed from languages.json cultures and from each Game.json translations entry.\r\nClick Save to write changes to disk."),
        ["AddCultureDialogTitle"] = ("添加语系", "Add Culture"),
        ["AddCultureDialogLabel"] = ("语系代码（例如 ja、ko、fr、zh-Hant）", "Culture code (e.g. ja, ko, fr, zh-Hant)"),
        ["DeleteCultureDialogTitle"] = ("删除语系", "Delete Culture"),
        ["DeleteCultureDialogLabel"] = ("选择要删除的语系", "Select culture to delete"),
        ["StatusUnsaved"] = ("有未保存的修改。", "Unsaved changes."),
        ["StatusDefaultCultureChanged"] = ("启动语言已更改，点击保存后生效。", "Startup language changed. Click Save to apply."),
        ["StatusLoaded"] = ("已加载 {0} 条文本，启动语言：{1}。路径：{2}", "Loaded {0} entries, startup language: {1}. Path: {2}"),
        ["StatusNoValidJson"] = ("未加载有效的本地化 JSON。", "No valid localization JSON loaded."),
        ["StatusLoadFailedKeepPrevious"] = ("加载失败，仍使用上一个有效目录：{0}", "Load failed; still using previous directory: {0}"),
        ["StatusNothingToUndo"] = ("没有可撤销的修改。", "Nothing to undo."),
        ["StatusUndone"] = ("已撤销 {0} 个单元格修改，有未保存的修改。", "Undid {0} cell change(s). Unsaved changes remain."),
        ["StatusPasted"] = ("已粘贴 {0} 个单元格，有未保存的修改。", "Pasted {0} cell(s). Unsaved changes remain."),
        ["StatusCultureAdded"] = ("已添加语系 {0}，点击保存后生效。", "Added culture {0}. Click Save to apply."),
        ["StatusCultureDeleted"] = ("已删除语系 {0}，点击保存后生效。", "Deleted culture {0}. Click Save to apply."),
        ["StatusSaved"] = ("已保存，启动语言：{0}。", "Saved. Startup language: {0}."),
        ["StatusSavedBackupPruneFailed"] = ("文件已保存，但旧备份裁剪失败：{0}", "Saved, but backup cleanup failed: {0}")
    };

    public static EditorUiLanguage Current { get; private set; } = EditorUiLanguage.Chinese;
    public static EditorUiTheme CurrentTheme { get; private set; } = EditorUiTheme.Light;

    public static void SetLanguage(EditorUiLanguage language) => Current = language;

    public static void SetTheme(EditorUiTheme theme) => CurrentTheme = theme;

    public static string GetToggleThemeLabel() =>
        Get(CurrentTheme == EditorUiTheme.Light ? "ToggleThemeDark" : "ToggleThemeLight");

    public static string Get(string key) =>
        Strings.TryGetValue(key, out var pair)
            ? Current == EditorUiLanguage.English ? pair.En : pair.Zh
            : key;

    public static string Format(string key, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, Get(key), args);

    public static string GetCultureDisplayName(string culture) =>
        Current == EditorUiLanguage.English
            ? CultureDisplayHelper.GetEnglishName(culture)
            : CultureDisplayHelper.GetChineseName(culture);

    public static string GetCulturePresetLabel(string culture) =>
        $"{culture} - {GetCultureDisplayName(culture)}";
}

internal static class EditorUiSettings
{
    private sealed class SettingsDocument
    {
        public string? UiLanguage { get; set; }
        public string? UiTheme { get; set; }
    }

    public static EditorUiLanguage LoadLanguage() => LoadSettings().Language;

    public static EditorUiTheme LoadTheme() => LoadSettings().Theme;

    public static void SaveLanguage(EditorUiLanguage language) =>
        SaveSettings(language, EditorUiStrings.CurrentTheme);

    public static void SaveTheme(EditorUiTheme theme) =>
        SaveSettings(EditorUiStrings.Current, theme);

    private static (EditorUiLanguage Language, EditorUiTheme Theme) LoadSettings()
    {
        foreach (var path in GetSettingsPaths())
        {
            try
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                var settings = JsonSerializer.Deserialize<SettingsDocument>(File.ReadAllText(path));
                var language = EditorUiLanguage.Chinese;
                if (string.Equals(settings?.UiLanguage, "en", StringComparison.OrdinalIgnoreCase))
                {
                    language = EditorUiLanguage.English;
                }
                else if (string.Equals(settings?.UiLanguage, "zh", StringComparison.OrdinalIgnoreCase))
                {
                    language = EditorUiLanguage.Chinese;
                }

                var theme = string.Equals(settings?.UiTheme, "dark", StringComparison.OrdinalIgnoreCase)
                    ? EditorUiTheme.Dark
                    : EditorUiTheme.Light;

                return (language, theme);
            }
            catch
            {
            }
        }

        return (EditorUiLanguage.Chinese, EditorUiTheme.Light);
    }

    private static void SaveSettings(EditorUiLanguage language, EditorUiTheme theme)
    {
        var json = JsonSerializer.Serialize(new SettingsDocument
        {
            UiLanguage = language == EditorUiLanguage.English ? "en" : "zh",
            UiTheme = theme == EditorUiTheme.Dark ? "dark" : "light"
        }, new JsonSerializerOptions { WriteIndented = true });

        foreach (var path in GetSettingsPaths())
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, json);
                return;
            }
            catch
            {
            }
        }
    }

    private static IEnumerable<string> GetSettingsPaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "ui-settings.json");
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(appData))
            yield return Path.Combine(appData, "UELocalizationTool", "ui-settings.json");
    }
}

internal static class CultureDisplayHelper
{
    private static readonly Dictionary<string, string> ChineseNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "英语", ["en-US"] = "英语（美国）", ["en-GB"] = "英语（英国）",
        ["zh"] = "中文", ["zh-CN"] = "中文（中国大陆）", ["zh-Hans"] = "简体中文", ["zh-Hant"] = "繁体中文",
        ["zh-TW"] = "中文（台湾）", ["zh-HK"] = "中文（香港）", ["ja"] = "日语", ["ko"] = "韩语",
        ["fr"] = "法语", ["fr-FR"] = "法语（法国）", ["de"] = "德语", ["de-DE"] = "德语（德国）",
        ["es"] = "西班牙语", ["es-ES"] = "西班牙语（西班牙）", ["es-MX"] = "西班牙语（墨西哥）",
        ["it"] = "意大利语", ["it-IT"] = "意大利语（意大利）", ["pt"] = "葡萄牙语",
        ["pt-BR"] = "葡萄牙语（巴西）", ["pt-PT"] = "葡萄牙语（葡萄牙）", ["ru"] = "俄语",
        ["pl"] = "波兰语", ["tr"] = "土耳其语", ["nl"] = "荷兰语", ["sv"] = "瑞典语", ["no"] = "挪威语",
        ["da"] = "丹麦语", ["fi"] = "芬兰语", ["ar"] = "阿拉伯语", ["he"] = "希伯来语", ["hi"] = "印地语",
        ["th"] = "泰语", ["vi"] = "越南语", ["id"] = "印度尼西亚语", ["ms"] = "马来语", ["uk"] = "乌克兰语",
        ["cs"] = "捷克语", ["hu"] = "匈牙利语", ["ro"] = "罗马尼亚语"
    };

    public static readonly string[] PresetCultureCodes =
    [
        "en", "en-US", "en-GB", "zh", "zh-CN", "zh-Hans", "zh-Hant", "zh-TW", "zh-HK",
        "ja", "ko", "fr", "fr-FR", "de", "de-DE", "es", "es-ES", "es-MX", "it", "it-IT",
        "pt", "pt-BR", "pt-PT", "ru", "pl", "tr", "nl", "sv", "no", "da", "fi", "ar", "he",
        "hi", "th", "vi", "id", "ms", "uk", "cs", "hu", "ro"
    ];

    public static string GetChineseName(string culture) =>
        ChineseNames.TryGetValue(culture, out var name) ? name : culture;

    public static string GetEnglishName(string culture)
    {
        try { return CultureInfo.GetCultureInfo(culture.Replace('_', '-')).EnglishName; }
        catch { return culture; }
    }
}
