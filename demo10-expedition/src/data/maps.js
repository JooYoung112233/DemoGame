const TILE = {
    FLOOR: 0, WALL: 1, DOOR: 2, LOOT: 3, ENEMY: 4, EXIT: 5,
    RUBBLE: 6, WATER: 7, GRASS: 8, ROAD: 9, FURNITURE: 10,
    BARREL: 11, LOCKER: 12, CRATE: 13, MEDICAL: 14, COMPUTER: 15
};

const TILE_COLORS = {
    [TILE.FLOOR]:     { top: 0x3a3a4a, side: 0x2a2a3a },
    [TILE.WALL]:      { top: 0x5a5a6a, side: 0x3a3a4a, height: 2 },
    [TILE.DOOR]:      { top: 0x6a5a3a, side: 0x4a3a2a },
    [TILE.LOOT]:      { top: 0x3a4a3a, side: 0x2a3a2a, glow: 0x44ff88 },
    [TILE.EXIT]:      { top: 0x3a3a5a, side: 0x2a2a4a, glow: 0x4488ff },
    [TILE.RUBBLE]:    { top: 0x4a4a3a, side: 0x3a3a2a },
    [TILE.WATER]:     { top: 0x2a3a5a, side: 0x1a2a4a },
    [TILE.GRASS]:     { top: 0x2a3a2a, side: 0x1a2a1a },
    [TILE.ROAD]:      { top: 0x4a4a4a, side: 0x3a3a3a },
    [TILE.FURNITURE]: { top: 0x5a4a3a, side: 0x3a2a1a },
    [TILE.BARREL]:    { top: 0x4a3a2a, side: 0x3a2a1a, glow: 0xffaa44 },
    [TILE.LOCKER]:    { top: 0x3a4a5a, side: 0x2a3a4a, glow: 0x44aaff },
    [TILE.CRATE]:     { top: 0x4a4a2a, side: 0x3a3a1a, glow: 0xffcc44 },
    [TILE.MEDICAL]:   { top: 0x3a5a3a, side: 0x2a3a2a, glow: 0x44ff88 },
    [TILE.COMPUTER]:  { top: 0x2a3a4a, side: 0x1a2a3a, glow: 0x8844ff }
};

const SEARCHABLE_TILES = {
    [TILE.BARREL]:   { name: '드럼통', lootPool: 'barrel', icon: '🛢️' },
    [TILE.LOCKER]:   { name: '사물함', lootPool: 'locker', icon: '🗄️' },
    [TILE.CRATE]:    { name: '보급상자', lootPool: 'crate', icon: '📦' },
    [TILE.MEDICAL]:  { name: '의료함', lootPool: 'medical', icon: '🏥' },
    [TILE.COMPUTER]: { name: '컴퓨터', lootPool: 'computer', icon: '💻' },
    [TILE.LOOT]:     { name: '루트', lootPool: 'common_crate', icon: '📦' }
};

const ZONE_DATA = {
    suburbs: {
        id: 'suburbs', name: '외곽 주거지',
        desc: '버려진 주택가. 좀비가 배회하지만 생활용품이 남아있다.',
        difficulty: 1,
        mapWidth: 70, mapHeight: 55,
        buildingCount: { min: 12, max: 18 },
        enemyCount: { min: 8, max: 14 },
        extractCount: 2,
        lootDensity: 0.6,
        encounterType: 'suburbs',
        bgColor: 0x1a1a2a,
        outdoorRatio: 0.4
    },
    industrial: {
        id: 'industrial', name: '산업 지구',
        desc: '폐공장과 창고. 약탈자 거점. 군용 물자가 숨겨져 있다.',
        difficulty: 2,
        mapWidth: 80, mapHeight: 60,
        buildingCount: { min: 10, max: 14 },
        enemyCount: { min: 12, max: 18 },
        extractCount: 2,
        lootDensity: 0.5,
        encounterType: 'industrial',
        bgColor: 0x1a1a1a,
        outdoorRatio: 0.3
    },
    deadzone: {
        id: 'deadzone', name: '오염 구역',
        desc: '변이체가 서식하는 위험 구역. 희귀 자원의 보고.',
        difficulty: 3,
        mapWidth: 90, mapHeight: 65,
        buildingCount: { min: 14, max: 20 },
        enemyCount: { min: 16, max: 24 },
        extractCount: 3,
        lootDensity: 0.7,
        encounterType: 'deadzone',
        bgColor: 0x1a0a1a,
        outdoorRatio: 0.35
    }
};
