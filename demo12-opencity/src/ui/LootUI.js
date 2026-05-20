// Loot popup UI — shows items found at a search spot
// Player can click items to add to inventory
// Uses fixed-position elements (not container) to avoid scroll factor input bug

class LootUI {
    constructor(scene, inventory, inventoryUI) {
        this.scene = scene;
        this.inventory = inventory;
        this.inventoryUI = inventoryUI; // reference to refresh on take
        this.isOpen = false;
        this.items = [];
        this.elements = [];
    }

    open(items) {
        this.items = items.slice();
        this.isOpen = true;
        this.render();
    }

    close() {
        this.isOpen = false;
        this.items = [];
        this._cleanup();
    }

    _cleanup() {
        for (const el of this.elements) {
            if (el && el.destroy) el.destroy();
        }
        this.elements = [];
    }

    _addEl(el) {
        if (el) {
            el.setScrollFactor(0).setDepth(200);
            this.elements.push(el);
        }
        return el;
    }

    render() {
        this._cleanup();

        const panelW = 300;
        const itemH = 52;
        const panelH = Math.max(120, this.items.length * itemH + 90);
        const px = 20; // left side
        const py = 60;

        // Background
        const bg = this._addEl(this.scene.add.graphics());
        bg.fillStyle(0x1a1a2a, 0.95);
        bg.lineStyle(2, 0x886644, 1);
        bg.fillRoundedRect(px, py, panelW, panelH, 8);
        bg.strokeRoundedRect(px, py, panelW, panelH, 8);

        // Title
        this._addEl(this.scene.add.text(px + panelW / 2, py + 18, '📦 발견한 물품', {
            fontSize: '16px', fontFamily: 'monospace', color: '#cc8844', fontStyle: 'bold'
        }).setOrigin(0.5));

        // Items list
        for (let i = 0; i < this.items.length; i++) {
            const item = this.items[i];
            const y = py + 40 + i * itemH;

            // Item row background
            const rowBg = this._addEl(this.scene.add.graphics());
            rowBg.fillStyle(0x222244, 0.8);
            rowBg.fillRect(px + 8, y, panelW - 16, itemH - 4);

            // Icon
            this._addEl(this.scene.add.text(px + 28, y + itemH / 2 - 2, item.icon, {
                fontSize: '22px'
            }).setOrigin(0.5));

            // Name + info
            const rarityColor = item.rarity === 'rare' ? '#ffaa44' :
                                item.rarity === 'uncommon' ? '#44aaff' : '#aaaaaa';
            this._addEl(this.scene.add.text(px + 50, y + 8, item.name, {
                fontSize: '13px', fontFamily: 'monospace', color: rarityColor, fontStyle: 'bold'
            }));

            this._addEl(this.scene.add.text(px + 50, y + 26,
                `${item.w}×${item.h} | ${item.weight}kg | 💰${item.value}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#666666'
            }));

            // Take button — individual element with own scrollFactor
            const canTake = this.inventory.currentWeight + item.weight <= this.inventory.maxWeight;
            const takeBtn = this._addEl(this.scene.add.text(px + panelW - 50, y + itemH / 2 - 2,
                canTake ? '[ 획득 ]' : '[ 초과 ]', {
                fontSize: '12px', fontFamily: 'monospace',
                color: canTake ? '#44ff88' : '#ff4444',
                backgroundColor: canTake ? '#1a3a1a' : '#3a1a1a',
                padding: { x: 6, y: 4 }
            }).setOrigin(0.5));

            if (canTake) {
                takeBtn.setInteractive({ useHandCursor: true });
                takeBtn.on('pointerover', () => takeBtn.setColor('#88ffaa'));
                takeBtn.on('pointerout', () => takeBtn.setColor('#44ff88'));
                const idx = i; // capture index
                takeBtn.on('pointerdown', () => {
                    if (this.inventory.autoAdd(this.items[idx])) {
                        this.items.splice(idx, 1);
                        if (this.inventoryUI) this.inventoryUI.render();
                        if (this.items.length === 0) {
                            this.close();
                        } else {
                            this.render();
                        }
                    }
                });
            }
        }

        // Bottom buttons
        const bottomY = py + panelH - 26;

        if (this.items.length > 0) {
            const takeAllBtn = this._addEl(this.scene.add.text(px + panelW / 2 - 45, bottomY, '[ 전부 획득 ]', {
                fontSize: '13px', fontFamily: 'monospace', color: '#44ccff',
                backgroundColor: '#1a1a3a', padding: { x: 8, y: 4 }
            }).setOrigin(0.5));
            takeAllBtn.setInteractive({ useHandCursor: true });
            takeAllBtn.on('pointerover', () => takeAllBtn.setColor('#88eeff'));
            takeAllBtn.on('pointerout', () => takeAllBtn.setColor('#44ccff'));
            takeAllBtn.on('pointerdown', () => {
                const remaining = [];
                for (const item of this.items) {
                    if (!this.inventory.autoAdd(item)) remaining.push(item);
                }
                this.items = remaining;
                if (this.inventoryUI) this.inventoryUI.render();
                if (this.items.length === 0) {
                    this.close();
                } else {
                    this.render();
                }
            });
        }

        const closeBtn = this._addEl(this.scene.add.text(px + panelW / 2 + 45, bottomY, '[ 닫기 ]', {
            fontSize: '13px', fontFamily: 'monospace', color: '#888888',
            backgroundColor: '#1a1a1a', padding: { x: 8, y: 4 }
        }).setOrigin(0.5));
        closeBtn.setInteractive({ useHandCursor: true });
        closeBtn.on('pointerover', () => closeBtn.setColor('#cccccc'));
        closeBtn.on('pointerout', () => closeBtn.setColor('#888888'));
        closeBtn.on('pointerdown', () => this.close());

        if (this.items.length === 0) {
            this._addEl(this.scene.add.text(px + panelW / 2, py + panelH / 2, '아무것도 남지 않았다', {
                fontSize: '14px', fontFamily: 'monospace', color: '#666666'
            }).setOrigin(0.5));
        }
    }

    destroy() {
        this._cleanup();
    }
}
