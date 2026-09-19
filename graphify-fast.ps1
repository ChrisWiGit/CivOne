# Usage: See README.md section "Graphify code graph workflow" for scope details and examples.
# Quick start: .\graphify-fast.ps1 -Scope src | -Scope api | -Scope both | -Scope combined
param(
    [string]$TargetPath = "",
    [ValidateSet("src", "api", "both", "combined")]
    [string]$Scope = "src",
    [string]$PythonExe = "",
    [string]$OutputRoot = (Join-Path $PSScriptRoot "graphify-out"),
    [string]$Granularity = "low",
    [string[]]$EntryPoints = @("app", "main", "index", "root", "server", "router")
)

$ErrorActionPreference = "Stop"

function Test-GraphifyPython {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Candidate
    )

    try {
        & $Candidate -c "import graphify" 1>$null 2>$null
        return $LASTEXITCODE -eq 0
    }
    catch {
        return $false
    }
}

if ([string]::IsNullOrWhiteSpace($PythonExe)) {
    $candidates = @()

    if (-not [string]::IsNullOrWhiteSpace($env:GRAPHIFY_PYTHON)) {
        $candidates += $env:GRAPHIFY_PYTHON
    }

    $candidates += (Join-Path $PSScriptRoot ".venv\Scripts\python.exe")
    $candidates += (Join-Path $env:USERPROFILE "graphify-ext\Scripts\python.exe")

    if (Get-Command python -ErrorAction SilentlyContinue) {
        $candidates += (Get-Command python -ErrorAction SilentlyContinue).Source
    }

    if (Get-Command python3 -ErrorAction SilentlyContinue) {
        $candidates += (Get-Command python3 -ErrorAction SilentlyContinue).Source
    }

    $PythonExe = $null
    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if ((Test-Path $candidate -PathType Leaf) -or ($candidate -eq "python") -or ($candidate -eq "python3")) {
            if (Test-GraphifyPython -Candidate $candidate) {
                $PythonExe = $candidate
                break
            }
        }
    }

    if ([string]::IsNullOrWhiteSpace($PythonExe)) {
        Write-Error "No Python with installed 'graphify' found. Set -PythonExe or GRAPHIFY_PYTHON to an interpreter where 'import graphify' works."
    }
}

if (-not (Test-GraphifyPython -Candidate $PythonExe)) {
    Write-Error "Selected Python has no graphify module: $PythonExe"
}

if (-not (Test-Path $PythonExe -PathType Leaf)) {
    if (($PythonExe -ne "python") -and ($PythonExe -ne "python3")) {
        Write-Error "Graphify Python not found: $PythonExe"
    }
}

$resolvedRepoRoot = (Resolve-Path $PSScriptRoot).Path
$resolvedOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)

function Invoke-GraphifyTarget {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CurrentTargetPath
    )

    if (-not (Test-Path $CurrentTargetPath -PathType Container)) {
        Write-Error "Target folder not found: $CurrentTargetPath"
    }

    $resolvedTarget = (Resolve-Path $CurrentTargetPath).Path
    $repoRootWithSlash = if ($resolvedRepoRoot.EndsWith([System.IO.Path]::DirectorySeparatorChar)) { $resolvedRepoRoot } else { "$resolvedRepoRoot\" }
    $targetWithSlash = if ($resolvedTarget.EndsWith([System.IO.Path]::DirectorySeparatorChar)) { $resolvedTarget } else { "$resolvedTarget\" }
    $repoUri = [Uri]$repoRootWithSlash
    $targetUri = [Uri]$targetWithSlash
    $relativeTarget = [Uri]::UnescapeDataString($repoUri.MakeRelativeUri($targetUri).ToString()).TrimEnd('/')

    if (($relativeTarget.Length -eq 0) -or $relativeTarget.StartsWith("..")) {
        $relativeTarget = [System.IO.Path]::GetFileName($resolvedTarget)
    }

    if ([string]::IsNullOrWhiteSpace($relativeTarget)) {
        $relativeTarget = "root"
    }

    $relativeTarget = $relativeTarget.Replace('/', '\\')
    $destinationOutput = Join-Path $resolvedOutputRoot $relativeTarget
    $generatedOutput = Join-Path $resolvedTarget "graphify-out"

    $env:GRAPHIFY_TARGET = $resolvedTarget
    $env:GRAPHIFY_GRANULARITY = $Granularity
    $env:GRAPHIFY_ENTRY_POINTS = ($EntryPoints -join ",")

    $pythonCode = @'
from pathlib import Path
import os
import graphify.ai as ai
from graphify.watch import _rebuild_code

ai.Summarizer.is_available = lambda self: False

target = Path(os.environ["GRAPHIFY_TARGET"])
gran = os.environ.get("GRAPHIFY_GRANULARITY", "low")
entries = [
    x for x in os.environ.get(
        "GRAPHIFY_ENTRY_POINTS",
        "app,main,index,root,server,router"
    ).split(",") if x
]

ok = _rebuild_code(target, granularity=gran, entry_points=entries)
raise SystemExit(0 if ok else 1)
'@

    $pythonCode | & $PythonExe -

    if (Test-Path $generatedOutput -PathType Container) {
        if (-not [System.IO.Path]::GetFullPath($generatedOutput).Equals([System.IO.Path]::GetFullPath($destinationOutput), [System.StringComparison]::OrdinalIgnoreCase)) {
            # Stage outside $generatedOutput first: when TargetPath is the repo root,
            # $destinationOutput is nested inside $generatedOutput and a direct move would fail.
            $stageOutput = Join-Path $resolvedTarget (".graphify-out.stage." + [System.Guid]::NewGuid().ToString("N"))
            Move-Item -Path $generatedOutput -Destination $stageOutput
            if (Test-Path $destinationOutput -PathType Container) {
                Remove-Item -Path $destinationOutput -Recurse -Force
            }
            New-Item -ItemType Directory -Path (Split-Path -Parent $destinationOutput) -Force | Out-Null
            Move-Item -Path $stageOutput -Destination $destinationOutput
        }
    }

    $env:GRAPHIFY_OUTPUT_DIR = $destinationOutput
    $compatPythonCode = @'
from pathlib import Path
import json
import os

from graphify.export import to_html

output_dir = Path(os.environ["GRAPHIFY_OUTPUT_DIR"])
html_path = output_dir / "graph.html"
json_path = output_dir / "graph.json"

def build_sample_html() -> bool:
    if not json_path.exists():
        return False

    data = json.loads(json_path.read_text(encoding="utf-8"))
    nodes = data.get("nodes", [])
    if not nodes:
        return False

    edges = data.get("links", data.get("edges", []))
    max_nodes = 4500
    selected_ids = {n.get("id") for n in nodes[:max_nodes] if n.get("id")}

    import networkx as nx
    G = nx.Graph()
    for node in nodes:
        node_id = node.get("id")
        if node_id in selected_ids:
            attrs = dict(node)
            attrs.pop("id", None)
            G.add_node(node_id, **attrs)

    for edge in edges:
        source = edge.get("source", edge.get("from"))
        target = edge.get("target", edge.get("to"))
        if source in selected_ids and target in selected_ids:
            attrs = dict(edge)
            attrs.pop("source", None)
            attrs.pop("target", None)
            attrs.pop("from", None)
            attrs.pop("to", None)
            G.add_edge(source, target, **attrs)

    communities: dict[int, list[str]] = {}
    for node_id, attrs in G.nodes(data=True):
        community = int(attrs.get("community", 0))
        communities.setdefault(community, []).append(node_id)

    metadata_path = output_dir / "metadata.json"
    labels = {}
    summaries = {}
    if metadata_path.exists():
        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
        labels = metadata.get("communityLabels", {})
        summaries = metadata.get("communitySummaries", {})

    to_html(
        G,
        communities,
        str(html_path),
        community_labels=labels or None,
        community_summaries=summaries or None,
        granularity="low",
    )
    return True

if not html_path.exists():
    created = build_sample_html()
    if created:
        print("Generated compatibility graph.html from sampled graph.json")
'@

    $compatPythonCode | & $PythonExe -

    $env:GRAPHIFY_SANITIZE_DIR = $destinationOutput
    $env:GRAPHIFY_SANITIZE_REPO_ROOT = $resolvedRepoRoot
    $sanitizePythonCode = @'
from pathlib import Path
import os

output_dir = Path(os.environ["GRAPHIFY_SANITIZE_DIR"])
repo_root = Path(os.environ["GRAPHIFY_SANITIZE_REPO_ROOT"]).resolve()

repo_win = str(repo_root)
repo_posix = repo_win.replace("\\", "/")
repo_win_escaped = repo_win.replace("\\", "\\\\")

replacements = [
    (f"file:///{repo_posix}/", ""),
    (f"file:///{repo_posix}", "."),
    (repo_posix + "/", ""),
    (repo_posix, "."),
    (repo_win + "\\", ""),
    (repo_win, "."),
    (repo_win_escaped + "\\\\", ""),
    (repo_win_escaped, "."),
]

for path in output_dir.rglob("*"):
    if not path.is_file():
        continue

    if path.suffix.lower() not in {".md", ".txt", ".json", ".html"}:
        continue

    text = path.read_text(encoding="utf-8", errors="ignore")
    new_text = text
    for old, new in replacements:
        new_text = new_text.replace(old, new)

    if new_text != text:
        path.write_text(new_text, encoding="utf-8")
'@

    $sanitizePythonCode | & $PythonExe -

    if (
        $relativeTarget.Equals("src", [System.StringComparison]::OrdinalIgnoreCase) -or
        $relativeTarget.Equals("root", [System.StringComparison]::OrdinalIgnoreCase)
    ) {
        New-Item -ItemType Directory -Path $resolvedOutputRoot -Force | Out-Null
        Get-ChildItem -Path $destinationOutput -File -ErrorAction SilentlyContinue | ForEach-Object {
            Copy-Item -Path $_.FullName -Destination (Join-Path $resolvedOutputRoot $_.Name) -Force
        }
    }

    Write-Host "Graphify output: $destinationOutput"
}

$targetPaths = @()
if (-not [string]::IsNullOrWhiteSpace($TargetPath)) {
    $targetPaths = @($TargetPath)
}
else {
    switch ($Scope) {
        "src" { $targetPaths = @((Join-Path $PSScriptRoot "src")) }
        "api" { $targetPaths = @((Join-Path $PSScriptRoot "api")) }
        "both" { $targetPaths = @((Join-Path $PSScriptRoot "src"), (Join-Path $PSScriptRoot "api")) }
        "combined" { $targetPaths = @($PSScriptRoot) }
    }
}

foreach ($currentTarget in $targetPaths) {
    Invoke-GraphifyTarget -CurrentTargetPath $currentTarget
}
