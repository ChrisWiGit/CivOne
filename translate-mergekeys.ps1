param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$SourceFileName,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$TargetFileName
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptRoot 'civtranslate-mergekeys/civtranslate-mergekeys.csproj'
$translationDir = Join-Path $scriptRoot 'translation'

function Resolve-TranslationPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FileName
    )

    if ([string]::IsNullOrWhiteSpace($FileName)) {
        throw 'A translation file name is required.'
    }

    $hasDirectory = $FileName.Contains('/') -or $FileName.Contains('\')
    if ($hasDirectory) {
        return $FileName
    }

    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($FileName)
    if ([string]::IsNullOrWhiteSpace($baseName)) {
        throw "Invalid translation file name: $FileName"
    }

    return Join-Path $translationDir "$baseName.txt"
}

$sourcePath = Resolve-TranslationPath -FileName $SourceFileName
$targetPath = Resolve-TranslationPath -FileName $TargetFileName

dotnet run --project $projectPath -- $sourcePath $targetPath