# LocalizationOverrides 插件

`LocalizationOverrides` 是一个面向 Unreal Engine 项目的运行时文本本地化覆盖插件。它把 UE 本地化数据导出为项目根目录下可编辑的 JSON，并在游戏启动时将 JSON 注册为 Polyglot Text Data，从而允许在不重新 Cook 主包的情况下修改 `FText` 翻译。

插件同时提供：

- 运行时 JSON 文本覆盖；
- 启动语言选择与蓝图切换接口；
- UE 本地化数据到 JSON 的手动生成命令；
- 打包时将现有 JSON 以 Non-UFS 文件复制到游戏 EXE 旁；
- 配套的中文桌面文本编辑工具。

> 插件只覆盖文本。模型、贴图、材质和音频仍使用 UE 原生资产本地化机制，不支持打包后通过本插件注入或替换资产。

## 适用范围

- Unreal Engine 5.x；插件二进制必须与具体引擎版本匹配。
- 当前工程主要按 UE 5.7 构建和验证。
- 使用 UE `FText` 的文本，包括可本地化的 UMG 文本。
- Windows 打包目标；其他平台需要重新验证输出目录和读写权限。

普通 `FString`、Blueprint String 和写死在图片或模型中的文字不会自动获得本地化覆盖。

## 目录结构

### 项目开发目录

```text
<Project>/
├─ LocalizationOverrides/
│  ├─ languages.json
│  └─ Game.json
├─ Plugins/
│  └─ LocalizationOverrides/
└─ <ProjectName>.uproject
```

JSON 属于项目数据，必须放在项目根目录的 `LocalizationOverrides` 中，不放在插件的 `Content` 目录中。

### 打包后目录

插件通过 `RuntimeDependencies` 将现有 JSON 作为 Non-UFS 文件复制到实际游戏 EXE 旁：

```text
<PackagedGame>/
└─ <ProjectName>/
   └─ Binaries/
      └─ Win64/
         ├─ <ProjectName>-Win64-Shipping.exe
         └─ LocalizationOverrides/
            ├─ languages.json
            └─ Game.json
```

这是运行时优先读取的位置。不要再在打包目录最外层手工放置第二份 `LocalizationOverrides`，否则容易造成编辑了错误副本的误判。

## 安装

1. 将插件目录复制到：

   ```text
   <Project>/Plugins/LocalizationOverrides
   ```

2. 在项目的 `.uproject` 中启用插件，或在 UE 的“编辑 > 插件”中启用。
3. 如果插件二进制与当前 UE 版本不匹配，关闭编辑器后重新编译插件或项目。
4. 重新打开项目。

推荐在 `.uproject` 中显式声明：

```json
{
  "Name": "LocalizationOverrides",
  "Enabled": true
}
```

## 首次配置流程

1. 在 UE 的本地化控制板中创建并配置本地化目标，例如 `Game`。
2. 设置 Native Culture，并添加需要支持的 culture，例如：

   ```text
   zh-Hans
   en
   de
   ja
   ```

3. 执行文本收集、翻译和导出，确保项目中存在可读取的 manifest/archive 数据。
4. 打开编辑器输出日志或控制台，执行：

   ```text
   LocalizationOverrides.Generate
   ```

5. 确认项目根目录已经生成：

   ```text
   LocalizationOverrides/languages.json
   LocalizationOverrides/Game.json
   ```

6. 使用配套工具编辑翻译，或直接按 schema 修改 JSON。
7. 正常 Build 并打包项目。

插件不会在打开编辑器或打包前自动运行生成器。需要从 UE 本地化数据重新同步 JSON 时，必须显式执行生成命令。

## JSON 文件格式

所有 JSON 文件使用：

- Schema `version`：`1`；
- 编码：UTF-16 LE；
- 文件头：BOM；
- 空翻译：表示未翻译，运行时回退到源文本或 UE 已有本地化结果。

### languages.json

```json
{
  "version": 1,
  "defaultCulture": "zh-Hans",
  "cultures": [
    "zh-Hans",
    "en",
    "de"
  ]
}
```

字段说明：

- `version`：必须为 `1`；
- `defaultCulture`：没有显式 UE 命令行语言时使用的启动语言；
- `cultures`：允许使用的 culture 列表，值必须非空且不能重复；
- `defaultCulture` 必须存在于 `cultures` 中。

### Game.json

```json
{
  "version": 1,
  "target": "Game",
  "nativeCulture": "zh-Hans",
  "entries": [
    {
      "Namespace": "UI.MainMenu",
      "Key": "StartGame",
      "Source": "开始游戏",
      "translations": {
        "zh-Hans": "开始游戏",
        "en": "Start Game",
        "de": "Spiel starten"
      }
    }
  ]
}
```

文本的唯一身份是 `Namespace + Key`，不是单独的 `Key`。同一文件中不能出现重复的复合身份。

`nativeCulture` 表示源文本所属 culture，与 `languages.json.defaultCulture` 的启动语言含义不同，不能互相替代。

## 启动语言优先级

启动时按以下顺序决定最终 culture：

1. UE 显式命令行参数：`-culture`、`-language` 或 `-locale`；
2. `languages.json.defaultCulture`；
3. UE 系统语言。

示例：

```text
MyGame.exe -culture=en
```

显式命令行参数优先于 JSON 默认值。

## 蓝图运行时接口

使用蓝图节点：

```text
Set Game Culture
├─ Culture
├─ Save as Default Culture
└─ Return Value
```

### 临时切换

```text
Culture = en
Save as Default Culture = false
```

只切换当前进程的文本语言，不修改 `languages.json`。

### 保存为下次启动语言

```text
Culture = en
Save as Default Culture = true
```

验证 culture 有效后，将 `languages.json.defaultCulture` 安全更新为 `en`。下次启动且没有命令行覆盖时使用该语言。

返回值为 `false` 时，检查 Output Log。常见原因包括：

- culture 未在 `languages.json.cultures` 中声明；
- JSON 格式或编码无效；
- 文件只读或当前进程没有写权限；
- 接口不是从 Game Thread 调用。

### 关于打包后的写入权限

Shipping 游戏中的 JSON 位于 EXE 旁。若游戏安装在 `Program Files` 等受保护目录，普通用户可能没有写权限，此时勾选 `Save as Default Culture` 会失败。正式产品如需可靠保存用户选择，建议把用户偏好保存在 `Saved/Config`，而不要依赖修改安装目录中的 JSON。

## 桌面文本编辑工具

配套程序位于：

```text
Tools/LocalizationOverrideEditor/publish/UELocalizationTool.exe
```

主要功能：

- 编辑 `Game.json` 中的多语系文本；
- 添加和删除 culture；
- 设置 `languages.json.defaultCulture`；
- 搜索、筛选和批量粘贴翻译；
- 保存前进行 schema 校验；
- 检测文件是否被其他程序修改；
- UTF-16 LE BOM 原子保存、失败回滚，并保留最近三组备份。

使用步骤：

1. 启动 `UELocalizationTool.exe`；
2. 选择包含 `LocalizationOverrides` 的项目根目录，或让工具自动定位；
3. 编辑翻译和启动语言；
4. 点击“保存”；
5. 若 UE 生成器随后再次执行，应确认生成合并结果，避免把人工翻译替换为 UE archive 中的旧内容。

## 无界面生成方式

除编辑器控制台命令外，也可以关闭普通编辑器进程后执行 Commandlet：

```bat
UnrealEditor-Cmd.exe "<Project>.uproject" -run=LocalizationOverridesGenerate -unattended -nop4 -utf8output
```

生成失败会返回非零退出码。多个本地化目标采用整体事务：任一目标失败时，不提交部分生成结果。

## 打包行为

打包阶段不会自动生成或更新 JSON，只复制项目根目录中已经存在并通过校验的 `.json` 文件。

规则如下：

- 必须存在 `languages.json`；
- 必须至少存在一个目标 JSON；
- 文件必须是 UTF-16 LE BOM；
- 文件必须能够解析为有效 JSON；
- `.bak`、`.tmp` 和其他非 JSON 文件不会发布；
- JSON 使用 `StagedFileType.NonUFS`，不会进入 `.pak`、`.utoc` 或 `.ucas`；
- 缺少或损坏 JSON 时，项目 Build 会明确失败，防止发布不完整版本；
- 使用旧 receipt 并跳过 Build，可能不会发现新增或删除的 JSON，修改文件集合后应重新 Build。

## UE 原生资产本地化

模型、贴图和音频必须使用 UE 原生目录结构：

```text
Content/L10N/<culture>/<原资产相对路径>
```

例如：

```text
Content/Mesh/SM_UI.uasset
Content/L10N/en/Mesh/SM_UI.uasset
```

注意：

- 本地化资产必须在打包前创建并 Cook；
- 没有对应本地化变体时，UE 自动回退到原始资产；
- 运行中切换 culture 可以立即刷新文本，但已经加载的模型、贴图和音频通常需要重新载入关卡或重启进程；
- 修改或新增资产版本后必须重新打包游戏，本插件不提供打包后资产补丁功能。

## 运行时加载位置

打包游戏优先从以下目录读取：

```text
FPlatformProcess::BaseDir()/LocalizationOverrides
```

也就是实际游戏二进制文件旁。编辑器和独立进程调试则优先使用项目根目录中的：

```text
<Project>/LocalizationOverrides
```

因此，在编辑器中修改项目 JSON 后启动独立进程，应读取项目中的最新内容；在 Shipping 包中则应修改或检查 EXE 旁的副本。

## 常见问题

### 执行 Generate 后提示找不到 `.manifest`

先在 UE 本地化控制板中执行文本收集并生成对应目标数据。生成器不能从不存在的 manifest/archive 创建翻译条目。

### 新增 culture 后没有显示翻译

确认以下内容：

1. culture 已加入 `languages.json.cultures`；
2. `defaultCulture` 或蓝图传入值使用完全相同的 culture 名称；
3. `Game.json.translations` 中存在该 culture 的非空翻译；
4. 修改的是当前运行环境实际读取的 JSON；
5. JSON 编码为 UTF-16 LE BOM；
6. Output Log 中没有 schema、线程或文件读取错误。

### 设置语言后文本变化，但模型或贴图没有变化

这是文本覆盖与 UE 原生资产本地化加载时机不同造成的。重新载入关卡或重启进程；并确认对应资产已经放入 `Content/L10N/<culture>` 且被 Cook。

### 打包后出现两个 LocalizationOverrides

正确位置只有实际 Shipping EXE 旁的一份。若包体外层还存在一份，通常是旧版 AutomationTool staging handler 的缓存或旧打包配置造成的，应清理旧的 AutomationTool 模块缓存和旧输出目录后重新 Build。

### 每次生成会不会覆盖人工翻译

生成器会从 UE manifest/archive 同步数据，并尽量保留没有 UE archive 的手工 culture 翻译。仍建议在生成前备份 JSON，并明确团队中“UE 数据同步”和“外部工具翻译编辑”的执行顺序。

## 日志

在 UE Output Log 中搜索：

```text
LocalizationOverrides
LocalizationOverridesGenerator
```

日志会记录：

- 最终 culture 及来源；
- JSON 加载、校验和注册结果；
- 外部文件在运行中发生变化的提示；
- 生成目标数量、条目数量和失败原因；
- 保存默认 culture 的失败原因。

## 版本升级建议

- 不同 UE 小版本之间不要直接复用旧的插件 DLL；应使用对应引擎重新编译。
- 升级插件后建议清理项目中该插件的 `Binaries` 和 `Intermediate`，再完整编译。
- 不要删除项目的 `Content`、`Config`、`LocalizationOverrides` 或其他资产数据。
- 从曾使用旧 staging handler 的版本升级时，确认包体只保留 EXE 旁的一份 JSON 目录。

## 功能边界总结

| 功能 | 是否支持 |
| --- | --- |
| 打包后修改 `FText` 翻译 | 支持 |
| 运行时切换文本 culture | 支持 |
| 保存 JSON 默认启动语言 | 支持，但受目录写权限影响 |
| 打包时复制现有 JSON | 支持 |
| 打包前自动生成 JSON | 不支持，需手动执行 |
| 普通 `FString` 自动本地化 | 不支持 |
| UE 原生本地化资产回退 | 支持，由 UE 负责 |
| 打包后注入 FBX、PNG、WAV | 不支持 |
| 打包后替换模型、贴图、音频 | 不支持 |

