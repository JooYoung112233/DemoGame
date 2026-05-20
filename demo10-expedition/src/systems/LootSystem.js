// Loot interaction system
// Handles searching loot spots and transferring items

class LootSystem {
    constructor() {
        this.activeLoot = null;   // currently open loot container
        this.searchProgress = 0;  // 0-1 search progress
        this.searchTime = 1.5;    // seconds to search
        this.isSearching = false;
    }

    // Check if player is adjacent to a loot spot
    getNearbyLootSpot(playerRow, playerCol) {
        const dirs = [
            [0, 0], [-1, 0], [1, 0], [0, -1], [0, 1],
            [-1, -1], [-1, 1], [1, -1], [1, 1]
        ];
        for (const [dr, dc] of dirs) {
            const r = playerRow + dr;
            const c = playerCol + dc;
            const key = `${r},${c}`;
            if (CITY_MAP[r] && CITY_MAP[r][c] === TILE_TYPES.LOOT && LOOT_SPOTS[key] && !LOOT_SPOTS[key].searched) {
                return { row: r, col: c, key, spot: LOOT_SPOTS[key] };
            }
        }
        return null;
    }

    // Start searching a loot spot
    startSearch(spot) {
        this.isSearching = true;
        this.searchProgress = 0;
        this.currentSpot = spot;
    }

    // Update search progress, returns items when complete
    updateSearch(delta) {
        if (!this.isSearching) return null;

        this.searchProgress += (delta / 1000) / this.searchTime;
        if (this.searchProgress >= 1) {
            this.isSearching = false;
            this.searchProgress = 0;

            // Generate loot
            const spot = this.currentSpot.spot;
            spot.searched = true;
            const items = generateLoot(spot.table, spot.items);
            this.activeLoot = items;
            return items;
        }
        return null;
    }

    cancelSearch() {
        this.isSearching = false;
        this.searchProgress = 0;
        this.currentSpot = null;
    }

    closeLoot() {
        this.activeLoot = null;
    }

    // Check if player is near escape point
    isNearEscape(playerRow, playerCol) {
        const dr = Math.abs(playerRow - ESCAPE_POINT.row);
        const dc = Math.abs(playerCol - ESCAPE_POINT.col);
        return dr <= 1 && dc <= 1;
    }

    // Check if player is in safe zone
    isInSafeZone(playerRow, playerCol) {
        const tile = CITY_MAP[playerRow] && CITY_MAP[playerRow][playerCol];
        return tile === TILE_TYPES.SAFE;
    }

    reset() {
        this.activeLoot = null;
        this.searchProgress = 0;
        this.isSearching = false;
        // Reset all loot spots
        for (const key of Object.keys(LOOT_SPOTS)) {
            LOOT_SPOTS[key].searched = false;
        }
    }
}
