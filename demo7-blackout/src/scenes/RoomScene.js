class BORoomScene extends Phaser.Scene {
    constructor() { super('BORoomScene'); }

    init(data) {
        this.party = data.party;
        this.mapSystem = data.mapSystem;
        this.weightSystem = data.weightSystem;
        this.tensionSystem = data.tensionSystem;
        this.roomType = data.roomType;
        this.depth = data.depth || 0;
        this.escapeBattle = data.escapeBattle || false;
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');
        const cx = 640;
        this.battleOver = false;
        this.speedMultiplier = 1;

        const roomInfo = BO_ROOM_TYPES[this.roomType] || BO_ROOM_TYPES.empty;
        this.add.text(cx, 30, `${roomInfo.icon} ${roomInfo.name}`, {
            fontSize: '24px', fontFamily: 'monospace', color: roomInfo.color, fontStyle: 'bold'
        }).setOrigin(0.5);

        if (this.roomType === 'enemy') {
            this.startCombat();
        } else if (this.roomType === 'item') {
            this.showItemEvent();
        } else if (this.roomType === 'trap') {
            this.showTrapEvent();
        } else if (this.roomType === 'rest') {
            this.showRestEvent();
        } else {
            this.showEmptyRoom();
        }
    }

    startCombat() {
        const cx = 640;
        this.allies = [];
        this.enemies = [];

        const enemyData = generateEnemiesForRoom(this.depth, this.tensionSystem.tension);

        this.add.text(cx, 60, `적 ${enemyData.length}마리 조우!`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff6666'
        }).setOrigin(0.5);

        const fleeChance = this.weightSystem.getFleeChance();
        this.add.text(cx, 80, `도주 확률: ${Math.floor(fleeChance * 100)}%`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa8866'
        }).setOrigin(0.5);

        this.drawBackground();

        this.party.forEach((key, i) => {
            const data = { ...BO_ALLIES[key] };
            const x = 180 + i * 70;
            const y = 380 + (i % 2 === 0 ? 0 : 15);
            const char = new BOCharacter(this, data, 'ally', x, y);
            if (BO_SKILLS[key]) {
                char.skill = BO_SKILLS[key];
                char.skillCooldown = BO_SKILLS[key].cooldown;
                char.skillTimer = 0;
            }
            this.allies.push(char);
        });

        enemyData.forEach((data, i) => {
            const x = 1100 - i * 60;
            const y = 380 + (i % 2 === 0 ? 0 : 15);
            this.enemies.push(new BOCharacter(this, data, 'enemy', x, y));
        });

        this.applyTensionCombatEffects();

        this.stolenItem = null;
        this.combatMode = true;
        this.reinforceTimer = 0;

        const speedBtn = this.add.text(1220, 30, '×1', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
            backgroundColor: '#221122', padding: { x: 8, y: 4 }
        }).setOrigin(0.5).setDepth(100).setInteractive();
        speedBtn.on('pointerdown', () => {
            this.speedMultiplier = this.speedMultiplier === 1 ? 2 : this.speedMultiplier === 2 ? 3 : 1;
            speedBtn.setText(`×${this.speedMultiplier}`);
        });

        const fleeBtn = this.add.rectangle(1200, 680, 120, 36, 0x442222)
            .setStrokeStyle(1, 0x884444).setInteractive().setDepth(100);
        this.add.text(1200, 680, '도주', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff8888'
        }).setOrigin(0.5).setDepth(100);

        fleeBtn.on('pointerover', () => fleeBtn.setFillStyle(0x663333));
        fleeBtn.on('pointerout', () => fleeBtn.setFillStyle(0x442222));
        fleeBtn.on('pointerdown', () => {
            if (this.battleOver) return;
            if (Math.random() < fleeChance) {
                this.battleOver = true;
                this.combatMode = false;
                this.add.text(cx, 300, '도주 성공!', {
                    fontSize: '28px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
                }).setOrigin(0.5).setDepth(200);
                this.time.delayedCall(1000, () => this.returnToMap());
            } else {
                this.add.text(cx, 300, '도주 실패!', {
                    fontSize: '22px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
                }).setOrigin(0.5).setDepth(200);
                this.time.delayedCall(800, () => {});
            }
        });
    }

    applyTensionCombatEffects() {
        const t = this.tensionSystem.tension;
        if (t <= 1) return;

        let label = '';
        if (t === 2) {
            this.enemies.forEach(e => { e.dodgeRate = (e.dodgeRate || 0) + 0.10; });
            label = '긴장 효과: 적 회피 +10%';
        } else if (t === 3) {
            this.enemies.forEach(e => { e.critRate = (e.critRate || 0) + 0.15; });
            this.allies.forEach(a => { a.dodgeRate = Math.max(0, (a.dodgeRate || 0) - 0.10); });
            label = '긴장 효과: 적 크리 +15%, 아군 회피 -10%';
        } else if (t === 4) {
            this.enemies.forEach(e => { e.dodgeRate = (e.dodgeRate || 0) + 0.15; e.critRate = (e.critRate || 0) + 0.10; });
            label = '긴장 효과: 적 강화 + 증원 소환 가능';
        } else if (t >= 5) {
            this.enemies.forEach(e => { e.dodgeRate = (e.dodgeRate || 0) + 0.20; e.lifesteal = (e.lifesteal || 0) + 0.05; });
            this.allies.forEach(a => { a.dodgeRate = Math.max(0, (a.dodgeRate || 0) - 0.15); });
            label = '공포: 적 회피+20%, 흡혈+5%, 아군 회피-15%';
        }

        if (label) {
            const colors = ['#ffffff', '#ffffff', '#ffff44', '#ff8844', '#ff4444'];
            this.add.text(640, 100, `⚠️ ${label}`, {
                fontSize: '12px', fontFamily: 'monospace', color: colors[Math.min(t, 4)]
            }).setOrigin(0.5).setDepth(100);
        }
    }

    drawBackground() {
        const gfx = this.add.graphics();
        gfx.fillStyle(0x0a0a1a);
        gfx.fillRect(0, 0, 1280, 720);
        gfx.fillStyle(0x111122);
        gfx.fillRect(0, 420, 1280, 300);
        gfx.fillStyle(0x1a1a2a);
        gfx.fillRect(0, 418, 1280, 4);
    }

    update(time, delta) {
        if (!this.combatMode || this.battleOver) return;
        const dt = delta * this.speedMultiplier;

        const all = [...this.allies, ...this.enemies];
        this.allies.forEach(a => a.update(dt, all));
        this.enemies.forEach(e => e.update(dt, all));

        this.enemies.forEach(e => {
            if (e.stealChance > 0 && e.alive && !this.stolenItem && Math.random() < 0.0005 * dt) {
                const stolen = this.weightSystem.removeRandom();
                if (stolen) {
                    this.stolenItem = stolen;
                    this.add.text(640, 340, `${e.name}이(가) ${stolen.name}을(를) 훔쳤다!`, {
                        fontSize: '16px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
                    }).setOrigin(0.5).setDepth(200);
                }
            }
        });

        if (this.tensionSystem.tension >= 4) {
            this.reinforceTimer += dt;
            if (this.reinforceTimer >= 5000) {
                this.reinforceTimer = 0;
                if (Math.random() < 0.20 && this.enemies.filter(e => e.alive).length < 8) {
                    const scale = 1 + this.depth * 0.15 + this.tensionSystem.tension * 0.10;
                    const base = BO_ENEMIES.stalker;
                    const data = {
                        ...base,
                        hp: Math.floor(base.hp * scale),
                        atk: Math.floor(base.atk * scale),
                        def: Math.floor(base.def * scale),
                        type: 'stalker'
                    };
                    const aliveEnemies = this.enemies.filter(e => e.alive);
                    const rightmost = aliveEnemies.length > 0 ? Math.max(...aliveEnemies.map(e => e.container.x)) : 1100;
                    const x = Math.min(rightmost + 60, 1220);
                    const y = 380 + (this.enemies.length % 2 === 0 ? 0 : 15);
                    const reinforcement = new BOCharacter(this, data, 'enemy', x, y);
                    this.enemies.push(reinforcement);
                    const popup = this.add.text(x, y - 40, '증원!', {
                        fontSize: '14px', fontFamily: 'monospace', color: '#ff4488', fontStyle: 'bold'
                    }).setOrigin(0.5).setDepth(200);
                    this.tweens.add({ targets: popup, alpha: 0, y: y - 70, duration: 800, onComplete: () => popup.destroy() });
                }
            }
        }

        const alliesAlive = this.allies.some(a => a.alive);
        const enemiesAlive = this.enemies.some(e => e.alive);

        if (!enemiesAlive) {
            this.battleOver = true;
            this.combatMode = false;
            this.time.delayedCall(800, () => this.onCombatWin());
        } else if (!alliesAlive) {
            this.battleOver = true;
            this.combatMode = false;
            this.time.delayedCall(800, () => this.onCombatLose());
        }
    }

    onCombatWin() {
        if (this.escapeBattle) {
            this.add.text(640, 300, '돌파 성공! 탈출!', {
                fontSize: '28px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
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
            return;
        }
        const item = getItemByDepth(this.depth);
        this.showItemChoice(item, true);
    }

    onCombatLose() {
        this.scene.start('BOResultScene', {
            party: this.party,
            weightSystem: this.weightSystem,
            tensionSystem: this.tensionSystem,
            mapSystem: this.mapSystem,
            escaped: false,
            died: true
        });
    }

    showItemEvent() {
        const item = getItemByDepth(this.depth);
        this.showItemChoice(item, false);
    }

    showItemChoice(item, fromCombat) {
        const cx = 640;
        const y = fromCombat ? 500 : 200;

        if (fromCombat) {
            this.add.text(cx, y - 60, '전투 승리! 아이템 발견!', {
                fontSize: '18px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
            }).setOrigin(0.5);
        } else {
            this.add.text(cx, y - 60, '아이템을 발견했습니다!', {
                fontSize: '18px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
            }).setOrigin(0.5);
        }

        const rarityInfo = BO_RARITIES[item.rarity] || BO_RARITIES.common;
        this.add.rectangle(cx, y + 30, 300, 120, 0x111122).setStrokeStyle(2, rarityInfo.borderColor);
        this.add.text(cx, y, `${item.icon} ${item.name}`, {
            fontSize: '18px', fontFamily: 'monospace', color: rarityInfo.color, fontStyle: 'bold'
        }).setOrigin(0.5);
        this.add.text(cx, y + 22, item.desc, {
            fontSize: '12px', fontFamily: 'monospace', color: '#8899aa'
        }).setOrigin(0.5);
        this.add.text(cx, y + 42, `무게: ${item.weight}kg  |  가치: ${item.value}  |  ${rarityInfo.name}`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#667788'
        }).setOrigin(0.5);

        const canCarry = this.weightSystem.canCarry(item);
        const curWeight = this.weightSystem.getCurrentWeight();
        this.add.text(cx, y + 62, `현재 무게: ${curWeight}/${this.weightSystem.maxCapacity}`, {
            fontSize: '11px', fontFamily: 'monospace', color: canCarry ? '#44ff88' : '#ff4444'
        }).setOrigin(0.5);

        const pickBtn = this.add.rectangle(cx - 100, y + 100, 150, 40, canCarry ? 0x224422 : 0x222222)
            .setStrokeStyle(1, canCarry ? 0x44ff44 : 0x444444);
        this.add.text(cx - 100, y + 100, canCarry ? '줍기' : '무거움', {
            fontSize: '14px', fontFamily: 'monospace', color: canCarry ? '#44ff88' : '#666666', fontStyle: 'bold'
        }).setOrigin(0.5);

        if (canCarry) {
            pickBtn.setInteractive();
            pickBtn.on('pointerover', () => pickBtn.setFillStyle(0x335533));
            pickBtn.on('pointerout', () => pickBtn.setFillStyle(0x224422));
            pickBtn.on('pointerdown', () => {
                this.weightSystem.addItem(item);
                if (item.apply) item.apply(this.allies || []);
                if (item.tensionReduce) this.tensionSystem.reduceTension(item.tensionReduce);
                this.returnToMap();
            });
        }

        const skipBtn = this.add.rectangle(cx + 100, y + 100, 150, 40, 0x332222)
            .setStrokeStyle(1, 0x664444).setInteractive();
        this.add.text(cx + 100, y + 100, '무시', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff8888', fontStyle: 'bold'
        }).setOrigin(0.5);

        skipBtn.on('pointerover', () => skipBtn.setFillStyle(0x443333));
        skipBtn.on('pointerout', () => skipBtn.setFillStyle(0x332222));
        skipBtn.on('pointerdown', () => this.returnToMap());
    }

    showTrapEvent() {
        const cx = 640;
        const tension = this.tensionSystem.tension;
        const dmgPercent = 10 + Math.floor(Math.random() * 15);
        const disarmChance = Math.max(0.15, 0.70 - tension * 0.10);

        this.add.text(cx, 160, '⚠️ 함정 감지!', {
            fontSize: '26px', fontFamily: 'monospace', color: '#ffaa00', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(cx, 200, '함정을 해체하거나 강행 돌파할 수 있습니다.', {
            fontSize: '13px', fontFamily: 'monospace', color: '#aa8844'
        }).setOrigin(0.5);

        const disarmBtn = this.add.rectangle(cx - 160, 290, 260, 90, 0x112244)
            .setStrokeStyle(2, 0x4488ff).setInteractive();
        this.add.text(cx - 160, 265, '🔧 해체 시도', {
            fontSize: '16px', fontFamily: 'monospace', color: '#4488ff', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.add.text(cx - 160, 290, `성공 확률: ${Math.floor(disarmChance * 100)}%`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#6688aa'
        }).setOrigin(0.5);
        this.add.text(cx - 160, 310, '실패 시 피해 2배', {
            fontSize: '11px', fontFamily: 'monospace', color: '#886644'
        }).setOrigin(0.5);

        const pushBtn = this.add.rectangle(cx + 160, 290, 260, 90, 0x442211)
            .setStrokeStyle(2, 0xff8844).setInteractive();
        this.add.text(cx + 160, 265, '💨 강행 돌파', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.add.text(cx + 160, 290, `확정 피해: HP ${dmgPercent}%`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa6644'
        }).setOrigin(0.5);
        this.add.text(cx + 160, 310, '보상 아이템 획득', {
            fontSize: '11px', fontFamily: 'monospace', color: '#44aa44'
        }).setOrigin(0.5);

        disarmBtn.on('pointerover', () => disarmBtn.setFillStyle(0x223355));
        disarmBtn.on('pointerout', () => disarmBtn.setFillStyle(0x112244));
        pushBtn.on('pointerover', () => pushBtn.setFillStyle(0x663322));
        pushBtn.on('pointerout', () => pushBtn.setFillStyle(0x442211));

        disarmBtn.on('pointerdown', () => {
            disarmBtn.disableInteractive();
            pushBtn.disableInteractive();
            if (Math.random() < disarmChance) {
                this.add.text(cx, 400, '✅ 해체 성공! 피해 없음', {
                    fontSize: '20px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
                }).setOrigin(0.5);
                this.tensionSystem.reduceTension(1);
                this.add.text(cx, 430, '긴장도 -1', {
                    fontSize: '13px', fontFamily: 'monospace', color: '#88aaff'
                }).setOrigin(0.5);
            } else {
                const doubleDmg = dmgPercent * 2;
                this.add.text(cx, 400, `❌ 해체 실패! HP ${doubleDmg}% 피해!`, {
                    fontSize: '20px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
                }).setOrigin(0.5);
            }
            this.time.delayedCall(1500, () => this.returnToMap());
        });

        pushBtn.on('pointerdown', () => {
            disarmBtn.disableInteractive();
            pushBtn.disableInteractive();
            this.add.text(cx, 400, `💥 강행 돌파! HP ${dmgPercent}% 피해`, {
                fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
            }).setOrigin(0.5);

            const item = getItemByDepth(this.depth);
            const rarityInfo = BO_RARITIES[item.rarity] || BO_RARITIES.common;
            this.add.text(cx, 440, `${item.icon} ${item.name} 발견!`, {
                fontSize: '16px', fontFamily: 'monospace', color: rarityInfo.color, fontStyle: 'bold'
            }).setOrigin(0.5);

            if (this.weightSystem.canCarry(item)) {
                this.weightSystem.addItem(item);
                this.add.text(cx, 465, '아이템 획득!', {
                    fontSize: '12px', fontFamily: 'monospace', color: '#44ff88'
                }).setOrigin(0.5);
            } else {
                this.add.text(cx, 465, '무게 초과 — 획득 불가', {
                    fontSize: '12px', fontFamily: 'monospace', color: '#ff4444'
                }).setOrigin(0.5);
            }
            this.time.delayedCall(1500, () => this.returnToMap());
        });
    }

    showRestEvent() {
        const cx = 640;

        this.add.text(cx, 200, '🏠 은신처 발견!', {
            fontSize: '24px', fontFamily: 'monospace', color: '#88aaff', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(cx, 240, '긴장도가 1 감소합니다.', {
            fontSize: '14px', fontFamily: 'monospace', color: '#6688aa'
        }).setOrigin(0.5);

        this.tensionSystem.reduceTension(1);

        const continueBtn = this.add.rectangle(cx, 320, 200, 44, 0x112244)
            .setStrokeStyle(2, 0x4488ff).setInteractive();
        this.add.text(cx, 320, '계속 탐색', {
            fontSize: '16px', fontFamily: 'monospace', color: '#4488ff', fontStyle: 'bold'
        }).setOrigin(0.5);

        continueBtn.on('pointerover', () => continueBtn.setFillStyle(0x223355));
        continueBtn.on('pointerout', () => continueBtn.setFillStyle(0x112244));
        continueBtn.on('pointerdown', () => this.returnToMap());
    }

    showEmptyRoom() {
        const cx = 640;

        this.add.text(cx, 200, '▫️ 빈 방', {
            fontSize: '24px', fontFamily: 'monospace', color: '#666666', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(cx, 240, '아무것도 없습니다.', {
            fontSize: '14px', fontFamily: 'monospace', color: '#556677'
        }).setOrigin(0.5);

        const continueBtn = this.add.rectangle(cx, 320, 200, 44, 0x222233)
            .setStrokeStyle(2, 0x445566).setInteractive();
        this.add.text(cx, 320, '계속 탐색', {
            fontSize: '16px', fontFamily: 'monospace', color: '#8899aa', fontStyle: 'bold'
        }).setOrigin(0.5);

        continueBtn.on('pointerover', () => continueBtn.setFillStyle(0x333344));
        continueBtn.on('pointerout', () => continueBtn.setFillStyle(0x222233));
        continueBtn.on('pointerdown', () => this.returnToMap());
    }

    returnToMap() {
        this.scene.start('BOMapScene', {
            party: this.party,
            mapSystem: this.mapSystem,
            weightSystem: this.weightSystem,
            tensionSystem: this.tensionSystem
        });
    }

    shutdown() {
        if (this.allies) this.allies.forEach(a => a.destroy());
        if (this.enemies) this.enemies.forEach(e => e.destroy());
    }
}
