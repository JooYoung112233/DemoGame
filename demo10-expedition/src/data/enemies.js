const ENEMY_DATA = {
    // ===== 낮 적 (Day Enemies) =====
    bandit:          { id: 'bandit', name: '밴딧', type: 'human', hp: 50, maxHp: 50, atk: 10, def: 3, color: 0xaa7744, detectionRange: 6, chaseSpeed: 2.2, attackCooldown: 1100, attackRange: 2, alertDuration: 600, loseRange: 13, loot: ['scrap','bandage','ammo_pistol','canned_food'] },
    bandit_heavy:    { id: 'bandit_heavy', name: '무장 밴딧', type: 'human', hp: 75, maxHp: 75, atk: 18, def: 6, color: 0x886644, detectionRange: 5, chaseSpeed: 1.8, attackCooldown: 1400, attackRange: 2, alertDuration: 700, loseRange: 11, loot: ['ammo_shotgun','bandage','canned_food','tactical_vest'] },
    rival_scout:     { id: 'rival_scout', name: '경쟁 원정대 정찰', type: 'human', hp: 55, maxHp: 55, atk: 12, def: 4, color: 0x5588aa, detectionRange: 8, chaseSpeed: 3.2, attackCooldown: 1000, attackRange: 2, alertDuration: 500, loseRange: 16, loot: ['ammo_pistol','lockpick','bandage','dogtag'] },
    rival_fighter:   { id: 'rival_fighter', name: '경쟁 원정대 전투원', type: 'human', hp: 90, maxHp: 90, atk: 16, def: 7, color: 0x4477aa, detectionRange: 7, chaseSpeed: 2.5, attackCooldown: 1000, attackRange: 2, alertDuration: 600, loseRange: 15, loot: ['ammo_shotgun','medicine','tactical_vest','gold_chain','dogtag'] },
    feral_dog:       { id: 'feral_dog', name: '야생 들개', type: 'animal', hp: 35, maxHp: 35, atk: 9, def: 1, color: 0x8a7a5a, detectionRange: 8, chaseSpeed: 4.0, attackCooldown: 800, attackRange: 1, alertDuration: 300, loseRange: 18, loot: ['scrap'] },

    // ===== 밤 적 (Night Enemies) =====
    night_bandit:    { id: 'night_bandit', name: '밤 밴딧', type: 'human', hp: 60, maxHp: 60, atk: 13, def: 4, color: 0x665533, detectionRange: 7, chaseSpeed: 2.5, attackCooldown: 1050, attackRange: 2, alertDuration: 700, loseRange: 14, loot: ['ammo_pistol','battery','flashlight_part','canned_food'] },
    guard:           { id: 'guard', name: '경비원', type: 'entity', hp: 80, maxHp: 80, atk: 15, def: 6, color: 0x334455, detectionRange: 4, chaseSpeed: 2.0, attackCooldown: 1200, attackRange: 1, alertDuration: 1200, loseRange: 8, loot: ['battery','flashlight_part','lockpick'] },
    patroller:       { id: 'patroller', name: '순찰자', type: 'entity', hp: 70, maxHp: 70, atk: 13, def: 5, color: 0x445566, detectionRange: 7, chaseSpeed: 2.3, attackCooldown: 1100, attackRange: 1, alertDuration: 900, loseRange: 12, loot: ['battery','scrap','flashlight_part'] },
    manager:         { id: 'manager', name: '관리자', type: 'entity', hp: 120, maxHp: 120, atk: 22, def: 9, color: 0x223344, detectionRange: 5, chaseSpeed: 1.5, attackCooldown: 1500, attackRange: 2, alertDuration: 1000, loseRange: 7, loot: ['rare_material','intel_folder','medicine','gold_chain'] },
    lost_customer:   { id: 'lost_customer', name: '길 잃은 손님', type: 'entity', hp: 40, maxHp: 40, atk: 7, def: 2, color: 0x667788, detectionRange: 3, chaseSpeed: 1.8, attackCooldown: 1300, attackRange: 1, alertDuration: 400, loseRange: 6, loot: ['scrap','battery'] }
};

const ENCOUNTER_TABLE = {
    ruins: [
        { enemies: ['bandit'], weight: 25 },
        { enemies: ['bandit','bandit'], weight: 15 },
        { enemies: ['bandit_heavy'], weight: 15 },
        { enemies: ['feral_dog','feral_dog'], weight: 20 },
        { enemies: ['feral_dog','feral_dog','feral_dog'], weight: 5 },
        { enemies: ['rival_scout'], weight: 10 },
        { enemies: ['bandit','feral_dog'], weight: 10 }
    ],
    opened_normal: [
        { enemies: ['night_bandit'], weight: 20 },
        { enemies: ['guard'], weight: 20 },
        { enemies: ['patroller'], weight: 20 },
        { enemies: ['lost_customer','lost_customer'], weight: 15 },
        { enemies: ['night_bandit','guard'], weight: 10 },
        { enemies: ['patroller','lost_customer'], weight: 10 },
        { enemies: ['guard','guard'], weight: 5 }
    ],
    opened_rare: [
        { enemies: ['manager'], weight: 20 },
        { enemies: ['guard','guard'], weight: 20 },
        { enemies: ['manager','guard'], weight: 15 },
        { enemies: ['rival_fighter'], weight: 15 },
        { enemies: ['rival_fighter','guard'], weight: 10 },
        { enemies: ['manager','patroller'], weight: 10 },
        { enemies: ['rival_fighter','rival_fighter'], weight: 5 },
        { enemies: ['manager','manager'], weight: 5 }
    ]
};

const ENEMY_ICONS = { human: '🔫', animal: '🐕', entity: '👤' };
