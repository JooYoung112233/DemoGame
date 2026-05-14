class EventScene extends Phaser.Scene {
    constructor() {
        super({ key: 'EventScene' });
    }

    init(data) {
        this.runState = data.runState;
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');

        const event = EVENT_DATA[Math.floor(Math.random() * EVENT_DATA.length)];

        this.add.text(640, 60, event.title, {
            fontSize: '28px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#44ff88', stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5);

        this.add.text(640, 100, event.desc, {
            fontSize: '14px', fontFamily: 'monospace',
            color: '#888888', wordWrap: { width: 600 }, align: 'center'
        }).setOrigin(0.5);

        this.add.text(30, 30, `🪙 ${this.runState.gold}G`, {
            fontSize: '14px', fontFamily: 'monospace',
            color: '#ffcc44', stroke: '#000000', strokeThickness: 2
        });

        this.add.text(1250, 30, `파티: ${this.runState.party.length}/${this.runState.maxPartySize}`, {
            fontSize: '12px', fontFamily: 'monospace',
            color: '#44ff88', stroke: '#000000', strokeThickness: 2
        }).setOrigin(1, 0);

        const cardWidth = 400;
        const gap = 40;
        const count = event.choices.length;
        const totalWidth = count * cardWidth + (count - 1) * gap;
        const startX = (1280 - totalWidth) / 2 + cardWidth / 2;

        event.choices.forEach((choice, i) => {
            const cx = startX + i * (cardWidth + gap);
            const cy = 300;
            const canApply = choice.canApply(this.runState);

            const card = this.add.graphics();
            card.fillStyle(canApply ? 0x151530 : 0x0a0a15, 1);
            card.fillRoundedRect(cx - cardWidth / 2, cy - 100, cardWidth, 200, 12);
            card.lineStyle(2, canApply ? 0x44ff88 : 0x333344, 0.6);
            card.strokeRoundedRect(cx - cardWidth / 2, cy - 100, cardWidth, 200, 12);

            this.add.text(cx, cy - 60, choice.text, {
                fontSize: '20px', fontFamily: 'monospace', fontStyle: 'bold',
                color: canApply ? '#ffffff' : '#555555'
            }).setOrigin(0.5);

            this.add.text(cx, cy - 20, choice.desc, {
                fontSize: '12px', fontFamily: 'monospace',
                color: canApply ? '#aaaaaa' : '#444444',
                wordWrap: { width: cardWidth - 40 }, align: 'center'
            }).setOrigin(0.5);

            if (canApply) {
                const hitArea = this.add.rectangle(cx, cy, cardWidth, 200, 0x000000, 0)
                    .setInteractive({ useHandCursor: true });

                hitArea.on('pointerover', () => {
                    card.clear();
                    card.fillStyle(0x1a2040, 1);
                    card.fillRoundedRect(cx - cardWidth / 2, cy - 100, cardWidth, 200, 12);
                    card.lineStyle(2, 0x66ffaa, 0.8);
                    card.strokeRoundedRect(cx - cardWidth / 2, cy - 100, cardWidth, 200, 12);
                });

                hitArea.on('pointerout', () => {
                    card.clear();
                    card.fillStyle(0x151530, 1);
                    card.fillRoundedRect(cx - cardWidth / 2, cy - 100, cardWidth, 200, 12);
                    card.lineStyle(2, 0x44ff88, 0.6);
                    card.strokeRoundedRect(cx - cardWidth / 2, cy - 100, cardWidth, 200, 12);
                });

                hitArea.on('pointerdown', () => {
                    this.handleChoice(choice);
                });
            }
        });

        PartyHUD.draw(this, this.runState, 550);
    }

    handleChoice(choice) {
        const result = choice.apply(this.runState);

        if (result === null && this.runState._ambushBattle) {
            delete this.runState._ambushBattle;
            this.scene.start('BattleScene', { runState: this.runState, isAmbush: true });
            return;
        }

        this.children.removeAll(true);
        this.cameras.main.setBackgroundColor('#0a0a1a');

        this.add.text(640, 250, '결과', {
            fontSize: '24px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#ffcc44', stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5);

        this.add.text(640, 310, result || '...', {
            fontSize: '16px', fontFamily: 'monospace',
            color: '#ffffff', wordWrap: { width: 600 }, align: 'center'
        }).setOrigin(0.5);

        this.add.text(640, 360, `🪙 ${this.runState.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44'
        }).setOrigin(0.5);

        PartyHUD.draw(this, this.runState, 500);

        const continueBtn = this.add.rectangle(640, 440, 160, 40, 0x2a3a2a)
            .setStrokeStyle(2, 0x44ff88)
            .setInteractive({ useHandCursor: true });

        this.add.text(640, 440, '계속 진행 ▶', {
            fontSize: '16px', fontFamily: 'monospace', fontStyle: 'bold', color: '#44ff88'
        }).setOrigin(0.5);

        continueBtn.on('pointerdown', () => {
            this.runState.currentNode++;
            this.scene.start('MapScene', { runState: this.runState });
        });
    }
}
