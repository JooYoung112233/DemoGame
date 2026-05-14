const BO_ROOM_TYPES = {
    empty: { name: '빈 방', icon: '▫️', color: '#666666' },
    enemy: { name: '적 조우', icon: '💀', color: '#ff4444' },
    item: { name: '아이템', icon: '📦', color: '#44ff88' },
    trap: { name: '함정', icon: '⚠️', color: '#ffaa00' },
    entrance: { name: '입구', icon: '🚪', color: '#44aaff' },
    exit: { name: '출구', icon: '🔓', color: '#44ff44' },
    locked: { name: '잠긴 방', icon: '🔒', color: '#ff8844' },
    rest: { name: '은신처', icon: '🏠', color: '#88aaff' }
};

function rollRoomEvent(depth, tension) {
    const r = Math.random();
    const enemyChance = 0.25 + tension * 0.05;
    const itemChance = 0.30 - tension * 0.02;
    const trapChance = 0.10 + tension * 0.03;
    const restChance = 0.05;

    if (r < enemyChance) return 'enemy';
    if (r < enemyChance + itemChance) return 'item';
    if (r < enemyChance + itemChance + trapChance) return 'trap';
    if (r < enemyChance + itemChance + trapChance + restChance) return 'rest';
    return 'empty';
}

function generateEnemiesForRoom(depth, tension) {
    const scale = 1 + depth * 0.15 + tension * 0.10;
    const count = Math.min(1 + Math.floor((depth + tension) / 3), 4);
    const enemies = [];

    for (let i = 0; i < count; i++) {
        const r = Math.random();
        let type;
        if (tension >= 3) {
            type = r < 0.25 ? 'stalker' : r < 0.50 ? 'charger' : r < 0.75 ? 'creeper' : 'collector';
        } else {
            type = r < 0.40 ? 'stalker' : r < 0.70 ? 'charger' : r < 0.90 ? 'creeper' : 'collector';
        }

        const base = BO_ENEMIES[type];
        enemies.push({
            ...base,
            hp: Math.floor(base.hp * scale),
            atk: Math.floor(base.atk * scale),
            def: Math.floor(base.def * scale),
            type: type
        });
    }
    return enemies;
}
