# UTF-8 CSV -> ItemData assets (category subfolders under Resources/Items/)
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "ItemPrices.ps1")
$scriptGuid = "fe39bfb873cc9304c8d8106a9584fe35"
$dir = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Assets\Resources\Items"))
$csv = Join-Path $PSScriptRoot "items.csv"
$med = @{
    bandage = "3df682d6038789446a7b822d46d2ab48"
    splint  = "b20cc94e77ff26546bd1b0fec9a92408"
    painkiller = "bd6c2245073e02341ad2983b36c373b1"
    kit     = "41259a63b5b1c624c9e7ccde98743af2"
}

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

function Get-OutputDir([string]$cat, [string]$itemId) {
    $folder = $catFolders[$cat]
    if (-not $folder) { throw "Unknown category: $cat" }
    if ($cat -eq "6") {
        $folder = Join-Path $folder (Get-MiscSubfolder $itemId)
    }
    $path = Join-Path $dir $folder
    if (-not (Test-Path $path)) { New-Item -ItemType Directory -Path $path -Force | Out-Null }
    $path
}

function Get-ExistingIds {
    $ids = @{}
    Get-ChildItem $dir -Filter "*.asset" -Recurse -File | ForEach-Object {
        $c = [IO.File]::ReadAllText($_.FullName)
        if ($c -match 'itemId:\s*(\S+)') { $ids[$Matches[1]] = $true }
    }
    $ids.Keys
}

function To-AssetName([string]$id) {
    ($id -split '_' | ForEach-Object { $_.Substring(0,1).ToUpper() + $_.Substring(1) }) -join ''
}

$utf8 = New-Object System.Text.UTF8Encoding $false
$rows = Import-Csv -Path $csv -Encoding UTF8
$existing = Get-ExistingIds
$created = 0
$skipped = 0

foreach ($r in $rows) {
    if ($existing -contains $r.id) { $skipped++; continue }
    $outDir = Get-OutputDir $r.cat $r.id
    $name = To-AssetName $r.id
    $guid = [guid]::NewGuid().ToString("N").Substring(0, 32)
    $medLine = "  medicalData: {fileID: 0}"
    if ($r.med -and $med.ContainsKey($r.med)) {
        $medLine = "  medicalData: {fileID: 11400000, guid: $($med[$r.med]), type: 2}"
    }
    $durBlock = ""
    $stack = [int]$r.stack
    if ([int]$r.dur -eq 1) {
        $stack = 1
        $durBlock = "  hasDurability: 1`n  maxDurability: $($r.maxDur)`n  durabilityCostPerUse: $($r.costDur)`n"
    }
    $durFlag = if ([int]$r.dur -eq 1) { 1 } else { 0 }
    $valNum = 0; if ($r.val) { $valNum = [double]$r.val }
    $fxNum = 0; if ($r.fx) { $fxNum = [int]$r.fx }
    $prices = Get-ItemPrices -Id $r.id -Cat ([int]$r.cat) -Rar ([int]$r.rar) -Dur $durFlag -Fx $fxNum -Val $valNum
    $assetPath = Join-Path $outDir "$name.asset"
    $yaml = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $scriptGuid, type: 3}
  m_Name: $name
  m_EditorClassIdentifier: Game.Scripts::ItemData
  itemId: $($r.id)
  displayName: "$($r.disp)"
  description: "$($r.desc)"
  icon: {fileID: 0}
  gridWidth: $($r.gw)
  gridHeight: $($r.gh)
  category: $($r.cat)
  rarity: $($r.rar)
  maxStack: $stack
  weight: $($r.wt)
  sellPrice: $($prices[0])
  buyPrice: $($prices[1])
  isUsable: $($r.use)
  useEffect: $($r.fx)
  effectValue: $($r.val)
$durBlock$medLine
  worldDropPrefab: {fileID: 0}
"@
    [IO.File]::WriteAllText($assetPath, $yaml, $utf8)
    $meta = @"
fileFormatVersion: 2
guid: $guid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    [IO.File]::WriteAllText("$assetPath.meta", $meta, [Text.Encoding]::ASCII)
    $created++
}

Write-Host "Existing: $($existing.Count) | Created: $created | Skipped: $skipped | Total: $($existing.Count + $created)"
