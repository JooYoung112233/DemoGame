class TreasureScene extends Phaser.Scene {
    constructor() {
        super('TreasureScene');
    }

    init(data) {
        this.act = data.act;
        this.map = data.map;
        this.playerState = data.playerState;
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a1a');

        this.add.text(W / 2, 60, '💎 보물 발견!', {
            fontSize: '32px', fontFamily: 'monospace', color: '#44ccff', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(W / 2, 100, '유물 하나를 선택하세요', {
            fontSize: '16px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);

        const ownedIds = this.playerState.relics || [];
        const choices = getRelicChoices(3, ownedIds);

        const startX = W / 2 - (choices.length - 1) * 200 / 2;

        for (let i = 0; i < choices.length; i++) {
            const relic = choices[i];
            const x = startX + i * 200;
            const y = 300;

            const rarityColor = relic.rarity === 'rare' ? 0xffaa00 :
                                relic.rarity === 'uncommon' ? 0x44aaff : 0x888888;

            const bg = this.add.graphics();
            bg.fillStyle(0x1a1a35, 1);
            bg.lineStyle(3, rarityColor, 0.8);
            bg.fillRoundedRect(x - 80, y - 90, 160, 220, 12);
            bg.strokeRoundedRect(x - 80, y - 90, 160, 220, 12);

            this.add.text(x, y - 55, relic.icon, { fontSize: '42px' }).setOrigin(0.5);

            this.add.text(x, y, relic.name, {
                fontSize: '16px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            }).setOrigin(0.5);

            const rarityLabel = relic.rarity === 'rare' ? '레어' :
                                relic.rarity === 'uncommon' ? '언커먼' : '커먼';
            this.add.text(x, y + 22, rarityLabel, {
                fontSize: '11px', fontFamily: 'monospace',
                color: '#' + rarityColor.toString(16).padStart(6, '0')
            }).setOrigin(0.5);

            this.add.text(x, y + 50, relic.desc, {
                fontSize: '12px', fontFamily: 'monospace', color: '#aaaaaa',
                wordWrap: { width: 140 }, align: 'center'
            }).setOrigin(0.5);

            // Show current stack count if already owned
            const ownCount = ownedIds.filter(id => id === relic.id).length;
            if (ownCount > 0) {
                this.add.text(x + 60, y - 80, `보유 ×${ownCount}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#44ff88'
                }).setOrigin(0.5);
            }

            const hitArea = this.add.rectangle(x, y + 15, 160, 220, 0x000000, 0)
                .setInteractive({ useHandCursor: true });

            hitArea.on('pointerover', () => {
                bg.clear().fillStyle(0x2a2a50, 1).lineStyle(3, 0xffffff, 1)
                    .fillRoundedRect(x - 80, y - 90, 160, 220, 12)
                    .strokeRoundedRect(x - 80, y - 90, 160, 220, 12);
            });
            hitArea.on('pointerout', () => {
                bg.clear().fillStyle(0x1a1a35, 1).lineStyle(3, rarityColor, 0.8)
                    .fillRoundedRect(x - 80, y - 90, 160, 220, 12)
                    .strokeRoundedRect(x - 80, y - 90, 160, 220, 12);
            });
            hitArea.on('pointerdown', () => this._pickRelic(relic));
        }

        // skip option
        const skipBtn = this.add.text(W / 2, 550, '[ 스킵 ]', {
            fontSize: '16px', fontFamily: 'monospace', color: '#666666',
            padding: { x: 16, y: 8 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });
        skipBtn.on('pointerover', () => skipBtn.setColor('#aaaaaa'));
        skipBtn.on('pointerout', () => skipBtn.setColor('#666666'));
        skipBtn.on('pointerdown', () => this._goBack());

        this._drawPlayerInfo();
    }

    _pickRelic(relic) {
        this.playerState.relics.push(relic.id);

        // show pickup animation
        const W = 1280;
        const pickupText = this.add.text(W / 2, 480, `${relic.icon} ${relic.name} 획득!`, {
            fontSize: '24px', fontFamily: 'monospace', color: '#44ccff', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setScale(0.5);

        this.tweens.add({
            targets: pickupText, scaleX: 1.2, scaleY: 1.2,
            duration: 200, ease: 'Back.easeOut',
            onComplete: () => {
                this.time.delayedCall(400, () => this._goBack());
            }
        });
    }

    _goBack() {
        this.scene.start('MapScene', {
            act: this.act,
            map: this.map,
            playerState: this.playerState
        });
    }

    _drawPlayerInfo() {
        const W = 1280;
        this.add.graphics().fillStyle(0x111128, 1).fillRect(0, 660, W, 60);
        this.add.text(20, 680, `❤️ ${this.playerState.hp}/${this.playerState.maxHp}  🪙 ${this.playerState.gold}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0, 0.5);
        if (this.playerState.relics.length > 0) {
            const counts = {};
            for (const r of this.playerState.relics) counts[r] = (counts[r] || 0) + 1;
            const relicStr = Object.entries(counts).map(([id, cnt]) => {
                const rd = RELIC_DATA[id];
                return rd ? (cnt > 1 ? `${rd.icon}×${cnt}` : rd.icon) : '?';
            }).join(' ');
            this.add.text(W - 20, 680, relicStr, { fontSize: '16px' }).setOrigin(1, 0.5);
        }
    }
}
