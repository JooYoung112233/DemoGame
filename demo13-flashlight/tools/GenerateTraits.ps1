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

# effects 컬럼 -> Unity List<TraitEffect> YAML 시퀀스로 변환.
# 입력 형식: "key:op:value;key2:op2:value2"  (세미콜론으로 항목 구분, 콜론으로 필드 구분)
#   op 어휘: mul(비율, value 0.15 = +15%) / add(가산) / flag(능력 on, value=1)
# 부정 특성은 value에 부호로 페널티 표기(예: -0.30).
# 출력: TraitData.effects 의 YAML(빈값이면 "[]").
function Build-EffectsYaml([string]$spec) {
    if ($null -eq $spec) { return "[]" }
    $spec = $spec.Trim()
    if (-not $spec) { return "[]" }
    $sb = New-Object System.Text.StringBuilder
    $items = $spec -split ';'
    $count = 0
    foreach ($it in $items) {
        $t = $it.Trim()
        if (-not $t) { continue }
        $parts = $t -split ':'
        if ($parts.Count -lt 3) {
            Write-Warning "effects 항목 형식 오류(무시): '$t' (key:op:value 필요)"
            continue
        }
        $key = $parts[0].Trim()
        $op  = $parts[1].Trim()
        $valRaw = $parts[2].Trim()
        # 소수점 보존 + 정상 숫자 포맷 (invariant culture)
        $valNum = 0.0
        [void][double]::TryParse($valRaw, [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$valNum)
        $valStr = $valNum.ToString([Globalization.CultureInfo]::InvariantCulture)
        [void]$sb.Append("`n  - effectKey: $key")
        [void]$sb.Append("`n    op: $op")
        [void]$sb.Append("`n    value: $valStr")
        $count++
        # NOTE: 2-space indent. Unity MonoBehaviour 필드 하위 시퀀스는
        #   "  effects:" 다음 줄에 "  - effectKey:" (필드와 동일 들여쓰기) 형식.
    }
    if ($count -eq 0) { return "[]" }
    return $sb.ToString()
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
    $effSpec = ""
    $effProp = $r.PSObject.Properties['effects']
    if ($effProp) { $effSpec = [string]$effProp.Value }
    $effectsYaml = Build-EffectsYaml $effSpec
    if ($effectsYaml -eq "[]") { $effectsLine = "  effects: []" }
    else { $effectsLine = "  effects:" + $effectsYaml }

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
$effectsLine
"@
    [IO.File]::WriteAllText($assetPath, $yaml + "`n", $utf8)   # 끝 개행 보장 (Unity YAML import)

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
