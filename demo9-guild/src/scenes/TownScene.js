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
        if (!gs.questDone) gs.questDone = {};
        SaveManager.save(gs);

        // 파견 완료 처리
        if (typeof ExpeditionManager !== 'undefined') {
            const newCompleted = ExpeditionManager.processCompleted(gs);
            if (newCompleted.length > 0) {
                GuildManager.addMessage(gs, `🎁 파견 ${newCompleted.length}건 완료 — 수령 대기`);
            }
        }
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
        this._drawQuestGuide();

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

        // === 레벨업 팝업 ===
        if (gs._pendingLevelUp) {
            this._showLevelUpPopup(gs._pendingLevelUp);
            delete gs._pendingLevelUp;
            SaveManager.save(gs);
        }
        // === 첫 플레이 시퀀스 ===
        else if (gs._isNewGame) {
            delete gs._isNewGame;
            SaveManager.save(gs);
            this._showFirstPlaySequence();
        }
    }

    // ═══════════════════════════════════════════════════════
    //  첫 플레이 시퀀스 — 신규 게임 시작 시 자동 유도
    // ═══════════════════════════════════════════════════════
    _showFirstPlaySequence() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        // 오버레이
        const overlay = this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.7).setDepth(900);

        // 환영 메시지 패널
        const panelW = 500, panelH = 320;
        const px = 640 - panelW / 2, py = 360 - panelH / 2;

        const bg = this.add.graphics().setDepth(901);
        bg.fillStyle(T.panelFill || 0x2a2218, 1);
        bg.fillRoundedRect(px, py, panelW, panelH, 12);
        bg.lineStyle(2, T.ornament || 0x8a7a4a, 0.8);
        bg.strokeRoundedRect(px, py, panelW, panelH, 12);
        bg.lineStyle(1, T.ornament || 0x8a7a4a, 0.2);
        bg.strokeRoundedRect(px + 4, py + 4, panelW - 8, panelH - 8, 10);

        this.add.text(640, py + 30, '⚔', { fontSize: '40px' }).setOrigin(0.5).setDepth(902);

        this.add.text(640, py + 75, '용병 길드에 오신 것을 환영합니다!', {
            fontSize: '18px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(902);

        // 스타터 용병 목록 표시
        const starterNames = gs.roster.map(m => {
            const base = m.getBaseClass();
            return `${base.icon} ${m.name} (${base.name})`;
        });

        const lines = [
            '당신은 신생 용병 길드의 길드마스터입니다.',
            '',
            '초기 용병 4명이 배치되었습니다:',
            ...starterNames,
            '',
            '전열(전사·도적)과 후열(사제·궁수)을 배치하고 출격하세요!'
        ];

        lines.forEach((line, i) => {
            const isName = starterNames.includes(line);
            this.add.text(640, py + 110 + i * 16, line, {
                fontSize: isName ? '12px' : '11px', fontFamily: T.fontFamily || 'monospace',
                color: line === '' ? '#000' : isName ? (T.textGold || '#ffcc44') : (T.textSecondary || '#b8a888'),
                fontStyle: isName ? 'bold' : 'normal',
                align: 'center'
            }).setOrigin(0.5).setDepth(902);
        });

        const allItems = [overlay, bg];

        UIButton.create(this, 640, py + panelH - 35, 200, 40, '▸ 편성하러 가기', {
            variant: 'primary', fontSize: 14, depth: 903,
            onClick: () => {
                this.scene.start('DeployScene', { gameState: gs });
            }
        });
    }

    // ═══════════════════════════════════════════════════════
    //  레벨업 팝업 — 새 시설/구역 해금 안내
    // ═══════════════════════════════════════════════════════
    _showLevelUpPopup(newLevel) {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        // 이 레벨에서 해금되는 것들 수집
        const newFacilities = Object.entries(FACILITY_DATA)
            .filter(([k, f]) => f.unlockLevel === newLevel && k !== 'gate')
            .map(([k, f]) => f);
        const newZones = Object.entries(ZONE_DATA)
            .filter(([k, z]) => z.unlockLevel === newLevel)
            .map(([k, z]) => z);

        const items = [
            ...newZones.map(z => `${z.icon} ${z.name} 구역`),
            ...newFacilities.map(f => `${f.icon} ${f.name}`)
        ];

        const overlay = this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.65).setDepth(900);
        overlay.setInteractive();

        const panelW = 420, panelH = 180 + items.length * 22;
        const px = 640 - panelW / 2, py = 360 - panelH / 2;

        const bg = this.add.graphics().setDepth(901);
        bg.fillStyle(T.panelFill || 0x2a2218, 1);
        bg.fillRoundedRect(px, py, panelW, panelH, 12);
        bg.lineStyle(2, T.textGold ? parseInt(T.textGold.replace('#', ''), 16) : 0xffcc44, 0.8);
        bg.strokeRoundedRect(px, py, panelW, panelH, 12);

        // 축하 이모지 + 텍스트
        this.add.text(640, py + 25, '🎉', { fontSize: '32px' }).setOrigin(0.5).setDepth(902);

        this.add.text(640, py + 60, `길드 레벨 ${newLevel} 달성!`, {
            fontSize: '20px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(902);

        if (items.length > 0) {
            this.add.text(640, py + 90, '─── 새로 해금 ───', {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5).setDepth(902);

            items.forEach((item, i) => {
                const txt = this.add.text(640, py + 112 + i * 22, item, {
                    fontSize: '13px', fontFamily: T.fontFamily || 'monospace',
                    color: '#88ffaa', fontStyle: 'bold'
                }).setOrigin(0.5).setDepth(902);

                // 등장 애니메이션
                txt.setAlpha(0).setScale(0.8);
                this.tweens.add({
                    targets: txt, alpha: 1, scaleX: 1, scaleY: 1,
                    delay: 200 + i * 150, duration: 300, ease: 'Back.easeOut'
                });
            });
        } else {
            this.add.text(640, py + 100, '길드가 더 강해졌습니다!', {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace',
                color: T.textSecondary || '#b8a888'
            }).setOrigin(0.5).setDepth(902);
        }

        // 보상 힌트
        const rosterLimit = GuildManager.getMaxRoster(gs);
        this.add.text(640, py + panelH - 65, `로스터 최대: ${rosterLimit}명  ·  훈련 포인트 +1`, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        }).setOrigin(0.5).setDepth(902);

        UIButton.create(this, 640, py + panelH - 32, 180, 36, '확인', {
            variant: 'primary', fontSize: 14, depth: 903,
            onClick: () => {
                overlay.destroy();
                bg.destroy();
                // 간단하게 씬 재시작하여 깔끔하게
                this.scene.restart({ gameState: gs });
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

        this.add.text(20, 10, `⚔ 길드 Lv.${gs.guildLevel}`, {
            fontSize: '16px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold'
        });

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

        const maxRoster = GuildManager.getMaxRoster(gs);
        this.add.text(640, 14, `용병 ${gs.roster.length}/${maxRoster}  ·  런 #${gs.runCount}  ·  평판 ${gs.guildReputation || 0}`, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
        }).setOrigin(0.5, 0);

        UIButton.create(this, 640, 38, 90, 20, '📋 로스터', {
            variant: 'ghost', fontSize: 9,
            onClick: () => this.scene.start('RosterScene', { gameState: gs })
        });

        this.add.text(1260, 10, `💰 ${gs.gold.toLocaleString()}G`, {
            fontSize: '16px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0);

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
    //  퀘스트 가이드 — 좌측 상단, 하이라이트 + 펄스
    // ═══════════════════════════════════════════════════════
    _drawQuestGuide() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        if (typeof getActiveQuests === 'undefined') return;
        const quests = getActiveQuests(gs);
        const progress = (typeof getQuestProgress !== 'undefined') ? getQuestProgress(gs) : null;

        if (quests.length === 0) {
            // 올 클리어
            if (progress && progress.done === progress.total) {
                const doneTxt = this.add.text(10, 58, '✅ 모든 임무 완료!', {
                    fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                    color: '#88ff88', fontStyle: 'bold'
                }).setDepth(100);
            }
            return;
        }

        const px = 10, py = 56, pw = 280;
        const ph = 24 + quests.length * 28;

        // 배경 — 살짝 빛나는 효과
        const bg = this.add.graphics().setDepth(100);
        bg.fillStyle(0x1a2a1a, 0.9);
        bg.fillRoundedRect(px, py, pw, ph, 6);
        bg.lineStyle(1.5, 0x44aa44, 0.5);
        bg.strokeRoundedRect(px, py, pw, ph, 6);

        // 헤더
        const header = this.add.text(px + 8, py + 5, '📌 다음 목표', {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: '#66cc66', fontStyle: 'bold'
        }).setDepth(101);

        if (progress) {
            this.add.text(px + pw - 8, py + 5, `${progress.done}/${progress.total}`, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
                color: '#448844'
            }).setOrigin(1, 0).setDepth(101);
        }

        quests.forEach((q, idx) => {
            const qy = py + 24 + idx * 28;

            // 화살표 아이콘 (펄스 애니메이션)
            const arrow = this.add.text(px + 10, qy, '▸', {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace',
                color: '#66ff66'
            }).setDepth(101);
            this.tweens.add({
                targets: arrow, alpha: 0.3,
                yoyo: true, repeat: -1, duration: 800, ease: 'Sine.easeInOut'
            });

            // 퀘스트 텍스트
            const txt = this.add.text(px + 24, qy, q.text, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace',
                color: '#aaddaa'
            }).setDepth(101);

            if (q.target) {
                // 클릭 가능 — 밑줄 + 커서
                txt.setInteractive({ useHandCursor: true });
                txt.on('pointerover', () => {
                    txt.setColor('#ffffff');
                    txt.setStyle({ ...txt.style, textDecoration: 'underline' });
                });
                txt.on('pointerout', () => {
                    txt.setColor('#aaddaa');
                });
                txt.on('pointerdown', () => {
                    if (q.target === 'DeployScene') {
                        this.scene.start(q.target, { gameState: gs, selectedZone: 'bloodpit' });
                    } else {
                        this.scene.start(q.target, { gameState: gs });
                    }
                });

                // "이동" 힌트
                this.add.text(px + pw - 10, qy + 2, '클릭 →', {
                    fontSize: '8px', fontFamily: T.fontFamily || 'monospace',
                    color: '#448844'
                }).setOrigin(1, 0).setDepth(101);
            }
        });

        // 전체 패널 글로우 펄스
        const glow = this.add.graphics().setDepth(99);
        glow.lineStyle(2, 0x44ff44, 0.15);
        glow.strokeRoundedRect(px - 1, py - 1, pw + 2, ph + 2, 7);
        this.tweens.add({
            targets: glow, alpha: 0.3,
            yoyo: true, repeat: -1, duration: 1500, ease: 'Sine.easeInOut'
        });
    }

    // ═══════════════════════════════════════════════════════
    //  메인 — 구역 카드 (단계별 해금)
    // ═══════════════════════════════════════════════════════
    _drawZoneCards() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        this.add.text(640, 62, '◈  출발 게이트  ◈', {
            fontSize: '14px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5);

        // 해금됐거나 다음 해금 대상인 구역만 표시
        const visibleZones = ZONE_KEYS.filter(key => {
            const zone = ZONE_DATA[key];
            return gs.guildLevel >= zone.unlockLevel - 1;
        });
        if (visibleZones.length === 0) visibleZones.push('bloodpit');

        const cardW = Math.min(290, (1240 - (visibleZones.length - 1) * 15) / visibleZones.length);
        const cardH = 340;
        const gap = 15;
        const totalW = visibleZones.length * cardW + (visibleZones.length - 1) * gap;
        const startX = (1280 - totalW) / 2;
        const startY = 82;

        visibleZones.forEach((key, idx) => {
            const x = startX + idx * (cardW + gap);
            this._drawZoneCard(key, x, startY, cardW, cardH);
        });
    }

    _drawZoneCard(zoneKey, x, y, w, h) {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const zone = ZONE_DATA[zoneKey];
        const zoneLevel = gs.zoneLevel[zoneKey] || 0;
        const isLocked = gs.guildLevel < zone.unlockLevel;

        const bg = this.add.graphics();
        const drawBg = (fill, strokeC, strokeA) => {
            bg.clear();
            bg.fillStyle(fill, 1);
            bg.fillRoundedRect(x, y, w, h, 8);
            if (!isLocked) {
                bg.fillStyle(zone.color, 0.08);
                bg.fillRect(x + 2, y + 2, w - 4, 50);
            }
            bg.lineStyle(2, strokeC, strokeA);
            bg.strokeRoundedRect(x, y, w, h, 8);
            bg.lineStyle(0.5, strokeC, strokeA * 0.3);
            bg.strokeRoundedRect(x + 3, y + 3, w - 6, h - 6, 6);
        };

        if (isLocked) {
            drawBg(0x151210, T.panelStroke || 0x5a4a2a, 0.3);
        } else {
            drawBg(T.cardFill || 0x231e14, zone.color, 0.6);
        }

        const iconSize = isLocked ? '28px' : '36px';
        this.add.text(x + w / 2, y + 30, zone.icon, {
            fontSize: iconSize
        }).setOrigin(0.5).setAlpha(isLocked ? 0.3 : 1);

        this.add.text(x + w / 2, y + 60, zone.name, {
            fontSize: '18px', fontFamily: T.fontFamily || 'monospace',
            color: isLocked ? (T.textMuted || '#887860') : zone.textColor,
            fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(x + w / 2, y + 82, zone.subtitle, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5);

        if (isLocked) {
            this.add.text(x + w / 2, y + 130, '🔒', { fontSize: '32px' }).setOrigin(0.5).setAlpha(0.3);
            this.add.text(x + w / 2, y + 175, `길드 Lv.${zone.unlockLevel} 해금`, {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5);
            return;
        }

        const displayLevel = zoneLevel || 1;
        const lvBg = this.add.graphics();
        lvBg.fillStyle(zone.color, 0.15);
        lvBg.fillRoundedRect(x + w / 2 - 50, y + 98, 100, 24, 4);
        lvBg.lineStyle(1, zone.color, 0.3);
        lvBg.strokeRoundedRect(x + w / 2 - 50, y + 98, 100, 24, 4);
        this.add.text(x + w / 2, y + 110, `Lv. ${displayLevel}`, {
            fontSize: '14px', fontFamily: T.fontFamily || 'monospace',
            color: zone.textColor, fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(x + w / 2, y + 140, zone.desc, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888', align: 'center',
            wordWrap: { width: w - 24 }
        }).setOrigin(0.5);

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

        this.add.text(x + w / 2, rewardY + 42, `특수 소재: ${zone.specialMaterial}`, {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5);

        const activeEffects = getZoneLevelEffects(zoneKey, displayLevel);
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

        const activeExp = (gs.activeExpeditions || []).filter(e => e.zoneKey === zoneKey);
        if (activeExp.length > 0) {
            this.add.text(x + w / 2, y + h - 68, `📦 파견 ${activeExp.length}건 진행 중`, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
                color: T.textBlue || '#6699cc'
            }).setOrigin(0.5);
        }

        // 출전 버튼 — 용병 없으면 비활성 + 힌트
        const hasRoster = gs.roster && gs.roster.length > 0;
        UIButton.create(this, x + w / 2, y + h - 38, w - 30, 40, `⚔  ${zone.name} 출전`, {
            variant: hasRoster ? 'primary' : 'ghost', fontSize: 14,
            onClick: () => {
                if (!hasRoster) {
                    UIToast.show(this, '먼저 용병을 고용하세요!', { type: 'danger' });
                    return;
                }
                this.scene.start('DeployScene', {
                    gameState: gs, selectedZone: zoneKey
                });
            }
        });

        const hitZone = this.add.zone(x + w / 2, y + h / 2 - 25, w, h - 50).setInteractive({ useHandCursor: true });
        hitZone.on('pointerover', () => {
            if (!isLocked) drawBg(T.cardHover || 0x3a3020, zone.color, 0.9);
        });
        hitZone.on('pointerout', () => {
            if (!isLocked) drawBg(T.cardFill || 0x231e14, zone.color, 0.6);
        });
    }

    // ═══════════════════════════════════════════════════════
    //  하단 — 시설 바 (단계별 확장)
    // ═══════════════════════════════════════════════════════
    _drawFacilityBar() {
        const gs = this.gameState;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const barY = 430;

        const divG = this.add.graphics();
        divG.lineStyle(1, T.ornament || 0x8a7a4a, 0.3);
        divG.lineBetween(15, barY, 1265, barY);
        divG.lineStyle(0.5, T.divider || 0x5a4a2a, 0.2);
        divG.lineBetween(15, barY + 2, 1265, barY + 2);

        // 현재 길드 레벨에서 보여야 할 시설 필터링
        const tierThresholds = { 1: 1, 2: 1, 3: 2, 4: 4, 5: 6 };
        const visibleKeys = Object.keys(FACILITY_DATA).filter(key => {
            const fac = FACILITY_DATA[key];
            if (key === 'gate') return false;
            const threshold = tierThresholds[fac.tier] || 1;
            return gs.guildLevel >= threshold;
        });

        if (visibleKeys.length === 0) return;

        const catDefs = [
            { label: '⚔ 용병',  keys: ['recruit', 'eliteRecruit', 'training'] },
            { label: '🎽 장비',  keys: ['equipment', 'storage', 'forge'] },
            { label: '💰 경제',  keys: ['auction', 'vault'] },
            { label: '🏛 길드',  keys: ['temple', 'intel', 'guildHall'] }
        ];

        const categories = catDefs
            .map(cat => ({
                label: cat.label,
                items: cat.keys.filter(k => visibleKeys.includes(k))
            }))
            .filter(cat => cat.items.length > 0);

        const totalCats = categories.length;
        const catGap = 12;
        const catAreaW = (1280 - 30 - (totalCats - 1) * catGap) / totalCats;
        let catX = 15;

        categories.forEach((cat) => {
            const cx = catX;
            const cy = barY + 10;

            const catBg = this.add.graphics();
            catBg.fillStyle(T.panelFill || 0x2a2218, 0.5);
            catBg.fillRoundedRect(cx, cy, catAreaW, 170, 6);
            catBg.lineStyle(1, T.panelStroke || 0x5a4a2a, 0.3);
            catBg.strokeRoundedRect(cx, cy, catAreaW, 170, 6);

            this.add.text(cx + catAreaW / 2, cy + 12, cat.label, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace',
                color: T.textAccent || '#cc8833', fontStyle: 'bold'
            }).setOrigin(0.5);

            const sepG = this.add.graphics();
            sepG.lineStyle(1, T.divider || 0x5a4a2a, 0.2);
            sepG.lineBetween(cx + 10, cy + 26, cx + catAreaW - 10, cy + 26);

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

        // 퀘스트 대상인지 체크 (하이라이트용)
        const isQuestTarget = typeof getActiveQuests !== 'undefined' &&
            getActiveQuests(gs).some(q => {
                if (q.target === fac.scene) return true;
                if (q.id === `unlock_${key}`) return true;
                return false;
            });

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

        // 퀘스트 대상이면 테두리 펄스
        if (isQuestTarget) {
            const highlight = this.add.graphics();
            highlight.lineStyle(2, 0x44ff44, 0.6);
            highlight.strokeRoundedRect(cx - w / 2 - 1, cy - 1, w + 2, 122, 6);
            this.tweens.add({
                targets: highlight, alpha: 0.15,
                yoyo: true, repeat: -1, duration: 1000, ease: 'Sine.easeInOut'
            });
        }

        this.add.text(cx, cy + 24, fac.icon, {
            fontSize: isUnlocked ? '22px' : '18px'
        }).setOrigin(0.5).setAlpha(isUnlocked ? 1 : 0.35);

        this.add.text(cx, cy + 50, fac.name, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: isUnlocked ? (T.textPrimary || '#e8d8c0') : (T.textMuted || '#887860'),
            fontStyle: 'bold', align: 'center',
            wordWrap: { width: w - 4 }
        }).setOrigin(0.5);

        if (!isUnlocked) {
            if (canUnlock) {
                this.add.text(cx, cy + 75, `해금`, {
                    fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
                    color: T.textGold || '#ffcc44'
                }).setOrigin(0.5);
                this.add.text(cx, cy + 88, fac.cost > 0 ? `${fac.cost}G` : '무료', {
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

        const active = gs.activeExpeditions || [];
        const pending = gs.pendingResults || [];

        if (active.length > 0 || pending.length > 0) {
            this._drawExpeditionMini(active, pending);
        }

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
