/**
 * ManualBattleScene — 다키스트 스타일 수동 전투.
 * 4명 일렬 × 적 4마리 일렬 + SPD 턴제 + 위치 기반 액션.
 *
 * data: { gameState, zoneKey, party }
 */
class ManualBattleScene extends Phaser.Scene {
    constructor() { super('ManualBattleScene'); }

    preload() {
        // 캐릭터 스프라이트 로드 (파일 없으면 자동 폴백)
        const classes = ['warrior', 'rogue', 'archer', 'mage', 'priest', 'alchemist'];
        classes.forEach(cls => {
            if (!this.textures.exists(`char_${cls}`)) {
                this.load.image(`char_${cls}_raw`, `assets/characters/${cls}.png`);
            }
        });
        // 로딩 에러 무시 (파일 없으면 폴백)
        this.load.on('loaderror', (file) => {
            console.warn('스프라이트 로드 실패 (이모지 폴백):', file.key);
        });
    }

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
        // 캐릭터 스프라이트 흰 배경 자동 제거 (최초 1회)
        this._processCharacterSprites();

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
            onClick: () => { this._retreated = true; this._endBattle(false); }
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
        const container = this.add.container(x, y);

        // 캐릭터 원형 (배경)
        const bodyColor = isAlly ? this._getClassColor(unit.classKey) : (ENEMY_DATA[unit.classKey] ? ENEMY_DATA[unit.classKey].color : 0xcc4444);
        const body = this.add.graphics();
        body.fillStyle(bodyColor, 0.4);
        body.fillCircle(0, 0, 42);
        body.lineStyle(2, 0xffffff, 0.7);
        body.strokeCircle(0, 0, 42);
        container.add(body);

        // 캐릭터 — 스프라이트 있으면 사용, 없으면 이모지
        let iconText;
        if (isAlly && this._useCharSprite(unit.classKey)) {
            iconText = this.add.image(0, -5, `char_${unit.classKey}`).setOrigin(0.5);
            // 사이즈 맞춤 (직경 80 정도)
            const desired = 80;
            const scale = desired / Math.max(iconText.width, iconText.height);
            iconText.setScale(scale);
            // 적 쪽 향하게 (아군은 우측 보게)
            if (unit.team === 'ally') iconText.setFlipX(false);
        } else {
            const icon = isAlly ? (CLASS_DATA[unit.classKey]?.icon || '?') : '👹';
            iconText = this.add.text(0, -5, icon, { fontSize: '32px' }).setOrigin(0.5);
        }
        container.add(iconText);

        // 포지션 번호
        const posNum = this.add.text(0, -60, `[${unit.position}]`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#88aaff', fontStyle: 'bold',
            stroke: '#000', strokeThickness: 2
        }).setOrigin(0.5);
        container.add(posNum);

        // 이름
        const nameText = this.add.text(0, 50, unit.name, {
            fontSize: '11px', fontFamily: 'monospace', color: '#ccccdd'
        }).setOrigin(0.5);
        container.add(nameText);

        // HP바
        const hpBg = this.add.rectangle(0, 70, 90, 8, 0x331111);
        const hpFill = this.add.rectangle(-45, 70, 90 * (unit.hp / unit.maxHp), 8, 0x44ff44).setOrigin(0, 0.5);
        const hpText = this.add.text(0, 70, `${unit.hp}/${unit.maxHp}`, {
            fontSize: '9px', fontFamily: 'monospace', color: '#ffffff', stroke: '#000', strokeThickness: 2
        }).setOrigin(0.5);
        container.add(hpBg);
        container.add(hpFill);
        container.add(hpText);

        // 상태 효과
        const statusText = this.add.text(0, 88, '', {
            fontSize: '12px', fontFamily: 'monospace', color: '#ffaa88'
        }).setOrigin(0.5);
        container.add(statusText);

        // 턴 강조 링 (컨테이너 외부 — 컨테이너와 함께 이동시킴)
        const turnRing = this.add.graphics();

        // 인터랙티브
        const hitZone = this.add.zone(0, 5, 90, 110).setInteractive({ useHandCursor: true });
        hitZone.on('pointerdown', () => this._onUnitClicked(unit));
        hitZone.on('pointerover', () => this._onUnitHover(unit, true));
        hitZone.on('pointerout', () => this._onUnitHover(unit, false));
        container.add(hitZone);

        this.unitGfx[unit.id] = {
            container, body, iconText, posNum, nameText, hpBg, hpFill, hpText, statusText, turnRing, hitZone,
            bodyColor,
            baseX: x, baseY: y
        };
    }

    _getClassColor(classKey) {
        const colors = { warrior: 0x4488ff, rogue: 0xcc44cc, mage: 0x8844ff, archer: 0x44cc44, priest: 0xffcc44, alchemist: 0x44cccc };
        return colors[classKey] || 0x888888;
    }

    /**
     * 캐릭터 스프라이트 흰 배경 자동 제거.
     * 'char_xxx_raw' 텍스처를 가공해서 'char_xxx'로 캐싱.
     */
    _processCharacterSprites() {
        const classes = ['warrior', 'rogue', 'archer', 'mage', 'priest', 'alchemist'];
        classes.forEach(cls => {
            const dstKey = `char_${cls}`;
            const srcKey = `char_${cls}_raw`;
            if (this.textures.exists(dstKey)) return;
            if (!this.textures.exists(srcKey)) return;

            const src = this.textures.get(srcKey).getSourceImage();
            const w = src.width, h = src.height;
            const canvas = this.textures.createCanvas(dstKey, w, h);
            if (!canvas) return;
            const ctx = canvas.context;
            ctx.drawImage(src, 0, 0);
            const imageData = ctx.getImageData(0, 0, w, h);
            const data = imageData.data;
            // 흰색에 가까운 픽셀 투명 처리 (R>235 G>235 B>235)
            for (let i = 0; i < data.length; i += 4) {
                const r = data[i], g = data[i+1], b = data[i+2];
                if (r > 235 && g > 235 && b > 235) {
                    data[i+3] = 0;
                } else if (r > 220 && g > 220 && b > 220) {
                    // 경계 알파 부드럽게
                    data[i+3] = Math.floor(((255 - r) + (255 - g) + (255 - b)) / 3 * 8);
                }
            }
            ctx.putImageData(imageData, 0, 0);
            canvas.refresh();
        });
    }

    /**
     * 클래스/적의 시각 표현 (스프라이트 있으면 사용, 없으면 이모지)
     */
    _useCharSprite(classKey) {
        return this.textures.exists(`char_${classKey}`);
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
        const label = this.add.text(g.container.x, g.container.y - 90, `${icon || '⚔'} ${actionName}`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#ff8866', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(50);
        this.tweens.add({
            targets: label, y: label.y - 25, alpha: 0,
            duration: 1200, onComplete: () => label.destroy()
        });
    }

    _highlightCurrentUnit(unit) {
        Object.values(this.unitGfx).forEach(g => g.turnRing && g.turnRing.clear());
        const g = this.unitGfx[unit.id];
        if (!g) return;
        g.turnRing.clear();
        g.turnRing.lineStyle(3, 0xffcc44, 1);
        g.turnRing.strokeCircle(g.container.x, g.container.y, 44);
        // 컨테이너 살짝 펄스
        this.tweens.add({
            targets: g.container, scale: 1.1, duration: 200, yoyo: true
        });
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

        // === 액션 패널 위 별도 줄: 위치 이동 + 스킵 ===
        const aliveTeam = this.combat.allies.filter(u => u.alive);
        const canMoveForward = unit.position > 1;
        const canMoveBack = unit.position < aliveTeam.length;

        const utilY = 520;  // 액션 슬롯(560) 위
        const utilW = 130, utilH = 32;
        const utilStartX = 640 - (utilW * 3 + 20) / 2;  // 가운데 정렬 3버튼

        // ◀ 앞열로
        const fwdX = utilStartX;
        const fwdBg = this.add.graphics();
        fwdBg.fillStyle(canMoveForward ? 0x224488 : 0x222233, 1);
        fwdBg.fillRoundedRect(fwdX, utilY, utilW, utilH, 5);
        fwdBg.lineStyle(1, canMoveForward ? 0x4488cc : 0x333344, 0.8);
        fwdBg.strokeRoundedRect(fwdX, utilY, utilW, utilH, 5);
        this.actionButtons.push(fwdBg);
        this.actionButtons.push(this.add.text(fwdX + utilW/2, utilY + utilH/2, '◀ 앞열 이동', {
            fontSize: '13px', fontFamily: 'monospace',
            color: canMoveForward ? '#aaccff' : '#555555', fontStyle: 'bold'
        }).setOrigin(0.5));
        if (canMoveForward) {
            const fwdHit = this.add.zone(fwdX + utilW/2, utilY + utilH/2, utilW, utilH).setInteractive({ useHandCursor: true });
            fwdHit.on('pointerdown', () => this._moveUnit(unit, -1));
            this.actionButtons.push(fwdHit);
        }

        // ⏭ 스킵
        const skipX = fwdX + utilW + 10;
        const skipBg = this.add.graphics();
        skipBg.fillStyle(0x332244, 1);
        skipBg.fillRoundedRect(skipX, utilY, utilW, utilH, 5);
        skipBg.lineStyle(1, 0x665577, 0.8);
        skipBg.strokeRoundedRect(skipX, utilY, utilW, utilH, 5);
        this.actionButtons.push(skipBg);
        this.actionButtons.push(this.add.text(skipX + utilW/2, utilY + utilH/2, '⏭ 스킵 (방어 +20%)', {
            fontSize: '12px', fontFamily: 'monospace', color: '#ccaaee', fontStyle: 'bold'
        }).setOrigin(0.5));
        const skipHit = this.add.zone(skipX + utilW/2, utilY + utilH/2, utilW, utilH).setInteractive({ useHandCursor: true });
        skipHit.on('pointerdown', () => this._skipTurn(unit));
        this.actionButtons.push(skipHit);

        // ▶ 뒤열로
        const backX = skipX + utilW + 10;
        const backBg = this.add.graphics();
        backBg.fillStyle(canMoveBack ? 0x224488 : 0x222233, 1);
        backBg.fillRoundedRect(backX, utilY, utilW, utilH, 5);
        backBg.lineStyle(1, canMoveBack ? 0x4488cc : 0x333344, 0.8);
        backBg.strokeRoundedRect(backX, utilY, utilW, utilH, 5);
        this.actionButtons.push(backBg);
        this.actionButtons.push(this.add.text(backX + utilW/2, utilY + utilH/2, '뒤열 이동 ▶', {
            fontSize: '13px', fontFamily: 'monospace',
            color: canMoveBack ? '#aaccff' : '#555555', fontStyle: 'bold'
        }).setOrigin(0.5));
        if (canMoveBack) {
            const backHit = this.add.zone(backX + utilW/2, utilY + utilH/2, utilW, utilH).setInteractive({ useHandCursor: true });
            backHit.on('pointerdown', () => this._moveUnit(unit, 1));
            this.actionButtons.push(backHit);
        }
    }

    /** 유닛 위치 이동 (행동 소모) — amount: -1 앞열, +1 후열 */
    _moveUnit(unit, amount) {
        DarkestCombat._shiftUnit(this.combat, unit, amount);
        const g = this.unitGfx[unit.id];
        if (g) {
            const label = this.add.text(g.container.x, g.container.y - 80, amount < 0 ? '◀ 이동' : '이동 ▶', {
                fontSize: '14px', fontFamily: 'monospace', color: '#aaccff', fontStyle: 'bold',
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

    /** 턴 스킵 — 방어 자세 (DEF +20% 1라운드) */
    _skipTurn(unit) {
        unit.statusEffects.push({ type: 'buff_def', duration: 1, value: 0.2 });
        // 시각 효과
        const g = this.unitGfx[unit.id];
        if (g) {
            const label = this.add.text(g.container.x, g.container.y - 70, '⏭ 방어 자세', {
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
                    g.turnRing.strokeCircle(g.container.x, g.container.y, 50);
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
        const px = g.container.x, py = g.container.y - 130;
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
        const tx = g.container.x, ty = g.container.y;

        // 공격 라인 (캐스터 → 타겟)
        if (caster && this.unitGfx[caster.id] && result.damage !== undefined) {
            this._showAttackLine(this.unitGfx[caster.id], g);
        }

        // 데미지
        if (result.damage !== undefined) {
            this._showBigDamage(tx, ty - 30, result.damage, result.isCrit);
            // 타겟 흔들기
            this.tweens.add({
                targets: g.container,
                x: tx + 8, duration: 50, yoyo: true, repeat: 2,
                onComplete: () => g.container.x = tx
            });
            this.cameras.main.shake(result.isCrit ? 250 : 120, result.isCrit ? 0.006 : 0.003);
        }
        if (result.heal !== undefined) {
            this._showHealPopup(tx, ty - 30, result.heal);
        }
        if (result.status) {
            const statusNames = { bleed: '🩸 출혈', burn: '🔥 화상', slow: '🐌 둔화' };
            const label = this.add.text(tx, ty - 60, statusNames[result.status] || result.status, {
                fontSize: '13px', fontFamily: 'monospace', color: '#ff88aa', fontStyle: 'bold',
                stroke: '#000', strokeThickness: 3
            }).setOrigin(0.5).setDepth(50);
            this.tweens.add({ targets: label, y: ty - 80, alpha: 0, duration: 1000, onComplete: () => label.destroy() });
        }
    }

    _showBigDamage(x, y, damage, isCrit) {
        const size = isCrit ? '36px' : '24px';
        const color = isCrit ? '#ffcc44' : '#ff4444';
        const text = isCrit ? `${damage}!!` : `${damage}`;
        const dmg = this.add.text(x, y, text, {
            fontSize: size, fontFamily: 'Arial Black, sans-serif', color, fontStyle: 'bold',
            stroke: '#000000', strokeThickness: isCrit ? 5 : 3
        }).setOrigin(0.5).setDepth(60);

        if (isCrit) {
            // 크리티컬: 큰 폰트 + 펄스
            dmg.setScale(0.5);
            this.tweens.add({
                targets: dmg, scale: 1.4, duration: 200, ease: 'Back.easeOut'
            });
            this.tweens.add({
                targets: dmg, y: y - 60, alpha: 0,
                duration: 1200, delay: 300,
                onComplete: () => dmg.destroy()
            });
            // CRIT! 텍스트
            const critLabel = this.add.text(x, y - 35, 'CRIT!', {
                fontSize: '16px', fontFamily: 'Arial Black, sans-serif', color: '#ffaa00', fontStyle: 'bold',
                stroke: '#000', strokeThickness: 4
            }).setOrigin(0.5).setDepth(61);
            this.tweens.add({ targets: critLabel, y: y - 75, alpha: 0, duration: 1500, onComplete: () => critLabel.destroy() });
        } else {
            this.tweens.add({
                targets: dmg, y: y - 40, alpha: 0,
                duration: 900,
                onComplete: () => dmg.destroy()
            });
        }
    }

    _showHealPopup(x, y, heal) {
        const txt = this.add.text(x, y, `+${heal}`, {
            fontSize: '24px', fontFamily: 'Arial Black, sans-serif', color: '#44ff88', fontStyle: 'bold',
            stroke: '#003300', strokeThickness: 3
        }).setOrigin(0.5).setDepth(60);
        this.tweens.add({
            targets: txt, y: y - 40, alpha: 0,
            duration: 900,
            onComplete: () => txt.destroy()
        });
    }

    _showAttackLine(casterGfx, targetGfx) {
        const startX = casterGfx.container.x, startY = casterGfx.container.y;
        const endX = targetGfx.container.x, endY = targetGfx.container.y;
        // 프로젝타일 (작은 원)
        const proj = this.add.circle(startX, startY, 6, 0xffcc44).setDepth(55);
        this.tweens.add({
            targets: proj,
            x: endX, y: endY,
            duration: 200, ease: 'Cubic.easeOut',
            onComplete: () => {
                // 임팩트 플래시
                const flash = this.add.circle(endX, endY, 30, 0xffffff, 0.7).setDepth(54);
                this.tweens.add({
                    targets: flash, scale: 2, alpha: 0, duration: 200,
                    onComplete: () => flash.destroy()
                });
                proj.destroy();
            }
        });
    }

    _refreshAllUnits() {
        const slotW = 110;
        [...this.combat.allies, ...this.combat.enemies].forEach(u => {
            const g = this.unitGfx[u.id];
            if (!g) return;

            // 사망 유닛은 컨테이너 숨김
            if (!u.alive) {
                if (g.container.visible) {
                    // 사망 이펙트
                    const deathX = g.container.x, deathY = g.container.y;
                    this._createDeathFx(deathX, deathY);
                    this.tweens.add({
                        targets: g.container,
                        alpha: 0, scale: 0.5, duration: 400,
                        onComplete: () => g.container.setVisible(false)
                    });
                    g.turnRing.clear();
                }
                return;
            }

            // 살아있는 유닛 — 새 위치 계산
            let newX;
            if (u.team === 'ally') {
                newX = 200 + (4 - u.position) * slotW;
            } else {
                newX = 720 + (u.position - 1) * slotW;
            }

            // 위치 이동 (컨테이너 트윈)
            if (Math.abs(g.container.x - newX) > 1) {
                this.tweens.add({
                    targets: g.container,
                    x: newX,
                    duration: 350, ease: 'Cubic.easeInOut'
                });
                g.baseX = newX;
            }

            // HP 갱신
            g.hpFill.width = 90 * Math.max(0, u.hp / u.maxHp);
            const hpColor = (u.hp / u.maxHp) > 0.6 ? 0x44ff44 : (u.hp / u.maxHp) > 0.3 ? 0xffaa44 : 0xff4444;
            g.hpFill.fillColor = hpColor;
            g.hpText.setText(`${Math.max(0, u.hp)}/${u.maxHp}`);
            g.posNum.setText(`[${u.position}]`);

            // 상태 효과 아이콘
            const statusIcons = u.statusEffects.map(e => {
                const map = { bleed: '🩸', burn: '🔥', slow: '🐌', taunt_active: '⚠', buff_atk: '⬆', buff_def: '🛡' };
                return map[e.type] || '·';
            }).join('');
            g.statusText.setText(statusIcons);
        });

        // 턴 링도 현재 턴 유닛 위치로
        const current = DarkestCombat.getCurrentTurnUnit(this.combat);
        if (current) {
            const cg = this.unitGfx[current.id];
            if (cg) {
                cg.turnRing.clear();
                cg.turnRing.lineStyle(3, 0xffcc44, 1);
                cg.turnRing.strokeCircle(cg.container.x, cg.container.y, 44);
            }
        }
    }

    _createDeathFx(x, y) {
        // 폭발 파편
        const colors = [0xff4444, 0xff8844, 0xffcc44];
        for (let i = 0; i < 8; i++) {
            const angle = (i / 8) * Math.PI * 2;
            const dist = 30 + Math.random() * 20;
            const p = this.add.circle(x, y, 4, colors[i % colors.length]).setDepth(40);
            this.tweens.add({
                targets: p,
                x: x + Math.cos(angle) * dist,
                y: y + Math.sin(angle) * dist,
                alpha: 0, scale: 0.2,
                duration: 600,
                onComplete: () => p.destroy()
            });
        }
        // 💀 텍스트
        const skull = this.add.text(x, y, '💀', { fontSize: '32px' }).setOrigin(0.5).setDepth(41);
        this.tweens.add({
            targets: skull,
            y: y - 30, alpha: 0,
            duration: 800,
            onComplete: () => skull.destroy()
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
            events: [success ? '전투 승리!' : (this._retreated ? '후퇴' : '전투 실패...')],
            zoneLevelUp: success && (this.currentRound > this.maxRounds),
            retreated: !!this._retreated     // 후퇴 시 마을 이벤트 발동 차단
        };
        // 마을 진입 시 이벤트 차단 플래그
        if (this._retreated) gs._suppressNextTownEvent = true;

        this.scene.start('RunResultScene', { gameState: gs, result });
    }
}
