# UELocalizationTool

`UELocalizationTool.exe` 是 [LocalizationOverrides](../../README.md) 插件的配套桌面编辑器，用于编辑 `<Project>/LocalizationOverrides/` 下的 `languages.json` 与 `Game.json`。

**完整文档（安装、JSON 规范、插件集成、打包、FAQ）请参阅根目录 [README.md](../../README.md)。**

## 快速使用

1. 运行 [`publish/UELocalizationTool.exe`](publish/UELocalizationTool.exe)（约 108 MB，自包含 .NET 9，无需安装运行时）
2. 输入或浏览项目根目录 / 打包目录 / `LocalizationOverrides` 路径，回车或点「重新加载」
3. 编辑翻译，点击「保存」

## 桌面部署

- exe 可放在任意位置（桌面等）；启动时**不会**扫描 exe 旁的项目目录
- 路径来源仅两种：
  1. exe 同目录下的 `ui-settings.json`（`lastRootDirectory`）
  2. 手动输入或浏览
- 首次打开、旁路无 settings → 路径框为空
- 上次路径仍有效（目录存在且含 `languages.json` + `Game.json`，或其一层子目录 `LocalizationOverrides` 有效）→ 自动加载
- 上次路径失效 → 清空路径与 settings 记录
- 加载时仅从输入路径**向下**最多 5 层查找含 JSON 的 `LocalizationOverrides`；成功后路径框改写为最终目录并写入 exe 旁 settings
- `%LocalAppData%` 不参与路径持久化

## 筛选编辑

筛选后修改单元格时，列表**不会**立刻按新状态重筛；需再次切换筛选条件或修改搜索框才会刷新。

## 自行编译

```powershell
dotnet publish Tools\LocalizationOverrideEditor\LocalizationOverrideEditor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Tools\LocalizationOverrideEditor\publish
```

## 源码

- [`Program.cs`](Program.cs) — 主窗体与 JSON 读写
- [`EditorUiStrings.cs`](EditorUiStrings.cs) — 双语 UI 与主题配色
- [`EditorThemeControls.cs`](EditorThemeControls.cs) — 暗色 ComboBox 等控件
