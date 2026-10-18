#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$CivOrigDirectory,

    [Parameter(Mandatory = $false)]
    [switch]$Trace
)

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptRoot 'civtext-hashgen/civtext-hashgen.csproj'
$outputPath = Join-Path $scriptRoot 'src/IO/Text/OriginalTextLanguageValidationDefaultDefinitions.cs'

if (-not (Test-Path -Path $CivOrigDirectory -PathType Container)) {
    throw "Directory not found: $CivOrigDirectory"
}

Write-Host 'Building civtext-hashgen...'
dotnet build $projectPath

Write-Host "Generating hash definitions into: $outputPath"
if ($Trace.IsPresent) {
    dotnet run --project $projectPath -- $CivOrigDirectory --trace --output $outputPath
}
else {
    dotnet run --project $projectPath -- $CivOrigDirectory --output $outputPath
}

Write-Host 'Done.'
