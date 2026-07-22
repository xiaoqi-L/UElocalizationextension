param(
    [string]$EngineRoot = "D:\UrealEngine\UE_5.7",
    [string]$ProjectRoot = "F:\UGit\localizedlanguage",
    [string]$PackageDir = "F:\UGit\localizedlanguage\Dist\LocalizationOverrides"
)

$ErrorActionPreference = "Stop"

$pluginFile = Join-Path $ProjectRoot "Plugins\LocalizationOverrides\LocalizationOverrides.uplugin"
$runUat = Join-Path $EngineRoot "Engine\Build\BatchFiles\RunUAT.bat"

& $runUat BuildPlugin "-Plugin=$pluginFile" "-Package=$PackageDir" -TargetPlatforms=Win64
if ($LASTEXITCODE -ne 0) {
    throw "BuildPlugin failed with exit code $LASTEXITCODE."
}

$runtimeRules = Join-Path $PackageDir "Source\LocalizationOverrides\LocalizationOverrides.Build.cs"
$editorRules = Join-Path $PackageDir "Source\LocalizationOverridesEditor\LocalizationOverridesEditor.Build.cs"

function Enable-PrecompiledConsumption([string]$RulesFile) {
    $text = Get-Content -Raw -LiteralPath $RulesFile
    if ($text -notmatch "bUsePrecompiled\s*=\s*true") {
        $text = $text -replace "(PrecompileForTargets\s*=\s*PrecompileTargetsType\.[^;]+;)", "`$1`r`n`t`tbUsePrecompiled = true;"
        Set-Content -LiteralPath $RulesFile -Value $text -Encoding UTF8
    }
}

Enable-PrecompiledConsumption $runtimeRules
Enable-PrecompiledConsumption $editorRules

$requiredFiles = @(
    (Join-Path $PackageDir "LocalizationOverrides.uplugin"),
    (Join-Path $PackageDir "Binaries\Win64\UnrealEditor-LocalizationOverrides.dll"),
    (Join-Path $PackageDir "Binaries\Win64\UnrealEditor-LocalizationOverridesEditor.dll"),
    (Join-Path $PackageDir "Intermediate\Build\Win64\x64\UnrealGame\Development\LocalizationOverrides\LocalizationOverrides.precompiled"),
    (Join-Path $PackageDir "Intermediate\Build\Win64\x64\UnrealGame\Shipping\LocalizationOverrides\LocalizationOverrides.precompiled")
)

foreach ($file in $requiredFiles) {
    if (!(Test-Path -LiteralPath $file)) {
        throw "Missing expected packaged plugin file: $file"
    }
}

Write-Host "Packaged LocalizationOverrides plugin is ready: $PackageDir"
