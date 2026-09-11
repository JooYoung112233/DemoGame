# region_items.csv -> Assets/Resources/Items/Regional/{RegionFolder}/
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "ItemPrices.ps1")
$scriptGuid = "fe39bfb873cc9304c8d8106a9584fe35"
$baseDir = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Assets\Resources\Items\Regional"))
$csv = Join-Path $PSScriptRoot "region_items.csv"

$regionFolders = @{
    silence_living = "SilenceLiving"
    scrap_market = "ScrapMarket"
    industrial = "Industrial"
    entertainment = "Entertainment"
    railway_scrap = "RailwayScrap"
    sanctuary_memorial = "SanctuaryMemorial"
    eternal_night_core = "EternalNightCore"
}

function To-AssetName([string]$id) {
    ($id -split '_' | ForEach-Object { $_.Substring(0,1).ToUpper() + $_.Substring(1) }) -join ''
}

function Get-ExistingIds {
    $ids = @{}
    $root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Assets\Resources\Items"))
    Get-ChildItem $root -Filter "*.asset" -Recurse -File | ForEach-Object {
        $c = [IO.File]::ReadAllText($_.FullName)
        if ($c -match 'itemId:\s*(\S+)') { $ids[$Matches[1]] = $true }
    }
    $ids.Keys
}

$utf8 = New-Object System.Text.UTF8Encoding $false
$rows = Import-Csv -Path $csv -Encoding UTF8
$existing = Get-ExistingIds
$created = 0
$skipped = 0

foreach ($r in $rows) {
    if ($existing -contains $r.id) { $skipped++; continue }
    $folder = $regionFolders[$r.region]
    if (-not $folder) { throw "Unknown region: $($r.region)" }
    $outDir = Join-Path $baseDir $folder
    if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

    $name = To-AssetName $r.id
    $guid = [guid]::NewGuid().ToString("N").Substring(0, 32)
    $durFlag = if ([int]$r.dur -eq 1) { 1 } else { 0 }
    $valNum = 0; if ($r.val) { $valNum = [double]$r.val }
    $fxNum = 0; if ($r.fx) { $fxNum = [int]$r.fx }
    $prices = Get-ItemPrices -Id $r.id -Cat ([int]$r.cat) -Rar ([int]$r.rar) -Dur $durFlag -Fx $fxNum -Val $valNum
    $stack = [int]$r.stack
    $durBlock = ""
    if ($durFlag -eq 1) {
        $stack = 1
        $durBlock = "  hasDurability: 1`n  maxDurability: $($r.maxDur)`n  durabilityCostPerUse: $($r.costDur)`n"
    }

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
  primaryRegionId: $($r.region)
  maxStack: $stack
  weight: $($r.wt)
  sellPrice: $($prices[0])
  buyPrice: $($prices[1])
  isUsable: $($r.use)
  useEffect: $($r.fx)
  effectValue: $($r.val)
$durBlock  medicalData: {fileID: 0}
  worldDropPrefab: {fileID: 0}
"@
    $assetPath = Join-Path $outDir "$name.asset"
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

$lootSrc = Join-Path $PSScriptRoot "region_loot.csv"
$lootDst = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Assets\Resources\region_loot.txt"))
Copy-Item $lootSrc $lootDst -Force
Write-Host "Regional items created: $created | skipped: $skipped | region_loot.txt synced"
