$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptRoot 'civtranslate/civtranslate.csproj'
$inputPath = Join-Path $scriptRoot 'src'
$outputDir = Join-Path $scriptRoot 'translation'
$outputPath = Join-Path $outputDir 'all.txt'

if (!(Test-Path -Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

dotnet run --project $projectPath -- $inputPath --output $outputPath
