class InventoryScene extends Phaser.Scene {
    constructor() { super('InventoryScene'); }

    create() {
        const gs = window.gameState;
        const cx = 640;

        this.add.rectangle(cx, 360, 1280, 720, 0x0a0a1a);

        this.add.text(cx, 40, '🎒 인벤토리', {
            fontSize: '28px', fontFamily: 'monospace', color: '#44ff88',
            stroke: '#000', strokeThickness: 3,
        }).setOrigin(0.5);

        this.add.text(cx, 75, `💰 ${gs.gold}G  |  아이템 ${gs.inventory.reduce((s, i) => s + i.qty, 0)}개`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#888',
        }).setOrigin(0.5);

        const items = gs.inventory.filter(i => i.qty > 0);

        if (items.length === 0) {
            this.add.text(cx, 300, '인벤토리가 비어있습니다.', {
                fontSize: '18px', fontFamily: 'monospace', color: '#555',
            }).setOrigin(0.5);
        } else {
            items.forEach((item, idx) => {
                const data = ITEM_DATA[item.id];
                if (!data) return;
                const col = idx % 4;
                const row = Math.floor(idx / 4);
                const x = 240 + col * 210;
                const y = 120 + row * 80;

                const catData = CATEGORIES[data.category];
                const bg = this.add.rectangle(x + 80, y + 28, 190, 60, 0x1a1a35);
                bg.setStrokeStyle(1, 0x2a2a50);

                this.add.text(x + 10, y + 10, data.icon, { fontSize: '24px' });
                this.add.text(x + 40, y + 8, data.name, {
                    fontSize: '14px', fontFamily: 'monospace', color: '#ddd',
                });
                this.add.text(x + 40, y + 28, `x${item.qty}  |  ${data.basePrice}G`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#888',
                });
                this.add.text(x + 40, y + 44, catData ? catData.name : '', {
                    fontSize: '10px', fontFamily: 'monospace', color: catData ? catData.color : '#666',
                });
            });
        }

        new UIButton(this, 100, 680, '← 돌아가기', {
            width: 160, height: 40, bg: 0x333333, hoverBg: 0x555555,
            textColor: '#aaa', fontSize: '14px',
            onClick: () => this.scene.start('HubScene'),
        });
    }
}
