# traits.csv -> Assets/Resources/Data/Traits/<traitId>.asset (+ .meta)
# TraitData SO 생성기. (GenerateMedicalRecipes.ps1 패턴)
param([switch]$Force)
$ErrorActionPreference = "Stop"

# TraitData.cs.meta 의 GUID (m_Script 참조)
$scriptGuid = "8c3730e547514f3d9800a5ed1cce3758"

$outDir = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Assets\Resources\Data\Traits"))
$csv = Join-Path $PSScriptRoot "traits.csv"

if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

function To-AssetName([string]$id) {
    ($id -split '_' | ForEach-Object { $_.Substring(0,1).ToUpper() + $_.Substring(1) }) -join ''
}

function Esc-Yaml([string]$s) {
    if ($null -eq $s) { return "" }
    # YAML 큰따옴표 스칼라 안의 " 와 \ 이스케이프
    $s.Replace('\', '\\').Replace('"', '\"')
}

$utf8 = New-Object System.Text.UTF8Encoding $false
$rows = Import-Csv -Path $csv -Encoding UTF8
$created = 0
$skipped = 0

foreach ($r in $rows) {
    $tid = $r.traitId.Trim()
    if (-not $tid) { continue }
    $name = To-AssetName $tid
    $assetPath = Join-Path $outDir "$name.asset"
    $metaPath = "$assetPath.meta"

    if (-not $Force -and (Test-Path $assetPath)) { $skipped++; continue }

    # .meta 가 이미 있으면 GUID 보존(참조 안정), 없으면 새로 생성
    $guid = [guid]::NewGuid().ToString("N").Substring(0, 32)
    if (Test-Path $metaPath) {
        $metaText = [IO.File]::ReadAllText($metaPath)
        if ($metaText -match 'guid:\s*([a-f0-9]{32})') { $guid = $Matches[1] }
    }

    $prereq = ""
    if ($r.prereq -and $r.prereq.Trim()) { $prereq = $r.prereq.Trim() }
    $tradeoff = if ([int]$r.tradeoff -eq 1) { 1 } else { 0 }

    $disp = Esc-Yaml $r.displayName
    $effect = Esc-Yaml $r.effectSummary

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
  m_EditorClassIdentifier: Game.Scripts::TraitData
  traitId: $tid
  displayName: "$disp"
  description: ""
  category: $($r.category)
  tier: $($r.tier)
  ppCost: $($r.ppCost)
  prereqTraitId: $prereq
  tradeoff: $tradeoff
  effectSummary: "$effect"
  effects: []
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

Write-Host "Traits | Created: $created | Skipped: $skipped | OutDir: $outDir"
