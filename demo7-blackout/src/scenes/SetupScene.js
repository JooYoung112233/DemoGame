class BOSetupScene extends Phaser.Scene {
    constructor() { super('BOSetupScene'); }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');
        const cx = 640;

        this.add.text(cx, 35, '🔦 BLACK OUT', { fontSize: '38px', fontFamily: 'monospace', color: '#8844ff', fontStyle: 'bold' }).setOrigin(0.5);
        this.add.text(cx, 75, '버려진 시설에서 아이템을 수집하고 탈출하라!', { fontSize: '15px', fontFamily: 'monospace', color: '#6688aa' }).setOrigin(0.5);

        this.add.text(cx, 110, '── 규칙 ──', { fontSize: '14px', fontFamily: 'monospace', color: '#334455' }).setOrigin(0.5);
        const rules = [
            '• 5×5 맵에서 인접한 방으로 이동합니다',
            '• 방에서 적/아이템/함정이 확률로 등장합니다',
            '• 아이템을 주울수록 무게↑ → 도주/탈출 확률↓',
            '• 출구에 도달해서 탈출에 성공하면 승리!'
        ];
        rules.forEach((r, i) => {
            this.add.text(cx, 135 + i * 20, r, { fontSize: '12px', fontFamily: 'monospace', color: '#556677' }).setOrigin(0.5);
        });

        this.add.text(cx, 235, '── 파티 선택 (클릭으로 변경) ──', { fontSize: '16px', fontFamily: 'monospace', color: '#7744aa' }).setOrigin(0.5);

        const allKeys = Object.keys(BO_ALLIES);
        this.partyKeys = ['scout', 'breacher', 'hacker', 'medic'];
        this.slotTexts = [];

        const slotW = 200, gap = 20;
        const totalW = 4 * slotW + 3 * gap;
        const startX = cx - totalW / 2 + slotW / 2;

        this.partyKeys.forEach((key, i) => {
            const x = startX + i * (slotW + gap);
            const y = 340;
            const data = BO_ALLIES[key];

            const card = this.add.rectangle(x, y, slotW, 140, 0x111122).setStrokeStyle(2, 0x334466).setInteractive();

            const nameText = this.add.text(x, y - 45, data.name, {
                fontSize: '18px', fontFamily: 'monospace', color: Phaser.Display.Color.IntegerToColor(data.color).rgba, fontStyle: 'bold'
            }).setOrigin(0.5);

            const roleText = this.add.text(x, y - 22, `역할: ${data.role}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#8899aa'
            }).setOrigin(0.5);

            const statsText = this.add.text(x, y + 2, `HP:${data.hp} ATK:${data.atk} DEF:${data.def}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#aaaacc'
            }).setOrigin(0.5);

            const descText = this.add.text(x, y + 22, data.desc, {
                fontSize: '10px', fontFamily: 'monospace', color: '#6677aa'
            }).setOrigin(0.5);

            const slotLabel = this.add.text(x, y + 48, `슬롯 ${i + 1}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#445566'
            }).setOrigin(0.5);

            this.slotTexts.push({ nameText, roleText, statsText, descText, card });

            card.on('pointerover', () => card.setStrokeStyle(2, 0x8844ff));
            card.on('pointerout', () => card.setStrokeStyle(2, 0x334466));
            card.on('pointerdown', () => {
                const curIdx = allKeys.indexOf(this.partyKeys[i]);
                const nextIdx = (curIdx + 1) % allKeys.length;
                this.partyKeys[i] = allKeys[nextIdx];
                this.updateSlot(i);
            });
        });

        const startBtn = this.add.rectangle(cx, 500, 260, 50, 0x442288).setStrokeStyle(2, 0x8844ff).setInteractive();
        this.add.text(cx, 500, '시설 진입', {
            fontSize: '20px', fontFamily: 'monospace', color: '#bb88ff', fontStyle: 'bold'
        }).setOrigin(0.5);

        startBtn.on('pointerover', () => startBtn.setFillStyle(0x553399));
        startBtn.on('pointerout', () => startBtn.setFillStyle(0x442288));
        startBtn.on('pointerdown', () => {
            this.scene.start('BOMapScene', { party: [...this.partyKeys] });
        });

        this.add.text(cx, 660, '← 목록으로', { fontSize: '14px', fontFamily: 'monospace', color: '#334455' })
            .setOrigin(0.5).setInteractive()
            .on('pointerdown', () => { window.location.href = '../index.html'; });
    }

    updateSlot(i) {
        const data = BO_ALLIES[this.partyKeys[i]];
        const slot = this.slotTexts[i];
        slot.nameText.setText(data.name);
        slot.nameText.setColor(Phaser.Display.Color.IntegerToColor(data.color).rgba);
        slot.roleText.setText(`역할: ${data.role}`);
        slot.statsText.setText(`HP:${data.hp} ATK:${data.atk} DEF:${data.def}`);
        slot.descText.setText(data.desc);
    }
}
