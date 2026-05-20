// Tile types and rendering data
const TILE_SIZE = 32;

const TILE_TYPES = {
    ROAD: 0,       // walkable road/ground
    WALL: 1,       // impassable wall
    FLOOR: 2,      // building interior floor
    LOOT: 3,       // searchable loot spot
    SAFE: 4,       // safe house
    ESCAPE: 5,     // escape point
    RUBBLE: 6,     // decorative rubble (walkable)
    DOOR: 7,       // door (walkable)
};

const TILE_COLORS = {
    [TILE_TYPES.ROAD]:   { fill: 0x3a3a3a, stroke: 0x2a2a2a },
    [TILE_TYPES.WALL]:   { fill: 0x555566, stroke: 0x444455 },
    [TILE_TYPES.FLOOR]:  { fill: 0x4a4035, stroke: 0x3a3025 },
    [TILE_TYPES.LOOT]:   { fill: 0x4a4035, stroke: 0x6a5a30, icon: '📦' },
    [TILE_TYPES.SAFE]:   { fill: 0x2a4a2a, stroke: 0x3a6a3a, icon: '🏠' },
    [TILE_TYPES.ESCAPE]: { fill: 0x2a2a5a, stroke: 0x4a4a8a, icon: '🚪' },
    [TILE_TYPES.RUBBLE]: { fill: 0x3a3a3a, stroke: 0x2a2a2a },
    [TILE_TYPES.DOOR]:   { fill: 0x5a4a30, stroke: 0x7a6a40 },
};

// Which tiles are walkable
function isWalkable(tileType) {
    return tileType !== TILE_TYPES.WALL;
}
