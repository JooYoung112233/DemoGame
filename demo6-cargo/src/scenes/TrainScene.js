class LCTrainScene extends Phaser.Scene {
    constructor() { super('LCTrainScene'); }

    init(data) {
        this.selectedModules = data.modules || [];
        this.trainManager = data.trainManager || new TrainManager();
        this.invasionSystem = data.invasionSystem || new InvasionSystem();
        this.party = data.party || ['guard', 'gunner', 'medic', 'engineer'];
    }

    create() {
        this.cameras.main.setBackgroundColor('#1a1510');

        if (this.invasionSystem.currentRound === 0) {
            this.selectedModules.forEach((mod, i) => {
                if (i < this.trainManager.cars.length) {
                    this.trainManager.cars[i].installModule(mod);
                }
            });
        }

        const round = this.invasionSystem.nextRound();
        const region = this.invasionSystem.getRegionEffect();

        this.add.text(640, 25, `라운드 ${round} / ${this.invasionSystem.maxRound}`, {
            fontSize: '24px', fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(100);

        this.add.text(640, 52, `지역: ${region.name} — ${region.desc}`, {
            fontSize: '13px', fontFamily: 'monospace', color: region.color
        }).setOrigin(0.5).setDepth(100);

        this.add.text(640, 72, `부품: ${this.trainManager.parts}  |  탄약: ${this.trainManager.ammo}  |  의료킷: ${this.trainManager.medkits}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa8866'
        }).setOrigin(0.5).setDepth(100);

        this.invasion = this.invasionSystem.generateInvasion(this.trainManager.cars);

        this.add.text(640, 105, '⚠️ 적 침입 예고 — 방어할 칸을 선택하세요!', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.drawTrainCars();
        this.selectedCar = -1;
    }

    drawTrainCars() {
        const carWidth = 260, carHeight = 200, gap = 20;
        const totalW = 4 * carWidth + 3 * gap;
        const startX = 640 - totalW / 2 + carWidth / 2;

        this.trainManager.cars.forEach((car, i) => {
            const x = startX + i * (carWidth + gap);
            const y = 310;

            const alpha = car.destroyed ? 0.3 : 1;
            const bgColor = car.destroyed ? 0x111111 : 0x221a10;
            const cardRect = this.add.rectangle(x, y, carWidth, carHeight, bgColor, alpha)
                .setStrokeStyle(2, car.color);

            this.add.text(x, y - 80, car.name, {
                fontSize: '16px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            }).setOrigin(0.5).setAlpha(alpha);

            const hpRatio = car.hp / car.maxHp;
            const hpColor = hpRatio > 0.6 ? '#44ff88' : hpRatio > 0.3 ? '#ffaa00' : '#ff4444';
            this.add.text(x, y - 58, `HP: ${car.hp}/${car.maxHp}`, {
                fontSize: '12px', fontFamily: 'monospace', color: hpColor
            }).setOrigin(0.5).setAlpha(alpha);

            const hpBarBg = this.add.rectangle(x, y - 44, carWidth - 20, 6, 0x333333).setAlpha(alpha);
            const hpBarW = (carWidth - 20) * hpRatio;
            if (hpBarW > 0) {
                const hpBarColor = hpRatio > 0.6 ? 0x44ff88 : hpRatio > 0.3 ? 0xffaa00 : 0xff4444;
                this.add.rectangle(x - (carWidth - 20) / 2 + hpBarW / 2, y - 44, hpBarW, 6, hpBarColor).setAlpha(alpha);
            }

            if (car.module) {
                this.add.text(x, y - 28, `${car.module.icon} ${car.module.name}`, {
                    fontSize: '11px', fontFamily: 'monospace', color: car.module.color
                }).setOrigin(0.5).setAlpha(alpha);
            }

            if (car.destroyed) {
                this.add.text(x, y + 5, '파괴됨', {
                    fontSize: '18px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
                }).setOrigin(0.5);
                this.add.text(x, y + 28, car.lostEffect, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#aa4444'
                }).setOrigin(0.5);
                return;
            }

            const inv = this.invasion[i];
            if (inv.enemies.length > 0) {
                this.add.text(x, y - 5, `⚠️ 침입: ${inv.enemies.length}마리`, {
                    fontSize: '14px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
                }).setOrigin(0.5);

                const enemyNames = inv.enemies.map(e => e.name).join(', ');
                this.add.text(x, y + 15, enemyNames, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#cc8866'
                }).setOrigin(0.5);

                this.add.text(x, y + 32, `경로: ${inv.entryType}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#886644'
                }).setOrigin(0.5);

                const autoRate = car.getAutoDefenseRate();
                if (autoRate > 0 && this.trainManager.isPowerOn()) {
                    this.add.text(x, y + 48, `자동방어: ${Math.floor(autoRate * 100)}%`, {
                        fontSize: '11px', fontFamily: 'monospace', color: '#44ccff'
                    }).setOrigin(0.5);
                }
            } else {
                this.add.text(x, y + 10, '안전', {
                    fontSize: '16px', fontFamily: 'monospace', color: '#44ff88'
                }).setOrigin(0.5);
            }

            const defendBtn = this.add.rectangle(x, y + 78, carWidth - 20, 32, 0x443311)
                .setStrokeStyle(1, 0x664422).setInteractive();
            const btnText = this.add.text(x, y + 78, '이 칸 방어', {
                fontSize: '14px', fontFamily: 'monospace', color: '#ffaa66'
            }).setOrigin(0.5);

            defendBtn.on('pointerover', () => defendBtn.setFillStyle(0x664422));
            defendBtn.on('pointerout', () => defendBtn.setFillStyle(0x443311));
            defendBtn.on('pointerdown', () => {
                this.selectedCar = i;
                this.startBattle(i);
            });
        });

    }

    startBattle(carIndex) {
        const results = this.trainManager.applyAutoDefense(
            this.invasion.filter((inv, idx) => idx !== carIndex)
        );

        const inv = this.invasion[carIndex];

        this.scene.start('LCBattleScene', {
            party: this.party,
            carIndex: carIndex,
            enemies: inv.enemies,
            entryType: inv.entryType,
            trainManager: this.trainManager,
            invasionSystem: this.invasionSystem,
            autoResults: results,
            selectedModules: this.selectedModules
        });
    }
}
