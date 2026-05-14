class HotelUpgradeScene extends Phaser.Scene {
    constructor() {
        super('HotelUpgradeScene');
    }

    init(data) {
        this.runState = data.runState;
        this.floorManager = data.floorManager;
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');

        const floor = this.floorManager.currentFloor;
        this.add.text(640, 50, floor + 'F 클리어!', {
            fontSize: '28px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#44ff88'
        }).setOrigin(0.5);

        this.add.text(640, 90, '강화를 선택하세요', {
            fontSize: '16px', fontFamily: 'monospace',
            color: '#888888'
        }).setOrigin(0.5);

        this.drawProgressBar();

        const available = [...HOTEL_UPGRADES];
        const shuffled = available.sort(() => Math.random() - 0.5);
        const choices = shuffled.slice(0, 3);

        const cardWidth = 240;
        const gap = 40;
        const totalWidth = 3 * cardWidth + 2 * gap;
        const startX = (1280 - totalWidth) / 2 + cardWidth / 2;

        choices.forEach((upgrade, i) => {
            const cx = startX + i * (cardWidth + gap);
            const cy = 340;

            const card = this.add.graphics();
            card.fillStyle(0x151530, 1);
            card.fillRoundedRect(cx - cardWidth / 2, cy - 110, cardWidth, 220, 10);
            card.lineStyle(2, 0x333366, 1);
            card.strokeRoundedRect(cx - cardWidth / 2, cy - 110, cardWidth, 220, 10);

            const colorDot = this.add.graphics();
            colorDot.fillStyle(Phaser.Display.Color.HexStringToColor(upgrade.color).color, 1);
            colorDot.fillCircle(cx, cy - 70, 8);

            this.add.text(cx, cy - 40, upgrade.name, {
                fontSize: '20px', fontFamily: 'monospace', fontStyle: 'bold',
                color: '#ffffff'
            }).setOrigin(0.5);

            this.add.text(cx, cy + 5, upgrade.desc, {
                fontSize: '13px', fontFamily: 'monospace',
                color: '#aaaaaa', wordWrap: { width: cardWidth - 30 },
                align: 'center'
            }).setOrigin(0.5);

            const hitArea = this.add.rectangle(cx, cy, cardWidth, 220, 0x000000, 0)
                .setInteractive({ useHandCursor: true });

            hitArea.on('pointerover', () => {
                card.clear();
                card.fillStyle(0x1a1a40, 1);
                card.fillRoundedRect(cx - cardWidth / 2, cy - 110, cardWidth, 220, 10);
                card.lineStyle(2, Phaser.Display.Color.HexStringToColor(upgrade.color).color, 1);
                card.strokeRoundedRect(cx - cardWidth / 2, cy - 110, cardWidth, 220, 10);
            });

            hitArea.on('pointerout', () => {
                card.clear();
                card.fillStyle(0x151530, 1);
                card.fillRoundedRect(cx - cardWidth / 2, cy - 110, cardWidth, 220, 10);
                card.lineStyle(2, 0x333366, 1);
                card.strokeRoundedRect(cx - cardWidth / 2, cy - 110, cardWidth, 220, 10);
            });

            hitArea.on('pointerdown', () => {
                this.selectUpgrade(upgrade);
            });
        });

        this.drawAppliedUpgrades();
    }

    drawProgressBar() {
        const barX = 240, barY = 130, barW = 800, barH = 8;
        const g = this.add.graphics();
        const floor = this.floorManager.currentFloor;
        const maxFloor = this.floorManager.maxFloor;

        g.fillStyle(0x222244, 1);
        g.fillRoundedRect(barX, barY, barW, barH, 4);

        g.fillStyle(0xff4466, 1);
        g.fillRoundedRect(barX, barY, barW * (floor / maxFloor), barH, 4);

        for (let i = 1; i <= maxFloor; i++) {
            const px = barX + (barW / maxFloor) * i;
            const done = i <= floor;
            this.add.text(px, barY + 16, i + '', {
                fontSize: '10px', fontFamily: 'monospace',
                color: done ? '#ff4466' : '#444466'
            }).setOrigin(0.5, 0);
        }
    }

    drawAppliedUpgrades() {
        const upgrades = this.runState.appliedUpgrades;
        if (upgrades.length === 0) return;

        this.add.text(640, 510, '적용된 강화', {
            fontSize: '12px', fontFamily: 'monospace',
            color: '#555566'
        }).setOrigin(0.5);

        const startX = 640 - (upgrades.length * 28) / 2;
        upgrades.forEach((u, i) => {
            const g = this.add.graphics();
            const c = Phaser.Display.Color.HexStringToColor(u.color).color;
            g.fillStyle(c, 0.6);
            g.fillCircle(startX + i * 28, 545, 8);
        });
    }

    selectUpgrade(upgrade) {
        const result = upgrade.apply(this.runState);
        this.runState.appliedUpgrades.push({ name: upgrade.name, color: upgrade.color, id: upgrade.id });

        if (result === 'random_upgrade') {
            const extra = HOTEL_UPGRADES[Math.floor(Math.random() * HOTEL_UPGRADES.length)];
            extra.apply(this.runState);
            this.runState.appliedUpgrades.push({ name: extra.name, color: extra.color, id: extra.id });
        }

        this.goToNextFloor();
    }

    goToNextFloor() {
        this.scene.start('HotelBattleScene', {
            runState: this.runState,
            floorManager: this.floorManager
        });
    }
}
