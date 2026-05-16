class TownScene extends Phaser.Scene {
    constructor() { super('TownScene'); }

    init(data) {
        this.gameState = data.gameState;
    }

    create() {
        const gs = this.gameState;

        // === 세이브 마이그레이션 (기존 세이브 호환) ===
        if (!gs.unlockedFacilities) gs.unlockedFacilities = [];
        if (!gs.unlockedFacilities.includes('equipment')) {
            gs.unlockedFacilities.push('equipment');
            if (typeof SaveManager !== 'undefined') SaveManager.save(gs);
        }

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
    }

    _drawRosterPanel() {
        const gs = this.gameState;
        const panel = UIPanel.create(this, 8, 65, 265, 640, { title: '로스터' });

        if (gs.roster.length === 0) {
            this.add.text(140, 200, '용병이 없습니다\n모집소에서 고용하세요', {
                fontSize: '12px', fontFamily: 'monospace', color: '#555566', align: 'center'
            }).setOrigin(0.5);
            return;
        }

        let yOff = 95;
        gs.roster.forEach((merc, idx) => {
            if (yOff > 670) return;
            this._drawMercCard(merc, 18, yOff, 245);
            yOff += 80;
        });
    }

    _drawMercCard(merc, x, y, width) {
        const base = merc.getBaseClass();
        const rarity = RARITY_DATA[merc.rarity];
        const stats = merc.getStats();
        const hpRatio = merc.currentHp / stats.hp;

        const cardBg = this.add.graphics();
        cardBg.fillStyle(0x1a1a2e, 1);
        cardBg.fillRoundedRect(x, y, width, 70, 3);
        cardBg.lineStyle(1, rarity.color, 0.4);
        cardBg.strokeRoundedRect(x, y, width, 70, 3);

        this.add.text(x + 8, y + 6, `${base.icon} ${merc.name}`, {
            fontSize: '12px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        });

        this.add.text(x + width - 8, y + 6, `Lv.${merc.level}`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa'
        }).setOrigin(1, 0);

        this.add.text(x + 8, y + 24, `${base.name} [${rarity.name}]`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#777788'
        });

        this.add.text(x + 8, y + 40, `HP:${merc.currentHp}/${stats.hp} ATK:${stats.atk} DEF:${stats.def}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#8888aa'
        });

        const barW = width - 16;
        const barH = 4;
        const barY = y + 56;
        const hpBar = this.add.graphics();
        hpBar.fillStyle(0x333344, 1);
        hpBar.fillRect(x + 8, barY, barW, barH);
        const hpColor = hpRatio > 0.6 ? 0x44ff88 : hpRatio > 0.3 ? 0xffaa44 : 0xff4444;
        hpBar.fillStyle(hpColor, 1);
        hpBar.fillRect(x + 8, barY, barW * hpRatio, barH);

        const traitText = merc.traits.map(t => {
            const sym = t.type === 'positive' ? '✦' : t.type === 'legendary' ? '★' : '✧';
            return sym;
        }).join('');
        if (traitText) {
            this.add.text(x + width - 8, y + 24, traitText, {
                fontSize: '10px', fontFamily: 'monospace', color: '#aaaaaa'
            }).setOrigin(1, 0);
        }

        const hitZone = this.add.zone(x + width / 2, y + 35, width, 70).setInteractive({ useHandCursor: true });
        hitZone.on('pointerover', () => {
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
            UITooltip.show(this, x + width + 5, y, lines);
        });
        hitZone.on('pointerout', () => UITooltip.hide(this));
    }

    _drawFacilityGrid() {
        const gs = this.gameState;
        const startX = 350;
        const startY = 85;
        const cellW = 130;
        const cellH = 95;
        const gap = 15;
        const cols = 3;

        const facilityOrder = ['recruit', 'storage', 'gate', 'forge', 'auction', 'training', 'temple', 'intel', 'eliteRecruit', 'vault'];

        this.add.text(startX + (cols * (cellW + gap) - gap) / 2, 68, '시설', {
            fontSize: '13px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
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

        const iconSize = isUnlocked ? '24px' : '20px';
        this.add.text(x + w / 2, y + 20, fac.icon, { fontSize: iconSize }).setOrigin(0.5);

        this.add.text(x + w / 2, y + 45, fac.name, {
            fontSize: '12px', fontFamily: 'monospace',
            color: isUnlocked ? '#ccccee' : '#555566', fontStyle: 'bold'
        }).setOrigin(0.5);

        if (!isUnlocked) {
            if (canUnlock) {
                this.add.text(x + w / 2, y + 63, `해금 (${fac.cost}G)`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#ffaa44'
                }).setOrigin(0.5);
            } else if (levelReached) {
                this.add.text(x + w / 2, y + 63, `${fac.cost}G 필요`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#884444'
                }).setOrigin(0.5);
            } else {
                this.add.text(x + w / 2, y + 63, `Lv.${fac.unlockLevel} 필요`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#555566'
                }).setOrigin(0.5);
            }

            if (!isUnlocked && !canUnlock) {
                this.add.text(x + w / 2, y + h / 2, '🔒', {
                    fontSize: '16px'
                }).setOrigin(0.5).setAlpha(0.3);
            }
        } else {
            this.add.text(x + w / 2, y + 63, fac.desc, {
                fontSize: '9px', fontFamily: 'monospace', color: '#667788',
                wordWrap: { width: w - 10 }, align: 'center'
            }).setOrigin(0.5);
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
