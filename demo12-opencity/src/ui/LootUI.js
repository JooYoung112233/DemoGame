// Loot popup UI — shows items found at a search spot
// Player can click items to add to inventory

class LootUI {
    constructor(scene, inventory) {
        this.scene = scene;
        this.inventory = inventory;
        this.isOpen = false;
        this.items = [];
        this.container = scene.add.container(0, 0).setScrollFactor(0).setDepth(200).setVisible(false);
    }

    open(items) {
        this.items = items.slice(); // copy
        this.isOpen = true;
        this.container.setVisible(true);
        this.render();
    }

    close() {
        this.isOpen = false;
        this.items = [];
        this.container.setVisible(false);
        this.container.removeAll(true);
    }

    render() {
        this.container.removeAll(true);

        const panelW = 320;
        const itemH = 50;
        const panelH = Math.max(100, this.items.length * itemH + 80);
        const px = 640 - panelW / 2;
        const py = 360 - panelH / 2;

        // Background
        const bg = this.scene.add.graphics();
        bg.fillStyle(0x1a1a2a, 0.95);
        bg.lineStyle(2, 0x886644, 1);
        bg.fillRoundedRect(px, py, panelW, panelH, 8);
        bg.strokeRoundedRect(px, py, panelW, panelH, 8);
        this.container.add(bg);

        // Title
        const title = this.scene.add.text(640, py + 16, '📦 발견한 물품', {
            fontSize: '16px', fontFamily: 'monospace', color: '#cc8844', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.container.add(title);

        // Items list
        for (let i = 0; i < this.items.length; i++) {
            const item = this.items[i];
            const y = py + 40 + i * itemH;

            // Item row background
            const rowBg = this.scene.add.graphics();
            rowBg.fillStyle(0x222244, 0.8);
            rowBg.fillRect(px + 8, y, panelW - 16, itemH - 4);
            this.container.add(rowBg);

            // Icon
            const icon = this.scene.add.text(px + 30, y + itemH / 2 - 2, item.icon, {
                fontSize: '22px'
            }).setOrigin(0.5);
            this.container.add(icon);

            // Name + info
            const rarityColor = item.rarity === 'rare' ? '#ffaa44' :
                                item.rarity === 'uncommon' ? '#44aaff' : '#aaaaaa';
            const nameText = this.scene.add.text(px + 55, y + 8, item.name, {
                fontSize: '13px', fontFamily: 'monospace', color: rarityColor, fontStyle: 'bold'
            });
            this.container.add(nameText);

            const infoText = this.scene.add.text(px + 55, y + 26,
                `${item.w}×${item.h} | ${item.weight}kg | 💰${item.value}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#666666'
            });
            this.container.add(infoText);

            // Take button
            const canTake = this.inventory.currentWeight + item.weight <= this.inventory.maxWeight;
            const takeBtn = this.scene.add.text(px + panelW - 60, y + itemH / 2 - 2,
                canTake ? '[ 획득 ]' : '[ 무거움 ]', {
                fontSize: '12px', fontFamily: 'monospace',
                color: canTake ? '#44ff88' : '#ff4444',
                padding: { x: 4, y: 2 }
            }).setOrigin(0.5);
            this.container.add(takeBtn);

            if (canTake) {
                takeBtn.setInteractive({ useHandCursor: true });
                takeBtn.on('pointerover', () => takeBtn.setColor('#88ffaa'));
                takeBtn.on('pointerout', () => takeBtn.setColor('#44ff88'));
                takeBtn.on('pointerdown', () => {
                    if (this.inventory.autoAdd(item)) {
                        this.items.splice(i, 1);
                        this.render();
                    }
                });
            }
        }

        // Close / Take All buttons
        const bottomY = py + panelH - 28;

        if (this.items.length > 0) {
            const takeAllBtn = this.scene.add.text(px + panelW / 2 - 50, bottomY, '[ 전부 획득 ]', {
                fontSize: '13px', fontFamily: 'monospace', color: '#44ccff',
                padding: { x: 8, y: 4 }
            }).setOrigin(0.5).setInteractive({ useHandCursor: true });
            takeAllBtn.on('pointerdown', () => {
                const taken = [];
                for (const item of this.items) {
                    if (this.inventory.autoAdd(item)) taken.push(item);
                }
                this.items = this.items.filter(it => !taken.includes(it));
                if (this.items.length === 0) {
                    this.close();
                } else {
                    this.render();
                }
            });
            this.container.add(takeAllBtn);
        }

        const closeBtn = this.scene.add.text(px + panelW / 2 + 50, bottomY, '[ 닫기 ]', {
            fontSize: '13px', fontFamily: 'monospace', color: '#888888',
            padding: { x: 8, y: 4 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });
        closeBtn.on('pointerdown', () => this.close());
        this.container.add(closeBtn);

        // Show message if empty
        if (this.items.length === 0) {
            const emptyText = this.scene.add.text(640, py + panelH / 2, '아무것도 남지 않았다', {
                fontSize: '14px', fontFamily: 'monospace', color: '#666666'
            }).setOrigin(0.5);
            this.container.add(emptyText);
        }
    }

    destroy() {
        this.container.destroy();
    }
}
