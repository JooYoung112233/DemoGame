/**
 * 후보 2: 빛/어둠 듀얼 트랙 전투 (Blackout Prototype).
 *
 * 핵심 메카닉:
 *  - 두 줄: 위 = 빛 트랙, 아래 = 어둠 트랙
 *  - 아군 4명 각 트랙에 분포 (시작 시 직접 배치)
 *  - 적은 정면 라인에 등장
 *  - 턴제 SPD
 *
 *  각 턴 옵션:
 *   - 트랙 전환 (빛 ↔ 어둠)
 *   - 공격 (빛 트랙만 가능, 어둠은 안 보임)
 *   - 스킬 (쿨다운)
 *   - 차징 (어둠 트랙 1턴 = 다음 공격 ×2)
 *   - 패스
 *
 *  적 AI:
 *   - 빛 트랙 아군 우선 공격
 *   - 어둠 트랙 아군은 공격 못 함 (50% 확률로 빛 트랙으로 끌어내려 시도)
 *
 *  클래스 적성:
 *   - warrior: 빛 트랙 DEF +30%
 *   - rogue: 어둠 차징 시 +200% (그림자 암살자)
 *   - mage: 트랙 상관없이 동일 (어둠에서도 공격 가능, 단 50%)
 *   - archer: 어둠 차징 시 일렬 관통
 *   - priest: 빛 트랙 힐 +50%
 *   - alchemist: 폭탄 = 양 트랙 동시 데미지
 */
class BlackoutLaneScene extends Phaser.Scene {
    constructor() { super('BlackoutLaneScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.party = data.party || [];
        this.zoneKey = data.zoneKey || 'blackout';
        this.zoneLevel = this.gameState.zoneLevel[this.zoneKey] || 1;
    }

    create() {
        this.LIGHT_Y = 240;
        this.SHADOW_Y = 410;
        this.ALLY_X_START = 200;
        this.ALLY_X_GAP = 90;
        this.ENEMY_X_START = 920;
        this.ENEMY_X_GAP = 90;

        this.round = 1;
        this.turnIndex = 0;
        this.gameOver = false;
        this.selectedAction = null;
        this.cooldowns = {};
        this.charging = {}; // unitId → true (다음 공격 ×2)
        this.buffs = {};    // unitId → [{ type, duration, value }]

        this._sceneObjects = [];
        this._unitObjects = [];
        this._actionPanelObjects = [];
        this._hudObjects = [];

        this._drawBackground();
        this._spawnUnits();
        this._calcTurnQueue();
        this._drawTracks();
        this._drawUnits();
        this._drawHUD();
        this._drawActionPanel();

        this._showAnnounce('🌗 듀얼 트랙 전투 — 빛은 표적, 어둠은 잠복', 0xbb88ff);
        this.time.delayedCall(800, () => this._beginTurn());
    }

    // ==================== INIT ====================

    _spawnUnits() {
        this.units = [];
        // 아군 — 처음에는 모두 빛 트랙 (좌측). 매 턴 전환 가능.
        this.party.forEach((merc, i) => {
            const stats = merc.getStats();
            const u = {
                id: 'ally_' + merc.id,
                mercRef: merc,
                name: merc.name,
                classKey: merc.classKey,
                team: 'ally',
                track: i % 2 === 0 ? 'light' : 'shadow', // 분산
                slot: i,
                hp: stats.hp, maxHp: stats.hp,
                atk: stats.atk, def: stats.def,
                spd: stats.moveSpeed || 100,
                alive: true,
                ...this._getClassConfig(merc.classKey)
            };
            this.units.push(u);
            this.cooldowns[u.id] = { skill: 0 };
            this.buffs[u.id] = [];
        });

        // 적 — 빛/어둠 분포 (랜덤)
        const enemyCount = Math.min(4, 2 + Math.floor(this.zoneLevel / 3));
        const enemyTypes = ['shadow_hunter', 'wraith', 'curse_caster', 'shade_brute'];
        for (let i = 0; i < enemyCount; i++) {
            const type = enemyTypes[i % enemyTypes.length];
            const cfg = this._getEnemyConfig(type);
            const u = {
                id: 'enemy_' + i,
                name: cfg.name,
                classKey: type,
                team: 'enemy',
                track: i % 2 === 0 ? 'light' : 'shadow',
                slot: i,
                hp: cfg.hp, maxHp: cfg.hp,
                atk: cfg.atk, def: cfg.def,
                spd: cfg.spd,
                alive: true,
                ...cfg
            };
            this.units.push(u);
            this.cooldowns[u.id] = { skill: 0 };
            this.buffs[u.id] = [];
        }
    }

    _getClassConfig(classKey) {
        const cfg = {
            warrior:   { icon: '🛡', color: 0xcc8844, lightBonus: { def: 0.3 }, shadowBonus: {} },
            rogue:     { icon: '🗡', color: 0xaa44cc, lightBonus: {}, shadowBonus: { chargeMult: 3.0 } },
            mage:      { icon: '🔮', color: 0x4488ff, lightBonus: {}, shadowBonus: { canAttackPartial: true } },
            archer:    { icon: '🏹', color: 0x88cc44, lightBonus: {}, shadowBonus: { chargePierce: true } },
            priest:    { icon: '✝', color: 0xffcc88, lightBonus: { healMult: 1.5 }, shadowBonus: {} },
            alchemist: { icon: '⚗', color: 0x44cc88, lightBonus: {}, shadowBonus: { bothLanes: true } }
        };
        return cfg[classKey] || cfg.warrior;
    }

    _getEnemyConfig(type) {
        const base = 35 + this.zoneLevel * 8;
        const cfg = {
            shadow_hunter:  { name: '그림자 사냥꾼', hp: base, atk: 14 + this.zoneLevel * 2, def: 2, spd: 120, icon: '👤', color: 0x664488 },
            wraith:         { name: '망령',         hp: base - 10, atk: 16 + this.zoneLevel * 2, def: 0, spd: 130, icon: '👻', color: 0x4488aa },
            curse_caster:   { name: '저주술사',     hp: base - 5,  atk: 18 + this.zoneLevel * 2, def: 1, spd: 100, icon: '🧙', color: 0xaa44aa },
            shade_brute:    { name: '그림자 폭군', hp: base + 15, atk: 12 + this.zoneLevel * 2, def: 4, spd: 90,  icon: '👹', color: 0x884422 }
        };
        return cfg[type] || cfg.shadow_hunter;
    }

    // ==================== TURN ====================

    _calcTurnQueue() {
        const alive = this.units.filter(u => u.alive);
        alive.sort((a, b) => (b.spd - a.spd) + (Math.random() - 0.5));
        this.turnQueue = alive;
        this.turnIndex = 0;
        alive.forEach(u => {
            if (this.cooldowns[u.id].skill > 0) this.cooldowns[u.id].skill--;
            // 버프 만료
            this.buffs[u.id] = (this.buffs[u.id] || []).map(b => ({ ...b, duration: b.duration - 1 })).filter(b => b.duration > 0);
        });
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
        if (!u.alive) { this.turnIndex++; this._beginTurn(); return; }

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
        return this.turnIndex < this.turnQueue.length ? this.turnQueue[this.turnIndex] : null;
    }

    _endTurn() {
        this.selectedAction = null;
        if (this._checkBattleEnd()) return;
        this.turnIndex++;
        this._drawUnits();
        this._drawTracks();
        this.time.delayedCall(300, () => this._beginTurn());
    }

    _checkBattleEnd() {
        const allies = this.units.filter(u => u.team === 'ally' && u.alive).length;
        const enemies = this.units.filter(u => u.team === 'enemy' && u.alive).length;
        if (enemies === 0) return this._endBattle(true);
        if (allies === 0) return this._endBattle(false);
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
        this.add.text(640, 330, victory ? '듀얼 트랙 정복' : '어둠에 잠겼다', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ccccdd'
        }).setOrigin(0.5).setDepth(202);
        this.add.text(640, 370, `라운드: ${this.round}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5).setDepth(202);

        UIButton.create(this, 640, 430, 240, 38, '프로토타입 메뉴로', {
            color: 0xaa44cc, hoverColor: 0xcc55dd, textColor: '#ffffff', fontSize: 14, depth: 202,
            onClick: () => this.scene.start('BlackoutProtoSelectScene', {
                gameState: this.gameState, party: this.party, zoneKey: this.zoneKey
            })
        });
        UIButton.create(this, 640, 475, 240, 30, '마을로 돌아가기', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaccee', fontSize: 12, depth: 202,
            onClick: () => this.scene.start('TownScene', { gameState: this.gameState })
        });
        return true;
    }

    // ==================== DRAW ====================

    _drawBackground() {
        const gfx = this.add.graphics();
        // 위쪽 = 빛 (밝음), 아래쪽 = 어둠
        gfx.fillGradientStyle(0x1a1830, 0x1a1830, 0x05050f, 0x05050f);
        gfx.fillRect(0, 0, 1280, 720);
        // 빛 트랙 영역
        gfx.fillStyle(0xffcc88, 0.04);
        gfx.fillRect(0, 165, 1280, 145);
        // 어둠 트랙 영역
        gfx.fillStyle(0x440066, 0.08);
        gfx.fillRect(0, 335, 1280, 145);
        // 안개
        for (let i = 0; i < 20; i++) {
            gfx.fillStyle(0x6644aa, 0.04);
            gfx.fillCircle(Phaser.Math.Between(0, 1280), Phaser.Math.Between(0, 720), Phaser.Math.Between(30, 90));
        }
    }

    _drawTracks() {
        this._clearGroup(this._sceneObjects);
        const _s = (o) => { this._sceneObjects.push(o); return o; };

        // 빛 트랙 라벨
        _s(this.add.rectangle(640, this.LIGHT_Y - 70, 1180, 30, 0xffcc88, 0.1).setDepth(5));
        _s(this.add.text(70, this.LIGHT_Y - 80, '☀ 빛 트랙', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffcc88', fontStyle: 'bold'
        }).setDepth(6));
        _s(this.add.text(70, this.LIGHT_Y - 62, '공격 가능 / 적의 표적', {
            fontSize: '10px', fontFamily: 'monospace', color: '#cc9966'
        }).setDepth(6));

        // 트랙 구분선
        _s(this.add.line(0, 0, 0, 335, 1280, 335, 0x6644aa, 0.3).setDepth(4).setOrigin(0));

        // 어둠 트랙 라벨
        _s(this.add.rectangle(640, this.SHADOW_Y + 70, 1180, 30, 0x6644aa, 0.15).setDepth(5));
        _s(this.add.text(70, this.SHADOW_Y + 60, '🌑 어둠 트랙', {
            fontSize: '14px', fontFamily: 'monospace', color: '#bb88ff', fontStyle: 'bold'
        }).setDepth(6));
        _s(this.add.text(70, this.SHADOW_Y + 78, '안 보임 / 차징 ×2 공격', {
            fontSize: '10px', fontFamily: 'monospace', color: '#9988cc'
        }).setDepth(6));

        // 중앙 vs 분리선
        _s(this.add.text(640, 325, 'VS', {
            fontSize: '20px', fontFamily: 'monospace', color: '#332244', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(5));
    }

    _drawUnits() {
        this._clearGroup(this._unitObjects);
        const _u = (o) => { this._unitObjects.push(o); return o; };

        const current = this._currentUnit();
        for (const u of this.units) {
            if (!u.alive) continue;
            const x = u.team === 'ally'
                ? this.ALLY_X_START + u.slot * this.ALLY_X_GAP
                : this.ENEMY_X_START + u.slot * this.ENEMY_X_GAP;
            const y = u.track === 'light' ? this.LIGHT_Y : this.SHADOW_Y;
            const cfg = u.team === 'ally' ? this._getClassConfig(u.classKey) : this._getEnemyConfig(u.classKey);
            const isHiddenEnemy = (u.team === 'enemy' && u.track === 'shadow');

            // 현재 턴 강조
            if (current && current.id === u.id) {
                const ring = _u(this.add.circle(x, y, 38, 0, 0).setDepth(19));
                ring.setStrokeStyle(3, u.team === 'ally' ? 0x44ff88 : 0xff4444, 0.9);
                this.tweens.add({ targets: ring, scale: 1.15, duration: 600, yoyo: true, repeat: -1 });
            }

            if (isHiddenEnemy) {
                _u(this.add.circle(x, y, 26, 0x222233, 0.6).setDepth(20));
                _u(this.add.text(x, y, '?', {
                    fontSize: '24px', fontFamily: 'monospace', color: '#552266', fontStyle: 'bold'
                }).setOrigin(0.5).setDepth(21));
                _u(this.add.text(x, y + 32, '???', {
                    fontSize: '9px', fontFamily: 'monospace', color: '#553366'
                }).setOrigin(0.5).setDepth(21));
                continue;
            }

            // 본체
            _u(this.add.circle(x, y, 26, cfg.color, 0.85).setDepth(20));
            _u(this.add.text(x, y, cfg.icon || '?', { fontSize: '22px' }).setOrigin(0.5).setDepth(21));

            // 차징 표시
            if (this.charging[u.id]) {
                const charge = _u(this.add.text(x + 22, y - 22, '⚡', { fontSize: '18px' }).setDepth(22));
                this.tweens.add({ targets: charge, scale: 1.3, duration: 500, yoyo: true, repeat: -1 });
            }

            // 이름 + 트랙 표시
            _u(this.add.text(x, y - 36, u.name, {
                fontSize: '10px', fontFamily: 'monospace',
                color: u.team === 'ally' ? '#aaccee' : '#cc8888'
            }).setOrigin(0.5).setDepth(22));

            // HP바
            const hpRatio = u.hp / u.maxHp;
            const barW = 60, barH = 5;
            _u(this.add.rectangle(x, y + 30, barW, barH, 0x220011).setDepth(22));
            const hpColor = hpRatio > 0.6 ? 0x44ff44 : hpRatio > 0.3 ? 0xffaa44 : 0xff4444;
            const fillW = Math.max(0, barW * hpRatio);
            if (fillW > 0) {
                _u(this.add.rectangle(x - barW / 2 + fillW / 2, y + 30, fillW, barH, hpColor).setDepth(23));
            }
            _u(this.add.text(x, y + 42, `${u.hp}/${u.maxHp}`, {
                fontSize: '9px', fontFamily: 'monospace', color: '#ccccdd'
            }).setOrigin(0.5).setDepth(23));

            // 클릭 가능 (공격 대상)
            if (this.selectedAction === 'attack' && u.team === 'enemy' && u.alive && u.track === 'light') {
                const z = _u(this.add.zone(x, y, 60, 60).setInteractive({ useHandCursor: true }).setDepth(50));
                z.on('pointerdown', () => this._executeAttack(u));
                const hi = _u(this.add.circle(x, y, 34, 0xff4444, 0).setStrokeStyle(3, 0xff4444, 0.9).setDepth(19));
                this.tweens.add({ targets: hi, scale: 1.1, duration: 400, yoyo: true, repeat: -1 });
            }
            // 스킬 대상 (사제 = 아군 / 알켐 = 적 어디든 / 마지/궁수 = 적)
            if (this.selectedAction === 'skill' && this._canSkillTarget(this._currentUnit(), u)) {
                const z = _u(this.add.zone(x, y, 60, 60).setInteractive({ useHandCursor: true }).setDepth(50));
                z.on('pointerdown', () => this._executeSkill(u));
                const hi = _u(this.add.circle(x, y, 34, 0xbb88ff, 0).setStrokeStyle(3, 0xbb88ff, 0.9).setDepth(19));
                this.tweens.add({ targets: hi, scale: 1.1, duration: 400, yoyo: true, repeat: -1 });
            }
        }
    }

    _drawHUD() {
        this._clearGroup(this._hudObjects);
        const _h = (o) => { this._hudObjects.push(o); return o; };

        _h(this.add.rectangle(640, 35, 1280, 70, 0x0a0a14, 0.9).setDepth(100));
        _h(this.add.text(20, 18, `🌗 듀얼 트랙 전투 — 라운드 ${this.round}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#bb88ff', fontStyle: 'bold'
        }).setDepth(101));
        _h(this.add.text(20, 38, `구역 Lv.${this.zoneLevel} | 트랙 전환과 차징의 게임`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#776688'
        }).setDepth(101));

        const u = this._currentUnit();
        if (u) {
            const turnLabel = u.team === 'ally' ? `🟢 ${u.name} 차례` : `🔴 ${u.name} 차례`;
            _h(this.add.text(640, 18, turnLabel, {
                fontSize: '14px', fontFamily: 'monospace',
                color: u.team === 'ally' ? '#88ff88' : '#ff8888', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(101));
            // 다음 턴
            const next = [];
            for (let i = 1; i < 5 && (this.turnIndex + i) < this.turnQueue.length; i++) {
                const n = this.turnQueue[this.turnIndex + i];
                if (n.alive) next.push(n);
            }
            _h(this.add.text(560, 42, '다음:', {
                fontSize: '9px', fontFamily: 'monospace', color: '#666677'
            }).setDepth(101));
            next.slice(0, 4).forEach((n, i) => {
                const cfg = n.team === 'ally' ? this._getClassConfig(n.classKey) : this._getEnemyConfig(n.classKey);
                _h(this.add.text(600 + i * 28, 42, cfg.icon || '?', {
                    fontSize: '14px', color: n.team === 'ally' ? '#88aaff' : '#cc6666'
                }).setOrigin(0.5).setDepth(101));
            });
        }

        _h(UIButton.create(this, 70, 700, 110, 25, '← 프로토 메뉴', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaccee', fontSize: 10, depth: 110,
            onClick: () => this.scene.start('BlackoutProtoSelectScene', {
                gameState: this.gameState, party: this.party, zoneKey: this.zoneKey
            })
        }));
    }

    _drawActionPanel() {
        this._clearGroup(this._actionPanelObjects);
        const _a = (o) => { this._actionPanelObjects.push(o); return o; };

        const u = this._currentUnit();
        if (!u || u.team !== 'ally') return;
        const cfg = this._getClassConfig(u.classKey);

        const panelY = 555;
        _a(this.add.rectangle(640, panelY + 55, 1180, 130, 0x0a0a14, 0.95).setDepth(99));
        _a(this.add.graphics().setDepth(99)
            .lineStyle(1, 0x332244, 0.7)
            .strokeRoundedRect(50, panelY, 1180, 130, 6));

        // 유닛 정보 (좌)
        _a(this.add.text(70, panelY + 12, `${cfg.icon} ${u.name}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffcc88', fontStyle: 'bold'
        }).setDepth(100));
        _a(this.add.text(70, panelY + 32, `HP ${u.hp}/${u.maxHp}  ATK ${u.atk}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#aaaabb'
        }).setDepth(100));

        const trackText = u.track === 'light' ? '☀ 빛 트랙' : '🌑 어둠 트랙';
        const trackColor = u.track === 'light' ? '#ffcc88' : '#bb88ff';
        _a(this.add.text(70, panelY + 50, `현재: ${trackText}`, {
            fontSize: '11px', fontFamily: 'monospace', color: trackColor, fontStyle: 'bold'
        }).setDepth(100));

        let bonusText = '';
        if (u.track === 'light' && cfg.lightBonus && cfg.lightBonus.def) bonusText = `+빛 DEF ×${1 + cfg.lightBonus.def}`;
        if (u.track === 'light' && cfg.lightBonus && cfg.lightBonus.healMult) bonusText = `+빛 회복 ×${cfg.lightBonus.healMult}`;
        if (u.track === 'shadow' && cfg.shadowBonus.chargeMult) bonusText = `+어둠 차징 시 ×${cfg.shadowBonus.chargeMult}`;
        if (u.track === 'shadow' && cfg.shadowBonus.canAttackPartial) bonusText = `+어둠에서 공격 50% 가능`;
        if (u.track === 'shadow' && cfg.shadowBonus.chargePierce) bonusText = `+어둠 차징 = 일렬 관통`;
        if (u.track === 'shadow' && cfg.shadowBonus.bothLanes) bonusText = `+폭탄 = 양 트랙 동시`;
        _a(this.add.text(70, panelY + 68, bonusText || ' ', {
            fontSize: '10px', fontFamily: 'monospace', color: '#88ccff'
        }).setDepth(100));

        if (this.charging[u.id]) {
            _a(this.add.text(70, panelY + 86, '⚡ 차징됨 — 다음 공격 강화', {
                fontSize: '10px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
            }).setDepth(100));
        }
        _a(this.add.text(70, panelY + 104, `스킬 쿨다운: ${this.cooldowns[u.id].skill > 0 ? this.cooldowns[u.id].skill + 'R' : '준비됨'}`, {
            fontSize: '10px', fontFamily: 'monospace',
            color: this.cooldowns[u.id].skill > 0 ? '#888899' : '#88ffaa'
        }).setDepth(100));

        // 액션 버튼
        const canAttack = (u.track === 'light') || (u.track === 'shadow' && cfg.shadowBonus.canAttackPartial);
        const buttons = [
            { label: u.track === 'light' ? '⬇ 어둠으로\n전환' : '⬆ 빛으로\n전환', action: 'switch', color: 0x6644aa, hint: '트랙 변경 (행동 종료)' },
            { label: '공격\n' + (canAttack ? '(빛 적)' : '(불가)'), action: 'attack', color: 0xcc4444, hint: canAttack ? '빛 트랙 적 선택' : '어둠에서 공격 불가', disabled: !canAttack || this._enemyInLightCount() === 0 },
            { label: '스킬\n' + this._getSkillName(u.classKey), action: 'skill', color: 0xbb44cc, hint: this._getSkillDesc(u.classKey), disabled: this.cooldowns[u.id].skill > 0 },
            { label: '⚡ 차징\n(다음 ×2)', action: 'charge', color: 0xcc8844, hint: '어둠 권장 (보너스 트리거)', disabled: this.charging[u.id] },
            { label: '패스\n(턴 종료)', action: 'pass', color: 0x555566, hint: '아무것도 안 함' }
        ];

        const btnW = 130, btnH = 80, startX = 360;
        buttons.forEach((b, i) => {
            const bx = startX + i * (btnW + 10);
            const by = panelY + 60;
            _a(UIButton.create(this, bx, by, btnW, btnH, b.label, {
                color: b.disabled ? 0x222233 : b.color,
                hoverColor: 0xffffff, textColor: '#ffffff', fontSize: 11,
                disabled: b.disabled, depth: 100,
                onClick: () => this._handleAction(b.action)
            }));
            _a(this.add.text(bx, by + btnH / 2 + 12, b.hint, {
                fontSize: '8px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5).setDepth(100));
        });
    }

    _enemyInLightCount() {
        return this.units.filter(u => u.team === 'enemy' && u.alive && u.track === 'light').length;
    }

    _getSkillName(classKey) {
        const names = {
            warrior: '방패 일격', rogue: '연속 베기', mage: '폭발',
            archer: '관통 사격', priest: '회복', alchemist: '폭탄'
        };
        return names[classKey] || '스킬';
    }
    _getSkillDesc(classKey) {
        const descs = {
            warrior:   '빛 적 ×2 + 도발',
            rogue:     '빛/어둠 적 1명 ×3',
            mage:      '빛 적 전체 1.4',
            archer:    '한 트랙 관통',
            priest:    '아군 1명 풀회복',
            alchemist: '양 트랙 동시 폭발'
        };
        return descs[classKey] || '';
    }

    _canSkillTarget(caster, target) {
        if (!caster || caster.team !== 'ally') return false;
        const cls = caster.classKey;
        if (cls === 'priest') return target.team === 'ally' && target.alive;
        // 다른 스킬은 적 대상
        if (target.team !== 'enemy' || !target.alive) return false;
        if (cls === 'warrior') return target.track === 'light';
        if (cls === 'rogue') return true;
        if (cls === 'mage') return target.track === 'light';
        if (cls === 'archer') return true;
        if (cls === 'alchemist') return true;
        return false;
    }

    // ==================== ACTIONS ====================

    _handleAction(action) {
        const u = this._currentUnit();
        if (!u) return;
        if (action === 'switch') {
            u.track = u.track === 'light' ? 'shadow' : 'light';
            this._showFloatText(u.team === 'ally'
                ? this.ALLY_X_START + u.slot * this.ALLY_X_GAP
                : this.ENEMY_X_START + u.slot * this.ENEMY_X_GAP,
                u.track === 'light' ? this.LIGHT_Y : this.SHADOW_Y,
                u.track === 'light' ? '☀ 빛으로' : '🌑 어둠으로',
                u.track === 'light' ? '#ffcc88' : '#bb88ff');
            this._endTurn();
        } else if (action === 'attack') {
            this.selectedAction = 'attack';
            this._drawUnits();
        } else if (action === 'skill') {
            this.selectedAction = 'skill';
            this._drawUnits();
        } else if (action === 'charge') {
            this.charging[u.id] = true;
            this._showFloatText(this.ALLY_X_START + u.slot * this.ALLY_X_GAP, u.track === 'light' ? this.LIGHT_Y : this.SHADOW_Y, '⚡ 차징', '#ffcc44');
            this._endTurn();
        } else if (action === 'pass') {
            this._endTurn();
        }
    }

    _executeAttack(target) {
        const u = this._currentUnit();
        const cfg = this._getClassConfig(u.classKey);
        let mult = 1.0;

        // 어둠 트랙에서 공격 (mage만 가능, 50% 데미지)
        if (u.track === 'shadow') {
            if (cfg.shadowBonus.canAttackPartial) mult *= 0.5;
            else return;
        }

        // 차징 사용
        if (this.charging[u.id]) {
            if (u.track === 'shadow' && cfg.shadowBonus.chargeMult) {
                mult *= cfg.shadowBonus.chargeMult;
            } else {
                mult *= 2.0;
            }
            this.charging[u.id] = false;

            // 궁수 어둠 차징 = 관통
            if (u.classKey === 'archer' && cfg.shadowBonus.chargePierce && u.track === 'shadow') {
                // 같은 트랙 모든 적
                const targets = this.units.filter(e => e.team === 'enemy' && e.alive && e.track === target.track);
                targets.forEach(t => this._applyDamage(u, t, mult));
                this._endTurn();
                return;
            }
        }

        this._applyDamage(u, target, mult);
        this._endTurn();
    }

    _executeSkill(target) {
        const u = this._currentUnit();
        const cfg = this._getClassConfig(u.classKey);
        const cls = u.classKey;

        if (cls === 'warrior') {
            this._applyDamage(u, target, 2.0);
            this.buffs[u.id].push({ type: 'taunt', duration: 2 });
        } else if (cls === 'rogue') {
            this._applyDamage(u, target, 3.0);
        } else if (cls === 'mage') {
            // 빛 트랙 모든 적
            const targets = this.units.filter(e => e.team === 'enemy' && e.alive && e.track === 'light');
            targets.forEach(t => this._applyDamage(u, t, 1.4));
        } else if (cls === 'archer') {
            // 선택한 적의 트랙 관통
            const targets = this.units.filter(e => e.team === 'enemy' && e.alive && e.track === target.track);
            targets.forEach(t => this._applyDamage(u, t, 1.6));
        } else if (cls === 'priest') {
            const healMult = (u.track === 'light' && cfg.lightBonus.healMult) ? cfg.lightBonus.healMult : 1.0;
            const heal = Math.floor(target.maxHp * 0.6 * healMult);
            target.hp = Math.min(target.maxHp, target.hp + heal);
            this._showFloatText(this.ALLY_X_START + target.slot * this.ALLY_X_GAP,
                target.track === 'light' ? this.LIGHT_Y : this.SHADOW_Y, `+${heal}`, '#44ff44');
        } else if (cls === 'alchemist') {
            // 양 트랙 동시
            const targets = this.units.filter(e => e.team === 'enemy' && e.alive);
            targets.forEach(t => this._applyDamage(u, t, 1.2));
        }

        this.cooldowns[u.id].skill = 3;
        this._endTurn();
    }

    // ==================== AI ====================

    _aiTurn(enemy) {
        // 빛 트랙 아군이 있으면 우선 공격
        const lightAllies = this.units.filter(u => u.team === 'ally' && u.alive && u.track === 'light');
        const shadowAllies = this.units.filter(u => u.team === 'ally' && u.alive && u.track === 'shadow');

        // 도발 적용
        const tauntAlly = this.units.find(u => u.team === 'ally' && u.alive && (this.buffs[u.id] || []).some(b => b.type === 'taunt'));

        let target = null;
        if (tauntAlly && (tauntAlly.track === 'light' || enemy.track === 'shadow')) target = tauntAlly;
        else if (lightAllies.length > 0) {
            // HP 낮은 빛 적 먼저
            lightAllies.sort((a, b) => (a.hp / a.maxHp) - (b.hp / b.maxHp));
            target = lightAllies[0];
        } else if (shadowAllies.length > 0 && enemy.track === 'shadow') {
            // 적이 어둠에 있으면 어둠 아군 공격 가능
            shadowAllies.sort((a, b) => (a.hp / a.maxHp) - (b.hp / b.maxHp));
            target = shadowAllies[0];
        } else {
            // 빛 아군 없음 → 적이 어둠 트랙으로 전환해서 따라간다 (30% 확률)
            if (Math.random() < 0.5 && enemy.track === 'light' && shadowAllies.length > 0) {
                enemy.track = 'shadow';
                this._showFloatText(this.ENEMY_X_START + enemy.slot * this.ENEMY_X_GAP, this.SHADOW_Y, '🌑 추격', '#bb88ff');
                this._drawUnits();
                this.time.delayedCall(400, () => this._endTurn());
                return;
            }
            // 또는 패스
            this._showFloatText(this.ENEMY_X_START + enemy.slot * this.ENEMY_X_GAP, enemy.track === 'light' ? this.LIGHT_Y : this.SHADOW_Y, '...', '#776688');
            this.time.delayedCall(400, () => this._endTurn());
            return;
        }

        if (!target) {
            this.time.delayedCall(400, () => this._endTurn());
            return;
        }

        this._applyDamage(enemy, target, 1.0);
        this._drawUnits();
        this.time.delayedCall(500, () => this._endTurn());
    }

    // ==================== DAMAGE ====================

    _applyDamage(caster, target, atkMult) {
        const cfg = this._getClassConfig(target.classKey);
        let defMult = 1.0;
        // warrior 빛 트랙 DEF 보너스
        if (target.team === 'ally' && target.track === 'light' && cfg.lightBonus && cfg.lightBonus.def) {
            defMult = 1 - cfg.lightBonus.def;
        }
        const rawDmg = caster.atk * atkMult * (Math.random() * 0.2 + 0.9);
        let finalDmg = Math.floor(rawDmg * (1 - target.def / (target.def + 80)) * defMult);
        finalDmg = Math.max(1, finalDmg);

        target.hp = Math.max(0, target.hp - finalDmg);
        const x = target.team === 'ally'
            ? this.ALLY_X_START + target.slot * this.ALLY_X_GAP
            : this.ENEMY_X_START + target.slot * this.ENEMY_X_GAP;
        const y = (target.track === 'light' ? this.LIGHT_Y : this.SHADOW_Y) - 20;
        this._showFloatText(x, y, `-${finalDmg}`, target.team === 'ally' ? '#ff4444' : '#ffdd44');

        if (target.hp <= 0) target.alive = false;
    }

    // ==================== UTIL ====================

    _showAnnounce(text, color) {
        const t = this.add.text(640, 130, text, {
            fontSize: '24px', fontFamily: 'monospace',
            color: `#${color.toString(16).padStart(6, '0')}`, fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(300);
        this.tweens.add({ targets: t, alpha: 0, y: 100, duration: 1500, onComplete: () => t.destroy() });
    }

    _showFloatText(x, y, text, color) {
        const t = this.add.text(x, y, text, {
            fontSize: '14px', fontFamily: 'monospace', color, fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(250);
        this.tweens.add({ targets: t, alpha: 0, y: y - 30, duration: 900, onComplete: () => t.destroy() });
    }

    _clearGroup(arr) {
        for (const o of arr) { if (o && o.destroy) o.destroy(); }
        arr.length = 0;
    }
}
