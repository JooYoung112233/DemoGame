// HUD overlay — time, HP, weight, prompts
class HUD {
    constructor(scene) {
        this.scene = scene;
        this.container = scene.add.container(0, 0).setScrollFactor(0).setDepth(100);

        // Top bar background
        const topBg = scene.add.graphics();
        topBg.fillStyle(0x000000, 0.7);
        topBg.fillRect(0, 0, 1280, 40);
        this.container.add(topBg);

        // Time display
        this.timeText = scene.add.text(640, 20, '', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.container.add(this.timeText);

        // HP bar
        this.hpBg = scene.add.graphics();
        this.container.add(this.hpBg);
        this.hpText = scene.add.text(20, 20, '', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffffff'
        }).setOrigin(0, 0.5);
        this.container.add(this.hpText);

        // Weight display
        this.weightText = scene.add.text(1260, 20, '', {
            fontSize: '14px', fontFamily: 'monospace', color: '#cccccc'
        }).setOrigin(1, 0.5);
        this.container.add(this.weightText);

        // Center prompt (e.g., "Press E to search")
        this.promptText = scene.add.text(640, 680, '', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44',
            backgroundColor: '#00000088', padding: { x: 12, y: 6 }
        }).setOrigin(0.5).setAlpha(0);
        this.container.add(this.promptText);

        // Search progress bar
        this.searchBarBg = scene.add.graphics().setAlpha(0);
        this.searchBarFill = scene.add.graphics().setAlpha(0);
        this.container.add(this.searchBarBg);
        this.container.add(this.searchBarFill);

        // Night warning
        this.warningText = scene.add.text(640, 80, '', {
            fontSize: '24px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setAlpha(0);
        this.container.add(this.warningText);

        // Minimap (bottom-left)
        this.minimap = scene.add.graphics().setScrollFactor(0).setDepth(101);
        this.minimapSize = 120;
        this.minimapX = 10;
        this.minimapY = 720 - this.minimapSize - 10;
    }

    update(timeSystem, player, inventory, lootSystem, enemies) {
        // Time
        this.timeText.setText(timeSystem.getTimeString());
        this.timeText.setColor(timeSystem.isNight ? '#8888ff' : '#ffdd44');

        // HP bar
        this.hpBg.clear();
        const hpW = 160, hpH = 12, hpX = 20, hpY = 14;
        this.hpBg.fillStyle(0x333333, 1).fillRect(hpX, hpY, hpW, hpH);
        const hpRatio = player.hp / player.maxHp;
        const hpColor = hpRatio > 0.5 ? 0x44cc44 : hpRatio > 0.25 ? 0xcccc44 : 0xcc4444;
        this.hpBg.fillStyle(hpColor, 1).fillRect(hpX, hpY, hpW * hpRatio, hpH);
        this.hpText.setText(`❤️ ${player.hp}/${player.maxHp}`).setPosition(hpX + hpW + 8, 20);

        // Weight
        this.weightText.setText(
            `🎒 ${inventory.currentWeight.toFixed(1)}/${inventory.maxWeight}kg (${inventory.getItemCount()}개)`
        );

        // Prompts
        const nearLoot = lootSystem.getNearbyLootSpot(player.tileRow, player.tileCol);
        const nearEscape = lootSystem.isNearEscape(player.tileRow, player.tileCol);
        const inSafe = lootSystem.isInSafeZone(player.tileRow, player.tileCol);

        if (lootSystem.isSearching) {
            this.promptText.setAlpha(0);
            this._showSearchBar(lootSystem.searchProgress);
        } else if (nearLoot) {
            this.promptText.setText('[ E ] 수색하기').setAlpha(1);
            this._hideSearchBar();
        } else if (nearEscape) {
            this.promptText.setText('[ E ] 탈출하기').setAlpha(1);
            this._hideSearchBar();
        } else if (inSafe) {
            this.promptText.setText('🏠 안전지대').setAlpha(0.6);
            this._hideSearchBar();
        } else {
            this.promptText.setAlpha(0);
            this._hideSearchBar();
        }

        // Minimap
        this._drawMinimap(player, enemies);
    }

    showWarning(text) {
        this.warningText.setText(text).setAlpha(1);
        this.scene.tweens.add({
            targets: this.warningText, alpha: 0,
            duration: 2000, ease: 'Power2'
        });
    }

    _showSearchBar(progress) {
        const w = 200, h = 8, x = 540, y = 660;
        this.searchBarBg.clear().setAlpha(1);
        this.searchBarBg.fillStyle(0x333333, 1).fillRect(x, y, w, h);
        this.searchBarFill.clear().setAlpha(1);
        this.searchBarFill.fillStyle(0x44ccff, 1).fillRect(x, y, w * progress, h);
    }

    _hideSearchBar() {
        this.searchBarBg.setAlpha(0);
        this.searchBarFill.setAlpha(0);
    }

    _drawMinimap(player, enemies) {
        const g = this.minimap;
        g.clear();

        const mx = this.minimapX;
        const my = this.minimapY;
        const ms = this.minimapSize;
        const ts = ms / MAP_WIDTH; // tile size on minimap

        // Background
        g.fillStyle(0x000000, 0.6);
        g.fillRect(mx, my, ms, ms);

        // Tiles
        for (let r = 0; r < MAP_HEIGHT; r++) {
            for (let c = 0; c < MAP_WIDTH; c++) {
                const tile = CITY_MAP[r][c];
                if (tile === TILE_TYPES.WALL) {
                    g.fillStyle(0x555555, 0.8);
                    g.fillRect(mx + c * ts, my + r * ts, ts, ts);
                } else if (tile === TILE_TYPES.SAFE) {
                    g.fillStyle(0x44aa44, 0.8);
                    g.fillRect(mx + c * ts, my + r * ts, ts, ts);
                } else if (tile === TILE_TYPES.ESCAPE) {
                    g.fillStyle(0x4444aa, 0.8);
                    g.fillRect(mx + c * ts, my + r * ts, ts, ts);
                }
            }
        }

        // Enemies (red dots)
        for (const enemy of enemies) {
            if (enemy.dead) continue;
            g.fillStyle(0xff4444, 1);
            g.fillCircle(mx + enemy.col * ts, my + enemy.row * ts, 2);
        }

        // Player (white dot)
        g.fillStyle(0xffffff, 1);
        g.fillCircle(mx + player.col * ts, my + player.row * ts, 3);

        // Border
        g.lineStyle(1, 0x666666, 0.8);
        g.strokeRect(mx, my, ms, ms);
    }

    destroy() {
        this.container.destroy();
        this.minimap.destroy();
    }
}
