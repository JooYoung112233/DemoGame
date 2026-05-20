const PLAYER_DATA = {
    name: '원정꾼',
    hp: 100, maxHp: 100,
    atk: 12, def: 4, spd: 6,
    attackRange: 1.5,
    attackCooldown: 650,
    moveSpeed: 3,
    visionRadius: 6,          // 낮 시야 (원형)
    nightVisionRadius: 2,     // 밤 기본 시야 (손전등 없이)
    flashlightArc: 90,        // 손전등 부채꼴 각도 (도)
    flashlightRange: 7,       // 손전등 시야 거리
    flashlightBattery: 100,   // 배터리 최대치
    batteryDrain: 0.15,       // 초당 배터리 소모
    color: 0x44aaff,
    equipment: { weapon: null, armor: null, consumable1: null, consumable2: null, consumable3: null }
};

const EQUIPMENT_DATA = {
    // ── 근접 무기 ──
    pipe:     { id: 'pipe', name: '쇠파이프', type: 'weapon', icon: '🔧', atk: 4, range: 1.2, cooldown: 500, value: 30 },
    bat:      { id: 'bat', name: '야구방망이', type: 'weapon', icon: '🏏', atk: 7, range: 1.5, cooldown: 700, value: 60 },
    machete:  { id: 'machete', name: '마체테', type: 'weapon', icon: '🔪', atk: 10, range: 1.3, cooldown: 550, value: 120 },
    crowbar:  { id: 'crowbar', name: '빠루', type: 'weapon', icon: '🪝', atk: 6, range: 1.3, cooldown: 600, value: 45 },
    // ── 원거리 무기 ──
    pistol:   { id: 'pistol', name: '권총', type: 'weapon', icon: '🔫', atk: 16, range: 4, cooldown: 800, value: 250 },
    shotgun:  { id: 'shotgun', name: '산탄총', type: 'weapon', icon: '💥', atk: 22, range: 2.5, cooldown: 1200, value: 350 },
    // ── 방어구 ──
    vest:     { id: 'vest', name: '방탄조끼', type: 'armor', icon: '🦺', def: 8, value: 150 },
    jacket:   { id: 'jacket', name: '가죽자켓', type: 'armor', icon: '🧥', def: 4, value: 50 },
    helmet:   { id: 'helmet', name: '헬멧', type: 'armor', icon: '⛑️', def: 5, value: 80 },
    // ── 손전등 업그레이드 ──
    flashlight_mod: { id: 'flashlight_mod', name: '개량 손전등', type: 'accessory', icon: '🔦', flashlightRange: 2, batteryDrain: -0.03, value: 120 }
};
