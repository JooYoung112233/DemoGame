# Move ItemData assets from Items/ root into category subfolders
$ErrorActionPreference = "Stop"
$root = Join-Path $PSScriptRoot "..\Assets\Resources\Items"
$root = [IO.Path]::GetFullPath($root)

$catFolders = @{
    "0" = "Weapon"
    "1" = "Medical"
    "2" = "Consumable"
    "3" = "Material"
    "4" = "Valuable"
    "5" = "Key"
    "6" = "Misc"
}

function Get-MiscSubfolder([string]$itemId) {
    if ($itemId -like "junk_*") { return "Junk" }
    if ($itemId -match "^(coat_|gloves_|boots_|vest_|helmet_|armor_)") { return "Armor" }
    if ($itemId -match "^(anomaly_|mark_|clock_|black_|whisper_|note_)") { return "Story" }
    return "Other"
}

function Get-TargetDir([string]$assetPath) {
    $text = [IO.File]::ReadAllText($assetPath)
    if ($text -notmatch "category:\s*(\d)") { return $null }
    $cat = $Matches[1]
    $folder = $catFolders[$cat]
    if (-not $folder) { return $null }
    if ($cat -eq "6") {
        if ($text -match "itemId:\s*(\S+)") {
            $sub = Get-MiscSubfolder $Matches[1]
            $folder = Join-Path $folder $sub
        }
    }
    Join-Path $root $folder
}

# Ensure folders exist
foreach ($f in $catFolders.Values) {
    $p = Join-Path $root $f
    if (-not (Test-Path $p)) { New-Item -ItemType Directory -Path $p -Force | Out-Null }
}
foreach ($sub in @("Junk","Armor","Story","Other")) {
    $p = Join-Path $root "Misc\$sub"
    if (-not (Test-Path $p)) { New-Item -ItemType Directory -Path $p -Force | Out-Null }
}

$moved = 0
Get-ChildItem $root -Filter "*.asset" -File | ForEach-Object {
    $targetDir = Get-TargetDir $_.FullName
    if (-not $targetDir) {
        Write-Warning "Skip (no category): $($_.Name)"
        return
    }
    if (-not (Test-Path $targetDir)) { New-Item -ItemType Directory -Path $targetDir -Force | Out-Null }
    $dest = Join-Path $targetDir $_.Name
    if ($_.FullName -eq $dest) { return }
    Move-Item $_.FullName $dest -Force
    $meta = "$($_.FullName).meta"
    if (Test-Path $meta) { Move-Item $meta "$dest.meta" -Force }
    $moved++
}

Write-Host "Moved $moved assets into category folders under Items/"
