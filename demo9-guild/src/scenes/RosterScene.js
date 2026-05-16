class RosterScene extends Phaser.Scene {
    constructor() { super('RosterScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.selectedMercId = null;
    }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
        this._drawUI();
    }

    _drawUI() {
        const gs = this.gameState;

        this.add.text(640, 25, '로스터 관리', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        if (gs.roster.length === 0) {
            this.add.text(640, 360, '로스터가 비어있습니다', {
                fontSize: '14px', fontFamily: 'monospace', color: '#555566'
            }).setOrigin(0.5);
            return;
        }

        const listX = 15;
        let listY = 65;
        gs.roster.forEach(merc => {
            this._drawListItem(merc, listX, listY, 300);
            listY += 68;
        });

        const selected = gs.roster.find(m => m.id === this.selectedMercId);
        if (selected) {
            this._drawDetailPanel(selected, 340, 65);
        } else {
            this.add.text(700, 360, '용병을 선택하세요', {
                fontSize: '14px', fontFamily: 'monospace', color: '#555566'
            }).setOrigin(0.5);
        }
    }

    _drawListItem(merc, x, y, w) {
        const base = merc.getBaseClass();
        const rarity = RARITY_DATA[merc.rarity];
        const stats = merc.getStats();
        const isSelected = merc.id === this.selectedMercId;

        const bg = this.add.graphics();
        bg.fillStyle(isSelected ? 0x223344 : 0x151525, 1);
        bg.fillRoundedRect(x, y, w, 60, 3);
        bg.lineStyle(1, isSelected ? 0x5588aa : rarity.color, isSelected ? 0.9 : 0.3);
        bg.strokeRoundedRect(x, y, w, 60, 3);

        this.add.text(x + 8, y + 6, `${base.icon} ${merc.name}`, {
            fontSize: '12px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        });
        this.add.text(x + w - 8, y + 6, `Lv.${merc.level} ${base.name}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(1, 0);

        this.add.text(x + 8, y + 25, `HP:${merc.currentHp}/${stats.hp} ATK:${stats.atk} DEF:${stats.def}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#8888aa'
        });

        const hpRatio = merc.currentHp / stats.hp;
        const hpBar = this.add.graphics();
        hpBar.fillStyle(0x333344, 1);
        hpBar.fillRect(x + 8, y + 44, w - 16, 4);
        const hpColor = hpRatio > 0.6 ? 0x44ff88 : hpRatio > 0.3 ? 0xffaa44 : 0xff4444;
        hpBar.fillStyle(hpColor, 1);
        hpBar.fillRect(x + 8, y + 44, (w - 16) * hpRatio, 4);

        const hitZone = this.add.zone(x + w / 2, y + 30, w, 60).setInteractive({ useHandCursor: true });
        hitZone.on('pointerdown', () => {
            this.selectedMercId = merc.id;
            this.scene.restart({ gameState: this.gameState });
        });
    }

    _drawDetailPanel(merc, x, y) {
        const gs = this.gameState;
        const base = merc.getBaseClass();
        const rarity = RARITY_DATA[merc.rarity];
        const stats = merc.getStats();
        const w = 920;

        UIPanel.create(this, x, y, w, 640, { fillColor: 0x151525, strokeColor: 0x446688 });

        let cy = y + 15;

        this.add.text(x + 20, cy, `${base.icon} ${merc.name}`, {
            fontSize: '18px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        });
        this.add.text(x + w - 20, cy, `Lv.${merc.level} ${base.name} [${rarity.name}]`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(1, 0);
        cy += 30;

        const xpLabel = merc.level >= 10 ? 'MAX' : `${merc.xp}/${merc.getXpToNextLevel()}`;
        this.add.text(x + 20, cy, `XP: ${xpLabel}`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#6688aa'
        });
        cy += 25;

        const statLabels = [
            ['HP', `${merc.currentHp}/${stats.hp}`],
            ['ATK', stats.atk], ['DEF', stats.def],
            ['SPD', stats.moveSpeed], ['CRIT', `${Math.floor(stats.critRate * 100)}%`],
            ['범위', stats.range], ['공속', `${stats.attackSpeed}ms`]
        ];
        const statCols = 4;
        statLabels.forEach((s, i) => {
            const col = i % statCols;
            const row = Math.floor(i / statCols);
            this.add.text(x + 20 + col * 115, cy + row * 20, `${s[0]}: ${s[1]}`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#aaaacc'
            });
        });
        cy += 50;

        const line1 = this.add.graphics();
        line1.lineStyle(1, 0x333355, 0.5);
        line1.lineBetween(x + 15, cy, x + w - 15, cy);
        cy += 12;

        this.add.text(x + 20, cy, '특성', {
            fontSize: '13px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
        });
        cy += 20;
        merc.traits.forEach(trait => {
            let color = '#44cc44';
            let sym = '✦';
            if (trait.type === 'negative') { color = '#ff6666'; sym = '✧'; }
            if (trait.type === 'legendary') { color = '#ffaa00'; sym = '★'; }
            this.add.text(x + 25, cy, `${sym} ${trait.name} — ${trait.desc}`, {
                fontSize: '11px', fontFamily: 'monospace', color
            });
            cy += 18;
        });
        cy += 10;

        if (merc.level >= 5) {
            this.add.text(x + 20, cy, `스킬: ${base.skillName}`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#44aaff', fontStyle: 'bold'
            });
            this.add.text(x + 25, cy + 18, base.skillDesc, {
                fontSize: '11px', fontFamily: 'monospace', color: '#6688aa'
            });
            cy += 40;
        }

        const line2 = this.add.graphics();
        line2.lineStyle(1, 0x333355, 0.5);
        line2.lineBetween(x + 15, cy, x + w - 15, cy);
        cy += 12;

        this.add.text(x + 20, cy, '장비', {
            fontSize: '13px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
        });
        cy += 22;

        ['weapon', 'armor', 'accessory'].forEach(slot => {
            const slotNames = { weapon: '무기', armor: '방어구', accessory: '장신구' };
            const eq = merc.equipment[slot];
            const label = eq ? `${slotNames[slot]}: ${eq.name} [${ITEM_RARITY[eq.rarity].name}]` : `${slotNames[slot]}: (비어있음)`;
            const color = eq ? ITEM_RARITY[eq.rarity].textColor : '#555566';

            this.add.text(x + 25, cy, label, {
                fontSize: '11px', fontFamily: 'monospace', color
            });

            if (eq) {
                const statStr = Object.entries(eq.stats || {}).map(([k, v]) => `${k}+${v}`).join(' ');
                if (statStr) {
                    this.add.text(x + 25, cy + 14, `  ${statStr}`, {
                        fontSize: '10px', fontFamily: 'monospace', color: '#667788'
                    });
                }

                if (eq.cursed) {
                    this.add.text(x + w - 80, cy + 4, '🔒 저주', {
                        fontSize: '10px', fontFamily: 'monospace', color: '#cc44ff', fontStyle: 'bold'
                    });
                    if (eq.curseDebuffDesc) {
                        this.add.text(x + 25, cy + 26, `  ⚠ ${eq.curseDebuffDesc}`, {
                            fontSize: '9px', fontFamily: 'monospace', color: '#cc4488'
                        });
                    }
                } else {
                    UIButton.create(this, x + w - 80, cy + 8, 60, 22, '해제', {
                        color: 0x444455, hoverColor: 0x555566, textColor: '#aaaaaa', fontSize: 10,
                        onClick: () => {
                            const item = merc.unequip(slot);
                            if (item) StorageManager.addItem(gs, item);
                            SaveManager.save(gs);
                            this.scene.restart({ gameState: gs });
                        }
                    });
                }
            } else {
                const equippable = StorageManager.getEquippableItems(gs, slot);
                if (equippable.length > 0) {
                    // 스탯 합이 가장 큰 거 우선
                    equippable.sort((a, b) => {
                        const sum = e => Object.values(e.stats || {}).reduce((s, v) => s + (typeof v === 'number' ? v : 0), 0);
                        return sum(b) - sum(a);
                    });
                    UIButton.create(this, x + w - 80, cy + 8, 75, 22, `장착(${equippable.length})`, {
                        color: 0x446688, hoverColor: 0x5588aa, textColor: '#ccccee', fontSize: 10,
                        onClick: () => this._showEquipMenu(merc, slot, equippable)
                    });
                } else {
                    this.add.text(x + w - 80, cy + 14, '(보관 없음)', {
                        fontSize: '9px', fontFamily: 'monospace', color: '#555566'
                    }).setOrigin(0, 0.5);
                }
            }
            cy += 32;
        });

        const line3 = this.add.graphics();
        line3.lineStyle(1, 0x333355, 0.5);
        line3.lineBetween(x + 15, cy + 5, x + w - 15, cy + 5);
        cy += 15;

        this.add.text(x + 20, cy, '구역 친화도', {
            fontSize: '13px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
        });
        cy += 20;
        ZONE_KEYS.forEach(zoneKey => {
            if (gs.zoneLevel[zoneKey] === 0) return;
            const zone = ZONE_DATA[zoneKey];
            const aLv = merc.affinityLevel?.[zoneKey] || 0;
            const aPts = merc.affinityPoints?.[zoneKey] || 0;
            const aXp = merc.affinityXp?.[zoneKey] || 0;
            const needed = aLv >= 5 ? 'MAX' : getAffinityXpNeeded(aLv);
            const ptsLabel = aPts > 0 ? ` [${aPts}P 사용가능]` : '';
            this.add.text(x + 25, cy, `${zone.icon} ${zone.name}: Lv.${aLv} (${aLv >= 5 ? 'MAX' : `${aXp}/${needed}`})${ptsLabel}`, {
                fontSize: '11px', fontFamily: 'monospace', color: aPts > 0 ? '#ffaa44' : zone.textColor
            });
            cy += 16;
        });

        UIButton.create(this, x + w / 2, cy + 12, 160, 26, '친화도 트리 열기', {
            color: 0x445566, hoverColor: 0x556677, textColor: '#aaccee', fontSize: 11,
            onClick: () => this.scene.start('AffinityScene', { gameState: gs, mercId: merc.id })
        });

        UIButton.create(this, x + w / 2 - 80, y + 610, 120, 30, '치료 (30G)', {
            color: gs.gold >= 30 && merc.currentHp < merc.getStats().hp ? 0x44aa44 : 0x444444,
            hoverColor: 0x55cc55, textColor: '#ffffff', fontSize: 12,
            disabled: gs.gold < 30 || merc.currentHp >= merc.getStats().hp,
            onClick: () => {
                if (GuildManager.spendGold(gs, 30)) {
                    merc.fullHeal();
                    GuildManager.addMessage(gs, `${merc.name} 치료 완료 (-30G)`);
                    SaveManager.save(gs);
                    this.scene.restart({ gameState: gs });
                }
            }
        });

        UIButton.create(this, x + w / 2 + 80, y + 610, 120, 30, '해고', {
            color: 0x884444, hoverColor: 0xaa5555, textColor: '#ffcccc', fontSize: 12,
            onClick: () => {
                MercenaryManager.dismiss(gs, merc.id);
                this.selectedMercId = null;
                this.scene.restart({ gameState: gs });
            }
        });

        // 자동 장착 버튼 (3슬롯 일괄)
        UIButton.create(this, x + w / 2, y + 580, 200, 30, '🤖 최적 장비 자동 장착', {
            color: 0x446688, hoverColor: 0x5588aa, textColor: '#ccccee', fontSize: 12,
            onClick: () => {
                let count = 0;
                ['weapon', 'armor', 'accessory'].forEach(slot => {
                    const candidates = StorageManager.getEquippableItems(gs, slot);
                    if (candidates.length === 0) return;
                    candidates.sort((a, b) => {
                        const sum = e => Object.values(e.stats || {}).reduce((s, v) => s + (typeof v === 'number' ? v : 0), 0);
                        return sum(b) - sum(a);
                    });
                    const best = candidates[0];
                    const current = merc.equipment[slot];
                    const sum = e => e ? Object.values(e.stats || {}).reduce((s, v) => s + (typeof v === 'number' ? v : 0), 0) : 0;
                    if (sum(best) > sum(current)) {
                        StorageManager.removeItem(gs, best.id);
                        const prev = merc.equip(best);
                        if (prev) StorageManager.addItem(gs, prev);
                        count++;
                    }
                });
                if (count > 0) {
                    SaveManager.save(gs);
                    UIToast.show(this, `${count}개 슬롯 자동 장착!`, { color: '#88ccff' });
                    this.scene.restart({ gameState: gs });
                } else {
                    UIToast.show(this, '더 좋은 장비 없음', { color: '#aaaaaa' });
                }
            }
        });
    }

    /**
     * 장비 선택 모달 — 슬롯 클릭 시 장착 가능한 아이템 목록 표시.
     */
    _showEquipMenu(merc, slot, items) {
        const gs = this.gameState;
        // 오버레이
        const overlay = this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.7).setDepth(100);
        const modalW = 600, modalH = Math.min(560, 80 + items.length * 56);
        const modalX = 640 - modalW / 2, modalY = 360 - modalH / 2;
        const modalObjs = [overlay];

        const bg = this.add.graphics().setDepth(101);
        bg.fillStyle(0x151525, 1);
        bg.fillRoundedRect(modalX, modalY, modalW, modalH, 6);
        bg.lineStyle(2, 0x5588aa, 0.8);
        bg.strokeRoundedRect(modalX, modalY, modalW, modalH, 6);
        modalObjs.push(bg);

        const slotNames = { weapon: '무기', armor: '방어구', accessory: '장신구' };
        modalObjs.push(this.add.text(modalX + modalW / 2, modalY + 20, `${slotNames[slot]} 선택 (${items.length}개)`, {
            fontSize: '15px', fontFamily: 'monospace', color: '#ccccee', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(101));

        modalObjs.push(UIButton.create(this, modalX + modalW - 50, modalY + 20, 60, 24, '닫기', {
            color: 0x444455, hoverColor: 0x555566, textColor: '#aaaaaa', fontSize: 11, depth: 101,
            onClick: () => modalObjs.forEach(o => o.destroy && o.destroy())
        }));

        let cy = modalY + 55;
        items.slice(0, 8).forEach(item => {
            const ir = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
            const rowBg = this.add.graphics().setDepth(101);
            rowBg.fillStyle(0x1a1a2a, 1);
            rowBg.fillRoundedRect(modalX + 12, cy, modalW - 24, 48, 3);
            rowBg.lineStyle(1, ir.color, 0.4);
            rowBg.strokeRoundedRect(modalX + 12, cy, modalW - 24, 48, 3);
            modalObjs.push(rowBg);

            modalObjs.push(this.add.text(modalX + 24, cy + 5, item.name, {
                fontSize: '12px', fontFamily: 'monospace', color: ir.textColor, fontStyle: 'bold'
            }).setDepth(102));
            const statStr = Object.entries(item.stats || {}).map(([k, v]) => `${k}+${v}`).join(' ');
            modalObjs.push(this.add.text(modalX + 24, cy + 24, `[${ir.name}] ${statStr}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#888899'
            }).setDepth(102));

            modalObjs.push(UIButton.create(this, modalX + modalW - 60, cy + 24, 80, 26, '장착', {
                color: 0x446688, hoverColor: 0x5588aa, textColor: '#ccccee', fontSize: 11, depth: 102,
                onClick: () => {
                    StorageManager.removeItem(gs, item.id);
                    const prev = merc.equip(item);
                    if (prev) StorageManager.addItem(gs, prev);
                    SaveManager.save(gs);
                    modalObjs.forEach(o => o.destroy && o.destroy());
                    this.scene.restart({ gameState: gs });
                }
            }));
            cy += 56;
        });
        if (items.length > 8) {
            modalObjs.push(this.add.text(modalX + modalW / 2, cy + 8, `... +${items.length - 8}개 더`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5).setDepth(101));
        }
    }
}
