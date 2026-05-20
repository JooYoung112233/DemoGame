const PLAYER_DATA = {
    name: '탐험가',
    hp: 100, maxHp: 100,
    atk: 15, def: 5, spd: 6,
    attackRange: 1.5,
    attackCooldown: 600,
    moveSpeed: 3,
    visionRadius: 5,
    color: 0x44aaff,
    equipment: { weapon: null, armor: null, consumable1: null, consumable2: null, consumable3: null }
};

const EQUIPMENT_DATA = {
    pipe: { id: 'pipe', name: '쇠파이프', type: 'weapon', icon: '🔧', atk: 5, range: 1.2, cooldown: 500, value: 30 },
    bat: { id: 'bat', name: '야구방망이', type: 'weapon', icon: '🏏', atk: 8, range: 1.5, cooldown: 700, value: 60 },
    machete: { id: 'machete', name: '마체테', type: 'weapon', icon: '🔪', atk: 12, range: 1.3, cooldown: 550, value: 120 },
    pistol: { id: 'pistol', name: '권총', type: 'weapon', icon: '🔫', atk: 20, range: 4, cooldown: 800, value: 250 },
    vest: { id: 'vest', name: '방탄조끼', type: 'armor', icon: '🦺', def: 8, value: 150 },
    jacket: { id: 'jacket', name: '가죽자켓', type: 'armor', icon: '🧥', def: 4, value: 50 },
    helmet: { id: 'helmet', name: '헬멧', type: 'armor', icon: '⛑️', def: 5, value: 80 }
};
