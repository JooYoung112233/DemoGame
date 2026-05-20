class CityScene extends Phaser.Scene {
    constructor() {
        super('CityScene');
    }

    create() {
        const W = 1280, H = 720;

        // Systems
        this.timeSystem = new TimeSystem();
        this.visionSystem = new VisionSystem();
        this.inventory = new InventorySystem(5, 4, 30);
        this.lootSystem = new LootSystem();
        this.combatSystem = new CombatSystem();

        // Player
        this.player = new Player(PLAYER_START.row, PLAYER_START.col);

        // Enemies (start empty, spawn at night)
        this.enemies = [];

        // Input
        this.cursors = this.input.keyboard.createCursorKeys();
        this.wasd = this.input.keyboard.addKeys('W,A,S,D');
        this.keyE = this.input.keyboard.addKey('E');
        this.keyTab = this.input.keyboard.addKey('TAB');
        this.keyEsc = this.input.keyboard.addKey('ESC');

        // Camera setup — follow player
        this.cameras.main.setBackgroundColor('#111111');
        const mapPixelW = MAP_WIDTH * TILE_SIZE;
        const mapPixelH = MAP_HEIGHT * TILE_SIZE;
        this.cameras.main.setBounds(0, 0, mapPixelW, mapPixelH);

        // Tile layer
        this.tileGraphics = this.add.graphics().setDepth(0);
        this.tileIcons = this.add.container(0, 0).setDepth(1);

        // Fog layer
        this.fogGraphics = this.add.graphics().setDepth(50);

        // Entity layer
        this.entityLayer = this.add.container(0, 0).setDepth(10);

        // Night overlay
        this.nightOverlay = this.add.graphics().setScrollFactor(0).setDepth(90);

        // Draw static map tiles
        this._drawMap();

        // UI (above everything)
        this.hud = new HUD(this);
        this.inventoryUI = new InventoryUI(this, this.inventory);
        this.lootUI = new LootUI(this, this.inventory);

        // Input handlers
        this.keyTab.on('down', () => {
            if (this.lootUI.isOpen) return;
            this.inventoryUI.toggle();
        });

        this.keyEsc.on('down', () => {
            if (this.lootUI.isOpen) this.lootUI.close();
            else if (this.inventoryUI.isOpen) this.inventoryUI.close();
        });

        this.keyE.on('down', () => this._handleInteraction());

        // Click to attack
        this.input.on('pointerdown', (pointer) => {
            if (this.inventoryUI.isOpen || this.lootUI.isOpen) return;
            this._handleAttack(pointer);
        });

        // Damage flash
        this.damageFlash = this.add.graphics().setScrollFactor(0).setDepth(95).setAlpha(0);

        // Track game state
        this.gameOver = false;
        this.playerDamageTimer = 0;
    }

    update(time, delta) {
        if (this.gameOver) return;

        // Pause gameplay when UI is open
        const uiOpen = this.inventoryUI.isOpen || this.lootUI.isOpen;

        if (!uiOpen) {
            // Update player
            this.player.update(delta, this.cursors, this.wasd);

            // Update time
            const timeEvent = this.timeSystem.update(delta);
            this._handleTimeEvent(timeEvent);

            // Update enemies
            const playerVisible = !this.lootSystem.isInSafeZone(this.player.tileRow, this.player.tileCol);
            for (const enemy of this.enemies) {
                enemy.update(delta, this.player.row, this.player.col, playerVisible);
            }

            // Remove dead enemies
            this.enemies = this.enemies.filter(e => !e.dead);

            // Combat
            this.combatSystem.update(delta);
            this._checkEnemyContact(delta);

            // Loot search
            if (this.lootSystem.isSearching) {
                const items = this.lootSystem.updateSearch(delta);
                if (items) {
                    this.lootUI.open(items);
                }
                // Cancel if player moves
                if (this.player.isMoving) {
                    this.lootSystem.cancelSearch();
                }
            }
        }

        // Update vision
        this.visionSystem.update(
            this.player.tileRow, this.player.tileCol,
            this.player.facingAngle,
            this.timeSystem.isNight,
            this.timeSystem.transitionAlpha
        );

        // Camera follow
        const px = this.player.col * TILE_SIZE + TILE_SIZE / 2;
        const py = this.player.row * TILE_SIZE + TILE_SIZE / 2;
        this.cameras.main.centerOn(px, py);

        // Render
        this._drawFog();
        this._drawEntities();
        this._drawNightOverlay();
        this.hud.update(this.timeSystem, this.player, this.inventory, this.lootSystem, this.enemies);

        // Check death
        if (this.player.isDead) {
            this._endGame(false);
        }
    }

    _drawMap() {
        const g = this.tileGraphics;
        g.clear();

        for (let r = 0; r < MAP_HEIGHT; r++) {
            for (let c = 0; c < MAP_WIDTH; c++) {
                const tile = CITY_MAP[r][c];
                const color = TILE_COLORS[tile] || TILE_COLORS[0];
                const x = c * TILE_SIZE;
                const y = r * TILE_SIZE;

                g.fillStyle(color.fill, 1);
                g.fillRect(x, y, TILE_SIZE, TILE_SIZE);
                g.lineStyle(1, color.stroke, 0.3);
                g.strokeRect(x, y, TILE_SIZE, TILE_SIZE);

                // Add icons for special tiles
                if (color.icon) {
                    const iconText = this.add.text(x + TILE_SIZE / 2, y + TILE_SIZE / 2, color.icon, {
                        fontSize: '18px'
                    }).setOrigin(0.5);
                    this.tileIcons.add(iconText);
                }
            }
        }
    }

    _drawFog() {
        const fog = this.fogGraphics;
        fog.clear();

        const cam = this.cameras.main;
        const startCol = Math.max(0, Math.floor(cam.scrollX / TILE_SIZE) - 1);
        const startRow = Math.max(0, Math.floor(cam.scrollY / TILE_SIZE) - 1);
        const endCol = Math.min(MAP_WIDTH, startCol + Math.ceil(cam.width / TILE_SIZE) + 2);
        const endRow = Math.min(MAP_HEIGHT, startRow + Math.ceil(cam.height / TILE_SIZE) + 2);

        for (let r = startRow; r < endRow; r++) {
            for (let c = startCol; c < endCol; c++) {
                const alpha = this.visionSystem.getAlpha(r, c);
                if (alpha < 1) {
                    const fogAlpha = 1 - alpha;
                    fog.fillStyle(0x000000, fogAlpha * 0.85);
                    fog.fillRect(c * TILE_SIZE, r * TILE_SIZE, TILE_SIZE, TILE_SIZE);
                }
            }
        }
    }

    _drawEntities() {
        this.entityLayer.removeAll(true);

        // Draw enemies
        for (const enemy of this.enemies) {
            if (enemy.dead) continue;
            // Only draw if visible
            if (!this.visionSystem.isVisible(Math.round(enemy.row), Math.round(enemy.col))) continue;

            const ex = enemy.col * TILE_SIZE + TILE_SIZE / 2;
            const ey = enemy.row * TILE_SIZE + TILE_SIZE / 2;

            const g = this.add.graphics();
            g.fillStyle(enemy.color, 1);
            g.fillCircle(ex, ey, 10);
            g.lineStyle(2, 0xff4444, 0.8);
            g.strokeCircle(ex, ey, 10);
            this.entityLayer.add(g);

            // Enemy icon
            const icon = this.add.text(ex, ey - 2, ENEMY_DATA[enemy.type].icon, {
                fontSize: '14px'
            }).setOrigin(0.5);
            this.entityLayer.add(icon);

            // HP bar if damaged
            if (enemy.hp < enemy.maxHp) {
                const barW = 20;
                const hpRatio = enemy.hp / enemy.maxHp;
                const barG = this.add.graphics();
                barG.fillStyle(0x333333, 1).fillRect(ex - barW / 2, ey - 18, barW, 3);
                barG.fillStyle(0xff4444, 1).fillRect(ex - barW / 2, ey - 18, barW * hpRatio, 3);
                this.entityLayer.add(barG);
            }

            // Alert indicator
            if (enemy.state === 'chase') {
                const alertText = this.add.text(ex, ey - 22, '❗', { fontSize: '12px' }).setOrigin(0.5);
                this.entityLayer.add(alertText);
            }
        }

        // Draw player
        const px = this.player.col * TILE_SIZE + TILE_SIZE / 2;
        const py = this.player.row * TILE_SIZE + TILE_SIZE / 2;

        const pg = this.add.graphics();
        const flashAlpha = this.player.invincibleTimer > 0 ? 0.5 : 1;
        pg.fillStyle(0x44aaff, flashAlpha);
        pg.fillCircle(px, py, 12);
        pg.lineStyle(2, 0x88ccff, flashAlpha);
        pg.strokeCircle(px, py, 12);

        // Facing indicator
        const fx = px + Math.cos(this.player.facingAngle) * 14;
        const fy = py + Math.sin(this.player.facingAngle) * 14;
        pg.fillStyle(0xffffff, 0.8);
        pg.fillCircle(fx, fy, 3);
        this.entityLayer.add(pg);

        // Player icon
        const playerIcon = this.add.text(px, py - 2, '🧑', { fontSize: '14px' }).setOrigin(0.5)
            .setAlpha(flashAlpha);
        this.entityLayer.add(playerIcon);
    }

    _drawNightOverlay() {
        const overlay = this.nightOverlay;
        overlay.clear();
        if (this.timeSystem.transitionAlpha > 0) {
            overlay.fillStyle(0x000022, this.timeSystem.transitionAlpha * 0.3);
            overlay.fillRect(0, 0, 1280, 720);
        }
    }

    _handleTimeEvent(event) {
        if (!event) return;

        switch (event) {
            case 'night_start':
                this.hud.showWarning('🌙 밤이 왔다... 조심하라');
                break;
            case 'day_start':
                this.hud.showWarning('☀️ 날이 밝았다');
                // Remove enemies at dawn
                this.enemies = [];
                break;
            case 'spawn_wave1':
                this._spawnEnemies(NIGHT_SPAWNS.wave1);
                break;
            case 'spawn_wave2':
                this._spawnEnemies(NIGHT_SPAWNS.wave2);
                break;
            case 'spawn_wave3':
                this._spawnEnemies(NIGHT_SPAWNS.wave3);
                break;
        }
    }

    _spawnEnemies(waveConfig) {
        const spawnable = getSpawnableTiles();
        for (const cfg of waveConfig) {
            for (let i = 0; i < cfg.count; i++) {
                // Find a spawn point far enough from player
                let attempts = 50;
                while (attempts-- > 0) {
                    const spot = spawnable[Math.floor(Math.random() * spawnable.length)];
                    const dist = Math.sqrt(
                        Math.pow(spot.row - this.player.row, 2) +
                        Math.pow(spot.col - this.player.col, 2)
                    );
                    if (dist >= cfg.minDist) {
                        this.enemies.push(new Enemy(cfg.type, spot.row, spot.col));
                        break;
                    }
                }
            }
        }
    }

    _handleInteraction() {
        if (this.lootUI.isOpen || this.inventoryUI.isOpen) return;

        // Check escape
        if (this.lootSystem.isNearEscape(this.player.tileRow, this.player.tileCol)) {
            this._endGame(true);
            return;
        }

        // Check loot
        const lootSpot = this.lootSystem.getNearbyLootSpot(this.player.tileRow, this.player.tileCol);
        if (lootSpot && !this.lootSystem.isSearching) {
            this.lootSystem.startSearch(lootSpot);
        }
    }

    _handleAttack(pointer) {
        // Convert pointer to world coords isn't needed - just attack nearby enemies
        const result = this.combatSystem.playerAttack(this.player.row, this.player.col, this.enemies);
        if (result && result.hit) {
            // Hit feedback
            this.cameras.main.shake(50, 0.003);
        }
    }

    _checkEnemyContact(delta) {
        // In safe zone = no damage
        if (this.lootSystem.isInSafeZone(this.player.tileRow, this.player.tileCol)) return;

        this.playerDamageTimer -= delta / 1000;
        if (this.playerDamageTimer > 0) return;

        const contact = this.combatSystem.checkEnemyContact(this.player.row, this.player.col, this.enemies);
        if (contact) {
            const dmg = this.player.takeDamage(contact.damage);
            if (dmg > 0) {
                this.playerDamageTimer = 1.0; // 1 second between contact damage
                this.cameras.main.shake(100, 0.01);
                this._showDamageFlash();
            }
        }
    }

    _showDamageFlash() {
        this.damageFlash.clear();
        this.damageFlash.fillStyle(0xff0000, 0.3);
        this.damageFlash.fillRect(0, 0, 1280, 720);
        this.damageFlash.setAlpha(1);
        this.tweens.add({
            targets: this.damageFlash, alpha: 0, duration: 300
        });
    }

    _endGame(survived) {
        this.gameOver = true;
        this.timeSystem.paused = true;

        this.scene.start('ResultScene', {
            survived,
            items: this.inventory.getAllItems(),
            totalValue: this.inventory.getTotalValue(),
            timeElapsed: this.timeSystem.elapsed,
        });
    }
}
