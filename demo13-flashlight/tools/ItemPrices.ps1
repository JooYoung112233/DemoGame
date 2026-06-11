# 루디 기준 sellPrice / buyPrice (0 = 해당 거래 불가)
# docs/items.md 판매 등급: D 1~5, C 8~20, B 25~60, A 80~150, S 200+

function Get-ItemPrices {
    param(
        [string]$Id,
        [int]$Cat,
        [int]$Rar,
        [int]$Dur = 0,
        [int]$Fx = 0,
        [double]$Val = 0
    )

    # 명시 오버라이드 (기존 수동 설정·기획 앵커)
    $fixed = @{
        first_aid_kit    = @(35, 80)
        ifak_injury      = @(40, 90)
        battery          = @(5, 12)
        canned_food      = @(8, 15)
        coin_scrap       = @(1, 0)
        coin_old         = @(3, 0)
        coin_military    = @(15, 0)
        token_subway     = @(9, 0)
        coupon_faded     = @(2, 0)
        val_gold_bar     = @(280, 0)
        val_diamond_ring = @(220, 0)
        val_antique_vase = @(160, 0)
        val_watch_brother = @(400, 0)
        val_platinum_chain = @(130, 0)
        val_emerald_brooch = @(110, 0)
        val_opal_ring    = @(140, 0)
        val_id_government = @(95, 0)
        val_ring_wedding = @(180, 0)
        ruby_core        = @(180, 0)
        ruby_corrupted   = @(120, 0)
        ruby_dust        = @(5, 0)
        photo_brother    = @(0, 0)
        map_fragment     = @(0, 0)
        key_warehouse    = @(0, 0)
        key_rooftop      = @(0, 0)
        stim_injector    = @(35, 75)
        morphine_ampule  = @(28, 60)
        gauze_roll       = @(3, 8)
        adrenaline_shot  = @(22, 48)
        reg_silence_market_chit = @(4, 0)
        reg_silence_note_bundle = @(3, 0)
        reg_scrap_neon_tube = @(12, 0)
        reg_scrap_coupon_stack = @(3, 0)
        reg_industrial_pressure_gauge = @(18, 0)
        reg_industrial_coolant_can = @(10, 0)
        reg_entertain_show_ticket = @(14, 0)
        reg_entertain_luna_sticker = @(5, 0)
        reg_railway_rail_clip = @(8, 0)
        reg_railway_grease_tin = @(11, 0)
        reg_sanctuary_prayer_bead = @(22, 0)
        reg_sanctuary_wilted_flower = @(4, 0)
        reg_core_observation_log = @(0, 0)
        reg_core_void_residue = @(45, 0)
        # 조리 레시피 (중복 획득 시 NPC 판매)
        recipe_stew    = @(8, 0)
        recipe_bread   = @(6, 0)
        recipe_tea     = @(12, 0)
        recipe_soup    = @(12, 0)
        recipe_special = @(25, 0)
        ingredient_meat      = @(5, 12)
        ingredient_vegetable = @(3, 8)
        ingredient_salt      = @(2, 6)
        ingredient_herb      = @(8, 18)
        ingredient_flour     = @(6, 14)
        ingredient_sugar     = @(4, 10)
        cooked_stew    = @(12, 28)
        cooked_bread   = @(6, 14)
        herbal_tea     = @(10, 22)
        energy_soup    = @(14, 30)
        special_meal   = @(35, 0)
    }
    if ($fixed.ContainsKey($Id)) {
        return $fixed[$Id]
    }

    # 카테고리별
    switch ($Cat) {
        0 { # Weapon — 판매만 (고철)
            $sell = 12 + ($Rar * 18)
            if ($Id -eq 'anomaly_blade') { $sell = 55 }
            return @($sell, 0)
        }
        1 { # Medical
            if ($Dur -eq 1) {
                $sell = 25 + ($Rar * 12)
                if ($Fx -eq 1 -and $Val -gt 0) { $sell = [int]($Val * 0.85) }
                return @($sell, [int]($sell * 2.3))
            }
            $sell = switch ($Rar) { 0 { 5 } 1 { 10 } 2 { 28 } default { 8 } }
            return @($sell, [int]($sell * 2.4))
        }
        2 { # Consumable
            if ($Fx -eq 1 -and $Val -gt 0) {
                $sell = [int]([math]::Max(6, $Val * 0.35))
                return @($sell, [int]($sell * 2))
            }
            $sell = switch ($Rar) { 0 { 6 } 1 { 10 } 2 { 32 } default { 8 } }
            return @($sell, [int]($sell * 2.2))
        }
        3 { # Material
            $sell = @(4, 10, 22, 48)[[math]::Min($Rar, 3)]
            if ($Id -like 'ruby*') { return @(5, 0) }
            return @($sell, [int]($sell * 2.5))
        }
        4 { # Valuable
            $sell = switch ($Rar) {
                0 { 3 }
                1 { 14 }
                2 { 45 }
                3 { 110 }
                4 { 250 }
                default { 5 }
            }
            if ($Id -like 'val_*') {
                if ($Id -match 'gold|diamond|platinum|emerald|opal|jade') { $sell += 15 }
                if ($Id -match 'brother|wedding|government') { $sell = [math]::Max($sell, 90) }
            }
            return @($sell, 0)
        }
        5 { return @(0, 0) } # Key
        6 { # Misc
            if ($Id -like 'junk_*') {
                $sell = switch ($Rar) { 0 { 2 } 1 { 12 } 2 { 35 } 3 { 50 } default { 2 } }
                if ($Id -match 'printer|mannequin|typewriter') { $sell += 8 }
                return @($sell, 0)
            }
            if ($Id -match '^(coat_|gloves_|boots_|vest_|helmet_|armor_)') {
                $sell = 18 + ($Rar * 22)
                return @($sell, 0)
            }
            if ($Id -match '^(anomaly_|mark_|clock_|black_|whisper_|note_)') {
                $sell = switch ($Rar) { 0 { 5 } 1 { 18 } 2 { 40 } 3 { 70 } default { 10 } }
                return @($sell, 0)
            }
            return @(8, 0)
        }
        default { return @(0, 0) }
    }
}

# 2026-06-10 새 스케일: 기본 ×100 + 루디/전력셀/잭팟 명시 오버라이드 (잭팟 20만~500만)
function Get-ItemPricesScaled {
    param([string]$Id, [int]$Cat, [int]$Rar, [int]$Dur = 0, [int]$Fx = 0, [double]$Val = 0)

    # 루디 + 전력셀 + legendary 잭팟 = 최종값(×100 미적용, 직접 지정)
    $jack = @{
        ruby_dust              = @(5000, 0)
        ruby_shard             = @(50000, 0)
        ruby_crystal           = @(180000, 0)
        ruby_core              = @(600000, 0)
        ruby_corrupted         = @(250000, 0)
        val_power_cell         = @(4000000, 0)
        val_power_core         = @(1500000, 0)
        val_power_bank         = @(80000, 0)
        val_power_cell_drained = @(5000, 0)
        val_gold_bar           = @(1500000, 0)
        val_diamond_ring       = @(1200000, 0)
        val_antique_vase       = @(800000, 0)
        val_platinum_chain     = @(700000, 0)
        val_emerald_brooch     = @(600000, 0)
        val_opal_ring          = @(700000, 0)
        val_ring_wedding       = @(900000, 0)
        val_id_government      = @(500000, 0)
        val_ruby_set           = @(5000000, 0)
    }
    if ($jack.ContainsKey($Id)) { return , $jack[$Id] }

    $base = @(Get-ItemPrices -Id $Id -Cat $Cat -Rar $Rar -Dur $Dur -Fx $Fx -Val $Val)
    [int]$sell = $base[0]
    [int]$buy = $base[1]

    # 귀중품 Epic/Legendary = 잭팟 기준선(이름 없는 것도 보장)
    if ($Cat -eq 4 -and $sell -gt 0) {
        if ($Rar -ge 4) { return , @(1000000, 0) }
        if ($Rar -eq 3) { return , @(250000, 0) }
    }
    [int]$rsell = $sell * 100
    [int]$rbuy = $buy * 100
    return , @($rsell, $rbuy)
}
