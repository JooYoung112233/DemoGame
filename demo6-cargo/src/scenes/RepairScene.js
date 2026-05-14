class LCRepairScene extends Phaser.Scene {
    constructor() { super('LCRepairScene'); }

    init(data) {
        this.party = data.party;
        this.trainManager = data.trainManager;
        this.invasionSystem = data.invasionSystem;
        this.autoResults = data.autoResults;
        this.battleResult = data.battleResult;
        this.selectedModules = data.selectedModules;
        this.allies = data.allies || [];
    }

    create() {
        this.cameras.main.setBackgroundColor('#1a1510');
        const cx = 640;

        this.trainManager.autoRepairAll();

        const healed = this.trainManager.healAllies(this.allies);

        this.add.text(cx, 25, `라운드 ${this.invasionSystem.currentRound} 결과`, {
            fontSize: '22px', fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold'
        }).setOrigin(0.5);

        let yPos = 55;

        const br = this.battleResult;
        const car = this.trainManager.cars[br.carIndex];
        const resultColor = br.victory ? '#44ff88' : '#ff4444';
        const resultText = br.victory ? '방어 성공!' : '방어 실패...';
        this.add.text(cx, yPos, `${car.name}: ${resultText} (처치: ${br.enemiesDefeated})`, {
            fontSize: '15px', fontFamily: 'monospace', color: resultColor
        }).setOrigin(0.5);
        yPos += 25;

        if (br.drops.length > 0) {
            const dropText = br.drops.map(d => `${d.name}×${d.amount}`).join(', ');
            this.add.text(cx, yPos, `획득: ${dropText}`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44'
            }).setOrigin(0.5);
            yPos += 20;
        }

        this.autoResults.forEach(r => {
            const aCar = this.trainManager.cars[r.carIndex];
            if (!aCar || aCar.destroyed) return;
            let info;
            if (r.defended && r.method === 'none') {
                info = `${aCar.name}: 침입 없음`;
            } else if (r.defended) {
                info = `${aCar.name}: ${r.method}(으)로 자동 방어 성공!`;
            } else {
                info = `${aCar.name}: 방어 실패 — ${r.damage} 피해 (${r.method})`;
            }
            const color = r.defended ? '#88aa66' : '#cc6644';
            this.add.text(cx, yPos, info, {
                fontSize: '12px', fontFamily: 'monospace', color: color
            }).setOrigin(0.5);
            yPos += 18;
        });

        if (healed > 0) {
            this.add.text(cx, yPos, `의료칸 효과: 아군 총 ${healed} HP 회복`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#44ff88'
            }).setOrigin(0.5);
            yPos += 18;
        }

        yPos += 15;
        this.add.text(cx, yPos, '── 열차 상태 ──', {
            fontSize: '14px', fontFamily: 'monospace', color: '#665533'
        }).setOrigin(0.5);
        yPos += 25;

        this.drawTrainStatus(yPos);

        yPos = 420;
        this.add.text(cx, yPos, `부품: ${this.trainManager.parts}  |  탄약: ${this.trainManager.ammo}  |  의료킷: ${this.trainManager.medkits}`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#aa8866'
        }).setOrigin(0.5);

        yPos += 30;
        this.drawMedkitOptions(yPos);

        yPos += 40;
        this.add.text(cx, yPos, '── 수리할 칸 선택 (부품 2개 소모) ──', {
            fontSize: '14px', fontFamily: 'monospace', color: '#aa7744'
        }).setOrigin(0.5);

        yPos += 30;
        this.drawRepairOptions(yPos);

        if (this.trainManager.isGameOver()) {
            this.time.delayedCall(1000, () => this.goToResult());
            return;
        }

        const nextBtn = this.add.rectangle(cx, 660, 220, 44, 0x664411)
            .setStrokeStyle(2, 0xff8844).setInteractive();
        this.add.text(cx, 660, this.invasionSystem.isLastRound() ? '최종 결과' : '다음 라운드', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold'
        }).setOrigin(0.5);

        nextBtn.on('pointerover', () => nextBtn.setFillStyle(0x884422));
        nextBtn.on('pointerout', () => nextBtn.setFillStyle(0x664411));
        nextBtn.on('pointerdown', () => {
            if (this.invasionSystem.isLastRound()) {
                this.goToResult();
            } else {
                this.scene.start('LCTrainScene', {
                    modules: this.selectedModules,
                    trainManager: this.trainManager,
                    invasionSystem: this.invasionSystem,
                    party: this.party
                });
            }
        });
    }

    drawTrainStatus(startY) {
        const carWidth = 240, gap = 15;
        const totalW = 4 * carWidth + 3 * gap;
        const startX = 640 - totalW / 2 + carWidth / 2;

        this.trainManager.cars.forEach((car, i) => {
            const x = startX + i * (carWidth + gap);
            const y = startY + 50;
            const alpha = car.destroyed ? 0.3 : 1;

            this.add.rectangle(x, y, carWidth, 90, car.destroyed ? 0x111111 : 0x221a10, alpha)
                .setStrokeStyle(1, car.color);

            this.add.text(x, y - 30, car.name, {
                fontSize: '13px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            }).setOrigin(0.5).setAlpha(alpha);

            if (car.destroyed) {
                this.add.text(x, y, '파괴됨', {
                    fontSize: '14px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
                }).setOrigin(0.5);
                this.add.text(x, y + 18, car.lostEffect, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#aa4444'
                }).setOrigin(0.5);
                return;
            }

            const hpRatio = car.hp / car.maxHp;
            const hpColor = hpRatio > 0.6 ? '#44ff88' : hpRatio > 0.3 ? '#ffaa00' : '#ff4444';
            this.add.text(x, y - 10, `HP: ${car.hp}/${car.maxHp}`, {
                fontSize: '11px', fontFamily: 'monospace', color: hpColor
            }).setOrigin(0.5);

            const barW = carWidth - 30;
            this.add.rectangle(x, y + 5, barW, 5, 0x333333);
            const fillW = barW * hpRatio;
            if (fillW > 0) {
                const barColor = hpRatio > 0.6 ? 0x44ff88 : hpRatio > 0.3 ? 0xffaa00 : 0xff4444;
                this.add.rectangle(x - barW / 2 + fillW / 2, y + 5, fillW, 5, barColor);
            }

            if (car.module) {
                this.add.text(x, y + 22, `${car.module.icon} ${car.module.name}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: car.module.color
                }).setOrigin(0.5);
            }

            if (car.doorBroken) {
                this.add.text(x, y + 36, '⚠️ 문 파손', {
                    fontSize: '9px', fontFamily: 'monospace', color: '#ff6644'
                }).setOrigin(0.5);
            }
        });
    }

    drawMedkitOptions(y) {
        const cx = 640;
        if (this.trainManager.medkits <= 0) return;

        const damaged = this.trainManager.cars.filter(c => !c.destroyed && c.hp < c.maxHp);
        if (damaged.length === 0) return;

        this.add.text(cx, y, `── 의료킷 사용 (${this.trainManager.medkits}개 보유, 칸 HP 25% 회복) ──`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#44ff88'
        }).setOrigin(0.5);

        const btnW = 120, gap = 10;
        const totalW = damaged.length * btnW + (damaged.length - 1) * gap;
        const startX = cx - totalW / 2 + btnW / 2;

        damaged.forEach((car, i) => {
            const x = startX + i * (btnW + gap);
            const btn = this.add.rectangle(x, y + 28, btnW, 30, 0x113322)
                .setStrokeStyle(1, 0x44ff88).setInteractive();
            const healAmt = Math.floor(car.maxHp * 0.25);
            this.add.text(x, y + 28, `💊 ${car.name}\n+${healAmt}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#44ff88', align: 'center'
            }).setOrigin(0.5);

            btn.on('pointerover', () => btn.setFillStyle(0x225533));
            btn.on('pointerout', () => btn.setFillStyle(0x113322));
            btn.on('pointerdown', () => {
                if (this.trainManager.medkits > 0) {
                    this.trainManager.medkits--;
                    car.repair(healAmt);
                    this.scene.restart({
                        party: this.party,
                        trainManager: this.trainManager,
                        invasionSystem: this.invasionSystem,
                        autoResults: this.autoResults,
                        battleResult: this.battleResult,
                        selectedModules: this.selectedModules,
                        allies: this.allies
                    });
                }
            });
        });
    }

    drawRepairOptions(y) {
        const carWidth = 140, gap = 15;
        const repairable = this.trainManager.cars.filter(c => !c.destroyed && c.hp < c.maxHp);

        if (repairable.length === 0) {
            this.add.text(640, y, '수리할 칸이 없습니다', {
                fontSize: '13px', fontFamily: 'monospace', color: '#665533'
            }).setOrigin(0.5);
            return;
        }

        if (this.trainManager.parts < 2) {
            this.add.text(640, y, '부품이 부족합니다 (2개 필요)', {
                fontSize: '13px', fontFamily: 'monospace', color: '#886644'
            }).setOrigin(0.5);
            return;
        }

        const totalW = repairable.length * carWidth + (repairable.length - 1) * gap;
        const startX = 640 - totalW / 2 + carWidth / 2;

        repairable.forEach((car, i) => {
            const x = startX + i * (carWidth + gap);
            const btn = this.add.rectangle(x, y, carWidth, 36, 0x443311)
                .setStrokeStyle(1, 0x664422).setInteractive();
            const healAmount = Math.floor(car.maxHp * 0.4);
            this.add.text(x, y, `${car.name}\n+${healAmount} HP`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#ffaa66', align: 'center'
            }).setOrigin(0.5);

            btn.on('pointerover', () => btn.setFillStyle(0x664422));
            btn.on('pointerout', () => btn.setFillStyle(0x443311));
            btn.on('pointerdown', () => {
                if (this.trainManager.repairCar(car.index)) {
                    this.scene.restart({
                        party: this.party,
                        trainManager: this.trainManager,
                        invasionSystem: this.invasionSystem,
                        autoResults: this.autoResults,
                        battleResult: this.battleResult,
                        selectedModules: this.selectedModules,
                        allies: this.allies
                    });
                }
            });
        });
    }

    goToResult() {
        this.scene.start('LCResultScene', {
            trainManager: this.trainManager,
            invasionSystem: this.invasionSystem,
            party: this.party
        });
    }
}
