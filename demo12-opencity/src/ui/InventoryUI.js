// Tarkov-style grid inventory UI
// Opens with TAB, shows 5x4 grid with item slots

class InventoryUI {
    constructor(scene, inventory) {
        this.scene = scene;
        this.inventory = inventory;
        this.isOpen = false;
        this.cellSize = 56;
        this.padding = 4;
        this.offsetX = 0;
        this.offsetY = 0;

        // Container (hidden initially)
        this.container = scene.add.container(0, 0).setScrollFactor(0).setDepth(200).setVisible(false);

        // Dragging state
        this.dragItem = null;
        this.dragOrigin = null;
    }

    toggle() {
        this.isOpen = !this.isOpen;
        this.container.setVisible(this.isOpen);
        if (this.isOpen) this.render();
    }

    open() {
        this.isOpen = true;
        this.container.setVisible(true);
        this.render();
    }

    close() {
        this.isOpen = false;
        this.container.setVisible(false);
    }

    render() {
        this.container.removeAll(true);

        const cols = this.inventory.cols;
        const rows = this.inventory.rows;
        const cs = this.cellSize;
        const pad = this.padding;

        const gridW = cols * (cs + pad) + pad;
        const gridH = rows * (cs + pad) + pad;

        // Position: right side of screen
        this.offsetX = 1280 - gridW - 20;
        this.offsetY = 60;

        // Panel background
        const panelBg = this.scene.add.graphics();
        panelBg.fillStyle(0x1a1a2a, 0.95);
        panelBg.fillRoundedRect(this.offsetX - 10, this.offsetY - 40, gridW + 20, gridH + 80, 8);
        panelBg.lineStyle(2, 0x444466, 1);
        panelBg.strokeRoundedRect(this.offsetX - 10, this.offsetY - 40, gridW + 20, gridH + 80, 8);
        this.container.add(panelBg);

        // Title
        const title = this.scene.add.text(this.offsetX + gridW / 2, this.offsetY - 20, '🎒 인벤토리', {
            fontSize: '16px', fontFamily: 'monospace', color: '#cccccc', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.container.add(title);

        // Weight info
        const weightInfo = this.scene.add.text(this.offsetX + gridW / 2, this.offsetY + gridH + 10,
            `${this.inventory.currentWeight.toFixed(1)} / ${this.inventory.maxWeight} kg  |  가치: ${this.inventory.getTotalValue()}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);
        this.container.add(weightInfo);

        // Draw grid cells
        for (let r = 0; r < rows; r++) {
            for (let c = 0; c < cols; c++) {
                const x = this.offsetX + pad + c * (cs + pad);
                const y = this.offsetY + pad + r * (cs + pad);

                const cellBg = this.scene.add.graphics();
                cellBg.fillStyle(0x222244, 1);
                cellBg.lineStyle(1, 0x333355, 1);
                cellBg.fillRect(x, y, cs, cs);
                cellBg.strokeRect(x, y, cs, cs);
                this.container.add(cellBg);
            }
        }

        // Draw items
        const drawnItems = new Set();
        for (const entry of this.inventory.items) {
            if (drawnItems.has(entry.uid)) continue;
            drawnItems.add(entry.uid);

            const x = this.offsetX + pad + entry.col * (cs + pad);
            const y = this.offsetY + pad + entry.row * (cs + pad);
            const w = entry.item.w * cs + (entry.item.w - 1) * pad;
            const h = entry.item.h * cs + (entry.item.h - 1) * pad;

            // Item background
            const rarityColors = { common: 0x445544, uncommon: 0x445566, rare: 0x665544 };
            const bg = this.scene.add.graphics();
            bg.fillStyle(rarityColors[entry.item.rarity] || 0x444444, 1);
            bg.lineStyle(1, 0x666688, 1);
            bg.fillRect(x, y, w, h);
            bg.strokeRect(x, y, w, h);
            this.container.add(bg);

            // Item icon
            const icon = this.scene.add.text(x + w / 2, y + h / 2 - 6, entry.item.icon, {
                fontSize: `${Math.min(w, h) * 0.5}px`
            }).setOrigin(0.5);
            this.container.add(icon);

            // Item name (small)
            const name = this.scene.add.text(x + w / 2, y + h - 8, entry.item.name, {
                fontSize: '9px', fontFamily: 'monospace', color: '#aaaaaa'
            }).setOrigin(0.5);
            this.container.add(name);

            // Click to drop item
            const hitArea = this.scene.add.rectangle(x + w / 2, y + h / 2, w, h, 0x000000, 0)
                .setScrollFactor(0).setDepth(201).setInteractive({ useHandCursor: true });
            this.container.add(hitArea);

            hitArea.on('pointerdown', () => {
                this.inventory.removeItem(entry);
                this.render();
            });
        }

        // Close hint
        const closeHint = this.scene.add.text(this.offsetX + gridW / 2, this.offsetY + gridH + 30,
            '[TAB] 닫기  |  클릭: 아이템 버리기', {
            fontSize: '10px', fontFamily: 'monospace', color: '#666666'
        }).setOrigin(0.5);
        this.container.add(closeHint);
    }

    destroy() {
        this.container.destroy();
    }
}
