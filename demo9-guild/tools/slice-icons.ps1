# Slice skill/action icon grids and rename per action_id.
#
# Inputs (must exist):
#   demo9-guild/assets/actions/_raw/skills.png   (2 rows x 3 cols  = 6 cells, 스킬)
#   demo9-guild/assets/actions/_raw/attacks.png  (4 rows x 5 cols  = 20 cells, 공격)
#
# Outputs:
#   demo9-guild/assets/actions/<action_id>.png        (이름 매핑된 칸 — ActionIcons 로더가 자동 인식)
#   demo9-guild/assets/actions/_unused/<row>_<col>.png (매칭 안 된 칸)
#
# Usage (worktree 루트에서):
#   pwsh -File demo9-guild/tools/slice-icons.ps1

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$repo = Resolve-Path (Join-Path $PSScriptRoot '..' '..') | Select-Object -ExpandProperty Path
$rawDir     = Join-Path $repo 'demo9-guild\assets\actions\_raw'
$outDir     = Join-Path $repo 'demo9-guild\assets\actions'
$unusedDir  = Join-Path $repo 'demo9-guild\assets\actions\_unused'
New-Item -ItemType Directory -Force -Path $outDir,$unusedDir | Out-Null

# 매핑 — 셀 좌표(1-base, "row,col") -> action_id 또는 '_unused'
$skillsMap = @{
    '1,1' = 'warrior_skill'     # 방패+빛
    '1,2' = 'rogue_skill'       # 단검+조준+피
    '1,3' = 'mage_skill'        # 보라 폭발
    '2,1' = 'archer_skill'      # 화살+해골
    '2,2' = 'priest_skill'      # 황금 십자+광채
    '2,3' = 'alchemist_skill'   # 불병
}

$attacksMap = @{
    '1,1' = 'mage_atk2'         # 불꽃 화살 -> 화염구
    '1,2' = 'warrior_atk2'      # 원형 회전 슬래시
    '1,3' = 'rogue_atk1'        # 단검 찌르기
    '1,4' = '_unused'           # 핑크 불꽃 - 매칭 애매
    '1,5' = 'mage_atk1'         # 작은 보라 폭발 -> 마력 화살
    '2,1' = 'archer_atk2'       # 금화살 -> 다중 사격
    '2,2' = 'rogue_atk2'        # 단검+불꽃 모션 -> 투척 단검
    '2,3' = 'alchemist_atk2'    # 빨간 폭발 덩어리 -> 폭탄
    '2,4' = 'mage_atk3'         # 얼음 결정 -> 냉기파
    '2,5' = 'warrior_atk1'      # 칼날+조준 -> 정면 베기
    '3,1' = 'archer_atk1'       # 활 -> 활쏘기
    '3,2' = 'rogue_atk3'        # 보라 화살/잔상 -> 측면 이동
    '3,3' = '_unused'           # 금 화살 중복
    '3,4' = 'archer_atk3'       # 조준점 -> 저격
    '3,5' = 'alchemist_atk1'    # 불병 -> 산성 투척 (병 형태)
    '4,1' = 'priest_atk1'       # 빛 구체 -> 신성 빛
    '4,2' = 'priest_atk2'       # 십자+충격 -> 정화 일격
    '4,3' = '_unused'           # 금 십자 중복
    '4,4' = 'priest_atk3'       # 녹 십자 -> 치유
    '4,5' = 'alchemist_atk3'    # 불병 alt -> 강화 물약
}

function Slice-Grid {
    param(
        [string]$ImagePath,
        [int]$Rows,
        [int]$Cols,
        [hashtable]$Map,
        [string]$Tag
    )
    if (-not (Test-Path $ImagePath)) {
        Write-Warning "원본 없음: $ImagePath  (저장 후 다시 실행)"
        return
    }
    Write-Output "[$Tag] $ImagePath  ($Rows x $Cols)"
    $img = [System.Drawing.Image]::FromFile($ImagePath)
    try {
        $cellW = [int]([math]::Floor($img.Width  / $Cols))
        $cellH = [int]([math]::Floor($img.Height / $Rows))
        Write-Output "  cell size: ${cellW}x${cellH}px"
        for ($r = 1; $r -le $Rows; $r++) {
            for ($c = 1; $c -le $Cols; $c++) {
                $key = "$r,$c"
                $name = $Map[$key]
                if (-not $name) { $name = '_unused' }
                $x = ($c - 1) * $cellW
                $y = ($r - 1) * $cellH
                $rect = New-Object System.Drawing.Rectangle($x, $y, $cellW, $cellH)
                $bmp = New-Object System.Drawing.Bitmap($cellW, $cellH)
                $g = [System.Drawing.Graphics]::FromImage($bmp)
                $g.DrawImage($img, (New-Object System.Drawing.Rectangle(0, 0, $cellW, $cellH)), $rect, [System.Drawing.GraphicsUnit]::Pixel)
                $g.Dispose()

                if ($name -eq '_unused') {
                    $outPath = Join-Path $unusedDir "${Tag}_r${r}_c${c}.png"
                } else {
                    $outPath = Join-Path $outDir "$name.png"
                }
                $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
                $bmp.Dispose()
                Write-Output "  ($r,$c) -> $(Split-Path $outPath -Leaf)"
            }
        }
    } finally {
        $img.Dispose()
    }
}

Slice-Grid -ImagePath (Join-Path $rawDir 'skills.png')  -Rows 2 -Cols 3 -Map $skillsMap  -Tag 'skills'
Slice-Grid -ImagePath (Join-Path $rawDir 'attacks.png') -Rows 4 -Cols 5 -Map $attacksMap -Tag 'attacks'

Write-Output ""
Write-Output "완료. 결과:"
Write-Output "  매칭: $outDir"
Write-Output "  잉여: $unusedDir"
