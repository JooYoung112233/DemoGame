const ENEMY_DATA = {
    zombie:          { id: 'zombie', name: '좀비', type: 'undead', hp: 45, maxHp: 45, atk: 8, def: 2, color: 0x668844, detectionRange: 4, chaseSpeed: 1.5, attackCooldown: 1200, attackRange: 1, alertDuration: 800, loseRange: 12, loot: ['scrap','bandage','cloth'] },
    zombie_crawler:  { id: 'zombie_crawler', name: '기는 좀비', type: 'undead', hp: 30, maxHp: 30, atk: 12, def: 1, color: 0x556633, detectionRange: 3, chaseSpeed: 0.8, attackCooldown: 1500, attackRange: 1, alertDuration: 600, loseRange: 8, loot: ['scrap','nails'] },
    zombie_runner:   { id: 'zombie_runner', name: '돌진 좀비', type: 'undead', hp: 35, maxHp: 35, atk: 10, def: 1, color: 0x778844, detectionRange: 6, chaseSpeed: 3.5, attackCooldown: 1000, attackRange: 1, alertDuration: 500, loseRange: 15, loot: ['bandage','cloth'] },
    zombie_brute:    { id: 'zombie_brute', name: '돌연변이 좀비', type: 'undead', hp: 120, maxHp: 120, atk: 20, def: 8, color: 0x446633, detectionRange: 5, chaseSpeed: 1.2, attackCooldown: 1800, attackRange: 1, alertDuration: 1000, loseRange: 10, loot: ['scrap','mutant_sample','chemicals'] },
    raider:          { id: 'raider', name: '약탈자', type: 'human', hp: 65, maxHp: 65, atk: 14, def: 5, color: 0xaa6644, detectionRange: 7, chaseSpeed: 2.5, attackCooldown: 1000, attackRange: 2, alertDuration: 600, loseRange: 14, loot: ['ammo_pistol','canned_food','bandage','dogtag'] },
    raider_shotgun:  { id: 'raider_shotgun', name: '산탄 약탈자', type: 'human', hp: 75, maxHp: 75, atk: 22, def: 6, color: 0x996644, detectionRange: 6, chaseSpeed: 2, attackCooldown: 1400, attackRange: 2, alertDuration: 600, loseRange: 12, loot: ['ammo_shotgun','ration','armor_plate'] },
    raider_sniper:   { id: 'raider_sniper', name: '저격 약탈자', type: 'human', hp: 50, maxHp: 50, atk: 20, def: 3, color: 0x886644, detectionRange: 10, chaseSpeed: 1.8, attackCooldown: 2000, attackRange: 5, alertDuration: 400, loseRange: 16, loot: ['ammo_rifle','scope','energy_drink'] },
    raider_boss:     { id: 'raider_boss', name: '약탈자 두목', type: 'human', hp: 110, maxHp: 110, atk: 18, def: 8, color: 0xcc6644, detectionRange: 8, chaseSpeed: 2.8, attackCooldown: 900, attackRange: 3, alertDuration: 500, loseRange: 16, loot: ['tactical_vest','ammo_rifle','gold_chain','medkit'] },
    mutant:          { id: 'mutant', name: '변이체', type: 'mutant', hp: 100, maxHp: 100, atk: 16, def: 6, color: 0x884488, detectionRange: 6, chaseSpeed: 3, attackCooldown: 1000, attackRange: 1, alertDuration: 600, loseRange: 14, loot: ['mutant_sample','rare_material','chemicals'] },
    mutant_spitter:  { id: 'mutant_spitter', name: '독뿜는 변이체', type: 'mutant', hp: 70, maxHp: 70, atk: 14, def: 4, color: 0x66aa66, detectionRange: 7, chaseSpeed: 2, attackCooldown: 1500, attackRange: 4, alertDuration: 500, loseRange: 14, loot: ['mutant_sample','chemicals'] },
    feral_dog:       { id: 'feral_dog', name: '야생 들개', type: 'animal', hp: 40, maxHp: 40, atk: 10, def: 2, color: 0x8a7a5a, detectionRange: 8, chaseSpeed: 4, attackCooldown: 800, attackRange: 1, alertDuration: 300, loseRange: 18, loot: ['cloth'] }
};

const ENCOUNTER_TABLE = {
    suburbs: [
        { enemies: ['zombie'], weight: 30 },
        { enemies: ['zombie','zombie_crawler'], weight: 20 },
        { enemies: ['zombie_runner'], weight: 15 },
        { enemies: ['zombie_brute'], weight: 5 },
        { enemies: ['feral_dog','feral_dog'], weight: 15 },
        { enemies: ['raider'], weight: 15 }
    ],
    industrial: [
        { enemies: ['raider','raider'], weight: 25 },
        { enemies: ['raider_shotgun'], weight: 20 },
        { enemies: ['raider_sniper'], weight: 15 },
        { enemies: ['raider_boss'], weight: 5 },
        { enemies: ['zombie_brute','zombie'], weight: 15 },
        { enemies: ['zombie_runner','zombie_runner'], weight: 10 },
        { enemies: ['raider','raider_shotgun'], weight: 10 }
    ],
    deadzone: [
        { enemies: ['mutant'], weight: 25 },
        { enemies: ['mutant_spitter'], weight: 20 },
        { enemies: ['mutant','mutant_spitter'], weight: 15 },
        { enemies: ['zombie_brute','zombie_runner'], weight: 15 },
        { enemies: ['raider_boss','raider_sniper'], weight: 10 },
        { enemies: ['mutant','mutant'], weight: 15 }
    ]
};

const ENEMY_ICONS = { undead: '💀', human: '🔫', mutant: '☣️', animal: '🐕', machine: '⚙️' };
