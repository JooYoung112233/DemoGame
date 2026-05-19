class ExpeditionScene extends Phaser.Scene {
    constructor() { super('ExpeditionScene'); }

    create() {
        const gs = window.gameState;
        const cx = 640;

        this.add.rectangle(cx, 360, 1280, 720, 0x0a0a1a);

        this.add.text(cx, 40, '☀️ 원정 구역 선택', {
            fontSize: '28px', fontFamily: 'monospace', color: '#ffcc44',
            stroke: '#000', strokeThickness: 3,
        }).setOrigin(0.5);

        this.add.text(cx, 75, `Day ${gs.day} — 어디로 떠나시겠습니까?`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#888',
        }).setOrigin(0.5);

        const zones = gs.getUnlockedZones();
        const allZones = Object.entries(ZONE_DATA);

        let y = 130;
        allZones.forEach(([id, zone]) => {
            const unlocked = gs.day >= zone.unlockDay;
            const panel = this.add.rectangle(cx, y + 45, 600, 80, unlocked ? 0x1a1a30 : 0x111120, 0.9);
            panel.setStrokeStyle(1, unlocked ? 0x3a3a60 : 0x222235);

            const icon = this.add.text(cx - 260, y + 30, zone.icon, { fontSize: '32px' });
            const name = this.add.text(cx - 210, y + 25, zone.name, {
                fontSize: '18px', fontFamily: 'monospace', color: unlocked ? '#fff' : '#555',
            });
            const stars = '★'.repeat(zone.difficulty) + '☆'.repeat(4 - zone.difficulty);
            this.add.text(cx - 210, y + 50, `${stars}  ${zone.desc}`, {
                fontSize: '12px', fontFamily: 'monospace', color: unlocked ? '#888' : '#444',
            });

            const drops = zone.drops.slice(0, 4).map(d => ITEM_DATA[d.id]?.icon || '?').join(' ');
            this.add.text(cx + 140, y + 35, drops, { fontSize: '16px' });

            if (unlocked) {
                panel.setInteractive({ useHandCursor: true });
                panel.on('pointerover', () => panel.setFillStyle(0x2a2a40));
                panel.on('pointerout', () => panel.setFillStyle(0x1a1a30));
                panel.on('pointerdown', () => this._startExpedition(id));
            } else {
                this.add.text(cx + 230, y + 35, `Day ${zone.unlockDay}+`, {
                    fontSize: '14px', fontFamily: 'monospace', color: '#ff4444',
                }).setOrigin(0.5);
            }

            y += 95;
        });

        new UIButton(this, 100, 680, '← 돌아가기', {
            width: 160, height: 40, bg: 0x333333, hoverBg: 0x555555,
            textColor: '#aaa', fontSize: '14px',
            onClick: () => this.scene.start('HubScene'),
        });

        this.resultContainer = this.add.container(0, 0).setVisible(false);
    }

    _startExpedition(zoneId) {
        const gs = window.gameState;
        const result = ExpeditionSystem.run(zoneId);

        gs.addItems(result.items);

        this.resultContainer.destroy();
        this.resultContainer = this.add.container(0, 0);

        const overlay = this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.7);
        const panel = this.add.rectangle(640, 360, 500, 400, 0x151530, 0.98);
        panel.setStrokeStyle(2, 0x4466aa);
        this.resultContainer.add([overlay, panel]);

        this.resultContainer.add(this.add.text(640, 200, '🎒 원정 결과', {
            fontSize: '24px', fontFamily: 'monospace', color: '#ffcc44',
        }).setOrigin(0.5));

        let y = 250;
        result.items.forEach(item => {
            const data = ITEM_DATA[item.id];
            this.resultContainer.add(this.add.text(480, y, `${data.icon} ${data.name} x${item.qty}`, {
                fontSize: '16px', fontFamily: 'monospace', color: '#ddd',
            }));
            y += 28;
        });

        this.resultContainer.add(this.add.text(640, y + 20, `총 ${result.items.reduce((s, i) => s + i.qty, 0)}개 획득!`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#44ff88',
        }).setOrigin(0.5));

        const btn = new UIButton(this, 640, y + 70, '확인', {
            width: 160, height: 44, bg: 0x224422, hoverBg: 0x336633,
            textColor: '#44ff88', fontSize: '16px',
            onClick: () => this.scene.start('HubScene'),
        });
        this.resultContainer.add(btn.container);
    }
}
