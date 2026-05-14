class LCBattleScene extends Phaser.Scene {
    constructor() { super('LCBattleScene'); }

    init(data) {
        this.party = data.party;
        this.carIndex = data.carIndex;
        this.enemyData = data.enemies || [];
        this.entryType = data.entryType || '문 파괴';
        this.trainManager = data.trainManager;
        this.invasionSystem = data.invasionSystem;
        this.autoResults = data.autoResults || [];
        this.selectedModules = data.selectedModules;
    }

    create() {
        this.cameras.main.setBackgroundColor('#1a1510');
        this.drawBackground();

        this.allies = [];
        this.enemies = [];
        this.battleOver = false;
        this.battleTime = 0;
        this.speedMultiplier = 1;

        const car = this.trainManager.cars[this.carIndex];
        this.add.text(640, 25, `${car.name} 방어전 — 라운드 ${this.invasionSystem.currentRound}`, {
            fontSize: '22px', fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(100);

        const region = this.invasionSystem.getRegionEffect();
        if (region.spdPenalty) {
            this.add.text(640, 52, `⚠️ ${region.name}: 전투 속도 -${region.spdPenalty * 100}%`, {
                fontSize: '13px', fontFamily: 'monospace', color: region.color
            }).setOrigin(0.5).setDepth(100);
        }

        this.timerText = this.add.text(640, 70, '0.0초', {
            fontSize: '14px', fontFamily: 'monospace', color: '#aa8866'
        }).setOrigin(0.5).setDepth(100);

        this.spawnAllies(region);
        this.spawnEnemies(region);

        if (car.module && car.module.atkBuff) {
            this.allies.forEach(a => { a.atk = Math.floor(a.atk * (1 + car.module.atkBuff)); });
        }

        if (car.module && car.module.allyThorns) {
            this.allies.forEach(a => { a.thorns += car.module.allyThorns; });
        }
        if (car.module && car.module.allyLifesteal) {
            this.allies.forEach(a => { a.lifesteal += car.module.allyLifesteal; });
        }

        LC_FARMING.applyEquippedGear(this.allies);

        if (!this.trainManager.isAmmoAvailable()) {
            this.allies.forEach(a => {
                if (a.range > 100) a.atk = Math.floor(a.atk * 0.5);
            });
        }

        this.applyEntryTypeEffect(car);

        const speedBtn = this.add.text(1220, 30, '×1', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
            backgroundColor: '#332211', padding: { x: 8, y: 4 }
        }).setOrigin(0.5).setDepth(100).setInteractive();
        speedBtn.on('pointerdown', () => {
            this.speedMultiplier = this.speedMultiplier === 1 ? 2 : this.speedMultiplier === 2 ? 3 : 1;
            speedBtn.setText(`×${this.speedMultiplier}`);
        });

        if (this.enemyData.length === 0) {
            this.battleOver = true;
            this.add.text(640, 360, '이 칸은 안전합니다!', {
                fontSize: '28px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(200);
            this.time.delayedCall(1500, () => this.onBattleEnd(true));
        }
    }

    drawBackground() {
        const gfx = this.add.graphics();
        gfx.fillGradientStyle(0x1a1510, 0x1a1510, 0x2a2015, 0x2a2015);
        gfx.fillRect(0, 0, 1280, 720);
        gfx.fillStyle(0x221a10);
        gfx.fillRect(0, 450, 1280, 270);
        gfx.fillStyle(0x332a18);
        gfx.fillRect(0, 448, 1280, 4);
    }

    applyEntryTypeEffect(car) {
        if (this.enemies.length === 0) return;

        let label = '';
        if (this.entryType === '창문 돌파') {
            this.enemies.forEach(e => {
                const dmg = Math.floor(e.maxHp * 0.15);
                e.hp -= dmg;
                if (e.hp <= 0) { e.hp = 1; }
            });
            label = '⚠️ 창문 돌파 — 적 HP 15% 감소';
        } else if (this.entryType === '천장 침입') {
            this.allies.forEach((a, i) => { a.container.x = 1100 - i * 70; });
            this.enemies.forEach((e, i) => { e.container.x = 180 + i * 60; });
            label = '⚠️ 천장 침입 — 위치 역전!';
        } else if (this.entryType === '문 파괴') {
            this.enemies.forEach(e => {
                const origAtk = e.atk;
                e.atk = Math.floor(e.atk * 1.2);
                this.time.delayedCall(5000, () => { if (e.alive) e.atk = origAtk; });
            });
            label = '⚠️ 문 파괴 — 적 ATK +20% (5초)';
        } else if (this.entryType === '외부 매달림') {
            this.enemies.forEach((e, i) => {
                if (i > 0) {
                    e.container.setAlpha(0);
                    e.alive = false;
                    this.time.delayedCall(i * 2000, () => {
                        e.container.setAlpha(1);
                        e.alive = true;
                        e.hp = e.maxHp;
                        DamagePopup.show(this, e.container.x, e.container.y - 30, '침입!', 0xff8844, false);
                    });
                }
            });
            label = '⚠️ 외부 매달림 — 적 순차 침입';
        }

        if (label) {
            const txt = this.add.text(640, 690, label, {
                fontSize: '13px', fontFamily: 'monospace', color: '#ff8844'
            }).setOrigin(0.5).setDepth(100);
            this.tweens.add({ targets: txt, alpha: 0, delay: 3000, duration: 1000 });
        }
    }

    spawnAllies(region) {
        const startX = 180;
        const spacing = 70;
        const car = this.trainManager.cars[this.carIndex];
        this.party.forEach((key, i) => {
            const data = { ...LC_ALLIES[key] };
            if (region.spdPenalty) {
                data.attackSpeed = Math.floor(data.attackSpeed * (1 + region.spdPenalty));
            }
            const x = startX + i * spacing;
            const y = 430 + (i % 2 === 0 ? 0 : 15);
            const char = new LCCharacter(this, data, 'ally', x, y);

            if (LC_SKILLS[key]) {
                char.assignSkill(LC_SKILLS[key]);
            }
            char.currentCar = car;
            char.role = data.role;

            this.allies.push(char);
        });
    }

    spawnEnemies(region) {
        const startX = 1100;
        const spacing = 60;
        this.enemyData.forEach((data, i) => {
            const d = { ...data };
            if (region.spdPenalty) {
                d.attackSpeed = Math.floor(d.attackSpeed * (1 + region.spdPenalty));
            }
            const x = startX - i * spacing;
            const y = 430 + (i % 2 === 0 ? 0 : 15);
            this.enemies.push(new LCCharacter(this, d, 'enemy', x, y));
        });
    }

    update(time, delta) {
        if (this.battleOver) return;
        const dt = delta * this.speedMultiplier;
        this.battleTime += dt;
        this.timerText.setText((this.battleTime / 1000).toFixed(1) + '초');

        const all = [...this.allies, ...this.enemies];
        this.allies.forEach(a => a.update(dt, all));
        this.enemies.forEach(e => e.update(dt, all));

        const alliesAlive = this.allies.some(a => a.alive);
        const enemiesAlive = this.enemies.some(e => e.alive);

        if (!enemiesAlive) {
            this.battleOver = true;
            this.time.delayedCall(800, () => this.onBattleEnd(true));
        } else if (!alliesAlive) {
            this.battleOver = true;
            const car = this.trainManager.cars[this.carIndex];
            const totalAtk = this.enemies.filter(e => e.alive).reduce((s, e) => s + e.atk, 0);
            car.takeDamage(totalAtk);
            this.time.delayedCall(800, () => this.onBattleEnd(false));
        }
    }

    onBattleEnd(victory) {
        const enemiesDefeated = this.enemies.filter(e => !e.alive).length;
        const drops = this.trainManager.generateDrops(enemiesDefeated);

        this.scene.start('LCRepairScene', {
            party: this.party,
            trainManager: this.trainManager,
            invasionSystem: this.invasionSystem,
            autoResults: this.autoResults,
            battleResult: { victory, carIndex: this.carIndex, enemiesDefeated, drops },
            selectedModules: this.selectedModules,
            allies: this.allies
        });
    }

    shutdown() {
        this.allies.forEach(a => a.destroy());
        this.enemies.forEach(e => e.destroy());
    }
}
