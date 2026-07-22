param(
    [string]$EngineRoot = "D:\UrealEngine\UE_5.7",
    [string]$ProjectFile = "",
    [string]$StagingDirectory = "",
    [ValidateSet("Development", "Shipping", "Test", "DebugGame", "Debug")]
    [string]$ClientConfig = "Shipping"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($ProjectFile)) {
    $projectFiles = @(Get-ChildItem -LiteralPath $projectRoot -Filter "*.uproject" -File)
    if ($projectFiles.Count -eq 0) {
        throw "No .uproject file was found in project root: $projectRoot"
    }
    if ($projectFiles.Count -gt 1) {
        $candidates = ($projectFiles.FullName -join ", ")
        throw "Multiple .uproject files were found in project root. Specify -ProjectFile explicitly: $candidates"
    }
    $ProjectFile = $projectFiles[0].FullName
}
else {
    $ProjectFile = [System.IO.Path]::GetFullPath($ProjectFile)
}

if ([string]::IsNullOrWhiteSpace($StagingDirectory)) {
    $StagingDirectory = Join-Path $projectRoot "Saved\StagedBuilds\LocalizationOverrides"
}
else {
    $StagingDirectory = [System.IO.Path]::GetFullPath($StagingDirectory)
}

$runUat = Join-Path $EngineRoot "Engine\Build\BatchFiles\RunUAT.bat"
if (!(Test-Path -LiteralPath $runUat)) {
    throw "RunUAT.bat was not found: $runUat"
}
if (!(Test-Path -LiteralPath $ProjectFile)) {
    throw "Project file was not found: $ProjectFile"
}

$arguments = @(
    "BuildCookRun",
    "-project=$ProjectFile",
    "-noP4",
    "-platform=Win64",
    "-clientconfig=$ClientConfig",
    "-build",
    "-cook",
    "-stage",
    "-pak",
    "-iostore",
    "-stagingdirectory=$StagingDirectory"
)

Write-Host "Packaging with LocalizationOverrides runtime dependencies..."
& $runUat @arguments
if ($LASTEXITCODE -ne 0) {
    throw "BuildCookRun failed with exit code $LASTEXITCODE."
}

$projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectFile)
$publishedDirectory = Join-Path $StagingDirectory "Windows\$projectName\Binaries\Win64\LocalizationOverrides"
$publishedFiles = @(Get-ChildItem -LiteralPath $publishedDirectory -Filter "*.json" -File -ErrorAction SilentlyContinue)
if ($publishedFiles.Count -lt 2) {
    throw "Packaging completed but LocalizationOverrides JSON files were not staged: $publishedDirectory"
}

$unexpectedFiles = @(Get-ChildItem -LiteralPath $publishedDirectory -File | Where-Object { $_.Extension -ne ".json" })
if ($unexpectedFiles.Count -gt 0) {
    throw "Unexpected non-JSON files were staged in LocalizationOverrides: $($unexpectedFiles.Name -join ', ')"
}

Write-Host "LocalizationOverrides staged to: $publishedDirectory"
$publishedFiles | ForEach-Object { Write-Host "  $($_.Name)" }
