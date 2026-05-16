/**
 * EquipmentScene — 장비 착용 전용 페이지.
 * 좌: 용병 목록, 우: 선택된 용병의 슬롯별 장착/해제 + 보관함 후보 리스트.
 */
class EquipmentScene extends Phaser.Scene {
    constructor() { super('EquipmentScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.selectedMercId = data.selectedMercId || null;
        this.selectedSlot = data.selectedSlot || 'weapon';
    }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
        const gs = this.gameState;

        this.add.text(640, 25, '⚔ 장비 착용', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        this.goldText = this.add.text(1260, 25, `${gs.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0);

        this._drawMercList(20, 60, 320, 640);
        const selected = gs.roster.find(m => m.id === this.selectedMercId);
        if (selected) {
            this._drawMercDetail(selected, 360, 60, 460, 640);
            this._drawSlotInventory(selected, 840, 60, 420, 640);
        } else {
            this.add.text(640, 360, '왼쪽에서 용병을 선택하세요', {
                fontSize: '14px', fontFamily: 'monospace', color: '#888899'
            }).setOrigin(0.5);
        }
    }

    _drawMercList(x, y, w, h) {
        const gs = this.gameState;
        UIPanel.create(this, x, y, w, h, { title: `로스터 (${gs.roster.filter(m=>m.alive).length})` });

        const alive = gs.roster.filter(m => m.alive);
        if (alive.length === 0) {
            this.add.text(x + w/2, y + 50, '용병이 없습니다', {
                fontSize: '12px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5);
            return;
        }

        let cy = y + 35;
        alive.forEach(merc => {
            if (cy > y + h - 60) return;
            const base = merc.getBaseClass();
            const rarity = RARITY_DATA[merc.rarity];
            const isSelected = merc.id === this.selectedMercId;

            const bg = this.add.graphics();
            bg.fillStyle(isSelected ? 0x2a3a4a : 0x151525, 1);
            bg.fillRoundedRect(x + 10, cy, w - 20, 50, 3);
            bg.lineStyle(1, isSelected ? 0x66aacc : rarity.color, isSelected ? 0.9 : 0.4);
            bg.strokeRoundedRect(x + 10, cy, w - 20, 50, 3);

            this.add.text(x + 20, cy + 6, `${base.icon} ${merc.name}`, {
                fontSize: '13px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
            });
            this.add.text(x + 20, cy + 24, `Lv.${merc.level} ${base.name}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#888899'
            });
            // 장착 카운트
            const equipCount = ['weapon','armor','accessory'].filter(s => merc.equipment[s]).length;
            this.add.text(x + w - 20, cy + 16, `${equipCount}/3`, {
                fontSize: '11px', fontFamily: 'monospace', color: equipCount === 3 ? '#88ffcc' : '#aaaaaa'
            }).setOrigin(1, 0);

            const hit = this.add.zone(x + w/2, cy + 25, w - 20, 50).setInteractive({ useHandCursor: true });
            hit.on('pointerdown', () => {
                this.selectedMercId = merc.id;
                this.scene.restart({ gameState: gs, selectedMercId: merc.id, selectedSlot: this.selectedSlot });
            });
            cy += 55;
        });
    }

    _drawMercDetail(merc, x, y, w, h) {
        const gs = this.gameState;
        const base = merc.getBaseClass();
        const rarity = RARITY_DATA[merc.rarity];
        const stats = merc.getStats();

        UIPanel.create(this, x, y, w, h, { title: '용병 정보 + 장비' });

        let cy = y + 30;
        this.add.text(x + 20, cy, `${base.icon} ${merc.name}`, {
            fontSize: '16px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        });
        cy += 24;
        this.add.text(x + 20, cy, `Lv.${merc.level} ${base.name} [${rarity.name}]`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#888899'
        });
        cy += 30;

        // 스탯
        const statRow = [['HP', `${merc.currentHp}/${stats.hp}`], ['ATK', stats.atk], ['DEF', stats.def], ['CRIT', `${Math.floor(stats.critRate * 100)}%`]];
        statRow.forEach((s, i) => {
            this.add.text(x + 20 + (i % 2) * 220, cy + Math.floor(i / 2) * 22, `${s[0]}: ${s[1]}`, {
                fontSize: '13px', fontFamily: 'monospace', color: '#aaaacc'
            });
        });
        cy += 60;

        // 슬롯
        ['weapon', 'armor', 'accessory'].forEach(slot => {
            const slotNames = { weapon: '⚔ 무기', armor: '🛡 방어구', accessory: '💍 장신구' };
            const eq = merc.equipment[slot];
            const isSelected = slot === this.selectedSlot;

            const slotH = eq && eq.specialDesc ? 72 : 60;
            const bg = this.add.graphics();
            bg.fillStyle(isSelected ? 0x2a3a4a : 0x1a1a2a, 1);
            bg.fillRoundedRect(x + 15, cy, w - 30, slotH, 4);
            bg.lineStyle(1, isSelected ? 0x66aacc : 0x444455, 0.6);
            bg.strokeRoundedRect(x + 15, cy, w - 30, slotH, 4);

            this.add.text(x + 25, cy + 8, slotNames[slot], {
                fontSize: '13px', fontFamily: 'monospace', color: '#aaccee', fontStyle: 'bold'
            });

            if (eq) {
                const ir = ITEM_RARITY[eq.rarity];
                this.add.text(x + 100, cy + 8, `${eq.name} [${ir.name}]`, {
                    fontSize: '12px', fontFamily: 'monospace', color: ir.textColor
                });
                const statStr = Object.entries(eq.stats || {}).map(([k,v]) => {
                    if (k === 'critRate') return `CRIT+${Math.round(v*100)}%`;
                    return `${k.toUpperCase()}+${v}`;
                }).join(' ');
                let displayStr = statStr;
                if (eq.penalty && Object.keys(eq.penalty).length > 0) {
                    const penStr = Object.entries(eq.penalty).map(([k,v]) => `${k.toUpperCase()}${v}`).join(' ');
                    displayStr += ` | ${penStr}`;
                }
                this.add.text(x + 100, cy + 28, displayStr, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#888899'
                });
                if (eq.specialDesc) {
                    this.add.text(x + 100, cy + 42, `★ ${eq.specialDesc}`, {
                        fontSize: '9px', fontFamily: 'monospace', color: '#88ccaa'
                    });
                }

                // 슬롯 클릭 시 보관함 필터링 — 해제 버튼보다 먼저 배치해야 버튼이 우선
                const hit = this.add.zone(x + w/2, cy + 30, w - 30, 60).setInteractive({ useHandCursor: true });
                hit.on('pointerdown', () => {
                    this.selectedSlot = slot;
                    this.scene.restart({ gameState: gs, selectedMercId: merc.id, selectedSlot: slot });
                });

                if (!eq.cursed) {
                    UIButton.create(this, x + w - 60, cy + 30, 80, 26, '해제', {
                        color: 0x664444, hoverColor: 0x885555, textColor: '#ffaaaa', fontSize: 11,
                        depth: 5,
                        onClick: () => {
                            const item = merc.unequip(slot);
                            if (item) StorageManager.addItem(gs, item);
                            SaveManager.save(gs);
                            this.scene.restart({ gameState: gs, selectedMercId: merc.id, selectedSlot: slot });
                        }
                    });
                } else {
                    this.add.text(x + w - 60, cy + 30, '🔒 저주', {
                        fontSize: '11px', fontFamily: 'monospace', color: '#cc44ff', fontStyle: 'bold'
                    }).setOrigin(0.5);
                }
            } else {
                this.add.text(x + 100, cy + 20, '(비어있음 — 오른쪽에서 선택)', {
                    fontSize: '11px', fontFamily: 'monospace', color: '#666677'
                });

                // 슬롯 클릭 시 보관함 필터링
                const hit = this.add.zone(x + w/2, cy + 30, w - 30, 60).setInteractive({ useHandCursor: true });
                hit.on('pointerdown', () => {
                    this.selectedSlot = slot;
                    this.scene.restart({ gameState: gs, selectedMercId: merc.id, selectedSlot: slot });
                });
            }
            cy += 68;
        });

        // 자동 최적 장착
        UIButton.create(this, x + w/2, cy + 30, 220, 32, '🤖 최적 장비 자동 장착', {
            color: 0x446688, hoverColor: 0x5588aa, textColor: '#ccccee', fontSize: 12,
            onClick: () => {
                let count = 0;
                ['weapon', 'armor', 'accessory'].forEach(slot => {
                    const candidates = StorageManager.getEquippableItems(gs, slot);
                    if (candidates.length === 0) return;
                    candidates.sort((a, b) => this._sumStats(b) - this._sumStats(a));
                    const best = candidates[0];
                    const current = merc.equipment[slot];
                    if (this._sumStats(best) > this._sumStats(current)) {
                        StorageManager.removeItem(gs, best.id);
                        const prev = merc.equip(best);
                        if (prev) StorageManager.addItem(gs, prev);
                        count++;
                    }
                });
                if (count > 0) {
                    SaveManager.save(gs);
                    UIToast.show(this, `${count}개 슬롯 자동 장착!`, { color: '#88ccff' });
                    this.scene.restart({ gameState: gs, selectedMercId: merc.id, selectedSlot: this.selectedSlot });
                } else {
                    UIToast.show(this, '더 좋은 장비 없음', { color: '#aaaaaa' });
                }
            }
        });
    }

    _drawSlotInventory(merc, x, y, w, h) {
        const gs = this.gameState;
        const slot = this.selectedSlot;
        const slotNames = { weapon: '무기', armor: '방어구', accessory: '장신구' };
        const items = StorageManager.getEquippableItems(gs, slot);
        items.sort((a, b) => this._sumStats(b) - this._sumStats(a));

        UIPanel.create(this, x, y, w, h, { title: `보관함 ${slotNames[slot]} (${items.length})` });

        if (items.length === 0) {
            this.add.text(x + w/2, y + 60, `${slotNames[slot]} 장비 없음`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5);
            return;
        }

        const current = merc.equipment[slot];
        let cy = y + 35;
        items.slice(0, 10).forEach(item => {
            if (cy > y + h - 60) return;
            const ir = ITEM_RARITY[item.rarity];
            const isUpgrade = this._sumStats(item) > this._sumStats(current);

            const bg = this.add.graphics();
            bg.fillStyle(isUpgrade ? 0x1a2a1a : 0x1a1a2a, 1);
            bg.fillRoundedRect(x + 10, cy, w - 20, 52, 3);
            bg.lineStyle(1, ir.color, 0.5);
            bg.strokeRoundedRect(x + 10, cy, w - 20, 52, 3);

            this.add.text(x + 20, cy + 5, item.name, {
                fontSize: '12px', fontFamily: 'monospace', color: ir.textColor, fontStyle: 'bold'
            });
            const tagParts = [`[${ir.name}]`];
            if (item.isZoneEquipment) tagParts.push('🌍');
            if (item.cursed) tagParts.push('⚠저주');
            if (item.special) tagParts.push(`★${item.special}`);
            this.add.text(x + 20, cy + 22, tagParts.join(' '), {
                fontSize: '10px', fontFamily: 'monospace', color: ir.textColor
            });
            const statStr = Object.entries(item.stats || {}).map(([k,v]) => {
                if (k === 'critRate') return `CRIT+${Math.round(v*100)}%`;
                return `${k.toUpperCase()}+${v}`;
            }).join(' ');
            this.add.text(x + 20, cy + 36, statStr, {
                fontSize: '10px', fontFamily: 'monospace', color: '#aaaacc'
            });

            if (isUpgrade && current) {
                this.add.text(x + w - 90, cy + 5, '↑', {
                    fontSize: '14px', fontFamily: 'monospace', color: '#88ff88', fontStyle: 'bold'
                });
            }

            UIButton.create(this, x + w - 60, cy + 27, 80, 24, '장착', {
                color: 0x446688, hoverColor: 0x5588aa, textColor: '#ccccee', fontSize: 11,
                onClick: () => {
                    StorageManager.removeItem(gs, item.id);
                    const prev = merc.equip(item);
                    if (prev) StorageManager.addItem(gs, prev);
                    SaveManager.save(gs);
                    this.scene.restart({ gameState: gs, selectedMercId: merc.id, selectedSlot: slot });
                }
            });
            cy += 58;
        });
    }

    _sumStats(item) {
        if (!item || !item.stats) return 0;
        let sum = 0;
        for (const [k, v] of Object.entries(item.stats)) {
            if (typeof v !== 'number') continue;
            if (k === 'critRate') sum += v * 100;
            else sum += v;
        }
        return sum;
    }
}
