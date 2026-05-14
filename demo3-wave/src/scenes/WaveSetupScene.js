class WaveSetupScene extends Phaser.Scene {
    constructor() { super('WaveSetupScene'); }

    create() {
        this.cameras.main.setBackgroundColor('#1a1a2e');
        const cx = 640, cy = 360;

        this.add.text(cx, 50, '웨이브 서바이벌', { fontSize: '36px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold' }).setOrigin(0.5);
        this.add.text(cx, 95, '파티를 편성하고 10웨이브를 생존하라!', { fontSize: '16px', fontFamily: 'monospace', color: '#aaaacc' }).setOrigin(0.5);

        const keys = Object.keys(WAVE_CHARS);
        this.party = ['tank', 'rogue', 'priest', 'mage'];
        this.slotTexts = [];

        this.add.text(cx, 150, '── 파티 편성 (클릭하여 변경) ──', { fontSize: '16px', fontFamily: 'monospace', color: '#8888aa' }).setOrigin(0.5);

        for (let i = 0; i < 4; i++) {
            const sx = 260 + i * 190;
            const sy = 260;
            const slot = this.add.rectangle(sx, sy, 150, 160, 0x222244).setStrokeStyle(2, 0x4444aa).setInteractive();
            const txt = this.add.text(sx, sy, '', { fontSize: '14px', fontFamily: 'monospace', color: '#ffffff', align: 'center' }).setOrigin(0.5);
            this.slotTexts.push(txt);

            slot.on('pointerover', () => slot.setStrokeStyle(2, 0x8888ff));
            slot.on('pointerout', () => slot.setStrokeStyle(2, 0x4444aa));
            slot.on('pointerdown', () => {
                const curIdx = keys.indexOf(this.party[i]);
                this.party[i] = keys[(curIdx + 1) % keys.length];
                this.updateSlots();
            });
        }
        this.updateSlots();

        const startBtn = this.add.rectangle(cx, 520, 240, 55, 0x44aa44).setStrokeStyle(2, 0x66cc66).setInteractive();
        this.add.text(cx, 520, '전투 시작!', { fontSize: '22px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold' }).setOrigin(0.5);
        startBtn.on('pointerover', () => startBtn.setFillStyle(0x55bb55));
        startBtn.on('pointerout', () => startBtn.setFillStyle(0x44aa44));
        startBtn.on('pointerdown', () => {
            this.scene.start('WaveBattleScene', { party: [...this.party] });
        });

        this.add.text(cx, 620, '← 메인으로', { fontSize: '14px', fontFamily: 'monospace', color: '#6666aa' })
            .setOrigin(0.5).setInteractive()
            .on('pointerdown', () => { window.location.href = '../index.html'; });
    }

    updateSlots() {
        for (let i = 0; i < 4; i++) {
            const d = WAVE_CHARS[this.party[i]];
            const roleLabel = { tank: '🛡️탱커', dps: '⚔️딜러', healer: '💚힐러' }[d.role] || d.role;
            this.slotTexts[i].setText(
                `${d.name}\n${roleLabel}\n\nHP ${d.hp}\nATK ${d.atk}\nDEF ${d.def}\nSPD ${d.moveSpeed}\nRNG ${d.range}`
            );
        }
    }
}
