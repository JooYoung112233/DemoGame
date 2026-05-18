class TownScene extends Phaser.Scene {
    constructor() { super('TownScene'); }

    init(data) {
        this.gameState = data.gameState;
    }

    create() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        // === 세이브 마이그레이션 ===
        if (!gs.unlockedFacilities) gs.unlockedFacilities = [];
        if (!gs.unlockedFacilities.includes('equipment')) gs.unlockedFacilities.push('equipment');
        if (!gs.unlockedFacilities.includes('guildHall')) gs.unlockedFacilities.push('guildHall');
        SaveManager.save(gs);

        // 파견 완료 처리
        if (typeof ExpeditionManager !== 'undefined') {
            const newCompleted = ExpeditionManager.processCompleted(gs);
            if (newCompleted.length > 0) {
                GuildManager.addMessage(gs, `🎁 파견 ${newCompleted.length}건 완료 — 수령 대기`);
            }
        }
        // D8 자동화
        if (typeof AutomationManager !== 'undefined') {
            AutomationManager.runFullAuto(gs);
            AutomationManager.runAutoCollect(gs);
        }

        // ── 배경 ──
        this.add.rectangle(640, 360, 1280, 720, T.bg || 0x1a1510);
        this._drawVignette();

        this._drawHeader();
        this._drawZoneCards();
        this._drawFacilityBar();
        this._drawSidePanel();

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

    // ═══════════════════════════════════════════════════════
    //  배경 장식
    // ═══════════════════════════════════════════════════════
    _drawVignette() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const v = this.add.graphics();
        v.fillStyle(0x000000, 0.15);
        v.fillRect(0, 0, 1280, 4); v.fillRect(0, 716, 1280, 4);
        v.fillRect(0, 0, 4, 720); v.fillRect(1276, 0, 4, 720);
        const c = this.add.graphics();
        c.lineStyle(1, T.ornament || 0x8a7a4a, 0.15);
        c.lineBetween(0, 20, 20, 0); c.lineBetween(0, 30, 30, 0);
        c.lineBetween(1260, 0, 1280, 20); c.lineBetween(1250, 0, 1280, 30);
        c.lineBetween(0, 700, 20, 720); c.lineBetween(0, 690, 30, 720);
        c.lineBetween(1260, 720, 1280, 700); c.lineBetween(1250, 720, 1280, 690);
    }

    // ═══════════════════════════════════════════════════════
    //  헤더 — 길드 정보 + 골드
    // ═══════════════════════════════════════════════════════
    _drawHeader() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        const hBg = this.add.graphics();
        hBg.fillStyle(T.headerBg || 0x1e1810, 1);
        hBg.fillRect(0, 0, 1280, 50);
        hBg.lineStyle(1, T.ornament || 0x8a7a4a, 0.4);
        hBg.lineBetween(0, 50, 1280, 50);

        // 길드 레벨
        this.add.text(20, 10, `⚔ 길드 Lv.${gs.guildLevel}`, {
            fontSize: '16px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold'
        });

        // XP 바
        const xpNeeded = GuildManager.getXpToNextLevel(gs);
        const xpRatio = gs.guildLevel >= 8 ? 1 : gs.guildXp / xpNeeded;
        const barX = 150, barY = 15, barW = 140, barH = 10;
        const xpBg = this.add.graphics();
        xpBg.fillStyle(0x1a1510, 1);
        xpBg.fillRoundedRect(barX, barY, barW, barH, 3);
        xpBg.fillStyle(T.buttonPrimary || 0x8a6a2a, 0.9);
        xpBg.fillRoundedRect(barX, barY, barW * xpRatio, barH, 3);
        xpBg.lineStyle(1, T.panelStroke || 0x5a4a2a, 0.4);
        xpBg.strokeRoundedRect(barX, barY, barW, barH, 3);
        const xpLabel = gs.guildLevel >= 8 ? 'MAX' : `${gs.guildXp}/${xpNeeded}`;
        this.add.text(barX + barW + 6, 10, xpLabel, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
        });

        // 중앙: 로스터 요약
        const maxRoster = GuildManager.getMaxRoster(gs);
        this.add.text(640, 14, `용병 ${gs.roster.length}/${maxRoster}  ·  런 #${gs.runCount}  ·  평판 ${gs.guildReputation || 0}`, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
        }).setOrigin(0.5, 0);

        // 로스터 버튼
        UIButton.create(this, 640, 38, 90, 20, '📋 로스터', {
            variant: 'ghost', fontSize: 9,
            onClick: () => this.scene.start('RosterScene', { gameState: gs })
        });

        // 골드
        this.add.text(1260, 10, `💰 ${gs.gold.toLocaleString()}G`, {
            fontSize: '16px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0);

        // 도감 / 자동화 (헤더 우측)
        UIButton.create(this, 1180, 38, 70, 20, '📖 도감', {
            variant: 'ghost', fontSize: 9,
            onClick: () => this.scene.start('CodexScene', { gameState: gs })
        });

        if (typeof GuildHallManager !== 'undefined') {
            GuildHallManager.ensureState(gs);
            if ((gs.guildHall.automation || 0) >= 1) {
                UIButton.create(this, 1090, 38, 80, 20, '⚙ 자동화', {
                    variant: 'ghost', fontSize: 9,
                    onClick: () => this.scene.start('AutomationScene', { gameState: gs })
                });
            }
        }
    }

    // ═══════════════════════════════════════════════════════
    //  메인 — 3구역 카드 (출발 게이트)
    // ═══════════════════════════════════════════════════════
    _drawZoneCards() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        // 섹션 타이틀
        this.add.text(640, 62, '◈  출발 게이트  ◈', {
            fontSize: '14px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5);

        const cardW = 290, cardH = 340, gap = 15;
        const totalW = 3 * cardW + 2 * gap;
        const startX = (1280 - totalW) / 2;
        const startY = 82;

        ZONE_KEYS.forEach((key, idx) => {
            const x = startX + idx * (cardW + gap);
            this._drawZoneCard(key, x, startY, cardW, cardH);
        });
    }

    _drawZoneCard(zoneKey, x, y, w, h) {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const zone = ZONE_DATA[zoneKey];
        const zoneLevel = gs.zoneLevel[zoneKey] || 0;
        const isLocked = zoneLevel === 0;

        const bg = this.add.graphics();
        const drawBg = (fill, strokeC, strokeA) => {
            bg.clear();
            bg.fillStyle(fill, 1);
            bg.fillRoundedRect(x, y, w, h, 8);
            // 상단 그라데이션 바
            if (!isLocked) {
                bg.fillStyle(zone.color, 0.08);
                bg.fillRect(x + 2, y + 2, w - 4, 50);
            }
            bg.lineStyle(2, strokeC, strokeA);
            bg.strokeRoundedRect(x, y, w, h, 8);
            // 이중 테두리
            bg.lineStyle(0.5, strokeC, strokeA * 0.3);
            bg.strokeRoundedRect(x + 3, y + 3, w - 6, h - 6, 6);
        };

        if (isLocked) {
            drawBg(0x151210, T.panelStroke || 0x5a4a2a, 0.3);
        } else {
            drawBg(T.cardFill || 0x231e14, zone.color, 0.6);
        }

        // 아이콘
        const iconSize = isLocked ? '28px' : '36px';
        this.add.text(x + w / 2, y + 30, zone.icon, {
            fontSize: iconSize
        }).setOrigin(0.5).setAlpha(isLocked ? 0.3 : 1);

        // 구역 이름
        this.add.text(x + w / 2, y + 60, zone.name, {
            fontSize: '18px', fontFamily: T.fontFamily || 'monospace',
            color: isLocked ? (T.textMuted || '#887860') : zone.textColor,
            fontStyle: 'bold'
        }).setOrigin(0.5);

        // 서브타이틀
        this.add.text(x + w / 2, y + 82, zone.subtitle, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5);

        if (isLocked) {
            // 잠긴 상태
            this.add.text(x + w / 2, y + 130, '🔒', { fontSize: '32px' }).setOrigin(0.5).setAlpha(0.3);
            this.add.text(x + w / 2, y + 175, `길드 Lv.${zone.unlockLevel} 해금`, {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5);
            return;
        }

        // 구역 레벨
        const lvBg = this.add.graphics();
        lvBg.fillStyle(zone.color, 0.15);
        lvBg.fillRoundedRect(x + w / 2 - 50, y + 98, 100, 24, 4);
        lvBg.lineStyle(1, zone.color, 0.3);
        lvBg.strokeRoundedRect(x + w / 2 - 50, y + 98, 100, 24, 4);
        this.add.text(x + w / 2, y + 110, `Lv. ${zoneLevel}`, {
            fontSize: '14px', fontFamily: T.fontFamily || 'monospace',
            color: zone.textColor, fontStyle: 'bold'
        }).setOrigin(0.5);

        // 설명
        this.add.text(x + w / 2, y + 140, zone.desc, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888', align: 'center',
            wordWrap: { width: w - 24 }
        }).setOrigin(0.5);

        // 보상 정보
        const rewardY = y + 170;
        const divG = this.add.graphics();
        divG.lineStyle(1, T.divider || 0x5a4a2a, 0.3);
        divG.lineBetween(x + 15, rewardY, x + w - 15, rewardY);

        this.add.text(x + 15, rewardY + 8, `💰 ${zone.baseGoldReward}~G`, {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textGold || '#ffcc44'
        });
        this.add.text(x + 15, rewardY + 22, `📦 ${zone.lootCount.min}~${zone.lootCount.max}개`, {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textSecondary || '#b8a888'
        });
        this.add.text(x + w - 15, rewardY + 8, `⭐ ${zone.baseXpReward} XP`, {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: '#4488ff'
        }).setOrigin(1, 0);
        this.add.text(x + w - 15, rewardY + 22, `☠ ${Math.floor(zone.deathChance * 100)}%`, {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: '#ff6666'
        }).setOrigin(1, 0);

        // 특수 재료
        this.add.text(x + w / 2, rewardY + 42, `특수 소재: ${zone.specialMaterial}`, {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5);

        // 활성 레벨 효과
        const activeEffects = getZoneLevelEffects(zoneKey, zoneLevel);
        if (activeEffects.length > 0) {
            const effectY = rewardY + 60;
            const effG = this.add.graphics();
            effG.lineStyle(1, zone.color, 0.15);
            effG.lineBetween(x + 15, effectY, x + w - 15, effectY);
            activeEffects.slice(-2).forEach((eff, i) => {
                this.add.text(x + w / 2, effectY + 8 + i * 14, `⚡ ${eff.name}`, {
                    fontSize: '8px', fontFamily: T.fontFamily || 'monospace',
                    color: zone.textColor
                }).setOrigin(0.5).setAlpha(0.7);
            });
        }

        // 파견 현황
        const activeExp = (gs.activeExpeditions || []).filter(e => e.zoneKey === zoneKey);
        if (activeExp.length > 0) {
            this.add.text(x + w / 2, y + h - 68, `📦 파견 ${activeExp.length}건 진행 중`, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
                color: T.textBlue || '#6699cc'
            }).setOrigin(0.5);
        }

        // ⚔ 출전 버튼 (메인 CTA)
        UIButton.create(this, x + w / 2, y + h - 38, w - 30, 40, `⚔  ${zone.name} 출전`, {
            variant: 'primary', fontSize: 14,
            onClick: () => this.scene.start('DeployScene', {
                gameState: gs, selectedZone: zoneKey
            })
        });

        // 호버
        const hitZone = this.add.zone(x + w / 2, y + h / 2 - 25, w, h - 50).setInteractive({ useHandCursor: true });
        hitZone.on('pointerover', () => drawBg(T.cardHover || 0x3a3020, zone.color, 0.9));
        hitZone.on('pointerout', () => drawBg(T.cardFill || 0x231e14, zone.color, 0.6));
    }

    // ═══════════════════════════════════════════════════════
    //  하단 — 시설 바 (카테고리별 정리)
    // ═══════════════════════════════════════════════════════
    _drawFacilityBar() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const barY = 430;

        // 구분선
        const divG = this.add.graphics();
        divG.lineStyle(1, T.ornament || 0x8a7a4a, 0.3);
        divG.lineBetween(15, barY, 1265, barY);
        divG.lineStyle(0.5, T.divider || 0x5a4a2a, 0.2);
        divG.lineBetween(15, barY + 2, 1265, barY + 2);

        // 카테고리 정의
        const categories = [
            {
                label: '⚔ 용병',
                items: ['recruit', 'eliteRecruit', 'training']
            },
            {
                label: '🎽 장비',
                items: ['equipment', 'storage', 'forge']
            },
            {
                label: '💰 경제',
                items: ['auction', 'vault']
            },
            {
                label: '🏛 길드',
                items: ['temple', 'intel', 'guildHall']
            }
        ];

        const totalCats = categories.length;
        const catGap = 12;
        const catAreaW = (1280 - 30 - (totalCats - 1) * catGap) / totalCats;
        let catX = 15;

        categories.forEach((cat, catIdx) => {
            const cx = catX;
            const cy = barY + 10;

            // 카테고리 배경
            const catBg = this.add.graphics();
            catBg.fillStyle(T.panelFill || 0x2a2218, 0.5);
            catBg.fillRoundedRect(cx, cy, catAreaW, 170, 6);
            catBg.lineStyle(1, T.panelStroke || 0x5a4a2a, 0.3);
            catBg.strokeRoundedRect(cx, cy, catAreaW, 170, 6);

            // 카테고리 레이블
            this.add.text(cx + catAreaW / 2, cy + 12, cat.label, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace',
                color: T.textAccent || '#cc8833', fontStyle: 'bold'
            }).setOrigin(0.5);

            // 구분선
            const sepG = this.add.graphics();
            sepG.lineStyle(1, T.divider || 0x5a4a2a, 0.2);
            sepG.lineBetween(cx + 10, cy + 26, cx + catAreaW - 10, cy + 26);

            // 시설 아이템들
            const itemW = Math.min(90, (catAreaW - 20) / Math.max(cat.items.length, 1));
            const itemStartX = cx + (catAreaW - cat.items.length * itemW) / 2;

            cat.items.forEach((key, itemIdx) => {
                const fac = FACILITY_DATA[key];
                if (!fac) return;
                const ix = itemStartX + itemIdx * itemW + itemW / 2;
                const iy = cy + 32;
                this._drawFacilityIcon(key, fac, ix, iy, itemW - 8);
            });

            catX += catAreaW + catGap;
        });
    }

    _drawFacilityIcon(key, fac, cx, cy, w) {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const isUnlocked = gs.unlockedFacilities.includes(key);
        const canUnlock = GuildManager.canUnlockFacility(gs, key);
        const levelReached = gs.guildLevel >= fac.unlockLevel;

        const iconBg = this.add.graphics();
        const drawIcon = (fill, strokeC, strokeA) => {
            iconBg.clear();
            iconBg.fillStyle(fill, 1);
            iconBg.fillRoundedRect(cx - w / 2, cy, w, 120, 5);
            iconBg.lineStyle(1, strokeC, strokeA);
            iconBg.strokeRoundedRect(cx - w / 2, cy, w, 120, 5);
        };

        if (isUnlocked) {
            drawIcon(T.cardFill || 0x231e14, T.panelStroke || 0x5a4a2a, 0.5);
        } else {
            drawIcon(0x1a1812, 0x3a3428, 0.3);
        }

        // 아이콘
        this.add.text(cx, cy + 24, fac.icon, {
            fontSize: isUnlocked ? '22px' : '18px'
        }).setOrigin(0.5).setAlpha(isUnlocked ? 1 : 0.35);

        // 이름
        this.add.text(cx, cy + 50, fac.name, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: isUnlocked ? (T.textPrimary || '#e8d8c0') : (T.textMuted || '#887860'),
            fontStyle: 'bold', align: 'center',
            wordWrap: { width: w - 4 }
        }).setOrigin(0.5);

        // 상태 표시
        if (!isUnlocked) {
            if (canUnlock) {
                this.add.text(cx, cy + 75, `해금`, {
                    fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
                    color: T.textGold || '#ffcc44'
                }).setOrigin(0.5);
                this.add.text(cx, cy + 88, `${fac.cost}G`, {
                    fontSize: '8px', fontFamily: T.fontFamily || 'monospace',
                    color: T.textGold || '#ffcc44'
                }).setOrigin(0.5);
            } else if (levelReached) {
                this.add.text(cx, cy + 80, `${fac.cost}G`, {
                    fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
                    color: T.textDanger || '#cc4422'
                }).setOrigin(0.5);
            } else {
                this.add.text(cx, cy + 75, '🔒', { fontSize: '12px' }).setOrigin(0.5).setAlpha(0.25);
                this.add.text(cx, cy + 95, `Lv.${fac.unlockLevel}`, {
                    fontSize: '8px', fontFamily: T.fontFamily || 'monospace',
                    color: T.textMuted || '#887860'
                }).setOrigin(0.5);
            }
        } else {
            this.add.text(cx, cy + 75, fac.desc, {
                fontSize: '7px', fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860',
                wordWrap: { width: w - 6 }, align: 'center'
            }).setOrigin(0.5);
        }

        // 히트존
        const hitZone = this.add.zone(cx, cy + 60, w, 120).setInteractive({ useHandCursor: true });
        hitZone.on('pointerdown', () => this._onFacilityClick(key));
        hitZone.on('pointerover', () => {
            if (isUnlocked) {
                drawIcon(T.cardHover || 0x3a3020, T.ornament || 0x8a7a4a, 0.8);
            } else if (canUnlock) {
                drawIcon(0x2a2418, T.buttonPrimary || 0x8a6a2a, 0.7);
            }
        });
        hitZone.on('pointerout', () => {
            if (isUnlocked) {
                drawIcon(T.cardFill || 0x231e14, T.panelStroke || 0x5a4a2a, 0.5);
            } else {
                drawIcon(0x1a1812, 0x3a3428, 0.3);
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
                UIToast.show(this, `골드 부족 (${fac.cost}G 필요)`, { type: 'danger' });
            }
            return;
        }

        if (fac.scene) {
            this.scene.start(fac.scene, { gameState: gs });
        } else {
            UIToast.show(this, `${fac.name} — 준비 중...`);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  우측 사이드 — 파견 현황 + 메시지 로그
    // ═══════════════════════════════════════════════════════
    _drawSidePanel() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        // 파견 현황 (하단 바 위 오른쪽 영역)
        const active = gs.activeExpeditions || [];
        const pending = gs.pendingResults || [];

        if (active.length > 0 || pending.length > 0) {
            this._drawExpeditionMini(active, pending);
        }

        // 메시지 로그 (하단 바 아래)
        this._drawMessageLogMini();
    }

    _drawExpeditionMini(active, pending) {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const px = 948, py = 612, pw = 322, ph = 100;

        const epBg = this.add.graphics();
        epBg.fillStyle(T.panelFill || 0x2a2218, 0.85);
        epBg.fillRoundedRect(px, py, pw, ph, 6);
        epBg.lineStyle(1, T.panelStroke || 0x5a4a2a, 0.4);
        epBg.strokeRoundedRect(px, py, pw, ph, 6);

        this.add.text(px + 8, py + 5, '📦 서브 파견', {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: T.textAccent || '#cc8833', fontStyle: 'bold'
        });

        const maxSlots = ExpeditionManager.getMaxSlots(gs);
        this.add.text(px + pw - 8, py + 5, `${active.length}/${maxSlots}`, {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(1, 0);

        let ey = py + 22;
        active.slice(0, 2).forEach(exp => {
            const zone = ZONE_DATA[exp.zoneKey];
            const remainSec = Math.ceil(ExpeditionManager.getRemainingMs(exp) / 1000);
            const mins = Math.floor(remainSec / 60);
            const secs = remainSec % 60;
            this.add.text(px + 8, ey, `${zone.icon} ${zone.name}`, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: zone.textColor
            });
            this.add.text(px + pw - 8, ey, `${mins}:${secs.toString().padStart(2, '0')}`, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textSecondary || '#b8a888'
            }).setOrigin(1, 0);
            ey += 16;
        });
        if (active.length > 2) {
            this.add.text(px + 8, ey, `... +${active.length - 2}건`, {
                fontSize: '8px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
            });
        }

        if (pending.length > 0) {
            UIButton.create(this, px + pw / 2, py + ph - 16, pw - 20, 22, `🎁 ${pending.length}건 수령`, {
                variant: 'primary', fontSize: 10,
                onClick: () => {
                    pending.forEach(r => ExpeditionManager.collectResult(gs, r.id));
                    SaveManager.save(gs);
                    this.scene.restart();
                }
            });
        }
    }

    _drawMessageLogMini() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const px = 10, py = 612, pw = 930, ph = 100;

        const logBg = this.add.graphics();
        logBg.fillStyle(T.panelFill || 0x2a2218, 0.5);
        logBg.fillRoundedRect(px, py, pw, ph, 6);
        logBg.lineStyle(1, T.panelStroke || 0x5a4a2a, 0.2);
        logBg.strokeRoundedRect(px, py, pw, ph, 6);

        this.add.text(px + 8, py + 5, '◆ 최근 소식', {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
            color: T.textAccent || '#cc8833', fontStyle: 'bold'
        });

        const messages = gs.messages || [];
        const display = messages.slice(0, 4);
        display.forEach((msg, idx) => {
            this.add.text(px + 12, py + 22 + idx * 16, `· ${msg}`, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
                color: T.textSecondary || '#b8a888'
            }).setAlpha(1 - idx * 0.15);
        });

        if (messages.length === 0) {
            this.add.text(px + pw / 2, py + ph / 2, '아직 소식이 없습니다', {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5);
        }
    }
}
