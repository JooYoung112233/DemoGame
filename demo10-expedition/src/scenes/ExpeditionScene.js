class ExpeditionScene extends Phaser.Scene {
    constructor() { super('ExpeditionScene'); }

    init(data) {
        this.zoneId = data.zone || 'suburbs';
        this.party = data.party || ['scout','fighter','medic','marksman'];
        this.inventory = data.inventory || [];
        this.stash = data.stash || [];
    }

    create() {
        this.TILE_SIZE = 32;
        this.zone = ZONE_DATA[this.zoneId];
        this.cameras.main.setBackgroundColor(this.zone.bgColor);
        this.cameras.main.fadeIn(400);

        const mapData = MapGenerator.generate(this.zone);
        this.mapGrid = mapData.grid;
        this.mapW = mapData.width;
        this.mapH = mapData.height;
        this.entities = mapData.entities;
        this.buildings = mapData.buildings;

        this.discovered = Array.from({ length: this.mapH }, () => Array(this.mapW).fill(false));
        this.searched = {};

        this.tileLayer = this.add.container(0, 0);
        this.entityLayer = this.add.container(0, 0);
        this.fogLayer = this.add.container(0, 0);
        this.uiLayer = this.add.container(0, 0).setScrollFactor(0).setDepth(100);

        this.drawMap();
        this.createPlayer();
        this.createEnemySprites();
        this.createExtractMarkers();
        this.createFog();
        this.revealAround(this.player.gx, this.player.gy, 5);

        this.setupCamera();
        this.setupInput();
        this.createUI();

        this.isMoving = false;
        this.enemiesDefeated = [];
        this.inventoryOpen = false;
        this.isSearching = false;
        this.fullMapOpen = false;
        this.searchPrompt = null;
        this.moveTimer = 0;
        this.MOVE_DELAY = 100;
        this.confirmingExit = false;
    }

    drawMap() {
        const T = this.TILE_SIZE;
        for (let y = 0; y < this.mapH; y++) {
            for (let x = 0; x < this.mapW; x++) {
                const tile = this.mapGrid[y][x];
                const tc = TILE_COLORS[tile] || TILE_COLORS[TILE.FLOOR];
                const g = this.add.graphics();
                const px = x * T, py = y * T;

                if (tile === TILE.WALL) {
                    g.fillStyle(tc.side, 1);
                    g.fillRect(px, py, T, T);
                    g.fillStyle(tc.top, 1);
                    g.fillRect(px, py, T, T - 4);
                    g.lineStyle(1, 0x6a6a7a, 0.2);
                    g.strokeRect(px, py, T, T - 4);
                } else if (tile === TILE.GRASS) {
                    const shade = (Phaser.Math.Between(0, 10) > 8) ? 0x2e3e2e : tc.top;
                    g.fillStyle(shade, 1);
                    g.fillRect(px, py, T, T);
                } else {
                    g.fillStyle(tc.top, 1);
                    g.fillRect(px, py, T, T);
                    if (tile !== TILE.ROAD && tile !== TILE.WATER) {
                        g.lineStyle(1, 0x4a4a5a, 0.1);
                        g.strokeRect(px, py, T, T);
                    }
                    if (tc.glow) {
                        g.fillStyle(tc.glow, 0.12);
                        g.fillCircle(px + T / 2, py + T / 2, T * 0.4);
                    }
                }

                if (tile === TILE.FURNITURE) {
                    g.fillStyle(0x5a4a3a, 0.8);
                    g.fillRect(px + 4, py + 4, T - 8, T - 8);
                }

                g.setDepth(0);
                this.tileLayer.add(g);

                const searchable = SEARCHABLE_TILES[tile];
                if (searchable) {
                    const icon = this.add.text(px + T / 2, py + T / 2, searchable.icon, { fontSize: '14px' }).setOrigin(0.5).setDepth(2);
                    this.tileLayer.add(icon);
                }
            }
        }
    }

    createPlayer() {
        const start = this.entities.start;
        const T = this.TILE_SIZE;
        this.player = { gx: start.x, gy: start.y, container: this.add.container(start.x * T + T / 2, start.y * T + T / 2) };

        const g = this.add.graphics();
        g.fillStyle(0x44aaff, 1); g.fillCircle(0, 0, 10);
        g.lineStyle(2, 0xffffff, 0.8); g.strokeCircle(0, 0, 10);
        const icon = this.add.text(0, 0, '🧭', { fontSize: '12px' }).setOrigin(0.5);
        this.player.container.add([g, icon]);
        this.player.container.setDepth(10);
        this.entityLayer.add(this.player.container);
    }

    createEnemySprites() {
        this.enemySprites = [];
        const T = this.TILE_SIZE;
        this.entities.enemies.forEach((e, i) => {
            const c = this.add.container(e.x * T + T / 2, e.y * T + T / 2);
            const g = this.add.graphics();
            g.fillStyle(0xcc4444, 0.8); g.fillCircle(0, 0, 8);
            g.lineStyle(1, 0xff6666, 0.5); g.strokeCircle(0, 0, 8);
            const type = ENEMY_DATA[e.encounter[0]]?.type || 'undead';
            const icon = this.add.text(0, 0, ENEMY_ICONS[type] || '💀', { fontSize: '10px' }).setOrigin(0.5);
            c.add([g, icon]); c.setDepth(9);
            c.enemyIndex = i; c.gx = e.x; c.gy = e.y;
            c.patrol = e.patrol; c.patrolDir = Math.floor(Math.random() * 4);
            c.encounter = e.encounter;
            this.entityLayer.add(c);
            this.enemySprites.push(c);
        });
    }

    createExtractMarkers() {
        const T = this.TILE_SIZE;
        this.entities.extracts.forEach(ext => {
            const t = this.add.text(ext.x * T + T / 2, ext.y * T + T / 2 - 14, `🚁 ${ext.name}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#4488ff', stroke: '#000', strokeThickness: 2
            }).setOrigin(0.5).setDepth(3);
            this.tileLayer.add(t);
        });
    }

    createFog() {
        const T = this.TILE_SIZE;
        this.fogTiles = [];
        for (let y = 0; y < this.mapH; y++) {
            this.fogTiles[y] = [];
            for (let x = 0; x < this.mapW; x++) {
                const g = this.add.graphics();
                g.fillStyle(0x000000, 0.85);
                g.fillRect(x * T, y * T, T, T);
                g.setDepth(50);
                this.fogLayer.add(g);
                this.fogTiles[y][x] = g;
            }
        }
    }

    revealAround(cx, cy, radius) {
        for (let dy = -radius; dy <= radius; dy++) {
            for (let dx = -radius; dx <= radius; dx++) {
                if (dx * dx + dy * dy > radius * radius) continue;
                const nx = cx + dx, ny = cy + dy;
                if (nx < 0 || ny < 0 || nx >= this.mapW || ny >= this.mapH) continue;
                if (!this.discovered[ny][nx]) {
                    this.discovered[ny][nx] = true;
                    this.tweens.add({ targets: this.fogTiles[ny][nx], alpha: 0, duration: 150 });
                }
            }
        }
    }

    setupCamera() {
        const T = this.TILE_SIZE;
        this.cameras.main.setBounds(0, 0, this.mapW * T, this.mapH * T);
        this.cameras.main.startFollow(this.player.container, true, 0.12, 0.12);
        this.cameras.main.setZoom(1.2);
    }

    setupInput() {
        if (this.game.canvas) {
            this.game.canvas.setAttribute('tabindex', '0');
            this.game.canvas.focus();
        }
        this.cursors = this.input.keyboard.createCursorKeys();
        this.wasd = { W: this.input.keyboard.addKey('W'), A: this.input.keyboard.addKey('A'), S: this.input.keyboard.addKey('S'), D: this.input.keyboard.addKey('D') };
        this.eKey = this.input.keyboard.addKey('E');
        this.tabKey = this.input.keyboard.addKey('TAB');
        this.mKey = this.input.keyboard.addKey('M');
        this.escKey = this.input.keyboard.addKey('ESC');

        this.input.on('pointerdown', () => {
            if (this.game.canvas) this.game.canvas.focus();
        });
    }

    createUI() {
        const cam = this.cameras.main;
        const w = cam.width, h = cam.height;
        const fs = Math.floor(h * 0.02);

        const topBg = this.add.graphics().setScrollFactor(0).setDepth(100);
        topBg.fillStyle(0x000000, 0.6);
        topBg.fillRoundedRect(10, 10, Math.floor(w * 0.2), Math.floor(h * 0.07), 8);
        this.uiLayer.add(topBg);

        this.add.text(20, 16, this.zone.name, { fontSize: `${Math.floor(fs * 1.1)}px`, fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold' }).setScrollFactor(0).setDepth(101);
        this.add.text(20, 16 + Math.floor(fs * 1.4), `난이도: ${'★'.repeat(this.zone.difficulty)}${'☆'.repeat(3 - this.zone.difficulty)}`, { fontSize: `${Math.floor(fs * 0.85)}px`, fontFamily: 'monospace', color: '#aaa' }).setScrollFactor(0).setDepth(101);

        const invBgW = Math.floor(w * 0.18);
        const invBg = this.add.graphics().setScrollFactor(0).setDepth(100);
        invBg.fillStyle(0x000000, 0.6);
        invBg.fillRoundedRect(w - invBgW - 10, 10, invBgW, Math.floor(h * 0.045), 8);
        this.uiLayer.add(invBg);

        this.inventoryText = this.add.text(w - invBgW, 18, '', { fontSize: `${fs}px`, fontFamily: 'monospace', color: '#ffcc44' }).setScrollFactor(0).setDepth(101);
        this.updateInventoryCount();

        const ctrlBg = this.add.graphics().setScrollFactor(0).setDepth(100);
        ctrlBg.fillStyle(0x000000, 0.5);
        ctrlBg.fillRoundedRect(10, h - Math.floor(h * 0.05), Math.floor(w * 0.45), Math.floor(h * 0.035), 6);
        this.uiLayer.add(ctrlBg);

        this.add.text(20, h - Math.floor(h * 0.045), 'WASD:이동  E:수색/탈출  TAB:인벤  M:전체맵  ESC:긴급철수', {
            fontSize: `${Math.floor(fs * 0.75)}px`, fontFamily: 'monospace', color: '#666'
        }).setScrollFactor(0).setDepth(101);

        this.msgText = this.add.text(w / 2, h - Math.floor(h * 0.09), '', {
            fontSize: `${Math.floor(fs * 1)}px`, fontFamily: 'monospace', color: '#fff', stroke: '#000', strokeThickness: 3, align: 'center'
        }).setOrigin(0.5).setScrollFactor(0).setDepth(101).setAlpha(0);

        this.searchPromptText = this.add.text(w / 2, h / 2 + Math.floor(h * 0.07), '', {
            fontSize: `${Math.floor(fs * 1.1)}px`, fontFamily: 'monospace', color: '#ffcc44', stroke: '#000', strokeThickness: 3, align: 'center'
        }).setOrigin(0.5).setScrollFactor(0).setDepth(150).setAlpha(0);

        this.drawMinimap();
    }

    updateInventoryCount() {
        this.inventoryText.setText(`🎒 ${this.inventory.length}/${MAX_INVENTORY}  💰 ${this.getInventoryValue()}`);
    }

    getInventoryValue() {
        return this.inventory.reduce((s, id) => s + (ITEM_DATA[id]?.value || 0), 0);
    }

    drawMinimap() {
        const cam = this.cameras.main;
        const maxMmW = Math.floor(cam.width * 0.15);
        const mmSize = Math.max(1, Math.floor(maxMmW / this.mapW));
        const mmX = cam.width - this.mapW * mmSize - 12;
        const mmY = Math.floor(cam.height * 0.08);

        const bg = this.add.graphics().setScrollFactor(0).setDepth(100);
        bg.fillStyle(0x000000, 0.7);
        bg.fillRoundedRect(mmX - 4, mmY - 4, this.mapW * mmSize + 8, this.mapH * mmSize + 8, 4);
        this.uiLayer.add(bg);

        this.minimapGfx = this.add.graphics().setScrollFactor(0).setDepth(101);
        this.minimapData = { x: mmX, y: mmY, size: mmSize };
        this.updateMinimap();
    }

    updateMinimap() {
        const { x: mmX, y: mmY, size: s } = this.minimapData;
        this.minimapGfx.clear();
        for (let y = 0; y < this.mapH; y++) {
            for (let x = 0; x < this.mapW; x++) {
                if (!this.discovered[y][x]) continue;
                const tile = this.mapGrid[y][x];
                let color = 0x333333;
                if (tile === TILE.FLOOR || tile === TILE.DOOR) color = 0x555566;
                else if (tile === TILE.WALL) color = 0x444455;
                else if (tile === TILE.ROAD) color = 0x666666;
                else if (tile === TILE.GRASS) color = 0x334433;
                else if (tile === TILE.EXIT) color = 0x4488ff;
                else if (tile >= TILE.BARREL) color = 0x886644;
                this.minimapGfx.fillStyle(color, 0.7);
                this.minimapGfx.fillRect(mmX + x * s, mmY + y * s, s, s);
            }
        }
        this.enemySprites.forEach(es => {
            if (!es.visible || !this.discovered[es.gy]?.[es.gx]) return;
            this.minimapGfx.fillStyle(0xff4444, 1);
            this.minimapGfx.fillRect(mmX + es.gx * s, mmY + es.gy * s, s + 1, s + 1);
        });
        this.minimapGfx.fillStyle(0x44aaff, 1);
        this.minimapGfx.fillRect(mmX + this.player.gx * s, mmY + this.player.gy * s, s + 1, s + 1);
    }

    showMessage(msg, duration = 2500) {
        this.msgText.setText(msg).setAlpha(1);
        this.tweens.killTweensOf(this.msgText);
        this.tweens.add({ targets: this.msgText, alpha: 0, delay: duration - 400, duration: 400 });
    }

    update(time) {
        if (Phaser.Input.Keyboard.JustDown(this.eKey)) {
            if (this.isSearching) {
                this.closeLootPopup();
            } else if (!this.inventoryOpen && !this.fullMapOpen && !this.isMoving) {
                this.interact();
            }
            return;
        }
        if (Phaser.Input.Keyboard.JustDown(this.tabKey)) {
            if (!this.isSearching) this.toggleInventory();
            return;
        }
        if (Phaser.Input.Keyboard.JustDown(this.escKey)) {
            if (this.isSearching) { this.closeLootPopup(); return; }
            if (this.inventoryOpen) { this.toggleInventory(); return; }
            if (this.fullMapOpen) { this.toggleFullMap(); return; }
            if (this.confirmingExit) {
                const exitData = { inventory: [], stash: this.stash, party: this.party, safe: false };
                this.cameras.main.fadeOut(400, 0, 0, 0);
                this.time.delayedCall(450, () => {
                    game.scene.stop('ExpeditionScene');
                    game.scene.start('SafeHouseScene', exitData);
                });
            } else {
                this.confirmExit();
            }
            return;
        }
        if (Phaser.Input.Keyboard.JustDown(this.mKey)) {
            if (!this.isSearching && !this.inventoryOpen) this.toggleFullMap();
            return;
        }

        if (this.isMoving || this.inventoryOpen || this.fullMapOpen || this.isSearching) return;
        if (time - this.moveTimer < this.MOVE_DELAY) return;

        let dx = 0, dy = 0;
        if (this.cursors.left.isDown || this.wasd.A.isDown) dx = -1;
        else if (this.cursors.right.isDown || this.wasd.D.isDown) dx = 1;
        else if (this.cursors.up.isDown || this.wasd.W.isDown) dy = -1;
        else if (this.cursors.down.isDown || this.wasd.S.isDown) dy = 1;

        if (dx !== 0 || dy !== 0) {
            this.moveTimer = time;
            this.tryMove(dx, dy);
        }
    }

    tryMove(dx, dy) {
        const nx = this.player.gx + dx, ny = this.player.gy + dy;
        if (nx < 0 || ny < 0 || nx >= this.mapW || ny >= this.mapH) return;
        const tile = this.mapGrid[ny][nx];
        if (tile === TILE.WALL || tile === TILE.WATER || tile === TILE.FURNITURE ||
            tile === TILE.BARREL || tile === TILE.LOCKER || tile === TILE.CRATE ||
            tile === TILE.MEDICAL || tile === TILE.COMPUTER) return;

        this.isMoving = true;
        const T = this.TILE_SIZE;
        this.player.gx = nx; this.player.gy = ny;

        this.tweens.add({
            targets: this.player.container,
            x: nx * T + T / 2, y: ny * T + T / 2,
            duration: 80, ease: 'Linear',
            onComplete: () => {
                this.isMoving = false;
                this.revealAround(nx, ny, 5);
                this.checkTileEvents(nx, ny);
                this.updateMinimap();
                this.moveEnemies();
            }
        });
    }

    moveEnemies() {
        const dirs = [[0,-1],[0,1],[-1,0],[1,0]];
        this.enemySprites.forEach(es => {
            if (!es.visible || !es.patrol) return;
            if (Math.random() > 0.3) return;
            if (Math.random() < 0.15) es.patrolDir = Math.floor(Math.random() * 4);
            const [ddx, ddy] = dirs[es.patrolDir];
            const nx = es.gx + ddx, ny = es.gy + ddy;
            if (nx < 1 || ny < 1 || nx >= this.mapW - 1 || ny >= this.mapH - 1) { es.patrolDir = (es.patrolDir + 1) % 4; return; }
            const tile = this.mapGrid[ny][nx];
            if (tile === TILE.WALL || tile === TILE.WATER || tile === TILE.FURNITURE || tile === TILE.EXIT) { es.patrolDir = (es.patrolDir + 1) % 4; return; }
            es.gx = nx; es.gy = ny;
            const T = this.TILE_SIZE;
            this.tweens.add({ targets: es, x: nx * T + T / 2, y: ny * T + T / 2, duration: 200 });
        });
    }

    checkTileEvents(x, y) {
        const tile = this.mapGrid[y][x];

        if (tile === TILE.EXIT) {
            this.showMessage(`🚁 탈출 지점! E키로 탈출`);
        }

        let foundSearchable = false;
        const searchable = SEARCHABLE_TILES[tile];
        if (searchable && !this.searched[`${x},${y}`]) {
            this.searchPromptText.setText(`E키: ${searchable.icon} ${searchable.name} 수색`).setAlpha(1);
            foundSearchable = true;
        }

        if (!foundSearchable) {
            const dirs = [[0,-1],[0,1],[-1,0],[1,0]];
            for (const [dx, dy] of dirs) {
                const nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= this.mapW || ny >= this.mapH) continue;
                const adjTile = this.mapGrid[ny][nx];
                const adjSearch = SEARCHABLE_TILES[adjTile];
                if (adjSearch && !this.searched[`${nx},${ny}`]) {
                    this.searchPromptText.setText(`E키: ${adjSearch.icon} ${adjSearch.name} 수색`).setAlpha(1);
                    foundSearchable = true;
                    break;
                }
                if (adjTile === TILE.EXIT) {
                    this.searchPromptText.setText(`E키: 🚁 탈출`).setAlpha(1);
                    foundSearchable = true;
                    break;
                }
            }
        }

        if (!foundSearchable) {
            this.searchPromptText.setAlpha(0);
        }

        const enemy = this.enemySprites.find(e => e.visible && Math.abs(e.gx - x) + Math.abs(e.gy - y) <= 2);
        if (enemy) this.startBattle(enemy);
    }

    interact() {
        const x = this.player.gx, y = this.player.gy;
        const tile = this.mapGrid[y][x];

        if (tile === TILE.EXIT) {
            this.extractSuccess();
            return;
        }

        const searchable = SEARCHABLE_TILES[tile];
        if (searchable && !this.searched[`${x},${y}`]) {
            this.searchContainer(x, y, searchable);
            return;
        }

        const dirs = [[0,-1],[0,1],[-1,0],[1,0]];
        for (const [dx, dy] of dirs) {
            const nx = x + dx, ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= this.mapW || ny >= this.mapH) continue;
            const adjTile = this.mapGrid[ny][nx];
            const adjSearch = SEARCHABLE_TILES[adjTile];
            if (adjSearch && !this.searched[`${nx},${ny}`]) {
                this.searchContainer(nx, ny, adjSearch);
                return;
            }
            if (adjTile === TILE.EXIT) {
                this.extractSuccess();
                return;
            }
        }

        this.showMessage('주변에 수색할 것이 없다', 1500);
    }

    searchContainer(x, y, searchable) {
        this.searched[`${x},${y}`] = true;
        this.searchPromptText.setAlpha(0);

        const lootTable = LOOT_TABLE[searchable.lootPool] || LOOT_TABLE.common_crate;
        const found = [];
        lootTable.forEach(entry => {
            if (Math.random() < entry.chance) {
                const count = entry.min + Math.floor(Math.random() * (entry.max - entry.min + 1));
                for (let i = 0; i < count; i++) found.push(entry.item);
            }
        });

        if (found.length === 0) {
            this.showLootPopup(searchable, [], 0);
            return;
        }

        let added = 0;
        found.forEach(id => {
            if (this.inventory.length < MAX_INVENTORY) { this.inventory.push(id); added++; }
        });

        this.showLootPopup(searchable, found, found.length - added);
        this.updateInventoryCount();
    }

    showLootPopup(searchable, found, overflow) {
        if (this.lootPopup) this.lootPopup.destroy();

        const cam = this.cameras.main;
        const w = cam.width, h = cam.height;
        const pw = Math.floor(w * 0.28);
        const lineH = Math.floor(h * 0.03);
        const headerH = Math.floor(h * 0.06);
        const footerH = Math.floor(h * 0.04);
        const ph = headerH + Math.max(found.length, 1) * lineH + footerH + 20;
        const px = Math.floor((w - pw) / 2);
        const py = Math.floor((h - ph) / 2);

        this.lootPopup = this.add.container(0, 0).setScrollFactor(0).setDepth(250);
        this.isSearching = true;

        const overlay = this.add.graphics().setScrollFactor(0);
        overlay.fillStyle(0x000000, 0.4);
        overlay.fillRect(0, 0, w, h);
        this.lootPopup.add(overlay);

        const bg = this.add.graphics().setScrollFactor(0);
        bg.fillStyle(0x111122, 0.95);
        bg.fillRoundedRect(px, py, pw, ph, 10);
        bg.lineStyle(2, found.length > 0 ? 0x44ff88 : 0x666666, 0.8);
        bg.strokeRoundedRect(px, py, pw, ph, 10);
        this.lootPopup.add(bg);

        const fs = Math.floor(h * 0.02);
        this.lootPopup.add(this.add.text(px + pw / 2, py + 12, `${searchable.icon} ${searchable.name} 수색`, {
            fontSize: `${Math.floor(fs * 1.1)}px`, fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5).setScrollFactor(0));

        if (found.length === 0) {
            this.lootPopup.add(this.add.text(px + pw / 2, py + headerH + 10, '비어있다...', {
                fontSize: `${fs}px`, fontFamily: 'monospace', color: '#666'
            }).setOrigin(0.5).setScrollFactor(0));
        } else {
            const counts = {};
            found.forEach(id => { counts[id] = (counts[id] || 0) + 1; });
            let row = 0;
            Object.entries(counts).forEach(([id, count]) => {
                const item = ITEM_DATA[id];
                if (!item) return;
                const iy = py + headerH + row * lineH;
                this.lootPopup.add(this.add.text(px + 15, iy, `${item.icon} ${item.name} x${count}`, {
                    fontSize: `${Math.floor(fs * 0.9)}px`, fontFamily: 'monospace', color: '#44ff88'
                }).setScrollFactor(0));
                this.lootPopup.add(this.add.text(px + pw - 15, iy, `${item.value * count}G`, {
                    fontSize: `${Math.floor(fs * 0.8)}px`, fontFamily: 'monospace', color: '#ffcc44'
                }).setOrigin(1, 0).setScrollFactor(0));
                row++;
            });

            if (overflow > 0) {
                this.lootPopup.add(this.add.text(px + pw / 2, py + headerH + row * lineH + 4, `⚠ ${overflow}개 초과 — 인벤토리 가득!`, {
                    fontSize: `${Math.floor(fs * 0.75)}px`, fontFamily: 'monospace', color: '#ff6644'
                }).setOrigin(0.5).setScrollFactor(0));
            }
        }

        const closeText = this.add.text(px + pw / 2, py + ph - footerH + 2, '[ E / ESC / 클릭: 닫기 ]', {
            fontSize: `${Math.floor(fs * 0.8)}px`, fontFamily: 'monospace', color: '#888'
        }).setOrigin(0.5).setScrollFactor(0);
        this.lootPopup.add(closeText);

        overlay.setInteractive(new Phaser.Geom.Rectangle(0, 0, w, h), Phaser.Geom.Rectangle.Contains);
        overlay.on('pointerdown', () => this.closeLootPopup());
    }

    closeLootPopup() {
        if (this.lootPopup) {
            this.lootPopup.destroy();
            this.lootPopup = null;
            this.isSearching = false;
        }
    }

    startBattle(enemySprite) {
        enemySprite.setVisible(false);
        this.enemiesDefeated.push(enemySprite.enemyIndex);

        const battleData = {
            party: this.party,
            enemies: enemySprite.encounter,
            zone: this.zoneId,
            returnData: {
                zone: this.zoneId, party: this.party, inventory: this.inventory, stash: this.stash
            }
        };
        this.cameras.main.fadeOut(400, 0, 0, 0);
        this.time.delayedCall(450, () => {
            game.scene.stop('ExpeditionScene');
            game.scene.start('BattleScene', battleData);
        });
    }

    extractSuccess() {
        this.showMessage('✅ 탈출 성공! 보급품을 가지고 안전가옥으로 귀환합니다.');
        const extractData = {
            inventory: [], stash: [...this.stash, ...this.inventory], party: this.party, safe: true,
            lootValue: this.getInventoryValue(), extracted: true
        };
        this.cameras.main.fadeOut(600, 0, 0, 0);
        this.time.delayedCall(650, () => {
            game.scene.stop('ExpeditionScene');
            game.scene.start('SafeHouseScene', extractData);
        });
    }

    confirmExit() {
        this.showMessage('⚠️ ESC 한번 더 = 긴급 철수 (전리품 전부 손실!)');
        this.confirmingExit = true;
        this.time.delayedCall(3000, () => {
            this.confirmingExit = false;
        });
    }

    toggleInventory() {
        if (this.inventoryOpen) {
            this.inventoryPanel.destroy();
            this.inventoryPanel = null;
            this.inventoryOpen = false;
            return;
        }
        this.inventoryOpen = true;
        const cam = this.cameras.main;
        const pw = Math.floor(cam.width * 0.3);
        const ph = Math.floor(cam.height * 0.7);
        const px = Math.floor((cam.width - pw) / 2);
        const py = Math.floor((cam.height - ph) / 2);

        this.inventoryPanel = this.add.container(0, 0).setScrollFactor(0).setDepth(200);

        const overlay = this.add.graphics().setScrollFactor(0);
        overlay.fillStyle(0x000000, 0.6);
        overlay.fillRect(0, 0, cam.width, cam.height);
        this.inventoryPanel.add(overlay);

        const bg = this.add.graphics().setScrollFactor(0);
        bg.fillStyle(0x111122, 0.95);
        bg.fillRoundedRect(px, py, pw, ph, 12);
        bg.lineStyle(1, 0x444466, 0.6);
        bg.strokeRoundedRect(px, py, pw, ph, 12);
        this.inventoryPanel.add(bg);

        const ifs = Math.floor(cam.height * 0.022);
        const rowH = Math.floor(ifs * 1.8);

        this.inventoryPanel.add(this.add.text(px + pw / 2, py + 15, `🎒 인벤토리 (${this.inventory.length}/${MAX_INVENTORY})`, {
            fontSize: `${Math.floor(ifs * 1.2)}px`, fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5).setScrollFactor(0));

        const totalWeight = this.inventory.reduce((s, id) => s + (ITEM_DATA[id]?.weight || 0), 0);
        const totalValue = this.getInventoryValue();
        this.inventoryPanel.add(this.add.text(px + pw / 2, py + 38, `무게: ${totalWeight.toFixed(1)}kg  |  가치: ${totalValue}G`, {
            fontSize: `${Math.floor(ifs * 0.85)}px`, fontFamily: 'monospace', color: '#888'
        }).setOrigin(0.5).setScrollFactor(0));

        const counts = {};
        this.inventory.forEach(id => { counts[id] = (counts[id] || 0) + 1; });

        const sorted = Object.entries(counts).sort((a, b) => {
            const ta = ITEM_DATA[a[0]]?.type || '', tb = ITEM_DATA[b[0]]?.type || '';
            return ta.localeCompare(tb) || a[0].localeCompare(b[0]);
        });

        let row = 0;
        let lastType = '';
        sorted.forEach(([id, count]) => {
            const item = ITEM_DATA[id];
            if (!item) return;
            if (item.type !== lastType) {
                lastType = item.type;
                const typeNames = { material: '재료', consumable: '소모품', ammo: '탄약', equipment: '장비', valuable: '귀중품', key: '열쇠' };
                this.inventoryPanel.add(this.add.text(px + 15, py + 60 + row * rowH, `── ${typeNames[item.type] || item.type} ──`, {
                    fontSize: `${Math.floor(ifs * 0.75)}px`, fontFamily: 'monospace', color: '#666'
                }).setScrollFactor(0));
                row++;
            }
            this.inventoryPanel.add(this.add.text(px + 15, py + 60 + row * rowH,
                `${item.icon} ${item.name} x${count}  (${item.value}G)`, {
                fontSize: `${Math.floor(ifs * 0.85)}px`, fontFamily: 'monospace', color: '#ccc'
            }).setScrollFactor(0));
            row++;
        });

        if (this.inventory.length === 0) {
            this.inventoryPanel.add(this.add.text(px + pw / 2, py + 80, '비어있음', {
                fontSize: `${ifs}px`, fontFamily: 'monospace', color: '#555'
            }).setOrigin(0.5).setScrollFactor(0));
        }

        this.inventoryPanel.add(this.add.text(px + pw / 2, py + ph - 20, 'TAB / ESC: 닫기', {
            fontSize: `${Math.floor(ifs * 0.75)}px`, fontFamily: 'monospace', color: '#666'
        }).setOrigin(0.5).setScrollFactor(0));
    }

    toggleFullMap() {
        if (this.fullMapOpen) {
            this.fullMapPanel.destroy();
            this.fullMapPanel = null;
            this.fullMapOpen = false;
            return;
        }
        this.fullMapOpen = true;
        const cam = this.cameras.main;
        this.fullMapPanel = this.add.container(0, 0).setScrollFactor(0).setDepth(200);

        const overlay = this.add.graphics().setScrollFactor(0);
        overlay.fillStyle(0x000000, 0.8);
        overlay.fillRect(0, 0, cam.width, cam.height);
        this.fullMapPanel.add(overlay);

        const scale = Math.min((cam.width - 40) / this.mapW, (cam.height - 80) / this.mapH);
        const ox = (cam.width - this.mapW * scale) / 2;
        const oy = (cam.height - this.mapH * scale) / 2 + 15;

        const mg = this.add.graphics().setScrollFactor(0);
        for (let y = 0; y < this.mapH; y++) for (let x = 0; x < this.mapW; x++) {
            if (!this.discovered[y][x]) continue;
            const tile = this.mapGrid[y][x];
            let c = 0x333333;
            if (tile === TILE.FLOOR || tile === TILE.DOOR) c = 0x666677;
            else if (tile === TILE.WALL) c = 0x555566;
            else if (tile === TILE.ROAD) c = 0x777777;
            else if (tile === TILE.GRASS) c = 0x445544;
            else if (tile === TILE.EXIT) c = 0x4488ff;
            else if (tile >= TILE.BARREL) c = 0xaa8855;
            mg.fillStyle(c, 0.9);
            mg.fillRect(ox + x * scale, oy + y * scale, Math.ceil(scale), Math.ceil(scale));
        }
        this.enemySprites.forEach(es => {
            if (!es.visible || !this.discovered[es.gy]?.[es.gx]) return;
            mg.fillStyle(0xff4444, 1);
            mg.fillCircle(ox + es.gx * scale + scale / 2, oy + es.gy * scale + scale / 2, Math.max(2, scale));
        });
        mg.fillStyle(0x44aaff, 1);
        mg.fillCircle(ox + this.player.gx * scale + scale / 2, oy + this.player.gy * scale + scale / 2, Math.max(3, scale * 1.5));
        this.fullMapPanel.add(mg);

        this.fullMapPanel.add(this.add.text(cam.width / 2, 15, `🗺 ${this.zone.name}  전체 맵`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#fff'
        }).setOrigin(0.5).setScrollFactor(0));
        this.fullMapPanel.add(this.add.text(cam.width / 2, cam.height - 15, 'M: 닫기', {
            fontSize: '10px', fontFamily: 'monospace', color: '#666'
        }).setOrigin(0.5).setScrollFactor(0));

    }
}
