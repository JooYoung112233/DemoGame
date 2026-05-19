class MapGenerator {
    static generate(zoneData) {
        const W = zoneData.mapWidth;
        const H = zoneData.mapHeight;
        const grid = Array.from({ length: H }, () => Array(W).fill(TILE.GRASS));

        this.generateRoads(grid, W, H);
        const buildings = this.generateBuildings(grid, W, H, zoneData.buildingCount);
        this.connectBuildings(grid, buildings, W, H);
        this.populateBuildings(grid, buildings, zoneData.lootDensity);
        this.addOutdoorDetails(grid, W, H, zoneData.outdoorRatio);

        const entities = { enemies: [], extracts: [], start: null };

        const startRoom = buildings[0];
        entities.start = { x: startRoom.cx, y: startRoom.cy };
        grid[startRoom.cy][startRoom.cx] = TILE.FLOOR;

        for (let i = 0; i < zoneData.extractCount; i++) {
            const ex = (i === 0) ? Math.floor(Math.random() * 10) + 2 : W - Math.floor(Math.random() * 10) - 3;
            const ey = (i === 0) ? Math.floor(Math.random() * 10) + 2 : H - Math.floor(Math.random() * 10) - 3;
            const sx = Math.max(2, Math.min(W - 3, ex));
            const sy = Math.max(2, Math.min(H - 3, ey));
            grid[sy][sx] = TILE.EXIT;
            for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) {
                const nx = sx + dx, ny = sy + dy;
                if (nx >= 0 && ny >= 0 && nx < W && ny < H && grid[ny][nx] === TILE.GRASS) grid[ny][nx] = TILE.ROAD;
            }
            entities.extracts.push({ x: sx, y: sy, name: `탈출구 ${String.fromCharCode(65 + i)}` });
        }

        const eCount = zoneData.enemyCount.min + Math.floor(Math.random() * (zoneData.enemyCount.max - zoneData.enemyCount.min + 1));
        const encounterTypes = ENCOUNTER_TABLE[zoneData.encounterType] || ENCOUNTER_TABLE.suburbs;
        for (let i = 0; i < eCount; i++) {
            const pos = this.randomOpenTile(grid, W, H, entities.start, 10);
            if (pos) {
                const enc = this.weightedRandom(encounterTypes);
                entities.enemies.push({ ...pos, encounter: enc.enemies, patrol: Math.random() < 0.4 });
            }
        }

        return { grid, buildings, entities, width: W, height: H };
    }

    static generateRoads(grid, W, H) {
        const roadCount = 2 + Math.floor(Math.random() * 3);
        for (let r = 0; r < roadCount; r++) {
            const horizontal = Math.random() < 0.5;
            if (horizontal) {
                const y = 8 + Math.floor(Math.random() * (H - 16));
                const roadW = 2 + Math.floor(Math.random() * 2);
                for (let x = 0; x < W; x++) for (let dy = 0; dy < roadW; dy++) {
                    if (y + dy < H) grid[y + dy][x] = TILE.ROAD;
                }
            } else {
                const x = 8 + Math.floor(Math.random() * (W - 16));
                const roadW = 2 + Math.floor(Math.random() * 2);
                for (let y = 0; y < H; y++) for (let dx = 0; dx < roadW; dx++) {
                    if (x + dx < W) grid[y][x + dx] = TILE.ROAD;
                }
            }
        }
    }

    static generateBuildings(grid, mapW, mapH, countRange) {
        const count = countRange.min + Math.floor(Math.random() * (countRange.max - countRange.min + 1));
        const buildings = [];
        let attempts = 0;
        while (buildings.length < count && attempts < 500) {
            attempts++;
            const w = 5 + Math.floor(Math.random() * 8);
            const h = 5 + Math.floor(Math.random() * 6);
            const x = 2 + Math.floor(Math.random() * (mapW - w - 4));
            const y = 2 + Math.floor(Math.random() * (mapH - h - 4));
            const room = { x, y, w, h, cx: Math.floor(x + w / 2), cy: Math.floor(y + h / 2) };

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

    static carveBuilding(grid, room) {
        for (let y = room.y; y < room.y + room.h; y++) {
            for (let x = room.x; x < room.x + room.w; x++) {
                if (y === room.y || y === room.y + room.h - 1 || x === room.x || x === room.x + room.w - 1) {
                    grid[y][x] = TILE.WALL;
                } else {
                    grid[y][x] = TILE.FLOOR;
                }
            }
        }
        const doorSide = Math.floor(Math.random() * 4);
        let dx, dy;
        if (doorSide === 0) { dx = room.cx; dy = room.y; }
        else if (doorSide === 1) { dx = room.cx; dy = room.y + room.h - 1; }
        else if (doorSide === 2) { dx = room.x; dy = room.cy; }
        else { dx = room.x + room.w - 1; dy = room.cy; }
        grid[dy][dx] = TILE.DOOR;

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

    static connectBuildings(grid, buildings, W, H) {
        for (let i = 1; i < buildings.length; i++) {
            const a = buildings[i - 1];
            const b = buildings[i];
            let cx = a.cx, cy = a.cy;
            while (cx !== b.cx) {
                if (cy >= 0 && cy < H && cx >= 0 && cx < W) {
                    if (grid[cy][cx] === TILE.GRASS) grid[cy][cx] = TILE.ROAD;
                }
                cx += cx < b.cx ? 1 : -1;
            }
            while (cy !== b.cy) {
                if (cy >= 0 && cy < H && cx >= 0 && cx < W) {
                    if (grid[cy][cx] === TILE.GRASS) grid[cy][cx] = TILE.ROAD;
                }
                cy += cy < b.cy ? 1 : -1;
            }
        }
    }

    static populateBuildings(grid, buildings, density) {
        const searchables = [TILE.BARREL, TILE.LOCKER, TILE.CRATE, TILE.MEDICAL, TILE.COMPUTER];
        buildings.forEach(room => {
            for (let y = room.y + 1; y < room.y + room.h - 1; y++) {
                for (let x = room.x + 1; x < room.x + room.w - 1; x++) {
                    if (grid[y][x] !== TILE.FLOOR) continue;
                    if (x === room.cx && y === room.cy) continue;

                    const isEdge = (x === room.x + 1 || x === room.x + room.w - 2 || y === room.y + 1 || y === room.y + room.h - 2);
                    if (isEdge && Math.random() < density * 0.3) {
                        grid[y][x] = TILE.FURNITURE;
                    } else if (isEdge && Math.random() < density * 0.2) {
                        grid[y][x] = searchables[Math.floor(Math.random() * searchables.length)];
                    }
                }
            }
        });
    }

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

    static generateBattleGrid(cols, rows) {
        const grid = Array.from({ length: rows }, () => Array(cols).fill(0));
        for (let y = 0; y < rows; y++) for (let x = 0; x < cols; x++) {
            if (Math.random() < 0.08) grid[y][x] = 1;
        }
        return grid;
    }
}
