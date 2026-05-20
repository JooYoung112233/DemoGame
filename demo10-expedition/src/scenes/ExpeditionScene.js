class ExpeditionScene extends Phaser.Scene {
    constructor() { super('ExpeditionScene'); }

    init(data) {
        this.zoneId = data.zone || 'suburbs';
        this.playerState = data.playerState || {
            hp: PLAYER_DATA.hp, maxHp: PLAYER_DATA.maxHp,
            atk: PLAYER_DATA.atk, def: PLAYER_DATA.def,
            attackRange: PLAYER_DATA.attackRange,
            attackCooldown: PLAYER_DATA.attackCooldown,
            equipment: PLAYER_DATA.equipment
        };
        this.inventory = data.inventory || [];
        this.stash = data.stash || [];
        this.gold = data.gold || 0;
    }

    // ── CREATE ─────────────────────────────────────────────

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
        this.revealAround(this.player.gx, this.player.gy, PLAYER_DATA.visionRadius);

        this.setupCamera();
        this.setupInput();
        this.createUI();

        // State flags
        this.inventoryOpen = false;
        this.isSearching = false;
        this.fullMapOpen = false;
        this.confirmingExit = false;
        this.playerDead = false;

        // Movement
        this.moveTimer = 0;
        this.MOVE_DELAY = 100;
        this.tilesMoved = 0;
        this.extractsRevealed = false;

        // Combat timing
        this.lastPlayerAttack = -99999;
        this.lastPlayerHitTime = 0;
        this.INVULN_MS = 500;
    }

    // ── MAP DRAWING ────────────────────────────────────────

    drawMap() {
        const T = this.TILE_SIZE;
        for (let y = 0; y < this.mapH; y++) {
            for (let x = 0; x < this.mapW; x++) {
                const tile = this.mapGrid[y][x];
                const tc = TILE_COLORS[tile] || TILE_COLORS[TILE.FLOOR];
                const g = this.add.graphics();
                const px = x * T, py = y * T;

                if (tile === TILE.WALL) {
                    g.fillStyle(tc.side, 1); g.fillRect(px, py, T, T);
                    g.fillStyle(tc.top, 1); g.fillRect(px, py, T, T - 4);
                    g.lineStyle(1, 0x6a6a7a, 0.2); g.strokeRect(px, py, T, T - 4);
                } else if (tile === TILE.GRASS) {
                    const shade = Phaser.Math.Between(0, 10) > 8 ? 0x2e3e2e : tc.top;
                    g.fillStyle(shade, 1); g.fillRect(px, py, T, T);
                } else {
                    g.fillStyle(tc.top, 1); g.fillRect(px, py, T, T);
                    if (tile !== TILE.ROAD && tile !== TILE.WATER) {
                        g.lineStyle(1, 0x4a4a5a, 0.1); g.strokeRect(px, py, T, T);
                    }
                    if (tc.glow) {
                        g.fillStyle(tc.glow, 0.12); g.fillCircle(px + T / 2, py + T / 2, T * 0.4);
                    }
                }

                if (tile === TILE.FURNITURE) {
                    g.fillStyle(0x5a4a3a, 0.8); g.fillRect(px + 4, py + 4, T - 8, T - 8);
                }

                g.setDepth(0);
                this.tileLayer.add(g);

                const searchable = SEARCHABLE_TILES[tile];
                if (searchable) {
                    const icon = this.add.text(px + T / 2, py + T / 2, searchable.icon, {
                        fontSize: '14px'
                    }).setOrigin(0.5).setDepth(2);
                    this.tileLayer.add(icon);
                }
            }
        }
    }

    // ── PLAYER ─────────────────────────────────────────────

    createPlayer() {
        const start = this.entities.start;
        const T = this.TILE_SIZE;
        const ps = this.playerState;

        this.player = {
            gx: start.x, gy: start.y,
            hp: ps.hp, maxHp: ps.maxHp,
            atk: ps.atk, def: ps.def,
            attackRange: ps.attackRange,
            attackCooldown: ps.attackCooldown,
            container: this.add.container(start.x * T + T / 2, start.y * T + T / 2)
        };

        // Body
        const body = this.add.graphics();
        body.fillStyle(PLAYER_DATA.color, 1);
        body.fillRect(-8, -8, 16, 16);
        body.lineStyle(2, 0xffffff, 0.8);
        body.strokeRect(-8, -8, 16, 16);

        // Name
        const name = this.add.text(0, -16, PLAYER_DATA.name, {
            fontSize: '9px', fontFamily: 'monospace', color: '#ffffff',
            stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5);

        // HP bar background + fill
        this.playerHpBg = this.add.graphics();
        this.playerHpBg.fillStyle(0x333333, 1);
        this.playerHpBg.fillRect(-10, 10, 20, 3);

        this.playerHpBar = this.add.graphics();
        this._drawPlayerHpBar();

        this.player.container.add([body, name, this.playerHpBg, this.playerHpBar]);
        this.player.container.setDepth(10);
        this.entityLayer.add(this.player.container);
    }

    _drawPlayerHpBar() {
        this.playerHpBar.clear();
        const ratio = Math.max(0, this.player.hp / this.player.maxHp);
        const color = ratio > 0.5 ? 0x44ff44 : ratio > 0.25 ? 0xffaa44 : 0xff4444;
        this.playerHpBar.fillStyle(color, 1);
        this.playerHpBar.fillRect(-10, 10, Math.floor(20 * ratio), 3);
    }

    // ── ENEMIES ────────────────────────────────────────────

    createEnemySprites() {
        this.enemies = [];
        const T = this.TILE_SIZE;

        this.entities.enemies.forEach((e, i) => {
            const data = ENEMY_DATA[e.enemyId];
            if (!data) return;

            const c = this.add.container(e.x * T + T / 2, e.y * T + T / 2);

            // Body
            const body = this.add.graphics();
            body.fillStyle(data.color, 0.9);
            body.fillRect(-7, -7, 14, 14);
            body.lineStyle(1, 0xff6666, 0.4);
            body.strokeRect(-7, -7, 14, 14);

            // Icon
            const typeIcon = ENEMY_ICONS[data.type] || '?';
            const icon = this.add.text(0, 0, typeIcon, { fontSize: '10px' }).setOrigin(0.5);

            // Name
            const nameText = this.add.text(0, -14, data.name, {
                fontSize: '7px', fontFamily: 'monospace', color: '#ff8888',
                stroke: '#000000', strokeThickness: 2
            }).setOrigin(0.5);

            // HP bar
            const hpBg = this.add.graphics();
            hpBg.fillStyle(0x333333, 1); hpBg.fillRect(-8, 9, 16, 2);
            const hpBar = this.add.graphics();
            hpBar.fillStyle(0xff4444, 1); hpBar.fillRect(-8, 9, 16, 2);

            // Alert indicator (hidden by default)
            const alert = this.add.text(0, -22, '!', {
                fontSize: '14px', fontFamily: 'monospace', fontStyle: 'bold',
                color: '#ff0000', stroke: '#000000', strokeThickness: 3
            }).setOrigin(0.5).setAlpha(0);

            c.add([body, icon, nameText, hpBg, hpBar, alert]);
            c.setDepth(9);

            const enemy = {
                container: c, index: i,
                gx: e.x, gy: e.y,
                data: { ...data },
                hp: data.hp, maxHp: data.maxHp,
                state: 'idle',
                patrol: e.patrol,
                patrolDir: Math.floor(Math.random() * 4),
                lastPatrolTime: -99999,
                lastChaseTime: -99999,
                lastAttackTime: -99999,
                alertStartTime: -99999,
                hpBar, alive: true
            };

            this.entityLayer.add(c);
            this.enemies.push(enemy);
        });
    }

    _updateEnemyHpBar(enemy) {
        enemy.hpBar.clear();
        const ratio = Math.max(0, enemy.hp / enemy.maxHp);
        enemy.hpBar.fillStyle(0xff4444, 1);
        enemy.hpBar.fillRect(-8, 9, Math.floor(16 * ratio), 2);
    }

    // ── EXTRACT MARKERS ────────────────────────────────────

    createExtractMarkers() {
        const T = this.TILE_SIZE;
        this.extractMarkers = [];
        this.entities.extracts.forEach(ext => {
            const t = this.add.text(ext.x * T + T / 2, ext.y * T + T / 2 - 14, `\u{1F681} ${ext.name}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#4488ff',
                stroke: '#000', strokeThickness: 2
            }).setOrigin(0.5).setDepth(3);
            t.setVisible(!ext.hidden);
            this.tileLayer.add(t);
            this.extractMarkers.push({ text: t, data: ext });
        });
    }

    // ── FOG OF WAR ─────────────────────────────────────────

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

    // ── CAMERA ─────────────────────────────────────────────

    setupCamera() {
        const T = this.TILE_SIZE;
        this.cameras.main.setBounds(0, 0, this.mapW * T, this.mapH * T);
        this.cameras.main.startFollow(this.player.container, true, 0.12, 0.12);
        this.cameras.main.setZoom(1.2);
    }

    // ── INPUT ──────────────────────────────────────────────

    setupInput() {
        if (this.game.canvas) {
            this.game.canvas.setAttribute('tabindex', '0');
            this.game.canvas.focus();
        }
        this.cursors = this.input.keyboard.createCursorKeys();
        this.wasd = {
            W: this.input.keyboard.addKey('W'),
            A: this.input.keyboard.addKey('A'),
            S: this.input.keyboard.addKey('S'),
            D: this.input.keyboard.addKey('D')
        };
        this.spaceKey = this.input.keyboard.addKey('SPACE');
        this.eKey = this.input.keyboard.addKey('E');
        this.tabKey = this.input.keyboard.addKey('TAB');
        this.mKey = this.input.keyboard.addKey('M');
        this.escKey = this.input.keyboard.addKey('ESC');

        this.input.on('pointerdown', (pointer) => {
            if (this.game.canvas) this.game.canvas.focus();
            if (!this.isSearching && !this.inventoryOpen && !this.fullMapOpen && !this.playerDead) {
                this.playerAttack();
            }
        });
    }

    // ── UI ─────────────────────────────────────────────────

    createUI() {
        const cam = this.cameras.main;
        const w = cam.width, h = cam.height;
        const fs = Math.floor(h * 0.02);

        // Top-left: zone info
        const topBg = this.add.graphics().setScrollFactor(0).setDepth(100);
        topBg.fillStyle(0x000000, 0.6);
        topBg.fillRoundedRect(10, 10, Math.floor(w * 0.22), Math.floor(h * 0.07), 8);
        this.uiLayer.add(topBg);

        this.add.text(20, 16, this.zone.name, {
            fontSize: `${Math.floor(fs * 1.1)}px`, fontFamily: 'monospace',
            color: '#ffffff', fontStyle: 'bold'
        }).setScrollFactor(0).setDepth(101);

        this.add.text(20, 16 + Math.floor(fs * 1.4),
            `${'★'.repeat(this.zone.difficulty)}${'☆'.repeat(3 - this.zone.difficulty)}`, {
            fontSize: `${Math.floor(fs * 0.85)}px`, fontFamily: 'monospace', color: '#aaa'
        }).setScrollFactor(0).setDepth(101);

        // Top-right: inventory count
        const invBgW = Math.floor(w * 0.18);
        const invBg = this.add.graphics().setScrollFactor(0).setDepth(100);
        invBg.fillStyle(0x000000, 0.6);
        invBg.fillRoundedRect(w - invBgW - 10, 10, invBgW, Math.floor(h * 0.045), 8);
        this.uiLayer.add(invBg);

        this.inventoryText = this.add.text(w - invBgW, 18, '', {
            fontSize: `${fs}px`, fontFamily: 'monospace', color: '#ffcc44'
        }).setScrollFactor(0).setDepth(101);
        this.updateInventoryCount();

        // Bottom: HP bar (wide)
        const hpBarW = Math.floor(w * 0.35);
        const hpBarH = Math.floor(h * 0.025);
        const hpBarX = Math.floor((w - hpBarW) / 2);
        const hpBarY = h - Math.floor(h * 0.07);

        const hpBg = this.add.graphics().setScrollFactor(0).setDepth(100);
        hpBg.fillStyle(0x000000, 0.6);
        hpBg.fillRoundedRect(hpBarX - 6, hpBarY - 6, hpBarW + 12, hpBarH + 22, 8);
        this.uiLayer.add(hpBg);

        this.hpBarBg = this.add.graphics().setScrollFactor(0).setDepth(101);
        this.hpBarBg.fillStyle(0x333333, 1);
        this.hpBarBg.fillRect(hpBarX, hpBarY, hpBarW, hpBarH);

        this.hpBarFill = this.add.graphics().setScrollFactor(0).setDepth(102);
        this.hpBarData = { x: hpBarX, y: hpBarY, w: hpBarW, h: hpBarH };
        this._drawHudHpBar();

        this.hpText = this.add.text(Math.floor(w / 2), hpBarY + hpBarH + 4, '', {
            fontSize: `${Math.floor(fs * 0.75)}px`, fontFamily: 'monospace', color: '#ccc'
        }).setOrigin(0.5, 0).setScrollFactor(0).setDepth(102);
        this._updateHpText();

        // Cooldown indicator
        this.cooldownBar = this.add.graphics().setScrollFactor(0).setDepth(102);

        // Controls hint
        const ctrlBg = this.add.graphics().setScrollFactor(0).setDepth(100);
        ctrlBg.fillStyle(0x000000, 0.5);
        ctrlBg.fillRoundedRect(10, h - Math.floor(h * 0.035), Math.floor(w * 0.52), Math.floor(h * 0.03), 6);
        this.uiLayer.add(ctrlBg);

        this.add.text(20, h - Math.floor(h * 0.032),
            'WASD:이동  SPACE/클릭:공격  E:수색/탈출  TAB:인벤  M:맵  ESC:철수', {
            fontSize: `${Math.floor(fs * 0.65)}px`, fontFamily: 'monospace', color: '#555'
        }).setScrollFactor(0).setDepth(101);

        // Center message
        this.msgText = this.add.text(w / 2, h * 0.4, '', {
            fontSize: `${Math.floor(fs * 1.1)}px`, fontFamily: 'monospace',
            color: '#fff', stroke: '#000', strokeThickness: 3, align: 'center'
        }).setOrigin(0.5).setScrollFactor(0).setDepth(101).setAlpha(0);

        // Search prompt
        this.searchPromptText = this.add.text(w / 2, h / 2 + Math.floor(h * 0.07), '', {
            fontSize: `${Math.floor(fs * 1.1)}px`, fontFamily: 'monospace',
            color: '#ffcc44', stroke: '#000', strokeThickness: 3, align: 'center'
        }).setOrigin(0.5).setScrollFactor(0).setDepth(150).setAlpha(0);

        // Tiles moved counter (top-left, small)
        this.tilesMovedText = this.add.text(20, Math.floor(h * 0.07) + 14, '', {
            fontSize: `${Math.floor(fs * 0.65)}px`, fontFamily: 'monospace', color: '#555'
        }).setScrollFactor(0).setDepth(101);

        this.drawMinimap();
    }

    _drawHudHpBar() {
        const d = this.hpBarData;
        this.hpBarFill.clear();
        const ratio = Math.max(0, this.player.hp / this.player.maxHp);
        const color = ratio > 0.5 ? 0x44ff44 : ratio > 0.25 ? 0xffaa44 : 0xff4444;
        this.hpBarFill.fillStyle(color, 1);
        this.hpBarFill.fillRect(d.x, d.y, Math.floor(d.w * ratio), d.h);
    }

    _updateHpText() {
        this.hpText.setText(`HP: ${Math.ceil(this.player.hp)} / ${this.player.maxHp}`);
    }

    updateInventoryCount() {
        const val = this.inventory.reduce((s, id) => s + (ITEM_DATA[id]?.value || 0), 0);
        this.inventoryText.setText(`\u{1F392} ${this.inventory.length}/${MAX_INVENTORY}  \u{1F4B0} ${val}`);
    }

    // ── MINIMAP ────────────────────────────────────────────

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
        // Enemies on minimap
        this.enemies.forEach(e => {
            if (!e.alive || !this.discovered[e.gy]?.[e.gx]) return;
            this.minimapGfx.fillStyle(0xff4444, 1);
            this.minimapGfx.fillRect(mmX + e.gx * s, mmY + e.gy * s, s + 1, s + 1);
        });
        // Player on minimap
        this.minimapGfx.fillStyle(0x44aaff, 1);
        this.minimapGfx.fillRect(mmX + this.player.gx * s, mmY + this.player.gy * s, s + 1, s + 1);
    }

    // ── MESSAGE ────────────────────────────────────────────

    showMessage(msg, duration = 2500) {
        this.msgText.setText(msg).setAlpha(1);
        this.tweens.killTweensOf(this.msgText);
        this.tweens.add({ targets: this.msgText, alpha: 0, delay: duration - 400, duration: 400 });
    }

    // ── UPDATE LOOP ────────────────────────────────────────

    update(time, delta) {
        if (this.playerDead) return;

        // Key polling
        if (Phaser.Input.Keyboard.JustDown(this.eKey)) {
            if (this.isSearching) { this.closeLootPopup(); }
            else if (!this.inventoryOpen && !this.fullMapOpen) { this.interact(); }
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
                this._emergencyExtract();
            } else {
                this.confirmExit();
            }
            return;
        }
        if (Phaser.Input.Keyboard.JustDown(this.mKey)) {
            if (!this.isSearching && !this.inventoryOpen) this.toggleFullMap();
            return;
        }
        if (Phaser.Input.Keyboard.JustDown(this.spaceKey)) {
            if (!this.isSearching && !this.inventoryOpen && !this.fullMapOpen) {
                this.playerAttack();
            }
            return;
        }

        // Paused states
        if (this.inventoryOpen || this.fullMapOpen || this.isSearching) return;

        // Player movement
        if (time - this.moveTimer >= this.MOVE_DELAY) {
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

        // Enemy AI
        this.updateEnemies(time, delta);

        // Cooldown indicator
        this._drawCooldownIndicator(time);
    }

    _drawCooldownIndicator(time) {
        this.cooldownBar.clear();
        const elapsed = time - this.lastPlayerAttack;
        const cd = this.player.attackCooldown;
        if (elapsed < cd) {
            const d = this.hpBarData;
            const ratio = elapsed / cd;
            this.cooldownBar.fillStyle(0x4488ff, 0.4);
            this.cooldownBar.fillRect(d.x, d.y + d.h + 2, Math.floor(d.w * ratio), 2);
        }
    }

    // ── PLAYER MOVEMENT ────────────────────────────────────

    tryMove(dx, dy) {
        const nx = this.player.gx + dx, ny = this.player.gy + dy;
        if (nx < 0 || ny < 0 || nx >= this.mapW || ny >= this.mapH) return;

        const tile = this.mapGrid[ny][nx];
        if (tile === TILE.WALL || tile === TILE.WATER || tile === TILE.FURNITURE ||
            tile === TILE.BARREL || tile === TILE.LOCKER || tile === TILE.CRATE ||
            tile === TILE.MEDICAL || tile === TILE.COMPUTER) return;

        // Check enemy collision
        const blocked = this.enemies.some(e => e.alive && e.gx === nx && e.gy === ny);
        if (blocked) return;

        const T = this.TILE_SIZE;
        this.player.gx = nx;
        this.player.gy = ny;

        this.tweens.add({
            targets: this.player.container,
            x: nx * T + T / 2, y: ny * T + T / 2,
            duration: 70, ease: 'Linear',
            onComplete: () => {
                this.revealAround(nx, ny, PLAYER_DATA.visionRadius);
                this.tilesMoved++;
                this.tilesMovedText.setText(`\u{1F9ED} ${this.tilesMoved} tiles`);
                this.checkExtractReveal();
                this.checkTileEvents(nx, ny);
                this.updateMinimap();
            }
        });
    }

    checkTileEvents(x, y) {
        const tile = this.mapGrid[y][x];

        if (tile === TILE.EXIT && this.extractsRevealed) {
            this.showMessage('\u{1F681} Extract point! Press E to extract');
        }

        // Show search prompt for nearby searchables
        let foundSearchable = false;
        const searchable = SEARCHABLE_TILES[tile];
        if (searchable && !this.searched[`${x},${y}`]) {
            this.searchPromptText.setText(`E: ${searchable.icon} ${searchable.name}`).setAlpha(1);
            foundSearchable = true;
        }

        if (!foundSearchable) {
            const dirs = [[0, -1], [0, 1], [-1, 0], [1, 0]];
            for (const [ddx, ddy] of dirs) {
                const nx = x + ddx, ny = y + ddy;
                if (nx < 0 || ny < 0 || nx >= this.mapW || ny >= this.mapH) continue;
                const adjTile = this.mapGrid[ny][nx];
                const adjSearch = SEARCHABLE_TILES[adjTile];
                if (adjSearch && !this.searched[`${nx},${ny}`]) {
                    this.searchPromptText.setText(`E: ${adjSearch.icon} ${adjSearch.name}`).setAlpha(1);
                    foundSearchable = true;
                    break;
                }
                if (adjTile === TILE.EXIT && this.extractsRevealed) {
                    this.searchPromptText.setText(`E: \u{1F681} Extract`).setAlpha(1);
                    foundSearchable = true;
                    break;
                }
            }
        }

        if (!foundSearchable) this.searchPromptText.setAlpha(0);
    }

    // ── EXTRACT REVEAL ─────────────────────────────────────

    checkExtractReveal() {
        if (this.extractsRevealed) return;
        if (this.tilesMoved >= 500) {
            this.extractsRevealed = true;
            this.extractMarkers.forEach(m => m.text.setVisible(true));
            this.showMessage('\u{1F681} Extract points revealed! Head to an extract to escape.', 4000);
        }
    }

    // ── ENEMY AI ───────────────────────────────────────────

    updateEnemies(time, delta) {
        const px = this.player.gx, py = this.player.gy;
        const dirs = [[0, -1], [0, 1], [-1, 0], [1, 0]];

        for (const enemy of this.enemies) {
            if (!enemy.alive) continue;

            const dist = Math.abs(enemy.gx - px) + Math.abs(enemy.gy - py);
            const d = enemy.data;
            const alertChild = enemy.container.list[5]; // alert text

            switch (enemy.state) {
                case 'idle':
                    // Check detection
                    if (dist <= d.detectionRange && this.discovered[enemy.gy]?.[enemy.gx]) {
                        enemy.state = 'alert';
                        enemy.alertStartTime = time;
                        alertChild.setAlpha(1);
                        this.tweens.add({
                            targets: alertChild, scaleX: 1.3, scaleY: 1.3,
                            yoyo: true, duration: 200
                        });
                    } else if (enemy.patrol && time - enemy.lastPatrolTime > 2000) {
                        this._enemyPatrol(enemy, time, dirs);
                    }
                    break;

                case 'alert':
                    if (time - enemy.alertStartTime >= d.alertDuration) {
                        enemy.state = 'chase';
                        alertChild.setAlpha(0);
                    }
                    break;

                case 'chase':
                    if (dist > d.loseRange) {
                        enemy.state = 'idle';
                        break;
                    }
                    if (dist <= d.attackRange) {
                        enemy.state = 'attack';
                        break;
                    }
                    // Move toward player (greedy)
                    if (time - enemy.lastChaseTime >= (1000 / d.chaseSpeed)) {
                        enemy.lastChaseTime = time;
                        this._enemyChaseStep(enemy, px, py, dirs);
                    }
                    break;

                case 'attack':
                    if (dist > d.attackRange + 1) {
                        enemy.state = 'chase';
                        break;
                    }
                    if (dist > d.loseRange) {
                        enemy.state = 'idle';
                        break;
                    }
                    this.enemyAttack(enemy, time);
                    break;
            }
        }
    }

    _enemyPatrol(enemy, time, dirs) {
        enemy.lastPatrolTime = time;
        if (Math.random() < 0.5) return; // sometimes stand still
        if (Math.random() < 0.15) enemy.patrolDir = Math.floor(Math.random() * 4);

        const [ddx, ddy] = dirs[enemy.patrolDir];
        const nx = enemy.gx + ddx, ny = enemy.gy + ddy;
        if (nx < 1 || ny < 1 || nx >= this.mapW - 1 || ny >= this.mapH - 1) {
            enemy.patrolDir = (enemy.patrolDir + 1) % 4;
            return;
        }
        const tile = this.mapGrid[ny][nx];
        if (tile === TILE.WALL || tile === TILE.WATER || tile === TILE.FURNITURE || tile === TILE.EXIT) {
            enemy.patrolDir = (enemy.patrolDir + 1) % 4;
            return;
        }
        this._moveEnemy(enemy, nx, ny);
    }

    _enemyChaseStep(enemy, px, py, dirs) {
        // Greedy pathfinding: pick direction that reduces distance most
        let best = null, bestDist = Infinity;
        for (const [ddx, ddy] of dirs) {
            const nx = enemy.gx + ddx, ny = enemy.gy + ddy;
            if (nx < 0 || ny < 0 || nx >= this.mapW || ny >= this.mapH) continue;
            const tile = this.mapGrid[ny][nx];
            if (tile === TILE.WALL || tile === TILE.WATER || tile === TILE.FURNITURE) continue;
            // Don't walk into other enemies
            if (this.enemies.some(e => e !== enemy && e.alive && e.gx === nx && e.gy === ny)) continue;
            const d = Math.abs(nx - px) + Math.abs(ny - py);
            if (d < bestDist) { bestDist = d; best = { x: nx, y: ny }; }
        }
        if (best) this._moveEnemy(enemy, best.x, best.y);
    }

    _moveEnemy(enemy, nx, ny) {
        enemy.gx = nx;
        enemy.gy = ny;
        const T = this.TILE_SIZE;
        this.tweens.add({
            targets: enemy.container,
            x: nx * T + T / 2, y: ny * T + T / 2,
            duration: 180
        });
    }

    // ── COMBAT ─────────────────────────────────────────────

    playerAttack() {
        const time = this.time.now;
        if (time - this.lastPlayerAttack < this.player.attackCooldown) return;

        // Find nearest enemy within range
        const range = this.player.attackRange;
        let nearest = null, nearestDist = Infinity;
        for (const enemy of this.enemies) {
            if (!enemy.alive) continue;
            const dist = Math.abs(enemy.gx - this.player.gx) + Math.abs(enemy.gy - this.player.gy);
            if (dist <= range && dist < nearestDist) {
                nearestDist = dist;
                nearest = enemy;
            }
        }

        if (!nearest) return;

        this.lastPlayerAttack = time;
        const damage = Math.max(1, this.player.atk - nearest.data.def);
        this.enemyTakeDamage(nearest, damage);

        // Brief visual feedback on player
        this.tweens.add({
            targets: this.player.container, scaleX: 1.15, scaleY: 1.15,
            yoyo: true, duration: 80
        });
    }

    enemyAttack(enemy, time) {
        if (time - enemy.lastAttackTime < enemy.data.attackCooldown) return;
        enemy.lastAttackTime = time;

        // Invulnerability check
        if (time - this.lastPlayerHitTime < this.INVULN_MS) return;

        const damage = Math.max(1, enemy.data.atk - this.player.def);
        this.takeDamage(damage);

        // Enemy lunge animation
        this.tweens.add({
            targets: enemy.container, scaleX: 1.2, scaleY: 1.2,
            yoyo: true, duration: 100
        });
    }

    takeDamage(amount) {
        this.lastPlayerHitTime = this.time.now;
        this.player.hp -= amount;

        DamagePopup.show(this, this.player.container.x, this.player.container.y - 20, amount, '#ff4444');

        // Screen shake
        this.cameras.main.shake(150, 0.005);

        // Red flash on player
        const flash = this.add.graphics();
        flash.fillStyle(0xff0000, 0.3);
        flash.fillRect(0, 0, this.cameras.main.width, this.cameras.main.height);
        flash.setScrollFactor(0).setDepth(90);
        this.tweens.add({ targets: flash, alpha: 0, duration: 200, onComplete: () => flash.destroy() });

        this._drawPlayerHpBar();
        this._drawHudHpBar();
        this._updateHpText();

        if (this.player.hp <= 0) {
            this.player.hp = 0;
            this.playerDeath();
        }
    }

    enemyTakeDamage(enemy, amount) {
        enemy.hp -= amount;
        DamagePopup.show(this, enemy.container.x, enemy.container.y - 20, amount, '#ffcc44');
        this._updateEnemyHpBar(enemy);

        // Hit flash
        this.tweens.add({
            targets: enemy.container, alpha: 0.4,
            yoyo: true, duration: 80
        });

        if (enemy.hp <= 0) {
            enemy.hp = 0;
            this.killEnemy(enemy);
        } else if (enemy.state === 'idle') {
            // Wake up if hit while idle
            enemy.state = 'chase';
        }
    }

    killEnemy(enemy) {
        enemy.alive = false;
        enemy.state = 'dead';

        DamagePopup.showText(this, enemy.container.x, enemy.container.y - 10, 'KILLED', '#ff8844');

        // Drop loot on ground
        const lootItems = enemy.data.loot || [];
        if (lootItems.length > 0) {
            const dropId = lootItems[Math.floor(Math.random() * lootItems.length)];
            if (this.inventory.length < MAX_INVENTORY) {
                this.inventory.push(dropId);
                const item = ITEM_DATA[dropId];
                if (item) {
                    DamagePopup.showText(this, enemy.container.x, enemy.container.y + 10,
                        `${item.icon} ${item.name}`, '#44ff88');
                }
                this.updateInventoryCount();
            }
        }

        // Fade out
        this.tweens.add({
            targets: enemy.container, alpha: 0, duration: 500,
            onComplete: () => enemy.container.setVisible(false)
        });
    }

    playerDeath() {
        this.playerDead = true;
        this.showMessage('You died...', 3000);

        // Red overlay
        const overlay = this.add.graphics().setScrollFactor(0).setDepth(200);
        overlay.fillStyle(0x000000, 0);
        this.tweens.add({
            targets: overlay, alpha: 1, duration: 1500,
            onUpdate: (tween) => {
                overlay.clear();
                overlay.fillStyle(0x110000, tween.getValue());
                overlay.fillRect(0, 0, this.cameras.main.width, this.cameras.main.height);
            }
        });

        this.tweens.add({
            targets: this.player.container, alpha: 0, duration: 1000
        });

        this.time.delayedCall(2000, () => {
            game.scene.stop('ExpeditionScene');
            game.scene.start('SafeHouseScene', {
                inventory: [], stash: this.stash, gold: this.gold, safe: false
            });
        });
    }

    // ── INTERACT ───────────────────────────────────────────

    interact() {
        const x = this.player.gx, y = this.player.gy;
        const tile = this.mapGrid[y][x];

        // Standing on extract
        if (tile === TILE.EXIT && this.extractsRevealed) {
            this.extractSuccess();
            return;
        }

        // Standing on searchable
        const searchable = SEARCHABLE_TILES[tile];
        if (searchable && !this.searched[`${x},${y}`]) {
            this.searchContainer(x, y, searchable);
            return;
        }

        // Adjacent tiles
        const dirs = [[0, -1], [0, 1], [-1, 0], [1, 0]];
        for (const [dx, dy] of dirs) {
            const nx = x + dx, ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= this.mapW || ny >= this.mapH) continue;
            const adjTile = this.mapGrid[ny][nx];
            const adjSearch = SEARCHABLE_TILES[adjTile];
            if (adjSearch && !this.searched[`${nx},${ny}`]) {
                this.searchContainer(nx, ny, adjSearch);
                return;
            }
            if (adjTile === TILE.EXIT && this.extractsRevealed) {
                this.extractSuccess();
                return;
            }
        }

        this.showMessage('Nothing to interact with', 1500);
    }

    // ── SEARCH ─────────────────────────────────────────────

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
        this.lootPopup.add(this.add.text(px + pw / 2, py + 12,
            `${searchable.icon} ${searchable.name}`, {
            fontSize: `${Math.floor(fs * 1.1)}px`, fontFamily: 'monospace',
            color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5).setScrollFactor(0));

        if (found.length === 0) {
            this.lootPopup.add(this.add.text(px + pw / 2, py + headerH + 10, 'Empty...', {
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
                this.lootPopup.add(this.add.text(px + pw / 2, py + headerH + row * lineH + 4,
                    `Inventory full! ${overflow} items lost`, {
                    fontSize: `${Math.floor(fs * 0.75)}px`, fontFamily: 'monospace', color: '#ff6644'
                }).setOrigin(0.5).setScrollFactor(0));
            }
        }

        const closeText = this.add.text(px + pw / 2, py + ph - footerH + 2, '[ E / ESC / Click to close ]', {
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

    // ── EXTRACT ────────────────────────────────────────────

    extractSuccess() {
        this.showMessage('Extract successful!');
        const extractData = {
            inventory: [], stash: [...this.stash, ...this.inventory],
            gold: this.gold, safe: true,
            lootValue: this.inventory.reduce((s, id) => s + (ITEM_DATA[id]?.value || 0), 0),
            extracted: true
        };
        this.cameras.main.fadeOut(600, 0, 0, 0);
        this.time.delayedCall(650, () => {
            game.scene.stop('ExpeditionScene');
            game.scene.start('SafeHouseScene', extractData);
        });
    }

    _emergencyExtract() {
        const exitData = { inventory: [], stash: this.stash, gold: this.gold, safe: false };
        this.cameras.main.fadeOut(400, 0, 0, 0);
        this.time.delayedCall(450, () => {
            game.scene.stop('ExpeditionScene');
            game.scene.start('SafeHouseScene', exitData);
        });
    }

    confirmExit() {
        this.showMessage('Press ESC again to emergency extract (lose all loot!)', 3000);
        this.confirmingExit = true;
        this.time.delayedCall(3000, () => { this.confirmingExit = false; });
    }

    // ── INVENTORY ──────────────────────────────────────────

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

        this.inventoryPanel.add(this.add.text(px + pw / 2, py + 15,
            `\u{1F392} Inventory (${this.inventory.length}/${MAX_INVENTORY})`, {
            fontSize: `${Math.floor(ifs * 1.2)}px`, fontFamily: 'monospace',
            color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5).setScrollFactor(0));

        const totalWeight = this.inventory.reduce((s, id) => s + (ITEM_DATA[id]?.weight || 0), 0);
        const totalValue = this.inventory.reduce((s, id) => s + (ITEM_DATA[id]?.value || 0), 0);
        this.inventoryPanel.add(this.add.text(px + pw / 2, py + 38,
            `Weight: ${totalWeight.toFixed(1)}kg  |  Value: ${totalValue}G`, {
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
                const typeNames = {
                    material: 'Materials', consumable: 'Consumables', ammo: 'Ammo',
                    equipment: 'Equipment', valuable: 'Valuables', key: 'Keys'
                };
                this.inventoryPanel.add(this.add.text(px + 15, py + 60 + row * rowH,
                    `-- ${typeNames[item.type] || item.type} --`, {
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
            this.inventoryPanel.add(this.add.text(px + pw / 2, py + 80, 'Empty', {
                fontSize: `${ifs}px`, fontFamily: 'monospace', color: '#555'
            }).setOrigin(0.5).setScrollFactor(0));
        }

        this.inventoryPanel.add(this.add.text(px + pw / 2, py + ph - 20, 'TAB / ESC: close', {
            fontSize: `${Math.floor(ifs * 0.75)}px`, fontFamily: 'monospace', color: '#666'
        }).setOrigin(0.5).setScrollFactor(0));
    }

    // ── FULL MAP ───────────────────────────────────────────

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
        for (let y = 0; y < this.mapH; y++) {
            for (let x = 0; x < this.mapW; x++) {
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
        }

        // Enemies on full map
        this.enemies.forEach(e => {
            if (!e.alive || !this.discovered[e.gy]?.[e.gx]) return;
            mg.fillStyle(0xff4444, 1);
            mg.fillCircle(ox + e.gx * scale + scale / 2, oy + e.gy * scale + scale / 2, Math.max(2, scale));
        });

        // Player on full map
        mg.fillStyle(0x44aaff, 1);
        mg.fillCircle(ox + this.player.gx * scale + scale / 2, oy + this.player.gy * scale + scale / 2,
            Math.max(3, scale * 1.5));
        this.fullMapPanel.add(mg);

        this.fullMapPanel.add(this.add.text(cam.width / 2, 15, `${this.zone.name}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#fff'
        }).setOrigin(0.5).setScrollFactor(0));

        this.fullMapPanel.add(this.add.text(cam.width / 2, cam.height - 15, 'M: close', {
            fontSize: '10px', fontFamily: 'monospace', color: '#666'
        }).setOrigin(0.5).setScrollFactor(0));
    }
}
