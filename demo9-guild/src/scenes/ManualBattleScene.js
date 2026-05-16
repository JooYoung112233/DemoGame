/**
 * ManualBattleScene — 다키스트 스타일 수동 전투.
 * 4명 일렬 × 적 4마리 일렬 + SPD 턴제 + 위치 기반 액션.
 *
 * data: { gameState, zoneKey, party }
 */
class ManualBattleScene extends Phaser.Scene {
    constructor() { super('ManualBattleScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.zoneKey = data.zoneKey || 'bloodpit';
        this.party = data.party || [];

        // 적 생성
        const enemies = this._spawnEnemies();
        this.combat = DarkestCombat.createCombat(this.party, enemies);

        this.currentRound = 1;
        this.maxRounds = 3;
        this.selectedAction = null;
        this.battleEnded = false;
        this.totalGold = 0;
        this.totalXp = 0;
        this.loot = [];
    }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a0e);

        // 배경 그라데이션
        const bgGfx = this.add.graphics();
        bgGfx.fillGradientStyle(0x150810, 0x150810, 0x1a0a18, 0x1a0a18, 1);
        bgGfx.fillRect(0, 0, 1280, 720);

        this._drawHeader();
        this._drawBattlefield();
        this._drawActionPanel();
        this._drawTurnQueue();

        DarkestCombat.startRound(this.combat);
        this._processNextTurn();
    }

    _spawnEnemies() {
        // BP 적 4마리 — types 순서대로 position 1,2,3,4 (전열→후열)
        // melee를 앞(전열), ranged를 뒤(후열)로 배치
        const zoneLevel = this.gameState.zoneLevel[this.zoneKey] || 1;
        let types;
        // [전열 melee, 전열 melee, 후열 ranged, 후열 ranged] 순서
        if (zoneLevel <= 1)      types = ['runner', 'bruiser', 'spitter', 'runner'];
        else if (zoneLevel <= 3) types = ['runner', 'bruiser', 'spitter', 'summoner'];
        else if (zoneLevel <= 6) types = ['bruiser', 'elite_runner', 'spitter', 'summoner'];
        else if (zoneLevel < 10) types = ['elite_runner', 'elite_bruiser', 'spitter', 'summoner'];
        else                     types = ['pitlord', 'elite_bruiser', 'summoner', 'summoner'];

        return types.map((type, i) => {
            const data = ENEMY_DATA[type];
            const scaleMult = zoneLevel === 1 ? 0.75 : 1.0 + (zoneLevel - 2) * 0.08;
            const enemyActions = (typeof getEnemyActions === 'function') ? getEnemyActions(type) : [];
            return {
                id: 'enemy_' + i,
                name: data.name,
                classKey: type,
                actions: enemyActions,    // 적도 액션 풀 보유
                getStats: () => ({
                    hp: Math.floor(data.hp * scaleMult),
                    atk: Math.floor(data.atk * scaleMult),
                    def: data.def,
                    moveSpeed: data.moveSpeed,
                    critRate: data.critRate,
                    critDmg: data.critDmg
                }),
                currentHp: Math.floor(data.hp * scaleMult)
            };
        });
    }

    _drawHeader() {
        const gs = this.gameState;
        this.headerText = this.add.text(640, 20, `Round ${this.currentRound}/${this.maxRounds}  |  Blood Pit Lv.${gs.zoneLevel[this.zoneKey]}`, {
            fontSize: '15px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);
        UIButton.create(this, 80, 20, 100, 28, '← 후퇴', {
            color: 0x554444, hoverColor: 0x665555, textColor: '#ffaaaa', fontSize: 11,
            onClick: () => this._endBattle(false)
        });
    }

    _drawBattlefield() {
        // 아군 위치 (왼쪽, 1번이 가장 우측 = 전열)
        // 좌→우: 4 3 2 1 |  1 2 3 4 (적)
        // 그러나 직관적으로: 아군 4321 가 왼쪽, 적 1234 가 오른쪽
        // 즉 포지션 1이 가운데 가까이

        const allyXBase = 200, enemyXBase = 720;
        const slotW = 110;

        this.unitGfx = {};

        // 아군 (포지션 1이 가장 오른쪽, 적과 가까움)
        this.combat.allies.forEach(u => {
            const x = allyXBase + (4 - u.position) * slotW;  // pos 1 = x=530, pos 4 = x=200
            const y = 340;
            this._drawUnit(u, x, y, 'ally');
        });

        // 적 (포지션 1이 가장 왼쪽)
        this.combat.enemies.forEach(u => {
            const x = enemyXBase + (u.position - 1) * slotW;  // pos 1 = x=720
            const y = 340;
            this._drawUnit(u, x, y, 'enemy');
        });

        // 중앙선
        const centerLine = this.add.graphics();
        centerLine.lineStyle(1, 0x441111, 0.5);
        centerLine.lineBetween(640, 220, 640, 460);
    }

    _drawUnit(unit, x, y, team) {
        const isAlly = team === 'ally';
        const gfx = {};

        // 캐릭터 원형 (간단한 표현)
        gfx.body = this.add.graphics();
        const bodyColor = isAlly ? this._getClassColor(unit.classKey) : (ENEMY_DATA[unit.classKey] ? ENEMY_DATA[unit.classKey].color : 0xcc4444);
        gfx.body.fillStyle(bodyColor, 1);
        gfx.body.fillCircle(x, y, 38);
        gfx.body.lineStyle(2, 0xffffff, 0.7);
        gfx.body.strokeCircle(x, y, 38);

        // 클래스/적 아이콘
        const icon = isAlly ? (CLASS_DATA[unit.classKey]?.icon || '?') : '👹';
        gfx.icon = this.add.text(x, y - 5, icon, { fontSize: '28px' }).setOrigin(0.5);

        // 포지션 번호
        gfx.posNum = this.add.text(x, y - 60, `[${unit.position}]`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#88aaff', fontStyle: 'bold'
        }).setOrigin(0.5);

        // 이름
        gfx.name = this.add.text(x, y + 50, unit.name, {
            fontSize: '11px', fontFamily: 'monospace', color: '#ccccdd'
        }).setOrigin(0.5);

        // HP바
        gfx.hpBg = this.add.rectangle(x, y + 70, 90, 8, 0x331111);
        gfx.hpFill = this.add.rectangle(x - 45, y + 70, 90 * (unit.hp / unit.maxHp), 8, 0x44ff44).setOrigin(0, 0.5);
        gfx.hpText = this.add.text(x, y + 70, `${unit.hp}/${unit.maxHp}`, {
            fontSize: '9px', fontFamily: 'monospace', color: '#ffffff', stroke: '#000', strokeThickness: 1
        }).setOrigin(0.5);

        // 상태 효과 아이콘
        gfx.statusText = this.add.text(x, y + 88, '', {
            fontSize: '10px', fontFamily: 'monospace', color: '#ffaa88'
        }).setOrigin(0.5);

        // 인터랙티브 (타겟 선택용)
        gfx.hitZone = this.add.zone(x, y + 5, 90, 110).setInteractive({ useHandCursor: true });
        gfx.hitZone.on('pointerdown', () => this._onUnitClicked(unit));
        gfx.hitZone.on('pointerover', () => this._onUnitHover(unit, true));
        gfx.hitZone.on('pointerout', () => this._onUnitHover(unit, false));

        // 현재 턴 강조
        gfx.turnRing = this.add.graphics();

        this.unitGfx[unit.id] = { ...gfx, baseX: x, baseY: y };
    }

    _getClassColor(classKey) {
        const colors = { warrior: 0x4488ff, rogue: 0xcc44cc, mage: 0x8844ff, archer: 0x44cc44, priest: 0xffcc44, alchemist: 0x44cccc };
        return colors[classKey] || 0x888888;
    }

    _drawActionPanel() {
        this.actionPanelY = 540;
        this.actionPanelBg = this.add.graphics();
        this.actionPanelBg.fillStyle(0x111122, 1);
        this.actionPanelBg.fillRect(0, 510, 1280, 210);
        this.actionPanelBg.lineStyle(1, 0x333355, 0.7);
        this.actionPanelBg.lineBetween(0, 510, 1280, 510);

        this.actionTitleText = this.add.text(640, 525, '', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffcc88', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.actionButtons = [];  // 라운드마다 갱신
    }

    _drawTurnQueue() {
        // 화면 우측에 턴 순서 표시
        this.queueText = this.add.text(1270, 60, '', {
            fontSize: '11px', fontFamily: 'monospace', color: '#aaaabb',
            align: 'right'
        }).setOrigin(1, 0);
    }

    _updateTurnQueueDisplay() {
        const lines = ['── 턴 순서 ──'];
        for (let i = 0; i < this.combat.turnQueue.length; i++) {
            const u = this.combat.turnQueue[i];
            if (!u.alive) continue;
            const prefix = i === this.combat.currentTurnIdx ? '▶ ' : '   ';
            const teamIcon = u.team === 'ally' ? '🟦' : '🟥';
            lines.push(`${prefix}${teamIcon} ${u.name} (P${u.position})`);
        }
        this.queueText.setText(lines.join('\n'));
    }

    _processNextTurn() {
        if (this.battleEnded) return;

        // 시각 갱신
        this._refreshAllUnits();
        this._updateTurnQueueDisplay();

        // 전투 종료 체크
        const end = DarkestCombat.checkBattleEnd(this.combat);
        if (end.ended) {
            this._endBattle(end.winner === 'ally');
            return;
        }

        // 라운드 끝 체크
        if (DarkestCombat.isRoundDone(this.combat)) {
            this._endRound();
            return;
        }

        const current = DarkestCombat.getCurrentTurnUnit(this.combat);
        if (!current) {
            this._endRound();
            return;
        }

        // 턴 강조
        this._highlightCurrentUnit(current);

        if (current.team === 'ally') {
            this._showAllyActionPanel(current);
        } else {
            // 적 AI
            this.time.delayedCall(700, () => {
                const result = DarkestCombat.executeAiAction(this.combat, current);
                if (result) {
                    // 적 액션 명 화면 표시
                    this._showEnemyActionLabel(current, result.actionName, result.actionIcon);
                    // 결과들 표시
                    if (result.allResults) {
                        result.allResults.forEach(r => this._showActionResult(r, current));
                    } else {
                        this._showActionResult(result, current);
                    }
                }
                this._refreshAllUnits();
                this.time.delayedCall(800, () => {
                    DarkestCombat.compactPositions(this.combat);
                    DarkestCombat.advanceTurn(this.combat);
                    this._processNextTurn();
                });
            });
        }
    }

    _showEnemyActionLabel(enemy, actionName, icon) {
        const g = this.unitGfx[enemy.id];
        if (!g) return;
        const label = this.add.text(g.baseX, g.baseY - 90, `${icon || '⚔'} ${actionName}`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#ff8866', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(50);
        this.tweens.add({
            targets: label, y: label.y - 25, alpha: 0,
            duration: 1200, onComplete: () => label.destroy()
        });
    }

    _highlightCurrentUnit(unit) {
        // 모든 ring 제거 후 현재만
        Object.values(this.unitGfx).forEach(g => g.turnRing && g.turnRing.clear());
        const g = this.unitGfx[unit.id];
        if (!g) return;
        g.turnRing.clear();
        g.turnRing.lineStyle(3, 0xffcc44, 1);
        g.turnRing.strokeCircle(g.baseX, g.baseY, 44);
    }

    _showAllyActionPanel(unit) {
        // 액션 버튼 4개 표시
        this.actionButtons.forEach(b => b.destroy && b.destroy());
        this.actionButtons = [];

        this.actionTitleText.setText(`▶ ${unit.name}의 턴 (포지션 ${unit.position}) — 액션 클릭 또는 유닛 클릭하여 상세 정보`);

        const actions = (typeof getClassActions === 'function') ? getClassActions(unit.classKey) : [];
        const startX = 80, btnW = 220, btnH = 60, gap = 10;

        actions.forEach((action, i) => {
            const x = startX + i * (btnW + gap);
            const y = 560;
            const canUse = action.casterPositions.includes(unit.position);
            const cd = unit.cooldowns[action.id] || 0;
            const onCooldown = cd > 0;
            const disabled = !canUse || onCooldown;

            const btnBg = this.add.graphics();
            const color = disabled ? 0x222233 : (action.type === 'skill' ? 0x664422 : 0x223355);
            btnBg.fillStyle(color, 1);
            btnBg.fillRoundedRect(x, y, btnW, btnH, 5);
            btnBg.lineStyle(1, disabled ? 0x333344 : (action.type === 'skill' ? 0xcc8844 : 0x5588cc), 0.7);
            btnBg.strokeRoundedRect(x, y, btnW, btnH, 5);
            this.actionButtons.push(btnBg);

            const titleColor = disabled ? '#555555' : (action.type === 'skill' ? '#ffcc88' : '#aaccff');
            const titleText = this.add.text(x + 10, y + 6, `${action.icon || ''} ${action.name}`, {
                fontSize: '13px', fontFamily: 'monospace', color: titleColor, fontStyle: 'bold'
            });
            this.actionButtons.push(titleText);

            const descColor = disabled ? '#333344' : '#888899';
            const descText = this.add.text(x + 10, y + 26, action.desc || '', {
                fontSize: '10px', fontFamily: 'monospace', color: descColor,
                wordWrap: { width: btnW - 20 }
            });
            this.actionButtons.push(descText);

            // 쿨다운 표시
            if (onCooldown) {
                const cdText = this.add.text(x + btnW - 10, y + 6, `쿨 ${cd}R`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#ff6666', fontStyle: 'bold'
                }).setOrigin(1, 0);
                this.actionButtons.push(cdText);
            } else if (!canUse) {
                const lockText = this.add.text(x + btnW - 10, y + 6, '🔒 위치', {
                    fontSize: '10px', fontFamily: 'monospace', color: '#aa6666'
                }).setOrigin(1, 0);
                this.actionButtons.push(lockText);
            }

            if (!disabled) {
                const hit = this.add.zone(x + btnW/2, y + btnH/2, btnW, btnH).setInteractive({ useHandCursor: true });
                hit.on('pointerdown', () => this._selectAction(unit, action));
                this.actionButtons.push(hit);
            }
        });

        // 스킵 버튼 (5번째 슬롯 위치)
        const skipX = startX + 4 * (btnW + gap);
        const skipBg = this.add.graphics();
        skipBg.fillStyle(0x332244, 1);
        skipBg.fillRoundedRect(skipX, 560, 160, 60, 5);
        skipBg.lineStyle(1, 0x665577, 0.7);
        skipBg.strokeRoundedRect(skipX, 560, 160, 60, 5);
        this.actionButtons.push(skipBg);
        this.actionButtons.push(this.add.text(skipX + 80, 566, '⏭ 스킵', {
            fontSize: '13px', fontFamily: 'monospace', color: '#ccaaee', fontStyle: 'bold'
        }).setOrigin(0.5));
        this.actionButtons.push(this.add.text(skipX + 80, 590, '턴 넘기기 (방어 +20%)', {
            fontSize: '9px', fontFamily: 'monospace', color: '#888899',
            wordWrap: { width: 150 }
        }).setOrigin(0.5));

        const skipHit = this.add.zone(skipX + 80, 590, 160, 60).setInteractive({ useHandCursor: true });
        skipHit.on('pointerdown', () => this._skipTurn(unit));
        this.actionButtons.push(skipHit);
    }

    /** 턴 스킵 — 방어 자세 (DEF +20% 1라운드) */
    _skipTurn(unit) {
        unit.statusEffects.push({ type: 'buff_def', duration: 1, value: 0.2 });
        // 시각 효과
        const g = this.unitGfx[unit.id];
        if (g) {
            const label = this.add.text(g.baseX, g.baseY - 70, '⏭ 방어 자세', {
                fontSize: '12px', fontFamily: 'monospace', color: '#aaccee', fontStyle: 'bold',
                stroke: '#000', strokeThickness: 3
            }).setOrigin(0.5).setDepth(50);
            this.tweens.add({ targets: label, y: label.y - 20, alpha: 0, duration: 1000, onComplete: () => label.destroy() });
        }
        this._refreshAllUnits();
        this.time.delayedCall(500, () => {
            DarkestCombat.advanceTurn(this.combat);
            this._processNextTurn();
        });
    }

    _selectAction(caster, action) {
        this.selectedAction = { caster, action };

        // 타겟 선택 모드 — 가능한 타겟 강조
        this.actionTitleText.setText(`▶ ${action.name} — 타겟 선택 (취소: 다시 행동 선택)`);

        // 자동 타겟 (전체/all 또는 타겟 정해진 것)
        if (action.targetCount === 'all') {
            // 자동 실행
            this._executePlayerAction([]);
            return;
        }

        if (typeof action.targetCount === 'number' && action.targetCount > 1 && action.targetType !== 'enemy_pair') {
            // 다중 타겟 (회전 베기, 다중 사격) — 자동
            this._executePlayerAction([]);
            return;
        }

        // 타겟 가능한 유닛 강조
        const targetPool = action.targetType.startsWith('enemy') ? this.combat.enemies : this.combat.allies;
        targetPool.forEach(u => {
            if (!u.alive) return;
            const inRange = action.targetPositions.includes(u.position);
            if (inRange) {
                const g = this.unitGfx[u.id];
                if (g) {
                    g.turnRing.lineStyle(3, action.targetType.startsWith('enemy') ? 0xff4444 : 0x44ff44, 0.7);
                    g.turnRing.strokeCircle(g.baseX, g.baseY, 50);
                }
            }
        });
    }

    _onUnitClicked(unit) {
        // 액션 선택 중 → 타겟으로 사용
        if (this.selectedAction) {
            const action = this.selectedAction.action;
            const isEnemyTarget = action.targetType.startsWith('enemy');
            const expectedTeam = isEnemyTarget ? 'enemy' : 'ally';
            if (unit.team !== expectedTeam) return;
            if (!action.targetPositions.includes(unit.position)) return;
            if (!unit.alive) return;
            this._executePlayerAction([unit.position]);
            return;
        }
        // 액션 선택 안 한 상태 — 유닛 상세 정보 토글
        if (this._inspectedUnitId === unit.id) {
            this._hideInspectPanel();
        } else {
            this._showInspectPanel(unit);
        }
    }

    _onUnitHover(unit, isOver) {
        // 호버 시 빠른 미리보기 (작은 툴팁)
        if (!isOver) {
            if (this._hoverPanel) {
                this._hoverPanel.forEach(o => o.destroy && o.destroy());
                this._hoverPanel = null;
            }
            return;
        }
        if (this._hoverPanel) this._hoverPanel.forEach(o => o.destroy && o.destroy());

        const g = this.unitGfx[unit.id];
        if (!g) return;
        const px = g.baseX, py = g.baseY - 130;
        const w = 200, h = 90;

        const objs = [];
        const bg = this.add.graphics().setDepth(60);
        bg.fillStyle(0x000000, 0.9);
        bg.fillRoundedRect(px - w/2, py, w, h, 4);
        bg.lineStyle(1, 0x88aacc, 0.7);
        bg.strokeRoundedRect(px - w/2, py, w, h, 4);
        objs.push(bg);

        const teamColor = unit.team === 'ally' ? '#88ccff' : '#ff8888';
        objs.push(this.add.text(px, py + 6, `${unit.name} (P${unit.position})`, {
            fontSize: '11px', fontFamily: 'monospace', color: teamColor, fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(61));
        objs.push(this.add.text(px - w/2 + 8, py + 24, `HP ${unit.hp}/${unit.maxHp}  ATK ${unit.atk}  DEF ${unit.def}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#cccccc'
        }).setDepth(61));
        objs.push(this.add.text(px - w/2 + 8, py + 40, `SPD ${unit.spd}  CRIT ${Math.floor(unit.critRate*100)}%`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#cccccc'
        }).setDepth(61));
        if (unit.statusEffects && unit.statusEffects.length > 0) {
            const statusStr = unit.statusEffects.map(e => {
                const names = { bleed:'출혈', burn:'화상', slow:'둔화', taunt_active:'도발', buff_atk:'ATK↑', buff_def:'DEF↑' };
                return `${names[e.type]||e.type}(${e.duration})`;
            }).join(' ');
            objs.push(this.add.text(px - w/2 + 8, py + 56, statusStr, {
                fontSize: '9px', fontFamily: 'monospace', color: '#ffaa66',
                wordWrap: { width: w - 16 }
            }).setDepth(61));
        }
        objs.push(this.add.text(px, py + h - 14, '(클릭: 상세 정보)', {
            fontSize: '9px', fontFamily: 'monospace', color: '#777788'
        }).setOrigin(0.5).setDepth(61));

        this._hoverPanel = objs;
    }

    _showInspectPanel(unit) {
        this._hideInspectPanel();
        this._inspectedUnitId = unit.id;

        const overlay = this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.7).setDepth(80);
        const w = 540, h = 460, mx = 640 - w/2, my = 360 - h/2;
        const bg = this.add.graphics().setDepth(81);
        bg.fillStyle(0x111122, 1);
        bg.fillRoundedRect(mx, my, w, h, 6);
        const borderColor = unit.team === 'ally' ? 0x4488cc : 0xcc4444;
        bg.lineStyle(2, borderColor, 0.8);
        bg.strokeRoundedRect(mx, my, w, h, 6);
        const objs = [overlay, bg];

        const teamLabel = unit.team === 'ally' ? '🟦 아군' : '🟥 적';
        objs.push(this.add.text(mx + w/2, my + 18, `${teamLabel} — ${unit.name}`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc88', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(82));
        objs.push(this.add.text(mx + w/2, my + 42, `포지션 ${unit.position}  |  ${unit.classKey}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aaccee'
        }).setOrigin(0.5).setDepth(82));

        // 스탯
        let cy = my + 70;
        objs.push(this.add.text(mx + 20, cy, '── 스탯 ──', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aaccee', fontStyle: 'bold'
        }).setDepth(82));
        cy += 22;
        const stats = [
            ['HP', `${unit.hp}/${unit.maxHp}`], ['ATK', unit.atk], ['DEF', unit.def],
            ['SPD', unit.spd], ['CRIT', `${Math.floor(unit.critRate*100)}%`], ['CRIT 배율', `${unit.critDmg.toFixed(1)}x`]
        ];
        stats.forEach((s, i) => {
            objs.push(this.add.text(mx + 30 + (i % 2) * 250, cy + Math.floor(i/2) * 20, `${s[0]}: ${s[1]}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#cccccc'
            }).setDepth(82));
        });
        cy += 70;

        // 상태 효과
        objs.push(this.add.text(mx + 20, cy, '── 상태 효과 ──', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aaccee', fontStyle: 'bold'
        }).setDepth(82));
        cy += 22;
        if (!unit.statusEffects || unit.statusEffects.length === 0) {
            objs.push(this.add.text(mx + 30, cy, '없음', {
                fontSize: '11px', fontFamily: 'monospace', color: '#666677'
            }).setDepth(82));
            cy += 18;
        } else {
            unit.statusEffects.forEach(e => {
                const names = { bleed:'🩸 출혈', burn:'🔥 화상', slow:'🐌 둔화', taunt_active:'⚠ 도발', buff_atk:'⬆ ATK 버프', buff_def:'🛡 DEF 버프' };
                const label = names[e.type] || e.type;
                objs.push(this.add.text(mx + 30, cy, `${label}  (${e.duration}R 남음)`, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#ffaa66'
                }).setDepth(82));
                cy += 16;
            });
        }
        cy += 10;

        // 액션 목록
        objs.push(this.add.text(mx + 20, cy, '── 보유 액션 ──', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aaccee', fontStyle: 'bold'
        }).setDepth(82));
        cy += 22;
        const actionIds = unit.actions || [];
        actionIds.forEach(aid => {
            const action = ACTION_DATA[aid];
            if (!action) return;
            const cd = unit.cooldowns[aid] || 0;
            const cdLabel = cd > 0 ? ` 쿨${cd}R` : (action.cooldown > 0 ? ` (쿨${action.cooldown})` : '');
            const canUse = action.casterPositions.includes(unit.position);
            const color = !canUse ? '#666677' : (action.type === 'skill' ? '#ffcc88' : '#cccccc');
            objs.push(this.add.text(mx + 30, cy, `${action.icon || ''} ${action.name}${cdLabel}`, {
                fontSize: '11px', fontFamily: 'monospace', color, fontStyle: 'bold'
            }).setDepth(82));
            objs.push(this.add.text(mx + 50, cy + 14, `${action.desc || ''} (P${action.casterPositions.join(',')})`, {
                fontSize: '9px', fontFamily: 'monospace', color: '#888899'
            }).setDepth(82));
            cy += 32;
        });

        // 닫기
        objs.push(UIButton.create(this, mx + w - 50, my + 18, 70, 24, '닫기', {
            color: 0x444455, hoverColor: 0x555566, textColor: '#aaaaaa', fontSize: 11, depth: 82,
            onClick: () => this._hideInspectPanel()
        }));

        // 배경 클릭 시 닫기
        const hitBg = this.add.zone(640, 360, 1280, 720).setInteractive().setDepth(80);
        hitBg.on('pointerdown', (pointer) => {
            const px = pointer.x, py = pointer.y;
            if (px < mx || px > mx + w || py < my || py > my + h) {
                this._hideInspectPanel();
            }
        });
        objs.push(hitBg);

        this._inspectPanel = objs;
    }

    _hideInspectPanel() {
        if (this._inspectPanel) {
            this._inspectPanel.forEach(o => o.destroy && o.destroy());
            this._inspectPanel = null;
        }
        this._inspectedUnitId = null;
    }

    _executePlayerAction(targetPositions) {
        if (!this.selectedAction) return;
        const { caster, action } = this.selectedAction;
        const result = DarkestCombat.executeAction(this.combat, caster, action.id, targetPositions);
        this.selectedAction = null;

        if (result.error) {
            UIToast.show(this, `행동 실패: ${result.error}`, { color: '#ff6644' });
            return;
        }

        // 결과 표시
        if (result.results) {
            result.results.forEach(r => this._showActionResult(r, caster));
        }

        // 사망 처리 + 위치 정리
        this.time.delayedCall(500, () => {
            DarkestCombat.compactPositions(this.combat);
            this._refreshAllUnits();
            this.time.delayedCall(300, () => {
                DarkestCombat.advanceTurn(this.combat);
                this._processNextTurn();
            });
        });
    }

    _showActionResult(result, caster) {
        const targetId = result.target;
        const g = this.unitGfx[targetId];
        if (!g) return;
        if (result.damage !== undefined) {
            DamagePopup.show(this, g.baseX, g.baseY - 30, result.damage, result.isCrit ? 0xff8844 : 0xff4444, result.isCrit);
            this.cameras.main.shake(150, 0.003);
        }
        if (result.heal !== undefined) {
            DamagePopup.show(this, g.baseX, g.baseY - 30, `+${result.heal}`, 0x44ff88, false);
        }
        if (result.status) {
            // 상태 효과 적용 popup
            const statusNames = { bleed: '출혈', burn: '화상', slow: '둔화' };
            this.add.text(g.baseX, g.baseY - 50, statusNames[result.status] || result.status, {
                fontSize: '11px', fontFamily: 'monospace', color: '#ff88aa', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(50);
        }
    }

    _refreshAllUnits() {
        [...this.combat.allies, ...this.combat.enemies].forEach(u => {
            const g = this.unitGfx[u.id];
            if (!g) return;
            // 위치 업데이트
            let newX;
            if (u.team === 'ally') {
                newX = 200 + (4 - u.position) * 110;
            } else {
                newX = 720 + (u.position - 1) * 110;
            }
            if (g.baseX !== newX) {
                g.baseX = newX;
                this.tweens.add({
                    targets: [g.body, g.icon, g.name, g.hpBg, g.hpFill, g.hpText, g.posNum, g.statusText, g.hitZone, g.turnRing],
                    x: function(target) {
                        const dx = newX - g.baseX;
                        return target.x + dx;
                    },
                    duration: 300
                });
            }

            // HP 갱신
            g.hpFill.width = 90 * Math.max(0, u.hp / u.maxHp);
            g.hpText.setText(`${Math.max(0, u.hp)}/${u.maxHp}`);
            g.posNum.setText(`[${u.position}]`);

            // 상태 효과
            const statusIcons = u.statusEffects.map(e => {
                const map = { bleed: '🩸', burn: '🔥', slow: '🐌', taunt_active: '⚠', buff_atk: '⬆', buff_def: '🛡' };
                return map[e.type] || '·';
            }).join('');
            g.statusText.setText(statusIcons);

            // 사망 처리
            if (!u.alive) {
                g.body.clear();
                g.body.fillStyle(0x333333, 0.5);
                g.body.fillCircle(g.baseX, g.baseY, 38);
                g.icon.setText('💀');
                g.icon.setAlpha(0.4);
                g.name.setAlpha(0.4);
            }
        });
    }

    _endRound() {
        // 다음 라운드 또는 끝
        this.currentRound++;
        if (this.currentRound > this.maxRounds) {
            this._endBattle(true);
            return;
        }
        this.headerText.setText(`Round ${this.currentRound}/${this.maxRounds}  |  Blood Pit Lv.${this.gameState.zoneLevel[this.zoneKey]}`);

        // 다음 웨이브 등장 (간소화: 현재 적 그대로 + 추가)
        DarkestCombat.startRound(this.combat);
        this._processNextTurn();
    }

    _endBattle(success) {
        if (this.battleEnded) return;
        this.battleEnded = true;

        const gs = this.gameState;
        const zone = ZONE_DATA[this.zoneKey];
        const zoneLevel = gs.zoneLevel[this.zoneKey];

        // 보상 계산
        if (success) {
            this.totalGold = Math.floor((zone.baseGoldReward + zoneLevel * 15) * 1.5 * this.currentRound);
            this.totalXp = Math.floor((zone.baseXpReward + zoneLevel * 8) * 1.5);
        }

        // 결과 정리
        const survivors = this.combat.allies.filter(u => u.alive);
        const casualties = this.combat.allies.filter(u => !u.alive).map(u => u.ref);

        // 용병 HP 동기화
        this.combat.allies.forEach(u => {
            if (u.ref) {
                u.ref.currentHp = u.hp;
            }
        });

        const result = {
            success, zoneKey: this.zoneKey,
            rounds: this.currentRound - (success ? 1 : 0),
            goldEarned: this.totalGold,
            xpEarned: this.totalXp,
            casualties, survivors: survivors.map(u => u.ref),
            loot: this.loot,
            events: [success ? '전투 승리!' : '전투 실패...'],
            zoneLevelUp: success && (this.currentRound > this.maxRounds)
        };

        this.scene.start('RunResultScene', { gameState: gs, result });
    }
}
