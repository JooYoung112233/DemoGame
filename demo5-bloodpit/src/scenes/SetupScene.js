class BPSetupScene extends Phaser.Scene {
    constructor() { super('BPSetupScene'); }

    create() {
        BP_FARMING.init();
        BP_ROSTER.init();
        this.cameras.main.setBackgroundColor('#1a0a0a');
        this.selectedParty = [];
        this._autoSelectParty();
        this._drawAll();
    }

    _autoSelectParty() {
        const available = BP_ROSTER.getAvailable();
        this.selectedParty = available.slice(0, 4).map(c => c.id);
    }

    _drawAll() {
        this.children.removeAll();
        FarmingUI.hideTooltip();
        this._drawHeader();
        this._drawRoster();
        this._drawParty();
        this._drawButtons();
    }

    _drawHeader() {
        const cx = 640;
        this.add.text(cx, 18, '💀 BLOOD PIT', {
            fontSize: '28px', fontFamily: 'monospace', color: '#ff2244', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.add.text(cx, 45, `💰 ${StashManager.getGold()}G  |  📦 ${StashManager.getStash().length}/${FARMING.STASH_CAPACITY}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffcc44'
        }).setOrigin(0.5);
        const stats = StashManager.getStats();
        this.add.text(cx, 64, `레이드 ${stats.raids}  |  탈출 ${stats.extractions}  |  사망 ${stats.deaths}  |  최고 ${stats.bestValue}G`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#776655'
        }).setOrigin(0.5);
    }

    _drawRoster() {
        const startX = 20;
        const headerY = 78;
        const colW = 245;
        const rowH = 80;
        const gap = 5;

        this.add.text(startX + colW + gap / 2, headerY, `── 로스터 (${BP_ROSTER.roster.length}/${BP_ROSTER.maxRoster}) ──`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#aa6666'
        }).setOrigin(0.5);

        BP_ROSTER.roster.forEach((char, i) => {
            const col = i % 2;
            const row = Math.floor(i / 2);
            const x = startX + col * (colW + gap) + colW / 2;
            const y = headerY + 24 + row * (rowH + gap) + rowH / 2;

            const inPartyIdx = this.selectedParty.indexOf(char.id);
            const isAvailable = char.status === 'ready';
            const base = BP_ALLIES[char.classKey];

            let bgColor = 0x221111;
            let borderColor = 0x442222;
            if (inPartyIdx >= 0) { bgColor = 0x112233; borderColor = 0x4488ff; }
            else if (!isAvailable) { bgColor = 0x1a1a1a; borderColor = 0x333333; }

            const card = this.add.rectangle(x, y, colW, rowH, bgColor)
                .setStrokeStyle(2, borderColor);

            if (isAvailable) {
                card.setInteractive();
                card.on('pointerover', () => card.setFillStyle(inPartyIdx >= 0 ? 0x1a3344 : 0x331818));
                card.on('pointerout', () => card.setFillStyle(bgColor));
                card.on('pointerdown', () => this._togglePartyMember(char.id));
            }

            if (inPartyIdx >= 0) {
                const badge = this.add.circle(x - colW / 2 + 14, y - rowH / 2 + 14, 10, 0x4488ff);
                this.add.text(x - colW / 2 + 14, y - rowH / 2 + 14, `${inPartyIdx + 1}`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
                }).setOrigin(0.5);
            }

            const nameColor = isAvailable ? (char.gradeColor || '#ffffff') : '#666666';
            const gradeTag = char.gradeLabel && char.grade !== 'common' ? `[${char.gradeLabel}] ` : '';
            this.add.text(x, y - 22, `${gradeTag}${base.name} ${char.name}`, {
                fontSize: '13px', fontFamily: 'monospace', color: nameColor, fontStyle: 'bold'
            }).setOrigin(0.5);

            const roleIcon = { tank: '🛡️', dps: '⚔️', healer: '💚' }[base.role] || '';
            const eStats = BP_ROSTER.getEffectiveStats(char);
            this.add.text(x, y + 0, `Lv.${char.level} ${roleIcon}  HP:${eStats.hp} ATK:${eStats.atk} DEF:${eStats.def}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa'
            }).setOrigin(0.5);

            if (!isAvailable) {
                const statusIcon = char.status === 'dead' ? '💀' : '🩹';
                const statusLabel = char.status === 'dead' ? '사망' : `부상(${char.recoveryRounds})`;
                this.add.text(x, y + 14, `${statusIcon} ${statusLabel}`, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#ff6644'
                }).setOrigin(0.5);

                if (char.status === 'dead') {
                    const cost = BP_ROSTER.getReviveCost(char);
                    const canAfford = StashManager.getGold() >= cost;
                    const revBtn = this.add.text(x, y + 30, `💰${cost}G 부활`, {
                        fontSize: '10px', fontFamily: 'monospace', color: canAfford ? '#ffaa44' : '#555555',
                        backgroundColor: canAfford ? '#331100' : '#1a1a1a', padding: { x: 6, y: 2 }
                    }).setOrigin(0.5);
                    if (canAfford) {
                        revBtn.setInteractive();
                        revBtn.on('pointerdown', (ptr) => {
                            ptr.event.stopPropagation();
                            BP_ROSTER.reviveCharacter(char.id, cost);
                            this._drawAll();
                        });
                    }
                }
            }

            // equipment icons
            const eqStr = ['weapon', 'armor', 'accessory'].map(slot => {
                const item = char.equipment[slot];
                if (item) {
                    const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;
                    return '■';
                }
                return '□';
            }).join(' ');
            this.add.text(x - 40, y + 22, eqStr, {
                fontSize: '11px', fontFamily: 'monospace', color: '#555555'
            }).setOrigin(0.5);

            // promotion button
            if (isAvailable) {
                const promoCheck = BP_ROSTER.canPromote(char.id);
                const promoInfo = BP_ROSTER.getPromotionInfo(char.id);
                if (promoInfo && promoCheck.can) {
                    const promoBtn = this.add.text(x + 30, y + 22, '⬆전직', {
                        fontSize: '9px', fontFamily: 'monospace', color: '#ffcc44',
                        backgroundColor: '#332200', padding: { x: 3, y: 1 }
                    }).setOrigin(0.5).setInteractive();
                    promoBtn.on('pointerover', () => promoBtn.setColor('#ffff88'));
                    promoBtn.on('pointerout', () => promoBtn.setColor('#ffcc44'));
                    promoBtn.on('pointerdown', (ptr) => {
                        ptr.event.stopPropagation();
                        this._showPromotionPopup(char);
                    });
                } else if (promoInfo && char.level >= promoInfo.minLevel && !promoCheck.can && promoCheck.reason === 'not_enough_gold') {
                    this.add.text(x + 30, y + 22, '⬆전직', {
                        fontSize: '9px', fontFamily: 'monospace', color: '#555544',
                        padding: { x: 3, y: 1 }
                    }).setOrigin(0.5);
                }
            }

            // dismiss button (small X)
            if (isAvailable && BP_ROSTER.roster.length > 1) {
                const dismissBtn = this.add.text(x + colW / 2 - 10, y + 22, '✕', {
                    fontSize: '11px', fontFamily: 'monospace', color: '#553333'
                }).setOrigin(1, 0.5).setInteractive();
                dismissBtn.on('pointerover', () => dismissBtn.setColor('#ff4444'));
                dismissBtn.on('pointerout', () => dismissBtn.setColor('#553333'));
                dismissBtn.on('pointerdown', (ptr) => {
                    ptr.event.stopPropagation();
                    this._confirmDismiss(char);
                });
            }
        });
    }

    _drawParty() {
        const startX = 520;
        const headerY = 78;
        const slotW = 420;
        const slotH = 105;
        const gap = 6;

        this.add.text(startX + slotW / 2, headerY, '── 출전 파티 ──', {
            fontSize: '13px', fontFamily: 'monospace', color: '#aa6666'
        }).setOrigin(0.5);

        for (let i = 0; i < 4; i++) {
            const y = headerY + 24 + i * (slotH + gap) + slotH / 2;
            const x = startX + slotW / 2;
            const charId = this.selectedParty[i];
            const char = charId ? BP_ROSTER.getById(charId) : null;

            this.add.rectangle(x, y, slotW, slotH, char ? 0x112233 : 0x151515)
                .setStrokeStyle(2, char ? 0x4488ff : 0x333333);

            this.add.text(startX + 8, y - slotH / 2 + 6, `${i + 1}`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#4488ff', fontStyle: 'bold'
            });

            if (!char) {
                this.add.text(x, y, '← 로스터에서 클릭하여 배치', {
                    fontSize: '13px', fontFamily: 'monospace', color: '#444444'
                }).setOrigin(0.5);
                continue;
            }

            const base = BP_ALLIES[char.classKey];
            const eStats = BP_ROSTER.getEffectiveStats(char);
            const roleLabel = { tank: '🛡️탱커', dps: '⚔️딜러', healer: '💚힐러' }[base.role] || base.role;

            this.add.text(x, y - 34, `${base.name} ${char.name}`, {
                fontSize: '16px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            }).setOrigin(0.5);
            this.add.text(x, y - 14, `Lv.${char.level}  ${roleLabel}`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#aaaaaa'
            }).setOrigin(0.5);
            this.add.text(x, y + 4, `HP:${eStats.hp}  ATK:${eStats.atk}  DEF:${eStats.def}  CRIT:${Math.floor((eStats.critRate || 0) * 100)}%`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#cccccc'
            }).setOrigin(0.5);

            // equipment slots
            const equipY = y + 28;
            const slotNames = { weapon: '⚔️무기', armor: '🛡️방어구', accessory: '💍악세' };
            const eqW = 125;
            const eqH = 26;
            const eqGap = 8;
            const eqTotalW = 3 * eqW + 2 * eqGap;
            const eqStartX = x - eqTotalW / 2 + eqW / 2;

            ['weapon', 'armor', 'accessory'].forEach((slot, si) => {
                const sx = eqStartX + si * (eqW + eqGap);
                const item = char.equipment[slot];

                const eqBg = this.add.rectangle(sx, equipY, eqW, eqH, item ? 0x222244 : 0x1a1a1a)
                    .setStrokeStyle(1, item ? 0x4444aa : 0x333333).setInteractive();

                if (item) {
                    const reg = ItemRegistry.get(item.itemId);
                    const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;
                    const enhLabel = item.enhanceLevel ? `+${item.enhanceLevel}` : '';
                    const name = reg ? reg.name : item.itemId;
                    this.add.text(sx, equipY, `${name}${enhLabel}`, {
                        fontSize: '10px', fontFamily: 'monospace', color: rarity.color
                    }).setOrigin(0.5);

                    eqBg.on('pointerover', () => {
                        eqBg.setFillStyle(0x333355);
                        if (reg) FarmingUI.showTooltip(this, { ...item, ...reg }, sx, equipY - 35);
                    });
                    eqBg.on('pointerout', () => { eqBg.setFillStyle(0x222244); FarmingUI.hideTooltip(); });
                    eqBg.on('pointerdown', () => {
                        FarmingUI.hideTooltip();
                        BP_ROSTER.unequipFromCharacter(charId, slot);
                        this._drawAll();
                    });
                } else {
                    this.add.text(sx, equipY, slotNames[slot], {
                        fontSize: '10px', fontFamily: 'monospace', color: '#555555'
                    }).setOrigin(0.5);

                    eqBg.on('pointerover', () => eqBg.setFillStyle(0x222222));
                    eqBg.on('pointerout', () => eqBg.setFillStyle(0x1a1a1a));
                    eqBg.on('pointerdown', () => this._openEquipOverlay(charId, slot));
                }
            });
        }
    }

    _drawButtons() {
        const y = 580;
        const cx = 640;

        const buttons = [
            { label: '🤝 징집', x: 100, w: 140, bg: 0x332211, bd: 0x664422, fn: () => this._openRecruit() },
            { label: '🔨 대장간', x: 260, w: 140, bg: 0x112233, bd: 0x224466, fn: () => this._openForge() },
            { label: '📦 스태시', x: 420, w: 140, bg: 0x221122, bd: 0x442244, fn: () => this._openStash() },
        ];
        buttons.forEach(b => {
            const btn = this.add.rectangle(b.x, y, b.w, 42, b.bg).setStrokeStyle(2, b.bd).setInteractive();
            this.add.text(b.x, y, b.label, { fontSize: '14px', fontFamily: 'monospace', color: '#ffffff' }).setOrigin(0.5);
            btn.on('pointerover', () => btn.setFillStyle(b.bg + 0x111111));
            btn.on('pointerout', () => btn.setFillStyle(b.bg));
            btn.on('pointerdown', b.fn);
        });

        const canStart = this.selectedParty.length > 0;
        const startBtn = this.add.rectangle(cx + 300, y, 240, 52, canStart ? 0x882222 : 0x333333)
            .setStrokeStyle(3, canStart ? 0xff4444 : 0x555555);
        if (canStart) startBtn.setInteractive();
        this.add.text(cx + 300, y, `⚔️ 출전 (${this.selectedParty.length}명)`, {
            fontSize: '18px', fontFamily: 'monospace', color: canStart ? '#ffffff' : '#666666', fontStyle: 'bold'
        }).setOrigin(0.5);
        if (canStart) {
            startBtn.on('pointerover', () => startBtn.setFillStyle(0xaa3333));
            startBtn.on('pointerout', () => startBtn.setFillStyle(0x882222));
            startBtn.on('pointerdown', () => this._startRun());
        }

        this.add.text(cx, y + 38, '20라운드 (전투+상점+대장간+이벤트)  |  사망 시 골드로 부활 필요  |  최종 보스 격파시 승리', {
            fontSize: '11px', fontFamily: 'monospace', color: '#776655'
        }).setOrigin(0.5);

        this.add.text(120, y + 58, '🗑️ 초기화', {
            fontSize: '11px', fontFamily: 'monospace', color: '#553333'
        }).setOrigin(0.5).setInteractive()
            .on('pointerdown', () => { BP_ROSTER.reset(); StashManager.reset(); this.scene.restart(); });

        this.add.text(cx + 400, y + 58, '← 목록', {
            fontSize: '11px', fontFamily: 'monospace', color: '#664444'
        }).setOrigin(0.5).setInteractive()
            .on('pointerdown', () => { window.location.href = '../index.html'; });
    }

    _togglePartyMember(charId) {
        const idx = this.selectedParty.indexOf(charId);
        if (idx >= 0) {
            this.selectedParty.splice(idx, 1);
        } else if (this.selectedParty.length < 4) {
            this.selectedParty.push(charId);
        }
        this._drawAll();
    }

    _startRun() {
        if (this.selectedParty.length === 0) return;
        FarmingUI.hideTooltip();

        const runManager = new RunManager();
        runManager.generateRunPlan();
        const firstRound = runManager.advanceRound();

        const dangerSystem = new DangerSystem();
        dangerSystem.level = firstRound.dangerLevel;

        const dropSystem = new DropSystem();

        this.scene.start('BPBattleScene', {
            party: [...this.selectedParty],
            dangerSystem,
            dropSystem,
            runManager,
            appliedDrops: []
        });
    }

    _openRecruit() {
        if (BP_ROSTER.roster.length >= BP_ROSTER.maxRoster) {
            this._showMsg('로스터가 가득 찼습니다 (10/10)');
            return;
        }

        const recruits = BP_ROSTER.generateRecruits(3);
        const popup = this.add.container(640, 360).setDepth(2000);
        popup.add(this.add.rectangle(0, 0, 1280, 720, 0x000000, 0.7).setInteractive());
        popup.add(this.add.rectangle(0, 0, 800, 340, 0x111111, 0.98).setStrokeStyle(2, 0x664422));
        popup.add(this.add.text(0, -145, '🤝 징집 — 캐릭터 고용', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5));
        popup.add(this.add.text(0, -120, `💰 ${StashManager.getGold()}G  |  로스터 ${BP_ROSTER.roster.length}/${BP_ROSTER.maxRoster}`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5));

        recruits.forEach((recruit, i) => {
            const rx = -260 + i * 260;
            const base = BP_ALLIES[recruit.classKey];
            const roleLabel = { tank: '🛡️탱커', dps: '⚔️딜러', healer: '💚힐러' }[base.role] || '';
            const gradeColor = recruit.gradeColor || '#aaaaaa';
            const gradeLabel = recruit.gradeLabel || '일반';
            const gradeBorderMap = { legendary: 0xffaa00, epic: 0xaa44ff, rare: 0x4488ff, uncommon: 0x44ff88, common: 0x888888 };
            const gradeBorder = gradeBorderMap[recruit.grade] || 0x888888;

            popup.add(this.add.rectangle(rx, 10, 230, 210, 0x221111).setStrokeStyle(3, gradeBorder));
            popup.add(this.add.rectangle(rx, -85, 226, 8, gradeBorder));
            popup.add(this.add.text(rx, -72, `[${gradeLabel}]`, {
                fontSize: '11px', fontFamily: 'monospace', color: gradeColor, fontStyle: 'bold'
            }).setOrigin(0.5));
            popup.add(this.add.text(rx, -54, base.name, {
                fontSize: '16px', fontFamily: 'monospace', color: gradeColor, fontStyle: 'bold'
            }).setOrigin(0.5));
            popup.add(this.add.text(rx, -34, `"${recruit.name}"`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#cccccc'
            }).setOrigin(0.5));
            popup.add(this.add.text(rx, -16, `Lv.${recruit.level} ${roleLabel}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#bbbbbb'
            }).setOrigin(0.5));
            popup.add(this.add.text(rx, 4, `HP:${recruit.baseStats.hp}  ATK:${recruit.baseStats.atk}  DEF:${recruit.baseStats.def}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#dddddd'
            }).setOrigin(0.5));
            popup.add(this.add.text(rx, 22, `CRIT:${Math.floor(recruit.baseStats.critRate * 100)}%  SPD:${recruit.baseStats.attackSpeed}ms`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#aaaaaa'
            }).setOrigin(0.5));
            popup.add(this.add.text(rx, 38, `출혈:${Math.floor((recruit.baseStats.bleedChance||0)*100)}%  회피:${Math.floor((recruit.baseStats.dodgeRate||0)*100)}%`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#888888'
            }).setOrigin(0.5));

            const canAfford = StashManager.getGold() >= recruit.hireCost;
            const hBtn = this.add.rectangle(rx, 68, 160, 36, canAfford ? 0x442211 : 0x222222)
                .setStrokeStyle(2, canAfford ? gradeBorder : 0x444444);
            if (canAfford) hBtn.setInteractive();
            popup.add(hBtn);
            popup.add(this.add.text(rx, 68, `💰 ${recruit.hireCost}G 고용`, {
                fontSize: '12px', fontFamily: 'monospace', color: canAfford ? '#ffaa44' : '#666666', fontStyle: 'bold'
            }).setOrigin(0.5));

            if (canAfford) {
                hBtn.on('pointerdown', () => {
                    const result = BP_ROSTER.hireCharacter(recruit);
                    if (result.success) { popup.destroy(); this._drawAll(); }
                });
                hBtn.on('pointerover', () => hBtn.setFillStyle(0x664422));
                hBtn.on('pointerout', () => hBtn.setFillStyle(0x442211));
            }
        });

        const closeBtn = this.add.rectangle(0, 140, 100, 30, 0x332222).setStrokeStyle(1, 0x664444).setInteractive();
        popup.add(closeBtn);
        popup.add(this.add.text(0, 140, '닫기', { fontSize: '12px', fontFamily: 'monospace', color: '#aa8888' }).setOrigin(0.5));
        closeBtn.on('pointerdown', () => popup.destroy());
    }

    _openForge() {
        const popup = this.add.container(640, 360).setDepth(2000);
        popup.add(this.add.rectangle(0, 0, 1280, 720, 0x000000, 0.7).setInteractive());
        popup.add(this.add.rectangle(0, 0, 900, 550, 0x111100, 0.98).setStrokeStyle(2, 0xffaa44));
        popup.add(this.add.text(0, -255, '🔨 대장간', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5));

        this._forgePopup = popup;
        this._forgeMode = 'enhance';
        this._drawForgeContent();
    }

    _drawForgeContent() {
        if (this._forgeContentGroup) this._forgeContentGroup.forEach(o => o.destroy());
        this._forgeContentGroup = [];
        const popup = this._forgePopup;
        const add = (obj) => { popup.add(obj); this._forgeContentGroup.push(obj); return obj; };

        add(this.add.text(-80, -225, `💰 ${StashManager.getGold()}G`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44'
        }));

        // tabs
        const enhTab = add(this.add.rectangle(-100, -195, 160, 30, this._forgeMode === 'enhance' ? 0x443300 : 0x222200)
            .setStrokeStyle(1, 0xffaa44).setInteractive());
        add(this.add.text(-100, -195, '⚒️ 강화', { fontSize: '13px', fontFamily: 'monospace', color: '#ffaa44' }).setOrigin(0.5));
        const craftTab = add(this.add.rectangle(100, -195, 160, 30, this._forgeMode === 'craft' ? 0x443300 : 0x222200)
            .setStrokeStyle(1, 0xffaa44).setInteractive());
        add(this.add.text(100, -195, '🔮 조합', { fontSize: '13px', fontFamily: 'monospace', color: '#ffaa44' }).setOrigin(0.5));
        enhTab.on('pointerdown', () => { this._forgeMode = 'enhance'; this._drawForgeContent(); });
        craftTab.on('pointerdown', () => { this._forgeMode = 'craft'; this._drawForgeContent(); });

        const stash = StashManager.getStash();
        const equipment = stash.filter(item => { const r = ItemRegistry.get(item.itemId); return r && r.category === 'equipment'; });

        if (this._forgeMode === 'enhance') {
            this._drawForgeEnhance(add, equipment);
        } else {
            this._drawForgeCraft(add, equipment);
        }

        const closeBtn = add(this.add.rectangle(0, 240, 120, 35, 0x332200).setStrokeStyle(1, 0x664400).setInteractive());
        add(this.add.text(0, 240, '닫기', { fontSize: '13px', fontFamily: 'monospace', color: '#aa8844' }).setOrigin(0.5));
        closeBtn.on('pointerdown', () => { this._forgePopup.destroy(); this._forgePopup = null; this._drawAll(); });
    }

    _drawForgeEnhance(add, equipment) {
        if (equipment.length === 0) {
            add(this.add.text(0, 0, '강화할 장비가 없습니다', { fontSize: '14px', fontFamily: 'monospace', color: '#886644' }).setOrigin(0.5));
            return;
        }

        add(this.add.text(0, -165, '장비를 선택하세요', { fontSize: '11px', fontFamily: 'monospace', color: '#aa8844' }).setOrigin(0.5));

        const cols = 4, cellW = 200, cellH = 45, gap = 6;
        const totalW = cols * cellW + (cols - 1) * gap;
        const sx = -totalW / 2 + cellW / 2;

        equipment.forEach((item, i) => {
            if (i >= 16) return;
            const col = i % cols;
            const row = Math.floor(i / cols);
            const x = sx + col * (cellW + gap);
            const y = -135 + row * (cellH + gap);
            const reg = ItemRegistry.get(item.itemId) || {};
            const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;
            const enhLevel = item.enhanceLevel || 0;

            const card = add(this.add.rectangle(x, y, cellW, cellH, 0x221100).setStrokeStyle(1, rarity.borderColor).setInteractive());
            add(this.add.text(x, y, `${reg.icon || ''} ${reg.name || '???'} +${enhLevel}`, {
                fontSize: '10px', fontFamily: 'monospace', color: rarity.color
            }).setOrigin(0.5));

            card.on('pointerdown', () => this._showEnhancePopup(item));
        });
    }

    _showEnhancePopup(item) {
        if (this._enhPopup) this._enhPopup.destroy();
        const reg = ItemRegistry.get(item.itemId) || {};
        const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;
        const enhLevel = item.enhanceLevel || 0;

        const ep = this.add.container(640, 480).setDepth(2500);
        this._enhPopup = ep;
        ep.add(this.add.rectangle(0, 0, 400, 200, 0x111100, 0.98).setStrokeStyle(2, 0xffaa44).setInteractive());
        ep.add(this.add.text(0, -80, `${reg.icon || ''} ${reg.name || ''} +${enhLevel}`, {
            fontSize: '15px', fontFamily: 'monospace', color: rarity.color, fontStyle: 'bold'
        }).setOrigin(0.5));

        if (!BP_ENHANCE.canEnhance(item)) {
            ep.add(this.add.text(0, -30, '최대 강화 단계입니다', { fontSize: '13px', fontFamily: 'monospace', color: '#886644' }).setOrigin(0.5));
        } else {
            const preview = BP_ENHANCE.getPreview(item);
            if (preview) {
                const statParts = Object.keys(preview.next).map(k => `${k}: ${preview.current[k]}→${preview.next[k]}`);
                ep.add(this.add.text(0, -45, statParts.join(' | '), {
                    fontSize: '10px', fontFamily: 'monospace', color: '#ccaa66', wordWrap: { width: 360 }
                }).setOrigin(0.5));
                ep.add(this.add.text(0, -18, `💰${preview.cost}G  성공:${Math.floor(preview.successRate * 100)}%  파괴:${Math.floor(preview.breakRate * 100)}%`, {
                    fontSize: '11px', fontFamily: 'monospace', color: preview.breakRate > 0 ? '#ff6644' : '#88cc44'
                }).setOrigin(0.5));

                const enhBtn = this.add.rectangle(0, 20, 140, 35, 0x443300).setStrokeStyle(2, 0xffaa44).setInteractive();
                ep.add(enhBtn);
                ep.add(this.add.text(0, 20, '⚒️ 강화!', { fontSize: '14px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold' }).setOrigin(0.5));
                enhBtn.on('pointerdown', () => {
                    const result = BP_ENHANCE.enhance(item);
                    ep.destroy(); this._enhPopup = null;
                    let msg, color;
                    if (result.result === 'success') { msg = `✅ +${result.level} 성공!`; color = '#44ff88'; }
                    else if (result.result === 'broken') {
                        msg = '💥 파괴됨!'; color = '#ff2222';
                        StashManager.removeFromStash(item.id);
                    } else if (result.result === 'fail') { msg = '❌ 실패...'; color = '#ff8844'; }
                    else { msg = result.result; color = '#888888'; }
                    const txt = this.add.text(640, 460, msg, {
                        fontSize: '22px', fontFamily: 'monospace', color, fontStyle: 'bold', stroke: '#000', strokeThickness: 3
                    }).setOrigin(0.5).setDepth(3000);
                    this.tweens.add({ targets: txt, y: 420, alpha: 0, duration: 1500, onComplete: () => { txt.destroy(); this._drawForgeContent(); } });
                });
            }
        }

        const closeBtn = this.add.rectangle(0, 70, 80, 25, 0x332200).setStrokeStyle(1, 0x664400).setInteractive();
        ep.add(closeBtn);
        ep.add(this.add.text(0, 70, '닫기', { fontSize: '11px', fontFamily: 'monospace', color: '#aa8844' }).setOrigin(0.5));
        closeBtn.on('pointerdown', () => { ep.destroy(); this._enhPopup = null; });
    }

    _drawForgeCraft(add, equipment) {
        const craftable = equipment.filter(e => e.rarity !== 'mythical');
        if (craftable.length < 2) {
            add(this.add.text(0, 0, '조합할 장비 부족 (같은 아이템 2개 필요)', { fontSize: '13px', fontFamily: 'monospace', color: '#886644' }).setOrigin(0.5));
            return;
        }

        add(this.add.text(0, -165, '같은 아이템 + 같은 등급 2개 → 상위 등급', { fontSize: '10px', fontFamily: 'monospace', color: '#aa8844' }).setOrigin(0.5));

        if (!this._craftSlot1) this._craftSlot1 = null;
        if (!this._craftSlot2) this._craftSlot2 = null;

        // slots
        const s1Label = this._craftSlot1 ? (ItemRegistry.get(this._craftSlot1.itemId) || {}).name || '???' : '슬롯1';
        const s2Label = this._craftSlot2 ? (ItemRegistry.get(this._craftSlot2.itemId) || {}).name || '???' : '슬롯2';
        add(this.add.rectangle(-120, 140, 160, 45, 0x221100).setStrokeStyle(1, 0x664400));
        add(this.add.text(-120, 140, s1Label, { fontSize: '11px', fontFamily: 'monospace', color: '#ccaa66' }).setOrigin(0.5));
        add(this.add.text(0, 140, '+', { fontSize: '18px', fontFamily: 'monospace', color: '#ffaa44' }).setOrigin(0.5));
        add(this.add.rectangle(120, 140, 160, 45, 0x221100).setStrokeStyle(1, 0x664400));
        add(this.add.text(120, 140, s2Label, { fontSize: '11px', fontFamily: 'monospace', color: '#ccaa66' }).setOrigin(0.5));

        if (this._craftSlot1 && this._craftSlot2) {
            const check = BP_CRAFT.canCombine(this._craftSlot1, this._craftSlot2);
            if (check.can) {
                const preview = BP_CRAFT.getPreview(this._craftSlot1, this._craftSlot2);
                const nr = FARMING.RARITIES[preview.rarity];
                add(this.add.text(0, 175, `→ ${preview.name} [${nr.name}] 💰${preview.cost}G`, {
                    fontSize: '12px', fontFamily: 'monospace', color: nr.color
                }).setOrigin(0.5));
                const craftBtn = add(this.add.rectangle(0, 205, 130, 30, 0x443300).setStrokeStyle(2, 0xffaa44).setInteractive());
                add(this.add.text(0, 205, '🔮 조합!', { fontSize: '13px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold' }).setOrigin(0.5));
                craftBtn.on('pointerdown', () => {
                    const result = BP_CRAFT.combine(this._craftSlot1, this._craftSlot2);
                    if (result.result === 'success') {
                        StashManager.removeFromStash(this._craftSlot1.id);
                        StashManager.removeFromStash(this._craftSlot2.id);
                        StashManager.addToStash(result.newItem);
                        this._craftSlot1 = null; this._craftSlot2 = null;
                        const nrr = FARMING.RARITIES[result.newItem.rarity];
                        const rn = (ItemRegistry.get(result.newItem.itemId) || result.newItem).name;
                        const txt = this.add.text(640, 500, `✨ ${rn} [${nrr.name}]!`, {
                            fontSize: '20px', fontFamily: 'monospace', color: nrr.color, fontStyle: 'bold', stroke: '#000', strokeThickness: 3
                        }).setOrigin(0.5).setDepth(3000);
                        this.tweens.add({ targets: txt, y: 460, alpha: 0, duration: 2000, onComplete: () => { txt.destroy(); this._drawForgeContent(); } });
                    }
                });
            } else {
                const reasons = { different_items: '다른 아이템', different_rarity: '다른 등급', max_rarity: '신화 조합불가' };
                add(this.add.text(0, 175, `❌ ${reasons[check.reason] || '조합 불가'}`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#ff4444'
                }).setOrigin(0.5));
            }
        }

        // item grid
        const cols = 4, cellW = 200, cellH = 40, gp = 5;
        const totalW = cols * cellW + (cols - 1) * gp;
        const startX = -totalW / 2 + cellW / 2;
        craftable.forEach((item, i) => {
            if (i >= 12) return;
            const col = i % cols;
            const row = Math.floor(i / cols);
            const x = startX + col * (cellW + gp);
            const y = -135 + row * (cellH + gp);
            const reg = ItemRegistry.get(item.itemId) || {};
            const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;

            const card = add(this.add.rectangle(x, y, cellW, cellH, 0x221100).setStrokeStyle(1, rarity.borderColor).setInteractive());
            add(this.add.text(x, y, `${reg.icon || ''} ${reg.name || '???'} [${rarity.name}]`, {
                fontSize: '9px', fontFamily: 'monospace', color: rarity.color
            }).setOrigin(0.5));

            card.on('pointerdown', () => {
                if (!this._craftSlot1) {
                    this._craftSlot1 = item;
                } else if (!this._craftSlot2 && item.id !== this._craftSlot1.id) {
                    this._craftSlot2 = item;
                } else {
                    this._craftSlot1 = item; this._craftSlot2 = null;
                }
                this._drawForgeContent();
            });
        });
    }

    _openStash() {
        FarmingUI.drawStashOverlay(this, (entry, regItem) => {
            this._showStashItemPopup(entry, regItem);
        }, () => {
            this._drawAll();
        });
    }

    _showStashItemPopup(entry, regItem) {
        if (this._stashPopup) this._stashPopup.destroy();
        FarmingUI.hideTooltip();
        const rarity = FARMING.RARITIES[entry.rarity] || FARMING.RARITIES.common;
        const value = ItemRegistry.getValue({ ...regItem, rarity: entry.rarity });

        const popup = this.add.container(640, 360).setDepth(2500);
        this._stashPopup = popup;
        popup.add(this.add.rectangle(0, 0, 1280, 720, 0x000000, 0.5).setInteractive());
        popup.add(this.add.rectangle(0, 0, 300, 120, 0x111111, 0.98).setStrokeStyle(2, rarity.borderColor));
        popup.add(this.add.text(0, -42, `${regItem.icon || ''} ${regItem.name} [${rarity.name}]`, {
            fontSize: '13px', fontFamily: 'monospace', color: rarity.color, fontStyle: 'bold'
        }).setOrigin(0.5));

        const dismBtn = this.add.rectangle(0, -5, 220, 35, 0x1a0a0a).setStrokeStyle(2, 0xff6644).setInteractive();
        popup.add(dismBtn);
        popup.add(this.add.text(0, -5, `🔨 분해 → 💰${value}G`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#ff6644', fontStyle: 'bold'
        }).setOrigin(0.5));
        dismBtn.on('pointerdown', () => {
            const result = BP_FARMING.dismantleStashItem(entry.id);
            popup.destroy(); this._stashPopup = null;
            if (result) {
                this._showMsg(`🔨 ${regItem.name} 분해! +${result.gold}G`);
                this._openStash();
            }
        });

        const cancelBtn = this.add.rectangle(0, 38, 80, 25, 0x332222).setStrokeStyle(1, 0x664444).setInteractive();
        popup.add(cancelBtn);
        popup.add(this.add.text(0, 38, '취소', { fontSize: '11px', fontFamily: 'monospace', color: '#aa8888' }).setOrigin(0.5));
        cancelBtn.on('pointerdown', () => { popup.destroy(); this._stashPopup = null; });
    }

    _openEquipOverlay(charId, targetSlot) {
        FarmingUI.drawStashOverlay(this, (entry, regItem) => {
            if (regItem.category !== 'equipment' || regItem.slot !== targetSlot) return;
            BP_ROSTER.equipToCharacter(charId, targetSlot, entry.id);
            FarmingUI.hideTooltip();
            this._drawAll();
        }, () => {
            this._drawAll();
        }, targetSlot);
    }

    _confirmDismiss(char) {
        const base = BP_ALLIES[char.classKey];
        const popup = this.add.container(640, 360).setDepth(2500);
        popup.add(this.add.rectangle(0, 0, 1280, 720, 0x000000, 0.5).setInteractive());
        popup.add(this.add.rectangle(0, 0, 350, 120, 0x111111, 0.98).setStrokeStyle(2, 0xff4444));
        popup.add(this.add.text(0, -35, `${base.name} "${char.name}" 추방?`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
        }).setOrigin(0.5));
        popup.add(this.add.text(0, -10, '장비는 스태시로 회수됩니다', {
            fontSize: '10px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5));

        const yesBtn = this.add.rectangle(-60, 30, 100, 30, 0x441111).setStrokeStyle(1, 0xff4444).setInteractive();
        popup.add(yesBtn);
        popup.add(this.add.text(-60, 30, '추방', { fontSize: '12px', fontFamily: 'monospace', color: '#ff4444' }).setOrigin(0.5));
        yesBtn.on('pointerdown', () => {
            this.selectedParty = this.selectedParty.filter(id => id !== char.id);
            BP_ROSTER.dismissCharacter(char.id);
            popup.destroy();
            this._drawAll();
        });

        const noBtn = this.add.rectangle(60, 30, 100, 30, 0x222222).setStrokeStyle(1, 0x664444).setInteractive();
        popup.add(noBtn);
        popup.add(this.add.text(60, 30, '취소', { fontSize: '12px', fontFamily: 'monospace', color: '#aaaaaa' }).setOrigin(0.5));
        noBtn.on('pointerdown', () => popup.destroy());
    }

    _showPromotionPopup(char) {
        const info = BP_ROSTER.getPromotionInfo(char.id);
        if (!info) return;
        const base = BP_ALLIES[char.classKey];
        const advBase = BP_ALLIES[info.advancedClass];

        const popup = this.add.container(640, 360).setDepth(2500);
        popup.add(this.add.rectangle(0, 0, 1280, 720, 0x000000, 0.6).setInteractive());
        popup.add(this.add.rectangle(0, 0, 480, 320, 0x111100, 0.98).setStrokeStyle(2, 0xffcc44));

        popup.add(this.add.text(0, -140, '⬆ 전직', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5));
        popup.add(this.add.text(0, -112, `${base.name} "${char.name}" → ${advBase.name}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5));

        // stat comparison
        const stats = [
            { key: 'hp', label: 'HP' }, { key: 'atk', label: 'ATK' }, { key: 'def', label: 'DEF' },
            { key: 'attackSpeed', label: '공속(ms)', invert: true }, { key: 'range', label: '사거리' },
            { key: 'critRate', label: '크리율', pct: true }, { key: 'critDmg', label: '크뎀', mult: true },
            { key: 'dodgeRate', label: '회피', pct: true }
        ];
        const startY = -80;
        stats.forEach((s, i) => {
            const row = Math.floor(i / 2);
            const col = i % 2;
            const sx = -180 + col * 200;
            const sy = startY + row * 22;
            const cur = info.currentStats[s.key] || 0;
            const nxt = info.newStats[s.key] || 0;
            let curStr, nxtStr;
            if (s.pct) { curStr = Math.floor(cur * 100) + '%'; nxtStr = Math.floor(nxt * 100) + '%'; }
            else if (s.mult) { curStr = cur.toFixed(1) + 'x'; nxtStr = nxt.toFixed(1) + 'x'; }
            else { curStr = '' + cur; nxtStr = '' + nxt; }
            const better = s.invert ? nxt < cur : nxt > cur;
            const worse = s.invert ? nxt > cur : nxt < cur;
            const color = better ? '#44ff88' : worse ? '#ff6644' : '#aaaaaa';
            popup.add(this.add.text(sx, sy, `${s.label}: ${curStr} → ${nxtStr}`, {
                fontSize: '11px', fontFamily: 'monospace', color
            }));
        });

        // skill comparison
        const curSkill = BP_SKILLS[char.classKey];
        const newSkill = BP_SKILLS[info.advancedClass];
        if (curSkill && newSkill) {
            popup.add(this.add.text(0, startY + 100, `스킬: ${curSkill.name} → ${newSkill.name}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#ccaa66'
            }).setOrigin(0.5));
            popup.add(this.add.text(0, startY + 118, newSkill.desc, {
                fontSize: '9px', fontFamily: 'monospace', color: '#888866', wordWrap: { width: 400 }
            }).setOrigin(0.5));
        }

        // cost
        popup.add(this.add.text(0, 80, `💰 ${info.cost}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5));

        // confirm
        const confirmBtn = this.add.rectangle(-70, 115, 130, 35, 0x443300).setStrokeStyle(2, 0xffcc44).setInteractive();
        popup.add(confirmBtn);
        popup.add(this.add.text(-70, 115, '전직 확정', {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5));
        confirmBtn.on('pointerdown', () => {
            const result = BP_ROSTER.promoteCharacter(char.id);
            popup.destroy();
            if (result.success) {
                this._showMsg(`⬆ ${result.newName}(으)로 전직 완료!`);
            } else {
                this._showMsg('전직 실패: ' + result.reason);
            }
            this._drawAll();
        });

        // cancel
        const cancelBtn = this.add.rectangle(70, 115, 100, 35, 0x222222).setStrokeStyle(1, 0x664444).setInteractive();
        popup.add(cancelBtn);
        popup.add(this.add.text(70, 115, '취소', {
            fontSize: '13px', fontFamily: 'monospace', color: '#aaaaaa'
        }).setOrigin(0.5));
        cancelBtn.on('pointerdown', () => popup.destroy());
    }

    _showMsg(text) {
        const msg = this.add.text(640, 360, text, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold',
            backgroundColor: '#111111', padding: { x: 20, y: 10 }
        }).setOrigin(0.5).setDepth(3000);
        this.time.delayedCall(1500, () => msg.destroy());
    }
}
