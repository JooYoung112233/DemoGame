class ExpeditionScene extends Phaser.Scene {
    constructor() { super('ExpeditionScene'); }

    init(data) {
        this.zoneId = data.zone || 'downtown';
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
        this.openBuildingIndices = mapData.openBuildingIndices;
        this.nightTileChanges = mapData.nightTileChanges;
        this.nightEnemyData = mapData.nightEnemies;

        this.discovered = Array.from({ length: this.mapH }, () => Array(this.mapW).fill(false));
        this.searched = {};

        this.tileLayer = this.add.container(0, 0);
        this.entityLayer = this.add.container(0, 0);
        this.fogLayer = this.add.container(0, 0);
        this.uiLayer = this.add.container(0, 0).setScrollFactor(0).setDepth(100);

        // Store tile graphics references for night swap (must be before drawMap)
        this.tileGraphics = Array.from({ length: this.mapH }, () => Array(this.mapW).fill(null));
        this.tileIcons = {};

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
        this.playerFacing = { x: 0, y: -1 }; // facing up by default

        // Combat timing
        this.lastPlayerAttack = -99999;
        this.lastPlayerHitTime = 0;
        this.INVULN_MS = 500;

        // ── Day/Night system ──
        this.isNight = false;
        this.dayElapsed = 0;   // seconds elapsed in day phase
        this.nightElapsed = 0; // seconds elapsed in night phase
        this.dayDuration = this.zone.dayDuration || 120;   // seconds
        this.nightDuration = this.zone.nightDuration || 300; // seconds
        this.nightTransitioning = false;
        this.nightWarningShown = { half: false, quarter: false, minute: false, ten: false };
        this.cityClosing = false;

        // ── Flashlight / battery ──
        this.flashlightOn = true;
        this.flashlightBattery = PLAYER_DATA.flashlightBattery;
        this.maxBattery = PLAYER_DATA.flashlightBattery;
        this.batteryDrain = PLAYER_DATA.batteryDrain;
        // Apply flashlight mod if equipped
        const eq = this.playerState.equipment;
        if (eq) {
            for (const key of Object.keys(eq)) {
                const eqId = eq[key];
                if (eqId && EQUIPMENT_DATA[eqId]?.type === 'accessory') {
                    const mod = EQUIPMENT_DATA[eqId];
                    if (mod.flashlightRange) this.flashlightBonusRange = mod.flashlightRange;
                    if (mod.batteryDrain) this.batteryDrain += mod.batteryDrain;
                }
            }
        }
        this.flashlightBonusRange = this.flashlightBonusRange || 0;
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

                this._drawTileGraphic(g, tile, tc, px, py, T);
                g.setDepth(0);
                this.tileLayer.add(g);
                this.tileGraphics[y][x] = g;

                const searchable = SEARCHABLE_TILES[tile];
                if (searchable) {
                    const icon = this.add.text(px + T / 2, py + T / 2, searchable.icon, {
                        fontSize: '14px'
                    }).setOrigin(0.5).setDepth(2);
                    this.tileLayer.add(icon);
                    this.tileIcons[`${x},${y}`] = icon;
                }
            }
        }
    }

    _drawTileGraphic(g, tile, tc, px, py, T) {
        if (tile === TILE.WALL || tile === TILE.NIGHT_WALL) {
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

        // Flashlight cone (visible graphic)
        this.flashlightCone = this.add.graphics();
        this.flashlightCone.setAlpha(0); // hidden during day

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

        this.player.container.add([this.flashlightCone, body, name, this.playerHpBg, this.playerHpBar]);
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
        this._spawnEnemyList(this.entities.enemies);
    }

    _spawnEnemyList(enemyList) {
        const T = this.TILE_SIZE;
        enemyList.forEach((e) => {
            const data = ENEMY_DATA[e.enemyId];
            if (!data) return;

            const c = this.add.container(e.x * T + T / 2, e.y * T + T / 2);

            const body = this.add.graphics();
            body.fillStyle(data.color, 0.9);
            body.fillRect(-7, -7, 14, 14);
            body.lineStyle(1, 0xff6666, 0.4);
            body.strokeRect(-7, -7, 14, 14);

            const typeIcon = ENEMY_ICONS[data.type] || '?';
            const icon = this.add.text(0, 0, typeIcon, { fontSize: '10px' }).setOrigin(0.5);

            const nameText = this.add.text(0, -14, data.name, {
                fontSize: '7px', fontFamily: 'monospace', color: '#ff8888',
                stroke: '#000000', strokeThickness: 2
            }).setOrigin(0.5);

            const hpBg = this.add.graphics();
            hpBg.fillStyle(0x333333, 1); hpBg.fillRect(-8, 9, 16, 2);
            const hpBar = this.add.graphics();
            hpBar.fillStyle(0xff4444, 1); hpBar.fillRect(-8, 9, 16, 2);

            const alert = this.add.text(0, -22, '!', {
                fontSize: '14px', fontFamily: 'monospace', fontStyle: 'bold',
                color: '#ff0000', stroke: '#000000', strokeThickness: 3
            }).setOrigin(0.5).setAlpha(0);

            c.add([body, icon, nameText, hpBg, hpBar, alert]);
            c.setDepth(9);

            const enemy = {
                container: c,
                index: this.enemies.length,
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
            t.setVisible(true);
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

    /**
     * Day: circular reveal (full radius)
     * Night (flashlight on): fan-shaped forward + tiny circle around player
     * Night (flashlight off): tiny circle only
     */
    revealAround(cx, cy, radius) {
        if (!this.isNight) {
            // Day: circular
            this._revealCircle(cx, cy, radius);
        } else {
            // Night: small base circle + flashlight cone
            const baseRadius = PLAYER_DATA.nightVisionRadius;
            this._revealCircle(cx, cy, baseRadius);

            if (this.flashlightOn && this.flashlightBattery > 0) {
                const flRange = PLAYER_DATA.flashlightRange + this.flashlightBonusRange;
                const flArc = PLAYER_DATA.flashlightArc;
                this._revealCone(cx, cy, flRange, this.playerFacing, flArc);
            }
        }
    }

    _revealCircle(cx, cy, radius) {
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

    _revealCone(cx, cy, range, facing, arcDegrees) {
        const halfArc = (arcDegrees / 2) * (Math.PI / 180);
        const facingAngle = Math.atan2(facing.y, facing.x);

        for (let dy = -range; dy <= range; dy++) {
            for (let dx = -range; dx <= range; dx++) {
                if (dx * dx + dy * dy > range * range) continue;
                if (dx === 0 && dy === 0) continue;
                const angle = Math.atan2(dy, dx);
                let diff = angle - facingAngle;
                // Normalize to [-PI, PI]
                while (diff > Math.PI) diff -= 2 * Math.PI;
                while (diff < -Math.PI) diff += 2 * Math.PI;
                if (Math.abs(diff) > halfArc) continue;

                const nx = cx + dx, ny = cy + dy;
                if (nx < 0 || ny < 0 || nx >= this.mapW || ny >= this.mapH) continue;

                // Simple line-of-sight: check if wall blocks
                if (this._hasWallBetween(cx, cy, nx, ny)) continue;

                if (!this.discovered[ny][nx]) {
                    this.discovered[ny][nx] = true;
                    this.tweens.add({ targets: this.fogTiles[ny][nx], alpha: 0, duration: 120 });
                }
            }
        }
    }

    /** Bresenham-ish LOS check */
    _hasWallBetween(x0, y0, x1, y1) {
        const dx = Math.abs(x1 - x0), dy = Math.abs(y1 - y0);
        const sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        let err = dx - dy;
        let cx = x0, cy = y0;
        while (cx !== x1 || cy !== y1) {
            const e2 = 2 * err;
            if (e2 > -dy) { err -= dy; cx += sx; }
            if (e2 < dx) { err += dx; cy += sy; }
            if (cx === x1 && cy === y1) break;
            const t = this.mapGrid[cy]?.[cx];
            if (t === TILE.WALL || t === TILE.NIGHT_WALL) return true;
        }
        return false;
    }

    /** Re-fog tiles not in current vision (night only) */
    _updateNightFog() {
        if (!this.isNight) return;
        const px = this.player.gx, py = this.player.gy;
        const baseR = PLAYER_DATA.nightVisionRadius;
        const flRange = (this.flashlightOn && this.flashlightBattery > 0)
            ? PLAYER_DATA.flashlightRange + this.flashlightBonusRange : 0;
        const maxR = Math.max(baseR, flRange) + 1;
        const flArc = PLAYER_DATA.flashlightArc;
        const halfArc = (flArc / 2) * (Math.PI / 180);
        const facingAngle = Math.atan2(this.playerFacing.y, this.playerFacing.x);

        // Re-fog previously discovered tiles that are now out of vision
        for (let dy = -maxR - 4; dy <= maxR + 4; dy++) {
            for (let dx = -maxR - 4; dx <= maxR + 4; dx++) {
                const nx = px + dx, ny = py + dy;
                if (nx < 0 || ny < 0 || nx >= this.mapW || ny >= this.mapH) continue;
                if (!this.discovered[ny][nx]) continue;

                const dist2 = dx * dx + dy * dy;

                // In base circle?
                if (dist2 <= baseR * baseR) continue;

                // In flashlight cone?
                if (flRange > 0 && dist2 <= flRange * flRange) {
                    const angle = Math.atan2(dy, dx);
                    let diff = angle - facingAngle;
                    while (diff > Math.PI) diff -= 2 * Math.PI;
                    while (diff < -Math.PI) diff += 2 * Math.PI;
                    if (Math.abs(diff) <= halfArc && !this._hasWallBetween(px, py, nx, ny)) continue;
                }

                // Near streetlight?
                if (this._nearStreetlight(nx, ny)) continue;

                // Re-fog this tile
                const fg = this.fogTiles[ny][nx];
                if (fg.alpha < 0.5) {
                    this.tweens.killTweensOf(fg);
                    this.tweens.add({ targets: fg, alpha: 0.7, duration: 200 });
                }
            }
        }
    }

    _nearStreetlight(x, y) {
        for (let dy = -2; dy <= 2; dy++) {
            for (let dx = -2; dx <= 2; dx++) {
                const nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= this.mapW || ny >= this.mapH) continue;
                if (this.mapGrid[ny][nx] === TILE.STREETLIGHT) return true;
            }
        }
        return false;
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
        this.fKey = this.input.keyboard.addKey('F'); // toggle flashlight

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

        // Top-left: zone info + day/night
        const topBg = this.add.graphics().setScrollFactor(0).setDepth(100);
        topBg.fillStyle(0x000000, 0.6);
        topBg.fillRoundedRect(10, 10, Math.floor(w * 0.26), Math.floor(h * 0.1), 8);
        this.uiLayer.add(topBg);

        this.zoneNameText = this.add.text(20, 16, this.zone.name, {
            fontSize: `${Math.floor(fs * 1.1)}px`, fontFamily: 'monospace',
            color: '#ffffff', fontStyle: 'bold'
        }).setScrollFactor(0).setDepth(101);

        this.add.text(20, 16 + Math.floor(fs * 1.4),
            `${'★'.repeat(this.zone.difficulty)}${'☆'.repeat(3 - this.zone.difficulty)}`, {
            fontSize: `${Math.floor(fs * 0.85)}px`, fontFamily: 'monospace', color: '#aaa'
        }).setScrollFactor(0).setDepth(101);

        // Day/Night & timer
        this.phaseText = this.add.text(20, 16 + Math.floor(fs * 2.6), '', {
            fontSize: `${Math.floor(fs * 0.9)}px`, fontFamily: 'monospace', color: '#ffcc44'
        }).setScrollFactor(0).setDepth(101);

        // Night timer bar (hidden during day)
        const timerBarW = Math.floor(w * 0.3);
        const timerBarH = Math.floor(h * 0.015);
        const timerBarX = Math.floor((w - timerBarW) / 2);
        const timerBarY = 12;

        this.nightTimerBg = this.add.graphics().setScrollFactor(0).setDepth(100);
        this.nightTimerBg.fillStyle(0x000000, 0.6);
        this.nightTimerBg.fillRoundedRect(timerBarX - 4, timerBarY - 4, timerBarW + 8, timerBarH + 20, 6);
        this.nightTimerBg.setAlpha(0);
        this.uiLayer.add(this.nightTimerBg);

        this.nightTimerBarBg = this.add.graphics().setScrollFactor(0).setDepth(101);
        this.nightTimerBarBg.fillStyle(0x333333, 1);
        this.nightTimerBarBg.fillRect(timerBarX, timerBarY, timerBarW, timerBarH);
        this.nightTimerBarBg.setAlpha(0);

        this.nightTimerFill = this.add.graphics().setScrollFactor(0).setDepth(102);
        this.nightTimerFill.setAlpha(0);
        this.nightTimerData = { x: timerBarX, y: timerBarY, w: timerBarW, h: timerBarH };

        this.nightTimerText = this.add.text(timerBarX + timerBarW / 2, timerBarY + timerBarH + 2, '', {
            fontSize: `${Math.floor(fs * 0.7)}px`, fontFamily: 'monospace', color: '#ff8844'
        }).setOrigin(0.5, 0).setScrollFactor(0).setDepth(102).setAlpha(0);

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

        // Battery indicator (top-right, below inventory)
        const battBgW = Math.floor(w * 0.14);
        const battBgY = Math.floor(h * 0.065);
        const battBg = this.add.graphics().setScrollFactor(0).setDepth(100);
        battBg.fillStyle(0x000000, 0.6);
        battBg.fillRoundedRect(w - battBgW - 10, battBgY, battBgW, Math.floor(h * 0.035), 6);
        this.uiLayer.add(battBg);

        this.batteryText = this.add.text(w - battBgW - 2, battBgY + 4, '', {
            fontSize: `${Math.floor(fs * 0.8)}px`, fontFamily: 'monospace', color: '#88ff88'
        }).setScrollFactor(0).setDepth(101);
        this._updateBatteryUI();

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
        ctrlBg.fillRoundedRect(10, h - Math.floor(h * 0.035), Math.floor(w * 0.58), Math.floor(h * 0.03), 6);
        this.uiLayer.add(ctrlBg);

        this.add.text(20, h - Math.floor(h * 0.032),
            'WASD:이동  SPACE/클릭:공격  E:수색/탈출  F:손전등  TAB:인벤  M:맵  ESC:철수', {
            fontSize: `${Math.floor(fs * 0.6)}px`, fontFamily: 'monospace', color: '#555'
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

        this.drawMinimap();
        this._updatePhaseUI();
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

    _updateBatteryUI() {
        const pct = Math.ceil((this.flashlightBattery / this.maxBattery) * 100);
        const color = pct > 50 ? '#88ff88' : pct > 20 ? '#ffcc44' : '#ff4444';
        const state = this.flashlightOn ? 'ON' : 'OFF';
        this.batteryText.setText(`\u{1F526} ${state} ${pct}%`).setColor(color);
    }

    _updatePhaseUI() {
        if (!this.isNight) {
            const remaining = Math.max(0, Math.ceil(this.dayDuration - this.dayElapsed));
            const min = Math.floor(remaining / 60);
            const sec = remaining % 60;
            this.phaseText.setText(`\u{2600}\u{FE0F} Day  ${min}:${sec.toString().padStart(2, '0')}`);
            this.phaseText.setColor('#ffcc44');
        } else {
            const remaining = Math.max(0, Math.ceil(this.nightDuration - this.nightElapsed));
            const min = Math.floor(remaining / 60);
            const sec = remaining % 60;
            this.phaseText.setText(`\u{1F319} Night  ${min}:${sec.toString().padStart(2, '0')}`);
            this.phaseText.setColor(remaining < 60 ? '#ff4444' : '#8888ff');
        }
    }

    _updateNightTimerUI() {
        if (!this.isNight) return;
        const remaining = Math.max(0, this.nightDuration - this.nightElapsed);
        const ratio = remaining / this.nightDuration;
        const d = this.nightTimerData;

        this.nightTimerFill.clear();
        const color = ratio > 0.5 ? 0x4488ff : ratio > 0.25 ? 0xffaa44 : 0xff4444;
        this.nightTimerFill.fillStyle(color, 1);
        this.nightTimerFill.fillRect(d.x, d.y, Math.floor(d.w * ratio), d.h);

        const min = Math.floor(remaining / 60);
        const sec = Math.floor(remaining % 60);
        this.nightTimerText.setText(`\u{23F0} ${min}:${sec.toString().padStart(2, '0')} — Close`);
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
        const mmY = Math.floor(cam.height * 0.12);

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
                if (tile === TILE.FLOOR || tile === TILE.DOOR || tile === TILE.NIGHT_FLOOR) color = 0x555566;
                else if (tile === TILE.WALL || tile === TILE.NIGHT_WALL) color = 0x444455;
                else if (tile === TILE.ROAD) color = 0x666666;
                else if (tile === TILE.GRASS) color = 0x334433;
                else if (tile === TILE.EXIT) color = 0x4488ff;
                else if (tile === TILE.LOCKED_DOOR) color = this.isNight ? 0x4466cc : 0x444455;
                else if (tile >= TILE.BARREL) color = 0x886644;
                this.minimapGfx.fillStyle(color, 0.7);
                this.minimapGfx.fillRect(mmX + x * s, mmY + y * s, s, s);
            }
        }
        // Open buildings highlight (night)
        if (this.isNight && this.openBuildingIndices) {
            this.minimapGfx.lineStyle(1, 0xffcc44, 0.8);
            for (const idx of this.openBuildingIndices) {
                const b = this.buildings[idx];
                this.minimapGfx.strokeRect(mmX + b.x * s, mmY + b.y * s, b.w * s, b.h * s);
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

    // ══════════════════════════════════════════════════════
    // ── DAY / NIGHT CYCLE ─────────────────────────────────
    // ══════════════════════════════════════════════════════

    _updateDayNightCycle(delta) {
        const dt = delta / 1000; // seconds

        if (!this.isNight) {
            // ── DAY PHASE ──
            this.dayElapsed += dt;
            if (this.dayElapsed >= this.dayDuration && !this.nightTransitioning) {
                this._beginNightTransition();
            }
        } else if (!this.cityClosing) {
            // ── NIGHT PHASE ──
            this.nightElapsed += dt;
            this._updateNightTimerUI();

            // Battery drain
            if (this.flashlightOn && this.flashlightBattery > 0) {
                this.flashlightBattery = Math.max(0, this.flashlightBattery - this.batteryDrain * dt);
                this._updateBatteryUI();
                if (this.flashlightBattery <= 0) {
                    this.flashlightOn = false;
                    this.showMessage('\u{1F526} Battery dead!', 2000);
                    this._updateBatteryUI();
                }
            }

            // Night warnings
            const remaining = this.nightDuration - this.nightElapsed;
            if (remaining <= this.nightDuration * 0.5 && !this.nightWarningShown.half) {
                this.nightWarningShown.half = true;
                this.showMessage('\u{26A0}\u{FE0F} Half time remaining...', 2000);
            }
            if (remaining <= 60 && !this.nightWarningShown.minute) {
                this.nightWarningShown.minute = true;
                this.showMessage('\u{1F6A8} 1 minute! Head to extract!', 3000);
                this.cameras.main.shake(200, 0.003);
            }
            if (remaining <= 10 && !this.nightWarningShown.ten) {
                this.nightWarningShown.ten = true;
                this.showMessage('\u{1F6A8}\u{1F6A8}\u{1F6A8} 10 SECONDS!', 2000);
                this.cameras.main.shake(400, 0.008);
            }

            // City closes
            if (this.nightElapsed >= this.nightDuration) {
                this._cityClose();
            }
        }
    }

    _beginNightTransition() {
        this.nightTransitioning = true;

        // Full-screen darkening overlay
        const cam = this.cameras.main;
        const overlay = this.add.graphics().setScrollFactor(0).setDepth(300);
        overlay.fillStyle(0x000000, 0);

        this.showMessage('\u{1F319} Night falls... the city opens.', 3500);

        // Darken screen
        this.tweens.add({
            targets: { v: 0 }, v: 1, duration: 1500,
            onUpdate: (tween) => {
                const val = tween.getValue();
                overlay.clear();
                overlay.fillStyle(0x000011, val * 0.7);
                overlay.fillRect(0, 0, cam.width, cam.height);
            },
            onComplete: () => {
                // Switch to night
                this.isNight = true;
                this.nightElapsed = 0;

                // Change background color
                this.cameras.main.setBackgroundColor(this.zone.nightBgColor || 0x0a0a1a);

                // Apply tile changes (buildings "open")
                this._applyNightTileChanges();

                // Spawn night enemies
                this._spawnEnemyList(this.nightEnemyData);

                // Show night timer UI
                this.nightTimerBg.setAlpha(1);
                this.nightTimerBarBg.setAlpha(1);
                this.nightTimerFill.setAlpha(1);
                this.nightTimerText.setAlpha(1);

                // Show flashlight cone
                this.flashlightCone.setAlpha(0.15);

                // Reveal with flashlight
                this.revealAround(this.player.gx, this.player.gy, PLAYER_DATA.visionRadius);

                // Flash the screen briefly
                this.cameras.main.flash(800, 20, 20, 60);

                // Fade overlay back out
                this.tweens.add({
                    targets: { v: 0.7 }, v: 0, duration: 1000,
                    onUpdate: (tween) => {
                        overlay.clear();
                        overlay.fillStyle(0x000011, tween.getValue());
                        overlay.fillRect(0, 0, cam.width, cam.height);
                    },
                    onComplete: () => {
                        overlay.destroy();
                        this.nightTransitioning = false;
                    }
                });
            }
        });
    }

    _applyNightTileChanges() {
        const T = this.TILE_SIZE;
        for (const change of this.nightTileChanges) {
            const { x, y, nightTile } = change;
            this.mapGrid[y][x] = nightTile;

            // Redraw tile graphic
            const oldG = this.tileGraphics[y][x];
            if (oldG) {
                oldG.clear();
                const tc = TILE_COLORS[nightTile] || TILE_COLORS[TILE.FLOOR];
                this._drawTileGraphic(oldG, nightTile, tc, x * T, y * T, T);
            }

            // Add searchable icon if applicable
            const searchable = SEARCHABLE_TILES[nightTile];
            if (searchable && !this.tileIcons[`${x},${y}`]) {
                const icon = this.add.text(x * T + T / 2, y * T + T / 2, searchable.icon, {
                    fontSize: '14px'
                }).setOrigin(0.5).setDepth(2);
                this.tileLayer.add(icon);
                this.tileIcons[`${x},${y}`] = icon;
            }
        }

        // Add "OPEN" markers on opening buildings
        for (const idx of this.openBuildingIndices) {
            const b = this.buildings[idx];
            const marker = this.add.text(b.cx * T + T / 2, b.y * T - 6, '\u{1F514} OPEN', {
                fontSize: '10px', fontFamily: 'monospace', color: '#ffcc44',
                stroke: '#000', strokeThickness: 2
            }).setOrigin(0.5).setDepth(3);
            this.tileLayer.add(marker);
            this.tweens.add({
                targets: marker, alpha: 0.4, yoyo: true, repeat: -1, duration: 800
            });
        }
    }

    _revertNightTileChanges() {
        const T = this.TILE_SIZE;
        for (const change of this.nightTileChanges) {
            const { x, y, dayTile } = change;
            this.mapGrid[y][x] = dayTile;

            const oldG = this.tileGraphics[y][x];
            if (oldG) {
                oldG.clear();
                const tc = TILE_COLORS[dayTile] || TILE_COLORS[TILE.FLOOR];
                this._drawTileGraphic(oldG, dayTile, tc, x * T, y * T, T);
            }

            // Remove night icons
            const iconKey = `${x},${y}`;
            if (this.tileIcons[iconKey] && SEARCHABLE_TILES[change.nightTile] && !SEARCHABLE_TILES[dayTile]) {
                this.tileIcons[iconKey].destroy();
                delete this.tileIcons[iconKey];
            }
        }
    }

    _cityClose() {
        if (this.cityClosing) return;
        this.cityClosing = true;

        this.showMessage('\u{1F6A8} City is closing! Loot lost!', 4000);
        this.cameras.main.shake(600, 0.01);

        // Revert tiles
        this._revertNightTileChanges();

        // Force extract with partial loot loss
        this.time.delayedCall(2000, () => {
            // Lose half of inventory
            const lostCount = Math.ceil(this.inventory.length / 2);
            for (let i = 0; i < lostCount; i++) {
                this.inventory.pop();
            }
            const exitData = {
                inventory: [],
                stash: [...this.stash, ...this.inventory],
                gold: this.gold,
                safe: true,
                extracted: true,
                lootValue: this.inventory.reduce((s, id) => s + (ITEM_DATA[id]?.value || 0), 0)
            };
            this.cameras.main.fadeOut(800, 0, 0, 0);
            this.time.delayedCall(850, () => {
                game.scene.stop('ExpeditionScene');
                game.scene.start('SafeHouseScene', exitData);
            });
        });
    }

    // ── FLASHLIGHT VISUAL ──────────────────────────────────

    _drawFlashlightCone() {
        this.flashlightCone.clear();
        if (!this.isNight || !this.flashlightOn || this.flashlightBattery <= 0) return;

        const range = (PLAYER_DATA.flashlightRange + this.flashlightBonusRange) * this.TILE_SIZE;
        const halfArc = (PLAYER_DATA.flashlightArc / 2) * (Math.PI / 180);
        const angle = Math.atan2(this.playerFacing.y, this.playerFacing.x);

        this.flashlightCone.fillStyle(0xffffcc, 0.08);
        this.flashlightCone.beginPath();
        this.flashlightCone.moveTo(0, 0);
        const steps = 12;
        for (let i = 0; i <= steps; i++) {
            const a = angle - halfArc + (2 * halfArc * i / steps);
            this.flashlightCone.lineTo(Math.cos(a) * range, Math.sin(a) * range);
        }
        this.flashlightCone.closePath();
        this.flashlightCone.fill();

        // Bright center beam
        this.flashlightCone.fillStyle(0xffffee, 0.04);
        this.flashlightCone.beginPath();
        this.flashlightCone.moveTo(0, 0);
        const narrowArc = halfArc * 0.3;
        for (let i = 0; i <= 6; i++) {
            const a = angle - narrowArc + (2 * narrowArc * i / 6);
            this.flashlightCone.lineTo(Math.cos(a) * range * 0.8, Math.sin(a) * range * 0.8);
        }
        this.flashlightCone.closePath();
        this.flashlightCone.fill();
    }

    // ── UPDATE LOOP ────────────────────────────────────────

    update(time, delta) {
        if (this.playerDead) return;

        // Day/Night cycle
        if (!this.nightTransitioning) {
            this._updateDayNightCycle(delta);
        }
        this._updatePhaseUI();

        // Key polling
        if (Phaser.Input.Keyboard.JustDown(this.fKey)) {
            if (this.isNight && this.flashlightBattery > 0) {
                this.flashlightOn = !this.flashlightOn;
                this._updateBatteryUI();
                this._drawFlashlightCone();
                this.showMessage(this.flashlightOn ? '\u{1F526} Flashlight ON' : '\u{1F526} Flashlight OFF', 1000);
                this.revealAround(this.player.gx, this.player.gy, PLAYER_DATA.visionRadius);
            }
            return;
        }
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
                this.playerFacing = { x: dx, y: dy };
                this.tryMove(dx, dy);
            }
        }

        // Night fog re-application
        if (this.isNight) {
            this._updateNightFog();
            this._drawFlashlightCone();
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
        if (tile === TILE.WALL || tile === TILE.NIGHT_WALL || tile === TILE.WATER || tile === TILE.FURNITURE ||
            tile === TILE.BARREL || tile === TILE.LOCKER || tile === TILE.CRATE ||
            tile === TILE.MEDICAL || tile === TILE.COMPUTER) return;

        // Locked door: need lockpick or old_key
        if (tile === TILE.LOCKED_DOOR) {
            const hasKey = this.inventory.includes('old_key') || this.inventory.includes('lockpick');
            if (!hasKey) {
                this.showMessage('\u{1F512} Locked! Need a lockpick or key.', 1500);
                return;
            }
            // Consume lockpick/key
            const keyIdx = this.inventory.indexOf('lockpick');
            const oldKeyIdx = this.inventory.indexOf('old_key');
            if (keyIdx !== -1) this.inventory.splice(keyIdx, 1);
            else if (oldKeyIdx !== -1) this.inventory.splice(oldKeyIdx, 1);
            this.mapGrid[ny][nx] = TILE.DOOR;
            // Redraw tile
            const T = this.TILE_SIZE;
            const g = this.tileGraphics[ny][nx];
            if (g) {
                g.clear();
                const tc = TILE_COLORS[TILE.DOOR];
                this._drawTileGraphic(g, TILE.DOOR, tc, nx * T, ny * T, T);
            }
            this.showMessage('\u{1F513} Door unlocked!', 1500);
            this.updateInventoryCount();
            return;
        }

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
                this.revealAround(nx, ny, this.isNight ? PLAYER_DATA.nightVisionRadius : PLAYER_DATA.visionRadius);
                this.checkTileEvents(nx, ny);
                this.updateMinimap();
            }
        });
    }

    checkTileEvents(x, y) {
        const tile = this.mapGrid[y][x];

        if (tile === TILE.EXIT) {
            this.showMessage('\u{1F681} Extract point! Press E to extract');
        }

        // Use battery items automatically if found in inventory while at low battery
        // (No, this should be manual — just check for searchable prompts)

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
                if (adjTile === TILE.EXIT) {
                    this.searchPromptText.setText(`E: \u{1F681} Extract`).setAlpha(1);
                    foundSearchable = true;
                    break;
                }
                if (adjTile === TILE.LOCKED_DOOR) {
                    this.searchPromptText.setText(`\u{1F512} Locked Door`).setAlpha(1);
                    foundSearchable = true;
                    break;
                }
            }
        }

        if (!foundSearchable) this.searchPromptText.setAlpha(0);
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
        if (Math.random() < 0.5) return;
        if (Math.random() < 0.15) enemy.patrolDir = Math.floor(Math.random() * 4);

        const [ddx, ddy] = dirs[enemy.patrolDir];
        const nx = enemy.gx + ddx, ny = enemy.gy + ddy;
        if (nx < 1 || ny < 1 || nx >= this.mapW - 1 || ny >= this.mapH - 1) {
            enemy.patrolDir = (enemy.patrolDir + 1) % 4;
            return;
        }
        const tile = this.mapGrid[ny][nx];
        if (tile === TILE.WALL || tile === TILE.NIGHT_WALL || tile === TILE.WATER ||
            tile === TILE.FURNITURE || tile === TILE.EXIT) {
            enemy.patrolDir = (enemy.patrolDir + 1) % 4;
            return;
        }
        this._moveEnemy(enemy, nx, ny);
    }

    _enemyChaseStep(enemy, px, py, dirs) {
        let best = null, bestDist = Infinity;
        for (const [ddx, ddy] of dirs) {
            const nx = enemy.gx + ddx, ny = enemy.gy + ddy;
            if (nx < 0 || ny < 0 || nx >= this.mapW || ny >= this.mapH) continue;
            const tile = this.mapGrid[ny][nx];
            if (tile === TILE.WALL || tile === TILE.NIGHT_WALL || tile === TILE.WATER || tile === TILE.FURNITURE) continue;
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

        this.tweens.add({
            targets: this.player.container, scaleX: 1.15, scaleY: 1.15,
            yoyo: true, duration: 80
        });
    }

    enemyAttack(enemy, time) {
        if (time - enemy.lastAttackTime < enemy.data.attackCooldown) return;
        enemy.lastAttackTime = time;

        if (time - this.lastPlayerHitTime < this.INVULN_MS) return;

        const damage = Math.max(1, enemy.data.atk - this.player.def);
        this.takeDamage(damage);

        this.tweens.add({
            targets: enemy.container, scaleX: 1.2, scaleY: 1.2,
            yoyo: true, duration: 100
        });
    }

    takeDamage(amount) {
        this.lastPlayerHitTime = this.time.now;
        this.player.hp -= amount;

        DamagePopup.show(this, this.player.container.x, this.player.container.y - 20, amount, '#ff4444');

        this.cameras.main.shake(150, 0.005);

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

        this.tweens.add({
            targets: enemy.container, alpha: 0.4,
            yoyo: true, duration: 80
        });

        if (enemy.hp <= 0) {
            enemy.hp = 0;
            this.killEnemy(enemy);
        } else if (enemy.state === 'idle') {
            enemy.state = 'chase';
        }
    }

    killEnemy(enemy) {
        enemy.alive = false;
        enemy.state = 'dead';

        DamagePopup.showText(this, enemy.container.x, enemy.container.y - 10, 'KILLED', '#ff8844');

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

        this.tweens.add({
            targets: enemy.container, alpha: 0, duration: 500,
            onComplete: () => enemy.container.setVisible(false)
        });
    }

    playerDeath() {
        this.playerDead = true;
        this.showMessage('You died...', 3000);

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

        if (tile === TILE.EXIT) {
            this.extractSuccess();
            return;
        }

        const searchable = SEARCHABLE_TILES[tile];
        if (searchable && !this.searched[`${x},${y}`]) {
            this.searchContainer(x, y, searchable);
            return;
        }

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
            if (adjTile === TILE.EXIT) {
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

        // Use night loot tables for night-placed containers
        let poolName = searchable.lootPool;
        if (this.isNight && this.nightTileChanges.some(c => c.x === x && c.y === y)) {
            // Night-opened containers use night-specific pools
            const nightPools = {
                'locker': 'night_shelf',
                'crate': 'night_safe',
                'medical': 'night_medical'
            };
            poolName = nightPools[poolName] || poolName;
        }

        const lootTable = LOOT_TABLE[poolName] || LOOT_TABLE.common_crate;
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

        // Battery items: auto-recharge if found
        const batteryCount = found.filter(id => id === 'battery').length;
        if (batteryCount > 0) {
            this.flashlightBattery = Math.min(this.maxBattery, this.flashlightBattery + batteryCount * 25);
            this._updateBatteryUI();
        }

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
        this.showMessage('\u{2705} Extract successful!');
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
                    equipment: 'Equipment', valuable: 'Valuables', key: 'Keys', tool: 'Tools'
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
                if (tile === TILE.FLOOR || tile === TILE.DOOR || tile === TILE.NIGHT_FLOOR) c = 0x666677;
                else if (tile === TILE.WALL || tile === TILE.NIGHT_WALL) c = 0x555566;
                else if (tile === TILE.ROAD) c = 0x777777;
                else if (tile === TILE.GRASS) c = 0x445544;
                else if (tile === TILE.EXIT) c = 0x4488ff;
                else if (tile === TILE.LOCKED_DOOR) c = 0x4466cc;
                else if (tile >= TILE.BARREL) c = 0xaa8855;
                mg.fillStyle(c, 0.9);
                mg.fillRect(ox + x * scale, oy + y * scale, Math.ceil(scale), Math.ceil(scale));
            }
        }

        // Highlight open buildings
        if (this.isNight) {
            mg.lineStyle(2, 0xffcc44, 0.8);
            for (const idx of this.openBuildingIndices) {
                const b = this.buildings[idx];
                mg.strokeRect(ox + b.x * scale, oy + b.y * scale, b.w * scale, b.h * scale);
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

        const phaseLabel = this.isNight ? '\u{1F319}' : '\u{2600}\u{FE0F}';
        this.fullMapPanel.add(this.add.text(cam.width / 2, 15, `${phaseLabel} ${this.zone.name}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#fff'
        }).setOrigin(0.5).setScrollFactor(0));

        this.fullMapPanel.add(this.add.text(cam.width / 2, cam.height - 15, 'M: close', {
            fontSize: '10px', fontFamily: 'monospace', color: '#666'
        }).setOrigin(0.5).setScrollFactor(0));
    }
}
