class PlaceholderBattleScene extends Phaser.Scene {
    constructor() { super('PlaceholderBattleScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.zoneKey = data.zoneKey;
        this.party = data.party;
    }

    create() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const zone = ZONE_DATA[this.zoneKey];

        this.add.rectangle(640, 360, 1280, 720, T.bg || 0x1a1510);

        // 비네팅
        const v = this.add.graphics();
        v.fillStyle(0x000000, 0.15);
        v.fillRect(0, 0, 1280, 4); v.fillRect(0, 716, 1280, 4);

        this.add.text(640, 80, zone.icon, { fontSize: '48px' }).setOrigin(0.5);
        this.add.text(640, 140, zone.name, {
            fontSize: '24px', fontFamily: T.fontFamily || 'monospace',
            color: zone.textColor, fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5);
        this.add.text(640, 170, zone.subtitle, {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5);

        this.add.text(640, 220, '탐사 진행 중...', {
            fontSize: '16px', fontFamily: T.fontFamily || 'monospace',
            color: T.textPrimary || '#e8d8c0'
        }).setOrigin(0.5);

        const barW = 400, barH = 16, barX = 640 - barW / 2, barY = 260;
        const barBg = this.add.graphics();
        barBg.fillStyle(T.panelFill || 0x2a2218, 1);
        barBg.fillRoundedRect(barX, barY, barW, barH, 5);
        barBg.lineStyle(1, T.panelStroke || 0x5a4a2a, 0.5);
        barBg.strokeRoundedRect(barX, barY, barW, barH, 5);

        const barFill = this.add.graphics();
        barFill.fillStyle(zone.color, 1);

        const result = RunSimulator.simulate(this.gameState, this.zoneKey, this.party);

        const eventText = this.add.text(640, 320, '', {
            fontSize: '12px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888',
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
                barFill.fillRoundedRect(barX, barY, barW * progress, barH, 5);
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
