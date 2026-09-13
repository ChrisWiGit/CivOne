param(
    [string]$TargetPath = (Join-Path $PSScriptRoot "src"),
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

if (-not (Test-Path $TargetPath -PathType Container)) {
    Write-Error "Target folder not found: $TargetPath"
}

$resolvedTarget = (Resolve-Path $TargetPath).Path
$resolvedRepoRoot = (Resolve-Path $PSScriptRoot).Path
$resolvedOutputRoot = [System.IO.Path]::GetFullPath($OutputRoot)

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
$compatOutput = Join-Path $resolvedTarget "graphify-out"

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
        New-Item -ItemType Directory -Path $resolvedOutputRoot -Force | Out-Null
        if (Test-Path $destinationOutput -PathType Container) {
            Remove-Item -Path $destinationOutput -Recurse -Force
        }
        Move-Item -Path $generatedOutput -Destination $destinationOutput
    }
}

$env:GRAPHIFY_OUTPUT_DIR = $destinationOutput
$env:GRAPHIFY_COMPAT_DIR = $compatOutput
$compatPythonCode = @'
from pathlib import Path
import json
import shutil
import os

from graphify.export import to_html

output_dir = Path(os.environ["GRAPHIFY_OUTPUT_DIR"])
compat_dir = Path(os.environ["GRAPHIFY_COMPAT_DIR"])
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

compat_dir.mkdir(parents=True, exist_ok=True)
if html_path.exists():
    shutil.copy2(html_path, compat_dir / "graph.html")
    print(f"Plugin compatibility report: {compat_dir / 'graph.html'}")
'@

$compatPythonCode | & $PythonExe -

Write-Host "Graphify output: $destinationOutput"
