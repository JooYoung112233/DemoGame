class HotelEventScene extends Phaser.Scene {
    constructor() {
        super('HotelEventScene');
    }

    init(data) {
        this.runState = data.runState;
        this.floorManager = data.floorManager;
        this.eventType = data.eventType;
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');

        const floor = this.floorManager.currentFloor;
        this.add.text(640, 50, floor + 'F', {
            fontSize: '36px', fontFamily: 'monospace', fontStyle: 'bold',
            color: this.eventType === 'shop' ? '#44aaff' : '#44ff88'
        }).setOrigin(0.5);

        if (this.eventType === 'shop') {
            this.createShop();
        } else {
            this.createEvent();
        }
    }

    createEvent() {
        const event = EVENT_DATA[Math.floor(Math.random() * EVENT_DATA.length)];

        this.add.text(640, 110, event.title, {
            fontSize: '24px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#ffffff'
        }).setOrigin(0.5);

        this.add.text(640, 150, event.desc, {
            fontSize: '14px', fontFamily: 'monospace',
            color: '#888888'
        }).setOrigin(0.5);

        const cardWidth = 300;
        const gap = 40;
        const count = event.choices.length;
        const totalWidth = count * cardWidth + (count - 1) * gap;
        const startX = (1280 - totalWidth) / 2 + cardWidth / 2;

        event.choices.forEach((choice, i) => {
            const cx = startX + i * (cardWidth + gap);
            const cy = 340;

            const card = this.add.graphics();
            card.fillStyle(0x151530, 1);
            card.fillRoundedRect(cx - cardWidth / 2, cy - 80, cardWidth, 160, 10);
            card.lineStyle(2, 0x44ff88, 0.5);
            card.strokeRoundedRect(cx - cardWidth / 2, cy - 80, cardWidth, 160, 10);

            this.add.text(cx, cy - 40, choice.text, {
                fontSize: '20px', fontFamily: 'monospace', fontStyle: 'bold',
                color: '#ffffff'
            }).setOrigin(0.5);

            this.add.text(cx, cy + 10, choice.desc, {
                fontSize: '13px', fontFamily: 'monospace',
                color: '#aaaaaa', wordWrap: { width: cardWidth - 30 },
                align: 'center'
            }).setOrigin(0.5);

            const hitArea = this.add.rectangle(cx, cy, cardWidth, 160, 0x000000, 0)
                .setInteractive({ useHandCursor: true });

            hitArea.on('pointerover', () => {
                card.clear();
                card.fillStyle(0x1a2a1a, 1);
                card.fillRoundedRect(cx - cardWidth / 2, cy - 80, cardWidth, 160, 10);
                card.lineStyle(2, 0x44ff88, 1);
                card.strokeRoundedRect(cx - cardWidth / 2, cy - 80, cardWidth, 160, 10);
            });

            hitArea.on('pointerout', () => {
                card.clear();
                card.fillStyle(0x151530, 1);
                card.fillRoundedRect(cx - cardWidth / 2, cy - 80, cardWidth, 160, 10);
                card.lineStyle(2, 0x44ff88, 0.5);
                card.strokeRoundedRect(cx - cardWidth / 2, cy - 80, cardWidth, 160, 10);
            });

            hitArea.on('pointerdown', () => {
                const result = choice.apply(this.runState);
                if (result === 'random_upgrade') {
                    const upgrade = HOTEL_UPGRADES[Math.floor(Math.random() * HOTEL_UPGRADES.length)];
                    upgrade.apply(this.runState);
                    this.runState.appliedUpgrades.push({ name: upgrade.name, color: upgrade.color, id: upgrade.id });
                }
                this.goToNextFloor();
            });
        });

        this.drawStatusBar();
    }

    createShop() {
        this.add.text(640, 100, '상점', {
            fontSize: '24px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#44aaff'
        }).setOrigin(0.5);

        this.add.text(640, 135, '코인: ' + this.runState.coins, {
            fontSize: '16px', fontFamily: 'monospace',
            color: '#ffcc44'
        }).setOrigin(0.5);

        const cardWidth = 220;
        const gap = 25;
        const items = SHOP_ITEMS;
        const totalWidth = items.length * cardWidth + (items.length - 1) * gap;
        const startX = (1280 - totalWidth) / 2 + cardWidth / 2;

        items.forEach((item, i) => {
            const cx = startX + i * (cardWidth + gap);
            const cy = 340;
            const canAfford = this.runState.coins >= item.cost;

            const card = this.add.graphics();
            card.fillStyle(0x151530, 1);
            card.fillRoundedRect(cx - cardWidth / 2, cy - 100, cardWidth, 200, 10);
            card.lineStyle(2, canAfford ? 0x44aaff : 0x333344, 0.5);
            card.strokeRoundedRect(cx - cardWidth / 2, cy - 100, cardWidth, 200, 10);

            this.add.text(cx, cy - 60, item.name, {
                fontSize: '18px', fontFamily: 'monospace', fontStyle: 'bold',
                color: canAfford ? '#ffffff' : '#555555'
            }).setOrigin(0.5);

            this.add.text(cx, cy - 20, item.desc, {
                fontSize: '12px', fontFamily: 'monospace',
                color: canAfford ? '#aaaaaa' : '#444444',
                wordWrap: { width: cardWidth - 20 }, align: 'center'
            }).setOrigin(0.5);

            this.add.text(cx, cy + 30, item.cost + ' 코인', {
                fontSize: '16px', fontFamily: 'monospace', fontStyle: 'bold',
                color: canAfford ? '#ffcc44' : '#664422'
            }).setOrigin(0.5);

            if (canAfford) {
                const hitArea = this.add.rectangle(cx, cy, cardWidth, 200, 0x000000, 0)
                    .setInteractive({ useHandCursor: true });

                hitArea.on('pointerover', () => {
                    card.clear();
                    card.fillStyle(0x1a1a30, 1);
                    card.fillRoundedRect(cx - cardWidth / 2, cy - 100, cardWidth, 200, 10);
                    card.lineStyle(2, 0x44aaff, 1);
                    card.strokeRoundedRect(cx - cardWidth / 2, cy - 100, cardWidth, 200, 10);
                });

                hitArea.on('pointerout', () => {
                    card.clear();
                    card.fillStyle(0x151530, 1);
                    card.fillRoundedRect(cx - cardWidth / 2, cy - 100, cardWidth, 200, 10);
                    card.lineStyle(2, 0x44aaff, 0.5);
                    card.strokeRoundedRect(cx - cardWidth / 2, cy - 100, cardWidth, 200, 10);
                });

                hitArea.on('pointerdown', () => {
                    this.runState.coins -= item.cost;
                    const result = item.apply(this.runState);
                    if (result === 'random_upgrade') {
                        const upgrade = HOTEL_UPGRADES[Math.floor(Math.random() * HOTEL_UPGRADES.length)];
                        upgrade.apply(this.runState);
                        this.runState.appliedUpgrades.push({ name: upgrade.name, color: upgrade.color, id: upgrade.id });
                    }
                    this.goToNextFloor();
                });
            }
        });

        const skipBtn = this.add.text(640, 560, '건너뛰기 →', {
            fontSize: '16px', fontFamily: 'monospace',
            color: '#666666'
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        skipBtn.on('pointerover', () => skipBtn.setColor('#aaaaaa'));
        skipBtn.on('pointerout', () => skipBtn.setColor('#666666'));
        skipBtn.on('pointerdown', () => this.goToNextFloor());

        this.drawStatusBar();
    }

    drawStatusBar() {
        const hp = this.runState.hp;
        const maxHp = this.runState.maxHp;
        this.add.text(640, 640, 'HP: ' + hp + '/' + maxHp + '  |  SP: ' + (this.runState.stamina || 0) + '/' + (this.runState.maxStamina || 0) + '  |  코인: ' + this.runState.coins, {
            fontSize: '14px', fontFamily: 'monospace',
            color: '#666666'
        }).setOrigin(0.5);
    }

    goToNextFloor() {
        this.scene.start('HotelBattleScene', {
            runState: this.runState,
            floorManager: this.floorManager
        });
    }
}
