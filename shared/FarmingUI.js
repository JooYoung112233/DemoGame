const FarmingUI = {
    // ── 공용 툴팁 ──
    _tooltip: null,

    showTooltip(scene, item, worldX, worldY) {
        this.hideTooltip();
        const regItem = ItemRegistry.get(item.itemId) || item;
        const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;
        const value = ItemRegistry.getValue(item);

        const lines = [];
        lines.push(`${regItem.icon || ''} ${regItem.name}  [${rarity.name}]`);
        lines.push(regItem.desc || '');
        lines.push(`💰 ${value}G`);
        if (regItem.stats) {
            const statParts = [];
            const STAT_NAMES = { atk: 'ATK', def: 'DEF', hp: 'HP', critRate: 'CRIT', critDmg: '치명뎀', bleedChance: '출혈', dodgeRate: '회피', lifesteal: '흡혈', thorns: '가시', moveSpeed: '이속' };
            Object.entries(regItem.stats).forEach(([k, v]) => {
                const label = STAT_NAMES[k] || k;
                const val = (typeof v === 'number' && v < 1) ? `+${Math.floor(v * 100)}%` : `+${v}`;
                statParts.push(`${label}${val}`);
            });
            if (statParts.length > 0) lines.push(statParts.join('  '));
        }
        if (regItem.weight) lines.push(`무게: ${regItem.weight}`);

        const text = lines.filter(l => l).join('\n');
        const tipW = 220, tipH = 18 * lines.length + 20;

        let tx = worldX + 15;
        let ty = worldY - tipH - 5;
        if (tx + tipW > 1280) tx = worldX - tipW - 15;
        if (ty < 0) ty = worldY + 20;

        const tip = scene.add.container(tx, ty).setDepth(2000);
        tip.add(scene.add.rectangle(tipW / 2, tipH / 2, tipW, tipH, 0x111111, 0.95).setStrokeStyle(2, rarity.borderColor));
        tip.add(scene.add.text(10, 8, text, {
            fontSize: '11px', fontFamily: 'monospace', color: rarity.color, lineSpacing: 4
        }));

        this._tooltip = tip;
    },

    hideTooltip() {
        if (this._tooltip) {
            this._tooltip.destroy();
            this._tooltip = null;
        }
    },

    _addTooltipEvents(scene, cell, item) {
        cell.on('pointerover', () => {
            cell.setFillStyle(0x2a1818);
            const bounds = cell.getBounds();
            this.showTooltip(scene, item, bounds.centerX, bounds.centerY);
        });
        cell.on('pointerout', () => {
            cell.setFillStyle(0x221111);
            this.hideTooltip();
        });
    },

    // ── 스태시 오버레이 ──
    _stashFilter: 'all',

    drawStashOverlay(scene, onSelect, onClose, initialFilter) {
        const self = this;
        self._stashFilter = initialFilter || 'all';
        self._stashOverlay = scene.add.container(0, 0).setDepth(1000);
        self._stashScene = scene;
        self._stashOnSelect = onSelect;
        self._stashOnClose = onClose;
        self._drawStashContent();
        return self._stashOverlay;
    },

    _drawStashContent() {
        const self = this;
        const scene = self._stashScene;
        const onSelect = self._stashOnSelect;
        const onClose = self._stashOnClose;
        const overlay = self._stashOverlay;

        overlay.removeAll(true);

        const bg = scene.add.rectangle(640, 360, 1280, 720, 0x000000, 0.85).setInteractive();
        overlay.add(bg);

        overlay.add(scene.add.text(640, 25, '📦 스태시', {
            fontSize: '28px', fontFamily: 'monospace', color: '#ffaa00', fontStyle: 'bold'
        }).setOrigin(0.5));

        const stash = StashManager.getStash();
        const gold = StashManager.getGold();

        overlay.add(scene.add.text(640, 55, `골드: ${gold}  |  ${stash.length}/${FARMING.STASH_CAPACITY}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#aa8866'
        }).setOrigin(0.5));

        // category tabs
        const tabs = [
            { key: 'all', label: '전체' },
            { key: 'weapon', label: '⚔️무기' },
            { key: 'armor', label: '🛡️방어구' },
            { key: 'accessory', label: '💍악세' },
            { key: 'consumable', label: '🧪소비' },
            { key: 'material', label: '📦재료' }
        ];
        const tabW = 130, tabH = 28, tabGap = 6;
        const tabTotalW = tabs.length * tabW + (tabs.length - 1) * tabGap;
        const tabStartX = 640 - tabTotalW / 2 + tabW / 2;
        const tabY = 80;

        tabs.forEach((tab, i) => {
            const tx = tabStartX + i * (tabW + tabGap);
            const isActive = self._stashFilter === tab.key;
            const tabBg = scene.add.rectangle(tx, tabY, tabW, tabH, isActive ? 0x442222 : 0x1a1a1a)
                .setStrokeStyle(1, isActive ? 0xff6644 : 0x444444).setInteractive();
            overlay.add(tabBg);
            overlay.add(scene.add.text(tx, tabY, tab.label, {
                fontSize: '11px', fontFamily: 'monospace', color: isActive ? '#ffaa44' : '#888888'
            }).setOrigin(0.5));

            // count badge
            const count = tab.key === 'all' ? stash.length : stash.filter(e => {
                const r = ItemRegistry.get(e.itemId);
                if (!r) return false;
                if (tab.key === 'weapon' || tab.key === 'armor' || tab.key === 'accessory') return r.category === 'equipment' && r.slot === tab.key;
                return r.category === tab.key;
            }).length;
            if (count > 0) {
                overlay.add(scene.add.text(tx + tabW / 2 - 8, tabY - tabH / 2 + 2, `${count}`, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#666666'
                }).setOrigin(0.5));
            }

            tabBg.on('pointerdown', () => {
                self._stashFilter = tab.key;
                self._drawStashContent();
            });
        });

        // filter stash items
        const filtered = stash.filter(entry => {
            if (self._stashFilter === 'all') return true;
            const regItem = ItemRegistry.get(entry.itemId);
            if (!regItem) return false;
            if (self._stashFilter === 'weapon' || self._stashFilter === 'armor' || self._stashFilter === 'accessory') {
                return regItem.category === 'equipment' && regItem.slot === self._stashFilter;
            }
            return regItem.category === self._stashFilter;
        });

        const cols = 6, cellW = 170, cellH = 70;
        const startX = 640 - (cols * cellW) / 2 + cellW / 2;
        const startY = 200;

        filtered.forEach((entry, i) => {
            const regItem = ItemRegistry.get(entry.itemId);
            if (!regItem) return;
            const col = i % cols;
            const row = Math.floor(i / cols);
            const x = startX + col * cellW;
            const y = startY + row * cellH;
            const rarity = FARMING.RARITIES[entry.rarity] || FARMING.RARITIES.common;

            const cell = scene.add.rectangle(x, y, cellW - 6, cellH - 6, 0x221111)
                .setStrokeStyle(2, rarity.borderColor).setInteractive();
            overlay.add(cell);

            const icon = regItem.icon || FARMING.ICON[regItem.category] || '?';
            const enhLabel = entry.enhanceLevel ? ` +${entry.enhanceLevel}` : '';
            overlay.add(scene.add.text(x - cellW/2 + 12, y - 12, icon + ' ' + regItem.name + enhLabel, {
                fontSize: '12px', fontFamily: 'monospace', color: rarity.color
            }));

            const slotLabel = regItem.slot ? FARMING.SLOTS[regItem.slot] : FARMING.CATEGORIES[regItem.category];
            overlay.add(scene.add.text(x - cellW/2 + 12, y + 8, slotLabel || '', {
                fontSize: '10px', fontFamily: 'monospace', color: '#666666'
            }));

            const tooltipItem = { ...regItem, rarity: entry.rarity, itemId: entry.itemId };
            cell.on('pointerover', () => {
                cell.setFillStyle(0x331818);
                self.showTooltip(scene, tooltipItem, x, y);
            });
            cell.on('pointerout', () => {
                cell.setFillStyle(0x221111);
                self.hideTooltip();
            });
            cell.on('pointerdown', () => {
                self.hideTooltip();
                if (onSelect) onSelect(entry, regItem);
            });
        });

        if (filtered.length === 0) {
            const emptyMsg = self._stashFilter === 'all' ? '스태시가 비어있습니다\n레이드에서 아이템을 획득하세요' : '이 카테고리에 아이템이 없습니다';
            overlay.add(scene.add.text(640, 300, emptyMsg, {
                fontSize: '16px', fontFamily: 'monospace', color: '#664444', align: 'center'
            }).setOrigin(0.5));
        }

        const closeBtn = scene.add.rectangle(640, 660, 160, 40, 0x442222).setStrokeStyle(2, 0x884444).setInteractive();
        overlay.add(closeBtn);
        overlay.add(scene.add.text(640, 660, '닫기', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffffff'
        }).setOrigin(0.5));
        closeBtn.on('pointerdown', () => {
            self.hideTooltip();
            self._stashFilter = 'all';
            overlay.destroy();
            if (onClose) onClose();
        });
    },

    // ── 로드아웃 패널 ──
    drawLoadoutPanel(scene, x, y, onSlotClick) {
        const container = scene.add.container(x, y);
        const loadout = StashManager.getLoadout();
        const slots = ['weapon', 'armor', 'accessory'];
        const slotW = 140, gap = 10;
        const totalW = slots.length * slotW + (slots.length - 1) * gap;
        const startX = -totalW / 2 + slotW / 2;

        container.add(scene.add.text(0, -55, '── 로드아웃 ──', {
            fontSize: '14px', fontFamily: 'monospace', color: '#884444'
        }).setOrigin(0.5));

        slots.forEach((slot, i) => {
            const sx = startX + i * (slotW + gap);
            const equipped = loadout[slot];
            const regItem = equipped ? ItemRegistry.get(equipped.itemId) : null;
            const rarity = regItem ? FARMING.RARITIES[equipped.rarity] : null;

            const cell = scene.add.rectangle(sx, 0, slotW, 50, 0x1a1111)
                .setStrokeStyle(2, rarity ? rarity.borderColor : 0x332222).setInteractive();
            container.add(cell);

            const label = regItem
                ? `${regItem.icon || ''} ${regItem.name}`
                : `[${FARMING.SLOTS[slot]}]`;
            const color = regItem ? (rarity ? rarity.color : '#ffffff') : '#554444';

            container.add(scene.add.text(sx, -5, label, {
                fontSize: '12px', fontFamily: 'monospace', color: color
            }).setOrigin(0.5));

            if (regItem && regItem.stats) {
                const statStr = Object.entries(regItem.stats)
                    .map(([k, v]) => `${k}+${typeof v === 'number' && v < 1 ? Math.floor(v*100)+'%' : v}`)
                    .join(' ');
                container.add(scene.add.text(sx, 12, statStr, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#888888'
                }).setOrigin(0.5));
            }

            if (regItem) {
                const tooltipItem = { ...regItem, rarity: equipped.rarity, itemId: equipped.itemId };
                cell.on('pointerover', () => {
                    cell.setFillStyle(0x2a1818);
                    const worldPos = container.getWorldTransformMatrix();
                    this.showTooltip(scene, tooltipItem, worldPos.tx + sx, worldPos.ty);
                });
                cell.on('pointerout', () => {
                    cell.setFillStyle(0x1a1111);
                    this.hideTooltip();
                });
            } else {
                cell.on('pointerover', () => cell.setFillStyle(0x2a1818));
                cell.on('pointerout', () => cell.setFillStyle(0x1a1111));
            }

            cell.on('pointerdown', () => {
                this.hideTooltip();
                if (onSlotClick) onSlotClick(slot, equipped);
            });
        });

        return container;
    },

    // ── 보안 컨테이너 (클릭 시 아이템 빼기) ──
    drawSecureContainer(scene, x, y, raidInventory, onSlotClick) {
        const container = scene.add.container(x, y);
        const secureSlots = StashManager.getSecureContainer();

        container.add(scene.add.text(0, -35, '🔒 보안 컨테이너 (클릭: 빼기)', {
            fontSize: '12px', fontFamily: 'monospace', color: '#ffaa00'
        }).setOrigin(0.5));

        const slotSize = 55, gap = 8;
        const totalW = FARMING.SECURE_CONTAINER_SIZE * slotSize + (FARMING.SECURE_CONTAINER_SIZE - 1) * gap;
        const startX = -totalW / 2 + slotSize / 2;

        for (let i = 0; i < FARMING.SECURE_CONTAINER_SIZE; i++) {
            const sx = startX + i * (slotSize + gap);
            const slot = secureSlots[i];
            const regItem = slot ? ItemRegistry.get(slot.itemId) : null;
            const rarity = regItem ? FARMING.RARITIES[slot.rarity] : null;

            const cell = scene.add.rectangle(sx, 0, slotSize, slotSize, 0x1a1a00)
                .setStrokeStyle(2, rarity ? rarity.borderColor : 0x444400).setInteractive();
            container.add(cell);

            if (regItem) {
                container.add(scene.add.text(sx, -8, regItem.icon || '?', {
                    fontSize: '18px', fontFamily: 'monospace'
                }).setOrigin(0.5));
                container.add(scene.add.text(sx, 12, regItem.name, {
                    fontSize: '9px', fontFamily: 'monospace', color: rarity ? rarity.color : '#ffffff'
                }).setOrigin(0.5));

                const tooltipItem = { ...regItem, rarity: slot.rarity, itemId: slot.itemId };
                cell.on('pointerover', () => {
                    cell.setFillStyle(0x2a2a00);
                    const worldPos = container.getWorldTransformMatrix();
                    this.showTooltip(scene, tooltipItem, worldPos.tx + sx, worldPos.ty);
                });
                cell.on('pointerout', () => {
                    cell.setFillStyle(0x1a1a00);
                    this.hideTooltip();
                });
            } else {
                container.add(scene.add.text(sx, 0, '비어있음', {
                    fontSize: '9px', fontFamily: 'monospace', color: '#444400'
                }).setOrigin(0.5));
                cell.on('pointerover', () => cell.setFillStyle(0x2a2a00));
                cell.on('pointerout', () => cell.setFillStyle(0x1a1a00));
            }

            cell.on('pointerdown', () => {
                this.hideTooltip();
                if (onSlotClick) onSlotClick(i, slot);
            });
        }

        return container;
    },

    // ── 드랍 팝업 ──
    drawLootPopup(scene, item, x, y) {
        const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;
        const regItem = ItemRegistry.get(item.itemId) || item;
        const value = ItemRegistry.getValue(item);

        const popup = scene.add.container(x, y).setDepth(500).setAlpha(0).setScale(0.5);
        popup.add(scene.add.rectangle(0, 0, 200, 55, 0x221111).setStrokeStyle(2, rarity.borderColor));
        popup.add(scene.add.text(0, -12, `${regItem.icon || ''} ${regItem.name}`, {
            fontSize: '13px', fontFamily: 'monospace', color: rarity.color, fontStyle: 'bold'
        }).setOrigin(0.5));
        popup.add(scene.add.text(0, 8, `[${rarity.name}]  💰${value}G`, {
            fontSize: '10px', fontFamily: 'monospace', color: rarity.color
        }).setOrigin(0.5));

        scene.tweens.add({
            targets: popup, alpha: 1, scale: 1, y: y - 30, duration: 300,
            ease: 'Back.easeOut',
            onComplete: () => {
                scene.tweens.add({
                    targets: popup, alpha: 0, y: y - 60, duration: 800, delay: 1200,
                    onComplete: () => popup.destroy()
                });
            }
        });

        return popup;
    },

    // ── 레이드 정산 ──
    drawRaidSummary(scene, x, y, gained, lost, gold) {
        const container = scene.add.container(x, y);

        container.add(scene.add.text(0, 0, '── 레이드 정산 ──', {
            fontSize: '16px', fontFamily: 'monospace', color: '#884444'
        }).setOrigin(0.5));

        let offsetY = 30;
        if (gained.length > 0) {
            container.add(scene.add.text(0, offsetY, `✅ 획득 (${gained.length}개)`, {
                fontSize: '14px', fontFamily: 'monospace', color: '#44ff88'
            }).setOrigin(0.5));
            offsetY += 22;

            gained.forEach(item => {
                const regItem = ItemRegistry.get(item.itemId) || item;
                const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;
                const val = ItemRegistry.getValue(item);
                container.add(scene.add.text(0, offsetY, `${regItem.icon||''} ${regItem.name} [${rarity.name}] +${val}G`, {
                    fontSize: '11px', fontFamily: 'monospace', color: rarity.color
                }).setOrigin(0.5));
                offsetY += 18;
            });
        }

        if (lost.length > 0) {
            offsetY += 5;
            container.add(scene.add.text(0, offsetY, `❌ 손실 (${lost.length}개)`, {
                fontSize: '14px', fontFamily: 'monospace', color: '#ff4444'
            }).setOrigin(0.5));
            offsetY += 22;

            lost.forEach(item => {
                const regItem = ItemRegistry.get(item.itemId) || item;
                const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;
                container.add(scene.add.text(0, offsetY, `${regItem.icon||''} ${regItem.name} [${rarity.name}]`, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#aa4444'
                }).setOrigin(0.5));
                offsetY += 18;
            });
        }

        if (gold > 0) {
            offsetY += 5;
            container.add(scene.add.text(0, offsetY, `💰 +${gold} 골드`, {
                fontSize: '14px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
            }).setOrigin(0.5));
        }

        return container;
    },

    // ── 레이드 인벤토리 (툴팁 포함) ──
    drawRaidInventory(scene, x, y, raidInventory, onItemClick) {
        const container = scene.add.container(x, y);

        container.add(scene.add.text(0, -20, `🎒 획득 아이템 (${raidInventory.length}개)`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#cc8888'
        }).setOrigin(0.5));

        const cols = 5, cellW = 130, cellH = 40;
        const totalW = cols * cellW;
        const startX = -totalW / 2 + cellW / 2;

        raidInventory.forEach((item, i) => {
            const regItem = ItemRegistry.get(item.itemId) || item;
            const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;
            const col = i % cols;
            const row = Math.floor(i / cols);
            const ix = startX + col * cellW;
            const iy = row * cellH;

            const cell = scene.add.rectangle(ix, iy, cellW - 4, cellH - 4, 0x1a1111)
                .setStrokeStyle(1, rarity.borderColor).setInteractive();
            container.add(cell);
            container.add(scene.add.text(ix, iy, `${regItem.icon||''} ${regItem.name}`, {
                fontSize: '10px', fontFamily: 'monospace', color: rarity.color
            }).setOrigin(0.5));

            cell.on('pointerover', () => {
                cell.setFillStyle(0x2a1818);
                const worldPos = container.getWorldTransformMatrix();
                this.showTooltip(scene, item, worldPos.tx + ix, worldPos.ty + iy);
            });
            cell.on('pointerout', () => {
                cell.setFillStyle(0x1a1111);
                this.hideTooltip();
            });
            if (onItemClick) {
                cell.on('pointerdown', () => {
                    this.hideTooltip();
                    onItemClick(item, i);
                });
            }
        });

        return container;
    }
};
