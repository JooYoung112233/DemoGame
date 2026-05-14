class ATBSetupScene extends Phaser.Scene {
    constructor() { super({ key: 'ATBSetupScene' }); }

    create() {
        this.party = ['tank', 'rogue', 'priest', 'mage'];
        this.selectedSlot = -1;

        const g = this.add.graphics();
        for (let i = 0; i < 720; i++) {
            const ratio = i / 720;
            g.fillStyle(Phaser.Display.Color.GetColor(Math.floor(18+ratio*12), Math.floor(18+ratio*18), Math.floor(40+ratio*25)));
            g.fillRect(0, i, 1280, 1);
        }

        this.add.text(640, 40, '⏱ 턴 압축 전투 데모 ⏱', {
            fontSize: '28px', fontFamily: 'monospace', color: '#ffcc44',
            stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5);
        this.add.text(640, 80, '게이지가 차면 행동! 속도 조합의 재미를 검증', {
            fontSize: '14px', fontFamily: 'monospace', color: '#aaaaaa'
        }).setOrigin(0.5);

        this.add.text(640, 130, '[ 아군 파티 ]', {
            fontSize: '18px', fontFamily: 'monospace', color: '#44ff88', stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5);

        this.slotContainers = [];
        const chars = ['tank', 'rogue', 'priest', 'mage'];
        const startX = 340, spacing = 160;

        for (let i = 0; i < 4; i++) {
            const x = startX + i * spacing, y = 220;
            const data = ATB_CHARS[this.party[i]];
            const bg = this.add.rectangle(x, y, 120, 140, 0x333355, 0.8)
                .setStrokeStyle(2, 0x5566aa).setInteractive({ useHandCursor: true });
            const colorBox = this.add.rectangle(x, y - 25, 32, 32, data.color);
            const nameText = this.add.text(x, y + 10, data.name, { fontSize: '14px', fontFamily: 'monospace', color: '#ffffff' }).setOrigin(0.5);
            const spdText = this.add.text(x, y + 30, 'SPD: ' + data.speed, { fontSize: '11px', fontFamily: 'monospace', color: '#ffcc44' }).setOrigin(0.5);
            const statsText = this.add.text(x, y + 48, 'HP:' + data.hp + ' ATK:' + data.atk, { fontSize: '10px', fontFamily: 'monospace', color: '#888' }).setOrigin(0.5);
            this.slotContainers.push({ bg, colorBox, nameText, spdText, statsText });

            bg.on('pointerdown', () => { this.selectedSlot = i; this.highlightSlot(i); });
            bg.on('pointerover', () => bg.setFillStyle(0x444466));
            bg.on('pointerout', () => bg.setFillStyle(0x333355));
        }

        this.add.text(640, 330, '[ 캐릭터 선택 ]', { fontSize: '13px', fontFamily: 'monospace', color: '#cccccc' }).setOrigin(0.5);

        chars.forEach((key, i) => {
            const data = ATB_CHARS[key];
            const x = startX + i * spacing, y = 400;
            const btn = this.add.rectangle(x, y, 120, 80, 0x222244, 0.8)
                .setStrokeStyle(2, data.color).setInteractive({ useHandCursor: true });
            this.add.rectangle(x, y - 10, 24, 24, data.color);
            this.add.text(x, y + 15, data.name, { fontSize: '13px', fontFamily: 'monospace', color: '#fff' }).setOrigin(0.5);
            this.add.text(x, y + 32, 'SPD:' + data.speed + ' ATK:' + data.atk, { fontSize: '9px', fontFamily: 'monospace', color: '#888' }).setOrigin(0.5);

            btn.on('pointerdown', () => {
                if (this.selectedSlot >= 0) {
                    this.party[this.selectedSlot] = key;
                    this.updateSlot(this.selectedSlot, key);
                }
            });
            btn.on('pointerover', () => btn.setFillStyle(0x333355));
            btn.on('pointerout', () => btn.setFillStyle(0x222244));
        });

        this.add.text(640, 500, '[ 적 구성 ]', { fontSize: '16px', fontFamily: 'monospace', color: '#ff6644', stroke: '#000000', strokeThickness: 2 }).setOrigin(0.5);
        ATB_DEFAULT_ENEMIES.forEach((key, i) => {
            const data = ATB_CHARS[key];
            const x = 240 + i * 165, y = 560;
            this.add.rectangle(x, y, 130, 60, 0x332222, 0.6).setStrokeStyle(1, 0x664444);
            this.add.rectangle(x - 40, y, 16, 16, data.color);
            this.add.text(x + 5, y - 8, data.name, { fontSize: '11px', fontFamily: 'monospace', color: '#ccaaaa' }).setOrigin(0, 0.5);
            this.add.text(x + 5, y + 10, 'SPD:' + data.speed, { fontSize: '10px', fontFamily: 'monospace', color: '#886666' }).setOrigin(0, 0.5);
        });

        const btn = this.add.rectangle(640, 660, 240, 50, 0xaa8800).setStrokeStyle(2, 0xccaa22).setInteractive({ useHandCursor: true });
        this.add.text(640, 660, '▶ 전투 시작', { fontSize: '20px', fontFamily: 'monospace', color: '#fff', fontStyle: 'bold' }).setOrigin(0.5);
        btn.on('pointerover', () => btn.setFillStyle(0xbb9911));
        btn.on('pointerout', () => btn.setFillStyle(0xaa8800));
        btn.on('pointerdown', () => {
            this.scene.start('ATBBattleScene', { party: [...this.party], enemies: ATB_DEFAULT_ENEMIES, speed: 1.5 });
        });
    }

    updateSlot(index, charKey) {
        const data = ATB_CHARS[charKey];
        const slot = this.slotContainers[index];
        slot.colorBox.setFillStyle(data.color);
        slot.nameText.setText(data.name);
        slot.spdText.setText('SPD: ' + data.speed);
        slot.statsText.setText('HP:' + data.hp + ' ATK:' + data.atk);
    }

    highlightSlot(index) {
        this.slotContainers.forEach((s, i) => {
            s.bg.setStrokeStyle(i === index ? 3 : 2, i === index ? 0xffcc00 : 0x5566aa);
        });
    }
}
