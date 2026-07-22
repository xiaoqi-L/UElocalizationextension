# UE Localization Override Tool

## Files to ship

Copy these items to the packaged build root, next to the game executable:

- `UELocalizationTool.exe`
- `LocalizationOverrides/languages.json`
- `LocalizationOverrides/Game.json`

The editor writes `LocalizationOverrides/Game.json` and `LocalizationOverrides/languages.json`, and creates timestamped `.bak` backups. It does not modify the game's existing `.pak`, `.utoc`, `.ucas`, or executable.

`LocalizationOverrides/*.json` files must be saved as UTF-16 LE with BOM. The Unreal plugin generator and this tool both write that encoding, and the tool rejects other encodings to avoid mixed-format JSON files.

The editor keeps only the latest three backup groups. Each save creates one group with the same timestamp for `Game.json` and `languages.json`, then older `.bak` groups are removed.

## Unreal integration

The `LocalizationOverrides` runtime plugin loads JSON overrides from:

- `<LaunchDir>/LocalizationOverrides`
- `<ProjectDir>/LocalizationOverrides`
- `<ProjectContentDir>/../LocalizationOverrides`

Blueprint nodes:

- `GetAvailableCultures`
- `SetGameCulture`
- `ReloadLocalizationOverrides`
- `GetLocalizationOverridesDirectory`
- `GetCurrentGameCulture`
- `LocalizeString`
- `LocalizeStringArray`
- `LocalizeMultilineString`

Call `SetGameCulture("zh", false)` or `SetGameCulture("en", true)` from a settings UI. It changes the current UE culture and refreshes already registered JSON text immediately. Passing `true` writes `defaultCulture` to `languages.json` for the next process launch; it does not write UE user config. JSON files are treated as process-start data: if they change while the game is running, restart the game to avoid stale Polyglot entries.

The tool writes `defaultCulture` in `LocalizationOverrides/languages.json`. On startup, the plugin applies a valid `defaultCulture` unless an explicit UE `-culture`, `-language`, or `-locale` argument is present.

In the UE editor, JSON generation is explicit. Use the `LocalizationOverrides.Generate` console command when the project manifest/archive data needs to be regenerated; the editor no longer rewrites `Game.json` on every startup.

## Packaging

The runtime module declares the existing project JSON files as Non-UFS runtime dependencies. Generate or save the JSON before packaging, then build the game target normally:

```powershell
RunUAT.bat BuildCookRun -project="F:\UGit\localizedlanguage\language.uproject" -platform=Win64 -clientconfig=Shipping -build -cook -stage -pak -iostore
```

`LocalizationOverrides.Build.cs` validates UTF-16 LE BOM JSON and stages only top-level `.json` files beside the actual game executable under `Binaries/Win64/LocalizationOverrides`. `.bak` and temporary files are not shipped. Missing or invalid JSON fails the game build. Adding or removing a target JSON requires rebuilding the game target so UnrealBuildTool refreshes the runtime dependency receipt.

Texts stored as Unreal `FText` can be overridden automatically by the plugin. Blueprint `String` values are not localization-aware and must be converted to `FText` or handled by project-specific display logic.

## UE native localized assets

Localized assets use Unreal's built-in asset localization. Place the localized variant at the same relative path below `Content/L10N/<culture>/`, for example:

```text
Content/Mesh/SM_UI.uasset
Content/L10N/en/Mesh/SM_UI.uasset
```

Package the game normally with the required cultures staged. UE cooks these assets into the main game package and resolves the appropriate `/Game/L10N/<culture>/...` asset as content loads for the active culture. When changing culture after assets are already loaded, restart the game to ensure native localized assets are reloaded.

There is no post-package asset patch, container mount, or asset installation workflow. Replacing or adding localized assets requires changing the UE project and repackaging the main game.

## Publish the tool

```powershell
dotnet publish Tools\LocalizationOverrideEditor\LocalizationOverrideEditor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Tools\LocalizationOverrideEditor\publish
```

The output executable is `Tools\LocalizationOverrideEditor\publish\UELocalizationTool.exe`.
