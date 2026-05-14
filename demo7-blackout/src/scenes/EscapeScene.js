class BOEscapeScene extends Phaser.Scene {
    constructor() { super('BOEscapeScene'); }

    init(data) {
        this.party = data.party;
        this.mapSystem = data.mapSystem;
        this.weightSystem = data.weightSystem;
        this.tensionSystem = data.tensionSystem;
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');
        const cx = 640;

        this.add.text(cx, 60, '🔓 출구 도달!', {
            fontSize: '32px', fontFamily: 'monospace', color: '#44ff44', fontStyle: 'bold'
        }).setOrigin(0.5);

        const escapeChance = this.weightSystem.getEscapeChance(this.tensionSystem.tension);
        const weight = this.weightSystem.getCurrentWeight();
        const maxW = this.weightSystem.maxCapacity;
        const totalValue = this.weightSystem.getTotalValue();

        this.add.text(cx, 110, `탈출 성공 확률: ${Math.floor(escapeChance * 100)}%`, {
            fontSize: '22px', fontFamily: 'monospace', color: escapeChance > 0.6 ? '#44ff88' : escapeChance > 0.3 ? '#ffaa44' : '#ff4444', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(cx, 145, `무게: ${weight}/${maxW}  |  긴장도: ${this.tensionSystem.tension}  |  총 가치: ${totalValue}`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#8899aa'
        }).setOrigin(0.5);

        const hasC4 = this.weightSystem.inventory.some(it => it.id === 'c4');
        const hasEMP = this.weightSystem.inventory.some(it => it.id === 'emp');
        const canBlast = hasC4 || hasEMP;

        const stealthBtn = this.add.rectangle(cx - 200, 230, 170, 80, 0x224422)
            .setStrokeStyle(2, 0x44ff44).setInteractive();
        this.add.text(cx - 200, 210, '🤫 은밀 탈출', {
            fontSize: '15px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.add.text(cx - 200, 232, `성공률: ${Math.floor(escapeChance * 100)}%`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#88aa88'
        }).setOrigin(0.5);
        this.add.text(cx - 200, 250, '무게에 따라 확률 변동', {
            fontSize: '10px', fontFamily: 'monospace', color: '#667766'
        }).setOrigin(0.5);

        const fightBtn = this.add.rectangle(cx, 230, 170, 80, 0x442222)
            .setStrokeStyle(2, 0xff4444).setInteractive();
        this.add.text(cx, 210, '⚔️ 정면 돌파', {
            fontSize: '15px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.add.text(cx, 232, '최종 전투', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa6666'
        }).setOrigin(0.5);
        this.add.text(cx, 250, '승리 시 확정 탈출', {
            fontSize: '10px', fontFamily: 'monospace', color: '#886666'
        }).setOrigin(0.5);

        const blastColor = canBlast ? 0x442244 : 0x222222;
        const blastBorder = canBlast ? 0xaa44ff : 0x444444;
        const blastBtn = this.add.rectangle(cx + 200, 230, 170, 80, blastColor)
            .setStrokeStyle(2, blastBorder);
        this.add.text(cx + 200, 210, '💥 폭발 돌파', {
            fontSize: '15px', fontFamily: 'monospace', color: canBlast ? '#aa44ff' : '#666666', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.add.text(cx + 200, 232, canBlast ? (hasC4 ? 'C4 소모' : 'EMP 소모') : '폭발물 없음', {
            fontSize: '12px', fontFamily: 'monospace', color: canBlast ? '#8866aa' : '#555555'
        }).setOrigin(0.5);
        this.add.text(cx + 200, 250, canBlast ? '100% 성공' : '사용 불가', {
            fontSize: '10px', fontFamily: 'monospace', color: canBlast ? '#8844aa' : '#555555'
        }).setOrigin(0.5);

        stealthBtn.on('pointerover', () => stealthBtn.setFillStyle(0x335533));
        stealthBtn.on('pointerout', () => stealthBtn.setFillStyle(0x224422));
        stealthBtn.on('pointerdown', () => this.attemptEscape());

        fightBtn.on('pointerover', () => fightBtn.setFillStyle(0x663333));
        fightBtn.on('pointerout', () => fightBtn.setFillStyle(0x442222));
        fightBtn.on('pointerdown', () => this.startEscapeBattle());

        if (canBlast) {
            blastBtn.setInteractive();
            blastBtn.on('pointerover', () => blastBtn.setFillStyle(0x553355));
            blastBtn.on('pointerout', () => blastBtn.setFillStyle(0x442244));
            blastBtn.on('pointerdown', () => this.blastEscape(hasC4 ? 'c4' : 'emp'));
        }

        if (this.weightSystem.inventory.length > 0) {
            this.add.text(cx, 310, '── 아이템 버리기 (탈출 확률↑) ──', {
                fontSize: '14px', fontFamily: 'monospace', color: '#556677'
            }).setOrigin(0.5);

            this.drawDropItems(340);
        }

        const backBtn = this.add.rectangle(cx, 680, 200, 40, 0x222233)
            .setStrokeStyle(1, 0x445566).setInteractive();
        this.add.text(cx, 680, '탐색 계속', {
            fontSize: '14px', fontFamily: 'monospace', color: '#8899aa'
        }).setOrigin(0.5);

        backBtn.on('pointerover', () => backBtn.setFillStyle(0x333344));
        backBtn.on('pointerout', () => backBtn.setFillStyle(0x222233));
        backBtn.on('pointerdown', () => {
            this.scene.start('BOMapScene', {
                party: this.party,
                mapSystem: this.mapSystem,
                weightSystem: this.weightSystem,
                tensionSystem: this.tensionSystem
            });
        });
    }

    drawDropItems(startY) {
        const inv = this.weightSystem.inventory;
        const cols = 6;
        const cardW = 160, cardH = 50, gap = 10;

        inv.forEach((item, i) => {
            const col = i % cols;
            const row = Math.floor(i / cols);
            const totalRowW = Math.min(inv.length - row * cols, cols) * (cardW + gap) - gap;
            const x = 640 - totalRowW / 2 + cardW / 2 + col * (cardW + gap);
            const y = startY + row * (cardH + gap);

            const rarityInfo = BO_RARITIES[item.rarity] || BO_RARITIES.common;
            const card = this.add.rectangle(x, y, cardW, cardH, 0x111122)
                .setStrokeStyle(1, rarityInfo.borderColor).setInteractive();

            this.add.text(x, y - 10, `${item.icon} ${item.name}`, {
                fontSize: '11px', fontFamily: 'monospace', color: rarityInfo.color
            }).setOrigin(0.5);

            this.add.text(x, y + 10, `${item.weight}kg | 가치:${item.value}`, {
                fontSize: '9px', fontFamily: 'monospace', color: '#667788'
            }).setOrigin(0.5);

            card.on('pointerover', () => card.setStrokeStyle(2, 0xff4444));
            card.on('pointerout', () => card.setStrokeStyle(1, rarityInfo.borderColor));
            card.on('pointerdown', () => {
                this.weightSystem.removeItem(i);
                this.scene.restart({
                    party: this.party,
                    mapSystem: this.mapSystem,
                    weightSystem: this.weightSystem,
                    tensionSystem: this.tensionSystem
                });
            });
        });
    }

    startEscapeBattle() {
        this.scene.start('BORoomScene', {
            party: this.party,
            mapSystem: this.mapSystem,
            weightSystem: this.weightSystem,
            tensionSystem: this.tensionSystem,
            roomType: 'enemy',
            depth: this.mapSystem.currentDepth || 5,
            escapeBattle: true
        });
    }

    blastEscape(itemId) {
        const idx = this.weightSystem.inventory.findIndex(it => it.id === itemId);
        if (idx >= 0) this.weightSystem.removeItem(idx);

        const cx = 640;
        this.add.text(cx, 450, `💥 ${itemId === 'c4' ? 'C4' : 'EMP'}로 출구를 폭파!`, {
            fontSize: '24px', fontFamily: 'monospace', color: '#aa44ff', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(200);

        this.time.delayedCall(1200, () => {
            this.scene.start('BOResultScene', {
                party: this.party,
                weightSystem: this.weightSystem,
                tensionSystem: this.tensionSystem,
                mapSystem: this.mapSystem,
                escaped: true,
                died: false
            });
        });
    }

    attemptEscape() {
        const escapeChance = this.weightSystem.getEscapeChance(this.tensionSystem.tension);
        const success = Math.random() < escapeChance;

        if (success) {
            this.scene.start('BOResultScene', {
                party: this.party,
                weightSystem: this.weightSystem,
                tensionSystem: this.tensionSystem,
                mapSystem: this.mapSystem,
                escaped: true,
                died: false
            });
        } else {
            const cx = 640;
            this.add.text(cx, 500, '❌ 탈출 실패!', {
                fontSize: '28px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(200);

            this.add.text(cx, 540, '아이템을 버리고 다시 시도하세요', {
                fontSize: '14px', fontFamily: 'monospace', color: '#cc6666'
            }).setOrigin(0.5).setDepth(200);

            this.time.delayedCall(1500, () => {
                this.scene.restart({
                    party: this.party,
                    mapSystem: this.mapSystem,
                    weightSystem: this.weightSystem,
                    tensionSystem: this.tensionSystem
                });
            });
        }
    }
}
