class PlaceholderBattleScene extends Phaser.Scene {
    constructor() { super('PlaceholderBattleScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.zoneKey = data.zoneKey;
        this.party = data.party;
    }

    create() {
        const zone = ZONE_DATA[this.zoneKey];
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);

        this.add.text(640, 80, zone.icon, { fontSize: '48px' }).setOrigin(0.5);
        this.add.text(640, 140, zone.name, {
            fontSize: '24px', fontFamily: 'monospace', color: zone.textColor, fontStyle: 'bold'
        }).setOrigin(0.5);
        this.add.text(640, 170, zone.subtitle, {
            fontSize: '14px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5);

        this.add.text(640, 220, '탐사 진행 중...', {
            fontSize: '16px', fontFamily: 'monospace', color: '#aaaacc'
        }).setOrigin(0.5);

        const barW = 400, barH = 16, barX = 640 - barW / 2, barY = 260;
        const barBg = this.add.graphics();
        barBg.fillStyle(0x222233, 1);
        barBg.fillRoundedRect(barX, barY, barW, barH, 4);

        const barFill = this.add.graphics();
        barFill.fillStyle(zone.color, 1);

        const result = RunSimulator.simulate(this.gameState, this.zoneKey, this.party);

        const eventText = this.add.text(640, 320, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#8888aa',
            align: 'center', lineSpacing: 6
        }).setOrigin(0.5, 0);

        const totalDuration = 3000;
        const eventInterval = totalDuration / result.events.length;
        let shownEvents = [];

        result.events.forEach((evt, idx) => {
            this.time.delayedCall(eventInterval * idx, () => {
                shownEvents.push(evt);
                if (shownEvents.length > 8) shownEvents = shownEvents.slice(-8);
                eventText.setText(shownEvents.join('\n'));

                barFill.clear();
                barFill.fillStyle(zone.color, 1);
                const progress = (idx + 1) / result.events.length;
                barFill.fillRoundedRect(barX, barY, barW * progress, barH, 4);
            });
        });

        this.time.delayedCall(totalDuration + 500, () => {
            this.scene.start('RunResultScene', {
                gameState: this.gameState,
                result
            });
        });
    }
}
