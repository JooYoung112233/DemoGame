class VillageScene extends Phaser.Scene {
    constructor() {
        super({ key: 'VillageScene' });
    }

    init(data) {
        this.runState = data.runState;
        this.selectedUnit = null;
        this.msgText = null;
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');
        const node = NODE_CONFIG[this.runState.currentNode];

        this.add.text(640, 30, node.name, {
            fontSize: '28px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#4488ff', stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5);

        this.add.text(640, 60, node.desc || '', {
            fontSize: '12px', fontFamily: 'monospace', color: '#666688'
        }).setOrigin(0.5);

        this.goldText = this.add.text(30, 690, `🪙 ${this.runState.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace',
            color: '#ffcc44', stroke: '#000000', strokeThickness: 2
        }).setDepth(20);

        this.drawPartyPanel();
        this.drawRecruitPanel();
        this.drawDepartButton();

        this.msgText = this.add.text(640, 680, '', {
            fontSize: '13px', fontFamily: 'monospace',
            color: '#ffcc44', stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5).setDepth(30);
    }

    showMessage(msg) {
        this.msgText.setText(msg);
        this.tweens.killTweensOf(this.msgText);
        this.msgText.setAlpha(1);
        this.tweens.add({
            targets: this.msgText, alpha: 0,
            delay: 2000, duration: 500
        });
    }

    drawPartyPanel() {
        if (this.partyGroup) this.partyGroup.forEach(o => o.destroy());
        this.partyGroup = [];

        this.add.text(130, 95, '[ 파티 ]', {
            fontSize: '16px', fontFamily: 'monospace', fontStyle: 'bold', color: '#44ff88'
        }).setOrigin(0.5);

        const party = this.runState.party;
        party.forEach((unit, i) => {
            const y = 130 + i * 85;
            const stats = unit.getStats();
            const hpRatio = unit.currentHp / stats.hp;
            const isSel = this.selectedUnit === unit;

            const bg = this.add.rectangle(130, y + 25, 230, 75, isSel ? 0x223344 : 0x151530)
                .setStrokeStyle(1, isSel ? 0x44aaff : 0x333355)
                .setInteractive({ useHandCursor: true });
            this.partyGroup.push(bg);

            bg.on('pointerdown', () => {
                this.selectedUnit = (this.selectedUnit === unit) ? null : unit;
                this.refresh();
            });

            const classData = UNIT_DATA[unit.classKey];
            const colorHex = '#' + classData.color.toString(16).padStart(6, '0');

            const nameT = this.add.text(30, y, `${stats.name} [T${unit.tier}]`, {
                fontSize: '13px', fontFamily: 'monospace', fontStyle: 'bold', color: colorHex
            });
            this.partyGroup.push(nameT);

            const statT = this.add.text(30, y + 16, `HP:${unit.currentHp}/${stats.hp} ATK:${stats.atk} DEF:${stats.def}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#888888'
            });
            this.partyGroup.push(statT);

            const hpBg = this.add.rectangle(30, y + 33, 200, 5, 0x333333).setOrigin(0, 0.5);
            let hpColor = 0x44ff44;
            if (hpRatio <= 0.3) hpColor = 0xff4444;
            else if (hpRatio <= 0.6) hpColor = 0xffaa00;
            const hpBar = this.add.rectangle(30, y + 33, 200 * hpRatio, 5, hpColor).setOrigin(0, 0.5);
            this.partyGroup.push(hpBg, hpBar);

            if (unit.traits.length > 0) {
                const traitStr = unit.traits.map(t => {
                    const isGood = GOOD_TRAITS.find(g => g.id === t.id);
                    return (isGood ? '✦' : '✧') + t.name;
                }).join(' ');
                const traitT = this.add.text(30, y + 42, traitStr, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#aa88cc'
                });
                this.partyGroup.push(traitT);
            }

            if (unit.canAdvance() && isSel) {
                const options = unit.getAdvanceOptions();
                options.forEach((opt, j) => {
                    const advY = y + 56;
                    const advX = 30 + j * 115;
                    const cost = opt.data.advanceCost;
                    const canAfford = this.runState.gold >= cost;

                    const advBtn = this.add.rectangle(advX + 50, advY + 4, 105, 18,
                        canAfford ? 0x2a2a4a : 0x1a1a2a)
                        .setStrokeStyle(1, canAfford ? 0x44aaff : 0x333344)
                        .setInteractive({ useHandCursor: canAfford });
                    this.partyGroup.push(advBtn);

                    const advText = this.add.text(advX + 50, advY + 4,
                        `→ ${opt.data.name} (${cost}G)`, {
                            fontSize: '9px', fontFamily: 'monospace',
                            color: canAfford ? '#44aaff' : '#555555'
                        }).setOrigin(0.5);
                    this.partyGroup.push(advText);

                    if (canAfford) {
                        advBtn.on('pointerdown', () => {
                            const result = PartyManager.advance(this.runState, unit, opt.key);
                            this.showMessage(result.msg);
                            this.selectedUnit = null;
                            this.refresh();
                        });
                    }
                });
            }
        });
    }

    drawRecruitPanel() {
        if (this.recruitGroup) this.recruitGroup.forEach(o => o.destroy());
        this.recruitGroup = [];

        this.add.text(890, 95, '[ 징집 ]', {
            fontSize: '16px', fontFamily: 'monospace', fontStyle: 'bold', color: '#ffaa44'
        }).setOrigin(0.5);

        BASE_CLASSES.forEach((key, i) => {
            const data = UNIT_DATA[key];
            const y = 130 + i * 130;
            const x = 660;

            const bg = this.add.rectangle(890, y + 40, 440, 110, 0x151530)
                .setStrokeStyle(1, 0x333355);
            this.recruitGroup.push(bg);

            const colorHex = '#' + data.color.toString(16).padStart(6, '0');
            const nameT = this.add.text(x + 15, y, data.name, {
                fontSize: '16px', fontFamily: 'monospace', fontStyle: 'bold', color: colorHex
            });
            this.recruitGroup.push(nameT);

            const statLines = [
                `HP: ${data.hp}  ATK: ${data.atk}  DEF: ${data.def}`,
                `공속: ${data.attackSpeed}ms  사거리: ${data.range}  이속: ${data.moveSpeed}`,
                `크리: ${Math.floor(data.critRate * 100)}%  크뎀: ${data.critDmg}x`
            ];
            statLines.forEach((line, li) => {
                const st = this.add.text(x + 15, y + 22 + li * 14, line, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#777788'
                });
                this.recruitGroup.push(st);
            });

            const advTree = (data.advanceTo || []).map(k => UNIT_DATA[k].name).join(' / ');
            if (advTree) {
                const treeT = this.add.text(x + 15, y + 66, `전직: ${advTree}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#666699'
                });
                this.recruitGroup.push(treeT);
            }

            const canRecruit = this.runState.gold >= data.cost && this.runState.party.length < this.runState.maxPartySize;
            const btn = this.add.rectangle(1060, y + 35, 80, 30,
                canRecruit ? 0x3a3a1a : 0x1a1a1a)
                .setStrokeStyle(1, canRecruit ? 0xffaa44 : 0x333333)
                .setInteractive({ useHandCursor: canRecruit });
            this.recruitGroup.push(btn);

            const btnText = this.add.text(1060, y + 35, `고용 ${data.cost}G`, {
                fontSize: '11px', fontFamily: 'monospace', fontStyle: 'bold',
                color: canRecruit ? '#ffaa44' : '#555555'
            }).setOrigin(0.5);
            this.recruitGroup.push(btnText);

            if (canRecruit) {
                btn.on('pointerdown', () => {
                    const result = PartyManager.recruit(this.runState, key);
                    this.showMessage(result.msg);
                    this.refresh();
                });
                btn.on('pointerover', () => btn.setFillStyle(0x5a5a2a));
                btn.on('pointerout', () => btn.setFillStyle(0x3a3a1a));
            }
        });
    }

    drawDepartButton() {
        const btn = this.add.rectangle(640, 700, 160, 40, 0x2a3a2a)
            .setStrokeStyle(2, 0x44ff88)
            .setInteractive({ useHandCursor: true })
            .setDepth(25);

        const text = this.add.text(640, 700, '▶ 출발', {
            fontSize: '18px', fontFamily: 'monospace', fontStyle: 'bold', color: '#44ff88'
        }).setOrigin(0.5).setDepth(26);

        btn.on('pointerover', () => { btn.setFillStyle(0x3a5a3a); text.setColor('#66ffaa'); });
        btn.on('pointerout', () => { btn.setFillStyle(0x2a3a2a); text.setColor('#44ff88'); });
        btn.on('pointerdown', () => {
            this.runState.currentNode++;
            this.scene.start('MapScene', { runState: this.runState });
        });
    }

    refresh() {
        this.goldText.setText(`🪙 ${this.runState.gold}G`);
        this.drawPartyPanel();
        this.drawRecruitPanel();
    }
}
