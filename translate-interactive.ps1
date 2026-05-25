param(
    [Parameter(Mandatory = $true)]
    [string]$Language
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptRoot 'civtranslate-interactive/civtranslate-interactive.csproj'

if ([string]::IsNullOrWhiteSpace($Language)) {
    throw 'A language postfix is required (example: german). Use --language <postfix>.'
}

if ($Language.Contains('/') -or $Language.Contains('\')) {
    throw 'Please pass only a language postfix, not a path.'
}

$postfix = $Language.Trim()
if ([string]::IsNullOrWhiteSpace($postfix)) {
    throw 'Invalid language postfix.'
}

$translationFile = "civ_$postfix.txt"
$translationPath = Join-Path (Join-Path $scriptRoot 'translation') $translationFile

Write-Host 'Step 1/3: Check whether translation/all.txt must be generated.'
Write-Host "Detected language file: $translationFile"
Write-Host 'Running translate.ps1 to generate translation/all.txt ...'
& (Join-Path $scriptRoot 'translate.ps1')
Write-Host 'translation/all.txt has been written.'
Write-Host "Ensure $translationFile exists (for example by copying all.txt once)."
Read-Host 'Press Enter to continue with interactive translation'

Write-Host 'Step 2/3: Run translate-interactive roundtrip.'
if (!(Test-Path -Path $translationPath -PathType Leaf)) {
    throw "Language file not found: $translationPath"
}

dotnet run --project $projectPath -- --language $postfix
if ($LASTEXITCODE -ne 0) {
    Write-Host "Step 2/3 failed with exit code $LASTEXITCODE."
    Write-Host 'Step 3/3 is skipped.'
    exit $LASTEXITCODE
}

Write-Host 'Step 3/3: Copy translations to target directory.'
& (Join-Path $scriptRoot 'copy-translations.ps1')

Write-Host 'Done.'
