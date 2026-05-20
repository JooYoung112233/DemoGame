class MapGenerator {
    static generate(zoneData) {
        const W = zoneData.mapWidth;
        const H = zoneData.mapHeight;
        const grid = Array.from({ length: H }, () => Array(W).fill(TILE.GRASS));

        this.generateRoads(grid, W, H);
        const buildings = this.generateBuildings(grid, W, H, zoneData.buildingCount);
        this.connectBuildings(grid, buildings, W, H);
        this.validateBuildingDoors(grid, buildings);
        this.populateBuildings(grid, buildings, zoneData.lootDensity);
        this.addOutdoorDetails(grid, W, H, zoneData.outdoorRatio);

        const entities = { enemies: [], extracts: [], start: null };

        // Start position: center of first building
        const startRoom = buildings[0];
        entities.start = { x: startRoom.cx, y: startRoom.cy };
        grid[startRoom.cy][startRoom.cx] = TILE.FLOOR;

        // Extract points at map edges, hidden until player moves 500 tiles
        this.placeExtracts(grid, W, H, zoneData.extractCount, entities);

        // Enemies: pick single enemyId per spawn (real-time 1v1)
        const eCount = zoneData.enemyCount.min + Math.floor(Math.random() * (zoneData.enemyCount.max - zoneData.enemyCount.min + 1));
        const encounterTypes = ENCOUNTER_TABLE[zoneData.encounterType] || ENCOUNTER_TABLE.suburbs;
        for (let i = 0; i < eCount; i++) {
            const pos = this.randomOpenTile(grid, W, H, entities.start, 10);
            if (pos) {
                const enc = this.weightedRandom(encounterTypes);
                const enemyId = enc.enemies[Math.floor(Math.random() * enc.enemies.length)];
                entities.enemies.push({ x: pos.x, y: pos.y, enemyId, patrol: Math.random() < 0.4 });
            }
        }

        return { grid, buildings, entities, width: W, height: H };
    }

    // ── Roads ──────────────────────────────────────────────

    static generateRoads(grid, W, H) {
        const roadCount = 2 + Math.floor(Math.random() * 3);
        for (let r = 0; r < roadCount; r++) {
            const horizontal = Math.random() < 0.5;
            if (horizontal) {
                const y = 8 + Math.floor(Math.random() * (H - 16));
                const roadW = 2 + Math.floor(Math.random() * 2);
                for (let x = 0; x < W; x++) {
                    for (let dy = 0; dy < roadW; dy++) {
                        if (y + dy < H) grid[y + dy][x] = TILE.ROAD;
                    }
                }
            } else {
                const x = 8 + Math.floor(Math.random() * (W - 16));
                const roadW = 2 + Math.floor(Math.random() * 2);
                for (let y = 0; y < H; y++) {
                    for (let dx = 0; dx < roadW; dx++) {
                        if (x + dx < W) grid[y][x + dx] = TILE.ROAD;
                    }
                }
            }
        }
    }

    // ── Buildings ──────────────────────────────────────────

    static generateBuildings(grid, mapW, mapH, countRange) {
        const count = countRange.min + Math.floor(Math.random() * (countRange.max - countRange.min + 1));
        const buildings = [];
        let attempts = 0;
        while (buildings.length < count && attempts < 500) {
            attempts++;
            const w = 5 + Math.floor(Math.random() * 8);  // 5..12
            const h = 5 + Math.floor(Math.random() * 6);  // 5..10
            const x = 2 + Math.floor(Math.random() * (mapW - w - 4));
            const y = 2 + Math.floor(Math.random() * (mapH - h - 4));
            const room = { x, y, w, h, cx: Math.floor(x + w / 2), cy: Math.floor(y + h / 2) };

            // Ensure 2-tile gap between buildings
            const overlap = buildings.some(r =>
                room.x - 2 < r.x + r.w && room.x + room.w + 2 > r.x &&
                room.y - 2 < r.y + r.h && room.y + room.h + 2 > r.y
            );
            if (!overlap) {
                this.carveBuilding(grid, room);
                buildings.push(room);
            }
        }
        return buildings;
    }

    /**
     * Carve a proper rectangular building with walls, floor, and doors on all 4 sides.
     * Optionally adds internal walls for larger buildings.
     */
    static carveBuilding(grid, room) {
        // Lay down walls and floor as a clean rectangle
        for (let y = room.y; y < room.y + room.h; y++) {
            for (let x = room.x; x < room.x + room.w; x++) {
                const isEdge = (y === room.y || y === room.y + room.h - 1 ||
                                x === room.x || x === room.x + room.w - 1);
                grid[y][x] = isEdge ? TILE.WALL : TILE.FLOOR;
            }
        }

        // Place one door on each of the 4 sides (at center of each wall)
        // Top wall
        grid[room.y][room.cx] = TILE.DOOR;
        // Bottom wall
        grid[room.y + room.h - 1][room.cx] = TILE.DOOR;
        // Left wall
        grid[room.cy][room.x] = TILE.DOOR;
        // Right wall
        grid[room.cy][room.x + room.w - 1] = TILE.DOOR;

        // Internal walls for large buildings
        if (room.w >= 9) {
            const splitX = room.x + Math.floor(room.w / 2);
            for (let y = room.y + 1; y < room.y + room.h - 1; y++) {
                grid[y][splitX] = TILE.WALL;
            }
            grid[room.cy][splitX] = TILE.DOOR;
        }
        if (room.h >= 9) {
            const splitY = room.y + Math.floor(room.h / 2);
            for (let x = room.x + 1; x < room.x + room.w - 1; x++) {
                grid[splitY][x] = TILE.WALL;
            }
            grid[splitY][room.cx] = TILE.DOOR;
        }
    }

    // ── Connect buildings with roads (walls become doors) ─

    static connectBuildings(grid, buildings, W, H) {
        for (let i = 1; i < buildings.length; i++) {
            const a = buildings[i - 1];
            const b = buildings[i];
            let cx = a.cx, cy = a.cy;

            // Horizontal segment
            while (cx !== b.cx) {
                if (cy >= 0 && cy < H && cx >= 0 && cx < W) {
                    const t = grid[cy][cx];
                    if (t === TILE.GRASS) grid[cy][cx] = TILE.ROAD;
                    else if (t === TILE.WALL) grid[cy][cx] = TILE.DOOR;
                }
                cx += cx < b.cx ? 1 : -1;
            }
            // Vertical segment
            while (cy !== b.cy) {
                if (cy >= 0 && cy < H && cx >= 0 && cx < W) {
                    const t = grid[cy][cx];
                    if (t === TILE.GRASS) grid[cy][cx] = TILE.ROAD;
                    else if (t === TILE.WALL) grid[cy][cx] = TILE.DOOR;
                }
                cy += cy < b.cy ? 1 : -1;
            }
        }
    }

    // ── Validation: every building must have >= 1 door per side ─

    static validateBuildingDoors(grid, buildings) {
        for (const room of buildings) {
            // Check top wall (y = room.y, x from room.x+1 to room.x+room.w-2)
            if (!this._hasDoorOnSegment(grid, room.x + 1, room.x + room.w - 2, room.y, true)) {
                grid[room.y][room.cx] = TILE.DOOR;
            }
            // Check bottom wall
            if (!this._hasDoorOnSegment(grid, room.x + 1, room.x + room.w - 2, room.y + room.h - 1, true)) {
                grid[room.y + room.h - 1][room.cx] = TILE.DOOR;
            }
            // Check left wall
            if (!this._hasDoorOnSegment(grid, room.y + 1, room.y + room.h - 2, room.x, false)) {
                grid[room.cy][room.x] = TILE.DOOR;
            }
            // Check right wall
            if (!this._hasDoorOnSegment(grid, room.y + 1, room.y + room.h - 2, room.x + room.w - 1, false)) {
                grid[room.cy][room.x + room.w - 1] = TILE.DOOR;
            }
        }
    }

    /**
     * Check if a wall segment has at least one door.
     * @param {boolean} horizontal - true: scan x from lo..hi at fixed y; false: scan y from lo..hi at fixed x
     */
    static _hasDoorOnSegment(grid, lo, hi, fixed, horizontal) {
        for (let i = lo; i <= hi; i++) {
            const tile = horizontal ? grid[fixed][i] : grid[i][fixed];
            if (tile === TILE.DOOR) return true;
        }
        return false;
    }

    // ── Populate buildings with furniture / searchables ────

    static populateBuildings(grid, buildings, density) {
        const searchables = [TILE.BARREL, TILE.LOCKER, TILE.CRATE, TILE.MEDICAL, TILE.COMPUTER];
        buildings.forEach(room => {
            for (let y = room.y + 1; y < room.y + room.h - 1; y++) {
                for (let x = room.x + 1; x < room.x + room.w - 1; x++) {
                    if (grid[y][x] !== TILE.FLOOR) continue;
                    if (x === room.cx && y === room.cy) continue;

                    const isEdge = (x === room.x + 1 || x === room.x + room.w - 2 ||
                                    y === room.y + 1 || y === room.y + room.h - 2);
                    if (isEdge && Math.random() < density * 0.3) {
                        grid[y][x] = TILE.FURNITURE;
                    } else if (isEdge && Math.random() < density * 0.2) {
                        grid[y][x] = searchables[Math.floor(Math.random() * searchables.length)];
                    }
                }
            }
        });
    }

    // ── Outdoor details ───────────────────────────────────

    static addOutdoorDetails(grid, W, H, ratio) {
        for (let y = 0; y < H; y++) {
            for (let x = 0; x < W; x++) {
                if (grid[y][x] !== TILE.GRASS) continue;
                const r = Math.random();
                if (r < 0.02) grid[y][x] = TILE.RUBBLE;
                else if (r < 0.025) grid[y][x] = TILE.WATER;
                else if (r < 0.04) grid[y][x] = TILE.BARREL;
                else if (r < 0.045) grid[y][x] = TILE.CRATE;
            }
        }
    }

    // ── Extract points (map edges, hidden) ────────────────

    static placeExtracts(grid, W, H, count, entities) {
        const edges = [
            { side: 'top',    genPos: () => ({ x: 5 + Math.floor(Math.random() * (W - 10)), y: 1 }) },
            { side: 'bottom', genPos: () => ({ x: 5 + Math.floor(Math.random() * (W - 10)), y: H - 2 }) },
            { side: 'left',   genPos: () => ({ x: 1, y: 5 + Math.floor(Math.random() * (H - 10)) }) },
            { side: 'right',  genPos: () => ({ x: W - 2, y: 5 + Math.floor(Math.random() * (H - 10)) }) }
        ];

        // Shuffle edges so extract placement varies
        for (let i = edges.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [edges[i], edges[j]] = [edges[j], edges[i]];
        }

        for (let i = 0; i < count; i++) {
            const edge = edges[i % edges.length];
            const pos = edge.genPos();
            const sx = Math.max(1, Math.min(W - 2, pos.x));
            const sy = Math.max(1, Math.min(H - 2, pos.y));

            grid[sy][sx] = TILE.EXIT;

            // Clear a small area around the extract so it's reachable
            for (let dy = -1; dy <= 1; dy++) {
                for (let dx = -1; dx <= 1; dx++) {
                    const nx = sx + dx, ny = sy + dy;
                    if (nx >= 0 && ny >= 0 && nx < W && ny < H && grid[ny][nx] === TILE.GRASS) {
                        grid[ny][nx] = TILE.ROAD;
                    }
                }
            }

            entities.extracts.push({
                x: sx,
                y: sy,
                name: `탈출구 ${String.fromCharCode(65 + i)}`,
                hidden: true
            });
        }
    }

    // ── Utility ───────────────────────────────────────────

    static randomOpenTile(grid, W, H, avoid, minDist) {
        for (let tries = 0; tries < 100; tries++) {
            const x = 3 + Math.floor(Math.random() * (W - 6));
            const y = 3 + Math.floor(Math.random() * (H - 6));
            const tile = grid[y][x];
            if (tile === TILE.WALL || tile === TILE.WATER || tile === TILE.FURNITURE || tile === TILE.EXIT) continue;
            if (avoid) {
                const dist = Math.abs(x - avoid.x) + Math.abs(y - avoid.y);
                if (dist < minDist) continue;
            }
            return { x, y };
        }
        return null;
    }

    static weightedRandom(table) {
        const total = table.reduce((s, e) => s + e.weight, 0);
        let roll = Math.random() * total, cum = 0;
        for (const entry of table) {
            cum += entry.weight;
            if (roll <= cum) return entry;
        }
        return table[0];
    }
}
