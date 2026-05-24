param(
    [Parameter(Mandatory = $true)]
    [string]$TranslationFileName
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptRoot 'civtranslate-interactive/civtranslate-interactive.csproj'

if ([string]::IsNullOrWhiteSpace($TranslationFileName)) {
    throw 'A translation file name is required (example: story_german or civ_german).'
}

if ($TranslationFileName.Contains('/') -or $TranslationFileName.Contains('\')) {
    throw 'Please pass only a file name from the translation folder, not a path.'
}

$baseName = [System.IO.Path]::GetFileNameWithoutExtension($TranslationFileName)
if ([string]::IsNullOrWhiteSpace($baseName)) {
    throw 'Invalid translation file name.'
}

$translationFile = "$baseName.txt"
$translationPath = Join-Path (Join-Path $scriptRoot 'translation') $translationFile

Write-Host 'Step 1/3: Check whether translation/all.txt must be generated.'
if ($baseName.StartsWith('civ', [System.StringComparison]::OrdinalIgnoreCase)) {
    Write-Host "Detected civ* file: $translationFile"
    Write-Host 'Running translate.ps1 to generate translation/all.txt ...'
    & (Join-Path $scriptRoot 'translate.ps1')
    Write-Host 'translation/all.txt has been written.'
    Write-Host 'You can now copy or adjust a civ_xy.txt file in the translation folder.'
    Read-Host 'Press Enter to continue with interactive translation'
}
else {
    Write-Host 'Skipped. The provided file name does not start with civ.'
}

Write-Host 'Step 2/3: Run translate-interactive roundtrip.'
if (!(Test-Path -Path $translationPath -PathType Leaf)) {
    throw "Translation file not found: $translationPath"
}

dotnet run --project $projectPath -- $translationPath
if ($LASTEXITCODE -ne 0) {
    Write-Host "Step 2/3 failed with exit code $LASTEXITCODE."
    Write-Host 'Step 3/3 is skipped.'
    exit $LASTEXITCODE
}

Write-Host 'Step 3/3: Copy translations to target directory.'
& (Join-Path $scriptRoot 'copy-translations.ps1')

Write-Host 'Done.'
