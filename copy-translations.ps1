# Copies generated translation .txt files from the repository translation folder
# into the active CivOne translations directory.
# Excludes all.txt and overwrites existing target files.
# Normalizes target file names to lowercase.

$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDir = Join-Path $scriptRoot 'translation'
$targetDir = if ($args.Length -gt 0 -and ![string]::IsNullOrWhiteSpace($args[0])) {
	$args[0]
}
else {
	Join-Path $env:LOCALAPPDATA 'CivOne\translations'
}

if (!(Test-Path -Path $sourceDir)) {
	throw "Source directory not found: $sourceDir"
}

New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

$files = Get-ChildItem -Path $sourceDir -Filter '*.txt' -File | Where-Object { $_.Name -ine 'all.txt' }

foreach ($file in $files) {
	$targetName = $file.Name.ToLowerInvariant()
	Copy-Item -Path $file.FullName -Destination (Join-Path $targetDir $targetName) -Force
	Write-Host "Copied $($file.Name) -> $targetName to $targetDir"
}

Write-Host "Done: $($files.Count) file(s) copied to $targetDir"