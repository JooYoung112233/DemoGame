const ENEMY_DATA = {
    zombie:          { id: 'zombie', name: '좀비', type: 'undead', hp: 45, maxHp: 45, ap: 2, moveRange: 2, atk: 8, def: 2, spd: 2, range: 1, color: 0x668844, loot: ['scrap','bandage','cloth'], ai: 'melee_rush' },
    zombie_crawler:  { id: 'zombie_crawler', name: '기는 좀비', type: 'undead', hp: 30, maxHp: 30, ap: 1, moveRange: 1, atk: 12, def: 1, spd: 1, range: 1, color: 0x556633, loot: ['scrap','nails'], ai: 'melee_rush' },
    zombie_runner:   { id: 'zombie_runner', name: '돌진 좀비', type: 'undead', hp: 35, maxHp: 35, ap: 3, moveRange: 4, atk: 10, def: 1, spd: 7, range: 1, color: 0x778844, loot: ['bandage','cloth'], ai: 'aggressive' },
    zombie_brute:    { id: 'zombie_brute', name: '돌연변이 좀비', type: 'undead', hp: 120, maxHp: 120, ap: 2, moveRange: 2, atk: 20, def: 8, spd: 1, range: 1, color: 0x446633, loot: ['scrap','mutant_sample','chemicals'], ai: 'melee_rush' },
    raider:          { id: 'raider', name: '약탈자', type: 'human', hp: 65, maxHp: 65, ap: 3, moveRange: 3, atk: 14, def: 5, spd: 5, range: 2, color: 0xaa6644, loot: ['ammo_pistol','canned_food','bandage','dogtag'], ai: 'balanced' },
    raider_shotgun:  { id: 'raider_shotgun', name: '산탄 약탈자', type: 'human', hp: 75, maxHp: 75, ap: 3, moveRange: 2, atk: 22, def: 6, spd: 4, range: 2, color: 0x996644, loot: ['ammo_shotgun','ration','armor_plate'], ai: 'balanced' },
    raider_sniper:   { id: 'raider_sniper', name: '저격 약탈자', type: 'human', hp: 50, maxHp: 50, ap: 3, moveRange: 2, atk: 20, def: 3, spd: 4, range: 5, color: 0x886644, loot: ['ammo_rifle','scope','energy_drink'], ai: 'ranged_kite' },
    raider_boss:     { id: 'raider_boss', name: '약탈자 두목', type: 'human', hp: 110, maxHp: 110, ap: 4, moveRange: 3, atk: 18, def: 8, spd: 6, range: 3, color: 0xcc6644, loot: ['tactical_vest','ammo_rifle','gold_chain','medkit'], ai: 'aggressive' },
    mutant:          { id: 'mutant', name: '변이체', type: 'mutant', hp: 100, maxHp: 100, ap: 3, moveRange: 4, atk: 16, def: 6, spd: 7, range: 1, color: 0x884488, loot: ['mutant_sample','rare_material','chemicals'], ai: 'aggressive' },
    mutant_spitter:  { id: 'mutant_spitter', name: '독뿜는 변이체', type: 'mutant', hp: 70, maxHp: 70, ap: 3, moveRange: 3, atk: 14, def: 4, spd: 5, range: 4, color: 0x66aa66, loot: ['mutant_sample','chemicals'], ai: 'ranged_kite' },
    feral_dog:       { id: 'feral_dog', name: '야생 들개', type: 'animal', hp: 40, maxHp: 40, ap: 3, moveRange: 5, atk: 10, def: 2, spd: 8, range: 1, color: 0x8a7a5a, loot: ['cloth'], ai: 'aggressive' },
    turret:          { id: 'turret', name: '자동 터렛', type: 'machine', hp: 80, maxHp: 80, ap: 2, moveRange: 0, atk: 18, def: 12, spd: 3, range: 6, color: 0x888888, loot: ['electronics','circuit_board','wire'], ai: 'ranged_kite' }
};

const ENCOUNTER_TABLE = {
    suburbs: [
        { enemies: ['zombie','zombie','zombie'], weight: 30 },
        { enemies: ['zombie','zombie','zombie','zombie_crawler'], weight: 20 },
        { enemies: ['zombie_runner','zombie','zombie'], weight: 15 },
        { enemies: ['zombie','zombie_brute'], weight: 10 },
        { enemies: ['feral_dog','feral_dog','feral_dog'], weight: 15 },
        { enemies: ['raider','raider'], weight: 10 }
    ],
    industrial: [
        { enemies: ['raider','raider','raider'], weight: 25 },
        { enemies: ['raider','raider_shotgun','raider'], weight: 20 },
        { enemies: ['raider','raider_sniper','raider'], weight: 15 },
        { enemies: ['raider_boss','raider','raider_shotgun'], weight: 8 },
        { enemies: ['zombie_brute','zombie','zombie','zombie'], weight: 15 },
        { enemies: ['turret','raider','raider'], weight: 7 },
        { enemies: ['zombie_runner','zombie_runner','zombie','zombie'], weight: 10 }
    ],
    deadzone: [
        { enemies: ['mutant','mutant'], weight: 25 },
        { enemies: ['mutant','mutant_spitter','zombie_brute'], weight: 20 },
        { enemies: ['mutant_spitter','mutant_spitter','mutant'], weight: 15 },
        { enemies: ['zombie_brute','zombie_brute','zombie_runner','zombie_runner'], weight: 15 },
        { enemies: ['raider_boss','raider_sniper','raider_shotgun','raider'], weight: 10 },
        { enemies: ['mutant','mutant','mutant_spitter','zombie_brute'], weight: 15 }
    ]
};

const ENEMY_ICONS = { undead: '💀', human: '🔫', mutant: '☣️', animal: '🐕', machine: '⚙️' };
