class BOResultScene extends Phaser.Scene {
    constructor() { super('BOResultScene'); }

    init(data) {
        this.party = data.party;
        this.weightSystem = data.weightSystem;
        this.tensionSystem = data.tensionSystem;
        this.mapSystem = data.mapSystem;
        this.escaped = data.escaped;
        this.died = data.died;
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');
        const cx = 640;

        let title, titleColor;
        if (this.escaped) {
            title = '🔦 탈출 성공!';
            titleColor = '#44ff88';
        } else {
            title = '💀 사망...';
            titleColor = '#ff4444';
        }

        this.add.text(cx, 60, title, {
            fontSize: '32px', fontFamily: 'monospace', color: titleColor, fontStyle: 'bold'
        }).setOrigin(0.5);

        const inv = this.weightSystem.inventory;
        let finalItems = [...inv];
        let lostItems = [];

        if (this.died) {
            const specialItems = finalItems.filter(it => it.special);
            lostItems = finalItems.filter(it => !it.special);
            finalItems = specialItems;
        }

        const totalValue = finalItems.reduce((s, it) => s + it.value, 0);
        const lostValue = lostItems.reduce((s, it) => s + it.value, 0);

        this.add.text(cx, 110, `탐색 방: ${this.mapSystem.roomsVisited}  |  최종 긴장도: ${this.tensionSystem.tension}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#8899aa'
        }).setOrigin(0.5);

        let yPos = 160;

        if (finalItems.length > 0) {
            this.add.text(cx, yPos, '── 획득 아이템 ──', {
                fontSize: '14px', fontFamily: 'monospace', color: '#446655'
            }).setOrigin(0.5);
            yPos += 25;

            const cols = 5;
            const cardW = 180, cardH = 45, gap = 10;

            finalItems.forEach((item, i) => {
                const col = i % cols;
                const row = Math.floor(i / cols);
                const totalRowW = Math.min(finalItems.length - row * cols, cols) * (cardW + gap) - gap;
                const x = cx - totalRowW / 2 + cardW / 2 + col * (cardW + gap);
                const y = yPos + row * (cardH + gap);

                const rarityInfo = BO_RARITIES[item.rarity] || BO_RARITIES.common;
                this.add.rectangle(x, y, cardW, cardH, 0x111122).setStrokeStyle(1, rarityInfo.borderColor);
                this.add.text(x, y - 8, `${item.icon} ${item.name}`, {
                    fontSize: '11px', fontFamily: 'monospace', color: rarityInfo.color
                }).setOrigin(0.5);
                this.add.text(x, y + 10, `가치: ${item.value}`, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#667788'
                }).setOrigin(0.5);
            });

            yPos += Math.ceil(finalItems.length / cols) * (cardH + gap) + 15;
        }

        if (lostItems.length > 0) {
            this.add.text(cx, yPos, `── 손실 아이템 (${lostItems.length}개, 가치: ${lostValue}) ──`, {
                fontSize: '13px', fontFamily: 'monospace', color: '#664444'
            }).setOrigin(0.5);
            yPos += 22;

            const names = lostItems.map(it => it.name).join(', ');
            this.add.text(cx, yPos, names, {
                fontSize: '11px', fontFamily: 'monospace', color: '#885555',
                wordWrap: { width: 800 }, align: 'center'
            }).setOrigin(0.5);
            yPos += 30;
        }

        yPos += 10;
        this.add.text(cx, yPos, `총 획득 가치: ${totalValue}`, {
            fontSize: '22px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5);
        yPos += 35;

        let grade, gradeColor;
        if (totalValue >= 500) { grade = 'S'; gradeColor = '#ffff44'; }
        else if (totalValue >= 350) { grade = 'A'; gradeColor = '#44ff88'; }
        else if (totalValue >= 200) { grade = 'B'; gradeColor = '#88aaff'; }
        else if (totalValue >= 100) { grade = 'C'; gradeColor = '#ffaa44'; }
        else { grade = 'D'; gradeColor = '#ff4444'; }

        this.add.text(cx, yPos, `등급: ${grade}`, {
            fontSize: '36px', fontFamily: 'monospace', color: gradeColor, fontStyle: 'bold'
        }).setOrigin(0.5);

        const retryBtn = this.add.rectangle(cx, 640, 200, 44, 0x442288)
            .setStrokeStyle(2, 0x8844ff).setInteractive();
        this.add.text(cx, 640, '다시 도전', {
            fontSize: '16px', fontFamily: 'monospace', color: '#bb88ff', fontStyle: 'bold'
        }).setOrigin(0.5);

        retryBtn.on('pointerover', () => retryBtn.setFillStyle(0x553399));
        retryBtn.on('pointerout', () => retryBtn.setFillStyle(0x442288));
        retryBtn.on('pointerdown', () => this.scene.start('BOSetupScene'));

        this.add.text(cx, 690, '← 목록으로', {
            fontSize: '13px', fontFamily: 'monospace', color: '#334455'
        }).setOrigin(0.5).setInteractive()
            .on('pointerdown', () => { window.location.href = '../index.html'; });
    }
}
