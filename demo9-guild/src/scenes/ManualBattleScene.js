/**
 * ManualBattleScene — 다키스트 스타일 수동 전투.
 * 4명 일렬 × 적 4마리 일렬 + SPD 턴제 + 위치 기반 액션.
 *
 * data: { gameState, zoneKey, party }
 */
class ManualBattleScene extends Phaser.Scene {
    constructor() { super('ManualBattleScene'); }

    preload() {
        // 캐릭터 스프라이트 로드
        const classes = ['warrior', 'rogue', 'archer', 'mage', 'priest', 'alchemist'];
        classes.forEach(cls => {
            if (!this.textures.exists(`char_${cls}`)) {
                this.load.image(`char_${cls}_raw`, `assets/characters/${cls}.png`);
            }
        });
        // 전투 배경 (zone별 — 1920×1080 권장)
        const zones = ['bloodpit', 'cargo', 'blackout'];
        zones.forEach(z => {
            if (!this.textures.exists(`bg_${z}`)) {
                this.load.image(`bg_${z}`, `assets/backgrounds/${z}.png`);
            }
        });
        // 액션 아이콘 — PNG 없으면 자동 이모지 폴백
        if (typeof ActionIcons !== 'undefined') {
            ActionIcons.preload(this);
        }
        this.load.on('loaderror', (file) => {
            // 액션 아이콘은 폴백 자동 처리 — 캐릭터/배경만 경고
            if (!file.key || !file.key.startsWith('act_')) {
                console.warn('자산 로드 실패 (폴백):', file.key);
            }
        });
    }

    init(data) {
        this.gameState = data.gameState;
        this.zoneKey = data.zoneKey || 'bloodpit';
        this.party = (data.party || []).slice(0, 4);

        // 적 생성
        const enemies = this._spawnEnemies();
        this.combat = DarkestCombat.createCombat(this.party, enemies);

        // === 스테미너 페널티 적용 (시너지/본드 전에) ===
        this.combat.allies.forEach(u => {
            const src = u.ref;
            if (src && typeof src.getStaminaMultiplier === 'function') {
                const mult = src.getStaminaMultiplier();
                if (mult < 1.0) {
                    u.atk = Math.floor(u.atk * mult);
                    u.def = Math.floor(u.def * mult);
                    u.spd = Math.floor(u.spd * mult);
                    u._staminaPenalty = mult;
                }
            }
        });

        // === 시너지 적용 (전투 시작 시 1회) ===
        this._activeSynergies = [];
        if (typeof applySynergies === 'function') {
            const classKeys = this.combat.allies.map(u => u.classKey);
            this._activeSynergies = applySynergies(this.combat.allies, classKeys) || [];
        }

        // === 본드 보너스 적용 ===
        this._activeBonds = [];
        if (typeof BondManager !== 'undefined') {
            this._activeBonds = BondManager.applyBondBonuses(this.gameState, this.combat.allies) || [];
        }

        this.currentRound = 1;
        // 다키스트 스타일 — 라운드 제한은 안전장치 (전멸로 끝나는 게 정상)
        // 너무 길어지면 자동 후퇴 처리
        this.maxRounds = 20;
        this.selectedAction = null;
        this.battleEnded = false;
        this.totalGold = 0;
        this.totalXp = 0;
        this.loot = [];

        // === BP v4: 핏 게이지 시스템 ===
        this.pitGauge = (typeof PitGauge !== 'undefined') ? new PitGauge(this.gameState) : null;
        this._crowdRushUnits = [];
        this._isBossRound = false;
    }

    create() {
        // 캐릭터 스프라이트 흰 배경 자동 제거 (최초 1회)
        this._processCharacterSprites();

        // pocket 아이템 슬롯 초기화
        if (!this.gameState.pocketSlots) this.gameState.pocketSlots = [null, null];

        // 배경 — 이미지 있으면 사용, 없으면 그라데이션 폴백
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a0e);
        const bgKey = `bg_${this.zoneKey}`;
        if (this.textures.exists(bgKey)) {
            // 배경은 전투 영역(y=0~440)에만 표시. 좌표상 (640, 220) 중심에 1280×440 으로 fit
            // 이미지 비율이 16:9 (1920×1080) 이므로 width 기준 fit → 1280 width 유지, height는 잘림
            const bg = this.add.image(640, 220, bgKey).setOrigin(0.5);
            bg.setDisplaySize(1280, 440);
            bg.setTint(0xaaaaaa);
            // 전투 분위기 비네트 — 상단 어둡게 + 하단 액션 패널 경계 페이드
            const vignette = this.add.graphics();
            vignette.fillStyle(0x000000, 0.4);
            vignette.fillRect(0, 0, 1280, 70);
            vignette.fillStyle(0x000000, 0.55);
            vignette.fillRect(0, 380, 1280, 60);  // 배경과 액션 패널 사이 페이드
        } else {
            // 폴백 — zone별 색조 그라데이션
            const colors = {
                bloodpit: [0x150810, 0x1a0a18],
                cargo:    [0x101822, 0x182030],
                blackout: [0x0a0818, 0x0e0e22]
            };
            const [c1, c2] = colors[this.zoneKey] || colors.bloodpit;
            const bgGfx = this.add.graphics();
            bgGfx.fillGradientStyle(c1, c1, c2, c2, 1);
            bgGfx.fillRect(0, 0, 1280, 440);
        }

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
        // 헤더 배경 (배경 이미지 위라도 텍스트 잘 보이게)
        const hdrBg = this.add.graphics();
        hdrBg.fillStyle(0x000000, 0.5);
        hdrBg.fillRect(0, 0, 1280, 44);

        this.headerText = this.add.text(640, 20, `Round ${this.currentRound}  |  Blood Pit Lv.${gs.zoneLevel[this.zoneKey]}`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc66', fontStyle: 'bold',
            stroke: '#000', strokeThickness: 2
        }).setOrigin(0.5);
        UIButton.create(this, 80, 22, 100, 28, '← 후퇴', {
            color: 0x554444, hoverColor: 0x665555, textColor: '#ffaaaa', fontSize: 11,
            onClick: () => { this._retreated = true; this._endBattle(false); }
        });

        // === 활성 시너지 표시 — 헤더 바로 아래 (y=44~70) ===
        let infoBarY = 44;
        if (this._activeSynergies && this._activeSynergies.length > 0) {
            const synBg = this.add.graphics();
            synBg.fillStyle(0x140828, 0.85);
            synBg.fillRect(0, infoBarY, 1280, 26);

            this.add.text(12, infoBarY + 6, '✨ 시너지:', {
                fontSize: '11px', fontFamily: 'monospace', color: '#ccaaff', fontStyle: 'bold'
            });

            let chipX = 90;
            this._activeSynergies.forEach(syn => {
                const typeColor = syn.type === 5 ? '#ffcc44' : syn.type === 3 ? '#ff88cc' : '#88ccff';
                const typeBadge = syn.type === 5 ? '⭐' : syn.type === 3 ? '★' : '✦';
                const chip = this.add.text(chipX, infoBarY + 5, `${typeBadge} ${syn.name} — ${syn.desc}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: typeColor,
                    stroke: '#000', strokeThickness: 1
                });
                chipX += chip.width + 22;
                if (chipX > 1240) return;
            });
            infoBarY += 26;
        }

        // === BP v4: 핏 게이지 바 ===
        if (this.pitGauge) {
            const pgBg = this.add.graphics();
            pgBg.fillStyle(0x220808, 0.85);
            pgBg.fillRect(0, infoBarY, 1280, 26);
            this.add.text(12, infoBarY + 6, '🩸 핏 게이지:', {
                fontSize: '11px', fontFamily: 'monospace', color: '#ff6644', fontStyle: 'bold'
            });
            const barX = 120, barW = 300, barH = 12, barY = infoBarY + 7;
            this._pitGaugeBarBg = this.add.rectangle(barX + barW/2, barY + barH/2, barW, barH, 0x331111);
            this._pitGaugeBarFill = this.add.rectangle(barX, barY + barH/2, 1, barH, 0xcc2222).setOrigin(0, 0.5);
            this._pitGaugeText = this.add.text(barX + barW + 8, barY + barH/2, '0%', {
                fontSize: '11px', fontFamily: 'monospace', color: '#ff8866', fontStyle: 'bold'
            }).setOrigin(0, 0.5);
            [33, 66].forEach(t => {
                const mx = barX + barW * (t / 100);
                const marker = this.add.graphics();
                marker.lineStyle(2, 0xffcc44, 0.8);
                marker.lineBetween(mx, barY - 1, mx, barY + barH + 1);
            });
            this._pitMultText = this.add.text(barX + barW + 60, barY + barH/2, '×1.0', {
                fontSize: '10px', fontFamily: 'monospace', color: '#ffcc88'
            }).setOrigin(0, 0.5);
            this._updatePitGaugeUI();
            infoBarY += 26;
        }

        // === 활성 본드 표시 ===
        if (this._activeBonds && this._activeBonds.length > 0) {
            const bondBg = this.add.graphics();
            bondBg.fillStyle(0x281428, 0.85);
            bondBg.fillRect(0, infoBarY, 1280, 26);

            this.add.text(12, infoBarY + 6, '💞 본드:', {
                fontSize: '11px', fontFamily: 'monospace', color: '#ff88cc', fontStyle: 'bold'
            });

            let chipX = 80;
            this._activeBonds.forEach(b => {
                // 티어 색
                const tierColors = ['#888888', '#88ccaa', '#88ddff', '#ffaa66', '#ff88cc', '#ffcc44'];
                const tierBadges = ['', '①', '②', '③', '④', '⑤'];
                const color = tierColors[b.tier] || '#888888';
                const badge = tierBadges[b.tier] || '';
                const chip = this.add.text(chipX, infoBarY + 5, `${badge} ${b.aName} ↔ ${b.bName} (${b.name})`, {
                    fontSize: '10px', fontFamily: 'monospace', color,
                    stroke: '#000', strokeThickness: 1
                });
                chipX += chip.width + 16;
                if (chipX > 1240) return;
            });
        }
    }

    _drawBattlefield() {
        // 아군 위치 (왼쪽, 1번이 가장 우측 = 전열)
        // 좌→우: 4 3 2 1 |  1 2 3 4 (적)
        // 그러나 직관적으로: 아군 4321 가 왼쪽, 적 1234 가 오른쪽
        // 즉 포지션 1이 가운데 가까이

        const allyXBase = 220, enemyXBase = 700;
        const slotW = 100;

        this.unitGfx = {};

        // 아군 (포지션 1이 가장 오른쪽, 적과 가까움)
        this.combat.allies.forEach(u => {
            const x = allyXBase + (4 - u.position) * slotW;  // pos 1 = x=520, pos 4 = x=220
            const y = 250;
            this._drawUnit(u, x, y, 'ally');
        });

        // 적 (포지션 1이 가장 왼쪽)
        this.combat.enemies.forEach(u => {
            const x = enemyXBase + (u.position - 1) * slotW;  // pos 1 = x=700
            const y = 250;
            this._drawUnit(u, x, y, 'enemy');
        });

        // 중앙선
        const centerLine = this.add.graphics();
        centerLine.lineStyle(1, 0x441111, 0.5);
        centerLine.lineBetween(640, 140, 640, 370);
    }

    _drawUnit(unit, x, y, team) {
        const isAlly = team === 'ally';
        const container = this.add.container(x, y);

        // 캐릭터 원형 (배경) — 축소 (42→34)
        const bodyRadius = 34;
        const bodyColor = isAlly ? this._getClassColor(unit.classKey) : (ENEMY_DATA[unit.classKey] ? ENEMY_DATA[unit.classKey].color : 0xcc4444);
        const body = this.add.graphics();
        body.fillStyle(bodyColor, 0.4);
        body.fillCircle(0, 0, bodyRadius);
        body.lineStyle(2, 0xffffff, 0.7);
        body.strokeCircle(0, 0, bodyRadius);
        container.add(body);

        // 캐릭터 — 스프라이트 있으면 사용, 없으면 이모지
        let iconText;
        if (isAlly && this._useCharSprite(unit.classKey)) {
            iconText = this.add.image(0, -4, `char_${unit.classKey}`).setOrigin(0.5);
            // 사이즈 맞춤 (직경 64 정도)
            const desired = 64;
            const scale = desired / Math.max(iconText.width, iconText.height);
            iconText.setScale(scale);
            if (unit.team === 'ally') iconText.setFlipX(false);
        } else {
            const icon = isAlly ? (CLASS_DATA[unit.classKey]?.icon || '?') : '👹';
            iconText = this.add.text(0, -4, icon, { fontSize: '26px' }).setOrigin(0.5);
        }
        container.add(iconText);

        // 포지션 번호
        const posNum = this.add.text(0, -50, `[${unit.position}]`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#88aaff', fontStyle: 'bold',
            stroke: '#000', strokeThickness: 2
        }).setOrigin(0.5);
        container.add(posNum);

        // 이름
        const nameText = this.add.text(0, 42, unit.name, {
            fontSize: '10px', fontFamily: 'monospace', color: '#ccccdd',
            stroke: '#000', strokeThickness: 2
        }).setOrigin(0.5);
        container.add(nameText);

        // HP바
        const hpBg = this.add.rectangle(0, 58, 78, 7, 0x331111);
        const hpFill = this.add.rectangle(-39, 58, 78 * (unit.hp / unit.maxHp), 7, 0x44ff44).setOrigin(0, 0.5);
        const hpText = this.add.text(0, 58, `${unit.hp}/${unit.maxHp}`, {
            fontSize: '9px', fontFamily: 'monospace', color: '#ffffff', stroke: '#000', strokeThickness: 2
        }).setOrigin(0.5);
        container.add(hpBg);
        container.add(hpFill);
        container.add(hpText);

        // 상태 효과
        const statusText = this.add.text(0, 73, '', {
            fontSize: '11px', fontFamily: 'monospace', color: '#ffaa88'
        }).setOrigin(0.5);
        container.add(statusText);

        // 턴 강조 링
        const turnRing = this.add.graphics();

        // 인터랙티브 (축소된 캐릭터에 맞춤)
        const hitZone = this.add.zone(0, 5, 76, 95).setInteractive({ useHandCursor: true });
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
        // === 하단 패널: y=440 ~ 720 (280px) ===
        this.actionPanelY = 440;
        this.actionPanelBg = this.add.graphics();
        this.actionPanelBg.fillStyle(0x0a0a14, 1);
        this.actionPanelBg.fillRect(0, 440, 1280, 280);
        this.actionPanelBg.lineStyle(2, 0x333355, 0.8);
        this.actionPanelBg.lineBetween(0, 440, 1280, 440);

        // 3 컬럼 구분선
        this.actionPanelBg.lineStyle(1, 0x222244, 0.7);
        this.actionPanelBg.lineBetween(265, 450, 265, 715);   // 좌/중 구분
        this.actionPanelBg.lineBetween(1015, 450, 1015, 715); // 중/우 구분

        // 컬럼 헤더
        this.add.text(133, 450, '── 현재 유닛 ──', {
            fontSize: '11px', fontFamily: 'monospace', color: '#8899aa'
        }).setOrigin(0.5, 0);
        this.add.text(640, 450, '── 액션 ──', {
            fontSize: '11px', fontFamily: 'monospace', color: '#8899aa'
        }).setOrigin(0.5, 0);
        this.add.text(1147, 450, '── 상태 & 소비템 ──', {
            fontSize: '11px', fontFamily: 'monospace', color: '#8899aa'
        }).setOrigin(0.5, 0);

        // 액션 타이틀 (중앙 컬럼 상단)
        this.actionTitleText = this.add.text(640, 470, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffcc88', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.actionButtons = [];
        this.unitInfoObjs = [];      // 좌측 — 라운드마다 갱신
        this.statusInfoObjs = [];    // 우측
    }

    /** 좌측 컬럼 — 현재 유닛 상세 정보 */
    _drawUnitInfoPanel(unit) {
        this.unitInfoObjs.forEach(o => o.destroy && o.destroy());
        this.unitInfoObjs = [];
        if (!unit) return;

        const cx = 133;  // 컬럼 중심
        let cy = 472;

        // 이름 + 클래스
        const base = CLASS_DATA[unit.classKey];
        const className = base ? base.name : (ENEMY_DATA[unit.classKey] ? ENEMY_DATA[unit.classKey].name : '');
        this.unitInfoObjs.push(this.add.text(cx, cy, unit.name, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffcc88', fontStyle: 'bold'
        }).setOrigin(0.5));
        cy += 18;
        this.unitInfoObjs.push(this.add.text(cx, cy, `${className} — P${unit.position}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#8899aa'
        }).setOrigin(0.5));
        cy += 20;

        // HP 바
        const hpW = 230, hpH = 12;
        const hpRatio = unit.hp / unit.maxHp;
        const hpColor = hpRatio > 0.6 ? 0x44ff88 : hpRatio > 0.3 ? 0xffaa44 : 0xff4444;
        const hpBgRect = this.add.rectangle(cx, cy + hpH/2, hpW, hpH, 0x331111);
        const hpFillRect = this.add.rectangle(cx - hpW/2, cy + hpH/2, hpW * hpRatio, hpH, hpColor).setOrigin(0, 0.5);
        const hpLabel = this.add.text(cx, cy + hpH/2, `HP ${unit.hp}/${unit.maxHp}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold',
            stroke: '#000', strokeThickness: 2
        }).setOrigin(0.5);
        this.unitInfoObjs.push(hpBgRect, hpFillRect, hpLabel);
        cy += hpH + 8;

        // 스탯 (2 컬럼)
        const stats = unit.getStats ? unit.getStats() : { atk: unit.atk, def: unit.def, moveSpeed: unit.spd };
        const atk = stats.atk || unit.atk || 0;
        const def = stats.def || unit.def || 0;
        const spd = stats.moveSpeed || unit.spd || 0;
        const crit = Math.floor((stats.critRate || unit.critRate || 0) * 100);

        const colL = cx - 55, colR = cx + 55;
        this.unitInfoObjs.push(this.add.text(colL, cy, `⚔ ATK ${atk}`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#ffaaaa'
        }).setOrigin(0.5));
        this.unitInfoObjs.push(this.add.text(colR, cy, `🛡 DEF ${def}`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#aaccff'
        }).setOrigin(0.5));
        cy += 16;
        this.unitInfoObjs.push(this.add.text(colL, cy, `⚡ SPD ${spd}`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#ffcc88'
        }).setOrigin(0.5));
        this.unitInfoObjs.push(this.add.text(colR, cy, `💥 CRIT ${crit}%`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#ffaa44'
        }).setOrigin(0.5));
        cy += 22;

        // 특성 (아군만)
        if (unit.team === 'ally' && unit.ref && unit.ref.traits) {
            this.unitInfoObjs.push(this.add.text(cx, cy, '— 특성 —', {
                fontSize: '10px', fontFamily: 'monospace', color: '#8899aa'
            }).setOrigin(0.5));
            cy += 14;
            unit.ref.traits.slice(0, 3).forEach(t => {
                const sym = t.type === 'positive' ? '✦' : t.type === 'legendary' ? '★' : '✧';
                const color = t.type === 'positive' ? '#44cc44' : t.type === 'legendary' ? '#ffaa00' : '#ff6666';
                this.unitInfoObjs.push(this.add.text(cx, cy, `${sym} ${t.name}`, {
                    fontSize: '10px', fontFamily: 'monospace', color, fontStyle: 'bold'
                }).setOrigin(0.5));
                cy += 13;
            });
            cy += 4;
        }

        // 장비 슬롯 요약 (아군만)
        if (unit.team === 'ally' && unit.ref && unit.ref.equipment) {
            const eq = unit.ref.equipment;
            const equipped = ['weapon','armor','accessory'].filter(s => eq[s]).length;
            this.unitInfoObjs.push(this.add.text(cx, cy, `🎽 장비: ${equipped}/3`, {
                fontSize: '10px', fontFamily: 'monospace',
                color: equipped === 3 ? '#88ffcc' : '#aaaaaa'
            }).setOrigin(0.5));
            cy += 14;
        }
    }

    /** 우측 컬럼 — 활성 상태이상, 쿨다운, 소비템 (placeholder) */
    _drawStatusInfoPanel(unit) {
        this.statusInfoObjs.forEach(o => o.destroy && o.destroy());
        this.statusInfoObjs = [];
        if (!unit) return;

        const cx = 1147;
        let cy = 472;

        // === 활성 상태이상 ===
        this.statusInfoObjs.push(this.add.text(cx, cy, '⚠ 상태이상', {
            fontSize: '11px', fontFamily: 'monospace', color: '#ffaa88', fontStyle: 'bold'
        }).setOrigin(0.5));
        cy += 16;

        // statusEffects는 배열 형태 — [{type, duration, value}, ...]
        const statuses = Array.isArray(unit.statusEffects) ? unit.statusEffects : [];
        const activeStatuses = statuses.filter(s => s && (s.duration === undefined || s.duration > 0));
        if (activeStatuses.length === 0) {
            this.statusInfoObjs.push(this.add.text(cx, cy, '(없음)', {
                fontSize: '10px', fontFamily: 'monospace', color: '#555566'
            }).setOrigin(0.5));
            cy += 14;
        } else {
            const statusLabels = {
                bleed: '🩸 출혈', burn: '🔥 화상', slow: '⏳ 둔화',
                taunt: '🎯 도발', buff_atk: '⚔ ATK↑', buff_def: '🛡 DEF↑',
                debuff_def: '🛡 DEF↓', stun: '💫 기절', poison: '☠ 중독'
            };
            activeStatuses.forEach(s => {
                const k = s.type || s.kind || '';
                const label = statusLabels[k] || k;
                const dur = (s.duration > 0) ? ` (${s.duration}R)` : '';
                const isBuff = (k === 'buff_atk' || k === 'buff_def' || k === 'taunt');
                const color = isBuff ? '#88ffaa' : '#ff8866';
                this.statusInfoObjs.push(this.add.text(cx, cy, `${label}${dur}`, {
                    fontSize: '10px', fontFamily: 'monospace', color
                }).setOrigin(0.5));
                cy += 13;
            });
        }
        cy += 8;

        // === 쿨다운 ===
        if (unit.cooldowns) {
            const cooldowns = Object.keys(unit.cooldowns).filter(aid => unit.cooldowns[aid] > 0);
            if (cooldowns.length > 0) {
                this.statusInfoObjs.push(this.add.text(cx, cy, '⏱ 쿨다운', {
                    fontSize: '11px', fontFamily: 'monospace', color: '#ffcc88', fontStyle: 'bold'
                }).setOrigin(0.5));
                cy += 16;
                cooldowns.forEach(aid => {
                    const act = ACTION_DATA[aid];
                    if (!act) return;
                    this.statusInfoObjs.push(this.add.text(cx, cy, `${act.name}: ${unit.cooldowns[aid]}R`, {
                        fontSize: '10px', fontFamily: 'monospace', color: '#ff8866'
                    }).setOrigin(0.5));
                    cy += 13;
                });
                cy += 6;
            }
        }

        // === 포켓 아이템 슬롯 ===
        this.statusInfoObjs.push(this.add.text(cx, cy, '🧪 포켓 아이템', {
            fontSize: '11px', fontFamily: 'monospace', color: '#88ccee', fontStyle: 'bold'
        }).setOrigin(0.5));
        cy += 16;
        const slots = this.gameState.pocketSlots || [null, null];
        const hasPocket = this.pitGauge && this.pitGauge.hasPocket;
        if (!hasPocket) {
            this.statusInfoObjs.push(this.add.text(cx, cy, '(F2 해금 필요)', {
                fontSize: '9px', fontFamily: 'monospace', color: '#555566'
            }).setOrigin(0.5));
        } else {
            for (let i = 0; i < slots.length; i++) {
                const itemKey = slots[i];
                const item = (itemKey && typeof POCKET_ITEM_DATA !== 'undefined') ? POCKET_ITEM_DATA[itemKey] : null;
                const label = item ? `${item.icon} ${item.name}` : `슬롯 ${i+1}: (비어있음)`;
                const color = item ? '#88ccee' : '#555566';
                this.statusInfoObjs.push(this.add.text(cx, cy, label, {
                    fontSize: '10px', fontFamily: 'monospace', color
                }).setOrigin(0.5));
                cy += 14;
            }
            this.statusInfoObjs.push(this.add.text(cx, cy, '(쉬는 곳에서만 사용)', {
                fontSize: '9px', fontFamily: 'monospace', color: '#555566'
            }).setOrigin(0.5));
        }
    }

    _drawTurnQueue() {
        // 우측에 턴 순서 표시 — 배경 박스 추가 (이미지 위에서도 가독성)
        const qx = 1090, qy = 50, qw = 180, qh = 220;
        const qBg = this.add.graphics();
        qBg.fillStyle(0x000000, 0.55);
        qBg.fillRoundedRect(qx, qy, qw, qh, 5);
        qBg.lineStyle(1, 0x445566, 0.6);
        qBg.strokeRoundedRect(qx, qy, qw, qh, 5);

        this.queueText = this.add.text(qx + qw - 8, qy + 6, '', {
            fontSize: '11px', fontFamily: 'monospace', color: '#cceeff',
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
            // F10 광전사 전장 — 게이지 70% 이상 시 ATK↑ DEF↓
            if (this.pitGauge) {
                const berserk = this.pitGauge.getBerserkerBonus();
                if (berserk && !current._berserkerApplied) {
                    current.atk = Math.floor(current.atk * berserk.atkMult);
                    current.def = Math.floor(current.def * berserk.defMult);
                    current._berserkerApplied = true;
                }
            }
            this._showAllyActionPanel(current);
        } else {
            // 적 턴 — 좌/우 패널을 적 정보로 갱신
            this.actionTitleText.setText(`▶ ${current.name}의 턴 (적, P${current.position})`);
            this._drawUnitInfoPanel(current);
            this._drawStatusInfoPanel(current);
            // 액션 버튼은 비움
            this.actionButtons.forEach(b => b.destroy && b.destroy());
            this.actionButtons = [];
            // 적 AI
            this.time.delayedCall(700, () => {
                const result = DarkestCombat.executeAiAction(this.combat, current);
                if (result) {
                    this._showEnemyActionLabel(current, result.actionName, result.actionIcon, result.actionId);
                    if (result.allResults) {
                        result.allResults.forEach(r => this._showActionResult(r, current));
                        this._chargePitGauge(result.allResults, false);
                    } else {
                        this._showActionResult(result, current);
                        this._chargePitGauge([result], false);
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

    _showEnemyActionLabel(enemy, actionName, icon, actionId) {
        const g = this.unitGfx[enemy.id];
        if (!g) return;
        const cx = g.container.x, cy = g.container.y - 90;

        // 아이콘 + 이름을 그룹으로 띄움
        const action = actionId ? ACTION_DATA[actionId] : null;
        const hasPng = action && typeof ActionIcons !== 'undefined' && ActionIcons.hasPng(this, action.id);

        let iconObj, labelText;
        if (hasPng) {
            // PNG 아이콘 좌측 + 이름 우측
            const labelTmp = this.add.text(0, 0, actionName, { fontSize: '13px', fontFamily: 'monospace' });
            const tw = labelTmp.width;
            labelTmp.destroy();
            const totalW = 22 + tw;
            iconObj = ActionIcons.render(this, cx - totalW/2 + 11, cy, action, 22);
            iconObj.setDepth(50);
            labelText = this.add.text(cx - totalW/2 + 24, cy, actionName, {
                fontSize: '13px', fontFamily: 'monospace', color: '#ff8866', fontStyle: 'bold',
                stroke: '#000000', strokeThickness: 3
            }).setOrigin(0, 0.5).setDepth(50);
        } else {
            // 폴백 — 이모지 + 이름 한 줄
            labelText = this.add.text(cx, cy, `${icon || '⚔'} ${actionName}`, {
                fontSize: '13px', fontFamily: 'monospace', color: '#ff8866', fontStyle: 'bold',
                stroke: '#000000', strokeThickness: 3
            }).setOrigin(0.5).setDepth(50);
        }

        const targets = iconObj ? [iconObj, labelText] : [labelText];
        this.tweens.add({
            targets, y: '-=25', alpha: 0,
            duration: 1200, onComplete: () => targets.forEach(t => t.destroy())
        });
    }

    _highlightCurrentUnit(unit) {
        Object.values(this.unitGfx).forEach(g => g.turnRing && g.turnRing.clear());
        const g = this.unitGfx[unit.id];
        if (!g) return;
        g.turnRing.clear();
        g.turnRing.lineStyle(3, 0xffcc44, 1);
        g.turnRing.strokeCircle(g.container.x, g.container.y, 36);
        // 컨테이너 살짝 펄스
        this.tweens.add({
            targets: g.container, scale: 1.1, duration: 200, yoyo: true
        });
    }

    _showAllyActionPanel(unit) {
        // 액션 버튼 4개 표시
        this.actionButtons.forEach(b => b.destroy && b.destroy());
        this.actionButtons = [];

        this.actionTitleText.setText(`▶ ${unit.name}의 턴 (포지션 ${unit.position})`);

        // 좌/우 정보 패널 갱신
        this._drawUnitInfoPanel(unit);
        this._drawStatusInfoPanel(unit);

        // 중앙 컬럼: x=275~1005 (730px) — 2×2 그리드 액션 슬롯
        const actions = (typeof getClassActions === 'function') ? getClassActions(unit.classKey) : [];
        const colW = 730, startX = 275;
        const btnW = 355, btnH = 56, gap = 10;
        // 2×2 그리드 — 자리 잡힘
        const positions = [
            { col: 0, row: 0 }, { col: 1, row: 0 },
            { col: 0, row: 1 }, { col: 1, row: 1 }
        ];

        actions.forEach((action, i) => {
            const p = positions[i] || { col: i % 2, row: Math.floor(i / 2) };
            const x = startX + p.col * (btnW + gap);
            const y = 560 + p.row * (btnH + gap);
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
            // 아이콘 (좌측 32×32)
            const iconObj = (typeof ActionIcons !== 'undefined')
                ? ActionIcons.render(this, x + 22, y + btnH/2, action, 32)
                : this.add.text(x + 10, y + btnH/2, action.icon || '', {
                    fontSize: '20px', fontFamily: 'monospace'
                }).setOrigin(0, 0.5);
            if (iconObj) {
                if (disabled) iconObj.setAlpha(0.4);
                this.actionButtons.push(iconObj);
            }

            const titleText = this.add.text(x + 44, y + 6, action.name, {
                fontSize: '13px', fontFamily: 'monospace', color: titleColor, fontStyle: 'bold'
            });
            this.actionButtons.push(titleText);

            const descColor = disabled ? '#333344' : '#888899';
            const descText = this.add.text(x + 44, y + 26, action.desc || '', {
                fontSize: '10px', fontFamily: 'monospace', color: descColor,
                wordWrap: { width: btnW - 54 }
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

        // === 액션 슬롯 위 별도 줄: 위치 이동 + 스킵 (중앙 컬럼 상단) ===
        const aliveTeam = this.combat.allies.filter(u => u.alive);
        const canMoveForward = unit.position > 1;
        const canMoveBack = unit.position < aliveTeam.length;

        const utilY = 495;  // 액션 슬롯(560) 위, 타이틀(470) 아래
        const utilW = 160, utilH = 30;
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

            // 아이콘 (좌측 18×18)
            if (typeof ActionIcons !== 'undefined') {
                const iconObj = ActionIcons.render(this, mx + 30, cy + 10, action, 18);
                if (iconObj) {
                    if (!canUse) iconObj.setAlpha(0.4);
                    iconObj.setDepth(82);
                    objs.push(iconObj);
                }
            }

            objs.push(this.add.text(mx + 46, cy, `${action.name}${cdLabel}`, {
                fontSize: '11px', fontFamily: 'monospace', color, fontStyle: 'bold'
            }).setDepth(82));
            objs.push(this.add.text(mx + 46, cy + 14, `${action.desc || ''} (P${action.casterPositions.join(',')})`, {
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

        // 결과 표시 + 핏 게이지 충전
        if (result.results) {
            result.results.forEach(r => this._showActionResult(r, caster));
            this._chargePitGauge(result.results, action.type === 'skill');
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
        const slotW = 100;
        const allyXBase = 220, enemyXBase = 700;
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
                newX = allyXBase + (4 - u.position) * slotW;
            } else {
                newX = enemyXBase + (u.position - 1) * slotW;
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

            // HP 갱신 (HP바 너비 78px)
            g.hpFill.width = 78 * Math.max(0, u.hp / u.maxHp);
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

    _updatePitGaugeUI() {
        if (!this.pitGauge || !this._pitGaugeBarFill) return;
        const pct = this.pitGauge.gauge / this.pitGauge.max;
        this._pitGaugeBarFill.width = Math.max(1, 300 * pct);
        const color = pct >= 0.66 ? 0xff4444 : pct >= 0.33 ? 0xcc6622 : 0xcc2222;
        this._pitGaugeBarFill.fillColor = color;
        this._pitGaugeText.setText(`${Math.floor(this.pitGauge.gauge)}%`);
        this._pitMultText.setText(`×${this.pitGauge.getMultiplier().toFixed(1)}`);
    }

    _chargePitGauge(results, wasSkill) {
        if (!this.pitGauge) return;
        let triggered = null;
        if (wasSkill) {
            const t = this.pitGauge.onCharge('skill_use');
            if (t) triggered = t;
        }
        const arr = Array.isArray(results) ? results : [results];
        for (const r of arr) {
            if (r.isCrit) {
                const t = this.pitGauge.onCharge('crit');
                if (t && !triggered) triggered = t;
            }
            if (r.killed) {
                const unit = [...this.combat.allies, ...this.combat.enemies].find(u => u.id === r.target);
                const type = (unit && unit.isElite) ? 'kill_elite' : (unit && unit.isBoss) ? 'kill_boss' : 'kill_normal';
                const t = this.pitGauge.onCharge(type);
                if (t && !triggered) triggered = t;
            }
            if (r.status === 'bleed') {
                const t = this.pitGauge.onCharge('bleed_apply');
                if (t && !triggered) triggered = t;
            }
            if (r.status === 'burn') {
                const t = this.pitGauge.onCharge('burn_apply');
                if (t && !triggered) triggered = t;
            }
            if (r.status && r.status !== 'bleed' && r.status !== 'burn') {
                const t = this.pitGauge.onCharge('debuff_apply');
                if (t && !triggered) triggered = t;
            }
        }
        this._updatePitGaugeUI();
        if (triggered) this._triggerCrowdRush(triggered);
    }

    _triggerCrowdRush(threshold) {
        if (!this.pitGauge) return;
        const zoneLevel = this.gameState.zoneLevel[this.zoneKey] || 1;
        const rush = this.pitGauge.decideCrowdRush(zoneLevel, this.currentRound, this._isBossRound);
        if (!rush) return;

        const isDopamine = rush.type === 'dopamine';
        const label = isDopamine ? '🎉 도파민 난입! 보너스 적!' : `⚔ 관중 난입! (${threshold}%)`;
        const color = isDopamine ? '#ffcc44' : '#ff4444';
        const announce = this.add.text(640, 200, label, {
            fontSize: '22px', fontFamily: 'monospace', color, fontStyle: 'bold',
            stroke: '#000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(70);
        this.tweens.add({ targets: announce, y: 170, alpha: 0, duration: 2000, onComplete: () => announce.destroy() });

        const scaleMult = zoneLevel === 1 ? 0.75 : 1.0 + (zoneLevel - 2) * 0.08;
        rush.enemies.forEach(re => {
            const data = (typeof ENEMY_DATA !== 'undefined') ? ENEMY_DATA[re.key] : null;
            if (!data) return;
            const aliveEnemies = this.combat.enemies.filter(u => u.alive);
            if (aliveEnemies.length >= 4) return;
            const pos = aliveEnemies.length + 1;
            const id = 'rush_' + Date.now() + '_' + Math.random().toString(36).slice(2, 6);
            const sm = scaleMult * re.scaleMult;
            const enemyActions = (typeof getEnemyActions === 'function') ? getEnemyActions(re.key) : [];
            const unit = {
                id, name: data.name, classKey: re.key, team: 'enemy', position: pos,
                hp: Math.floor(data.hp * sm), maxHp: Math.floor(data.hp * sm),
                atk: Math.floor(data.atk * sm), def: data.def, spd: data.moveSpeed || 80,
                critRate: data.critRate || 0.05, critDmg: data.critDmg || 1.5,
                alive: true, statusEffects: [], cooldowns: {},
                actions: enemyActions, isCrowdRush: true,
                isElite: !!data.isElite
            };
            this.combat.enemies.push(unit);
            this._crowdRushUnits.push(unit);
            this.combat.turnQueue.push(unit);
            const ex = 700 + (pos - 1) * 100;
            this._drawUnit(unit, ex, 250, 'enemy');
            const g = this.unitGfx[unit.id];
            if (g) {
                g.container.setAlpha(0).setScale(0.3);
                this.tweens.add({ targets: g.container, alpha: 1, scale: 1, duration: 500, ease: 'Back.easeOut' });
            }
        });
        if (rush.lootBonus > 0) this._rushLootBonus = (this._rushLootBonus || 0) + rush.lootBonus;
    }

    _showRestRoom(callback) {
        if (!this.pitGauge) { callback(); return; }
        const pg = this.pitGauge;
        const restObjs = [];

        const overlay = this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.8).setDepth(90).setInteractive();
        restObjs.push(overlay);
        const panelW = 600, panelH = 400, px = 640 - panelW/2, py = 360 - panelH/2;
        const bg = this.add.graphics().setDepth(91);
        bg.fillStyle(0x111822, 1);
        bg.fillRoundedRect(px, py, panelW, panelH, 8);
        bg.lineStyle(2, 0x446688, 0.8);
        bg.strokeRoundedRect(px, py, panelW, panelH, 8);
        restObjs.push(bg);

        restObjs.push(this.add.text(640, py + 24, '🏚 쉬는 곳 — 투기장 대기실', {
            fontSize: '18px', fontFamily: 'monospace', color: '#88ccff', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(92));

        let infoY = py + 60;
        const deadAllies = this.combat.allies.filter(u => !u.alive);
        if (deadAllies.length > 0) {
            const revived = deadAllies[0];
            revived.alive = true;
            revived.hp = Math.max(1, Math.floor(revived.maxHp * pg.restReviveHpPct));
            restObjs.push(this.add.text(640, infoY, `✨ ${revived.name} 부활! (HP ${revived.hp})`, {
                fontSize: '14px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(92));
            infoY += 24;
        }
        this.combat.allies.filter(u => u.alive).forEach(u => {
            const heal = Math.floor(u.maxHp * pg.restHealPct);
            u.hp = Math.min(u.maxHp, u.hp + heal);
            restObjs.push(this.add.text(640, infoY, `💚 ${u.name} HP +${heal} (→ ${u.hp}/${u.maxHp})`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#88ffaa'
            }).setOrigin(0.5).setDepth(92));
            infoY += 18;
        });
        infoY += 16;

        if (pg.hasPocket && typeof POCKET_ITEM_DATA !== 'undefined') {
            restObjs.push(this.add.text(640, infoY, '🧪 포켓 아이템 (쉬는 곳에서만 사용 가능)', {
                fontSize: '13px', fontFamily: 'monospace', color: '#88ccee', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(92));
            infoY += 24;
            const slots = this.gameState.pocketSlots || [null, null];
            for (let i = 0; i < slots.length; i++) {
                const slotX = 640 - 140 + i * 280;
                const itemKey = slots[i];
                const item = itemKey ? POCKET_ITEM_DATA[itemKey] : null;
                const slotBg = this.add.graphics().setDepth(92);
                slotBg.fillStyle(item ? 0x223344 : 0x181828, 1);
                slotBg.fillRoundedRect(slotX - 120, infoY, 240, 50, 5);
                slotBg.lineStyle(1, item ? 0x4488aa : 0x333344, 0.8);
                slotBg.strokeRoundedRect(slotX - 120, infoY, 240, 50, 5);
                restObjs.push(slotBg);
                if (item) {
                    restObjs.push(this.add.text(slotX - 100, infoY + 6, `${item.icon} ${item.name}`, {
                        fontSize: '12px', fontFamily: 'monospace', color: '#ffcc88', fontStyle: 'bold'
                    }).setDepth(93));
                    restObjs.push(this.add.text(slotX - 100, infoY + 24, item.desc, {
                        fontSize: '10px', fontFamily: 'monospace', color: '#aabbcc'
                    }).setDepth(93));
                    const useBtn = UIButton.create(this, slotX + 80, infoY + 25, 55, 24, '사용', {
                        color: 0x336644, hoverColor: 0x448855, textColor: '#88ffaa', fontSize: 11, depth: 93,
                        onClick: () => {
                            this._usePocketItem(i, itemKey, restObjs);
                        }
                    });
                    restObjs.push(useBtn);
                } else {
                    restObjs.push(this.add.text(slotX, infoY + 25, '(비어있음)', {
                        fontSize: '11px', fontFamily: 'monospace', color: '#555566'
                    }).setOrigin(0.5).setDepth(93));
                }
            }
            infoY += 60;
        }

        restObjs.push(UIButton.create(this, 640 - 75, py + panelH - 50, 150, 36, '▶ 다음 라운드로', {
            color: 0x224488, hoverColor: 0x3366aa, textColor: '#aaccff', fontSize: 14, depth: 93,
            onClick: () => {
                restObjs.forEach(o => o.destroy && o.destroy());
                this._refreshAllUnits();
                callback();
            }
        }));
    }

    _usePocketItem(slotIndex, itemKey, restObjs) {
        const item = (typeof POCKET_ITEM_DATA !== 'undefined') ? POCKET_ITEM_DATA[itemKey] : null;
        if (!item) return;
        const fx = item.effect;
        const allies = this.combat.allies.filter(u => u.alive);

        if (fx.healPct) {
            if (item.targetType === 'single_ally') {
                const target = allies.reduce((a, b) => (a.hp / a.maxHp < b.hp / b.maxHp) ? a : b);
                const heal = Math.floor(target.maxHp * fx.healPct);
                target.hp = Math.min(target.maxHp, target.hp + heal);
                UIToast.show(this, `${item.icon} ${target.name} HP +${heal}`, { color: '#44ff88' });
            } else {
                allies.forEach(u => {
                    const heal = Math.floor(u.maxHp * fx.healPct);
                    u.hp = Math.min(u.maxHp, u.hp + heal);
                });
                UIToast.show(this, `${item.icon} 전원 HP 회복!`, { color: '#44ff88' });
            }
        }
        if (fx.revivePct) {
            const dead = this.combat.allies.filter(u => !u.alive);
            if (dead.length > 0) {
                const target = dead[0];
                target.alive = true;
                target.hp = Math.floor(target.maxHp * fx.revivePct);
                UIToast.show(this, `${item.icon} ${target.name} 부활! (HP ${target.hp})`, { color: '#ffcc44' });
            } else {
                UIToast.show(this, '부활 대상 없음', { color: '#ff6644' });
                return;
            }
        }
        if (fx.purify) {
            allies.forEach(u => {
                u.statusEffects = u.statusEffects.filter(e => e.type.startsWith('buff_'));
            });
            UIToast.show(this, `${item.icon} 상태이상 정화!`, { color: '#88ccff' });
        }
        if (fx.atkBuff) {
            allies.forEach(u => {
                u.statusEffects.push({ type: 'buff_atk', duration: fx.buffDuration || 1, value: fx.atkBuff });
            });
            UIToast.show(this, `${item.icon} 전원 ATK 버프!`, { color: '#ffaa44' });
        }
        if (fx.pitGaugeAdd && this.pitGauge) {
            this.pitGauge.gauge = Math.min(this.pitGauge.max, this.pitGauge.gauge + fx.pitGaugeAdd);
            this._updatePitGaugeUI();
            UIToast.show(this, `${item.icon} 핏 게이지 +${fx.pitGaugeAdd}%!`, { color: '#ff4444' });
        }

        this.gameState.pocketSlots[slotIndex] = null;
        this._refreshAllUnits();
    }

    _endRound() {
        // 관중 난입 결과 정산
        if (this.pitGauge && this._crowdRushUnits.length > 0) {
            const killed = this._crowdRushUnits.filter(u => !u.alive).length;
            const missed = this._crowdRushUnits.filter(u => u.alive).length;
            if (killed > 0) this.pitGauge.onRushKill(killed);
            if (missed > 0) this.pitGauge.onRushMiss(missed);
            this._crowdRushUnits = [];
            this._updatePitGaugeUI();
        }

        // 라운드 클리어 충전
        if (this.pitGauge) {
            this.pitGauge.onCharge('round_clear');
            this.pitGauge.resetThresholdsForNewRound();
            this._updatePitGaugeUI();
        }

        this.currentRound++;
        if (this.currentRound > this.maxRounds) {
            this._retreated = true;
            UIToast.show(this, `${this.maxRounds}라운드 초과 — 자동 후퇴`, { color: '#ff8866' });
            this._endBattle(false);
            return;
        }

        const startNextRound = () => {
            this.headerText.setText(`Round ${this.currentRound}  |  Blood Pit Lv.${this.gameState.zoneLevel[this.zoneKey]}`);
            DarkestCombat.startRound(this.combat);
            this._processNextTurn();
        };

        // 쉬는 곳 체크
        if (this.pitGauge) {
            const zoneLevel = this.gameState.zoneLevel[this.zoneKey] || 1;
            if (this.pitGauge.shouldShowRestRoom(zoneLevel)) {
                this._showRestRoom(startNextRound);
                return;
            }
        }
        startNextRound();
    }

    _endBattle(success) {
        if (this.battleEnded) return;
        this.battleEnded = true;

        const gs = this.gameState;
        const zone = ZONE_DATA[this.zoneKey];
        const zoneLevel = gs.zoneLevel[this.zoneKey];

        // 보상 계산 (핏 게이지 배율 적용)
        if (success) {
            const pitMult = this.pitGauge ? this.pitGauge.getMultiplier() : 1.0;
            const lootMult = 1 + (this._rushLootBonus || 0);
            this.totalGold = Math.floor((zone.baseGoldReward + zoneLevel * 15) * pitMult * lootMult * this.currentRound);
            this.totalXp = Math.floor((zone.baseXpReward + zoneLevel * 8) * pitMult);
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
            rounds: this.currentRound,
            goldEarned: this.totalGold,
            xpEarned: this.totalXp,
            casualties, survivors: survivors.map(u => u.ref),
            loot: this.loot,
            events: [success ? '전투 승리!' : (this._retreated ? '후퇴' : '전투 실패...')],
            zoneLevelUp: success,  // 적 전멸 = 레벨 업
            retreated: !!this._retreated
        };
        // 마을 진입 시 이벤트 차단 플래그
        if (this._retreated) gs._suppressNextTownEvent = true;

        this.scene.start('RunResultScene', { gameState: gs, result });
    }
}
