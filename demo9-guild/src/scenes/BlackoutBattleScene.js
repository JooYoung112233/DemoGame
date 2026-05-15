class BlackoutBattleScene extends Phaser.Scene {
    constructor() { super('BlackoutBattleScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.party = data.party;
        this.loot = data.loot || [];
        this.totalGold = data.totalGold || 0;
        this.totalXp = data.totalXp || 0;
        this.zoneLevel = this.gameState.zoneLevel['blackout'] || 1;

        // Multi-floor state
        this.currentFloor = data.currentFloor || 1;
        this.curseLevel = data.curseLevel || 0;
        this.adaptations = data.adaptations || [];
        this.adaptationCount = data.adaptationCount || 0;
        this.cluesCollected = data.cluesCollected || 0;
        this.bossDefeated = false;
    }

    create() {
        this.roomsVisited = 0;
        this.moveCount = 0;
        this.maxMoves = 15 + this.zoneLevel * 5 + (this.currentFloor - 1) * 3;
        this.inCombat = false;
        this.hasKey = false;
        this.casualties = [];
        this.speedMultiplier = parseInt(localStorage.getItem('demo9_speed') || '1', 10) || 1;

        this.allies = [];
        this.enemies = [];
        this.allUnits = [];
        this.battleOver = false;
        this.battleTime = 0;

        this._gridObjects = [];
        this._hudObjects = [];
        this._overlayObjects = [];
        this._combatObjects = [];
        this._partyPanelObjects = [];
        this._inventoryBarObjects = [];

        this._gridSize = this._getGridSize();
        this._maxFloors = this._getMaxFloors();

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

        this._lastAllyHp = {};
        this.allies.forEach(u => {
            if (u.alive && u.mercId) {
                this._lastAllyHp[u.mercId] = u.hp;
            }
        });
    }

    // ==================== FLOOR CONFIG ====================

    _getGridSize() {
        const sizes = { 1: 4, 2: 5, 3: 6, 4: 7, 5: 7 };
        return sizes[this.currentFloor] || 5;
    }

    _getMaxFloors() {
        if (this.zoneLevel >= 10) return 5;
        if (this.zoneLevel >= 5) return 4;
        return 3;
    }

    _getFloorClearCondition() {
        const conditions = [
            { type: 'boss', desc: '보스 처치' },
            { type: 'clues', desc: '단서 5개 수집', needed: 5 },
            { type: 'exit', desc: '탈출구 도달' }
        ];
        if (this.currentFloor === 1) return conditions[2];
        if (this.currentFloor >= 3) return conditions[0];
        return conditions[Phaser.Math.Between(0, 2)];
    }

    // ==================== GRID GENERATION ====================

    _generateGrid() {
        const size = this._gridSize;
        this.grid = [];
        for (let y = 0; y < size; y++) {
            this.grid[y] = [];
            for (let x = 0; x < size; x++) {
                this.grid[y][x] = { type: 'unknown', visited: false, revealed: false, depth: x };
            }
        }

        // Entrance at left-center
        const midY = Math.floor(size / 2);
        this.grid[midY][0] = { type: 'entrance', visited: true, revealed: true, depth: 0 };
        this.playerX = 0;
        this.playerY = midY;

        // Exit at right side
        this.exitY = Phaser.Math.Between(0, size - 1);
        this.grid[this.exitY][size - 1] = { type: 'exit', visited: false, revealed: false, depth: size - 1 };

        // Boss room (1 per floor, placed in deeper columns)
        const bossX = Phaser.Math.Between(Math.floor(size * 0.6), size - 2);
        let bossY = Phaser.Math.Between(0, size - 1);
        if (bossX === size - 1 && bossY === this.exitY) bossY = (bossY + 1) % size;
        this.grid[bossY][bossX] = { type: 'boss', visited: false, revealed: false, depth: bossX };

        // Blocked terrain (walls) - more on higher floors / more adaptations
        const wallCount = Math.floor(size * 0.3) + this.adaptationCount;
        let placed = 0;
        let attempts = 0;
        while (placed < wallCount && attempts < 100) {
            const wx = Phaser.Math.Between(1, size - 2);
            const wy = Phaser.Math.Between(0, size - 1);
            if (this.grid[wy][wx].type === 'unknown') {
                this.grid[wy][wx] = { type: 'wall', visited: false, revealed: false, depth: wx };
                placed++;
            }
            attempts++;
        }

        // Locked room
        const lockX = Phaser.Math.Between(2, Math.min(size - 2, 3));
        const lockY = Phaser.Math.Between(0, size - 1);
        if (this.grid[lockY][lockX].type === 'unknown') {
            this.grid[lockY][lockX] = { type: 'locked', visited: false, revealed: false, depth: lockX };
        }

        // Floor clear condition
        this.clearCondition = this._getFloorClearCondition();

        this._revealAdjacent(0, midY);
    }

    _revealAdjacent(x, y) {
        const size = this._gridSize;
        const range = this._hasAdaptation('dark_eyes') ? 2 : 1;
        for (let dx = -range; dx <= range; dx++) {
            for (let dy = -range; dy <= range; dy++) {
                if (Math.abs(dx) + Math.abs(dy) > range) continue;
                const nx = x + dx, ny = y + dy;
                if (nx >= 0 && nx < size && ny >= 0 && ny < size) {
                    this.grid[ny][nx].revealed = true;
                }
            }
        }
    }

    _rollRoomType(depth) {
        const c = this.curseLevel;
        const ambushBoost = this.adaptationCount >= 2 ? 5 : 0;
        const weights = {
            empty:   30,
            enemy:   25,
            ambush:  15 + ambushBoost,
            trap:    10,
            clue:    10,
            rest:    5,
            secret:  c >= 3 ? 4 : 0
        };
        const total = Object.values(weights).reduce((s, v) => s + v, 0);
        let roll = Math.random() * total;
        for (const [type, w] of Object.entries(weights)) {
            roll -= w;
            if (roll <= 0) return type;
        }
        return 'empty';
    }

    // ==================== CURSE LEVEL ====================

    _addCurse(amount) {
        const prev = this.curseLevel;
        this.curseLevel += amount;

        if (this.curseLevel > prev) {
            DamagePopup.show(this, 640, 300, `저주 Lv.${this.curseLevel}`, 0xcc44ff, false);
        }

        // Curse Lv7+: spawn curse avatar pursuit
        const avatarThreshold = this.zoneLevel >= 7 ? 5 : 7;
        if (this.curseLevel >= avatarThreshold && !this._curseAvatarActive) {
            this._curseAvatarActive = true;
            DamagePopup.show(this, 640, 340, '저주 화신 출현!', 0xff00cc, false);
            this.cameras.main.shake(200, 0.005);
        }

        // Curse Lv5-6: fog regen
        if (this.curseLevel >= 5 && this.curseLevel <= 6) {
            this._regenFog();
        }

        this._drawHUD();
    }

    _reduceCurse(amount) {
        this.curseLevel = Math.max(0, this.curseLevel - amount);
        DamagePopup.show(this, 640, 300, `저주 감소! Lv.${this.curseLevel}`, 0x44ff88, false);
        this._drawHUD();
    }

    _regenFog() {
        const size = this._gridSize;
        let regenCount = 0;
        for (let y = 0; y < size; y++) {
            for (let x = 0; x < size; x++) {
                if (this.grid[y][x].revealed && !this.grid[y][x].visited &&
                    !(x === this.playerX && y === this.playerY) && Math.random() < 0.3) {
                    this.grid[y][x].revealed = false;
                    regenCount++;
                }
            }
        }
        if (regenCount > 0) {
            DamagePopup.show(this, 640, 370, '안개가 다시 밀려온다...', 0x8844cc, false);
        }
    }

    _getCurseEnemyHpMult() {
        if (this.curseLevel >= 3) return 1.2;
        return 1.0;
    }

    _getCurseTrapMult() {
        if (this.curseLevel >= 3) return 1.3;
        return 1.0;
    }

    // ==================== ADAPTATION ====================

    _hasAdaptation(key) {
        return this.adaptations.includes(key);
    }

    // ==================== BACKGROUND ====================

    _drawBackground() {
        const gfx = this.add.graphics();
        gfx.fillGradientStyle(0x0a0a12, 0x0a0a12, 0x0e0a1a, 0x0e0a1a);
        gfx.fillRect(0, 0, 1280, 720);

        for (let i = 0; i < 15; i++) {
            const fx = Phaser.Math.Between(50, 1230);
            const fy = Phaser.Math.Between(50, 670);
            gfx.fillStyle(0x8844ff, 0.03);
            gfx.fillCircle(fx, fy, Phaser.Math.Between(20, 60));
        }

        gfx.lineStyle(1, 0x221133, 0.15);
        for (let i = 0; i < 8; i++) {
            gfx.lineBetween(0, 100 + i * 80, 1280, 100 + i * 80);
        }
    }

    // ==================== HUD ====================

    _drawHUD() {
        this._clearGroup(this._hudObjects);
        const _h = (obj) => { this._hudObjects.push(obj); return obj; };

        _h(this.add.rectangle(640, 30, 1280, 60, 0x0a0a12, 0.85).setDepth(100));

        // Title + floor
        _h(this.add.text(30, 12, `🔦 Blackout ${this.currentFloor}F`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#8844ff', fontStyle: 'bold'
        }).setDepth(101));

        // Curse bar
        const curseNames = ['평온', '불안', '경계', '위험', '극도위험', '공포', '광기', '심연'];
        const curseColors = [0x44cc44, 0x88cc44, 0xcccc44, 0xff8844, 0xff4444, 0xff0022, 0xcc00ff, 0xcc00ff];
        const curseTextColors = ['#44cc44', '#88cc44', '#cccc44', '#ff8844', '#ff4444', '#ff0022', '#cc00ff', '#cc00ff'];
        const cIdx = Math.min(this.curseLevel, 7);

        _h(this.add.text(200, 10, '저주:', {
            fontSize: '11px', fontFamily: 'monospace', color: '#886688'
        }).setDepth(101));

        const barX = 250, barY = 16, barW = 180, barH = 10;
        _h(this.add.rectangle(barX + barW / 2, barY + barH / 2, barW, barH, 0x221133).setDepth(101));
        const maxCurse = 8;
        const fillW = Math.min(barW, (this.curseLevel / maxCurse) * barW);
        if (fillW > 0) {
            _h(this.add.rectangle(barX + fillW / 2, barY + barH / 2, fillW, barH, curseColors[cIdx]).setDepth(102));
        }

        _h(this.add.text(440, 10, `Lv.${this.curseLevel} ${curseNames[cIdx]}`, {
            fontSize: '12px', fontFamily: 'monospace', color: curseTextColors[cIdx], fontStyle: 'bold'
        }).setDepth(101));

        // Gold
        this.goldText = _h(this.add.text(570, 10, `💰 ${this.totalGold}G`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44'
        }).setDepth(101));

        // Weight
        const weight = this._getWeight();
        this.weightText = _h(this.add.text(670, 10, `무게: ${weight}/40kg`, {
            fontSize: '12px', fontFamily: 'monospace', color: weight > 32 ? '#ff4444' : weight > 24 ? '#ffaa44' : '#88aa88'
        }).setDepth(101));

        // Moves
        this.moveText = _h(this.add.text(800, 10, `이동: ${this.moveCount}/${this.maxMoves}`, {
            fontSize: '12px', fontFamily: 'monospace', color: this.moveCount > this.maxMoves * 0.8 ? '#ff4444' : '#886688'
        }).setDepth(101));

        // Key
        if (this.hasKey) {
            _h(this.add.text(940, 10, '🔑 열쇠', {
                fontSize: '11px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
            }).setDepth(101));
        }

        // Clues
        if (this.cluesCollected > 0) {
            _h(this.add.text(1010, 10, `📜 단서 ${this.cluesCollected}/5`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#88ccff', fontStyle: 'bold'
            }).setDepth(101));
        }

        // Floor info line
        _h(this.add.text(30, 35, `구역 Lv.${this.zoneLevel} | ${this.currentFloor}/${this._maxFloors}층 | 조건: ${this.clearCondition ? this.clearCondition.desc : ''}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#665577'
        }).setDepth(101));

        // Adaptation icons
        if (this.adaptations.length > 0) {
            const adaptIcons = {
                dark_eyes: '👁', ghost_memory: '👻', curse_absorb: '🌀',
                shadow_step: '🥷', mansion_eye: '🔮', crack_detect: '⚡', ghost_pact: '💀'
            };
            let ax = 400;
            this.adaptations.forEach(key => {
                _h(this.add.text(ax, 35, adaptIcons[key] || '?', {
                    fontSize: '12px'
                }).setDepth(101));
                ax += 20;
            });
        }

        // Curse avatar warning
        if (this._curseAvatarActive) {
            const warn = _h(this.add.text(1200, 35, '⚠ 저주 화신 추격 중', {
                fontSize: '10px', fontFamily: 'monospace', color: '#ff00cc', fontStyle: 'bold'
            }).setOrigin(1, 0).setDepth(101));
            this.tweens.add({ targets: warn, alpha: 0.4, duration: 600, yoyo: true, repeat: -1 });
        }

        // Darkness overlay based on curse
        if (this.curseLevel > 0) {
            const darkness = _h(this.add.rectangle(640, 360, 1280, 720, 0x000000,
                Math.min(0.35, this.curseLevel * 0.05)).setDepth(2));
            this.tweens.add({ targets: darkness, alpha: darkness.alpha * 0.6, duration: 2000, yoyo: true, repeat: -1 });
        }
    }

    // ==================== GRID DRAWING ====================

    _drawGrid() {
        this._clearGroup(this._gridObjects);
        const size = this._gridSize;

        const maxCellSize = Math.min(90, Math.floor(480 / size));
        const cellSize = maxCellSize;
        const gap = Math.max(3, 6 - (size - 4));
        const totalW = size * cellSize + (size - 1) * gap;
        const totalH = size * cellSize + (size - 1) * gap;
        const startX = 440 - totalW / 2;
        const startY = 340 - totalH / 2;

        const _g = (obj) => { this._gridObjects.push(obj); return obj; };

        const roomIcons = {
            unknown: '?', entrance: '🏠', exit: '🚪', empty: '·',
            enemy: '💀', item: '💎', trap: '⚠', rest: '😴', locked: '🔒',
            ambush: '⚡', clue: '📜', secret: '✨', boss: '👑', wall: '▪'
        };
        const roomColors = {
            unknown: 0x111122, entrance: 0x223344, exit: 0x224422,
            empty: 0x1a1a2a, enemy: 0x331122, item: 0x222233,
            trap: 0x332211, rest: 0x112233, locked: 0x222200,
            ambush: 0x331133, clue: 0x112244, secret: 0x332244,
            boss: 0x331111, wall: 0x0a0a0a
        };
        const roomBorderColors = {
            unknown: 0x222233, entrance: 0x446688, exit: 0x44aa44,
            empty: 0x333344, enemy: 0x884444, item: 0x4488ff,
            trap: 0xff8844, rest: 0x4488cc, locked: 0xaaaa44,
            ambush: 0xcc44cc, clue: 0x4488ff, secret: 0xffcc44,
            boss: 0xff2222, wall: 0x222222
        };

        for (let y = 0; y < size; y++) {
            for (let x = 0; x < size; x++) {
                const room = this.grid[y][x];
                const cx = startX + x * (cellSize + gap) + cellSize / 2;
                const cy = startY + y * (cellSize + gap) + cellSize / 2;

                if (room.type === 'wall') {
                    const gfx = _g(this.add.graphics().setDepth(10));
                    gfx.fillStyle(0x0a0a0a, 0.8);
                    gfx.fillRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 4);
                    gfx.lineStyle(1, 0x1a1a1a, 0.5);
                    gfx.strokeRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 4);
                    _g(this.add.text(cx, cy, '▪', {
                        fontSize: '16px', fontFamily: 'monospace', color: '#1a1a1a'
                    }).setOrigin(0.5).setDepth(11));
                    continue;
                }

                const isPlayer = (x === this.playerX && y === this.playerY);
                const isAdjacent = this._isAdjacent(x, y, this.playerX, this.playerY);
                const canMove = isAdjacent && !this.inCombat && room.revealed && room.type !== 'wall';

                const gfx = _g(this.add.graphics().setDepth(10));

                if (!room.revealed) {
                    gfx.fillStyle(0x111122, 1);
                    gfx.fillRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);
                    gfx.lineStyle(1, 0x222233, 0.5);
                    gfx.strokeRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);
                    _g(this.add.text(cx, cy, '?', {
                        fontSize: Math.max(16, cellSize * 0.3) + 'px', fontFamily: 'monospace', color: '#333344'
                    }).setOrigin(0.5).setDepth(11));
                } else if (room.visited && room.type !== 'entrance') {
                    gfx.fillStyle(0x0e0e1a, 1);
                    gfx.fillRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);
                    gfx.lineStyle(1, 0x222233, 0.4);
                    gfx.strokeRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);
                    _g(this.add.text(cx, cy - 4, roomIcons[room.type] || '·', {
                        fontSize: Math.max(14, cellSize * 0.22) + 'px', fontFamily: 'monospace', color: '#333344'
                    }).setOrigin(0.5).setDepth(11));
                    _g(this.add.text(cx, cy + cellSize * 0.25, '탐색됨', {
                        fontSize: '8px', fontFamily: 'monospace', color: '#333344'
                    }).setOrigin(0.5).setDepth(11));
                } else {
                    const bgColor = roomColors[room.type] || 0x1a1a2a;
                    const borderColor = isPlayer ? 0x8844ff : (roomBorderColors[room.type] || 0x333344);
                    const borderWidth = isPlayer ? 3 : (canMove ? 2 : 1);
                    const borderAlpha = isPlayer ? 1 : (canMove ? 0.8 : 0.5);

                    gfx.fillStyle(bgColor, isPlayer ? 0.9 : 0.7);
                    gfx.fillRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);
                    gfx.lineStyle(borderWidth, borderColor, borderAlpha);
                    gfx.strokeRoundedRect(cx - cellSize / 2, cy - cellSize / 2, cellSize, cellSize, 6);

                    // Show type if adaptation "mansion_eye" or room is known
                    const showType = room.type !== 'unknown' || (this._hasAdaptation('mansion_eye') && isAdjacent);
                    const displayType = showType ? room.type : 'unknown';
                    const icon = displayType === 'unknown' ? '?' : (roomIcons[displayType] || '?');
                    const iconColor = isPlayer ? '#bb88ff' : '#aaaacc';
                    _g(this.add.text(cx, cy - cellSize * 0.1, icon, {
                        fontSize: Math.max(16, cellSize * 0.28) + 'px', fontFamily: 'monospace', color: iconColor
                    }).setOrigin(0.5).setDepth(11));

                    if (isPlayer) {
                        _g(this.add.text(cx, cy + cellSize * 0.28, '👤', {
                            fontSize: Math.max(12, cellSize * 0.18) + 'px'
                        }).setOrigin(0.5).setDepth(12));
                        const glow = _g(this.add.circle(cx, cy, cellSize / 2 + 4, 0x8844ff, 0.08).setDepth(9));
                        this.tweens.add({ targets: glow, alpha: 0.15, duration: 1000, yoyo: true, repeat: -1 });
                    }

                    if (displayType !== 'unknown' && displayType !== 'entrance' && !isPlayer) {
                        const typeNames = {
                            exit: '출구', enemy: '적', item: '보물', trap: '함정', rest: '휴식',
                            locked: '잠김', ambush: '기습', clue: '단서', secret: '비밀', boss: '보스'
                        };
                        if (typeNames[displayType]) {
                            _g(this.add.text(cx, cy + cellSize * 0.28, typeNames[displayType], {
                                fontSize: '8px', fontFamily: 'monospace', color: '#666677'
                            }).setOrigin(0.5).setDepth(11));
                        }
                    }

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
                        hitZone.on('pointerdown', () => this._moveToRoom(x, y));

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

            const iconColor = isDead ? 0x333333 : base.color;
            _p(this.add.circle(panelX + 25, my + 15, 12, iconColor, isDead ? 0.3 : 0.8).setDepth(11));

            const roleIcons = { tank: '🛡', melee_dps: '⚔', ranged_dps: '🏹', healer: '💚', support: '🔮' };
            _p(this.add.text(panelX + 25, my + 15, roleIcons[base.role] || '⚔', {
                fontSize: '10px'
            }).setOrigin(0.5).setDepth(12));

            const nameColor = isDead ? '#444444' : '#cccccc';
            _p(this.add.text(panelX + 45, my + 2, `${merc.name}`, {
                fontSize: '11px', fontFamily: 'monospace', color: nameColor, fontStyle: isDead ? '' : 'bold'
            }).setDepth(11));

            _p(this.add.text(panelX + 45, my + 17, `${base.name} Lv.${merc.level}`, {
                fontSize: '9px', fontFamily: 'monospace', color: isDead ? '#333333' : '#666677'
            }).setDepth(11));

            if (isDead) {
                _p(this.add.text(panelX + 280, my + 10, '전사', {
                    fontSize: '11px', fontFamily: 'monospace', color: '#662222', fontStyle: 'bold'
                }).setDepth(11));
            } else {
                const barX = panelX + 150, barY2 = my + 6, barW = 150, barH = 8;
                _p(this.add.rectangle(barX + barW / 2, barY2 + barH / 2, barW, barH, 0x221122).setDepth(11));
                const hpFillW = barW * Math.max(0, hpRatio);
                const hpColor = hpRatio > 0.6 ? 0x44ff44 : hpRatio > 0.3 ? 0xffaa00 : 0xff4444;
                if (hpFillW > 0) {
                    _p(this.add.rectangle(barX + hpFillW / 2, barY2 + barH / 2, hpFillW, barH, hpColor).setDepth(12));
                }
                _p(this.add.text(barX + barW / 2, barY2 + barH + 6, `${merc.currentHp}/${stats.hp}`, {
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
            _i(this.add.rectangle(ix + 40, barY + 22, 82, 32, 0x111122, 0.9)
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

        if (room.type === 'wall') return;

        if (room.type === 'locked' && !this.hasKey) {
            this._showLockedRoom(x, y);
            return;
        }

        // Secret room requires curse >= 3
        if (room.type === 'secret' && this.curseLevel < 3) {
            DamagePopup.show(this, 640, 340, '저주 Lv.3 이상 필요...', 0x886688, false);
            return;
        }

        this.playerX = x;
        this.playerY = y;
        this.moveCount++;
        this.roomsVisited++;

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

        // Curse avatar random encounter
        if (this._curseAvatarActive && Math.random() < 0.25) {
            this._drawGrid();
            this._drawHUD();
            this._startCurseAvatarCombat();
            return;
        }

        this._drawGrid();
        this._drawHUD();

        if (this._checkMoveLimit()) return;

        this._handleRoom(x, y);
    }

    _handleRoom(x, y) {
        const room = this.grid[y][x];

        switch (room.type) {
            case 'empty':
            case 'entrance':
                if (room.type === 'empty') this._addCurse(1);
                break;
            case 'enemy':
                this._startCombat(room.depth);
                break;
            case 'ambush':
                this._startAmbushCombat(room.depth);
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
            case 'clue':
                this._handleClueRoom(x, y);
                break;
            case 'secret':
                this._handleSecretRoom(x, y);
                break;
            case 'boss':
                this._startBossCombat();
                break;
            case 'exit':
                this._checkFloorClear();
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

        const flash = this.add.rectangle(640, 360, 1280, 720, 0x8844ff, 0.3).setDepth(25);
        this.tweens.add({ targets: flash, alpha: 0, duration: 500, onComplete: () => flash.destroy() });

        // Spawn allies
        let allyIdx = 0;
        this.party.forEach((merc) => {
            if (!merc.alive) return;
            const ax = 180 + allyIdx * 70;
            const ay = 430 + (allyIdx % 2 === 0 ? 0 : 15);
            const unit = BattleUnit.fromMercenary(this, merc, ax, ay);
            this.allies.push(unit);
            this.allUnits.push(unit);
            this._combatObjects.push(unit);
            allyIdx++;
        });

        // Determine enemy count & types per floor
        const enemyCount = Math.min(4, 1 + Math.floor((depth + this.curseLevel) / 3));
        const adaptHpMult = 1 + this.adaptationCount * 0.1;
        const scaleMult = (1 + depth * 0.15 + this.curseLevel * 0.10) * this._getCurseEnemyHpMult() * adaptHpMult;

        const typePool = this._getEnemyPool();
        const totalWeight = typePool.reduce((s, t) => s + t.weight, 0);
        const pickType = () => {
            let roll = Math.random() * totalWeight;
            for (const entry of typePool) {
                roll -= entry.weight;
                if (roll <= 0) return entry.type;
            }
            return 'wraith';
        };

        const enemyStartX = 800;
        for (let i = 0; i < enemyCount; i++) {
            const ex = enemyStartX + i * 75;
            const ey = 420 + (i % 2 === 0 ? 0 : 15);
            const unit = BattleUnit.fromEnemyData(this, pickType(), scaleMult, ex, ey);
            this.enemies.push(unit);
            this.allUnits.push(unit);
            this._combatObjects.push(unit);
        }

        this._addCombatUI('⚔ 전투 발생!', depth);

        this.cameras.main.shake(100, 0.003);
    }

    _startAmbushCombat(depth) {
        // Apply pre-emptive damage unless adapted
        if (!this._hasAdaptation('shadow_step')) {
            this.party.forEach(merc => {
                if (!merc.alive) return;
                const stats = merc.getStats();
                const dmg = Math.floor(stats.hp * 0.1);
                merc.currentHp = Math.max(1, merc.currentHp - dmg);
            });
            DamagePopup.show(this, 640, 250, '기습! 선제 피해!', 0xcc44cc, false);
            this.cameras.main.shake(150, 0.004);
            this._drawPartyPanel();
        } else {
            DamagePopup.show(this, 640, 250, '그림자 발걸음: 기습 무효!', 0x44ff88, false);
        }

        this.time.delayedCall(400, () => this._startCombat(depth));
    }

    _startBossCombat() {
        this.inCombat = true;
        this.battleOver = false;
        this.battleTime = 0;
        this.allies = [];
        this.enemies = [];
        this.allUnits = [];

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
            const ax = 180 + allyIdx * 70;
            const ay = 430 + (allyIdx % 2 === 0 ? 0 : 15);
            const unit = BattleUnit.fromMercenary(this, merc, ax, ay);
            this.allies.push(unit);
            this.allUnits.push(unit);
            this._combatObjects.push(unit);
            allyIdx++;
        });

        // Boss + minions
        const bossScale = 1.0 + this.currentFloor * 0.2 + this.curseLevel * 0.05;
        const boss = BattleUnit.fromEnemyData(this, 'mansion_lord', bossScale, 900, 420);
        this.enemies.push(boss);
        this.allUnits.push(boss);
        this._combatObjects.push(boss);

        // Minions
        const minionCount = Math.min(2, this.currentFloor - 1);
        for (let i = 0; i < minionCount; i++) {
            const mType = i === 0 ? 'elite_wraith' : 'elite_cursed';
            const unit = BattleUnit.fromEnemyData(this, mType, bossScale * 0.8, 780 + i * 90, 430);
            this.enemies.push(unit);
            this.allUnits.push(unit);
            this._combatObjects.push(unit);
        }

        this._isBossFight = true;
        this._addCombatUI('👑 저택 주인 등장!', 5);

        this.cameras.main.shake(200, 0.005);
    }

    _startCurseAvatarCombat() {
        this.inCombat = true;
        this.battleOver = false;
        this.battleTime = 0;
        this.allies = [];
        this.enemies = [];
        this.allUnits = [];

        this._gridObjects.forEach(obj => { if (obj && obj.setAlpha) obj.setAlpha(0.3); });
        this._partyPanelObjects.forEach(obj => { if (obj && obj.setAlpha) obj.setAlpha(0.3); });
        this._inventoryBarObjects.forEach(obj => { if (obj && obj.setAlpha) obj.setAlpha(0.3); });

        const combatBg = this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.6).setDepth(20);
        this._combatObjects.push(combatBg);

        const ground = this.add.graphics().setDepth(20);
        ground.fillStyle(0x110a22, 1);
        ground.fillRect(0, 450, 1280, 270);
        ground.fillStyle(0x330033, 1);
        ground.fillRect(0, 448, 1280, 4);
        this._combatObjects.push(ground);

        let allyIdx = 0;
        this.party.forEach((merc) => {
            if (!merc.alive) return;
            const ax = 180 + allyIdx * 70;
            const ay = 430 + (allyIdx % 2 === 0 ? 0 : 15);
            const unit = BattleUnit.fromMercenary(this, merc, ax, ay);
            this.allies.push(unit);
            this.allUnits.push(unit);
            this._combatObjects.push(unit);
            allyIdx++;
        });

        const avatarScale = 1.0 + this.curseLevel * 0.15;
        const avatar = BattleUnit.fromEnemyData(this, 'curse_avatar', avatarScale, 900, 420);
        this.enemies.push(avatar);
        this.allUnits.push(avatar);
        this._combatObjects.push(avatar);

        this._addCombatUI('⚡ 저주 화신 습격!', 5);
        this.cameras.main.shake(200, 0.006);
    }

    _getEnemyPool() {
        if (this.currentFloor <= 1) {
            return [
                { type: 'wraith', weight: 50 },
                { type: 'bone_golem', weight: 30 },
                { type: 'cursed_mage', weight: 20 }
            ];
        }
        if (this.currentFloor === 2) {
            return [
                { type: 'wraith', weight: 25 },
                { type: 'cursed_mage', weight: 30 },
                { type: 'bone_golem', weight: 20 },
                { type: 'shade', weight: 25 }
            ];
        }
        return [
            { type: 'wraith', weight: 15 },
            { type: 'cursed_mage', weight: 25 },
            { type: 'shade', weight: 30 },
            { type: 'bone_golem', weight: 15 },
            { type: 'elite_wraith', weight: 15 }
        ];
    }

    _addCombatUI(label, depth) {
        const combatLabel = this.add.text(640, 80, label, {
            fontSize: '22px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(200).setAlpha(0);
        this.tweens.add({ targets: combatLabel, alpha: 1, duration: 300, yoyo: true, hold: 600,
            onComplete: () => combatLabel.destroy() });

        this.combatEnemyText = this.add.text(1200, 80, `적: ${this.enemies.length}`, {
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

                let goldDrop = Phaser.Math.Between(8, 18) + depth * 4 + this.currentFloor * 5;
                this.totalGold += goldDrop;
                if (this.goldText) this.goldText.setText(`💰 ${this.totalGold}G`);

                const gText = this.add.text(unit.container.x, unit.container.y - 10, `+${goldDrop}G`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
                    stroke: '#000', strokeThickness: 2
                }).setOrigin(0.5).setDepth(90);
                this.tweens.add({ targets: gText, y: gText.y - 30, alpha: 0, duration: 800, onComplete: () => gText.destroy() });

                this.totalXp += 5 + depth * 2 + this.currentFloor * 3;

                const dropMult = this._hasAdaptation('ghost_memory') ? 0.9 : 1.0;
                if (Math.random() < 0.25 * dropMult) {
                    const rarityBonus = Math.floor(depth * 0.5 + this.curseLevel * 0.3);
                    const item = generateItem('blackout', this.gameState.guildLevel, rarityBonus);
                    if (item) {
                        this.loot.push(item);
                        this._showLootPopup(unit.container.x, unit.container.y - 40, item);
                    }
                }

                if (!this.hasKey && Math.random() < 0.12) {
                    this.hasKey = true;
                    DamagePopup.show(this, unit.container.x, unit.container.y - 50, '🔑 열쇠 획득!', 0xffcc44, false);
                }
            }

            this._checkCombatEnd();
        };
    }

    _checkCombatEnd() {
        if (this.battleOver) return;
        const allyAlive = this.allies.filter(u => u.alive);
        const enemyAlive = this.enemies.filter(u => u.alive);

        if (allyAlive.length === 0) {
            this.battleOver = true;
            this.cameras.main.shake(200, 0.005);
            this._snapshotAllyState();
            this._addCurse(3);
            this.time.delayedCall(800, () => this._endRun(false));
            return;
        }

        if (enemyAlive.length === 0) {
            this.battleOver = true;
            this.cameras.main.shake(100, 0.002);
            this._snapshotAllyState();

            if (this._isBossFight) {
                this.bossDefeated = true;
                this._isBossFight = false;
            }

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

        this._combatObjects.forEach(obj => {
            if (obj && obj.destroy) obj.destroy();
        });
        this._combatObjects = [];

        this.allUnits.forEach(u => { if (u.destroy) u.destroy(); });
        this.allies = [];
        this.enemies = [];
        this.allUnits = [];

        if (!won && !fled) return;

        this._returnToGrid();
    }

    _returnToGrid() {
        this._drawGrid();
        this._drawHUD();
        this._drawPartyPanel();
        this._drawInventoryBar();
    }

    // ==================== ROOM HANDLERS ====================

    _handleClueRoom(x, y) {
        this.cluesCollected++;
        DamagePopup.show(this, 640, 300, `📜 단서 획득! (${this.cluesCollected}/5)`, 0x88ccff, false);

        if (this.cluesCollected >= 5) {
            this.cluesCollected -= 5;
            this._reduceCurse(2);
            DamagePopup.show(this, 640, 340, '정화 의식! 저주 -2', 0x44ff88, false);
            this.cameras.main.flash(300, 100, 100, 255);
        }

        this.grid[y][x].type = 'empty';
        this._drawGrid();
        this._drawHUD();
    }

    _handleSecretRoom(x, y) {
        this._clearGroup(this._overlayObjects);
        const _o = (obj) => { this._overlayObjects.push(obj); return obj; };

        _o(this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.5).setDepth(50));

        const popW = 400, popH = 260;
        _o(this.add.rectangle(640, 340, popW, popH, 0x222233, 0.95)
            .setStrokeStyle(2, 0xffcc44, 0.8).setDepth(51));

        _o(this.add.text(640, 240, '✨ 비밀 방!', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(52));

        _o(this.add.text(640, 270, '고급 보상을 발견했다!', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa9944'
        }).setOrigin(0.5).setDepth(52));

        // High-rarity item
        const rarityBonus = 3 + this.currentFloor;
        const item = generateItem('blackout', this.gameState.guildLevel, rarityBonus);
        if (item) {
            const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
            _o(this.add.rectangle(640, 330, 340, 38, 0x1a1a2a, 0.9)
                .setStrokeStyle(1, rarity.color, 0.6).setDepth(52));
            _o(this.add.text(640, 330, `${item.name} (${item.weight}kg)`, {
                fontSize: '12px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(53));

            const pickBtn = _o(this.add.text(640, 380, '획득', {
                fontSize: '14px', fontFamily: 'monospace', color: '#44ff88',
                backgroundColor: '#1a331a', padding: { x: 16, y: 6 }
            }).setOrigin(0.5).setDepth(53).setInteractive({ useHandCursor: true }));
            pickBtn.on('pointerdown', () => {
                if (this._getWeight() + item.weight > 40) {
                    DamagePopup.show(this, 640, 280, '무게 초과!', 0xff4444, false);
                    return;
                }
                this.loot.push(item);
                DamagePopup.show(this, 640, 280, `${item.name} 획득!`, rarity.color, false);
                this._clearGroup(this._overlayObjects);
                this._drawInventoryBar();
            });
        }

        // Bonus gold
        const bonusGold = 30 + this.currentFloor * 20;
        this.totalGold += bonusGold;
        DamagePopup.show(this, 640, 360, `+${bonusGold}G`, 0xffcc44, false);

        const closeBtn = _o(this.add.text(640, 430, '닫기', {
            fontSize: '12px', fontFamily: 'monospace', color: '#888899',
            backgroundColor: '#222233', padding: { x: 16, y: 6 }
        }).setOrigin(0.5).setDepth(53).setInteractive({ useHandCursor: true }));
        closeBtn.on('pointerdown', () => {
            this._clearGroup(this._overlayObjects);
            this.grid[y][x].type = 'empty';
            this._drawGrid();
            this._drawHUD();
        });
    }

    // ==================== ITEM POPUP ====================

    _showItemPopup(depth, bonusRarity) {
        this._clearGroup(this._overlayObjects);
        const _o = (obj) => { this._overlayObjects.push(obj); return obj; };

        _o(this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.5).setDepth(50));

        const popW = 400, popH = 280;
        _o(this.add.rectangle(640, 340, popW, popH, 0x111122, 0.95)
            .setStrokeStyle(2, 0x4488ff, 0.7).setDepth(51));

        _o(this.add.text(640, 220, '💎 보물 발견!', {
            fontSize: '20px', fontFamily: 'monospace', color: '#4488ff', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(52));

        const itemCount = 1 + (Math.random() < 0.4 ? 1 : 0);
        const rarityBonus = Math.floor(depth * 0.5 + this.curseLevel * 0.3) + (bonusRarity || 0);
        const items = [];

        for (let i = 0; i < itemCount; i++) {
            const item = generateItem('blackout', this.gameState.guildLevel, rarityBonus);
            if (item) items.push(item);
        }

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

                    if (!this.hasKey && Math.random() < 0.2) {
                        this.hasKey = true;
                        DamagePopup.show(this, 640, 280, '🔑 열쇠 발견!', 0xffcc44, false);
                        this._drawHUD();
                    }
                });
            }
        });

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

        // Trap warning from adaptation
        if (this._hasAdaptation('crack_detect')) {
            _o(this.add.text(640, 240, '⚡ 균열 감지: 함정 경고!', {
                fontSize: '11px', fontFamily: 'monospace', color: '#44ccff'
            }).setOrigin(0.5).setDepth(52));
        }

        _o(this.add.text(640, 260, '⚠ 함정 발견!', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(52));

        const dodgeChance = Math.max(10, 60 - this.curseLevel * 5);

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
                const trapMult = this._getCurseTrapMult();
                this._applyTrapDamage(0.1 * trapMult, 0.2 * trapMult);
            }
            this._addCurse(2);
        });

        const forceBtn = _o(this.add.text(640, 380, '💪 강행 돌파 (5-10% 피해, +20-40G)', {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffaa44',
            backgroundColor: '#332211', padding: { x: 12, y: 6 }
        }).setOrigin(0.5).setDepth(52).setInteractive({ useHandCursor: true }));
        forceBtn.on('pointerdown', () => {
            this._clearGroup(this._overlayObjects);
            const trapMult = this._getCurseTrapMult();
            this._applyTrapDamage(0.05 * trapMult, 0.10 * trapMult);
            const goldReward = Phaser.Math.Between(20, 40);
            this.totalGold += goldReward;
            DamagePopup.show(this, 640, 340, `강행 돌파! +${goldReward}G`, 0xffcc44, false);
            this._addCurse(2);
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
        this.party.forEach(merc => {
            if (!merc.alive) return;
            const stats = merc.getStats();
            const healAmt = Math.floor(stats.hp * 0.2);
            merc.currentHp = Math.min(stats.hp, merc.currentHp + healAmt);
        });

        DamagePopup.show(this, 640, 320, '😴 휴식! HP 20% 회복', 0x44ccff, false);

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

    // ==================== FLOOR CLEAR CHECK ====================

    _checkFloorClear() {
        const cond = this.clearCondition;
        let cleared = false;

        if (cond.type === 'exit') {
            cleared = true;
        } else if (cond.type === 'boss') {
            cleared = this.bossDefeated;
        } else if (cond.type === 'clues') {
            cleared = this.cluesCollected >= (cond.needed || 5);
        }

        if (!cleared) {
            DamagePopup.show(this, 640, 300, `클리어 조건 미달: ${cond.desc}`, 0xff8844, false);
            // Still allow escape
            this._showEscapeUI();
            return;
        }

        this._showFloorClearUI();
    }

    _showFloorClearUI() {
        this._clearGroup(this._overlayObjects);
        const _o = (obj) => { this._overlayObjects.push(obj); return obj; };

        _o(this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.6).setDepth(50));

        const popW = 520, popH = 400;
        _o(this.add.rectangle(640, 340, popW, popH, 0x112211, 0.95)
            .setStrokeStyle(2, 0x44aa44, 0.7).setDepth(51));

        _o(this.add.text(640, 170, `🏆 ${this.currentFloor}층 클리어!`, {
            fontSize: '22px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(52));

        _o(this.add.text(640, 200, `저주 Lv.${this.curseLevel} | 무게: ${this._getWeight()}/40kg`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#668866'
        }).setOrigin(0.5).setDepth(52));

        const hasNextFloor = this.currentFloor < this._maxFloors;

        // Option 1: Escape
        const escBtn = _o(this.add.rectangle(640, 260, 420, 45, 0x1a2a1a)
            .setStrokeStyle(1, 0x44aa44, 0.6).setDepth(52).setInteractive({ useHandCursor: true }));
        _o(this.add.text(640, 252, '🚪 탈출 (현재 보상 보존)', {
            fontSize: '14px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(53));
        _o(this.add.text(640, 270, '안전하게 귀환', {
            fontSize: '9px', fontFamily: 'monospace', color: '#448844'
        }).setOrigin(0.5).setDepth(53));
        escBtn.on('pointerover', () => escBtn.setFillStyle(0x2a3a2a));
        escBtn.on('pointerout', () => escBtn.setFillStyle(0x1a2a1a));
        escBtn.on('pointerdown', () => {
            this._clearGroup(this._overlayObjects);
            this.cameras.main.fadeOut(600, 0, 0, 0, (cam, progress) => {
                if (progress === 1) this._endRun(true);
            });
        });

        if (hasNextFloor) {
            // Option 2: Continue with adaptation
            const adaptBtn = _o(this.add.rectangle(640, 330, 420, 45, 0x2a1a2a)
                .setStrokeStyle(1, 0xcc44ff, 0.6).setDepth(52).setInteractive({ useHandCursor: true }));
            _o(this.add.text(640, 322, '🌀 적응 보상 선택 후 다음 층', {
                fontSize: '14px', fontFamily: 'monospace', color: '#cc88ff', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(53));
            _o(this.add.text(640, 340, '강화 + 디버프 쌍 최대 3개 선택', {
                fontSize: '9px', fontFamily: 'monospace', color: '#886688'
            }).setOrigin(0.5).setDepth(53));
            adaptBtn.on('pointerover', () => adaptBtn.setFillStyle(0x3a2a3a));
            adaptBtn.on('pointerout', () => adaptBtn.setFillStyle(0x2a1a2a));
            adaptBtn.on('pointerdown', () => {
                this._clearGroup(this._overlayObjects);
                this._showAdaptationUI();
            });

            // Option 3: Continue without
            const contBtn = _o(this.add.rectangle(640, 400, 420, 45, 0x1a1a2a)
                .setStrokeStyle(1, 0x4488cc, 0.6).setDepth(52).setInteractive({ useHandCursor: true }));
            _o(this.add.text(640, 392, '➡ 강화 없이 다음 층', {
                fontSize: '14px', fontFamily: 'monospace', color: '#4488cc', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(53));
            _o(this.add.text(640, 410, '기본 난이도로 계속', {
                fontSize: '9px', fontFamily: 'monospace', color: '#446688'
            }).setOrigin(0.5).setDepth(53));
            contBtn.on('pointerover', () => contBtn.setFillStyle(0x2a2a3a));
            contBtn.on('pointerout', () => contBtn.setFillStyle(0x1a1a2a));
            contBtn.on('pointerdown', () => {
                this._clearGroup(this._overlayObjects);
                this._goToNextFloor(0);
            });
        }

        // Summary stats
        _o(this.add.text(640, 460, `골드: ${this.totalGold}G | XP: ${this.totalXp} | 전리품: ${this.loot.length}개`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#556655'
        }).setOrigin(0.5).setDepth(52));
    }

    // ==================== ADAPTATION REWARDS ====================

    _showAdaptationUI() {
        this._clearGroup(this._overlayObjects);
        const _o = (obj) => { this._overlayObjects.push(obj); return obj; };

        _o(this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.7).setDepth(50));

        const ADAPTATIONS = [
            { key: 'dark_eyes', name: '어둠에 익숙해짐', icon: '👁',
                pos: '안개 제거 범위 +1칸', neg: '이동 속도 -5%' },
            { key: 'ghost_memory', name: '망령의 기억', icon: '👻',
                pos: '탐색한 방 재진입 시 전투 없음', neg: '새 방 드랍 -10%' },
            { key: 'curse_absorb', name: '저주 흡수', icon: '🌀',
                pos: '저주 레벨 높을수록 ATK +5%', neg: '저주 레벨 +1 즉시' },
            { key: 'shadow_step', name: '그림자 발걸음', icon: '🥷',
                pos: '기습 방 선제 피해 무효', neg: 'DEF -10%' },
            { key: 'mansion_eye', name: '저택의 눈', icon: '🔮',
                pos: '인접 방 타입 사전 공개', neg: 'HP 최대치 -10%' },
            { key: 'crack_detect', name: '균열 감지', icon: '⚡',
                pos: '함정 방 진입 전 경고', neg: 'CRIT -10%' },
            { key: 'ghost_pact', name: '혼령과의 계약', icon: '💀',
                pos: '사망 용병 자리 망령 소환', neg: '저주 레벨 +2 즉시' }
        ];

        // Filter out already chosen
        const available = ADAPTATIONS.filter(a => !this.adaptations.includes(a.key));

        // Show up to 4 random choices
        const shuffled = [...available].sort(() => Math.random() - 0.5);
        const choices = shuffled.slice(0, Math.min(4, shuffled.length));

        let selectedKeys = [];

        _o(this.add.text(640, 60, '🌀 적응 보상 선택', {
            fontSize: '20px', fontFamily: 'monospace', color: '#cc88ff', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(52));

        _o(this.add.text(640, 90, '최대 3개 선택 가능 — 많이 선택할수록 다음 층 난이도 상승', {
            fontSize: '11px', fontFamily: 'monospace', color: '#886688'
        }).setOrigin(0.5).setDepth(52));

        // Difficulty preview
        const diffText = _o(this.add.text(640, 110, '선택 0개: 기본 난이도', {
            fontSize: '10px', fontFamily: 'monospace', color: '#666677'
        }).setOrigin(0.5).setDepth(52));

        const updateDiffText = () => {
            const n = selectedKeys.length;
            const msgs = [
                '선택 0개: 기본 난이도',
                '선택 1개: 적 HP +10%',
                '선택 2개: 적 HP +10%, 기습 방 비율 증가',
                '선택 3개: 적 HP +20%, 저주 기본값 +1, 막힌 지형 증가'
            ];
            diffText.setText(msgs[Math.min(n, 3)]);
            diffText.setColor(n === 0 ? '#666677' : n === 1 ? '#cccc44' : n === 2 ? '#ff8844' : '#ff4444');
        };

        // Card layout
        const cardW = 280, cardH = 120, cardGap = 15;
        const cols = 2;
        const startX = 640 - (cols * cardW + (cols - 1) * cardGap) / 2 + cardW / 2;
        const startY = 180;

        const cardBgs = [];

        choices.forEach((adapt, idx) => {
            const col = idx % cols;
            const row = Math.floor(idx / cols);
            const cx = startX + col * (cardW + cardGap);
            const cy = startY + row * (cardH + cardGap) + cardH / 2;

            const bg = _o(this.add.rectangle(cx, cy, cardW, cardH, 0x1a1a2a, 0.95)
                .setStrokeStyle(2, 0x554466, 0.7).setDepth(52).setInteractive({ useHandCursor: true }));
            cardBgs.push({ bg, key: adapt.key });

            _o(this.add.text(cx, cy - 40, `${adapt.icon} ${adapt.name}`, {
                fontSize: '13px', fontFamily: 'monospace', color: '#cc88ff', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(53));

            _o(this.add.text(cx, cy - 10, `✅ ${adapt.pos}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#44ff88',
                wordWrap: { width: cardW - 20 }
            }).setOrigin(0.5).setDepth(53));

            _o(this.add.text(cx, cy + 20, `❌ ${adapt.neg}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#ff6644',
                wordWrap: { width: cardW - 20 }
            }).setOrigin(0.5).setDepth(53));

            bg.on('pointerdown', () => {
                const isSelected = selectedKeys.includes(adapt.key);
                if (isSelected) {
                    selectedKeys = selectedKeys.filter(k => k !== adapt.key);
                    bg.setStrokeStyle(2, 0x554466, 0.7);
                    bg.setFillStyle(0x1a1a2a, 0.95);
                } else {
                    if (selectedKeys.length >= 3) {
                        DamagePopup.show(this, 640, 440, '최대 3개까지!', 0xff4444, false);
                        return;
                    }
                    selectedKeys.push(adapt.key);
                    bg.setStrokeStyle(2, 0xcc88ff, 1);
                    bg.setFillStyle(0x2a1a3a, 0.95);
                }
                updateDiffText();
            });

            bg.on('pointerover', () => {
                if (!selectedKeys.includes(adapt.key)) {
                    bg.setFillStyle(0x221133, 0.95);
                }
            });
            bg.on('pointerout', () => {
                if (!selectedKeys.includes(adapt.key)) {
                    bg.setFillStyle(0x1a1a2a, 0.95);
                }
            });
        });

        // Confirm button
        const confirmBtn = _o(this.add.text(640, 530, '✅ 확정 후 다음 층 진입', {
            fontSize: '16px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold',
            backgroundColor: '#1a331a', padding: { x: 20, y: 8 }
        }).setOrigin(0.5).setDepth(53).setInteractive({ useHandCursor: true }));
        confirmBtn.on('pointerdown', () => {
            // Apply adaptations
            selectedKeys.forEach(key => {
                this.adaptations.push(key);
                this._applyAdaptationImmediate(key);
            });

            this._clearGroup(this._overlayObjects);
            this._goToNextFloor(selectedKeys.length);
        });

        // Skip button
        const skipBtn = _o(this.add.text(640, 580, '선택 안 하고 진입', {
            fontSize: '12px', fontFamily: 'monospace', color: '#666677',
            backgroundColor: '#1a1a2a', padding: { x: 12, y: 4 }
        }).setOrigin(0.5).setDepth(53).setInteractive({ useHandCursor: true }));
        skipBtn.on('pointerdown', () => {
            this._clearGroup(this._overlayObjects);
            this._goToNextFloor(0);
        });
    }

    _applyAdaptationImmediate(key) {
        switch (key) {
            case 'curse_absorb':
                this._addCurse(1);
                break;
            case 'ghost_pact':
                this._addCurse(2);
                break;
            case 'mansion_eye':
                this.party.forEach(merc => {
                    if (!merc.alive) return;
                    const stats = merc.getStats();
                    merc.currentHp = Math.min(merc.currentHp, Math.floor(stats.hp * 0.9));
                });
                break;
        }
    }

    _goToNextFloor(adaptCount) {
        this.adaptationCount += adaptCount;

        this.scene.restart({
            gameState: this.gameState,
            party: this.party,
            loot: this.loot,
            totalGold: this.totalGold,
            totalXp: this.totalXp,
            currentFloor: this.currentFloor + 1,
            curseLevel: this.curseLevel + (adaptCount >= 3 ? 1 : 0),
            adaptations: this.adaptations,
            adaptationCount: this.adaptationCount,
            cluesCollected: 0
        });
    }

    // ==================== ESCAPE UI (for uncleaned exit) ====================

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

        _o(this.add.text(640, 210, `무게: ${this._getWeight()}/40kg | 저주: Lv.${this.curseLevel}`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#668866'
        }).setOrigin(0.5).setDepth(52));

        // Stealth escape
        const stealthChance = this._getStealthChance();
        const stealthPct = Math.floor(stealthChance * 100);
        const stealthBtn = _o(this.add.rectangle(640, 270, 400, 45, 0x1a2a1a)
            .setStrokeStyle(1, 0x44aa44, 0.6).setDepth(52).setInteractive({ useHandCursor: true }));
        _o(this.add.text(640, 262, `🤫 은밀 탈출 (성공률: ${stealthPct}%)`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(53));
        _o(this.add.text(640, 280, '실패 시 저주 +1, 출구 이동', {
            fontSize: '9px', fontFamily: 'monospace', color: '#448844'
        }).setOrigin(0.5).setDepth(53));
        stealthBtn.on('pointerover', () => stealthBtn.setFillStyle(0x2a3a2a));
        stealthBtn.on('pointerout', () => stealthBtn.setFillStyle(0x1a2a1a));
        stealthBtn.on('pointerdown', () => {
            this._clearGroup(this._overlayObjects);
            this._attemptStealth();
        });

        // Combat breakthrough
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

        // Drop items
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
            DamagePopup.show(this, 640, 340, '탈출 실패! 저주 상승!', 0xff4444, false);
            this.cameras.main.shake(100, 0.003);
            this._addCurse(1);
            this._shuffleExit();
            this._drawGrid();
        }
    }

    _attemptCombatEscape() {
        this.inCombat = true;
        this.battleOver = false;
        this.battleTime = 0;
        this.allies = [];
        this.enemies = [];
        this.allUnits = [];

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

        let allyIdx = 0;
        this.party.forEach((merc) => {
            if (!merc.alive) return;
            const ax = 180 + allyIdx * 70;
            const ay = 430 + (allyIdx % 2 === 0 ? 0 : 15);
            const unit = BattleUnit.fromMercenary(this, merc, ax, ay);
            this.allies.push(unit);
            this.allUnits.push(unit);
            this._combatObjects.push(unit);
            allyIdx++;
        });

        const enemyCount = Math.min(4, 2 + Math.floor(this.curseLevel / 2));
        const scaleMult = 1.2 + this.curseLevel * 0.15;
        const eliteTypes = ['elite_wraith', 'elite_cursed', 'bone_golem', 'shade'];

        for (let i = 0; i < enemyCount; i++) {
            const ex = 800 + i * 75;
            const ey = 420 + (i % 2 === 0 ? 0 : 15);
            const eType = eliteTypes[i % eliteTypes.length];
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

        _o(this.add.text(640, 340 - popH / 2 + 45, `현재 무게: ${this._getWeight()}/40kg`, {
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
                    this._showDropItemsUI();
                });
            });
        }

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
        const size = this._gridSize;
        this.grid[this.exitY][size - 1].type = 'unknown';
        this.grid[this.exitY][size - 1].visited = false;

        let newY = Phaser.Math.Between(0, size - 1);
        while (newY === this.exitY) {
            newY = Phaser.Math.Between(0, size - 1);
        }
        this.exitY = newY;
        this.grid[this.exitY][size - 1] = { type: 'exit', visited: false, revealed: true, depth: size - 1 };

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
        const ratio = this._getWeight() / 40;
        let basePct;
        if (ratio <= 0.3) basePct = 0.95;
        else if (ratio <= 0.6) basePct = 0.80;
        else if (ratio <= 0.8) basePct = 0.50;
        else basePct = 0.20;
        return Math.max(0.05, basePct - this.curseLevel * 0.05);
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
        const floorLabel = this.currentFloor === 1 ? '진입' : `${this.currentFloor}층`;
        const txt = this.add.text(640, 300, `🔦 Blackout ${floorLabel}`, {
            fontSize: '32px', fontFamily: 'monospace', color: '#8844ff', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(200).setAlpha(0);
        this.tweens.add({
            targets: txt, alpha: 1, duration: 400, yoyo: true, hold: 800,
            onComplete: () => txt.destroy()
        });

        const gridLabel = `${this._gridSize}×${this._gridSize}`;
        const sub = this.add.text(640, 340, `구역 Lv.${this.zoneLevel} | ${gridLabel} | 이동 제한: ${this.maxMoves} | 조건: ${this.clearCondition ? this.clearCondition.desc : ''}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#665577',
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
            curseReached: this.curseLevel,
            floorsCleared: this.currentFloor - (success ? 0 : 1),
            adaptations: this.adaptations
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
