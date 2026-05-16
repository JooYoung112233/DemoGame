/**
 * 후보 1: 그리드 통합 전투 (Blackout Prototype).
 *
 * 핵심 메카닉:
 *  - 5×5 전투 그리드. 아군 4명 + 적 3-4마리가 셀에 배치
 *  - 턴제 (SPD 순). 각 턴 1유닛이 행동
 *  - 행동 옵션: 이동(인접), 공격(사거리 내), 스킬(쿨), 횃불 비추기, 패스
 *  - 시야: 횃불 든 용병 인접 8칸 = 빛 / 그 외 = 어둠
 *  - 어둠 셀 적은 위치 안 보임 → 인접 위험 숫자로만 추측 (마인스위퍼)
 *  - 빛 안의 적만 직접 공격 가능
 *
 * 클래스별 사거리/특성:
 *  - warrior  : 사거리 1 (인접 8방향), HP+ATK 강함
 *  - rogue    : 사거리 1, 어둠 셀에서 ATK +50% (그림자 암살)
 *  - mage     : 사거리 3 (직선/대각), AoE 스킬 (3×3)
 *  - archer   : 사거리 4 (직선만), 시야 확장 (인접 24칸)
 *  - priest   : 사거리 2, 빛 범위 5×5 + 힐 스킬
 *  - alchemist: 사거리 2 (지정 셀 + 8칸 폭발)
 */
class BlackoutGridScene extends Phaser.Scene {
    constructor() { super('BlackoutGridScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.party = data.party || [];
        this.zoneKey = data.zoneKey || 'blackout';
        this.zoneLevel = this.gameState.zoneLevel[this.zoneKey] || 1;
    }

    create() {
        this.GRID_SIZE = 5;
        this.CELL_SIZE = 90;
        this.GRID_X = 640 - (this.GRID_SIZE * this.CELL_SIZE) / 2;
        this.GRID_Y = 130;

        this.turnIndex = 0;
        this.round = 1;
        this.gameOver = false;
        this.selectedAction = null; // 'move' | 'attack' | 'skill' | 'torch' | null
        this.cooldowns = {}; // unitId → { skill: 0 }
        this.statusEffects = {}; // unitId → [{ type, duration, value }]
        this.torchUsedThisRound = {}; // unitId → bool

        this._cellObjects = [];
        this._unitObjects = [];
        this._actionPanelObjects = [];
        this._hudObjects = [];

        this._drawBackground();
        this._initGrid();
        this._spawnUnits();
        this._calcTurnQueue();
        this._drawHUD();
        this._drawGrid();
        this._drawUnits();
        this._drawActionPanel();

        this._showAnnounce('🔦 그리드 전투 — 빛으로 비추고, 어둠으로 숨어라', 0xbb88ff);
        this.time.delayedCall(800, () => this._beginTurn());
    }

    // ==================== INIT ====================

    _initGrid() {
        // grid[y][x] = { walkable, hasUnit, light }
        this.grid = [];
        for (let y = 0; y < this.GRID_SIZE; y++) {
            this.grid[y] = [];
            for (let x = 0; x < this.GRID_SIZE; x++) {
                this.grid[y][x] = {
                    walkable: true,
                    hasUnit: null,
                    light: false,
                    pillar: false
                };
            }
        }

        // 장애물 (기둥) 2-3개 무작위
        const obstacleCount = 2 + Math.floor(Math.random() * 2);
        let placed = 0, attempts = 0;
        while (placed < obstacleCount && attempts < 30) {
            const px = Phaser.Math.Between(1, this.GRID_SIZE - 2);
            const py = Phaser.Math.Between(1, this.GRID_SIZE - 2);
            if (!this.grid[py][px].pillar) {
                this.grid[py][px].pillar = true;
                this.grid[py][px].walkable = false;
                placed++;
            }
            attempts++;
        }
    }

    _spawnUnits() {
        this.units = [];

        // 아군 — 왼쪽 2열에 분산 배치
        this.party.forEach((merc, i) => {
            const stats = merc.getStats();
            const px = i < 2 ? 0 : 1;
            const py = (i % 2 === 0) ? 1 : 3;
            // 충돌 회피
            let spawnPos = this._findFreeNear(px, py);
            if (!spawnPos) spawnPos = { x: px, y: py };
            const u = {
                id: 'ally_' + merc.id,
                mercRef: merc,
                name: merc.name,
                classKey: merc.classKey,
                team: 'ally',
                x: spawnPos.x, y: spawnPos.y,
                hp: stats.hp, maxHp: stats.hp,
                atk: stats.atk, def: stats.def,
                spd: stats.moveSpeed || 100,
                alive: true,
                hasTorch: i === 0, // 첫 용병이 초기 횃불 보유
                ...this._getClassConfig(merc.classKey)
            };
            this.grid[u.y][u.x].hasUnit = u;
            this.units.push(u);
            this.cooldowns[u.id] = { skill: 0 };
            this.statusEffects[u.id] = [];
        });

        // 적 — 오른쪽 2열에 배치, 구역 레벨 기반
        const enemyCount = Math.min(4, 2 + Math.floor(this.zoneLevel / 3));
        const enemyTypes = ['shadow_lurker', 'wraith', 'curse_hound', 'shade_archer'];
        for (let i = 0; i < enemyCount; i++) {
            const type = enemyTypes[i % enemyTypes.length];
            const ex = this.GRID_SIZE - 1 - (i % 2);
            const ey = Math.floor(i / 2) * 2 + 1;
            const spawnPos = this._findFreeNear(ex, ey);
            if (!spawnPos) continue;
            const cfg = this._getEnemyConfig(type);
            const u = {
                id: 'enemy_' + i,
                name: cfg.name,
                classKey: type,
                team: 'enemy',
                x: spawnPos.x, y: spawnPos.y,
                hp: cfg.hp, maxHp: cfg.hp,
                atk: cfg.atk, def: cfg.def,
                spd: cfg.spd,
                alive: true,
                hasTorch: false,
                ...cfg
            };
            this.grid[u.y][u.x].hasUnit = u;
            this.units.push(u);
            this.cooldowns[u.id] = { skill: 0 };
            this.statusEffects[u.id] = [];
        }
    }

    _findFreeNear(x, y) {
        if (this.grid[y] && this.grid[y][x] && this.grid[y][x].walkable && !this.grid[y][x].hasUnit) {
            return { x, y };
        }
        for (let r = 1; r < this.GRID_SIZE; r++) {
            for (let dy = -r; dy <= r; dy++) {
                for (let dx = -r; dx <= r; dx++) {
                    const nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= this.GRID_SIZE || ny < 0 || ny >= this.GRID_SIZE) continue;
                    if (this.grid[ny][nx].walkable && !this.grid[ny][nx].hasUnit) return { x: nx, y: ny };
                }
            }
        }
        return null;
    }

    _getClassConfig(classKey) {
        const cfg = {
            warrior:   { range: 1, lightRadius: 1, icon: '🛡', color: 0xcc8844, shadowBuff: 1.0, lineOnly: false },
            rogue:     { range: 1, lightRadius: 1, icon: '🗡', color: 0xaa44cc, shadowBuff: 1.5, lineOnly: false },
            mage:      { range: 3, lightRadius: 1, icon: '🔮', color: 0x4488ff, shadowBuff: 1.0, lineOnly: false, aoe: 1 },
            archer:    { range: 4, lightRadius: 2, icon: '🏹', color: 0x88cc44, shadowBuff: 1.0, lineOnly: true },
            priest:    { range: 2, lightRadius: 2, icon: '✝', color: 0xffcc88, shadowBuff: 1.0, lineOnly: false, healSkill: true },
            alchemist: { range: 2, lightRadius: 1, icon: '⚗', color: 0x44cc88, shadowBuff: 1.0, lineOnly: false, aoe: 1 }
        };
        return cfg[classKey] || cfg.warrior;
    }

    _getEnemyConfig(type) {
        const base = 30 + this.zoneLevel * 8;
        const cfg = {
            shadow_lurker: { name: '그림자 잠복자', hp: base, atk: 14 + this.zoneLevel * 2, def: 2, spd: 110, range: 1, icon: '👤', color: 0x664488 },
            wraith:        { name: '망령',         hp: base - 10, atk: 18 + this.zoneLevel * 2, def: 0, spd: 130, range: 2, icon: '👻', color: 0x4488aa },
            curse_hound:   { name: '저주 사냥개', hp: base + 10, atk: 12 + this.zoneLevel * 2, def: 4, spd: 140, range: 1, icon: '🐺', color: 0x884422 },
            shade_archer:  { name: '그림자 사수', hp: base - 5, atk: 16 + this.zoneLevel * 2, def: 1, spd: 100, range: 3, icon: '🏹', color: 0x442266, lineOnly: true }
        };
        return cfg[type] || cfg.shadow_lurker;
    }

    // ==================== TURN MANAGEMENT ====================

    _calcTurnQueue() {
        const alive = this.units.filter(u => u.alive);
        alive.sort((a, b) => (b.spd - a.spd) + (Math.random() - 0.5));
        this.turnQueue = alive;
        this.turnIndex = 0;

        // 매 라운드 시작: 횃불 사용 초기화, 상태효과 처리
        alive.forEach(u => {
            this.torchUsedThisRound[u.id] = false;
            if (this.cooldowns[u.id] && this.cooldowns[u.id].skill > 0) {
                this.cooldowns[u.id].skill--;
            }
            this._tickStatusEffects(u);
        });
        this._updateLightMap();
    }

    _tickStatusEffects(u) {
        const effects = this.statusEffects[u.id] || [];
        const remaining = [];
        for (const e of effects) {
            if (e.type === 'curse_burn') {
                const dmg = Math.floor(u.maxHp * 0.05);
                u.hp = Math.max(0, u.hp - dmg);
                if (u.hp <= 0) {
                    u.alive = false;
                    this.grid[u.y][u.x].hasUnit = null;
                }
            }
            e.duration--;
            if (e.duration > 0) remaining.push(e);
        }
        this.statusEffects[u.id] = remaining;
    }

    _beginTurn() {
        if (this.gameOver) return;
        const u = this._currentUnit();
        if (!u) {
            this.round++;
            this._calcTurnQueue();
            this._drawHUD();
            this._drawUnits();
            this._showAnnounce(`라운드 ${this.round}`, 0xaaaaff);
            this.time.delayedCall(700, () => this._beginTurn());
            return;
        }

        if (!u.alive) {
            this.turnIndex++;
            this._beginTurn();
            return;
        }

        this._drawHUD();
        this._drawUnits();

        if (u.team === 'ally') {
            this.selectedAction = null;
            this._drawActionPanel();
        } else {
            this.time.delayedCall(600, () => this._aiTurn(u));
        }
    }

    _currentUnit() {
        if (this.turnIndex >= this.turnQueue.length) return null;
        return this.turnQueue[this.turnIndex];
    }

    _endTurn() {
        this.selectedAction = null;
        const end = this._checkBattleEnd();
        if (end) return;
        this.turnIndex++;
        this._drawGrid();
        this._drawUnits();
        this.time.delayedCall(300, () => this._beginTurn());
    }

    _checkBattleEnd() {
        const allies = this.units.filter(u => u.team === 'ally' && u.alive).length;
        const enemies = this.units.filter(u => u.team === 'enemy' && u.alive).length;
        if (enemies === 0) {
            this._endBattle(true);
            return true;
        }
        if (allies === 0) {
            this._endBattle(false);
            return true;
        }
        return false;
    }

    _endBattle(victory) {
        this.gameOver = true;
        this._clearGroup(this._actionPanelObjects);

        const overlay = this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.6).setDepth(200);
        const panel = this.add.graphics().setDepth(201);
        panel.fillStyle(0x111122, 0.95);
        panel.fillRoundedRect(440, 220, 400, 280, 10);
        panel.lineStyle(2, victory ? 0xffcc44 : 0xff4444, 0.8);
        panel.strokeRoundedRect(440, 220, 400, 280, 10);

        this.add.text(640, 270, victory ? '✨ 승리' : '💀 패배', {
            fontSize: '32px', fontFamily: 'monospace',
            color: victory ? '#ffcc44' : '#ff4444', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(202);

        this.add.text(640, 330, victory ? '모든 적을 처치했다' : '파티가 전멸했다', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ccccdd'
        }).setOrigin(0.5).setDepth(202);

        this.add.text(640, 370, `라운드: ${this.round}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5).setDepth(202);

        UIButton.create(this, 640, 430, 240, 38, '프로토타입 메뉴로', {
            color: 0x4477cc, hoverColor: 0x5588dd, textColor: '#ffffff', fontSize: 14,
            depth: 202,
            onClick: () => this.scene.start('BlackoutProtoSelectScene', {
                gameState: this.gameState,
                party: this.party,
                zoneKey: this.zoneKey
            })
        });

        UIButton.create(this, 640, 475, 240, 30, '마을로 돌아가기', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaccee', fontSize: 12,
            depth: 202,
            onClick: () => this.scene.start('TownScene', { gameState: this.gameState })
        });
    }

    // ==================== LIGHT MAP ====================

    _updateLightMap() {
        for (let y = 0; y < this.GRID_SIZE; y++) {
            for (let x = 0; x < this.GRID_SIZE; x++) {
                this.grid[y][x].light = false;
            }
        }
        // 횃불 든 아군 주변 lightRadius만큼 빛
        this.units.filter(u => u.team === 'ally' && u.alive && u.hasTorch).forEach(u => {
            const r = u.lightRadius || 1;
            for (let dy = -r; dy <= r; dy++) {
                for (let dx = -r; dx <= r; dx++) {
                    const nx = u.x + dx, ny = u.y + dy;
                    if (nx >= 0 && nx < this.GRID_SIZE && ny >= 0 && ny < this.GRID_SIZE) {
                        this.grid[ny][nx].light = true;
                    }
                }
            }
        });
    }

    _isInLight(x, y) {
        return this.grid[y] && this.grid[y][x] && this.grid[y][x].light;
    }

    _adjacentDangerCount(x, y) {
        let count = 0;
        for (let dy = -1; dy <= 1; dy++) {
            for (let dx = -1; dx <= 1; dx++) {
                if (dx === 0 && dy === 0) continue;
                const nx = x + dx, ny = y + dy;
                if (nx < 0 || nx >= this.GRID_SIZE || ny < 0 || ny >= this.GRID_SIZE) continue;
                const cell = this.grid[ny][nx];
                if (cell.hasUnit && cell.hasUnit.team === 'enemy' && cell.hasUnit.alive) count++;
            }
        }
        return count;
    }

    // ==================== DRAW ====================

    _drawBackground() {
        const gfx = this.add.graphics();
        gfx.fillGradientStyle(0x05050f, 0x05050f, 0x0a0a18, 0x0a0a18);
        gfx.fillRect(0, 0, 1280, 720);
        for (let i = 0; i < 25; i++) {
            const fx = Phaser.Math.Between(0, 1280);
            const fy = Phaser.Math.Between(0, 720);
            gfx.fillStyle(0x6644aa, 0.03);
            gfx.fillCircle(fx, fy, Phaser.Math.Between(30, 90));
        }
    }

    _drawHUD() {
        this._clearGroup(this._hudObjects);
        const _h = (o) => { this._hudObjects.push(o); return o; };

        _h(this.add.rectangle(640, 35, 1280, 70, 0x0a0a14, 0.9).setDepth(100));

        _h(this.add.text(20, 18, `🗺 그리드 전투 — 라운드 ${this.round}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#bb88ff', fontStyle: 'bold'
        }).setDepth(101));

        _h(this.add.text(20, 38, `구역 Lv.${this.zoneLevel} | 마인스위퍼식 시야 + 턴제`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#776688'
        }).setDepth(101));

        // 턴 큐 표시
        const u = this._currentUnit();
        if (u) {
            const turnLabel = u.team === 'ally' ? `🟢 ${u.name} 차례` : `🔴 ${u.name} 차례`;
            _h(this.add.text(640, 18, turnLabel, {
                fontSize: '14px', fontFamily: 'monospace',
                color: u.team === 'ally' ? '#88ff88' : '#ff8888', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(101));

            // 다음 3턴 미리보기
            const next = [];
            for (let i = 1; i < 5 && (this.turnIndex + i) < this.turnQueue.length; i++) {
                const n = this.turnQueue[this.turnIndex + i];
                if (n.alive) next.push(n);
            }
            let nx = 640 - 80;
            _h(this.add.text(nx, 42, '다음:', {
                fontSize: '9px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(1, 0.5).setDepth(101));
            next.slice(0, 4).forEach((n, i) => {
                const cfg = n.team === 'ally' ? this._getClassConfig(n.classKey) : this._getEnemyConfig(n.classKey);
                _h(this.add.text(nx + 20 + i * 28, 42, cfg.icon || '?', {
                    fontSize: '14px', color: n.team === 'ally' ? '#88aaff' : '#cc6666'
                }).setOrigin(0.5).setDepth(101));
            });
        }

        // 우상단: 횃불 보유 안내
        const torches = this.units.filter(u2 => u2.team === 'ally' && u2.alive && u2.hasTorch).length;
        _h(this.add.text(1260, 18, `🔦 횃불 보유 ${torches}/${this.party.length}`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#ffcc88'
        }).setOrigin(1, 0).setDepth(101));
        _h(this.add.text(1260, 38, `(횃불 주변 빛 = 어둠 적 보임)`, {
            fontSize: '9px', fontFamily: 'monospace', color: '#776688'
        }).setOrigin(1, 0).setDepth(101));

        // 좌측 메뉴 버튼
        _h(UIButton.create(this, 70, 700, 110, 25, '← 프로토 메뉴', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaccee', fontSize: 10, depth: 110,
            onClick: () => this.scene.start('BlackoutProtoSelectScene', {
                gameState: this.gameState, party: this.party, zoneKey: this.zoneKey
            })
        }));
    }

    _drawGrid() {
        this._clearGroup(this._cellObjects);
        const _g = (o) => { this._cellObjects.push(o); return o; };

        for (let y = 0; y < this.GRID_SIZE; y++) {
            for (let x = 0; x < this.GRID_SIZE; x++) {
                const cell = this.grid[y][x];
                const cx = this.GRID_X + x * this.CELL_SIZE + this.CELL_SIZE / 2;
                const cy = this.GRID_Y + y * this.CELL_SIZE + this.CELL_SIZE / 2;
                const isLight = cell.light;

                const gfx = _g(this.add.graphics().setDepth(10));
                if (cell.pillar) {
                    gfx.fillStyle(0x222230, 1);
                    gfx.fillRoundedRect(cx - 40, cy - 40, 80, 80, 6);
                    gfx.lineStyle(1, 0x44445a, 0.7);
                    gfx.strokeRoundedRect(cx - 40, cy - 40, 80, 80, 6);
                    _g(this.add.text(cx, cy, '▣', {
                        fontSize: '36px', color: '#555566'
                    }).setOrigin(0.5).setDepth(11));
                    continue;
                }

                const bgColor = isLight ? 0x2a2434 : 0x0a0a14;
                const borderColor = isLight ? 0xffcc88 : 0x222233;
                const borderAlpha = isLight ? 0.5 : 0.4;
                gfx.fillStyle(bgColor, 0.9);
                gfx.fillRoundedRect(cx - 40, cy - 40, 80, 80, 6);
                gfx.lineStyle(1, borderColor, borderAlpha);
                gfx.strokeRoundedRect(cx - 40, cy - 40, 80, 80, 6);

                // 어둠 셀이고 인접 위험 표시
                if (!isLight && !cell.hasUnit) {
                    const danger = this._adjacentDangerCount(x, y);
                    if (danger > 0) {
                        _g(this.add.text(cx, cy, danger.toString(), {
                            fontSize: '20px', fontFamily: 'monospace',
                            color: danger >= 3 ? '#ff4444' : danger === 2 ? '#ffaa44' : '#cccc44',
                            fontStyle: 'bold'
                        }).setOrigin(0.5).setDepth(12));
                    }
                }

                // 액션 모드일 때 셀 하이라이트
                if (this.selectedAction === 'move' || this.selectedAction === 'attack' || this.selectedAction === 'torch') {
                    this._maybeHighlightCell(gfx, x, y, cx, cy);
                }
            }
        }
    }

    _maybeHighlightCell(gfx, x, y, cx, cy) {
        const u = this._currentUnit();
        if (!u || u.team !== 'ally') return;
        const cell = this.grid[y][x];

        if (this.selectedAction === 'move') {
            // 인접 빈 셀
            const dist = Math.abs(u.x - x) + Math.abs(u.y - y);
            if (dist === 1 && cell.walkable && !cell.hasUnit) {
                gfx.lineStyle(2, 0x44ff88, 0.9);
                gfx.strokeRoundedRect(cx - 40, cy - 40, 80, 80, 6);
                this._addClickZone(cx, cy, 80, () => this._executeMove(x, y));
            }
        } else if (this.selectedAction === 'attack') {
            // 사거리 내 적 (빛 안)
            if (this._canAttackCell(u, x, y)) {
                const enemyAtCell = cell.hasUnit && cell.hasUnit.team === 'enemy' && cell.hasUnit.alive && cell.light;
                if (enemyAtCell) {
                    gfx.lineStyle(3, 0xff4444, 0.9);
                    gfx.strokeRoundedRect(cx - 40, cy - 40, 80, 80, 6);
                    this._addClickZone(cx, cy, 80, () => this._executeAttack(x, y));
                } else {
                    gfx.lineStyle(1, 0xff8844, 0.4);
                    gfx.strokeRoundedRect(cx - 40, cy - 40, 80, 80, 6);
                }
            }
        } else if (this.selectedAction === 'torch') {
            // 인접 아군에게 횃불 패스 가능
            const adj = Math.max(Math.abs(u.x - x), Math.abs(u.y - y));
            if (adj === 1 && cell.hasUnit && cell.hasUnit.team === 'ally' && cell.hasUnit.alive) {
                gfx.lineStyle(2, 0xffcc44, 0.9);
                gfx.strokeRoundedRect(cx - 40, cy - 40, 80, 80, 6);
                this._addClickZone(cx, cy, 80, () => this._executePassTorch(cell.hasUnit));
            }
        } else if (this.selectedAction === 'skill') {
            // 스킬 대상 표시
            if (this._canSkillCell(u, x, y)) {
                gfx.lineStyle(3, 0xbb88ff, 0.9);
                gfx.strokeRoundedRect(cx - 40, cy - 40, 80, 80, 6);
                this._addClickZone(cx, cy, 80, () => this._executeSkill(x, y));
            }
        }
    }

    _addClickZone(cx, cy, size, onClick) {
        const z = this.add.zone(cx, cy, size, size).setInteractive({ useHandCursor: true }).setDepth(50);
        z.on('pointerdown', onClick);
        this._cellObjects.push(z);
    }

    _drawUnits() {
        this._clearGroup(this._unitObjects);
        const _u = (o) => { this._unitObjects.push(o); return o; };
        const current = this._currentUnit();

        for (const u of this.units) {
            if (!u.alive) continue;
            const cx = this.GRID_X + u.x * this.CELL_SIZE + this.CELL_SIZE / 2;
            const cy = this.GRID_Y + u.y * this.CELL_SIZE + this.CELL_SIZE / 2;
            const inLight = this._isInLight(u.x, u.y);
            const isEnemyHidden = (u.team === 'enemy' && !inLight);

            // 적이 어둠에 있으면 ? 만 표시 (위치는 노출 X 그러나 셀 내 캐릭은 보이게)
            if (isEnemyHidden) {
                _u(this.add.text(cx, cy, '?', {
                    fontSize: '32px', fontFamily: 'monospace', color: '#552266', fontStyle: 'bold'
                }).setOrigin(0.5).setDepth(20).setAlpha(0.7));
                continue;
            }

            const cfg = u.team === 'ally' ? this._getClassConfig(u.classKey) : this._getEnemyConfig(u.classKey);
            const color = cfg.color || 0x888888;

            // 현재 턴 강조 링
            if (current && current.id === u.id) {
                const ring = _u(this.add.circle(cx, cy, 38, u.team === 'ally' ? 0x44ff88 : 0xff4444, 0).setDepth(19));
                ring.setStrokeStyle(3, u.team === 'ally' ? 0x44ff88 : 0xff4444, 0.9);
                this.tweens.add({ targets: ring, scale: 1.15, duration: 600, yoyo: true, repeat: -1 });
            }

            // 본체
            _u(this.add.circle(cx, cy - 5, 22, color, 0.85).setDepth(20));
            _u(this.add.text(cx, cy - 5, cfg.icon || '?', { fontSize: '20px' }).setOrigin(0.5).setDepth(21));

            // 횃불 표시
            if (u.hasTorch) {
                const torch = _u(this.add.text(cx + 18, cy - 22, '🔦', { fontSize: '14px' }).setDepth(22));
                this.tweens.add({ targets: torch, alpha: 0.6, duration: 700, yoyo: true, repeat: -1 });
            }

            // HP바
            const hpRatio = u.hp / u.maxHp;
            const barW = 60, barH = 5;
            _u(this.add.rectangle(cx, cy + 22, barW, barH, 0x220011).setDepth(22));
            const hpColor = hpRatio > 0.6 ? 0x44ff44 : hpRatio > 0.3 ? 0xffaa44 : 0xff4444;
            const fillW = Math.max(0, barW * hpRatio);
            if (fillW > 0) {
                _u(this.add.rectangle(cx - barW / 2 + fillW / 2, cy + 22, fillW, barH, hpColor).setDepth(23));
            }
            _u(this.add.text(cx, cy + 32, `${u.hp}/${u.maxHp}`, {
                fontSize: '9px', fontFamily: 'monospace', color: '#ccccdd'
            }).setOrigin(0.5).setDepth(23));

            // 이름 (아래 살짝)
            _u(this.add.text(cx, cy - 30, u.name, {
                fontSize: '9px', fontFamily: 'monospace', color: u.team === 'ally' ? '#aaccee' : '#cc8888'
            }).setOrigin(0.5).setDepth(22));

            // 상태효과
            const effects = this.statusEffects[u.id] || [];
            if (effects.length > 0) {
                _u(this.add.text(cx - 18, cy - 22, '🩸', { fontSize: '11px' }).setDepth(22));
            }
        }
    }

    _drawActionPanel() {
        this._clearGroup(this._actionPanelObjects);
        const _a = (o) => { this._actionPanelObjects.push(o); return o; };

        const u = this._currentUnit();
        if (!u || u.team !== 'ally') return;
        const cfg = this._getClassConfig(u.classKey);

        const panelY = 605;
        _a(this.add.rectangle(640, panelY + 35, 1180, 88, 0x0a0a14, 0.95).setDepth(99));
        _a(this.add.graphics().setDepth(99)
            .lineStyle(1, 0x332244, 0.7)
            .strokeRoundedRect(50, panelY, 1180, 88, 6));

        // 현재 유닛 정보
        _a(this.add.text(70, panelY + 8, `${cfg.icon} ${u.name}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffcc88', fontStyle: 'bold'
        }).setDepth(100));
        _a(this.add.text(70, panelY + 27, `HP ${u.hp}/${u.maxHp}  ATK ${u.atk}  사거리 ${u.range}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#aaaabb'
        }).setDepth(100));
        const isInShadow = !this._isInLight(u.x, u.y);
        const shadowText = isInShadow && u.shadowBuff > 1
            ? `🌑 어둠 보너스 ATK ×${u.shadowBuff}`
            : (isInShadow ? '🌑 어둠 (적이 못 봄)' : '☀ 빛 (적이 볼 수 있음)');
        _a(this.add.text(70, panelY + 45, shadowText, {
            fontSize: '10px', fontFamily: 'monospace',
            color: isInShadow && u.shadowBuff > 1 ? '#bb88ff' : (isInShadow ? '#aa88cc' : '#ffcc88')
        }).setDepth(100));
        _a(this.add.text(70, panelY + 63, `스킬 쿨다운: ${this.cooldowns[u.id].skill > 0 ? this.cooldowns[u.id].skill + 'R' : '준비됨'}`, {
            fontSize: '10px', fontFamily: 'monospace',
            color: this.cooldowns[u.id].skill > 0 ? '#888899' : '#88ffaa'
        }).setDepth(100));

        // 액션 버튼
        const buttons = [
            { label: '이동\n(인접)',        action: 'move',   color: 0x44aa66, hint: '인접 셀로 이동' },
            { label: '공격\n(사거리)',      action: 'attack', color: 0xcc4444, hint: '빛 안의 적 공격' },
            { label: '스킬\n' + this._getSkillName(u.classKey), action: 'skill', color: 0xbb44cc, hint: this._getSkillDesc(u.classKey), disabled: this.cooldowns[u.id].skill > 0 },
            { label: '횃불 패스\n(인접 아군)', action: 'torch',  color: 0xcc8844, hint: '인접 아군에게 횃불 넘김', disabled: !u.hasTorch || this.torchUsedThisRound[u.id] },
            { label: '패스\n(턴 종료)',     action: 'pass',   color: 0x555566, hint: '아무것도 안 함' }
        ];

        const btnW = 130, btnH = 60, startX = 380;
        buttons.forEach((b, i) => {
            const bx = startX + i * (btnW + 10);
            const by = panelY + 38;
            _a(UIButton.create(this, bx, by, btnW, btnH, b.label, {
                color: b.disabled ? 0x222233 : b.color,
                hoverColor: 0xffffff, textColor: '#ffffff', fontSize: 11,
                disabled: b.disabled, depth: 100,
                onClick: () => {
                    if (b.action === 'pass') {
                        this._executePass();
                    } else {
                        this.selectedAction = b.action;
                        this._drawGrid();
                    }
                }
            }));
            _a(this.add.text(bx, by + btnH / 2 + 12, b.hint, {
                fontSize: '8px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5).setDepth(100));
        });
    }

    _getSkillName(classKey) {
        const names = {
            warrior: '방패 일격', rogue: '암살', mage: '폭발(3×3)', archer: '관통',
            priest: '회복(아군)', alchemist: '폭탄(3×3)'
        };
        return names[classKey] || '스킬';
    }

    _getSkillDesc(classKey) {
        const descs = {
            warrior:   '인접 적 강타 +방어',
            rogue:     '인접 적 ×2.5 데미지',
            mage:      '3×3 광역 폭발',
            archer:    '직선 관통 사격',
            priest:    '인접 아군 50% 회복',
            alchemist: '지정 셀+8칸 폭탄'
        };
        return descs[classKey] || '';
    }

    // ==================== ACTIONS ====================

    _canAttackCell(u, x, y) {
        const dist = Math.max(Math.abs(u.x - x), Math.abs(u.y - y));
        if (dist === 0) return false;
        if (dist > u.range) return false;
        if (u.lineOnly) {
            // 직선만 (가로/세로/대각)
            if (u.x !== x && u.y !== y && Math.abs(u.x - x) !== Math.abs(u.y - y)) return false;
        }
        return true;
    }

    _canSkillCell(u, x, y) {
        if (u.classKey === 'warrior' || u.classKey === 'rogue') {
            return Math.max(Math.abs(u.x - x), Math.abs(u.y - y)) === 1;
        }
        if (u.classKey === 'mage' || u.classKey === 'alchemist') {
            return Math.max(Math.abs(u.x - x), Math.abs(u.y - y)) <= 3;
        }
        if (u.classKey === 'archer') {
            return this._canAttackCell(u, x, y);
        }
        if (u.classKey === 'priest') {
            // 아군 선택
            const cell = this.grid[y][x];
            return cell.hasUnit && cell.hasUnit.team === 'ally' && cell.hasUnit.alive
                && Math.max(Math.abs(u.x - x), Math.abs(u.y - y)) <= 2;
        }
        return false;
    }

    _executeMove(nx, ny) {
        const u = this._currentUnit();
        this.grid[u.y][u.x].hasUnit = null;
        u.x = nx; u.y = ny;
        this.grid[ny][nx].hasUnit = u;
        this._updateLightMap();
        this._showFloatText(this.GRID_X + nx * this.CELL_SIZE + 45, this.GRID_Y + ny * this.CELL_SIZE + 20, '이동', '#88ff88');
        this._endTurn();
    }

    _executeAttack(tx, ty) {
        const u = this._currentUnit();
        const target = this.grid[ty][tx].hasUnit;
        if (!target || target.team !== 'enemy' || !target.alive) return;

        let atkMult = 1.0;
        // 어둠 셀에서 공격 시 보너스
        if (!this._isInLight(u.x, u.y)) atkMult *= u.shadowBuff;

        this._applyDamage(u, target, atkMult);
        if (!target.alive) this.grid[target.y][target.x].hasUnit = null;
        this._endTurn();
    }

    _executeSkill(tx, ty) {
        const u = this._currentUnit();
        if (this.cooldowns[u.id].skill > 0) return;

        const cls = u.classKey;
        if (cls === 'warrior') {
            const t = this.grid[ty][tx].hasUnit;
            if (!t || t.team !== 'enemy') return;
            this._applyDamage(u, t, 1.5);
            this.statusEffects[u.id].push({ type: 'def_buff', duration: 2, value: 0.4 });
            this._showFloatText(this.GRID_X + u.x * this.CELL_SIZE + 45, this.GRID_Y + u.y * this.CELL_SIZE + 20, 'DEF↑', '#ffcc44');
            if (!t.alive) this.grid[t.y][t.x].hasUnit = null;
        } else if (cls === 'rogue') {
            const t = this.grid[ty][tx].hasUnit;
            if (!t || t.team !== 'enemy') return;
            this._applyDamage(u, t, 2.5);
            if (!t.alive) this.grid[t.y][t.x].hasUnit = null;
        } else if (cls === 'mage' || cls === 'alchemist') {
            // 3×3 AoE
            for (let dy = -1; dy <= 1; dy++) {
                for (let dx = -1; dx <= 1; dx++) {
                    const nx = tx + dx, ny = ty + dy;
                    if (nx < 0 || nx >= this.GRID_SIZE || ny < 0 || ny >= this.GRID_SIZE) continue;
                    const t = this.grid[ny][nx].hasUnit;
                    if (t && t.team === 'enemy' && t.alive) {
                        this._applyDamage(u, t, cls === 'mage' ? 1.4 : 1.2);
                        if (!t.alive) this.grid[t.y][t.x].hasUnit = null;
                    }
                }
            }
            this._showFloatText(this.GRID_X + tx * this.CELL_SIZE + 45, this.GRID_Y + ty * this.CELL_SIZE + 45, '💥', '#ff4444');
        } else if (cls === 'archer') {
            // 직선 관통
            const dx = Math.sign(tx - u.x);
            const dy = Math.sign(ty - u.y);
            let x = u.x + dx, y = u.y + dy;
            let hits = 0;
            while (x >= 0 && x < this.GRID_SIZE && y >= 0 && y < this.GRID_SIZE && hits < 4) {
                const t = this.grid[y][x].hasUnit;
                if (t && t.team === 'enemy' && t.alive) {
                    this._applyDamage(u, t, 1.5);
                    if (!t.alive) this.grid[t.y][t.x].hasUnit = null;
                    hits++;
                }
                x += dx; y += dy;
            }
        } else if (cls === 'priest') {
            const t = this.grid[ty][tx].hasUnit;
            if (!t || t.team !== 'ally') return;
            const heal = Math.floor(t.maxHp * 0.5);
            t.hp = Math.min(t.maxHp, t.hp + heal);
            this._showFloatText(this.GRID_X + tx * this.CELL_SIZE + 45, this.GRID_Y + ty * this.CELL_SIZE + 20, `+${heal}`, '#44ff44');
        }

        this.cooldowns[u.id].skill = 3;
        this._updateLightMap();
        this._endTurn();
    }

    _executePassTorch(target) {
        const u = this._currentUnit();
        u.hasTorch = false;
        target.hasTorch = true;
        this.torchUsedThisRound[u.id] = true;
        this._updateLightMap();
        this._showFloatText(this.GRID_X + target.x * this.CELL_SIZE + 45, this.GRID_Y + target.y * this.CELL_SIZE + 20, '🔦', '#ffcc44');
        // 패스만 했으니 턴 종료 X (다른 행동 가능)
        this.selectedAction = null;
        this._drawGrid();
        this._drawUnits();
        this._drawActionPanel();
    }

    _executePass() {
        this._endTurn();
    }

    // ==================== DAMAGE ====================

    _applyDamage(caster, target, atkMult) {
        const rawDmg = caster.atk * atkMult * (Math.random() * 0.2 + 0.9);
        let finalDmg = Math.floor(rawDmg * (1 - target.def / (target.def + 80)));
        // 방어 버프 (warrior 스킬)
        const defBuff = (this.statusEffects[target.id] || []).find(e => e.type === 'def_buff');
        if (defBuff) finalDmg = Math.floor(finalDmg * (1 - defBuff.value));
        finalDmg = Math.max(1, finalDmg);

        target.hp = Math.max(0, target.hp - finalDmg);
        const cx = this.GRID_X + target.x * this.CELL_SIZE + 45;
        const cy = this.GRID_Y + target.y * this.CELL_SIZE + 5;
        this._showFloatText(cx, cy, `-${finalDmg}`, target.team === 'ally' ? '#ff4444' : '#ffdd44');

        if (target.hp <= 0) {
            target.alive = false;
        }
    }

    // ==================== AI ====================

    _aiTurn(enemy) {
        // 가장 가까운 살아있는 아군 찾기
        const allies = this.units.filter(u => u.team === 'ally' && u.alive);
        if (allies.length === 0) return this._endTurn();

        allies.sort((a, b) => {
            const da = Math.max(Math.abs(a.x - enemy.x), Math.abs(a.y - enemy.y));
            const db = Math.max(Math.abs(b.x - enemy.x), Math.abs(b.y - enemy.y));
            return da - db;
        });
        const target = allies[0];

        // 공격 가능?
        const dist = Math.max(Math.abs(target.x - enemy.x), Math.abs(target.y - enemy.y));
        const canAttack = dist <= enemy.range;
        if (canAttack && (!enemy.lineOnly || enemy.x === target.x || enemy.y === target.y)) {
            this._applyDamage(enemy, target, 1.0);
            if (!target.alive) this.grid[target.y][target.x].hasUnit = null;
            this._drawUnits();
            this.time.delayedCall(500, () => this._endTurn());
            return;
        }

        // 이동: 타겟 방향으로 1칸
        const dx = Math.sign(target.x - enemy.x);
        const dy = Math.sign(target.y - enemy.y);
        const candidates = [
            { x: enemy.x + dx, y: enemy.y + dy },
            { x: enemy.x + dx, y: enemy.y },
            { x: enemy.x, y: enemy.y + dy }
        ];
        for (const c of candidates) {
            if (c.x < 0 || c.x >= this.GRID_SIZE || c.y < 0 || c.y >= this.GRID_SIZE) continue;
            if (this.grid[c.y][c.x].walkable && !this.grid[c.y][c.x].hasUnit) {
                this.grid[enemy.y][enemy.x].hasUnit = null;
                enemy.x = c.x; enemy.y = c.y;
                this.grid[c.y][c.x].hasUnit = enemy;
                this._showFloatText(this.GRID_X + c.x * this.CELL_SIZE + 45, this.GRID_Y + c.y * this.CELL_SIZE + 20, '이동', '#cc8888');
                this._updateLightMap();
                this._drawGrid();
                this._drawUnits();
                this.time.delayedCall(500, () => this._endTurn());
                return;
            }
        }

        // 못 움직임 → 패스
        this.time.delayedCall(400, () => this._endTurn());
    }

    // ==================== UTIL ====================

    _showAnnounce(text, color) {
        const t = this.add.text(640, 250, text, {
            fontSize: '24px', fontFamily: 'monospace',
            color: `#${color.toString(16).padStart(6, '0')}`, fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(300);
        this.tweens.add({
            targets: t, alpha: 0, y: 220, duration: 1500,
            onComplete: () => t.destroy()
        });
    }

    _showFloatText(x, y, text, color) {
        const t = this.add.text(x, y, text, {
            fontSize: '14px', fontFamily: 'monospace', color, fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(250);
        this.tweens.add({
            targets: t, alpha: 0, y: y - 30, duration: 900,
            onComplete: () => t.destroy()
        });
    }

    _clearGroup(arr) {
        for (const o of arr) {
            if (o && o.destroy) o.destroy();
        }
        arr.length = 0;
    }
}
