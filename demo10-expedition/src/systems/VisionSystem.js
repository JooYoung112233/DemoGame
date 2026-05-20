// Vision / Fog of War system
// Day: circular vision (radius 6 tiles)
// Night: fan-shaped cone (7 tiles, 90 degrees) in movement direction

class VisionSystem {
    constructor() {
        this.dayRadius = 7;      // tiles
        this.nightRadius = 7;    // tiles
        this.nightAngle = Math.PI / 2;  // 90 degrees
        this.nightBackRadius = 2; // small area behind player at night
        this.revealedTiles = new Set(); // permanently revealed (explored)
        this.visibleTiles = new Set();  // currently visible
    }

    update(playerRow, playerCol, facingAngle, isNight, nightAlpha) {
        this.visibleTiles.clear();

        if (isNight && nightAlpha > 0.5) {
            // Night: cone vision
            this._calculateConeVision(playerRow, playerCol, facingAngle);
            // Plus small circle behind
            this._calculateCircleVision(playerRow, playerCol, this.nightBackRadius);
        } else {
            // Day: full circle
            this._calculateCircleVision(playerRow, playerCol, this.dayRadius);
        }

        // Add all visible to revealed
        for (const key of this.visibleTiles) {
            this.revealedTiles.add(key);
        }
    }

    _calculateCircleVision(row, col, radius) {
        const r2 = radius * radius;
        for (let dr = -radius; dr <= radius; dr++) {
            for (let dc = -radius; dc <= radius; dc++) {
                if (dr * dr + dc * dc <= r2) {
                    const tr = row + dr;
                    const tc = col + dc;
                    if (tr >= 0 && tr < MAP_HEIGHT && tc >= 0 && tc < MAP_WIDTH) {
                        // Simple line-of-sight check
                        if (this._hasLineOfSight(row, col, tr, tc)) {
                            this.visibleTiles.add(`${tr},${tc}`);
                        }
                    }
                }
            }
        }
    }

    _calculateConeVision(row, col, facingAngle) {
        const radius = this.nightRadius;
        const halfAngle = this.nightAngle / 2;

        for (let dr = -radius; dr <= radius; dr++) {
            for (let dc = -radius; dc <= radius; dc++) {
                const dist = Math.sqrt(dr * dr + dc * dc);
                if (dist > radius) continue;

                const angle = Math.atan2(dr, dc);
                let angleDiff = angle - facingAngle;
                // Normalize to [-PI, PI]
                while (angleDiff > Math.PI) angleDiff -= Math.PI * 2;
                while (angleDiff < -Math.PI) angleDiff += Math.PI * 2;

                if (Math.abs(angleDiff) <= halfAngle) {
                    const tr = row + dr;
                    const tc = col + dc;
                    if (tr >= 0 && tr < MAP_HEIGHT && tc >= 0 && tc < MAP_WIDTH) {
                        if (this._hasLineOfSight(row, col, tr, tc)) {
                            this.visibleTiles.add(`${tr},${tc}`);
                        }
                    }
                }
            }
        }
    }

    _hasLineOfSight(r1, c1, r2, c2) {
        // Bresenham line — blocked by walls
        const dr = Math.abs(r2 - r1);
        const dc = Math.abs(c2 - c1);
        const sr = r1 < r2 ? 1 : -1;
        const sc = c1 < c2 ? 1 : -1;
        let err = dr - dc;
        let r = r1, c = c1;

        while (true) {
            if (r === r2 && c === c2) return true;
            // Check if current tile blocks vision
            if (r !== r1 || c !== c1) {
                const tile = CITY_MAP[r] && CITY_MAP[r][c];
                if (tile === TILE_TYPES.WALL) return false;
            }
            const e2 = 2 * err;
            if (e2 > -dc) { err -= dc; r += sr; }
            if (e2 < dr) { err += dr; c += sc; }
        }
    }

    isVisible(row, col) {
        return this.visibleTiles.has(`${row},${col}`);
    }

    isRevealed(row, col) {
        return this.revealedTiles.has(`${row},${col}`);
    }

    // Get visibility alpha for a tile (1 = fully visible, 0.3 = revealed but not visible, 0 = unexplored)
    getAlpha(row, col) {
        if (this.isVisible(row, col)) return 1.0;
        if (this.isRevealed(row, col)) return 0.25;
        return 0;
    }

    reset() {
        this.revealedTiles.clear();
        this.visibleTiles.clear();
    }
}
