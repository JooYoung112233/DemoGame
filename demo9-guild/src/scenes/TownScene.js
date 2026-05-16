class TownScene extends Phaser.Scene {
    constructor() { super('TownScene'); }

    init(data) {
        this.gameState = data.gameState;
    }

    create() {
        const gs = this.gameState;

        // === 세이브 마이그레이션 (기존 세이브 호환) ===
        if (!gs.unlockedFacilities) gs.unlockedFacilities = [];
        let migrated = false;
        ['equipment', 'guildHall'].forEach(f => {
            if (!gs.unlockedFacilities.includes(f)) {
                gs.unlockedFacilities.push(f);
                migrated = true;
            }
        });
        // 길드 회관 데이터 초기화
        if (!gs.guildHall) {
            gs.guildHall = { operations: 0, infrastructure: 0, recovery: 0, automation: 0,
                             intel: 0, pit_control: 0, cargo_control: 0, dark_control: 0 };
            migrated = true;
        }
        if (gs.guildReputation === undefined) {
            gs.guildReputation = 0;
            migrated = true;
        }
        if (migrated && typeof SaveManager !== 'undefined') SaveManager.save(gs);

        // 파견 완료 처리 (마을 진입 시)
        if (typeof ExpeditionManager !== 'undefined') {
            const newCompleted = ExpeditionManager.processCompleted(gs);
            if (newCompleted.length > 0) {
                GuildManager.addMessage(gs, `🎁 파견 ${newCompleted.length}건 완료 — 수령 대기`);
            }
        }

        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
        this._drawHeader();
        this._drawRosterPanel();
        this._drawExpeditionPanel();
        this._drawFacilityGrid();
        this._drawMessageLog();

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

    _drawExpeditionPanel() {
        const gs = this.gameState;
        const active = gs.activeExpeditions || [];
        const pending = gs.pendingResults || [];
        if (active.length === 0 && pending.length === 0) return;

        const panelX = 875, panelY = 110, panelW = 390, panelH = 175;
        const bg = this.add.graphics();
        bg.fillStyle(0x111125, 1);
        bg.fillRoundedRect(panelX, panelY, panelW, panelH, 5);
        bg.lineStyle(1, 0x4488cc, 0.7);
        bg.strokeRoundedRect(panelX, panelY, panelW, panelH, 5);

        const maxSlots = ExpeditionManager.getMaxSlots(gs);
        this.add.text(panelX + 10, panelY + 8, `📦 서브 파견 ${active.length}/${maxSlots}`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#88ccff', fontStyle: 'bold'
        });
        if (pending.length > 0) {
            this.add.text(panelX + panelW - 10, panelY + 8, `🎁 수령 대기: ${pending.length}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
            }).setOrigin(1, 0);
        }

        // 활성 파견 목록
        let cy = panelY + 30;
        active.slice(0, 3).forEach(exp => {
            const zone = ZONE_DATA[exp.zoneKey];
            const progress = ExpeditionManager.getProgress(exp);
            const remainSec = Math.ceil(ExpeditionManager.getRemainingMs(exp) / 1000);
            const mins = Math.floor(remainSec / 60);
            const secs = remainSec % 60;
            const timeStr = `${mins}:${secs.toString().padStart(2, '0')}`;

            this.add.text(panelX + 10, cy, `${zone.icon} ${zone.name} Lv.${exp.zoneLevel}`, {
                fontSize: '11px', fontFamily: 'monospace', color: zone.textColor
            });
            this.add.text(panelX + panelW - 10, cy, timeStr, {
                fontSize: '11px', fontFamily: 'monospace', color: '#aaaacc'
            }).setOrigin(1, 0);
            // 진행 바
            const barX = panelX + 10, barW = panelW - 20;
            const barBg = this.add.graphics();
            barBg.fillStyle(0x222244, 1);
            barBg.fillRoundedRect(barX, cy + 16, barW, 5, 2);
            barBg.fillStyle(zone.color, 1);
            barBg.fillRoundedRect(barX, cy + 16, barW * progress, 5, 2);
            cy += 27;
        });

        // 수령 버튼
        if (pending.length > 0) {
            UIButton.create(this, panelX + panelW / 2, panelY + panelH - 18, 200, 26, `🎁 ${pending.length}건 모두 수령`, {
                color: 0xaa8844, hoverColor: 0xccaa55, textColor: '#ffffff', fontSize: 12,
                onClick: () => {
                    const ids = pending.map(r => r.id);
                    let totalGold = 0, totalLoot = 0;
                    ids.forEach(id => {
                        const r = ExpeditionManager.collectResult(gs, id);
                        if (r) { totalGold += r.goldEarned; totalLoot += (r.loot || []).length; }
                    });
                    SaveManager.save(gs);
                    UIToast.show(this, `+${totalGold}G, 장비 ${totalLoot}개`, { color: '#ffcc44' });
                    this.scene.restart();
                }
            });
        }
    }

    _drawHeader() {
        const gs = this.gameState;

        const headerBg = this.add.graphics();
        headerBg.fillStyle(0x111122, 1);
        headerBg.fillRect(0, 0, 1280, 55);
        headerBg.lineStyle(1, 0x333355, 0.5);
        headerBg.lineBetween(0, 55, 1280, 55);

        this.add.text(20, 18, `길드 Lv.${gs.guildLevel}`, {
            fontSize: '18px', fontFamily: 'monospace', color: '#44aaff', fontStyle: 'bold'
        });

        const xpNeeded = GuildManager.getXpToNextLevel(gs);
        const xpRatio = gs.guildLevel >= 8 ? 1 : gs.guildXp / xpNeeded;
        const barX = 150, barY = 22, barW = 200, barH = 12;

        const xpBg = this.add.graphics();
        xpBg.fillStyle(0x222244, 1);
        xpBg.fillRoundedRect(barX, barY, barW, barH, 3);
        xpBg.fillStyle(0x4488ff, 1);
        xpBg.fillRoundedRect(barX, barY, barW * xpRatio, barH, 3);
        xpBg.lineStyle(1, 0x446688, 0.5);
        xpBg.strokeRoundedRect(barX, barY, barW, barH, 3);

        const xpLabel = gs.guildLevel >= 8 ? 'MAX' : `${gs.guildXp}/${xpNeeded}`;
        this.add.text(barX + barW + 8, 18, `XP: ${xpLabel}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#6688aa'
        });

        this.add.text(1260, 18, `${gs.gold}G`, {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0);

        this.add.text(1180, 20, '🪙', { fontSize: '14px' }).setOrigin(1, 0);

        const maxRoster = GuildManager.getMaxRoster(gs);
        this.add.text(640, 18, `용병 ${gs.roster.length}/${maxRoster}  |  런 #${gs.runCount}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5, 0);

        // === 단체 휴식 버튼 — 전 용병 스테미너 회복 (10G × 로스터 수) ===
        const restCost = gs.roster.length * 10;
        const needsRest = gs.roster.some(m => m.alive && (m.stamina || 0) < 100);
        const canAfford = gs.gold >= restCost;
        // 평균 스테미너 표시
        const aliveMercs = gs.roster.filter(m => m.alive);
        const avgStamina = aliveMercs.length > 0
            ? Math.round(aliveMercs.reduce((s, m) => s + (m.stamina || 100), 0) / aliveMercs.length)
            : 100;
        UIButton.create(this, 1075, 32, 110, 28, `🛌 휴식 ${restCost}G`, {
            color: needsRest && canAfford ? 0x336655 : 0x333344,
            hoverColor: 0x448866,
            textColor: needsRest && canAfford ? '#aaffcc' : '#666677',
            fontSize: 11,
            disabled: !needsRest || !canAfford,
            onClick: () => this._restAllMercs(restCost)
        });
        // 평균 스테미너 라벨 (휴식 버튼 옆)
        this.add.text(1010, 34, `⚡${avgStamina}%`, {
            fontSize: '11px', fontFamily: 'monospace',
            color: avgStamina >= 70 ? '#88ccff' : avgStamina >= 40 ? '#ffaa44' : '#ff6666',
            fontStyle: 'bold'
        }).setOrigin(1, 0.5);
    }

    _restAllMercs(cost) {
        const gs = this.gameState;
        if (!GuildManager.spendGold(gs, cost)) return;
        gs.roster.forEach(m => {
            if (m.alive && typeof m.restStamina === 'function') m.restStamina(100);
        });
        GuildManager.addMessage(gs, `🛌 단체 휴식 — 모든 용병 스테미너 회복 (-${cost}G)`);
        SaveManager.save(gs);
        this.scene.restart({ gameState: gs });
    }

    _drawRosterPanel() {
        const gs = this.gameState;
        // 제목 없는 panel — 커스텀 헤더로 대체
        const panel = UIPanel.create(this, 8, 65, 265, 640, {});

        // 커스텀 헤더 — 좌측 제목 + 우측 버튼들
        this.add.text(20, 76, '로스터', {
            fontSize: '13px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
        });

        if (gs.roster.length === 0) {
            this.add.text(140, 200, '용병이 없습니다\n모집소에서 고용하세요', {
                fontSize: '12px', fontFamily: 'monospace', color: '#555566', align: 'center'
            }).setOrigin(0.5);
            return;
        }

        // 패널 헤더 우측 버튼들
        UIButton.create(this, 168, 80, 46, 22, '전체', {
            color: 0x335577, hoverColor: 0x446688, textColor: '#cceeff', fontSize: 10,
            onClick: () => this.scene.start('RosterScene', { gameState: gs })
        });
        UIButton.create(this, 212, 80, 38, 22, '💞', {
            color: 0x553355, hoverColor: 0x664466, textColor: '#ffccee', fontSize: 12,
            onClick: () => this.scene.start('BondScene', { gameState: gs })
        });
        UIButton.create(this, 252, 80, 38, 22, '✨', {
            color: 0x443366, hoverColor: 0x554477, textColor: '#ccaaff', fontSize: 12,
            onClick: () => this.scene.start('SynergyScene', { gameState: gs, returnTo: 'TownScene', returnData: { gameState: gs } })
        });

        // === 스크롤 가능한 로스터 목록 ===
        // 영역: y=95 ~ y=700 (605px 가용)
        // 카드 80px × N개. 8개 이상이면 스크롤 필요.
        this._rosterScrollAreaY = 95;
        this._rosterScrollAreaH = 605;
        this._rosterCardH = 80;
        if (this._rosterScrollY === undefined) this._rosterScrollY = 0;

        // 마스크 — 로스터 카드 영역 클립
        const maskShape = this.make.graphics({ x: 0, y: 0, add: false });
        maskShape.fillStyle(0xffffff, 1);
        maskShape.fillRect(13, this._rosterScrollAreaY, 260, this._rosterScrollAreaH);
        this._rosterMask = maskShape.createGeometryMask();

        this._renderRosterCards();

        // === 마우스 휠 — 로스터 영역에서만 스크롤 (scene.restart 없이 부분 갱신) ===
        // 기존 핸들러 제거 (씬 첫 진입 외 안전)
        this.input.off('wheel', this._onRosterWheel, this);
        this._onRosterWheel = (pointer, _gameObjects, _dx, dy) => {
            if (pointer.x > 8 && pointer.x < 273
                && pointer.y > this._rosterScrollAreaY
                && pointer.y < this._rosterScrollAreaY + this._rosterScrollAreaH) {
                const totalH = this.gameState.roster.length * this._rosterCardH;
                const maxScroll = Math.max(0, totalH - this._rosterScrollAreaH);
                this._rosterScrollY = Math.max(0, Math.min(maxScroll, this._rosterScrollY + dy * 0.5));
                this._renderRosterCards();
            }
        };
        this.input.on('wheel', this._onRosterWheel, this);
    }

    /** 로스터 카드 + 스크롤바 부분 렌더링 — wheel 이벤트 등에서 재호출 */
    _renderRosterCards() {
        const gs = this.gameState;
        // 기존 카드/스크롤바 제거
        if (this._rosterRenderObjs) {
            this._rosterRenderObjs.forEach(o => o.destroy && o.destroy());
        }
        this._rosterRenderObjs = [];

        const scrollAreaY = this._rosterScrollAreaY;
        const scrollAreaH = this._rosterScrollAreaH;
        const cardH = this._rosterCardH;
        const totalH = gs.roster.length * cardH;
        const maxScroll = Math.max(0, totalH - scrollAreaH);
        if (this._rosterScrollY > maxScroll) this._rosterScrollY = maxScroll;

        // 영역 안에 들어오는 카드만 렌더
        gs.roster.forEach((merc, idx) => {
            const cardY = scrollAreaY + idx * cardH - this._rosterScrollY;
            if (cardY < scrollAreaY - cardH || cardY > scrollAreaY + scrollAreaH) return;
            const objs = this._drawMercCard(merc, 18, cardY, 245);
            if (Array.isArray(objs)) {
                objs.forEach(o => { if (o && this._rosterMask) o.setMask(this._rosterMask); });
                this._rosterRenderObjs.push(...objs);
            }
        });

        // 스크롤바
        if (maxScroll > 0) {
            const trackX = 264, trackY = scrollAreaY, trackH = scrollAreaH;
            const track = this.add.graphics();
            track.fillStyle(0x222233, 0.6);
            track.fillRoundedRect(trackX, trackY, 6, trackH, 3);
            this._rosterRenderObjs.push(track);

            const thumbH = Math.max(30, trackH * (scrollAreaH / totalH));
            const thumbY = trackY + (this._rosterScrollY / maxScroll) * (trackH - thumbH);
            const thumb = this.add.graphics();
            thumb.fillStyle(0x6688aa, 0.9);
            thumb.fillRoundedRect(trackX, thumbY, 6, thumbH, 3);
            this._rosterRenderObjs.push(thumb);
        }
    }

    _drawMercCard(merc, x, y, width) {
        const base = merc.getBaseClass();
        const rarity = RARITY_DATA[merc.rarity];
        const stats = merc.getStats();
        const hpRatio = merc.currentHp / stats.hp;
        const objs = [];

        const cardBg = this.add.graphics();
        cardBg.fillStyle(0x1a1a2e, 1);
        cardBg.fillRoundedRect(x, y, width, 70, 3);
        cardBg.lineStyle(1, rarity.color, 0.4);
        cardBg.strokeRoundedRect(x, y, width, 70, 3);
        objs.push(cardBg);

        objs.push(this.add.text(x + 8, y + 6, `${base.icon} ${merc.name}`, {
            fontSize: '12px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }));

        objs.push(this.add.text(x + width - 8, y + 6, `Lv.${merc.level}`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa'
        }).setOrigin(1, 0));

        objs.push(this.add.text(x + 8, y + 24, `${base.name} [${rarity.name}]`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#777788'
        }));

        objs.push(this.add.text(x + 8, y + 40, `HP:${merc.currentHp}/${stats.hp} ATK:${stats.atk} DEF:${stats.def}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#8888aa'
        }));

        const barW = width - 16;
        const barH = 4;
        const barY = y + 50;
        const hpBar = this.add.graphics();
        hpBar.fillStyle(0x333344, 1);
        hpBar.fillRect(x + 8, barY, barW, barH);
        const hpColor = hpRatio > 0.6 ? 0x44ff88 : hpRatio > 0.3 ? 0xffaa44 : 0xff4444;
        hpBar.fillStyle(hpColor, 1);
        hpBar.fillRect(x + 8, barY, barW * hpRatio, barH);
        objs.push(hpBar);

        // 스테미너 바 (HP 바로 아래)
        const stamina = merc.stamina !== undefined ? merc.stamina : 100;
        const staminaRatio = stamina / (merc.maxStamina || 100);
        const staminaBar = this.add.graphics();
        staminaBar.fillStyle(0x223344, 1);
        staminaBar.fillRect(x + 8, barY + 6, barW, 3);
        const stColor = staminaRatio > 0.6 ? 0x44ccff : staminaRatio > 0.3 ? 0xffaa44 : 0xff6666;
        staminaBar.fillStyle(stColor, 1);
        staminaBar.fillRect(x + 8, barY + 6, barW * staminaRatio, 3);
        objs.push(staminaBar);
        objs.push(this.add.text(x + width - 10, barY + 6, `⚡${Math.round(stamina)}`, {
            fontSize: '8px', fontFamily: 'monospace', color: '#88ccdd'
        }).setOrigin(1, 0));

        const traitText = merc.traits.map(t => {
            const sym = t.type === 'positive' ? '✦' : t.type === 'legendary' ? '★' : '✧';
            return sym;
        }).join('');
        if (traitText) {
            objs.push(this.add.text(x + width - 8, y + 24, traitText, {
                fontSize: '10px', fontFamily: 'monospace', color: '#aaaaaa'
            }).setOrigin(1, 0));
        }

        const hitZone = this.add.zone(x + width / 2, y + 35, width, 70).setInteractive({ useHandCursor: true });
        objs.push(hitZone);
        hitZone.on('pointerover', () => {
            cardBg.clear();
            cardBg.fillStyle(0x2a2a4a, 1);
            cardBg.fillRoundedRect(x, y, width, 70, 3);
            cardBg.lineStyle(2, 0x88ccff, 0.8);
            cardBg.strokeRoundedRect(x, y, width, 70, 3);
            const lines = [
                `${base.icon} ${merc.name} [${rarity.name} ${base.name}]`,
                `Lv.${merc.level}  XP: ${merc.xp}/${merc.level >= 10 ? 'MAX' : merc.getXpToNextLevel()}`,
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
            lines.push('---', '🖱 클릭 — 상세/장비/해고');
            UITooltip.show(this, x + width + 5, y, lines);
        });
        hitZone.on('pointerout', () => {
            cardBg.clear();
            cardBg.fillStyle(0x1a1a2e, 1);
            cardBg.fillRoundedRect(x, y, width, 70, 3);
            cardBg.lineStyle(1, rarity.color, 0.4);
            cardBg.strokeRoundedRect(x, y, width, 70, 3);
            UITooltip.hide(this);
        });
        hitZone.on('pointerdown', () => {
            UITooltip.hide(this);
            this.scene.start('RosterScene', { gameState: this.gameState, selectedMercId: merc.id });
        });

        return objs;
    }

    _drawFacilityGrid() {
        const gs = this.gameState;
        // 중앙 영역 — 우측 패널(x=815)과 겹치지 않게 폭 제한
        const panelX = 285, panelW = 515;

        // === 1) 출발 게이트 — 큰 메인 카드 ===
        this._drawGateCard(panelX, 75, panelW, 110);

        // === 2) 카테고리별 시설 그리드 ===
        const groups = [
            { title: '🏛 길드 운영',  color: '#ffcc66', facilities: ['guildHall', 'recruit', 'eliteRecruit'] },
            { title: '⚒ 용병 관리',  color: '#aaccff', facilities: ['equipment', 'training', 'temple'] },
            { title: '💰 경제',       color: '#88ccaa', facilities: ['storage', 'forge', 'auction', 'vault'] },
            { title: '🔍 기타',       color: '#cc99ee', facilities: ['intel'] }
        ];

        const cellW = 158, cellH = 70, gap = 10;
        const cols = 3;   // 3열 고정 (4번째 아이템은 자동 wrap)
        let curY = 195;

        groups.forEach(group => {
            this.add.text(panelX, curY, group.title, {
                fontSize: '12px', fontFamily: 'monospace', color: group.color, fontStyle: 'bold'
            });
            curY += 16;

            const rowStartY = curY;
            group.facilities.forEach((key, idx) => {
                const col = idx % cols;
                const row = Math.floor(idx / cols);
                const x = panelX + col * (cellW + gap);
                const y = rowStartY + row * (cellH + gap);
                this._drawFacilityCell(key, x, y, cellW, cellH);
            });
            const rowsUsed = Math.ceil(group.facilities.length / cols);
            curY += rowsUsed * (cellH + gap) + 4;
        });
    }

    /** 출발 게이트 — 큰 메인 카드 (좁은 폭 대응) */
    _drawGateCard(x, y, w, h) {
        const gs = this.gameState;
        const isUnlocked = gs.unlockedFacilities.includes('gate');

        const bg = this.add.graphics();
        bg.fillGradientStyle(0x442211, 0x442211, 0x664422, 0x664422, 1);
        bg.fillRoundedRect(x, y, w, h, 10);
        bg.lineStyle(3, 0xffaa44, 0.9);
        bg.strokeRoundedRect(x, y, w, h, 10);

        // 좌측 아이콘
        this.add.text(x + 32, y + h/2, '🚪', { fontSize: '44px' }).setOrigin(0.5);

        // 중앙 텍스트
        this.add.text(x + 66, y + 14, '출발 게이트', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffe088', fontStyle: 'bold'
        });
        this.add.text(x + 66, y + 38, '메인 도전 / 서브 파견', {
            fontSize: '11px', fontFamily: 'monospace', color: '#ffccaa'
        });

        // 활성 파견 / 수령 대기 (중앙 하단)
        const activeExp = (gs.activeExpeditions || []).length;
        const maxSlots = (typeof ExpeditionManager !== 'undefined') ? ExpeditionManager.getMaxSlots(gs) : 0;
        const pending = (gs.pendingResults || []).length;

        let statusText = `서브 ${activeExp}/${maxSlots}`;
        if (pending > 0) statusText += `  |  🎁 ${pending}건 수령`;
        this.add.text(x + 66, y + h - 18, statusText, {
            fontSize: '10px', fontFamily: 'monospace',
            color: pending > 0 ? '#ffcc44' : '#88ccff'
        });

        // 큰 ▶ 출발 버튼 (우측)
        const btnW = 120, btnH = 60;
        const btnX = x + w - btnW - 12, btnY = y + (h - btnH) / 2;
        const btnBg = this.add.graphics();
        btnBg.fillStyle(0xff8833, 1);
        btnBg.fillRoundedRect(btnX, btnY, btnW, btnH, 8);
        btnBg.lineStyle(2, 0xffcc66, 0.9);
        btnBg.strokeRoundedRect(btnX, btnY, btnW, btnH, 8);
        this.add.text(btnX + btnW/2, btnY + btnH/2, '▶\n출발', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold',
            stroke: '#000', strokeThickness: 2, align: 'center'
        }).setOrigin(0.5);

        // 전체 카드 클릭 → 출발
        const hit = this.add.zone(x + w/2, y + h/2, w, h).setInteractive({ useHandCursor: true });
        hit.on('pointerdown', () => this._onFacilityClick('gate'));
        hit.on('pointerover', () => {
            bg.clear();
            bg.fillGradientStyle(0x553322, 0x553322, 0x885533, 0x885533, 1);
            bg.fillRoundedRect(x, y, w, h, 10);
            bg.lineStyle(3, 0xffcc66, 1);
            bg.strokeRoundedRect(x, y, w, h, 10);
        });
        hit.on('pointerout', () => {
            bg.clear();
            bg.fillGradientStyle(0x442211, 0x442211, 0x664422, 0x664422, 1);
            bg.fillRoundedRect(x, y, w, h, 10);
            bg.lineStyle(3, 0xffaa44, 0.9);
            bg.strokeRoundedRect(x, y, w, h, 10);
        });
    }

    _drawFacilityCell(key, x, y, w, h) {
        const gs = this.gameState;
        const fac = FACILITY_DATA[key];
        const isUnlocked = gs.unlockedFacilities.includes(key);
        const canUnlock = GuildManager.canUnlockFacility(gs, key);
        const levelReached = gs.guildLevel >= fac.unlockLevel;

        const bg = this.add.graphics();
        if (isUnlocked) {
            bg.fillStyle(0x1a2a3a, 1);
            bg.fillRoundedRect(x, y, w, h, 5);
            bg.lineStyle(1, 0x446688, 0.7);
            bg.strokeRoundedRect(x, y, w, h, 5);
        } else {
            bg.fillStyle(0x181822, 1);
            bg.fillRoundedRect(x, y, w, h, 5);
            bg.lineStyle(1, 0x333344, 0.5);
            bg.strokeRoundedRect(x, y, w, h, 5);
        }

        // 좌측 아이콘
        const iconSize = isUnlocked ? '26px' : '20px';
        this.add.text(x + 24, y + h/2, fac.icon, { fontSize: iconSize }).setOrigin(0.5);

        // 우측 텍스트
        this.add.text(x + 48, y + 10, fac.name, {
            fontSize: '12px', fontFamily: 'monospace',
            color: isUnlocked ? '#ccccee' : '#555566', fontStyle: 'bold'
        });

        if (!isUnlocked) {
            if (canUnlock) {
                this.add.text(x + 48, y + 30, `해금 ${fac.cost}G`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#ffaa44'
                });
            } else if (levelReached) {
                this.add.text(x + 48, y + 30, `${fac.cost}G 필요`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#884444'
                });
            } else {
                this.add.text(x + 48, y + 30, `Lv.${fac.unlockLevel} 필요`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#555566'
                });
            }

            if (!canUnlock) {
                this.add.text(x + w - 14, y + h - 14, '🔒', {
                    fontSize: '14px'
                }).setOrigin(1, 1).setAlpha(0.5);
            }
        } else {
            this.add.text(x + 48, y + 28, fac.desc, {
                fontSize: '9px', fontFamily: 'monospace', color: '#778899',
                wordWrap: { width: w - 56 }
            });
        }

        const hitZone = this.add.zone(x + w / 2, y + h / 2, w, h).setInteractive({ useHandCursor: true });
        hitZone.on('pointerdown', () => this._onFacilityClick(key));

        hitZone.on('pointerover', () => {
            bg.clear();
            if (isUnlocked) {
                bg.fillStyle(0x223344, 1);
                bg.fillRoundedRect(x, y, w, h, 5);
                bg.lineStyle(2, 0x5588aa, 0.9);
                bg.strokeRoundedRect(x, y, w, h, 5);
            } else if (canUnlock) {
                bg.fillStyle(0x222233, 1);
                bg.fillRoundedRect(x, y, w, h, 5);
                bg.lineStyle(2, 0xffaa44, 0.7);
                bg.strokeRoundedRect(x, y, w, h, 5);
            }
        });
        hitZone.on('pointerout', () => {
            bg.clear();
            if (isUnlocked) {
                bg.fillStyle(0x1a2a3a, 1);
                bg.fillRoundedRect(x, y, w, h, 5);
                bg.lineStyle(1, 0x446688, 0.7);
                bg.strokeRoundedRect(x, y, w, h, 5);
            } else {
                bg.fillStyle(0x181822, 1);
                bg.fillRoundedRect(x, y, w, h, 5);
                bg.lineStyle(1, 0x333344, 0.5);
                bg.strokeRoundedRect(x, y, w, h, 5);
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
                UIToast.show(this, `길드 Lv.${fac.unlockLevel} 필요`, { color: '#ff6666' });
            } else {
                UIToast.show(this, `골드가 부족합니다 (${fac.cost}G 필요)`, { color: '#ff6666' });
            }
            return;
        }

        if (fac.scene) {
            this.scene.start(fac.scene, { gameState: gs });
        } else {
            UIToast.show(this, `${fac.name} — 준비 중...`, { color: '#888899' });
        }
    }

    _drawMessageLog() {
        const gs = this.gameState;
        const panelX = 815;
        UIPanel.create(this, panelX, 65, 457, 420, { title: '최근 소식' });

        const messages = gs.messages || [];
        const display = messages.slice(0, 12);

        display.forEach((msg, idx) => {
            const alpha = 1 - idx * 0.05;
            this.add.text(panelX + 12, 95 + idx * 20, `• ${msg}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#8888aa'
            }).setAlpha(Math.max(0.3, alpha));
        });

        if (messages.length === 0) {
            this.add.text(panelX + 228, 250, '아직 소식이 없습니다', {
                fontSize: '12px', fontFamily: 'monospace', color: '#444455'
            }).setOrigin(0.5);
        }

        if (typeof MERCHANT_DATA !== 'undefined') {
            UIPanel.create(this, panelX, 495, 457, 210, { title: '상인 호감도' });
            if (typeof initMerchantFavor === 'function') initMerchantFavor(gs);
            let my = 525;
            for (const [key, data] of Object.entries(MERCHANT_DATA)) {
                const favor = gs.merchantFavor?.[key] || 0;
                const tier = typeof getMerchantTier === 'function' ? getMerchantTier(gs, key) : null;
                const tierName = tier ? tier.name : '?';
                const barW = 120, barH = 5;
                const ratio = Math.min(1, favor / 10);

                this.add.text(panelX + 12, my, `${data.icon} ${data.name}`, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
                });
                this.add.text(panelX + 100, my, `[${tierName}]`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#888899'
                });

                const barBg = this.add.rectangle(panelX + 250 + barW / 2, my + 7, barW, barH, 0x222244);
                if (ratio > 0) {
                    this.add.rectangle(panelX + 250 + (barW * ratio) / 2, my + 7, barW * ratio, barH, 0x44aaff);
                }
                this.add.text(panelX + 250 + barW + 8, my + 1, `${favor}`, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#667788'
                });

                if (tier && tier.perks) {
                    this.add.text(panelX + 12, my + 16, tier.perks, {
                        fontSize: '8px', fontFamily: 'monospace', color: '#669966'
                    });
                }
                my += 36;
            }
        }
    }
}
