const ITEM_DATA = {
    // === 재료 (material) ===
    scrap:        { id: 'scrap', name: '고철', type: 'material', value: 5, icon: '🔩', weight: 1 },
    cloth:        { id: 'cloth', name: '천 조각', type: 'material', value: 3, icon: '🧵', weight: 0.5 },
    wire:         { id: 'wire', name: '전선', type: 'material', value: 8, icon: '🔌', weight: 0.5 },
    duct_tape:    { id: 'duct_tape', name: '덕트테이프', type: 'material', value: 12, icon: '🩹', weight: 0.3 },
    nails:        { id: 'nails', name: '못', type: 'material', value: 4, icon: '📌', weight: 0.5 },
    electronics:  { id: 'electronics', name: '전자부품', type: 'material', value: 25, icon: '🔧', weight: 1 },
    chemicals:    { id: 'chemicals', name: '화학약품', type: 'material', value: 20, icon: '🧪', weight: 1.5 },
    flashlight_part: { id: 'flashlight_part', name: '손전등 부품', type: 'material', value: 18, icon: '🔦', weight: 0.5 },
    lantern_fuel: { id: 'lantern_fuel', name: '랜턴 연료', type: 'material', value: 12, icon: '⛽', weight: 0.8 },

    // === 소모품 (consumable) ===
    bandage:      { id: 'bandage', name: '붕대', type: 'consumable', value: 10, heal: 15, icon: '🩹', weight: 0.3 },
    medkit:       { id: 'medkit', name: '의료킷', type: 'consumable', value: 40, heal: 50, icon: '💊', weight: 1 },
    painkillers:  { id: 'painkillers', name: '진통제', type: 'consumable', value: 20, heal: 25, icon: '💊', weight: 0.3 },
    medicine:     { id: 'medicine', name: '의약품', type: 'consumable', value: 30, heal: 35, icon: '💊', weight: 0.5 },
    battery:      { id: 'battery', name: '배터리', type: 'consumable', value: 15, icon: '🔋', weight: 0.3 },
    energy_drink: { id: 'energy_drink', name: '에너지드링크', type: 'consumable', value: 8, stamina: 15, icon: '🥤', weight: 0.5 },
    canned_food:  { id: 'canned_food', name: '통조림', type: 'consumable', value: 12, stamina: 25, icon: '🥫', weight: 1 },
    water:        { id: 'water', name: '생수', type: 'consumable', value: 6, stamina: 10, icon: '💧', weight: 0.8 },
    ration:       { id: 'ration', name: '전투식량', type: 'consumable', value: 18, stamina: 40, heal: 10, icon: '🍱', weight: 1 },

    // === 탄약 (ammo) ===
    ammo_pistol:  { id: 'ammo_pistol', name: '권총탄', type: 'ammo', value: 8, icon: '🔫', weight: 0.3 },
    ammo_rifle:   { id: 'ammo_rifle', name: '소총탄', type: 'ammo', value: 15, icon: '🎯', weight: 0.5 },
    ammo_shotgun: { id: 'ammo_shotgun', name: '산탄', type: 'ammo', value: 12, icon: '💥', weight: 0.5 },

    // === 도구 (tool) ===
    lockpick:     { id: 'lockpick', name: '락픽', type: 'tool', value: 20, icon: '🔑', weight: 0.2 },

    // === 장비 (equipment) ===
    scope:        { id: 'scope', name: '조준경', type: 'equipment', value: 50, effect: { range: 1 }, icon: '🔭', weight: 0.5 },
    armor_plate:  { id: 'armor_plate', name: '방탄판', type: 'equipment', value: 45, effect: { def: 3 }, icon: '🛡️', weight: 2 },
    helmet:       { id: 'helmet', name: '헬멧', type: 'equipment', value: 35, effect: { def: 2 }, icon: '⛑️', weight: 1.5 },
    tactical_vest:{ id: 'tactical_vest', name: '택티컬 조끼', type: 'equipment', value: 60, effect: { def: 4 }, icon: '🦺', weight: 2 },
    backpack:     { id: 'backpack', name: '백팩', type: 'equipment', value: 30, effect: { capacity: 10 }, icon: '🎒', weight: 1 },
    night_vision: { id: 'night_vision', name: '야시경', type: 'equipment', value: 80, effect: { vision: 2 }, icon: '🥽', weight: 1 },

    // === 귀중품 (valuable) ===
    gold_chain:   { id: 'gold_chain', name: '금목걸이', type: 'valuable', value: 80, icon: '📿', weight: 0.3 },
    flash_drive:  { id: 'flash_drive', name: 'USB', type: 'valuable', value: 60, icon: '💾', weight: 0.1 },
    dogtag:       { id: 'dogtag', name: '인식표', type: 'valuable', value: 25, icon: '🏷️', weight: 0.1 },
    rare_material:{ id: 'rare_material', name: '희귀 소재', type: 'valuable', value: 100, icon: '💎', weight: 1 },
    intel_folder: { id: 'intel_folder', name: '기밀문서', type: 'valuable', value: 90, icon: '📁', weight: 0.3 },
    circuit_board:{ id: 'circuit_board', name: '회로기판', type: 'valuable', value: 55, icon: '🖥️', weight: 0.5 },
    city_map:     { id: 'city_map', name: '도시 지도', type: 'valuable', value: 45, icon: '🗺️', weight: 0.2 },

    // === 열쇠 (key) ===
    old_key:      { id: 'old_key', name: '낡은 열쇠', type: 'key', value: 35, icon: '🗝️', weight: 0.1 }
};

const LOOT_TABLE = {
    barrel: [
        { item: 'scrap', chance: 0.5, min: 1, max: 2 },
        { item: 'nails', chance: 0.3, min: 1, max: 3 },
        { item: 'cloth', chance: 0.3, min: 1, max: 2 },
        { item: 'wire', chance: 0.15, min: 1, max: 1 }
    ],
    locker: [
        { item: 'bandage', chance: 0.4, min: 1, max: 2 },
        { item: 'painkillers', chance: 0.2, min: 1, max: 1 },
        { item: 'ammo_pistol', chance: 0.25, min: 1, max: 2 },
        { item: 'dogtag', chance: 0.1, min: 1, max: 1 },
        { item: 'gold_chain', chance: 0.05, min: 1, max: 1 },
        { item: 'battery', chance: 0.2, min: 1, max: 2 }
    ],
    crate: [
        { item: 'ammo_rifle', chance: 0.35, min: 1, max: 3 },
        { item: 'ammo_pistol', chance: 0.3, min: 1, max: 3 },
        { item: 'ammo_shotgun', chance: 0.2, min: 1, max: 2 },
        { item: 'armor_plate', chance: 0.1, min: 1, max: 1 },
        { item: 'helmet', chance: 0.1, min: 1, max: 1 },
        { item: 'lockpick', chance: 0.1, min: 1, max: 1 }
    ],
    medical: [
        { item: 'bandage', chance: 0.6, min: 1, max: 3 },
        { item: 'medkit', chance: 0.25, min: 1, max: 1 },
        { item: 'painkillers', chance: 0.4, min: 1, max: 2 },
        { item: 'medicine', chance: 0.15, min: 1, max: 1 },
        { item: 'chemicals', chance: 0.1, min: 1, max: 1 }
    ],
    computer: [
        { item: 'electronics', chance: 0.4, min: 1, max: 2 },
        { item: 'flash_drive', chance: 0.15, min: 1, max: 1 },
        { item: 'circuit_board', chance: 0.2, min: 1, max: 1 },
        { item: 'intel_folder', chance: 0.1, min: 1, max: 1 },
        { item: 'wire', chance: 0.3, min: 1, max: 2 },
        { item: 'flashlight_part', chance: 0.1, min: 1, max: 1 }
    ],
    common_crate: [
        { item: 'scrap', chance: 0.5, min: 1, max: 3 },
        { item: 'bandage', chance: 0.3, min: 1, max: 2 },
        { item: 'canned_food', chance: 0.3, min: 1, max: 2 },
        { item: 'water', chance: 0.4, min: 1, max: 2 },
        { item: 'cloth', chance: 0.3, min: 1, max: 2 },
        { item: 'battery', chance: 0.15, min: 1, max: 1 },
        { item: 'lantern_fuel', chance: 0.1, min: 1, max: 1 }
    ],
    rare_cache: [
        { item: 'rare_material', chance: 0.3, min: 1, max: 1 },
        { item: 'tactical_vest', chance: 0.08, min: 1, max: 1 },
        { item: 'night_vision', chance: 0.05, min: 1, max: 1 },
        { item: 'backpack', chance: 0.1, min: 1, max: 1 },
        { item: 'scope', chance: 0.1, min: 1, max: 1 },
        { item: 'medkit', chance: 0.3, min: 1, max: 2 },
        { item: 'city_map', chance: 0.15, min: 1, max: 1 }
    ],

    // === 개장(밤) 전용 루트 테이블 ===
    night_shelf: [
        { item: 'gold_chain', chance: 0.35, min: 1, max: 1 },
        { item: 'city_map', chance: 0.25, min: 1, max: 1 },
        { item: 'rare_material', chance: 0.2, min: 1, max: 1 },
        { item: 'electronics', chance: 0.3, min: 1, max: 2 },
        { item: 'circuit_board', chance: 0.25, min: 1, max: 1 },
        { item: 'flashlight_part', chance: 0.15, min: 1, max: 1 },
        { item: 'battery', chance: 0.3, min: 1, max: 2 }
    ],
    night_safe: [
        { item: 'intel_folder', chance: 0.4, min: 1, max: 1 },
        { item: 'flash_drive', chance: 0.35, min: 1, max: 1 },
        { item: 'tactical_vest', chance: 0.15, min: 1, max: 1 },
        { item: 'old_key', chance: 0.2, min: 1, max: 1 },
        { item: 'gold_chain', chance: 0.3, min: 1, max: 1 },
        { item: 'rare_material', chance: 0.25, min: 1, max: 1 },
        { item: 'night_vision', chance: 0.1, min: 1, max: 1 }
    ],
    night_medical: [
        { item: 'medicine', chance: 0.5, min: 1, max: 2 },
        { item: 'medkit', chance: 0.4, min: 1, max: 2 },
        { item: 'painkillers', chance: 0.5, min: 1, max: 3 },
        { item: 'bandage', chance: 0.6, min: 1, max: 3 },
        { item: 'chemicals', chance: 0.25, min: 1, max: 1 },
        { item: 'energy_drink', chance: 0.3, min: 1, max: 2 }
    ]
};

const MAX_INVENTORY = 20;
const MAX_WEIGHT = 30;   // kg

// Rarity tiers (determines search reveal speed & visual)
const ITEM_RARITY = {
    // common (grey)
    scrap: 'common', cloth: 'common', nails: 'common', wire: 'common',
    bandage: 'common', water: 'common', canned_food: 'common',
    ammo_pistol: 'common', ammo_shotgun: 'common',
    // uncommon (green)
    duct_tape: 'uncommon', electronics: 'uncommon', chemicals: 'uncommon',
    painkillers: 'uncommon', energy_drink: 'uncommon', ration: 'uncommon',
    ammo_rifle: 'uncommon', battery: 'uncommon', lantern_fuel: 'uncommon',
    lockpick: 'uncommon', medicine: 'uncommon', flashlight_part: 'uncommon',
    dogtag: 'uncommon', helmet: 'uncommon',
    // rare (blue)
    medkit: 'rare', scope: 'rare', armor_plate: 'rare', backpack: 'rare',
    circuit_board: 'rare', flash_drive: 'rare', city_map: 'rare', old_key: 'rare',
    // epic (purple)
    gold_chain: 'epic', tactical_vest: 'epic', intel_folder: 'epic',
    night_vision: 'epic', rare_material: 'epic'
};

const RARITY_CONFIG = {
    common:   { color: '#aaaaaa', border: 0x666666, revealMs: 300,  glow: false },
    uncommon: { color: '#44ff88', border: 0x44ff88, revealMs: 500,  glow: false },
    rare:     { color: '#4488ff', border: 0x4488ff, revealMs: 900,  glow: true },
    epic:     { color: '#aa44ff', border: 0xaa44ff, revealMs: 1400, glow: true }
};
