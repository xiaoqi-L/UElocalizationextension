# UELocalizationTool

`UELocalizationTool.exe` 是 [LocalizationOverrides](../../README.md) 插件的配套桌面编辑器，用于编辑 `<Project>/LocalizationOverrides/` 下的 `languages.json` 与 `Game.json`。

**完整文档（安装、JSON 规范、插件集成、打包、FAQ）请参阅根目录 [README.md](../../README.md)。**

## 快速使用

1. 运行 [`publish/UELocalizationTool.exe`](publish/UELocalizationTool.exe)（约 108 MB，自包含 .NET 9，无需安装运行时）
2. 选择项目根目录或打包目录（含 `LocalizationOverrides/`）
3. 编辑翻译，点击「保存」

## 自行编译

```powershell
dotnet publish Tools\LocalizationOverrideEditor\LocalizationOverrideEditor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Tools\LocalizationOverrideEditor\publish
```

## 源码

- [`Program.cs`](Program.cs) — 主窗体与 JSON 读写
- [`EditorUiStrings.cs`](EditorUiStrings.cs) — 双语 UI 与主题配色
- [`EditorThemeControls.cs`](EditorThemeControls.cs) — 暗色 ComboBox 等控件
