class TownScene extends Phaser.Scene {
    constructor() { super('TownScene'); }

    init(data) {
        this.gameState = data.gameState;
    }

    create() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        // === 세이브 마이그레이션 (기존 세이브 호환) ===
        if (!gs.unlockedFacilities) gs.unlockedFacilities = [];
        if (!gs.unlockedFacilities.includes('equipment')) {
            gs.unlockedFacilities.push('equipment');
        }
        if (!gs.unlockedFacilities.includes('guildHall')) {
            gs.unlockedFacilities.push('guildHall');
        }
        SaveManager.save(gs);

        // 파견 완료 처리 (마을 진입 시)
        if (typeof ExpeditionManager !== 'undefined') {
            const newCompleted = ExpeditionManager.processCompleted(gs);
            if (newCompleted.length > 0) {
                GuildManager.addMessage(gs, `🎁 파견 ${newCompleted.length}건 완료 — 수령 대기`);
            }
        }

        // D8 완전 자동화 (마을 진입 시 실행)
        if (typeof AutomationManager !== 'undefined') {
            AutomationManager.runFullAuto(gs);
            AutomationManager.runAutoCollect(gs);
        }

        // ── 배경 ──
        this.add.rectangle(640, 360, 1280, 720, T.bg || 0x1a1510);
        this._drawVignette();

        this._drawHeader();
        this._drawRosterPanel();
        this._drawExpeditionPanel();
        this._drawFacilityGrid();
        this._drawMessageLog();
        this._drawBottomBar();

        // 1초마다 파견 완료 체크
        this._expTimer = this.time.addEvent({
            delay: 1000, loop: true,
            callback: () => {
                if (typeof ExpeditionManager !== 'undefined') {
                    const newDone = ExpeditionManager.processCompleted(this.gameState);
                    if (newDone.length > 0) {
                        newDone.forEach(r => {
                            const icon = r.success ? '✅' : '⚠';
                            GuildManager.addMessage(this.gameState, `${icon} ${r.zoneName} 파견 ${r.success ? '성공' : '실패'} (+${r.goldEarned}G)`);
                        });
                        this.scene.restart();
                    }
                }
            }
        });
    }

    _drawVignette() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const v = this.add.graphics();
        v.fillStyle(0x000000, 0.15);
        v.fillRect(0, 0, 1280, 4);
        v.fillRect(0, 716, 1280, 4);
        v.fillRect(0, 0, 4, 720);
        v.fillRect(1276, 0, 4, 720);
        // 코너 장식
        const c = this.add.graphics();
        c.lineStyle(1, T.ornament || 0x8a7a4a, 0.15);
        c.lineBetween(0, 20, 20, 0); c.lineBetween(0, 30, 30, 0);
        c.lineBetween(1260, 0, 1280, 20); c.lineBetween(1250, 0, 1280, 30);
        c.lineBetween(0, 700, 20, 720); c.lineBetween(0, 690, 30, 720);
        c.lineBetween(1260, 720, 1280, 700); c.lineBetween(1250, 720, 1280, 690);
    }

    _drawHeader() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        // 헤더 배경
        const hBg = this.add.graphics();
        hBg.fillStyle(T.headerBg || 0x1e1810, 1);
        hBg.fillRect(0, 0, 1280, 55);
        // 장식 하단선
        hBg.lineStyle(1, T.ornament || 0x8a7a4a, 0.4);
        hBg.lineBetween(0, 55, 1280, 55);
        hBg.lineStyle(0.5, T.divider || 0x5a4a2a, 0.3);
        hBg.lineBetween(0, 57, 1280, 57);

        // 길드 레벨
        this.add.text(20, 12, `⚔ 길드 Lv.${gs.guildLevel}`, {
            fontSize: '18px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold',
            stroke: '#000', strokeThickness: 1
        });

        // XP 바
        const xpNeeded = GuildManager.getXpToNextLevel(gs);
        const xpRatio = gs.guildLevel >= 8 ? 1 : gs.guildXp / xpNeeded;
        const barX = 165, barY = 18, barW = 180, barH = 12;

        const xpBg = this.add.graphics();
        xpBg.fillStyle(0x1a1510, 1);
        xpBg.fillRoundedRect(barX, barY, barW, barH, 4);
        xpBg.fillStyle(T.buttonPrimary || 0x8a6a2a, 0.9);
        xpBg.fillRoundedRect(barX, barY, barW * xpRatio, barH, 4);
        xpBg.lineStyle(1, T.panelStroke || 0x5a4a2a, 0.5);
        xpBg.strokeRoundedRect(barX, barY, barW, barH, 4);

        const xpLabel = gs.guildLevel >= 8 ? 'MAX' : `${gs.guildXp}/${xpNeeded}`;
        this.add.text(barX + barW + 8, 14, `XP: ${xpLabel}`, {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        });

        // 골드
        this.add.text(1260, 14, `💰 ${gs.gold.toLocaleString()}G`, {
            fontSize: '18px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0);

        // 중앙 정보
        const maxRoster = GuildManager.getMaxRoster(gs);
        this.add.text(640, 14, `용병 ${gs.roster.length}/${maxRoster}  ·  런 #${gs.runCount}`, {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5, 0);

        this.add.text(640, 34, `${gs.reputation || 0} 평판`, {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5, 0);
    }

    _drawRosterPanel() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        UIPanel.create(this, 8, 65, 265, 640, {
            title: '◆ 로스터 ◆',
            ornament: true,
            innerGlow: true,
        });

        if (gs.roster.length === 0) {
            this.add.text(140, 200, '용병이 없습니다\n모집소에서 고용하세요', {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860', align: 'center'
            }).setOrigin(0.5);
            return;
        }

        // 전체 로스터 버튼
        UIButton.create(this, 230, 78, 60, 22, '전체', {
            variant: 'ghost',
            fontSize: 10,
            onClick: () => this.scene.start('RosterScene', { gameState: gs })
        });

        let yOff = 100;
        gs.roster.forEach((merc, idx) => {
            if (yOff > 670) return;
            this._drawMercCard(merc, 18, yOff, 245);
            yOff += 78;
        });
    }

    _drawMercCard(merc, x, y, width) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const base = merc.getBaseClass();
        const rarityData = RARITY_DATA[merc.rarity];
        const rarityTheme = T.rarity && T.rarity[merc.rarity] ? T.rarity[merc.rarity] : { color: rarityData.color, text: rarityData.textColor };
        const stats = merc.getStats();
        const hpRatio = merc.currentHp / stats.hp;

        const cardBg = this.add.graphics();
        const drawCard = (fill, stroke, strokeAlpha) => {
            cardBg.clear();
            cardBg.fillStyle(fill, 1);
            cardBg.fillRoundedRect(x, y, width, 68, 5);
            // 상단 하이라이트
            cardBg.fillStyle(0xffffff, 0.03);
            cardBg.fillRect(x + 2, y + 2, width - 4, 12);
            // 테두리
            cardBg.lineStyle(1, stroke, strokeAlpha);
            cardBg.strokeRoundedRect(x, y, width, 68, 5);
        };
        drawCard(T.cardFill || 0x231e14, rarityTheme.color, 0.5);

        // 이름
        this.add.text(x + 8, y + 6, `${base.icon} ${merc.name}`, {
            fontSize: '12px', fontFamily: T.fontFamily || 'monospace',
            color: rarityTheme.text, fontStyle: 'bold'
        });

        // 레벨
        this.add.text(x + width - 8, y + 6, `Lv.${merc.level}`, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        }).setOrigin(1, 0);

        // 클래스 + 희귀도
        const rarityLabel = (T.rarity && T.rarity[merc.rarity]) ? T.rarity[merc.rarity].label : rarityData.name;
        this.add.text(x + 8, y + 22, `${base.name} · ${rarityLabel}`, {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        });

        // 스탯
        this.add.text(x + 8, y + 36, `HP:${merc.currentHp}/${stats.hp}  ATK:${stats.atk}  DEF:${stats.def}`, {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        });

        // HP 바
        const barW = width - 16, barH = 4, barY2 = y + 52;
        const hpBar = this.add.graphics();
        hpBar.fillStyle(0x1a1510, 1);
        hpBar.fillRoundedRect(x + 8, barY2, barW, barH, 2);
        const hpColor = hpRatio > 0.6 ? 0x66bb55 : hpRatio > 0.3 ? 0xcc8833 : 0xcc4422;
        hpBar.fillStyle(hpColor, 0.9);
        hpBar.fillRoundedRect(x + 8, barY2, barW * hpRatio, barH, 2);

        // 특성 아이콘
        const traitText = merc.traits.map(t => {
            return t.type === 'positive' ? '✦' : t.type === 'legendary' ? '★' : '✧';
        }).join('');
        if (traitText) {
            this.add.text(x + width - 8, y + 22, traitText, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
                color: T.textSecondary || '#b8a888'
            }).setOrigin(1, 0);
        }

        // 파견 중 표시
        if (typeof ExpeditionManager !== 'undefined' && ExpeditionManager.isOnExpedition(this.gameState, merc.id)) {
            const exp = (this.gameState.activeExpeditions || []).find(e => e.partyIds.includes(merc.id));
            const zoneName = exp ? ZONE_DATA[exp.zoneKey].name : '파견';
            this.add.text(x + width - 8, y + 56, `📦 ${zoneName}`, {
                fontSize: '8px', fontFamily: T.fontFamily || 'monospace',
                color: T.textBlue || '#6699cc'
            }).setOrigin(1, 0);
        }

        // 인터랙션
        const hitZone = this.add.zone(x + width / 2, y + 34, width, 68).setInteractive({ useHandCursor: true });
        hitZone.on('pointerover', () => {
            drawCard(T.cardHover || 0x3a3020, rarityTheme.color, 0.9);
            const lines = [
                `${base.icon} ${merc.name} [${rarityLabel} ${base.name}]`,
                `Lv.${merc.level}  XP: ${merc.xp}/${merc.level >= 30 ? 'MAX' : merc.getXpToNextLevel()}`,
                `HP: ${merc.currentHp}/${stats.hp}`,
                `ATK: ${stats.atk}  DEF: ${stats.def}  SPD: ${stats.moveSpeed}`,
                `CRIT: ${Math.floor(stats.critRate * 100)}%  범위: ${stats.range}`,
                '---',
                ...merc.traits.map(t => {
                    const sym = t.type === 'positive' ? '✦' : t.type === 'legendary' ? '★' : '✧';
                    return `${sym} ${t.name}: ${t.desc}`;
                })
            ];
            if (merc.level >= 5) {
                lines.push('---', `스킬: ${base.skillName} — ${base.skillDesc}`);
            }
            lines.push('---', '클릭 — 상세/장비/해고');
            UITooltip.show(this, x + width + 5, y, lines);
        });
        hitZone.on('pointerout', () => {
            drawCard(T.cardFill || 0x231e14, rarityTheme.color, 0.5);
            UITooltip.hide(this);
        });
        hitZone.on('pointerdown', () => {
            UITooltip.hide(this);
            this.scene.start('RosterScene', { gameState: this.gameState, selectedMercId: merc.id });
        });
    }

    _drawExpeditionPanel() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const active = gs.activeExpeditions || [];
        const pending = gs.pendingResults || [];
        if (active.length === 0 && pending.length === 0) return;

        const panelX = 875, panelY = 110, panelW = 390, panelH = 175;
        UIPanel.create(this, panelX, panelY, panelW, panelH, {
            title: '📦 서브 파견',
            ornament: true,
        });

        const maxSlots = ExpeditionManager.getMaxSlots(gs);
        this.add.text(panelX + panelW - 12, panelY + 14, `${active.length}/${maxSlots}`, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        }).setOrigin(1, 0.5);

        if (pending.length > 0) {
            this.add.text(panelX + panelW - 12, panelY + 30, `🎁 수령 대기: ${pending.length}`, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
                color: T.textGold || '#ffcc44'
            }).setOrigin(1, 0);
        }

        // 활성 파견 목록
        let cy = panelY + 38;
        active.slice(0, 3).forEach(exp => {
            const zone = ZONE_DATA[exp.zoneKey];
            const progress = ExpeditionManager.getProgress(exp);
            const remainSec = Math.ceil(ExpeditionManager.getRemainingMs(exp) / 1000);
            const mins = Math.floor(remainSec / 60);
            const secs = remainSec % 60;
            const timeStr = `${mins}:${secs.toString().padStart(2, '0')}`;

            this.add.text(panelX + 12, cy, `${zone.icon} ${zone.name} Lv.${exp.zoneLevel}`, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                color: zone.textColor || T.textPrimary
            });
            this.add.text(panelX + panelW - 12, cy, timeStr, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                color: T.textSecondary || '#b8a888'
            }).setOrigin(1, 0);

            // 진행 바
            const barX2 = panelX + 12, barW = panelW - 24;
            const barBg = this.add.graphics();
            barBg.fillStyle(0x1a1510, 1);
            barBg.fillRoundedRect(barX2, cy + 14, barW, 5, 2);
            barBg.fillStyle(zone.color || (T.buttonPrimary || 0x8a6a2a), 0.8);
            barBg.fillRoundedRect(barX2, cy + 14, barW * progress, 5, 2);
            cy += 27;
        });

        // 수령 버튼
        if (pending.length > 0) {
            UIButton.create(this, panelX + panelW / 2, panelY + panelH - 20, 200, 28, `🎁 ${pending.length}건 모두 수령`, {
                variant: 'primary',
                fontSize: 11,
                onClick: () => {
                    const ids = pending.map(r => r.id);
                    let totalGold = 0, totalLoot = 0;
                    ids.forEach(id => {
                        const r = ExpeditionManager.collectResult(gs, id);
                        if (r) { totalGold += r.goldEarned; totalLoot += (r.loot || []).length; }
                    });
                    SaveManager.save(gs);
                    UIToast.show(this, `+${totalGold}G, 장비 ${totalLoot}개`);
                    this.scene.restart();
                }
            });
        }
    }

    _drawFacilityGrid() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        const startX = 310;
        const startY = 85;
        const cellW = 135;
        const cellH = 100;
        const gap = 12;
        const cols = 3;

        const facilityOrder = ['recruit', 'storage', 'equipment', 'gate', 'forge', 'auction', 'training', 'temple', 'intel', 'eliteRecruit', 'vault', 'guildHall'];

        // 시설 제목
        this.add.text(startX + (cols * (cellW + gap) - gap) / 2, 68, '◆ 시설 ◆', {
            fontSize: '12px', fontFamily: T.fontFamily || 'monospace',
            color: T.textAccent || '#cc8833', fontStyle: 'bold'
        }).setOrigin(0.5);

        facilityOrder.forEach((key, idx) => {
            const col = idx % cols;
            const row = Math.floor(idx / cols);
            const x = startX + col * (cellW + gap);
            const y = startY + row * (cellH + gap);
            this._drawFacilityCell(key, x, y, cellW, cellH);
        });
    }

    _drawFacilityCell(key, x, y, w, h) {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const fac = FACILITY_DATA[key];
        const isUnlocked = gs.unlockedFacilities.includes(key);
        const canUnlock = GuildManager.canUnlockFacility(gs, key);
        const levelReached = gs.guildLevel >= fac.unlockLevel;

        const bg = this.add.graphics();
        const drawCell = (fill, stroke, strokeAlpha) => {
            bg.clear();
            bg.fillStyle(fill, 1);
            bg.fillRoundedRect(x, y, w, h, 6);
            // 상단 하이라이트
            if (isUnlocked) {
                bg.fillStyle(0xffffff, 0.03);
                bg.fillRect(x + 2, y + 2, w - 4, 16);
            }
            bg.lineStyle(1, stroke, strokeAlpha);
            bg.strokeRoundedRect(x, y, w, h, 6);
        };

        if (isUnlocked) {
            drawCell(T.cardFill || 0x231e14, T.panelStroke || 0x5a4a2a, 0.7);
        } else {
            drawCell(0x1a1812, 0x3a3428, 0.4);
        }

        // 아이콘
        const iconSize = isUnlocked ? '24px' : '20px';
        this.add.text(x + w / 2, y + 22, fac.icon, {
            fontSize: iconSize
        }).setOrigin(0.5).setAlpha(isUnlocked ? 1 : 0.4);

        // 이름
        this.add.text(x + w / 2, y + 48, fac.name, {
            fontSize: '12px', fontFamily: T.fontFamily || 'monospace',
            color: isUnlocked ? (T.textPrimary || '#e8d8c0') : (T.textMuted || '#887860'),
            fontStyle: 'bold'
        }).setOrigin(0.5);

        if (!isUnlocked) {
            if (canUnlock) {
                this.add.text(x + w / 2, y + 66, `해금 (${fac.cost}G)`, {
                    fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                    color: T.textGold || '#ffcc44'
                }).setOrigin(0.5);
            } else if (levelReached) {
                this.add.text(x + w / 2, y + 66, `${fac.cost}G 필요`, {
                    fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                    color: T.textDanger || '#cc4422'
                }).setOrigin(0.5);
            } else {
                this.add.text(x + w / 2, y + 66, `Lv.${fac.unlockLevel} 필요`, {
                    fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                    color: T.textMuted || '#887860'
                }).setOrigin(0.5);
            }
            if (!canUnlock) {
                this.add.text(x + w / 2, y + h / 2, '🔒', { fontSize: '16px' })
                    .setOrigin(0.5).setAlpha(0.2);
            }
        } else {
            this.add.text(x + w / 2, y + 66, fac.desc, {
                fontSize: '8px', fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860',
                wordWrap: { width: w - 14 }, align: 'center'
            }).setOrigin(0.5);
        }

        // 인터랙션
        const hitZone = this.add.zone(x + w / 2, y + h / 2, w, h).setInteractive({ useHandCursor: true });
        hitZone.on('pointerdown', () => this._onFacilityClick(key));

        hitZone.on('pointerover', () => {
            if (isUnlocked) {
                drawCell(T.cardHover || 0x3a3020, T.ornament || 0x8a7a4a, 0.9);
            } else if (canUnlock) {
                drawCell(0x2a2418, T.buttonPrimary || 0x8a6a2a, 0.8);
            }
        });
        hitZone.on('pointerout', () => {
            if (isUnlocked) {
                drawCell(T.cardFill || 0x231e14, T.panelStroke || 0x5a4a2a, 0.7);
            } else {
                drawCell(0x1a1812, 0x3a3428, 0.4);
            }
        });
    }

    _onFacilityClick(key) {
        const gs = this.gameState;
        const fac = FACILITY_DATA[key];
        const isUnlocked = gs.unlockedFacilities.includes(key);

        if (!isUnlocked) {
            if (GuildManager.canUnlockFacility(gs, key)) {
                GuildManager.unlockFacility(gs, key);
                UIToast.show(this, `${fac.name} 해금! (-${fac.cost}G)`);
                this.scene.restart({ gameState: gs });
            } else if (gs.guildLevel < fac.unlockLevel) {
                UIToast.show(this, `길드 Lv.${fac.unlockLevel} 필요`, { type: 'danger' });
            } else {
                UIToast.show(this, `골드가 부족합니다 (${fac.cost}G 필요)`, { type: 'danger' });
            }
            return;
        }

        if (fac.scene) {
            this.scene.start(fac.scene, { gameState: gs });
        } else {
            UIToast.show(this, `${fac.name} — 준비 중...`);
        }
    }

    _drawBottomBar() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        // 하단 바 배경
        const btm = this.add.graphics();
        btm.lineStyle(1, T.divider || 0x5a4a2a, 0.3);
        btm.lineBetween(280, 688, 1000, 688);

        // 자동화 버튼
        if (typeof GuildHallManager !== 'undefined') {
            GuildHallManager.ensureState(gs);
            const stage = gs.guildHall.automation || 0;
            if (stage >= 1) {
                UIButton.create(this, 580, 703, 140, 26, '⚙ 자동화 설정', {
                    variant: 'info',
                    fontSize: 10,
                    onClick: () => this.scene.start('AutomationScene', { gameState: gs })
                });
            }
        }

        // 도감 버튼
        UIButton.create(this, 740, 703, 100, 26, '📖 도감', {
            variant: 'ghost',
            fontSize: 10,
            onClick: () => this.scene.start('CodexScene', { gameState: gs })
        });
    }

    _drawMessageLog() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const panelX = 815;

        UIPanel.create(this, panelX, 65, 457, 420, {
            title: '◆ 최근 소식 ◆',
            ornament: true,
        });

        const messages = gs.messages || [];
        const display = messages.slice(0, 12);

        display.forEach((msg, idx) => {
            const alpha = 1 - idx * 0.05;
            this.add.text(panelX + 12, 98 + idx * 20, `· ${msg}`, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                color: T.textSecondary || '#b8a888'
            }).setAlpha(Math.max(0.3, alpha));
        });

        if (messages.length === 0) {
            this.add.text(panelX + 228, 250, '아직 소식이 없습니다', {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5);
        }

        // 상인 호감도
        if (typeof MERCHANT_DATA !== 'undefined') {
            UIPanel.create(this, panelX, 495, 457, 210, {
                title: '◆ 상인 호감도 ◆',
            });
            if (typeof initMerchantFavor === 'function') initMerchantFavor(gs);
            let my = 528;
            for (const [key, data] of Object.entries(MERCHANT_DATA)) {
                const favor = gs.merchantFavor?.[key] || 0;
                const tier = typeof getMerchantTier === 'function' ? getMerchantTier(gs, key) : null;
                const tierName = tier ? tier.name : '?';
                const barW = 120, barH = 5;
                const ratio = Math.min(1, favor / 10);

                this.add.text(panelX + 12, my, `${data.icon} ${data.name}`, {
                    fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                    color: T.textPrimary || '#e8d8c0', fontStyle: 'bold'
                });
                this.add.text(panelX + 100, my, `[${tierName}]`, {
                    fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
                    color: T.textMuted || '#887860'
                });

                const barBg = this.add.graphics();
                barBg.fillStyle(0x1a1510, 1);
                barBg.fillRoundedRect(panelX + 250, my + 2, barW, barH, 2);
                if (ratio > 0) {
                    barBg.fillStyle(T.buttonPrimary || 0x8a6a2a, 0.8);
                    barBg.fillRoundedRect(panelX + 250, my + 2, barW * ratio, barH, 2);
                }
                barBg.lineStyle(0.5, T.panelStroke || 0x5a4a2a, 0.3);
                barBg.strokeRoundedRect(panelX + 250, my + 2, barW, barH, 2);

                this.add.text(panelX + 250 + barW + 8, my, `${favor}`, {
                    fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
                    color: T.textMuted || '#887860'
                });

                if (tier && tier.perks) {
                    this.add.text(panelX + 12, my + 15, tier.perks, {
                        fontSize: '8px', fontFamily: T.fontFamily || 'monospace',
                        color: T.textSuccess || '#66bb55'
                    });
                }
                my += 36;
            }
        }
    }
}
