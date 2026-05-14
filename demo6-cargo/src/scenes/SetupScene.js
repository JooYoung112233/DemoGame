class LCSetupScene extends Phaser.Scene {
    constructor() { super('LCSetupScene'); }

    create() {
        this.cameras.main.setBackgroundColor('#1a1510');
        const cx = 640;

        this.add.text(cx, 35, '🚂 LAST CARGO', { fontSize: '38px', fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold' }).setOrigin(0.5);
        this.add.text(cx, 75, '열차를 방어하고 목적지까지 생존하라!', { fontSize: '15px', fontFamily: 'monospace', color: '#aa8866' }).setOrigin(0.5);

        this.add.text(cx, 110, '── 규칙 ──', { fontSize: '14px', fontFamily: 'monospace', color: '#665533' }).setOrigin(0.5);
        const rules = ['• 매 라운드 적이 열차 칸에 침입합니다', '• 1개 칸만 직접 방어 가능, 나머지는 모듈이 자동 방어', '• 칸이 파괴되면 기능을 잃습니다', '• 10라운드 생존 시 승리'];
        rules.forEach((r, i) => {
            this.add.text(cx, 135 + i * 20, r, { fontSize: '12px', fontFamily: 'monospace', color: '#887755' }).setOrigin(0.5);
        });

        this.add.text(cx, 230, '── 모듈 3개를 선택하세요 ──', { fontSize: '16px', fontFamily: 'monospace', color: '#aa7744' }).setOrigin(0.5);

        const modules = getRandomModules(6);
        this.selectedModules = [];
        this.moduleCards = [];

        const cardW = 180, cardH = 120, gap = 15;
        const totalW = 6 * cardW + 5 * gap;
        const startX = cx - totalW / 2 + cardW / 2;

        modules.forEach((mod, i) => {
            const x = startX + i * (cardW + gap);
            const y = 330;

            const card = this.add.rectangle(x, y, cardW, cardH, 0x221a10).setStrokeStyle(2, 0x443322).setInteractive();
            const icon = this.add.text(x, y - 35, mod.icon, { fontSize: '22px' }).setOrigin(0.5);
            const name = this.add.text(x, y - 10, mod.name, { fontSize: '13px', fontFamily: 'monospace', color: mod.color, fontStyle: 'bold' }).setOrigin(0.5);
            const desc = this.add.text(x, y + 15, mod.desc, { fontSize: '10px', fontFamily: 'monospace', color: '#999', wordWrap: { width: cardW - 20 }, align: 'center' }).setOrigin(0.5);
            const check = this.add.text(x, y + 45, '', { fontSize: '16px', fontFamily: 'monospace', color: '#44ff88' }).setOrigin(0.5);

            this.moduleCards.push({ card, check, mod, selected: false });

            card.on('pointerover', () => card.setStrokeStyle(2, Phaser.Display.Color.HexStringToColor(mod.color).color));
            card.on('pointerout', () => {
                const mc = this.moduleCards[i];
                card.setStrokeStyle(2, mc.selected ? 0x44ff88 : 0x443322);
            });
            card.on('pointerdown', () => {
                const mc = this.moduleCards[i];
                if (mc.selected) {
                    mc.selected = false;
                    mc.check.setText('');
                    card.setStrokeStyle(2, 0x443322);
                    this.selectedModules = this.selectedModules.filter(m => m.id !== mod.id);
                } else if (this.selectedModules.length < 3) {
                    mc.selected = true;
                    mc.check.setText('✓');
                    card.setStrokeStyle(2, 0x44ff88);
                    this.selectedModules.push(mod);
                }
                this.updateStartBtn();
            });
        });

        this.add.text(cx, 420, '── 모듈을 칸에 배치합니다 (자동) ──', { fontSize: '12px', fontFamily: 'monospace', color: '#665533' }).setOrigin(0.5);

        // 열차 칸 미리보기
        LC_CARS.forEach((car, i) => {
            const x = 200 + i * 230;
            this.add.rectangle(x, 490, 200, 60, car.color, 0.3).setStrokeStyle(1, car.color);
            this.add.text(x, 475, car.name, { fontSize: '14px', fontFamily: 'monospace', color: '#ffffff' }).setOrigin(0.5);
            this.add.text(x, 498, `HP: ${car.maxHp}`, { fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa' }).setOrigin(0.5);
        });

        this.startBtn = this.add.rectangle(cx, 580, 260, 50, 0x553311).setStrokeStyle(2, 0x664422).setInteractive();
        this.startText = this.add.text(cx, 580, '모듈 3개 선택...', { fontSize: '18px', fontFamily: 'monospace', color: '#886644', fontStyle: 'bold' }).setOrigin(0.5);

        this.startBtn.on('pointerover', () => { if (this.selectedModules.length === 3) this.startBtn.setFillStyle(0x774422); });
        this.startBtn.on('pointerout', () => { if (this.selectedModules.length === 3) this.startBtn.setFillStyle(0x664411); });
        this.startBtn.on('pointerdown', () => {
            if (this.selectedModules.length === 3) {
                this.scene.start('LCTrainScene', { modules: [...this.selectedModules] });
            }
        });

        this.drawGearPanel();

        this.add.text(cx, 700, '← 목록으로', { fontSize: '14px', fontFamily: 'monospace', color: '#665533' })
            .setOrigin(0.5).setInteractive()
            .on('pointerdown', () => { window.location.href = '../index.html'; });
    }

    drawGearPanel() {
        const cx = 640;
        const farmData = LC_FARMING.load();

        this.add.text(cx, 540, '── 장비 제작 ──', {
            fontSize: '14px', fontFamily: 'monospace', color: '#665533'
        }).setOrigin(0.5);

        this.add.text(cx, 558, `🔩${farmData.scrap || 0}  ⚡${farmData.circuit || 0}  💎${farmData.core || 0}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#ccaa66'
        }).setOrigin(0.5);

        const equipped = farmData.equipped || [];
        const gear = LC_FARMING.GEAR;
        const cardW = 180, gap = 10;
        const totalW = gear.length * (cardW + gap) - gap;
        const startX = cx - totalW / 2 + cardW / 2;

        gear.forEach((g, i) => {
            const x = startX + i * (cardW + gap);
            const y = 610;
            const isOwned = equipped.includes(g.id);
            const canCraft = LC_FARMING.canCraft(g.id);

            const bg = isOwned ? 0x223322 : canCraft ? 0x332211 : 0x1a1510;
            const border = isOwned ? 0x44ff88 : canCraft ? 0xff8844 : 0x443322;

            const card = this.add.rectangle(x, y, cardW, 60, bg).setStrokeStyle(1, border);
            this.add.text(x, y - 18, g.name, {
                fontSize: '12px', fontFamily: 'monospace', color: isOwned ? '#44ff88' : '#ccaa88', fontStyle: 'bold'
            }).setOrigin(0.5);
            this.add.text(x, y, g.desc, {
                fontSize: '10px', fontFamily: 'monospace', color: '#888866'
            }).setOrigin(0.5);

            if (isOwned) {
                this.add.text(x, y + 18, '장착 중', {
                    fontSize: '10px', fontFamily: 'monospace', color: '#44ff88'
                }).setOrigin(0.5);
            } else {
                const costStr = Object.entries(g.cost).map(([k, v]) => {
                    const icons = { scrap: '🔩', circuit: '⚡', core: '💎' };
                    return `${icons[k] || k}${v}`;
                }).join(' ');
                this.add.text(x, y + 18, costStr, {
                    fontSize: '10px', fontFamily: 'monospace', color: canCraft ? '#ffaa44' : '#555544'
                }).setOrigin(0.5);

                if (canCraft) {
                    card.setInteractive();
                    card.on('pointerover', () => card.setFillStyle(0x553322));
                    card.on('pointerout', () => card.setFillStyle(0x332211));
                    card.on('pointerdown', () => {
                        LC_FARMING.craft(g.id);
                        this.scene.restart();
                    });
                }
            }
        });
    }

    updateStartBtn() {
        if (this.selectedModules.length === 3) {
            this.startBtn.setFillStyle(0x664411);
            this.startBtn.setStrokeStyle(2, 0xff8844);
            this.startText.setText('출발!');
            this.startText.setColor('#ff8844');
        } else {
            this.startBtn.setFillStyle(0x553311);
            this.startBtn.setStrokeStyle(2, 0x664422);
            this.startText.setText(`모듈 ${this.selectedModules.length}/3 선택...`);
            this.startText.setColor('#886644');
        }
    }
}
