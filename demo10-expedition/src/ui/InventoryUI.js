// Tarkov-style grid inventory UI
// Uses individual scrollFactor(0) elements instead of container to fix input bug

class InventoryUI {
    constructor(scene, inventory) {
        this.scene = scene;
        this.inventory = inventory;
        this.isOpen = false;
        this.cellSize = 56;
        this.padding = 4;
        this.elements = [];
    }

    toggle() {
        this.isOpen = !this.isOpen;
        if (this.isOpen) this.render();
        else this._cleanup();
    }

    open() {
        this.isOpen = true;
        this.render();
    }

    close() {
        this.isOpen = false;
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

        const cols = this.inventory.cols;
        const rows = this.inventory.rows;
        const cs = this.cellSize;
        const pad = this.padding;

        const gridW = cols * (cs + pad) + pad;
        const gridH = rows * (cs + pad) + pad;

        // Position: right side of screen
        const offsetX = 1280 - gridW - 20;
        const offsetY = 60;

        // Panel background
        const panelBg = this._addEl(this.scene.add.graphics());
        panelBg.fillStyle(0x1a1a2a, 0.95);
        panelBg.fillRoundedRect(offsetX - 10, offsetY - 40, gridW + 20, gridH + 80, 8);
        panelBg.lineStyle(2, 0x444466, 1);
        panelBg.strokeRoundedRect(offsetX - 10, offsetY - 40, gridW + 20, gridH + 80, 8);

        // Title
        this._addEl(this.scene.add.text(offsetX + gridW / 2, offsetY - 20, '🎒 인벤토리', {
            fontSize: '16px', fontFamily: 'monospace', color: '#cccccc', fontStyle: 'bold'
        }).setOrigin(0.5));

        // Weight info
        this._addEl(this.scene.add.text(offsetX + gridW / 2, offsetY + gridH + 10,
            `${this.inventory.currentWeight.toFixed(1)} / ${this.inventory.maxWeight} kg  |  가치: ${this.inventory.getTotalValue()}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5));

        // Draw grid cells
        const gridBg = this._addEl(this.scene.add.graphics());
        for (let r = 0; r < rows; r++) {
            for (let c = 0; c < cols; c++) {
                const x = offsetX + pad + c * (cs + pad);
                const y = offsetY + pad + r * (cs + pad);
                gridBg.fillStyle(0x222244, 1);
                gridBg.lineStyle(1, 0x333355, 1);
                gridBg.fillRect(x, y, cs, cs);
                gridBg.strokeRect(x, y, cs, cs);
            }
        }

        // Draw items
        const drawnItems = new Set();
        for (const entry of this.inventory.items) {
            if (drawnItems.has(entry.uid)) continue;
            drawnItems.add(entry.uid);

            const x = offsetX + pad + entry.col * (cs + pad);
            const y = offsetY + pad + entry.row * (cs + pad);
            const w = entry.item.w * cs + (entry.item.w - 1) * pad;
            const h = entry.item.h * cs + (entry.item.h - 1) * pad;

            const rarityColors = { common: 0x445544, uncommon: 0x445566, rare: 0x665544 };
            const bg = this._addEl(this.scene.add.graphics());
            bg.fillStyle(rarityColors[entry.item.rarity] || 0x444444, 1);
            bg.lineStyle(1, 0x666688, 1);
            bg.fillRect(x, y, w, h);
            bg.strokeRect(x, y, w, h);

            this._addEl(this.scene.add.text(x + w / 2, y + h / 2 - 6, entry.item.icon, {
                fontSize: `${Math.min(w, h) * 0.5}px`
            }).setOrigin(0.5));

            this._addEl(this.scene.add.text(x + w / 2, y + h - 8, entry.item.name, {
                fontSize: '9px', fontFamily: 'monospace', color: '#aaaaaa'
            }).setOrigin(0.5));

            // Click to drop item
            const hitArea = this._addEl(this.scene.add.rectangle(x + w / 2, y + h / 2, w, h, 0x000000, 0));
            hitArea.setDepth(201).setInteractive({ useHandCursor: true });

            const capturedEntry = entry;
            hitArea.on('pointerdown', () => {
                this.inventory.removeItem(capturedEntry);
                this.render();
            });
        }

        // Close hint
        this._addEl(this.scene.add.text(offsetX + gridW / 2, offsetY + gridH + 30,
            '[TAB] 닫기  |  클릭: 버리기', {
            fontSize: '10px', fontFamily: 'monospace', color: '#666666'
        }).setOrigin(0.5));
    }

    destroy() {
        this._cleanup();
    }
}
