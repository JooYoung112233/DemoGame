# cooking_recipes.csv -> Assets/Resources/Data/Recipes/
param([switch]$Force)
$ErrorActionPreference = "Stop"
$scriptGuid = "7a3bc92e1f4d5e6a8b9c0d1e2f3a4b5c"
$outDir = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Assets\Resources\Data\Recipes"))
$csv = Join-Path $PSScriptRoot "cooking_recipes.csv"

if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

function To-AssetName([string]$id) {
    ($id -split '_' | ForEach-Object { $_.Substring(0,1).ToUpper() + $_.Substring(1) }) -join ''
}

function Get-ExistingRecipeIds {
    $ids = @{}
    if (-not (Test-Path $outDir)) { return $ids.Keys }
    Get-ChildItem $outDir -Filter "*.asset" -File | ForEach-Object {
        $c = [IO.File]::ReadAllText($_.FullName)
        if ($c -match 'recipeId:\s*(\S+)') { $ids[$Matches[1]] = $true }
    }
    $ids.Keys
}

function Format-Ingredients([string]$raw) {
    $lines = @()
    foreach ($part in ($raw -split '\|')) {
        $kv = $part -split ':', 2
        if ($kv.Length -lt 2) { continue }
        $lines += "  - itemId: $($kv[0].Trim())"
        $lines += "    count: $($kv[1].Trim())"
    }
    $lines -join "`n"
}

$utf8 = New-Object System.Text.UTF8Encoding $false
$rows = Import-Csv -Path $csv -Encoding UTF8
$existing = Get-ExistingRecipeIds
$created = 0
$skipped = 0

foreach ($r in $rows) {
    if (-not $Force -and ($existing -contains $r.recipeId)) { $skipped++; continue }
    $name = To-AssetName $r.recipeId
    $ingBlock = Format-Ingredients $r.ingredients
    $assetPath = Join-Path $outDir "$name.asset"
    $metaPath = "$assetPath.meta"
    $guid = [guid]::NewGuid().ToString("N").Substring(0, 32)
    if (Test-Path $metaPath) {
        $metaText = [IO.File]::ReadAllText($metaPath)
        if ($metaText -match 'guid:\s*([a-f0-9]{32})') { $guid = $Matches[1] }
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
  m_EditorClassIdentifier: Game.Scripts::RecipeData
  recipeId: $($r.recipeId)
  displayName: "$($r.displayName)"
  description: "$($r.description)"
  station: 2
  ingredients:
$ingBlock
  resultItemId: $($r.resultId)
  resultCount: $($r.resultCount)
  unlockedByDefault: 0
  unlockRecipeItemId: $($r.unlockItem)
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
    if (-not (Test-Path $metaPath)) {
        [IO.File]::WriteAllText($metaPath, $meta, [Text.Encoding]::ASCII)
    }
    $created++
}

Write-Host "Recipes existing: $($existing.Count) | Created: $created | Skipped: $skipped"
