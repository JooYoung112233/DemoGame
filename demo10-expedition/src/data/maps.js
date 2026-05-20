const TILE = {
    FLOOR: 0, WALL: 1, DOOR: 2, LOOT: 3, ENEMY: 4, EXIT: 5,
    RUBBLE: 6, WATER: 7, GRASS: 8, ROAD: 9, FURNITURE: 10,
    BARREL: 11, LOCKER: 12, CRATE: 13, MEDICAL: 14, COMPUTER: 15,
    LOCKED_DOOR: 16, NIGHT_FLOOR: 17, NIGHT_WALL: 18, STREETLIGHT: 19
};

const TILE_COLORS = {
    [TILE.FLOOR]:       { top: 0x3a3a4a, side: 0x2a2a3a },
    [TILE.WALL]:        { top: 0x5a5a6a, side: 0x3a3a4a, height: 2 },
    [TILE.DOOR]:        { top: 0x6a5a3a, side: 0x4a3a2a },
    [TILE.LOOT]:        { top: 0x3a4a3a, side: 0x2a3a2a, glow: 0x44ff88 },
    [TILE.EXIT]:        { top: 0x3a3a5a, side: 0x2a2a4a, glow: 0x4488ff },
    [TILE.RUBBLE]:      { top: 0x4a4a3a, side: 0x3a3a2a },
    [TILE.WATER]:       { top: 0x2a3a5a, side: 0x1a2a4a },
    [TILE.GRASS]:       { top: 0x2a3a2a, side: 0x1a2a1a },
    [TILE.ROAD]:        { top: 0x4a4a4a, side: 0x3a3a3a },
    [TILE.FURNITURE]:   { top: 0x5a4a3a, side: 0x3a2a1a },
    [TILE.BARREL]:      { top: 0x4a3a2a, side: 0x3a2a1a, glow: 0xffaa44 },
    [TILE.LOCKER]:      { top: 0x3a4a5a, side: 0x2a3a4a, glow: 0x44aaff },
    [TILE.CRATE]:       { top: 0x4a4a2a, side: 0x3a3a1a, glow: 0xffcc44 },
    [TILE.MEDICAL]:     { top: 0x3a5a3a, side: 0x2a3a2a, glow: 0x44ff88 },
    [TILE.COMPUTER]:    { top: 0x2a3a4a, side: 0x1a2a3a, glow: 0x8844ff },
    [TILE.LOCKED_DOOR]: { top: 0x5a4a3a, side: 0x3a2a1a, glow: 0x4466cc },
    [TILE.NIGHT_FLOOR]: { top: 0x2a2a4a, side: 0x1a1a3a },
    [TILE.NIGHT_WALL]:  { top: 0x3a3a6a, side: 0x2a2a5a, height: 2 },
    [TILE.STREETLIGHT]: { top: 0x5a5a3a, side: 0x4a4a2a, glow: 0xffdd66 }
};

const SEARCHABLE_TILES = {
    [TILE.BARREL]:   { name: '드럼통', lootPool: 'barrel', icon: '🛢️', slots: 4, searchTime: 3000 },
    [TILE.LOCKER]:   { name: '사물함', lootPool: 'locker', icon: '🗄️', slots: 6, searchTime: 5000 },
    [TILE.CRATE]:    { name: '보급상자', lootPool: 'crate', icon: '📦', slots: 6, searchTime: 5000 },
    [TILE.MEDICAL]:  { name: '의료함', lootPool: 'medical', icon: '🏥', slots: 6, searchTime: 4000 },
    [TILE.COMPUTER]: { name: '컴퓨터', lootPool: 'computer', icon: '💻', slots: 4, searchTime: 6000 },
    [TILE.LOOT]:     { name: '루트', lootPool: 'common_crate', icon: '📦', slots: 4, searchTime: 3500 }
};

const ZONE_DATA = {
    downtown: {
        id: 'downtown', name: '폐도심',
        desc: '버려진 도심 거리. 낮엔 밴딧이 배회하고, 밤엔 상가들이 개장한다.',
        difficulty: 1,
        mapWidth: 70, mapHeight: 55,
        buildingCount: { min: 12, max: 18 },
        enemyCount: { min: 8, max: 14 },
        nightEnemyCount: { min: 10, max: 16 },
        extractCount: 2,
        lootDensity: 0.5,
        encounterType: 'ruins',
        nightEncounterType: 'opened_normal',
        bgColor: 0x1a1a2a,
        nightBgColor: 0x0a0a1a,
        outdoorRatio: 0.4,
        dayDuration: 120,
        nightDuration: 300,
        openBuildings: { min: 3, max: 5 }
    },
    hospital_district: {
        id: 'hospital_district', name: '병원 구역',
        desc: '폐병원과 의료시설 밀집 구역. 밤이면 병원이 다시 열린다.',
        difficulty: 2,
        mapWidth: 80, mapHeight: 60,
        buildingCount: { min: 10, max: 14 },
        enemyCount: { min: 10, max: 16 },
        nightEnemyCount: { min: 14, max: 20 },
        extractCount: 2,
        lootDensity: 0.6,
        encounterType: 'ruins',
        nightEncounterType: 'opened_normal',
        bgColor: 0x1a1a1a,
        nightBgColor: 0x0a0a15,
        outdoorRatio: 0.3,
        dayDuration: 100,
        nightDuration: 270,
        openBuildings: { min: 2, max: 4 }
    },
    station_area: {
        id: 'station_area', name: '중앙역 일대',
        desc: '폐역과 주변 상가. 밤에 열차가 도착하는 소문이 있다.',
        difficulty: 3,
        mapWidth: 90, mapHeight: 65,
        buildingCount: { min: 14, max: 20 },
        enemyCount: { min: 12, max: 18 },
        nightEnemyCount: { min: 18, max: 26 },
        extractCount: 3,
        lootDensity: 0.7,
        encounterType: 'ruins',
        nightEncounterType: 'opened_rare',
        bgColor: 0x1a0a1a,
        nightBgColor: 0x080810,
        outdoorRatio: 0.35,
        dayDuration: 90,
        nightDuration: 240,
        openBuildings: { min: 4, max: 7 }
    }
};
