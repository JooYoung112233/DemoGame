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
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
        const gs = this.gameState;

        if (this.selectedZone) {
            this._drawZoneDetail();
        } else {
            this._drawZoneOverview();
        }
    }

    // ═══════════════════════════════════════════════
    //  구역 개요 — 3구역 카드 + 각각 파견 현황
    // ═══════════════════════════════════════════════

    _drawZoneOverview() {
        const gs = this.gameState;

        this.add.text(640, 25, '출발 게이트', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        const activeExp = (gs.activeExpeditions || []).length;
        const maxSlots = ExpeditionManager.getMaxSlots(gs);
        this.add.text(640, 50, `서브 파견 슬롯: ${activeExp}/${maxSlots}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5);

        const cardW = 380, cardH = 590, gap = 20;
        const totalW = ZONE_KEYS.length * cardW + (ZONE_KEYS.length - 1) * gap;
        const startX = (1280 - totalW) / 2;

        ZONE_KEYS.forEach((key, idx) => {
            this._drawZoneOverviewCard(key, startX + idx * (cardW + gap), 70, cardW, cardH);
        });
    }

    _drawZoneOverviewCard(zoneKey, x, y, w, h) {
        const gs = this.gameState;
        const zone = ZONE_DATA[zoneKey];
        const isLocked = gs.zoneLevel[zoneKey] === 0;

        const bg = this.add.graphics();
        bg.fillStyle(isLocked ? 0x101018 : 0x12121e, 1);
        bg.fillRoundedRect(x, y, w, h, 6);
        bg.lineStyle(2, isLocked ? 0x333344 : zone.color, isLocked ? 0.3 : 0.6);
        bg.strokeRoundedRect(x, y, w, h, 6);

        // 헤더
        this.add.text(x + w / 2, y + 25, zone.icon, { fontSize: '32px' }).setOrigin(0.5);
        this.add.text(x + w / 2, y + 60, zone.name, {
            fontSize: '16px', fontFamily: 'monospace', color: isLocked ? '#555566' : zone.textColor, fontStyle: 'bold'
        }).setOrigin(0.5);
        this.add.text(x + w / 2, y + 80, zone.subtitle, {
            fontSize: '11px', fontFamily: 'monospace', color: '#777788'
        }).setOrigin(0.5);

        if (isLocked) {
            this.add.text(x + w / 2, y + 120, `🔒 길드 Lv.${zone.unlockLevel} 필요`, {
                fontSize: '13px', fontFamily: 'monospace', color: '#555566'
            }).setOrigin(0.5);
            return;
        }

        // 구역 레벨/정보
        const zLv = gs.zoneLevel[zoneKey];
        const rounds = getMaxRounds(zLv);
        this.add.text(x + w / 2, y + 100, `구역 Lv.${zLv}  |  ${rounds}라운드`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aaaacc'
        }).setOrigin(0.5);

        this.add.text(x + w / 2, y + 118, zone.desc, {
            fontSize: '10px', fontFamily: 'monospace', color: '#667788'
        }).setOrigin(0.5);

        // 서브 해금 상태
        const maxUnlocked = GuildManager.getMaxUnlockedSubLevel(gs, zoneKey);
        const clears = GuildManager.getZoneClearCount(gs, zoneKey, zLv);
        const needed = GuildManager.SUB_UNLOCK_CLEARS;
        if (maxUnlocked > 0) {
            this.add.text(x + w / 2, y + 140, `📦 서브 해금 (최대 Lv.${maxUnlocked})`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#88ccff'
            }).setOrigin(0.5);
        } else {
            this.add.text(x + w / 2, y + 140, `🔓 서브 해금까지 ${clears}/${needed}회 클리어`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#aaaaaa'
            }).setOrigin(0.5);
        }

        // === 활성 파견 표시 ===
        const activeExps = (gs.activeExpeditions || []).filter(e => e.zoneKey === zoneKey);
        const pendingResults = (gs.pendingResults || []).filter(r => r.zoneKey === zoneKey);

        let cy = y + 165;
        this.add.text(x + 15, cy, '파견 현황', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aaccee', fontStyle: 'bold'
        });
        cy += 20;

        if (activeExps.length === 0 && pendingResults.length === 0) {
            this.add.text(x + 15, cy, '진행 중인 파견 없음', {
                fontSize: '10px', fontFamily: 'monospace', color: '#555566'
            });
            cy += 18;
        }

        activeExps.forEach(exp => {
            const progress = ExpeditionManager.getProgress(exp);
            const remainMs = ExpeditionManager.getRemainingMs(exp);
            const remainSec = Math.ceil(remainMs / 1000);
            const remainStr = remainSec > 60 ? `${Math.floor(remainSec / 60)}분 ${remainSec % 60}초` : `${remainSec}초`;

            const expBg = this.add.graphics();
            expBg.fillStyle(0x1a2a3a, 1);
            expBg.fillRoundedRect(x + 10, cy, w - 20, 55, 3);

            this.add.text(x + 18, cy + 5, `⏳ Lv.${exp.zoneLevel} 파견`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#88ccff', fontStyle: 'bold'
            });
            this.add.text(x + w - 18, cy + 5, `잔여 ${remainStr}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#ffaa66'
            }).setOrigin(1, 0);

            // 진행률 바
            const barW = w - 40;
            this.add.graphics().fillStyle(0x222233, 1).fillRect(x + 18, cy + 24, barW, 6);
            this.add.graphics().fillStyle(0x4488cc, 1).fillRect(x + 18, cy + 24, barW * progress, 6);
            this.add.text(x + 18 + barW / 2, cy + 25, `${Math.floor(progress * 100)}%`, {
                fontSize: '8px', fontFamily: 'monospace', color: '#ffffff'
            }).setOrigin(0.5);

            // 파견된 용병 이름
            const names = exp.partyIds.map(id => {
                const m = gs.roster.find(r => r.id === id);
                return m ? m.name : '?';
            }).join(', ');
            this.add.text(x + 18, cy + 36, names, {
                fontSize: '9px', fontFamily: 'monospace', color: '#778899'
            });
            cy += 60;
        });

        pendingResults.forEach(result => {
            const resBg = this.add.graphics();
            resBg.fillStyle(result.success ? 0x1a2a1a : 0x2a1a1a, 1);
            resBg.fillRoundedRect(x + 10, cy, w - 20, 40, 3);

            const icon = result.success ? '✅' : '⚠';
            this.add.text(x + 18, cy + 5, `${icon} Lv.${result.zoneLevel} — ${result.success ? '성공' : '실패'}`, {
                fontSize: '11px', fontFamily: 'monospace', color: result.success ? '#88ff88' : '#ff8888'
            });
            this.add.text(x + 18, cy + 22, `+${result.goldEarned}G | 장비 ${result.loot.length}개 — 수령 대기`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#aaaaaa'
            });

            UIButton.create(this, x + w - 55, cy + 20, 70, 26, '수령', {
                color: 0x448844, hoverColor: 0x55aa55, textColor: '#ccffcc', fontSize: 10,
                onClick: () => {
                    ExpeditionManager.collectResult(gs, result.id);
                    SaveManager.save(gs);
                    this.scene.restart({ gameState: gs });
                }
            });
            cy += 45;
        });

        // 입장 버튼
        UIButton.create(this, x + w / 2, y + h - 35, w - 40, 36, `▶ ${zone.name} 입장`, {
            color: 0x335566, hoverColor: 0x446688, textColor: '#ccddee', fontSize: 14,
            onClick: () => {
                this.selectedZone = zoneKey;
                this.scene.restart({ gameState: gs, selectedZone: zoneKey, deployedIds: this.deployedIds, deployMode: this.deployMode });
            }
        });
    }

    // ═══════════════════════════════════════════════
    //  구역 상세 — 라운드 선택 + 편성 + 출발
    // ═══════════════════════════════════════════════

    _drawZoneDetail() {
        const gs = this.gameState;
        const zoneKey = this.selectedZone;
        const zone = ZONE_DATA[zoneKey];
        const zLv = gs.zoneLevel[zoneKey];

        // 헤더
        this.add.text(640, 25, `${zone.icon} ${zone.name} — ${zone.subtitle}`, {
            fontSize: '18px', fontFamily: 'monospace', color: zone.textColor, fontStyle: 'bold'
        }).setOrigin(0.5);

        UIButton.create(this, 80, 25, 110, 30, '← 구역선택', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.restart({ gameState: gs, deployedIds: [], deployMode: this.deployMode })
        });

        // 메인/서브 모드
        const isMain = this.deployMode === 'main';
        UIButton.create(this, 970, 25, 130, 28, '⚔ 메인 도전', {
            color: isMain ? 0x884422 : 0x333344,
            hoverColor: 0xaa5533, textColor: isMain ? '#ffcc88' : '#888899', fontSize: 11,
            onClick: () => { if (!isMain) this.scene.restart({ gameState: gs, selectedZone: zoneKey, deployedIds: this.deployedIds, deployMode: 'main', selectedLevel: this.selectedLevel }); }
        });
        UIButton.create(this, 1120, 25, 130, 28, '📦 서브 파견', {
            color: !isMain ? 0x224488 : 0x333344,
            hoverColor: 0x3355aa, textColor: !isMain ? '#88ccff' : '#888899', fontSize: 11,
            onClick: () => { if (isMain) this.scene.restart({ gameState: gs, selectedZone: zoneKey, deployedIds: this.deployedIds, deployMode: 'sub', selectedLevel: this.selectedLevel }); }
        });

        const modeDesc = isMain
            ? '메인 도전: 직접 전투. 스킬 발동 가능, 보상 1.5배, 구역 레벨업'
            : '서브 파견: 시간 경과형 자동. 일반공격만, 깬 레벨까지만 파밍';
        this.add.text(640, 52, modeDesc, {
            fontSize: '10px', fontFamily: 'monospace', color: isMain ? '#ffaa66' : '#88aaff'
        }).setOrigin(0.5);

        // 좌: 레벨 선택 + 파견 현황 | 우: 파티 편성
        this._drawLevelSelect(20, 70, 400, 620);
        this._drawPartyPanel(440, 70, 820, 620);
    }

    _drawLevelSelect(x, y, w, h) {
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
                fontSize: '13px', fontFamily: 'monospace', color: '#ffcc88', fontStyle: 'bold'
            });
            cy += 20;
            const rounds = getMaxRounds(zLv);
            this.add.text(x + 15, cy, `${rounds}라운드 | 보스 포함`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa'
            });
            cy += 16;
            this.add.text(x + 15, cy, `승리 시 → Lv.${zLv + 1} 해금`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#88ff88'
            });
            cy += 30;
            this.selectedLevel = zLv;
        } else {
            // 서브: 해금된 레벨 중 선택
            const maxUnlocked = GuildManager.getMaxUnlockedSubLevel(gs, zoneKey);
            if (maxUnlocked === 0) {
                const clears = GuildManager.getZoneClearCount(gs, zoneKey, zLv);
                this.add.text(x + 15, cy, `🔓 서브 미해금`, {
                    fontSize: '13px', fontFamily: 'monospace', color: '#ff8866', fontStyle: 'bold'
                });
                cy += 18;
                this.add.text(x + 15, cy, `메인 ${clears}/${GuildManager.SUB_UNLOCK_CLEARS}회 클리어 필요`, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa'
                });
                cy += 30;
            } else {
                this.add.text(x + 15, cy, `서브 가능 레벨 (1 ~ ${maxUnlocked})`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#88ccff'
                });
                cy += 22;

                const selected = this.selectedLevel || maxUnlocked;
                for (let lv = 1; lv <= maxUnlocked; lv++) {
                    const isSel = lv === selected;
                    UIButton.create(this, x + 15 + (lv - 1) * 50, cy + 12, 44, 24, `Lv.${lv}`, {
                        color: isSel ? 0x4488cc : 0x222233,
                        hoverColor: 0x5599dd,
                        textColor: isSel ? '#ffffff' : '#888899',
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
            fontSize: '11px', fontFamily: 'monospace', color: '#aaccee', fontStyle: 'bold'
        });
        cy += 20;

        const activeExps = (gs.activeExpeditions || []).filter(e => e.zoneKey === zoneKey);
        const pendingResults = (gs.pendingResults || []).filter(r => r.zoneKey === zoneKey);

        if (activeExps.length === 0 && pendingResults.length === 0) {
            this.add.text(x + 15, cy, '진행 중인 파견 없음', {
                fontSize: '10px', fontFamily: 'monospace', color: '#555566'
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
            expBg.fillStyle(0x1a2a3a, 1);
            expBg.fillRoundedRect(x + 10, cy, w - 20, 75, 3);

            this.add.text(x + 18, cy + 5, `⏳ Lv.${exp.zoneLevel} 서브 파견`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#88ccff', fontStyle: 'bold'
            });
            this.add.text(x + w - 18, cy + 5, remainStr, {
                fontSize: '10px', fontFamily: 'monospace', color: '#ffaa66'
            }).setOrigin(1, 0);

            // 진행률 바
            const barW = w - 40;
            this.add.graphics().fillStyle(0x222233, 1).fillRect(x + 18, cy + 24, barW, 6);
            this.add.graphics().fillStyle(0x4488cc, 1).fillRect(x + 18, cy + 24, barW * progress, 6);

            this.add.text(x + 18, cy + 36, elapsedStr, {
                fontSize: '9px', fontFamily: 'monospace', color: '#667788'
            });

            // 파견 용병
            const names = exp.partyIds.map(id => {
                const m = gs.roster.find(r => r.id === id);
                return m ? `${m.getBaseClass().icon}${m.name}` : '?';
            }).join('  ');
            this.add.text(x + 18, cy + 52, names, {
                fontSize: '9px', fontFamily: 'monospace', color: '#aabbcc'
            });
            cy += 80;
        });

        pendingResults.forEach(result => {
            const resBg = this.add.graphics();
            resBg.fillStyle(result.success ? 0x1a2a1a : 0x2a1a1a, 1);
            resBg.fillRoundedRect(x + 10, cy, w - 20, 44, 3);

            const icon = result.success ? '✅' : '⚠';
            this.add.text(x + 18, cy + 5, `${icon} Lv.${result.zoneLevel} ${result.success ? '성공' : '실패'}`, {
                fontSize: '11px', fontFamily: 'monospace', color: result.success ? '#88ff88' : '#ff8888'
            });
            this.add.text(x + 18, cy + 22, `+${result.goldEarned}G | 장비 ${result.loot.length}개`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#aaaaaa'
            });
            UIButton.create(this, x + w - 55, cy + 22, 70, 24, '수령', {
                color: 0x448844, hoverColor: 0x55aa55, textColor: '#ccffcc', fontSize: 10,
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
        const gs = this.gameState;
        const slotW = Math.min(190, (panelW - (maxDeploy - 1) * 8) / maxDeploy);

        this.add.text(x, y - 2, '← 후열(원거리)          전열(근접) →', {
            fontSize: '9px', fontFamily: 'monospace', color: '#667788', fontStyle: 'italic'
        });

        for (let i = 0; i < maxDeploy; i++) {
            const sx = x + i * (slotW + 8);
            const merc = gs.roster.find(m => m.id === this.deployedIds[i]);
            const position = maxDeploy - i;

            const bg = this.add.graphics();
            bg.fillStyle(merc ? 0x1a2a3a : 0x151525, 1);
            bg.fillRoundedRect(sx, y + 14, slotW, 170, 4);
            bg.lineStyle(1, merc ? 0x446688 : 0x333355, 0.6);
            bg.strokeRoundedRect(sx, y + 14, slotW, 170, 4);

            const posColor = position <= 2 ? '#ff8866' : '#88ccff';
            const posLabel = position <= 2 ? `전열[${position}]` : `후열[${position}]`;
            this.add.text(sx + 5, y + 18, posLabel, {
                fontSize: '9px', fontFamily: 'monospace', color: posColor, fontStyle: 'bold'
            });

            if (merc) {
                const base = merc.getBaseClass();
                const rarity = RARITY_DATA[merc.rarity];
                const stats = merc.getStats();

                this.add.text(sx + slotW - 5, y + 18, `Lv.${merc.level}`, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#aaaaaa'
                }).setOrigin(1, 0);
                this.add.text(sx + 5, y + 32, `${base.icon} ${merc.name}`, {
                    fontSize: '11px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
                });
                this.add.text(sx + 5, y + 48, `HP:${merc.currentHp}/${stats.hp}`, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#8888aa'
                });
                this.add.text(sx + 5, y + 60, `ATK:${stats.atk} DEF:${stats.def}`, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#8888aa'
                });

                // 액션 미리보기
                if (typeof getClassActions === 'function') {
                    const actions = getClassActions(merc.classKey);
                    let ay = y + 76;
                    actions.forEach(action => {
                        if (ay > y + 155) return;
                        const canUse = action.casterPositions.includes(position);
                        const color = canUse ? '#88ccaa' : '#663333';
                        this.add.text(sx + 5, ay, `${canUse ? '✓' : '✗'} ${action.name}`, {
                            fontSize: '8px', fontFamily: 'monospace', color
                        });
                        ay += 11;
                    });
                }

                // 이동/해제
                if (i > 0) {
                    UIButton.create(this, sx + 20, y + 168, 30, 18, '◀', {
                        color: 0x445566, hoverColor: 0x556677, textColor: '#aaccee', fontSize: 10,
                        onClick: () => this._swapDeployed(i, i - 1)
                    });
                }
                if (i < maxDeploy - 1 && this.deployedIds[i + 1]) {
                    UIButton.create(this, sx + 55, y + 168, 30, 18, '▶', {
                        color: 0x445566, hoverColor: 0x556677, textColor: '#aaccee', fontSize: 10,
                        onClick: () => this._swapDeployed(i, i + 1)
                    });
                }
                UIButton.create(this, sx + slotW - 30, y + 168, 50, 18, '해제', {
                    color: 0x555555, hoverColor: 0x666666, textColor: '#cccccc', fontSize: 9,
                    onClick: () => {
                        this.deployedIds = this.deployedIds.filter(id => id !== merc.id);
                        this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: this.deployMode, selectedLevel: this.selectedLevel });
                    }
                });
            } else {
                this.add.text(sx + slotW / 2, y + 95, '(빈 슬롯)', {
                    fontSize: '10px', fontFamily: 'monospace', color: '#444455'
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
            fontSize: '11px', fontFamily: 'monospace', color: '#888899'
        });

        if (available.length === 0 && onExpedition.length === 0) {
            this.add.text(x + 100, y + 30, '편성 가능한 용병 없음', {
                fontSize: '11px', fontFamily: 'monospace', color: '#555566'
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
            bg.fillStyle(0x151525, 1);
            bg.fillRoundedRect(cx, cy, cardW, cardH, 3);
            bg.lineStyle(1, rarity.color, 0.3);
            bg.strokeRoundedRect(cx, cy, cardW, cardH, 3);

            this.add.text(cx + 5, cy + 4, `${base.icon} ${merc.name}`, {
                fontSize: '10px', fontFamily: 'monospace', color: rarity.textColor
            });
            this.add.text(cx + 5, cy + 18, `Lv.${merc.level} ${base.name}`, {
                fontSize: '9px', fontFamily: 'monospace', color: '#777788'
            });
            this.add.text(cx + 5, cy + 32, `HP:${merc.currentHp}/${merc.getStats().hp}`, {
                fontSize: '8px', fontFamily: 'monospace', color: '#8888aa'
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
                fontSize: '10px', fontFamily: 'monospace', color: '#667788'
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
                    fontSize: '9px', fontFamily: 'monospace', color: '#556677'
                });
                cx += cardW + 6;
            });
        }
    }

    _drawSavedParties(x, y, panelW) {
        const gs = this.gameState;
        if (!gs.savedParties) gs.savedParties = [];

        this.add.text(x, y, '저장 편성 (클릭=불러오기 / 우클릭=저장)', {
            fontSize: '10px', fontFamily: 'monospace', color: '#888899'
        });

        const SLOT_COUNT = 4;
        const slotW = Math.min(180, (panelW - (SLOT_COUNT - 1) * 6) / SLOT_COUNT);
        for (let i = 0; i < SLOT_COUNT; i++) {
            const sx = x + i * (slotW + 6);
            const saved = gs.savedParties[i];
            const hasSaved = !!saved;

            const bg = this.add.graphics();
            bg.fillStyle(hasSaved ? 0x223344 : 0x1a1a2a, 1);
            bg.fillRoundedRect(sx, y + 15, slotW, 38, 3);
            bg.lineStyle(1, hasSaved ? 0x5588aa : 0x333344, 0.5);
            bg.strokeRoundedRect(sx, y + 15, slotW, 38, 3);

            if (hasSaved) {
                const aliveCount = saved.mercIds.filter(id => gs.roster.find(m => m.id === id && m.alive)).length;
                this.add.text(sx + 5, y + 19, saved.name || `편성 ${i + 1}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#aaccff', fontStyle: 'bold'
                });
                this.add.text(sx + 5, y + 34, `${aliveCount}/${saved.mercIds.length}명`, {
                    fontSize: '9px', fontFamily: 'monospace', color: aliveCount === saved.mercIds.length ? '#88ccaa' : '#aa8888'
                });
            } else {
                this.add.text(sx + slotW / 2, y + 33, `슬롯 ${i + 1}`, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#555566'
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

    _drawDepartButton(panelX, panelY, panelW, panelH) {
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

        const btnLabel = isMain ? '⚔ 출발 (메인 전투)' : '📦 파견 시작';
        const btnX = panelX + panelW / 2;
        const btnY = panelY + panelH - 25;

        UIButton.create(this, btnX, btnY, 240, 36, btnLabel, {
            color: canDepart ? (isMain ? 0xaa4422 : 0x4488cc) : 0x333333,
            hoverColor: isMain ? 0xcc5533 : 0x55aaee,
            textColor: canDepart ? '#ffffff' : '#555555',
            fontSize: 14,
            disabled: !canDepart,
            onClick: () => {
                if (!canDepart) return;
                const party = this.deployedIds.map(id => gs.roster.find(m => m.id === id)).filter(Boolean);

                if (isMain) {
                    gs.runCount++;
                    SaveManager.save(gs);
                    const sceneMap = { bloodpit: 'ManualBattleScene', cargo: 'CargoBattleScene', blackout: 'BlackoutBattleScene' };
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
                fontSize: '10px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5);
        }
    }
}
