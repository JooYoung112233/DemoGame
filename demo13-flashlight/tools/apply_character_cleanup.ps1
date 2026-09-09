$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath('D:\Demo\demo13-flashlight')
$planPath = Join-Path $projectRoot 'Library\CodexBlender\character-cleanup-plan.json'
$plan = Get-Content -LiteralPath $planPath -Raw | ConvertFrom-Json
$characterRoot = [IO.Path]::GetFullPath($plan.character_root)
$archiveRoot = [IO.Path]::GetFullPath($plan.archive_root)
$currentRoot = [IO.Path]::GetFullPath($plan.current_root)
function Assert-Within([string] $path, [string] $root) {
    if (-not $path.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Path outside scope: $path" }
}
Assert-Within $characterRoot $projectRoot
Assert-Within $archiveRoot $projectRoot
Assert-Within $currentRoot $characterRoot
# Preflight every absolute path and content hash before moving any user files.
foreach ($op in $plan.operations) {
    $src = (Resolve-Path -LiteralPath $op.source).ProviderPath
    $dst = [IO.Path]::GetFullPath($op.destination)
    Assert-Within $src $projectRoot
    if ($op.kind -eq 'current') { Assert-Within $dst $currentRoot } else { Assert-Within $dst $archiveRoot }
    if (-not (Test-Path -LiteralPath $src -PathType Leaf)) { throw "Not a file: $src" }
    if (Test-Path -LiteralPath $dst) { throw "Destination exists: $dst" }
    if ((Get-FileHash -LiteralPath $src -Algorithm SHA256).Hash -ne $op.sha256) { throw "File changed: $src" }
}
foreach ($op in $plan.operations) {
    $parent = [IO.Path]::GetDirectoryName($op.destination)
    if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
}
# Metadata travels with the assets, preserving existing Unity identifiers.
foreach ($op in ($plan.operations | Sort-Object @{Expression={-not $_.source.EndsWith('.meta')}}, source)) {
    Move-Item -LiteralPath $op.source -Destination $op.destination
}
foreach ($op in $plan.operations) {
    if ((Get-FileHash -LiteralPath $op.destination -Algorithm SHA256).Hash -ne $op.sha256) { throw "Moved file hash mismatch: $($op.destination)" }
}
foreach ($path in $plan.kept_files) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Kept file missing: $path" }
}
# Remove only empty directories inside the explicitly checked character workspace.
foreach ($dir in (Get-ChildItem -LiteralPath $characterRoot -Directory -Recurse | Sort-Object {$_.FullName.Length} -Descending)) {
    $resolved = (Resolve-Path -LiteralPath $dir.FullName).ProviderPath
    Assert-Within $resolved $characterRoot
    if (@(Get-ChildItem -LiteralPath $resolved -Force).Count -eq 0) { Remove-Item -LiteralPath $resolved }
}
Copy-Item -LiteralPath $planPath -Destination (Join-Path $archiveRoot 'cleanup-manifest.json')
@{status='complete'; moved=$plan.operations.Count; kept=$plan.preserved_assets; destination=$currentRoot; archive=$archiveRoot} | ConvertTo-Json -Depth 4
