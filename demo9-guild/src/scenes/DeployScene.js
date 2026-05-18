class DeployScene extends Phaser.Scene {
    constructor() { super('DeployScene'); }

    preload() {
        if (typeof ActionIcons !== 'undefined') ActionIcons.preload(this);
    }

    init(data) {
        this.gameState = data.gameState;
        this.selectedZone = data.selectedZone || null;
        this.deployedIds = data.deployedIds || [];
        this.deployMode = data.deployMode || 'main';
        this.selectedLevel = data.selectedLevel || null;
    }

    create() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        this.add.rectangle(640, 360, 1280, 720, T.bg || 0x1a1510);
        const gs = this.gameState;

        // 구역 미선택 시 첫 번째 해금된 구역 자동 선택
        if (!this.selectedZone) {
            this.selectedZone = ZONE_KEYS.find(k => gs.zoneLevel[k] > 0) || ZONE_KEYS[0];
        }
        this._drawZoneDetail();
    }

    // ═══════════════════════════════════════════════
    //  구역 상세 — 구역탭 + 레벨 선택 + 편성 + 출발
    // ═══════════════════════════════════════════════

    _drawZoneDetail() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const zoneKey = this.selectedZone;
        const zone = ZONE_DATA[zoneKey];
        const zLv = gs.zoneLevel[zoneKey];

        // ── 헤더 장식 ──
        const headerBg = this.add.graphics();
        headerBg.fillStyle(T.headerBg || 0x1e1810, 1);
        headerBg.fillRect(0, 0, 1280, T.headerHeight || 55);
        headerBg.lineStyle(1, T.ornament || 0x8a7a4a, T.ornamentAlpha || 0.4);
        headerBg.lineBetween(0, (T.headerHeight || 55), 1280, (T.headerHeight || 55));

        UIButton.create(this, 60, 25, 80, 30, '← 마을', {
            variant: 'ghost',
            fontSize: (T.fontSize && T.fontSize.body) || 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        // ── 구역 탭 (헤더 중앙) ──
        let tabX = 320;
        ZONE_KEYS.forEach(key => {
            const z = ZONE_DATA[key];
            const isLocked = gs.zoneLevel[key] === 0;
            const isCurrent = key === zoneKey;
            const label = `${z.icon} ${z.name}`;

            if (isLocked) {
                this.add.text(tabX, 16, `🔒 ${z.name}`, {
                    fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
                }).setOrigin(0.5);
            } else {
                UIButton.create(this, tabX, 16, 130, 26, label, {
                    variant: isCurrent ? 'primary' : 'ghost',
                    fontSize: 11,
                    onClick: () => {
                        if (!isCurrent) this.scene.restart({ gameState: gs, selectedZone: key, deployedIds: [], deployMode: this.deployMode });
                    }
                });
            }
            // 구역 레벨 표시
            if (!isLocked) {
                this.add.text(tabX, 34, `Lv.${gs.zoneLevel[key]}`, {
                    fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: isCurrent ? z.textColor : (T.textMuted || '#887860')
                }).setOrigin(0.5);
            }
            tabX += 160;
        });

        // 메인/서브 모드
        const isMain = this.deployMode === 'main';
        UIButton.create(this, 970, 16, 130, 26, '⚔ 메인 도전', {
            variant: isMain ? 'primary' : 'ghost',
            fontSize: 11,
            onClick: () => { if (!isMain) this.scene.restart({ gameState: gs, selectedZone: zoneKey, deployedIds: this.deployedIds, deployMode: 'main', selectedLevel: this.selectedLevel }); }
        });
        UIButton.create(this, 1120, 16, 130, 26, '📦 서브 파견', {
            variant: !isMain ? 'primary' : 'ghost',
            fontSize: 11,
            onClick: () => { if (isMain) this.scene.restart({ gameState: gs, selectedZone: zoneKey, deployedIds: this.deployedIds, deployMode: 'sub', selectedLevel: this.selectedLevel }); }
        });

        const modeDesc = isMain
            ? '메인 도전: 직접 전투. 스킬 발동 가능, 보상 1.5배, 구역 레벨업'
            : '서브 파견: 시간 경과형 자동. 일반공격만, 깬 레벨까지만 파밍';
        this.add.text(1045, 38, modeDesc, {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: isMain ? (T.textAccent || '#cc8833') : (T.textBlue || '#6699cc')
        }).setOrigin(0.5);

        // 좌: 레벨 선택 + 파견 현황 | 우: 파티 편성
        this._drawLevelSelect(20, 70, 400, 620);
        this._drawPartyPanel(440, 70, 820, 620);
    }

    _drawLevelSelect(x, y, w, h) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const zoneKey = this.selectedZone;
        const zone = ZONE_DATA[zoneKey];
        const zLv = gs.zoneLevel[zoneKey];
        const isMain = this.deployMode === 'main';

        UIPanel.create(this, x, y, w, h, { title: '레벨 선택' });

        let cy = y + 30;

        if (isMain) {
            // 메인: 현재 구역 레벨로 도전
            this.add.text(x + 15, cy, `현재 도전 레벨: Lv.${zLv}`, {
                fontSize: '13px', fontFamily: T.fontFamily || 'monospace', color: T.textGold || '#ffcc44', fontStyle: 'bold'
            });
            cy += 20;
            const rounds = getMaxRounds(zLv);
            this.add.text(x + 15, cy, `${rounds}라운드 | 보스 포함`, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textSecondary || '#b8a888'
            });
            cy += 16;
            this.add.text(x + 15, cy, `승리 시 → Lv.${zLv + 1} 해금`, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textSuccess || '#66bb55'
            });
            cy += 30;
            this.selectedLevel = zLv;
        } else {
            // 서브: 해금된 레벨 중 선택
            const maxUnlocked = GuildManager.getMaxUnlockedSubLevel(gs, zoneKey);
            if (maxUnlocked === 0) {
                const clears = GuildManager.getZoneClearCount(gs, zoneKey, zLv);
                this.add.text(x + 15, cy, `🔓 서브 미해금`, {
                    fontSize: '13px', fontFamily: T.fontFamily || 'monospace', color: T.textDanger || '#cc4422', fontStyle: 'bold'
                });
                cy += 18;
                this.add.text(x + 15, cy, `메인 ${clears}/${GuildManager.SUB_UNLOCK_CLEARS}회 클리어 필요`, {
                    fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textSecondary || '#b8a888'
                });
                cy += 30;
            } else {
                this.add.text(x + 15, cy, `서브 가능 레벨 (1 ~ ${maxUnlocked})`, {
                    fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: T.textBlue || '#6699cc'
                });
                cy += 22;

                const selected = this.selectedLevel || maxUnlocked;
                for (let lv = 1; lv <= maxUnlocked; lv++) {
                    const isSel = lv === selected;
                    UIButton.create(this, x + 15 + (lv - 1) * 50, cy + 12, 44, 24, `Lv.${lv}`, {
                        variant: isSel ? 'primary' : 'ghost',
                        fontSize: 10,
                        onClick: () => {
                            this.selectedLevel = lv;
                            this.scene.restart({ gameState: gs, selectedZone: zoneKey, deployedIds: this.deployedIds, deployMode: 'sub', selectedLevel: lv });
                        }
                    });
                }
                this.selectedLevel = selected;
                cy += 36;
            }
        }

        // === 이 구역 파견 현황 ===
        cy += 10;
        this.add.text(x + 15, cy, '── 파견 현황 ──', {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textAccent || '#cc8833', fontStyle: 'bold'
        });
        cy += 20;

        const activeExps = (gs.activeExpeditions || []).filter(e => e.zoneKey === zoneKey);
        const pendingResults = (gs.pendingResults || []).filter(r => r.zoneKey === zoneKey);

        if (activeExps.length === 0 && pendingResults.length === 0) {
            this.add.text(x + 15, cy, '진행 중인 파견 없음', {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
            });
            cy += 18;
        }

        activeExps.forEach(exp => {
            const progress = ExpeditionManager.getProgress(exp);
            const remainMs = ExpeditionManager.getRemainingMs(exp);
            const remainSec = Math.ceil(remainMs / 1000);
            const remainStr = remainSec > 60 ? `${Math.floor(remainSec / 60)}분 ${remainSec % 60}초` : `${remainSec}초`;
            const elapsed = Date.now() - exp.startedAtMs;
            const elapsedStr = elapsed > 60000 ? `${Math.floor(elapsed / 60000)}분 경과` : `${Math.floor(elapsed / 1000)}초 경과`;

            const expBg = this.add.graphics();
            expBg.fillStyle(T.panelFill || 0x2a2218, 1);
            expBg.fillRoundedRect(x + 10, cy, w - 20, 75, 3);

            this.add.text(x + 18, cy + 5, `⏳ Lv.${exp.zoneLevel} 서브 파견`, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textBlue || '#6699cc', fontStyle: 'bold'
            });
            this.add.text(x + w - 18, cy + 5, remainStr, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textAccent || '#cc8833'
            }).setOrigin(1, 0);

            // 진행률 바
            const barW = w - 40;
            this.add.graphics().fillStyle(T.panelFill || 0x2a2218, 1).fillRect(x + 18, cy + 24, barW, 6);
            this.add.graphics().fillStyle(T.panelStroke || 0x5a4a2a, 1).fillRect(x + 18, cy + 24, barW * progress, 6);

            this.add.text(x + 18, cy + 36, elapsedStr, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
            });

            // 파견 용병
            const names = exp.partyIds.map(id => {
                const m = gs.roster.find(r => r.id === id);
                return m ? `${m.getBaseClass().icon}${m.name}` : '?';
            }).join('  ');
            this.add.text(x + 18, cy + 52, names, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textSecondary || '#b8a888'
            });
            cy += 80;
        });

        pendingResults.forEach(result => {
            const resBg = this.add.graphics();
            resBg.fillStyle(result.success ? 0x1a2a1a : 0x2a1a1a, 1);
            resBg.fillRoundedRect(x + 10, cy, w - 20, 44, 3);

            const icon = result.success ? '✅' : '⚠';
            this.add.text(x + 18, cy + 5, `${icon} Lv.${result.zoneLevel} ${result.success ? '성공' : '실패'}`, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: result.success ? (T.textSuccess || '#66bb55') : (T.textDanger || '#cc4422')
            });
            this.add.text(x + 18, cy + 22, `+${result.goldEarned}G | 장비 ${result.loot.length}개`, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textSecondary || '#b8a888'
            });
            UIButton.create(this, x + w - 55, cy + 22, 70, 24, '수령', {
                variant: 'primary',
                fontSize: 10,
                onClick: () => {
                    ExpeditionManager.collectResult(gs, result.id);
                    SaveManager.save(gs);
                    this.scene.restart({ gameState: gs, selectedZone: zoneKey, deployedIds: this.deployedIds, deployMode: this.deployMode });
                }
            });
            cy += 50;
        });
    }

    _drawPartyPanel(x, y, w, h) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const isMain = this.deployMode === 'main';
        const maxDeploy = GuildManager.getMaxDeploy(gs);

        UIPanel.create(this, x, y, w, h, { title: `파티 편성 (${this.deployedIds.length}/${maxDeploy})` });

        // 편성 슬롯
        this._drawDeploySlots(x + 10, y + 30, maxDeploy, w - 20);

        // 대기 로스터
        this._drawRosterPick(x + 10, y + 230, w - 20);

        // 편성 저장/불러오기
        this._drawSavedParties(x + 10, y + 430, w - 20);

        // 출발 버튼
        this._drawDepartButton(x, y, w, h);
    }

    _drawDeploySlots(x, y, maxDeploy, panelW) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const slotW = Math.min(190, (panelW - (maxDeploy - 1) * 8) / maxDeploy);

        this.add.text(x, y - 2, '← 후열(원거리)          전열(근접) →', {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860', fontStyle: 'italic'
        });

        for (let i = 0; i < maxDeploy; i++) {
            const sx = x + i * (slotW + 8);
            const merc = gs.roster.find(m => m.id === this.deployedIds[i]);
            const position = maxDeploy - i;

            const bg = this.add.graphics();
            bg.fillStyle(merc ? (T.cardSelected || 0x4a3a20) : (T.panelFill || 0x2a2218), 1);
            bg.fillRoundedRect(sx, y + 14, slotW, 170, 4);
            bg.lineStyle(1, merc ? (T.panelStroke || 0x5a4a2a) : (T.cardStroke || 0x4a3c28), 0.6);
            bg.strokeRoundedRect(sx, y + 14, slotW, 170, 4);

            const posColor = position <= 2 ? (T.textDanger || '#cc4422') : (T.textBlue || '#6699cc');
            const posLabel = position <= 2 ? `전열[${position}]` : `후열[${position}]`;
            this.add.text(sx + 5, y + 18, posLabel, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: posColor, fontStyle: 'bold'
            });

            if (merc) {
                const base = merc.getBaseClass();
                const rarity = RARITY_DATA[merc.rarity];
                const stats = merc.getStats();

                this.add.text(sx + slotW - 5, y + 18, `Lv.${merc.level}`, {
                    fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textSecondary || '#b8a888'
                }).setOrigin(1, 0);
                this.add.text(sx + 5, y + 32, `${base.icon} ${merc.name}`, {
                    fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: rarity.textColor, fontStyle: 'bold'
                });
                this.add.text(sx + 5, y + 48, `HP:${merc.currentHp}/${stats.hp}`, {
                    fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textSecondary || '#b8a888'
                });
                this.add.text(sx + 5, y + 60, `ATK:${stats.atk} DEF:${stats.def}`, {
                    fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textSecondary || '#b8a888'
                });

                // 액션 미리보기
                if (typeof getClassActions === 'function') {
                    const actions = getClassActions(merc.classKey);
                    let ay = y + 76;
                    actions.forEach(action => {
                        if (ay > y + 155) return;
                        const canUse = action.casterPositions.includes(position);
                        const color = canUse ? (T.textSuccess || '#66bb55') : (T.textDanger || '#cc4422');
                        this.add.text(sx + 5, ay, `${canUse ? '✓' : '✗'} ${action.name}`, {
                            fontSize: '8px', fontFamily: T.fontFamily || 'monospace', color
                        });
                        ay += 11;
                    });
                }

                // 이동/해제
                if (i > 0) {
                    UIButton.create(this, sx + 20, y + 168, 30, 18, '◀', {
                        variant: 'ghost',
                        fontSize: 10,
                        onClick: () => this._swapDeployed(i, i - 1)
                    });
                }
                if (i < maxDeploy - 1 && this.deployedIds[i + 1]) {
                    UIButton.create(this, sx + 55, y + 168, 30, 18, '▶', {
                        variant: 'ghost',
                        fontSize: 10,
                        onClick: () => this._swapDeployed(i, i + 1)
                    });
                }
                UIButton.create(this, sx + slotW - 30, y + 168, 50, 18, '해제', {
                    variant: 'ghost',
                    fontSize: 9,
                    onClick: () => {
                        this.deployedIds = this.deployedIds.filter(id => id !== merc.id);
                        this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: this.deployMode, selectedLevel: this.selectedLevel });
                    }
                });
            } else {
                this.add.text(sx + slotW / 2, y + 95, '(빈 슬롯)', {
                    fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
                }).setOrigin(0.5);
            }
        }
    }

    _swapDeployed(idx1, idx2) {
        const gs = this.gameState;
        const arr = this.deployedIds.slice();
        const tmp = arr[idx1];
        arr[idx1] = arr[idx2];
        arr[idx2] = tmp;
        this.deployedIds = arr.filter(id => id);
        this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: this.deployMode, selectedLevel: this.selectedLevel });
    }

    _drawRosterPick(x, y, panelW) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const available = gs.roster.filter(m =>
            m.alive &&
            !this.deployedIds.includes(m.id) &&
            !ExpeditionManager.isOnExpedition(gs, m.id)
        );
        const onExpedition = gs.roster.filter(m =>
            m.alive && ExpeditionManager.isOnExpedition(gs, m.id)
        );

        this.add.text(x, y, '대기 용병 (클릭하여 편성)', {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
        });

        if (available.length === 0 && onExpedition.length === 0) {
            this.add.text(x + 100, y + 30, '편성 가능한 용병 없음', {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
            });
            return;
        }

        const maxDeploy = GuildManager.getMaxDeploy(gs);
        let cx = x, cy = y + 18;
        const cardW = 135, cardH = 48;

        available.forEach(merc => {
            if (cx + cardW > x + panelW) { cx = x; cy += cardH + 4; }
            if (cy > y + 165) return;

            const base = merc.getBaseClass();
            const rarity = RARITY_DATA[merc.rarity];
            const canAdd = this.deployedIds.length < maxDeploy;

            const bg = this.add.graphics();
            bg.fillStyle(T.cardFill || 0x231e14, 1);
            bg.fillRoundedRect(cx, cy, cardW, cardH, 3);
            bg.lineStyle(1, rarity.color, 0.3);
            bg.strokeRoundedRect(cx, cy, cardW, cardH, 3);

            this.add.text(cx + 5, cy + 4, `${base.icon} ${merc.name}`, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: rarity.textColor
            });
            this.add.text(cx + 5, cy + 18, `Lv.${merc.level} ${base.name}`, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
            });
            this.add.text(cx + 5, cy + 32, `HP:${merc.currentHp}/${merc.getStats().hp}`, {
                fontSize: '8px', fontFamily: T.fontFamily || 'monospace', color: T.textSecondary || '#b8a888'
            });

            if (canAdd) {
                const hit = this.add.zone(cx + cardW / 2, cy + cardH / 2, cardW, cardH).setInteractive({ useHandCursor: true });
                hit.on('pointerdown', () => {
                    this.deployedIds.push(merc.id);
                    this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: this.deployMode, selectedLevel: this.selectedLevel });
                });
            }
            cx += cardW + 6;
        });

        // 파견 중인 용병 표시
        if (onExpedition.length > 0) {
            cy += cardH + 10;
            if (cy > y + 165) return;
            this.add.text(x, cy, `파견 중 (${onExpedition.length}명)`, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
            });
            cy += 14;
            cx = x;
            onExpedition.forEach(merc => {
                if (cx + cardW > x + panelW) { cx = x; cy += 22; }
                if (cy > y + 185) return;
                const base = merc.getBaseClass();
                const exp = (gs.activeExpeditions || []).find(e => e.partyIds.includes(merc.id));
                const zoneName = exp ? ZONE_DATA[exp.zoneKey].name : '?';
                this.add.text(cx, cy, `${base.icon}${merc.name} → ${zoneName}`, {
                    fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
                });
                cx += cardW + 6;
            });
        }
    }

    _drawSavedParties(x, y, panelW) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        if (!gs.savedParties) gs.savedParties = [];

        this.add.text(x, y, '저장 편성 (클릭=불러오기 / 우클릭=저장)', {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
        });

        const SLOT_COUNT = 4;
        const slotW = Math.min(180, (panelW - (SLOT_COUNT - 1) * 6) / SLOT_COUNT);
        for (let i = 0; i < SLOT_COUNT; i++) {
            const sx = x + i * (slotW + 6);
            const saved = gs.savedParties[i];
            const hasSaved = !!saved;

            const bg = this.add.graphics();
            bg.fillStyle(hasSaved ? (T.cardSelected || 0x4a3a20) : (T.cardFill || 0x231e14), 1);
            bg.fillRoundedRect(sx, y + 15, slotW, 38, 3);
            bg.lineStyle(1, hasSaved ? (T.panelStroke || 0x5a4a2a) : (T.cardStroke || 0x4a3c28), 0.5);
            bg.strokeRoundedRect(sx, y + 15, slotW, 38, 3);

            if (hasSaved) {
                const aliveCount = saved.mercIds.filter(id => gs.roster.find(m => m.id === id && m.alive)).length;
                this.add.text(sx + 5, y + 19, saved.name || `편성 ${i + 1}`, {
                    fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textBlue || '#6699cc', fontStyle: 'bold'
                });
                this.add.text(sx + 5, y + 34, `${aliveCount}/${saved.mercIds.length}명`, {
                    fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: aliveCount === saved.mercIds.length ? (T.textSuccess || '#66bb55') : (T.textDanger || '#cc4422')
                });
            } else {
                this.add.text(sx + slotW / 2, y + 33, `슬롯 ${i + 1}`, {
                    fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
                }).setOrigin(0.5);
            }

            const hit = this.add.zone(sx + slotW / 2, y + 15 + 19, slotW, 38).setInteractive();
            hit.on('pointerdown', (pointer) => {
                const isRight = (pointer.event && pointer.event.button === 2);
                if (isRight) this._savePartySlot(i);
                else if (hasSaved) this._loadPartySlot(i);
                else this._savePartySlot(i);
            });
            this.input.mouse.disableContextMenu();
        }
    }

    _savePartySlot(idx) {
        const gs = this.gameState;
        if (this.deployedIds.length === 0) {
            UIToast.show(this, '편성된 용병이 없습니다', { color: '#ff6644' });
            return;
        }
        if (!gs.savedParties) gs.savedParties = [];
        gs.savedParties[idx] = { name: `편성 ${idx + 1}`, mercIds: this.deployedIds.slice() };
        SaveManager.save(gs);
        UIToast.show(this, `슬롯 ${idx + 1} 저장됨`, { color: '#88ccff' });
        this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: this.deployMode, selectedLevel: this.selectedLevel });
    }

    _loadPartySlot(idx) {
        const gs = this.gameState;
        const saved = gs.savedParties[idx];
        if (!saved) return;
        const validIds = saved.mercIds.filter(id => {
            const m = gs.roster.find(r => r.id === id);
            return m && m.alive && !ExpeditionManager.isOnExpedition(gs, id);
        });
        if (validIds.length === 0) {
            UIToast.show(this, '저장된 용병들 출전 불가', { color: '#ff6644' });
            return;
        }
        const maxDeploy = GuildManager.getMaxDeploy(gs);
        this.deployedIds = validIds.slice(0, maxDeploy);
        this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: this.deployMode, selectedLevel: this.selectedLevel });
    }

    _drawPocketSlots(panelX, panelY, panelW) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const fLevel = (gs.guildHall && gs.guildHall.pit_control) || 0;
        if (fLevel < 2) return 0;   // F2 미해금
        if (typeof POCKET_ITEM_DATA === 'undefined' || typeof POCKET_ITEM_SHOP === 'undefined') return 0;

        if (!gs.pocketSlots) gs.pocketSlots = [null, null];
        const slots = gs.pocketSlots;

        const sx = panelX + 10;
        let cy = panelY;

        this.add.text(sx, cy, '🧪 포켓 아이템 (BP 쉬는 곳 전용)', {
            fontSize: '12px', fontFamily: T.fontFamily || 'monospace',
            color: T.textHighlight || '#ffcc66', fontStyle: 'bold'
        });
        cy += 20;

        // 현재 장착 슬롯
        for (let i = 0; i < slots.length; i++) {
            const itemKey = slots[i];
            const item = itemKey ? POCKET_ITEM_DATA[itemKey] : null;
            const label = item ? `${item.icon} ${item.name}` : `슬롯 ${i+1}: (비어있음)`;
            const color = item ? '#88ccee' : '#665544';
            this.add.text(sx, cy, label, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color
            });
            if (item) {
                UIButton.create(this, sx + panelW - 70, cy + 2, 50, 18, '해제', {
                    color: 0x553333, hoverColor: 0x664444, textColor: '#ffaaaa', fontSize: 9,
                    onClick: () => {
                        gs.pocketSlots[i] = null;
                        gs.gold += Math.floor((POCKET_ITEM_SHOP[itemKey]?.cost || 0) * 0.5);
                        SaveManager.save(gs);
                        this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: this.deployMode, selectedLevel: this.selectedLevel });
                    }
                });
            }
            cy += 20;
        }
        cy += 6;

        // 구매 목록
        const emptySlotIdx = slots.indexOf(null);
        Object.entries(POCKET_ITEM_SHOP).forEach(([key, shop]) => {
            const item = POCKET_ITEM_DATA[key];
            if (!item) return;
            const canBuy = emptySlotIdx >= 0 && gs.gold >= shop.cost;
            const label = `${item.icon} ${item.name} — ${shop.cost}G`;
            const color = canBuy ? '#aaccaa' : '#665544';
            this.add.text(sx + 4, cy, label, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color
            });
            if (canBuy) {
                UIButton.create(this, sx + panelW - 70, cy + 2, 50, 18, '구매', {
                    color: 0x335533, hoverColor: 0x446644, textColor: '#88ffaa', fontSize: 9,
                    onClick: () => {
                        const slot = gs.pocketSlots.indexOf(null);
                        if (slot < 0 || gs.gold < shop.cost) return;
                        gs.gold -= shop.cost;
                        gs.pocketSlots[slot] = key;
                        SaveManager.save(gs);
                        this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: this.deployMode, selectedLevel: this.selectedLevel });
                    }
                });
            }
            cy += 18;
        });

        return cy - panelY; // 사용한 높이 반환
    }

    _drawDepartButton(panelX, panelY, panelW, panelH) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const zoneKey = this.selectedZone;
        const isMain = this.deployMode === 'main';
        const activeExp = (gs.activeExpeditions || []).length;
        const maxSlots = ExpeditionManager.getMaxSlots(gs);
        const slotsFull = !isMain && activeExp >= maxSlots;

        let canDepart = this.deployedIds.length > 0 && !slotsFull;
        if (canDepart && !isMain) {
            const maxUnlocked = GuildManager.getMaxUnlockedSubLevel(gs, zoneKey);
            if (maxUnlocked === 0) canDepart = false;
        }

        // BP 메인 전투 시 포켓 아이템 상점 표시
        if (isMain && zoneKey === 'bloodpit') {
            this._drawPocketSlots(panelX, panelY + panelH - 230, panelW - 20);
        }

        const btnLabel = isMain ? '⚔ 출발 (메인 전투)' : '📦 파견 시작';
        const btnX = panelX + panelW / 2;
        const btnY = panelY + panelH - 25;

        UIButton.create(this, btnX, btnY, 240, 36, btnLabel, {
            variant: isMain ? 'danger' : 'info',
            fontSize: 14,
            disabled: !canDepart,
            onClick: () => {
                if (!canDepart) return;
                const party = this.deployedIds.map(id => gs.roster.find(m => m.id === id)).filter(Boolean);

                if (isMain) {
                    gs.runCount++;
                    SaveManager.save(gs);
                    const sceneMap = { bloodpit: 'ManualBattleScene', cargo: 'CargoBattleScene', blackout: 'BlackoutProtoSelectScene' };
                    this.scene.start(sceneMap[zoneKey] || 'BattleScene', { gameState: gs, zoneKey, party });
                } else {
                    const level = this.selectedLevel || GuildManager.getMaxUnlockedSubLevel(gs, zoneKey);
                    const exp = ExpeditionManager.dispatch(gs, zoneKey, party, level);
                    if (!exp) {
                        UIToast.show(this, '파견 실패 (슬롯/해금 확인)', { color: '#ff6644' });
                        return;
                    }
                    const mins = Math.ceil(exp.durationMs / 60000);
                    UIToast.show(this, `${ZONE_DATA[zoneKey].name} Lv.${level} 파견 (~${mins}분)`, { color: '#88ccff' });
                    SaveManager.save(gs);
                    this.scene.restart({ gameState: gs, selectedZone: zoneKey, deployedIds: [], deployMode: this.deployMode, selectedLevel: this.selectedLevel });
                }
            }
        });

        // 안내
        let hint = '';
        if (this.deployedIds.length === 0) hint = '용병을 편성하세요';
        else if (slotsFull) hint = `파견 슬롯 가득 (${activeExp}/${maxSlots})`;
        else if (!isMain && GuildManager.getMaxUnlockedSubLevel(gs, zoneKey) === 0) hint = '서브 파견 미해금';

        if (hint) {
            this.add.text(btnX, btnY - 18, hint, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
            }).setOrigin(0.5);
        }
    }
}
