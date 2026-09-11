# Patch sellPrice/buyPrice on all ItemData assets (CSV + assets not in CSV)
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "ItemPrices.ps1")

$itemsDir = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\Assets\Resources\Items"))
$csvPath = Join-Path $PSScriptRoot "items.csv"
$csvById = @{}
if (Test-Path $csvPath) {
    Import-Csv -Path $csvPath -Encoding UTF8 | ForEach-Object { $csvById[$_.id] = $_ }
}

function Read-AssetFields([string]$path) {
    $t = [IO.File]::ReadAllText($path)
    $id = if ($t -match 'itemId:\s*(\S+)') { $Matches[1] } else { $null }
    $cat = if ($t -match 'category:\s*(\d+)') { [int]$Matches[1] } else { 6 }
    $rar = if ($t -match 'rarity:\s*(\d+)') { [int]$Matches[1] } else { 0 }
    $dur = if ($t -match 'hasDurability:\s*1') { 1 } else { 0 }
    $fx = if ($t -match 'useEffect:\s*(\d+)') { [int]$Matches[1] } else { 0 }
    $val = if ($t -match 'effectValue:\s*([\d.]+)') { [double]$Matches[1] } else { 0 }
    if ($id -and $csvById.ContainsKey($id)) {
        $r = $csvById[$id]
        $cat = [int]$r.cat
        $rar = [int]$r.rar
        $dur = [int]$r.dur
        $fx = [int]$r.fx
        if ($r.val) { $val = [double]$r.val }
    }
    @{ id = $id; cat = $cat; rar = $rar; dur = $dur; fx = $fx; val = $val; text = $t }
}

function Set-PriceLines([string]$text, [int]$sell, [int]$buy) {
    $text = [regex]::Replace($text, '(?m)^  weight:\s*([\d.]+)\s+ sellPrice:\s*(\d+)\s*$', "  weight: `$1`n  sellPrice: `$2")
    $text = [regex]::Replace($text, '(?m)^  weight:\s*([\d.]+)  sellPrice:\s*(\d+)\s*$', "  weight: `$1`n  sellPrice: `$2")
    $block = "`n  sellPrice: $sell`n  buyPrice: $buy`n"
    if ($text -match '(?m)^  sellPrice:\s*\d+\s*$') {
        $text = [regex]::Replace($text, '(?m)^  sellPrice:\s*\d+\s*$', "  sellPrice: $sell")
        $text = [regex]::Replace($text, '(?m)^  buyPrice:\s*\d+\s*$', "  buyPrice: $buy")
        return $text
    }
    if ($text -match '(?m)^  weight:\s*[\d.]+\s*$') {
        return [regex]::Replace($text, '(?m)^(  weight:\s*[\d.]+\s*)$', "`$1$block", 1)
    }
    if ($text -match '(?m)^  maxStack:\s*\d+\s*$') {
        return [regex]::Replace($text, '(?m)^(  maxStack:\s*\d+\s*)$', "`$1$block", 1)
    }
    throw "Cannot insert price block"
}

$utf8 = New-Object System.Text.UTF8Encoding $false
$updated = 0
Get-ChildItem $itemsDir -Filter "*.asset" -Recurse -File | ForEach-Object {
    $f = Read-AssetFields $_.FullName
    if (-not $f.id) { return }
    $prices = Get-ItemPricesScaled -Id $f.id -Cat $f.cat -Rar $f.rar -Dur $f.dur -Fx $f.fx -Val $f.val
    $newText = Set-PriceLines $f.text $prices[0] $prices[1]
    if ($newText -ne $f.text) {
        [IO.File]::WriteAllText($_.FullName, $newText, $utf8)
        $updated++
    }
}

Write-Host "Updated $updated ItemData assets with sellPrice/buyPrice."
