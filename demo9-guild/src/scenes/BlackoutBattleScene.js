class BlackoutBattleScene extends Phaser.Scene {
    constructor() { super('BlackoutBattleScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.party = data.party;
        this.loot = data.loot || [];
        this.totalGold = data.totalGold || 0;
        this.totalXp = data.totalXp || 0;
        this.zoneLevel = this.gameState.zoneLevel['blackout'] || 1;
    }

    create() {
        this.tension = 0;
        this.roomsVisited = 0;
        this.moveCount = 0;
        this.maxMoves = 15 + this.zoneLevel * 5;
        this.inCombat = false;
        this.hasKey = false;
        this.casualties = [];
        this.speedMultiplier = parseInt(localStorage.getItem('demo9_speed') || '1', 10) || 1;

        // Combat state
        this.allies = [];
        this.enemies = [];
        this.allUnits = [];
        this.battleOver = false;
        this.battleTime = 0;

        // UI layer groups
        this._gridObjects = [];
        this._hudObjects = [];
        this._overlayObjects = [];
        this._combatObjects = [];
        this._partyPanelObjects = [];
        this._inventoryBarObjects = [];

        this._drawBackground();
        this._generateGrid();
        this._drawHUD();
        this._drawGrid();
        this._drawPartyPanel();
        this._drawInventoryBar();
        this._showEntryAnnounce();
    }

    update(time, delta) {
        if (!this.inCombat) return;
        if (this.battleOver) return;

        const dt = delta * this.speedMultiplier;
        this.battleTime += dt;

        for (const unit of this.allUnits) {
            if (unit.alive) unit.update(dt, this.allUnits);
        }

        // Update ally HP snapshot
        this._lastAllyHp = {};
        this.allies.forEach(u => {
            if (u.alive && u.mercId) {
                this._lastAllyHp[u.mercId] = u.hp;
            }
        });
    }

    // ==================== GRID GENERATION ====================

    _generateGrid() {
        this.grid = [];
        for (let y = 0; y < 5; y++) {
            this.grid[y] = [];
            for (let x = 0; x < 5; x++) {
                this.grid[y][x] = { type: 'unknown', visited: false, revealed: false, depth: x };
            }
        }
        // Entrance
        this.grid[2][0] = { type: 'entrance', visited: true, revealed: true, depth: 0 };
        // Exit
        this.exitY = Phaser.Math.Between(0, 4);
        this.grid[this.exitY][4] = { type: 'exit', visited: false, revealed: false, depth: 4 };
        // Locked room
        const lockX = Phaser.Math.Between(2, 3);
        const lockY = Phaser.Math.Between(0, 4);
        if (!(lockX === 4 && lockY === this.exitY)) {
            this.grid[lockY][lockX] = { type: 'locked', visited: false, revealed: false, depth: lockX };
        }
        // Player start
        this.playerX = 0;
        this.playerY = 2;
        this._revealAdjacent(0, 2);
    }

    _revealAdjacent(x, y) {
        const dirs = [[0, -1], [0, 1], [-1, 0], [1, 0]];
        for (const [dx, dy] of dirs) {
            const nx = x + dx, ny = y + dy;
            if (nx >= 0 && nx < 5 && ny >= 0 && ny < 5) {
                this.grid[ny][nx].revealed = true;
            }
        }
    }

    _rollRoomType(depth) {
        const t = this.tension;
        const weights = {
            empty:  Math.max(5, 30 - t * 2),
            enemy:  25 + t * 5,
            item:   Math.max(5, 25 - t * 2),
            trap:   10 + t * 3,
            rest:   5
        };
        const total = Object.values(weights).reduce((s, v) => s + v, 0);
        let roll = Math.random() * total;
        for (const [type, w] of Object.entries(weights)) {
            roll -= w;
            if (roll <= 0) return type;
        }
        return 'empty';
    }

    // ==================== BACKGROUND ====================

    _drawBackground() {
        const gfx = this.add.graphics();
        gfx.fillGradientStyle(0x0a0a12, 0x0a0a12, 0x0e0a1a, 0x0e0a1a);
        gfx.fillRect(0, 0, 1280, 720);

        // Purple fog particles
        for (let i = 0; i < 15; i++) {
            const fx = Phaser.Math.Between(50, 1230);
            const fy = Phaser.Math.Between(50, 670);
            gfx.fillStyle(0x8844ff, 0.03);
            gfx.fillCircle(fx, fy, Phaser.Math.Between(20, 60));
        }

        // Subtle grid floor lines
        gfx.lineStyle(1, 0x221133, 0.15);
        for (let i = 0; i < 8; i++) {
            gfx.lineBetween(0, 100 + i * 80, 1280, 100 + i * 80);
        }
    }

    // ==================== HUD ====================

    _drawHUD() {
        this._clearGroup(this._hudObjects);

        const _h = (obj) => { this._hudObjects.push(obj); return obj; };

        // Top bar background
        _h(this.add.rectangle(640, 30, 1280, 60, 0x0a0a12, 0.85).setDepth(100));

        // Title
        _h(this.add.text(30, 12, '🔦 Blackout', {
            fontSize: '16px', fontFamily: 'monospace', color: '#8844ff', fontStyle: 'bold'
        }).setDepth(101));

        // Tension bar
        const tensionNames = ['평온', '불안', '경계', '위험', '극도위험', '공포'];
        const tensionColors = [0x44cc44, 0x88cc44, 0xcccc44, 0xff8844, 0xff4444, 0xff0022];
        const tensionTextColors = ['#44cc44', '#88cc44', '#cccc44', '#ff8844', '#ff4444', '#ff0022'];

        _h(this.add.text(200, 10, '긴장도:', {
            fontSize: '11px', fontFamily: 'monospace', color: '#886688'
        }).setDepth(101));

        const barX = 270, barY = 16, barW = 200, barH = 10;
        _h(this.add.rectangle(barX + barW / 2, barY + barH / 2, barW, barH, 0x221133).setDepth(101));
        const fillW = Math.min(barW, (this.tension / 5) * barW);
        if (fillW > 0) {
            const tColor = tensionColors[Math.min(this.tension, 5)];
            _h(this.add.rectangle(barX + fillW / 2, barY + barH / 2, fillW, barH, tColor).setDepth(102));
        }

        const tName = tensionNames[Math.min(this.tension, 5)];
        const tTextColor = tensionTextColors[Math.min(this.tension, 5)];
        _h(this.add.text(480, 10, `Lv.${this.tension} ${tName}`, {
            fontSize: '12px', fontFamily: 'monospace', color: tTextColor, fontStyle: 'bold'
        }).setDepth(101));

        // Gold
        this.goldText = _h(this.add.text(620, 10, `💰 ${this.totalGold}G`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44'
        }).setDepth(101));

        // Weight
        const weight = this._getWeight();
        this.weightText = _h(this.add.text(720, 10, `무게: ${weight}/40kg`, {
            fontSize: '12px', fontFamily: 'monospace', color: weight > 32 ? '#ff4444' : weight > 24 ? '#ffaa44' : '#88aa88'
        }).setDepth(101));

        // Moves
        this.moveText = _h(this.add.text(860, 10, `이동: ${this.moveCount}/${this.maxMoves}`, {
            fontSize: '12px', fontFamily: 'monospace', color: this.moveCount > this.maxMoves * 0.8 ? '#ff4444' : '#886688'
        }).setDepth(101));

        // Key indicator
        if (this.hasKey) {
            _h(this.add.text(1000, 10, '🔑 열쇠 보유', {
                fontSize: '11px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
            }).setDepth(101));
        }

        // Zone level
        _h(this.add.text(30, 35, `구역 Lv.${this.zoneLevel}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#665577'
        }).setDepth(101));

        // Tension darkness overlay
        if (this.tension > 0) {
            const darkness = _h(this.add.rectangle(640, 360, 1280, 720, 0x000000,
                Math.min(0.3, this.tension * 0.06)).setDepth(2));
            this.tweens.add({ targets: darkness, alpha: darkness.alpha * 0.6, duration: 2000, yoyo: true, repeat: -1 });
        }
    }

    // ==================== GRID DRAWING ====================

    _drawGrid() {
        this._clearGroup(this._gridObjects);

        const cellSize = 90;
        const gap = 6;
        const totalW = 5 * cellSize + 4 * gap;
        const totalH = 5 * cellSize + 4 * gap;
        const startX = 440 - totalW / 2;
        const startY = 340 - totalH / 2;

        const _g = (obj) => { this._gridObjects.push(obj); return obj; };

        const roomIcons = {
            unknown: '?', entrance: '🏠', exit: '🚪', empty: '·',
            enemy: '💀', item: '💎', trap: '⚠', rest: '😴', locked: '🔒'
        };
        const roomColors = {
            unknown: 0x111122, entrance: 0x223344, exit: 0x224422,
            empty: 0x1a1a2a, enemy: 0x331122, item: 0x222233,
            trap: 0x332211, rest: 0x112233, locked: 0x222200
        };
        const roomBorderColors = {
            unknown: 0x222233, entrance: 0x446688, exit: 0x44aa44,
            empty: 0x333344, enemy: 0x884444, item: 0x4488ff,
            trap: 0xff8844, rest: 0x4488cc, locked: 0xaaaa44
        };

        for (let y = 0; y < 5; y++) {
            for (let x = 0; x < 5; x++) {
                const room = this.grid[y][x];
                const cx = startX + x * (cellSize + gap) + cellSize / 2;
                const cy = startY + y * (cellSize + gap) + cellSize / 2;

                const isPlayer = (x === this.playerX && y === this.playerY);
                const isAdjacent = this._isAdjacent(x, y, this.playerX, this.playerY);
                const canMove = isAdjacent && !this.inCombat && room.revealed;

                // Cell background
                const gfx = _g(this.add.graphics().setDepth(10));

                if (!room.revealed) {
                    // Unrevealed
                    gfx.fillStyle(0x111122, 1);
                    gfx.fillRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);
                    gfx.lineStyle(1, 0x222233, 0.5);
                    gfx.strokeRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);
                    _g(this.add.text(cx, cy, '?', {
                        fontSize: '28px', fontFamily: 'monospace', color: '#333344'
                    }).setOrigin(0.5).setDepth(11));
                } else if (room.visited && room.type !== 'entrance') {
                    // Visited
                    gfx.fillStyle(0x0e0e1a, 1);
                    gfx.fillRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);
                    gfx.lineStyle(1, 0x222233, 0.4);
                    gfx.strokeRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);
                    _g(this.add.text(cx, cy - 8, roomIcons[room.type] || '·', {
                        fontSize: '20px', fontFamily: 'monospace', color: '#333344'
                    }).setOrigin(0.5).setDepth(11));
                    _g(this.add.text(cx, cy + 18, '탐색됨', {
                        fontSize: '9px', fontFamily: 'monospace', color: '#333344'
                    }).setOrigin(0.5).setDepth(11));
                } else {
                    // Revealed but unvisited (or entrance/current)
                    const bgColor = roomColors[room.type] || 0x1a1a2a;
                    const borderColor = isPlayer ? 0x8844ff : (roomBorderColors[room.type] || 0x333344);
                    const borderWidth = isPlayer ? 3 : (canMove ? 2 : 1);
                    const borderAlpha = isPlayer ? 1 : (canMove ? 0.8 : 0.5);

                    gfx.fillStyle(bgColor, isPlayer ? 0.9 : 0.7);
                    gfx.fillRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);
                    gfx.lineStyle(borderWidth, borderColor, borderAlpha);
                    gfx.strokeRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);

                    // Room icon
                    const icon = room.type === 'unknown' ? '?' : roomIcons[room.type] || '?';
                    const iconColor = isPlayer ? '#bb88ff' : '#aaaacc';
                    _g(this.add.text(cx, cy - 8, icon, {
                        fontSize: '24px', fontFamily: 'monospace', color: iconColor
                    }).setOrigin(0.5).setDepth(11));

                    // Player marker
                    if (isPlayer) {
                        _g(this.add.text(cx, cy + 22, '👤', {
                            fontSize: '16px'
                        }).setOrigin(0.5).setDepth(12));

                        // Purple glow
                        const glow = _g(this.add.circle(cx, cy, cellSize / 2 + 4, 0x8844ff, 0.08).setDepth(9));
                        this.tweens.add({ targets: glow, alpha: 0.15, duration: 1000, yoyo: true, repeat: -1 });
                    }

                    // Room type label for revealed rooms
                    if (room.type !== 'unknown' && room.type !== 'entrance' && !isPlayer) {
                        const typeNames = {
                            exit: '출구', enemy: '적', item: '보물', trap: '함정', rest: '휴식', locked: '잠김'
                        };
                        if (typeNames[room.type]) {
                            _g(this.add.text(cx, cy + 22, typeNames[room.type], {
                                fontSize: '9px', fontFamily: 'monospace', color: '#666677'
                            }).setOrigin(0.5).setDepth(11));
                        }
                    }

                    // Clickable adjacent cells
                    if (canMove) {
                        const hitZone = _g(this.add.zone(cx, cy, cellSize, cellSize)
                            .setInteractive({ useHandCursor: true }).setDepth(15));
                        hitZone.on('pointerover', () => {
                            gfx.clear();
                            gfx.fillStyle(bgColor, 0.95);
                            gfx.fillRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);
                            gfx.lineStyle(2, 0xbb88ff, 1);
                            gfx.strokeRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);
                        });
                        hitZone.on('pointerout', () => {
                            gfx.clear();
                            gfx.fillStyle(bgColor, 0.7);
                            gfx.fillRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);
                            gfx.lineStyle(borderWidth, borderColor, borderAlpha);
                            gfx.strokeRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);
                        });
                        hitZone.on('pointerdown', () => {
                            this._moveToRoom(x, y);
                        });

                        // Subtle glow on moveable cells
                        const moveGlow = _g(this.add.circle(cx, cy, cellSize / 2 - 4, 0xbb88ff, 0.04).setDepth(9));
                        this.tweens.add({ targets: moveGlow, alpha: 0.08, duration: 800, yoyo: true, repeat: -1 });
                    }
                }
            }
        }
    }

    _isAdjacent(x1, y1, x2, y2) {
        return (Math.abs(x1 - x2) + Math.abs(y1 - y2)) === 1;
    }

    // ==================== PARTY PANEL ====================

    _drawPartyPanel() {
        this._clearGroup(this._partyPanelObjects);
        const _p = (obj) => { this._partyPanelObjects.push(obj); return obj; };

        const panelX = 920, panelY = 100, panelW = 340, panelH = 320;

        // Background
        _p(this.add.rectangle(panelX + panelW / 2, panelY + panelH / 2, panelW, panelH, 0x0e0e1a, 0.85)
            .setStrokeStyle(1, 0x332244, 0.5).setDepth(10));

        _p(this.add.text(panelX + 10, panelY + 8, '파티 상태', {
            fontSize: '12px', fontFamily: 'monospace', color: '#886688', fontStyle: 'bold'
        }).setDepth(11));

        this.party.forEach((merc, i) => {
            const my = panelY + 35 + i * 65;
            const base = merc.getBaseClass();
            const stats = merc.getStats();
            const hpRatio = merc.currentHp / stats.hp;
            const isDead = !merc.alive;

            // Class icon circle
            const iconColor = isDead ? 0x333333 : base.color;
            _p(this.add.circle(panelX + 25, my + 15, 12, iconColor, isDead ? 0.3 : 0.8).setDepth(11));

            const roleIcons = { tank: '🛡', melee_dps: '⚔', ranged_dps: '🏹', healer: '💚', support: '🔮' };
            _p(this.add.text(panelX + 25, my + 15, roleIcons[base.role] || '⚔', {
                fontSize: '10px'
            }).setOrigin(0.5).setDepth(12));

            // Name
            const nameColor = isDead ? '#444444' : '#cccccc';
            _p(this.add.text(panelX + 45, my + 2, `${merc.name}`, {
                fontSize: '11px', fontFamily: 'monospace', color: nameColor, fontStyle: isDead ? '' : 'bold'
            }).setDepth(11));

            // Class + level
            _p(this.add.text(panelX + 45, my + 17, `${base.name} Lv.${merc.level}`, {
                fontSize: '9px', fontFamily: 'monospace', color: isDead ? '#333333' : '#666677'
            }).setDepth(11));

            if (isDead) {
                _p(this.add.text(panelX + 280, my + 10, '전사', {
                    fontSize: '11px', fontFamily: 'monospace', color: '#662222', fontStyle: 'bold'
                }).setDepth(11));
            } else {
                // HP bar
                const barX = panelX + 150, barY = my + 6, barW = 150, barH = 8;
                _p(this.add.rectangle(barX + barW / 2, barY + barH / 2, barW, barH, 0x221122).setDepth(11));
                const hpFillW = barW * Math.max(0, hpRatio);
                const hpColor = hpRatio > 0.6 ? 0x44ff44 : hpRatio > 0.3 ? 0xffaa00 : 0xff4444;
                if (hpFillW > 0) {
                    _p(this.add.rectangle(barX + hpFillW / 2, barY + barH / 2, hpFillW, barH, hpColor).setDepth(12));
                }
                _p(this.add.text(barX + barW / 2, barY + barH + 6, `${merc.currentHp}/${stats.hp}`, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#666677'
                }).setOrigin(0.5).setDepth(11));
            }
        });
    }

    // ==================== INVENTORY BAR ====================

    _drawInventoryBar() {
        this._clearGroup(this._inventoryBarObjects);
        const _i = (obj) => { this._inventoryBarObjects.push(obj); return obj; };

        const barY = 660, barH = 55;
        _i(this.add.rectangle(640, barY + barH / 2, 1280, barH, 0x0a0a12, 0.9)
            .setStrokeStyle(1, 0x221133, 0.5).setDepth(100));

        _i(this.add.text(20, barY + 5, '소지품:', {
            fontSize: '10px', fontFamily: 'monospace', color: '#665577'
        }).setDepth(101));

        if (this.loot.length === 0) {
            _i(this.add.text(90, barY + 5, '(없음)', {
                fontSize: '10px', fontFamily: 'monospace', color: '#333344'
            }).setDepth(101));
            return;
        }

        let ix = 90;
        this.loot.forEach((item, idx) => {
            if (ix > 1200) return;
            const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
            const itemBg = _i(this.add.rectangle(ix + 40, barY + 22, 82, 32, 0x111122, 0.9)
                .setStrokeStyle(1, rarity.color, 0.6).setDepth(101).setInteractive({ useHandCursor: true }));
            _i(this.add.text(ix + 40, barY + 16, item.name, {
                fontSize: '9px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(102));
            _i(this.add.text(ix + 40, barY + 30, `${item.weight}kg`, {
                fontSize: '8px', fontFamily: 'monospace', color: '#555566'
            }).setOrigin(0.5).setDepth(102));
            ix += 90;
        });
    }

    // ==================== MOVEMENT ====================

    _moveToRoom(x, y) {
        if (this.inCombat) return;

        const room = this.grid[y][x];

        // Locked room check
        if (room.type === 'locked' && !this.hasKey) {
            this._showLockedRoom(x, y);
            return;
        }

        this.playerX = x;
        this.playerY = y;
        this.moveCount++;
        this.roomsVisited++;

        // Increase tension every 4 moves
        if (this.roomsVisited > 0 && this.roomsVisited % 4 === 0 && this.tension < 5) {
            this.tension++;
            DamagePopup.show(this, 640, 300, `긴장도 상승! Lv.${this.tension}`, 0xff4444, false);
            this.cameras.main.shake(80, 0.002);

            // Shuffle exit at tension >= 4
            if (this.tension >= 4) {
                this._shuffleExit();
            }
        }

        this._revealAdjacent(x, y);

        // Roll room type if unknown
        if (room.type === 'unknown') {
            room.type = this._rollRoomType(room.depth);
        }

        // Convert locked room with key
        if (room.type === 'locked' && this.hasKey) {
            this.hasKey = false;
            room.type = 'item';
            room._bonusRarity = 2;
            DamagePopup.show(this, 640, 340, '열쇠 사용! 보물 발견!', 0xffcc44, false);
        }

        room.visited = true;

        // Refresh visuals
        this._drawGrid();
        this._drawHUD();

        // Check move limit
        if (this._checkMoveLimit()) return;

        // Handle room event
        this._handleRoom(x, y);
    }

    _handleRoom(x, y) {
        const room = this.grid[y][x];

        switch (room.type) {
            case 'empty':
            case 'entrance':
                // Nothing happens
                break;
            case 'enemy':
                this._startCombat(room.depth);
                break;
            case 'item':
                this._showItemPopup(room.depth, room._bonusRarity || 0);
                break;
            case 'trap':
                this._showTrapPopup();
                break;
            case 'rest':
                this._showRestRoom(x, y);
                break;
            case 'exit':
                this._showEscapeUI();
                break;
        }
    }

    // ==================== COMBAT ====================

    _startCombat(depth) {
        this.inCombat = true;
        this.battleOver = false;
        this.battleTime = 0;
        this.allies = [];
        this.enemies = [];
        this.allUnits = [];

        // Fade grid
        this._gridObjects.forEach(obj => { if (obj && obj.setAlpha) obj.setAlpha(0.3); });
        this._partyPanelObjects.forEach(obj => { if (obj && obj.setAlpha) obj.setAlpha(0.3); });
        this._inventoryBarObjects.forEach(obj => { if (obj && obj.setAlpha) obj.setAlpha(0.3); });

        // Combat overlay background
        const combatBg = this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.5).setDepth(20);
        this._combatObjects.push(combatBg);

        // Ground line
        const ground = this.add.graphics().setDepth(20);
        ground.fillStyle(0x110a22, 1);
        ground.fillRect(0, 450, 1280, 270);
        ground.fillStyle(0x221144, 1);
        ground.fillRect(0, 448, 1280, 4);
        this._combatObjects.push(ground);

        // Purple flash
        const flash = this.add.rectangle(640, 360, 1280, 720, 0x8844ff, 0.3).setDepth(25);
        this.tweens.add({ targets: flash, alpha: 0, duration: 500, onComplete: () => flash.destroy() });

        // Spawn allies
        const startX = 180;
        const spacing = 70;
        let allyIdx = 0;
        this.party.forEach((merc) => {
            if (!merc.alive) return;
            const x = startX + allyIdx * spacing;
            const y = 430 + (allyIdx % 2 === 0 ? 0 : 15);
            const unit = BattleUnit.fromMercenary(this, merc, x, y);
            this.allies.push(unit);
            this.allUnits.push(unit);
            this._combatObjects.push(unit);
            allyIdx++;
        });

        // Determine enemy count & types
        const enemyCount = Math.min(4, 1 + Math.floor((depth + this.tension) / 3));
        const scaleMult = 1 + depth * 0.15 + this.tension * 0.10;

        let typePool;
        if (this.tension < 3) {
            typePool = [
                { type: 'wraith', weight: 40 },
                { type: 'cursed_mage', weight: 30 },
                { type: 'bone_golem', weight: 20 },
                { type: 'shade', weight: 10 }
            ];
        } else {
            typePool = [
                { type: 'wraith', weight: 25 },
                { type: 'cursed_mage', weight: 25 },
                { type: 'bone_golem', weight: 25 },
                { type: 'shade', weight: 25 }
            ];
        }

        const totalWeight = typePool.reduce((s, t) => s + t.weight, 0);
        const pickType = () => {
            let roll = Math.random() * totalWeight;
            for (const entry of typePool) {
                roll -= entry.weight;
                if (roll <= 0) return entry.type;
            }
            return 'wraith';
        };

        // Spawn enemies
        const enemyStartX = 800;
        const enemySpacing = 75;
        for (let i = 0; i < enemyCount; i++) {
            const ex = enemyStartX + i * enemySpacing;
            const ey = 420 + (i % 2 === 0 ? 0 : 15);
            const eType = pickType();
            const unit = BattleUnit.fromEnemyData(this, eType, scaleMult, ex, ey);
            this.enemies.push(unit);
            this.allUnits.push(unit);
            this._combatObjects.push(unit);
        }

        // Combat HUD
        const combatLabel = this.add.text(640, 80, '⚔ 전투 발생!', {
            fontSize: '22px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(200).setAlpha(0);
        this.tweens.add({ targets: combatLabel, alpha: 1, duration: 300, yoyo: true, hold: 600,
            onComplete: () => combatLabel.destroy() });

        // Enemy count text
        this.combatEnemyText = this.add.text(1200, 80, `적: ${enemyCount}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#ff8888'
        }).setOrigin(1, 0).setDepth(100);
        this._combatObjects.push(this.combatEnemyText);

        // Speed button
        const speedBtn = this.add.text(1220, 690, `×${this.speedMultiplier}`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
            backgroundColor: '#331111', padding: { x: 6, y: 3 }
        }).setOrigin(0.5).setDepth(200).setInteractive();
        speedBtn.on('pointerdown', () => {
            this.speedMultiplier = this.speedMultiplier === 1 ? 2 : this.speedMultiplier === 2 ? 3 : 1;
            speedBtn.setText(`×${this.speedMultiplier}`);
            localStorage.setItem('demo9_speed', this.speedMultiplier);
        });
        this._combatObjects.push(speedBtn);

        // Flee button
        const fleeChance = this._getFleeChance();
        const fleePct = Math.floor(fleeChance * 100);
        const fleeBtn = this.add.text(60, 690, `🏃 도주 (${fleePct}%)`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#ffaa44',
            backgroundColor: '#332211', padding: { x: 8, y: 4 }
        }).setOrigin(0, 0.5).setDepth(200).setInteractive({ useHandCursor: true });
        fleeBtn.on('pointerdown', () => {
            if (Math.random() < fleeChance) {
                DamagePopup.show(this, 640, 350, '도주 성공!', 0x44ff88, false);
                this.time.delayedCall(500, () => this._endCombat(true, true));
            } else {
                DamagePopup.show(this, 640, 350, '도주 실패!', 0xff4444, false);
                fleeBtn.destroy();
            }
        });
        this._combatObjects.push(fleeBtn);

        // On unit death callback
        this.onUnitDeath = (unit, killer) => {
            if (unit.team === 'enemy') {
                const alive = this.enemies.filter(e => e.alive).length;
                if (this.combatEnemyText) this.combatEnemyText.setText(`적: ${alive}`);

                // Gold drop
                let goldDrop = Phaser.Math.Between(8, 18) + depth * 4;
                this.totalGold += goldDrop;
                if (this.goldText) this.goldText.setText(`💰 ${this.totalGold}G`);

                const gText = this.add.text(unit.container.x, unit.container.y - 10, `+${goldDrop}G`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
                    stroke: '#000', strokeThickness: 2
                }).setOrigin(0.5).setDepth(90);
                this.tweens.add({ targets: gText, y: gText.y - 30, alpha: 0, duration: 800, onComplete: () => gText.destroy() });

                // XP
                this.totalXp += 5 + depth * 2;

                // Loot drop
                if (Math.random() < 0.25) {
                    const rarityBonus = Math.floor(depth * 0.5 + this.tension * 0.3);
                    const item = generateItem('blackout', this.gameState.guildLevel, rarityBonus);
                    if (item) {
                        this.loot.push(item);
                        this._showLootPopup(unit.container.x, unit.container.y - 40, item);
                    }
                }

                // Key drop chance from enemies
                if (!this.hasKey && Math.random() < 0.12) {
                    this.hasKey = true;
                    DamagePopup.show(this, unit.container.x, unit.container.y - 50, '🔑 열쇠 획득!', 0xffcc44, false);
                }
            }

            this._checkCombatEnd();
        };

        this.cameras.main.shake(100, 0.003);
    }

    _checkCombatEnd() {
        if (this.battleOver) return;
        const allyAlive = this.allies.filter(u => u.alive);
        const enemyAlive = this.enemies.filter(u => u.alive);

        if (allyAlive.length === 0) {
            this.battleOver = true;
            this.cameras.main.shake(200, 0.005);
            this._snapshotAllyState();
            this.time.delayedCall(800, () => this._endRun(false));
            return;
        }

        if (enemyAlive.length === 0) {
            this.battleOver = true;
            this.cameras.main.shake(100, 0.002);
            this._snapshotAllyState();

            const clearTxt = this.add.text(640, 300, 'CLEAR', {
                fontSize: '36px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold',
                stroke: '#000', strokeThickness: 4
            }).setOrigin(0.5).setDepth(300).setAlpha(0);
            this.tweens.add({
                targets: clearTxt, alpha: 1, duration: 300, hold: 600,
                onComplete: () => {
                    clearTxt.destroy();
                    this._endCombat(true, false);
                }
            });
        }
    }

    _endCombat(won, fled) {
        this.inCombat = false;
        this.onUnitDeath = null;

        // Clean up combat objects
        this._combatObjects.forEach(obj => {
            if (obj && obj.destroy) obj.destroy();
        });
        this._combatObjects = [];

        // Destroy units
        this.allUnits.forEach(u => { if (u.destroy) u.destroy(); });
        this.allies = [];
        this.enemies = [];
        this.allUnits = [];

        if (!won && !fled) {
            // Lost combat = end run
            return;
        }

        this._returnToGrid();
    }

    _returnToGrid() {
        // Restore grid visibility
        this._drawGrid();
        this._drawHUD();
        this._drawPartyPanel();
        this._drawInventoryBar();
    }

    // ==================== ITEM POPUP ====================

    _showItemPopup(depth, bonusRarity) {
        this._clearGroup(this._overlayObjects);
        const _o = (obj) => { this._overlayObjects.push(obj); return obj; };

        const dimBg = _o(this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.5).setDepth(50));

        const popW = 400, popH = 280;
        _o(this.add.rectangle(640, 340, popW, popH, 0x111122, 0.95)
            .setStrokeStyle(2, 0x4488ff, 0.7).setDepth(51));

        _o(this.add.text(640, 220, '💎 보물 발견!', {
            fontSize: '20px', fontFamily: 'monospace', color: '#4488ff', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(52));

        const itemCount = 1 + (Math.random() < 0.4 ? 1 : 0);
        const rarityBonus = Math.floor(depth * 0.5 + this.tension * 0.3) + (bonusRarity || 0);
        const items = [];

        for (let i = 0; i < itemCount; i++) {
            const item = generateItem('blackout', this.gameState.guildLevel, rarityBonus);
            if (item) items.push(item);
        }

        // Check weight
        const currentWeight = this._getWeight();
        const capacity = 40;

        items.forEach((item, idx) => {
            const iy = 270 + idx * 50;
            const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
            const wouldExceed = (currentWeight + item.weight) > capacity;

            _o(this.add.rectangle(640, iy, 340, 38, 0x1a1a2a, 0.9)
                .setStrokeStyle(1, rarity.color, 0.6).setDepth(52));
            _o(this.add.text(490, iy, `${item.name} (${item.weight}kg)`, {
                fontSize: '12px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
            }).setOrigin(0, 0.5).setDepth(53));

            if (wouldExceed) {
                _o(this.add.text(790, iy, '무게 초과!', {
                    fontSize: '10px', fontFamily: 'monospace', color: '#ff4444'
                }).setOrigin(1, 0.5).setDepth(53));
            } else {
                const pickBtn = _o(this.add.text(790, iy, '획득', {
                    fontSize: '12px', fontFamily: 'monospace', color: '#44ff88',
                    backgroundColor: '#1a331a', padding: { x: 8, y: 3 }
                }).setOrigin(1, 0.5).setDepth(53).setInteractive({ useHandCursor: true }));
                pickBtn.on('pointerdown', () => {
                    this.loot.push(item);
                    pickBtn.setText('획득!').removeInteractive();
                    pickBtn.setColor('#226622');
                    DamagePopup.show(this, 640, 250, `${item.name} 획득!`, rarity.color, false);
                    this._drawInventoryBar();
                    this._drawHUD();

                    // Also give key from item rooms
                    if (!this.hasKey && Math.random() < 0.2) {
                        this.hasKey = true;
                        DamagePopup.show(this, 640, 280, '🔑 열쇠 발견!', 0xffcc44, false);
                        this._drawHUD();
                    }
                });
            }
        });

        // Close button
        const closeBtn = _o(this.add.text(640, 420, '닫기', {
            fontSize: '14px', fontFamily: 'monospace', color: '#888899',
            backgroundColor: '#222233', padding: { x: 16, y: 6 }
        }).setOrigin(0.5).setDepth(53).setInteractive({ useHandCursor: true }));
        closeBtn.on('pointerdown', () => {
            this._clearGroup(this._overlayObjects);
        });
    }

    _showLootPopup(x, y, item) {
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const bg = this.add.rectangle(x, y, 120, 24, 0x111111, 0.9).setStrokeStyle(1, rarity.color).setDepth(90);
        const txt = this.add.text(x, y, `📦 ${item.name}`, {
            fontSize: '10px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(91);
        this.tweens.add({
            targets: [bg, txt], y: y - 40, alpha: 0, duration: 1500, delay: 800,
            onComplete: () => { bg.destroy(); txt.destroy(); }
        });
    }

    // ==================== TRAP POPUP ====================

    _showTrapPopup() {
        this._clearGroup(this._overlayObjects);
        const _o = (obj) => { this._overlayObjects.push(obj); return obj; };

        _o(this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.5).setDepth(50));

        const popW = 380, popH = 220;
        _o(this.add.rectangle(640, 340, popW, popH, 0x221111, 0.95)
            .setStrokeStyle(2, 0xff8844, 0.7).setDepth(51));

        _o(this.add.text(640, 255, '⚠ 함정 발견!', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(52));

        const dodgeChance = Math.max(10, 60 - this.tension * 5);

        // Option 1: Dodge
        const dodgeBtn = _o(this.add.text(640, 320, `🏃 회피 시도 (${dodgeChance}% 성공)`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#44cccc',
            backgroundColor: '#112233', padding: { x: 12, y: 6 }
        }).setOrigin(0.5).setDepth(52).setInteractive({ useHandCursor: true }));
        dodgeBtn.on('pointerdown', () => {
            this._clearGroup(this._overlayObjects);
            if (Math.random() * 100 < dodgeChance) {
                DamagePopup.show(this, 640, 340, '회피 성공!', 0x44cccc, false);
            } else {
                DamagePopup.show(this, 640, 340, '회피 실패! 피해!', 0xff4444, false);
                this._applyTrapDamage(0.1, 0.2);
            }
        });

        // Option 2: Force through
        const forceBtn = _o(this.add.text(640, 380, '💪 강행 돌파 (5-10% 피해, +20-40G)', {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffaa44',
            backgroundColor: '#332211', padding: { x: 12, y: 6 }
        }).setOrigin(0.5).setDepth(52).setInteractive({ useHandCursor: true }));
        forceBtn.on('pointerdown', () => {
            this._clearGroup(this._overlayObjects);
            this._applyTrapDamage(0.05, 0.10);
            const goldReward = Phaser.Math.Between(20, 40);
            this.totalGold += goldReward;
            DamagePopup.show(this, 640, 340, `강행 돌파! +${goldReward}G`, 0xffcc44, false);
            this._drawHUD();
            this._drawPartyPanel();
        });
    }

    _applyTrapDamage(minPct, maxPct) {
        this.party.forEach(merc => {
            if (!merc.alive) return;
            const stats = merc.getStats();
            const dmgPct = minPct + Math.random() * (maxPct - minPct);
            const dmg = Math.floor(stats.hp * dmgPct);
            merc.currentHp = Math.max(1, merc.currentHp - dmg);
        });
        this.cameras.main.shake(100, 0.003);
        this._drawPartyPanel();
    }

    // ==================== REST ROOM ====================

    _showRestRoom(x, y) {
        // Heal 20% maxHP, tension -1
        this.party.forEach(merc => {
            if (!merc.alive) return;
            const stats = merc.getStats();
            const healAmt = Math.floor(stats.hp * 0.2);
            merc.currentHp = Math.min(stats.hp, merc.currentHp + healAmt);
        });

        const prevTension = this.tension;
        this.tension = Math.max(0, this.tension - 1);

        DamagePopup.show(this, 640, 320, '😴 휴식! HP 20% 회복', 0x44ccff, false);
        if (prevTension > this.tension) {
            DamagePopup.show(this, 640, 350, '긴장도 감소!', 0x44ff88, false);
        }

        // Convert to empty
        this.grid[y][x].type = 'empty';

        this._drawPartyPanel();
        this._drawHUD();
        this._drawGrid();
    }

    // ==================== LOCKED ROOM ====================

    _showLockedRoom(x, y) {
        this._clearGroup(this._overlayObjects);
        const _o = (obj) => { this._overlayObjects.push(obj); return obj; };

        _o(this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.4).setDepth(50));

        const popW = 300, popH = 140;
        _o(this.add.rectangle(640, 340, popW, popH, 0x222200, 0.95)
            .setStrokeStyle(2, 0xaaaa44, 0.7).setDepth(51));

        _o(this.add.text(640, 310, '🔒 잠겨있다...', {
            fontSize: '18px', fontFamily: 'monospace', color: '#aaaa44', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(52));

        _o(this.add.text(640, 340, '열쇠가 필요합니다', {
            fontSize: '12px', fontFamily: 'monospace', color: '#888866'
        }).setOrigin(0.5).setDepth(52));

        const closeBtn = _o(this.add.text(640, 380, '돌아가기', {
            fontSize: '12px', fontFamily: 'monospace', color: '#888899',
            backgroundColor: '#222233', padding: { x: 12, y: 4 }
        }).setOrigin(0.5).setDepth(52).setInteractive({ useHandCursor: true }));
        closeBtn.on('pointerdown', () => {
            this._clearGroup(this._overlayObjects);
        });
    }

    // ==================== ESCAPE UI ====================

    _showEscapeUI() {
        this._clearGroup(this._overlayObjects);
        const _o = (obj) => { this._overlayObjects.push(obj); return obj; };

        _o(this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.6).setDepth(50));

        const popW = 500, popH = 380;
        _o(this.add.rectangle(640, 340, popW, popH, 0x112211, 0.95)
            .setStrokeStyle(2, 0x44aa44, 0.7).setDepth(51));

        _o(this.add.text(640, 180, '🚪 출구 발견!', {
            fontSize: '22px', fontFamily: 'monospace', color: '#44aa44', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(52));

        const weight = this._getWeight();
        _o(this.add.text(640, 210, `무게: ${weight}/40kg | 긴장도: Lv.${this.tension}`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#668866'
        }).setOrigin(0.5).setDepth(52));

        // Option 1: Stealth escape
        const stealthChance = this._getStealthChance();
        const stealthPct = Math.floor(stealthChance * 100);
        const stealthBtn = _o(this.add.rectangle(640, 270, 400, 45, 0x1a2a1a)
            .setStrokeStyle(1, 0x44aa44, 0.6).setDepth(52).setInteractive({ useHandCursor: true }));
        _o(this.add.text(640, 262, `🤫 은밀 탈출 (성공률: ${stealthPct}%)`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(53));
        _o(this.add.text(640, 280, '실패 시 긴장도 +1, 출구 이동 가능', {
            fontSize: '9px', fontFamily: 'monospace', color: '#448844'
        }).setOrigin(0.5).setDepth(53));
        stealthBtn.on('pointerover', () => stealthBtn.setFillStyle(0x2a3a2a));
        stealthBtn.on('pointerout', () => stealthBtn.setFillStyle(0x1a2a1a));
        stealthBtn.on('pointerdown', () => {
            this._clearGroup(this._overlayObjects);
            this._attemptStealth();
        });

        // Option 2: Combat breakthrough
        const combatBtn = _o(this.add.rectangle(640, 340, 400, 45, 0x2a1a1a)
            .setStrokeStyle(1, 0xff4444, 0.6).setDepth(52).setInteractive({ useHandCursor: true }));
        _o(this.add.text(640, 332, '⚔ 정면 돌파 (강적 전투)', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff6644', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(53));
        _o(this.add.text(640, 350, '승리 시 확정 탈출', {
            fontSize: '9px', fontFamily: 'monospace', color: '#884444'
        }).setOrigin(0.5).setDepth(53));
        combatBtn.on('pointerover', () => combatBtn.setFillStyle(0x3a2a2a));
        combatBtn.on('pointerout', () => combatBtn.setFillStyle(0x2a1a1a));
        combatBtn.on('pointerdown', () => {
            this._clearGroup(this._overlayObjects);
            this._attemptCombatEscape();
        });

        // Option 3: Drop items
        const dropBtn = _o(this.add.rectangle(640, 410, 400, 45, 0x2a2a1a)
            .setStrokeStyle(1, 0xffcc44, 0.6).setDepth(52).setInteractive({ useHandCursor: true }));
        _o(this.add.text(640, 402, '📦 아이템 투하 (무게 감소)', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(53));
        _o(this.add.text(640, 420, '아이템을 버려 은밀 탈출 확률 상승', {
            fontSize: '9px', fontFamily: 'monospace', color: '#888844'
        }).setOrigin(0.5).setDepth(53));
        dropBtn.on('pointerover', () => dropBtn.setFillStyle(0x3a3a2a));
        dropBtn.on('pointerout', () => dropBtn.setFillStyle(0x2a2a1a));
        dropBtn.on('pointerdown', () => {
            this._clearGroup(this._overlayObjects);
            this._showDropItemsUI();
        });

        // Back button
        const backBtn = _o(this.add.text(640, 475, '돌아가기', {
            fontSize: '12px', fontFamily: 'monospace', color: '#666677',
            backgroundColor: '#1a1a2a', padding: { x: 12, y: 4 }
        }).setOrigin(0.5).setDepth(52).setInteractive({ useHandCursor: true }));
        backBtn.on('pointerdown', () => {
            this._clearGroup(this._overlayObjects);
        });
    }

    _attemptStealth() {
        const chance = this._getStealthChance();
        if (Math.random() < chance) {
            DamagePopup.show(this, 640, 340, '은밀 탈출 성공!', 0x44ff88, false);
            this.cameras.main.fadeOut(600, 0, 0, 0, (cam, progress) => {
                if (progress === 1) this._endRun(true);
            });
        } else {
            DamagePopup.show(this, 640, 340, '탈출 실패! 긴장도 상승!', 0xff4444, false);
            this.cameras.main.shake(100, 0.003);
            this.tension = Math.min(5, this.tension + 1);
            if (this.tension >= 4) {
                this._shuffleExit();
                this._drawGrid();
            }
            this._drawHUD();
        }
    }

    _attemptCombatEscape() {
        // Start a hard fight at the exit
        this.inCombat = true;
        this.battleOver = false;
        this.battleTime = 0;
        this.allies = [];
        this.enemies = [];
        this.allUnits = [];

        // Fade grid
        this._gridObjects.forEach(obj => { if (obj && obj.setAlpha) obj.setAlpha(0.3); });
        this._partyPanelObjects.forEach(obj => { if (obj && obj.setAlpha) obj.setAlpha(0.3); });
        this._inventoryBarObjects.forEach(obj => { if (obj && obj.setAlpha) obj.setAlpha(0.3); });

        const combatBg = this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.5).setDepth(20);
        this._combatObjects.push(combatBg);

        const ground = this.add.graphics().setDepth(20);
        ground.fillStyle(0x110a22, 1);
        ground.fillRect(0, 450, 1280, 270);
        ground.fillStyle(0x221144, 1);
        ground.fillRect(0, 448, 1280, 4);
        this._combatObjects.push(ground);

        // Spawn allies
        let allyIdx = 0;
        this.party.forEach((merc) => {
            if (!merc.alive) return;
            const x = 180 + allyIdx * 70;
            const y = 430 + (allyIdx % 2 === 0 ? 0 : 15);
            const unit = BattleUnit.fromMercenary(this, merc, x, y);
            this.allies.push(unit);
            this.allUnits.push(unit);
            this._combatObjects.push(unit);
            allyIdx++;
        });

        // Spawn elite enemies (harder)
        const enemyCount = Math.min(4, 2 + Math.floor(this.tension / 2));
        const scaleMult = 1.2 + this.tension * 0.15;
        const eliteTypes = ['elite_wraith', 'elite_cursed', 'bone_golem', 'shade'];

        for (let i = 0; i < enemyCount; i++) {
            const ex = 800 + i * 75;
            const ey = 420 + (i % 2 === 0 ? 0 : 15);
            const eType = eliteTypes[i % eliteTypes.length];
            // Use base type if elite not in ENEMY_DATA
            const actualType = ENEMY_DATA[eType] ? eType : 'wraith';
            const unit = BattleUnit.fromEnemyData(this, actualType, scaleMult, ex, ey);
            this.enemies.push(unit);
            this.allUnits.push(unit);
            this._combatObjects.push(unit);
        }

        // Speed button
        const speedBtn = this.add.text(1220, 690, `×${this.speedMultiplier}`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
            backgroundColor: '#331111', padding: { x: 6, y: 3 }
        }).setOrigin(0.5).setDepth(200).setInteractive();
        speedBtn.on('pointerdown', () => {
            this.speedMultiplier = this.speedMultiplier === 1 ? 2 : this.speedMultiplier === 2 ? 3 : 1;
            speedBtn.setText(`×${this.speedMultiplier}`);
            localStorage.setItem('demo9_speed', this.speedMultiplier);
        });
        this._combatObjects.push(speedBtn);

        const label = this.add.text(640, 80, '⚔ 정면 돌파!', {
            fontSize: '22px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(200).setAlpha(0);
        this.tweens.add({ targets: label, alpha: 1, duration: 300, yoyo: true, hold: 600,
            onComplete: () => label.destroy() });

        this._escapeMode = true;

        this.onUnitDeath = (unit, killer) => {
            if (unit.team === 'enemy') {
                let goldDrop = Phaser.Math.Between(15, 30);
                this.totalGold += goldDrop;

                const gText = this.add.text(unit.container.x, unit.container.y - 10, `+${goldDrop}G`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
                    stroke: '#000', strokeThickness: 2
                }).setOrigin(0.5).setDepth(90);
                this.tweens.add({ targets: gText, y: gText.y - 30, alpha: 0, duration: 800, onComplete: () => gText.destroy() });
            }

            const allyAlive = this.allies.filter(u => u.alive);
            const enemyAlive = this.enemies.filter(u => u.alive);

            if (allyAlive.length === 0) {
                this.battleOver = true;
                this._snapshotAllyState();
                this.time.delayedCall(800, () => this._endRun(false));
                return;
            }

            if (enemyAlive.length === 0) {
                this.battleOver = true;
                this._snapshotAllyState();
                const clearTxt = this.add.text(640, 300, '돌파 성공!', {
                    fontSize: '36px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold',
                    stroke: '#000', strokeThickness: 4
                }).setOrigin(0.5).setDepth(300).setAlpha(0);
                this.tweens.add({
                    targets: clearTxt, alpha: 1, duration: 300, hold: 600,
                    onComplete: () => {
                        clearTxt.destroy();
                        // Clean up combat
                        this._combatObjects.forEach(obj => { if (obj && obj.destroy) obj.destroy(); });
                        this._combatObjects = [];
                        this.allUnits.forEach(u => { if (u.destroy) u.destroy(); });
                        this.allies = [];
                        this.enemies = [];
                        this.allUnits = [];
                        this.inCombat = false;
                        this.cameras.main.fadeOut(600, 0, 0, 0, (cam, progress) => {
                            if (progress === 1) this._endRun(true);
                        });
                    }
                });
            }
        };

        this.cameras.main.shake(120, 0.003);
    }

    // ==================== DROP ITEMS UI ====================

    _showDropItemsUI() {
        this._clearGroup(this._overlayObjects);
        const _o = (obj) => { this._overlayObjects.push(obj); return obj; };

        _o(this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.6).setDepth(50));

        const popW = 450, popH = Math.min(500, 180 + this.loot.length * 40);
        _o(this.add.rectangle(640, 340, popW, popH, 0x1a1a22, 0.95)
            .setStrokeStyle(2, 0xffcc44, 0.7).setDepth(51));

        _o(this.add.text(640, 340 - popH / 2 + 20, '📦 아이템 투하', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(52));

        const weightText = _o(this.add.text(640, 340 - popH / 2 + 45, `현재 무게: ${this._getWeight()}/40kg`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#888866'
        }).setOrigin(0.5).setDepth(52));

        if (this.loot.length === 0) {
            _o(this.add.text(640, 340, '버릴 아이템이 없습니다', {
                fontSize: '13px', fontFamily: 'monospace', color: '#555555'
            }).setOrigin(0.5).setDepth(52));
        } else {
            const listY = 340 - popH / 2 + 70;
            this.loot.forEach((item, idx) => {
                const iy = listY + idx * 38;
                const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;

                _o(this.add.rectangle(640, iy + 10, 380, 32, 0x111122, 0.8)
                    .setStrokeStyle(1, rarity.color, 0.4).setDepth(52));
                _o(this.add.text(470, iy + 10, `${item.name} (${item.weight}kg)`, {
                    fontSize: '11px', fontFamily: 'monospace', color: rarity.textColor
                }).setOrigin(0, 0.5).setDepth(53));

                const dropBtn = _o(this.add.text(810, iy + 10, '버리기', {
                    fontSize: '11px', fontFamily: 'monospace', color: '#ff8844',
                    backgroundColor: '#332211', padding: { x: 6, y: 2 }
                }).setOrigin(1, 0.5).setDepth(53).setInteractive({ useHandCursor: true }));
                dropBtn.on('pointerdown', () => {
                    this.loot.splice(this.loot.indexOf(item), 1);
                    DamagePopup.show(this, 640, 300, `${item.name} 투하!`, 0xff8844, false);
                    this._showDropItemsUI(); // Refresh
                });
            });
        }

        // Done button - return to escape UI
        const doneBtn = _o(this.add.text(640, 340 + popH / 2 - 30, '완료 → 탈출 선택으로', {
            fontSize: '12px', fontFamily: 'monospace', color: '#44ff88',
            backgroundColor: '#1a331a', padding: { x: 12, y: 4 }
        }).setOrigin(0.5).setDepth(52).setInteractive({ useHandCursor: true }));
        doneBtn.on('pointerdown', () => {
            this._clearGroup(this._overlayObjects);
            this._drawInventoryBar();
            this._drawHUD();
            this._showEscapeUI();
        });
    }

    // ==================== EXIT SHUFFLE ====================

    _shuffleExit() {
        // Remove old exit
        this.grid[this.exitY][4].type = 'unknown';
        this.grid[this.exitY][4].visited = false;

        // New random y
        let newY = Phaser.Math.Between(0, 4);
        while (newY === this.exitY) {
            newY = Phaser.Math.Between(0, 4);
        }
        this.exitY = newY;
        this.grid[this.exitY][4] = { type: 'exit', visited: false, revealed: true, depth: 4 };

        DamagePopup.show(this, 640, 380, '출구가 이동했다!', 0xff8844, false);
    }

    // ==================== UTILITY ====================

    _getWeight() {
        return this.loot.reduce((sum, item) => sum + (item.weight || 0), 0);
    }

    _getFleeChance() {
        const ratio = this._getWeight() / 40;
        if (ratio <= 0.3) return 0.9;
        if (ratio <= 0.6) return 0.7;
        if (ratio <= 0.8) return 0.4;
        return 0.15;
    }

    _getStealthChance() {
        const weight = this._getWeight();
        const ratio = weight / 40;
        let basePct;
        if (ratio <= 0.3) basePct = 0.95;
        else if (ratio <= 0.6) basePct = 0.80;
        else if (ratio <= 0.8) basePct = 0.50;
        else basePct = 0.20;
        return Math.max(0.05, basePct - this.tension * 0.05);
    }

    _snapshotAllyState() {
        this.allies.forEach(u => {
            const merc = this.party.find(m => m.id === u.mercId);
            if (!merc) return;
            if (u.alive) {
                merc.currentHp = u.hp;
            } else {
                if (!this.casualties.find(c => c.id === merc.id)) {
                    this.casualties.push(merc);
                }
                merc.alive = false;
            }
        });
    }

    _checkMoveLimit() {
        if (this.moveCount >= this.maxMoves) {
            DamagePopup.show(this, 640, 300, '시간 초과! 탈출 실패...', 0xff2222, false);
            this.cameras.main.shake(200, 0.005);
            this.time.delayedCall(1200, () => this._endRun(false));
            return true;
        }
        return false;
    }

    _showEntryAnnounce() {
        const txt = this.add.text(640, 300, '🔦 Blackout 진입', {
            fontSize: '32px', fontFamily: 'monospace', color: '#8844ff', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(200).setAlpha(0);
        this.tweens.add({
            targets: txt, alpha: 1, duration: 400, yoyo: true, hold: 800,
            onComplete: () => txt.destroy()
        });

        const sub = this.add.text(640, 340, `구역 Lv.${this.zoneLevel} | 이동 제한: ${this.maxMoves}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#665577',
            stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5).setDepth(200).setAlpha(0);
        this.tweens.add({
            targets: sub, alpha: 1, duration: 400, delay: 200, yoyo: true, hold: 600,
            onComplete: () => sub.destroy()
        });
    }

    // ==================== END RUN ====================

    _endRun(success) {
        this._snapshotAllyState();

        const survivors = this.party.filter(m => m.alive);
        const allCasualties = [...this.casualties];

        const result = {
            success,
            zoneKey: 'blackout',
            zoneLevel: this.zoneLevel,
            rounds: this.roomsVisited,
            goldEarned: this.totalGold,
            xpEarned: this.totalXp,
            loot: this.loot,
            casualties: allCasualties,
            survivors,
            zoneLevelUp: success,
            tensionReached: this.tension
        };

        this.scene.start('RunResultScene', {
            gameState: this.gameState,
            result
        });
    }

    // ==================== HELPERS ====================

    _clearGroup(group) {
        if (!group) return;
        group.forEach(obj => {
            if (obj && obj.destroy) obj.destroy();
        });
        group.length = 0;
    }

    shutdown() {
        this.allUnits.forEach(u => { if (u && u.destroy) u.destroy(); });
        this._clearGroup(this._gridObjects);
        this._clearGroup(this._hudObjects);
        this._clearGroup(this._overlayObjects);
        this._clearGroup(this._combatObjects);
        this._clearGroup(this._partyPanelObjects);
        this._clearGroup(this._inventoryBarObjects);
    }
}
