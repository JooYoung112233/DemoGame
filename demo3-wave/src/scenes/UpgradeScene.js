class UpgradeScene extends Phaser.Scene {
    constructor() { super('UpgradeScene'); }

    init(data) {
        this.partyKeys = data.party;
        this.waveManager = data.waveManager;
        this.appliedUpgrades = data.appliedUpgrades || [];
        this.prevAllies = data.allies || [];
    }

    create() {
        this.cameras.main.setBackgroundColor('#1a1a2e');
        const cx = 640;

        const clearedWave = this.waveManager.currentWave;
        this.add.text(cx, 40, `웨이브 ${clearedWave} 클리어!`, {
            fontSize: '28px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(cx, 80, '강화를 선택하세요', {
            fontSize: '18px', fontFamily: 'monospace', color: '#aaaacc'
        }).setOrigin(0.5);

        const survivorInfo = this.prevAllies.filter(a => a.alive).length;
        this.add.text(cx, 115, `생존자: ${survivorInfo} / ${this.prevAllies.length}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#88aacc'
        }).setOrigin(0.5);

        const upgrades = getRandomUpgrades(3);
        const cardWidth = 300, cardHeight = 280, gap = 40;
        const totalWidth = 3 * cardWidth + 2 * gap;
        const startX = cx - totalWidth / 2 + cardWidth / 2;

        upgrades.forEach((upg, i) => {
            const x = startX + i * (cardWidth + gap);
            const y = 320;

            const card = this.add.rectangle(x, y, cardWidth, cardHeight, 0x222244)
                .setStrokeStyle(3, Phaser.Display.Color.HexStringToColor(upg.color).color)
                .setInteractive();

            this.add.text(x, y - 90, upg.name, {
                fontSize: '22px', fontFamily: 'monospace', color: upg.color, fontStyle: 'bold'
            }).setOrigin(0.5);

            this.add.text(x, y - 40, upg.desc, {
                fontSize: '14px', fontFamily: 'monospace', color: '#cccccc',
                wordWrap: { width: cardWidth - 40 }, align: 'center'
            }).setOrigin(0.5);

            const alreadyCount = this.appliedUpgrades.filter(id => id === upg.id).length;
            if (alreadyCount > 0) {
                this.add.text(x, y + 20, `(중첩 ${alreadyCount + 1}회)`, {
                    fontSize: '13px', fontFamily: 'monospace', color: '#ffcc44'
                }).setOrigin(0.5);
            }

            const selectBtn = this.add.rectangle(x, y + 80, 160, 40, Phaser.Display.Color.HexStringToColor(upg.color).color)
                .setInteractive();
            this.add.text(x, y + 80, '선택', {
                fontSize: '18px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            }).setOrigin(0.5);

            card.on('pointerover', () => card.setFillStyle(0x333366));
            card.on('pointerout', () => card.setFillStyle(0x222244));

            const selectUpgrade = () => {
                this.appliedUpgrades.push(upg.id);
                this.scene.start('WaveBattleScene', {
                    party: this.partyKeys,
                    waveManager: this.waveManager,
                    appliedUpgrades: this.appliedUpgrades
                });
            };
            card.on('pointerdown', selectUpgrade);
            selectBtn.on('pointerdown', selectUpgrade);
        });

        this.add.text(cx, 550, '── 적용된 강화 ──', {
            fontSize: '14px', fontFamily: 'monospace', color: '#6666aa'
        }).setOrigin(0.5);

        if (this.appliedUpgrades.length === 0) {
            this.add.text(cx, 580, '없음', {
                fontSize: '13px', fontFamily: 'monospace', color: '#555577'
            }).setOrigin(0.5);
        } else {
            const counts = {};
            this.appliedUpgrades.forEach(id => { counts[id] = (counts[id] || 0) + 1; });
            const labels = Object.entries(counts).map(([id, cnt]) => {
                const u = UPGRADES.find(u => u.id === id);
                return cnt > 1 ? `${u.name} ×${cnt}` : u.name;
            });
            this.add.text(cx, 585, labels.join('  |  '), {
                fontSize: '12px', fontFamily: 'monospace', color: '#8888aa',
                wordWrap: { width: 1000 }, align: 'center'
            }).setOrigin(0.5);
        }

        this.add.text(cx, 680, `다음: 웨이브 ${clearedWave + 1} / ${this.waveManager.maxWave}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);
    }
}
