class BattleScene extends Phaser.Scene {
    constructor() {
        super({ key: 'BattleScene' });
    }

    init(data) {
        this.runState = data.runState;
        this.isAmbush = data.isAmbush || false;
        this.gameSpeed = 1.5;
        this.mapManager = new MapManager();
    }

    create() {
        this.createBackground();

        this.combat = new CombatSystem(this);
        this.battleTime = 0;
        this.battleEnded = false;
        this.allyChars = [];

        PartyManager.applyBattleTraits(this.runState);

        this.spawnAllies();
        this.spawnEnemies();
        this.createUI();
    }

    createBackground() {
        const g = this.add.graphics();
        const progress = this.runState.currentNode / (this.mapManager.getNodeCount() - 1);

        for (let i = 0; i < 460; i++) {
            const ratio = i / 460;
            const r = Math.floor(20 + ratio * 20 + progress * 30);
            const gr = Math.floor(20 + ratio * 25 - progress * 15);
            const b = Math.floor(40 + ratio * 30 - progress * 20);
            g.fillStyle(Phaser.Display.Color.GetColor(
                Math.min(255, Math.max(0, r)),
                Math.min(255, Math.max(0, gr)),
                Math.min(255, Math.max(0, b))
            ));
            g.fillRect(0, i, 1280, 1);
        }

        g.fillStyle(0x2a3a2a);
        g.fillRect(0, 460, 1280, 260);
        g.lineStyle(2, 0x4a5a4a);
        g.lineBetween(0, 460, 1280, 460);

        for (let i = 0; i < 50; i++) {
            const x = Math.random() * 1280;
            const y = 460 + Math.random() * 260;
            g.fillStyle(0x3a4a3a);
            g.fillRect(x, y, 2 + Math.random() * 4, 1);
        }

        const node = this.mapManager.getNode(this.runState.currentNode);
        const title = node ? node.name : '전투';
        this.add.text(640, 470, title, {
            fontSize: '12px', fontFamily: 'monospace', color: '#335533', alpha: 0.5
        }).setOrigin(0.5);
    }

    spawnAllies() {
        const startX = 180;
        const spacing = 70;
        const y = 440;

        this.runState.party.forEach((unit, i) => {
            const data = unit.toCharacterData();
            data.hp = unit.currentHp;
            const x = startX + i * spacing;
            const char = new Character(this, data, 'ally', x, y);
            char.maxHp = unit.getStats().hp;
            char.hp = unit.currentHp;
            char.assignSkills(SkillSystem.getSkillsForUnit(unit));
            this.combat.addCharacter(char);
            this.allyChars.push(char);
        });
    }

    spawnEnemies() {
        const startX = 1100;
        const spacing = 70;
        const y = 440;

        let enemies;
        if (this.isAmbush) {
            enemies = [
                this.mapManager.getScaledEnemies(this.runState.currentNode)[0],
                this.mapManager.getScaledEnemies(this.runState.currentNode)[0]
            ].filter(Boolean);
        } else {
            enemies = this.mapManager.getScaledEnemies(this.runState.currentNode);
        }

        enemies.forEach((eData, i) => {
            const x = startX - i * spacing;
            const char = new Character(this, eData, 'enemy', x, y);
            char.assignSkills(eData.skills || []);
            this.combat.addCharacter(char);
        });

        this.enemyCount = enemies.length;
    }

    createUI() {
        this.timeText = this.add.text(640, 20, '00:00', {
            fontSize: '18px', fontFamily: 'monospace',
            color: '#ffffff', stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(20);

        this.speedText = this.add.text(1200, 20, 'x' + this.gameSpeed, {
            fontSize: '14px', fontFamily: 'monospace',
            color: '#ffcc00', stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5).setDepth(20);

        const speedBtn = this.add.rectangle(1245, 20, 50, 24, 0x333355)
            .setStrokeStyle(1, 0x5566aa)
            .setInteractive({ useHandCursor: true }).setDepth(20);
        this.add.text(1245, 20, 'SPD', {
            fontSize: '10px', fontFamily: 'monospace', color: '#aaaacc'
        }).setOrigin(0.5).setDepth(21);

        speedBtn.on('pointerdown', () => {
            if (this.gameSpeed === 1) this.gameSpeed = 1.5;
            else if (this.gameSpeed === 1.5) this.gameSpeed = 2;
            else if (this.gameSpeed === 2) this.gameSpeed = 3;
            else this.gameSpeed = 1;
            this.speedText.setText('x' + this.gameSpeed);
        });

        this.allyCountText = this.add.text(20, 20, '', {
            fontSize: '14px', fontFamily: 'monospace',
            color: '#44ff88', stroke: '#000000', strokeThickness: 2
        }).setDepth(20);

        this.enemyCountText = this.add.text(1260, 50, '', {
            fontSize: '14px', fontFamily: 'monospace',
            color: '#ff6644', stroke: '#000000', strokeThickness: 2
        }).setOrigin(1, 0).setDepth(20);

        this.add.text(20, 50, `🪙 ${this.runState.gold}G`, {
            fontSize: '12px', fontFamily: 'monospace',
            color: '#ffcc44', stroke: '#000000', strokeThickness: 2
        }).setDepth(20);
    }

    update(time, rawDelta) {
        if (this.battleEnded) return;

        const delta = rawDelta * this.gameSpeed;
        this.battleTime += delta;

        this.combat.update(delta);

        const mins = Math.floor(this.battleTime / 60000);
        const secs = Math.floor((this.battleTime % 60000) / 1000);
        this.timeText.setText(String(mins).padStart(2, '0') + ':' + String(secs).padStart(2, '0'));

        const allyAlive = this.combat.characters.filter(c => c.team === 'ally' && c.alive).length;
        const enemyAlive = this.combat.characters.filter(c => c.team === 'enemy' && c.alive).length;
        this.allyCountText.setText('ALLY: ' + allyAlive + '/' + this.runState.party.length);
        this.enemyCountText.setText('ENEMY: ' + enemyAlive + '/' + this.enemyCount);
    }

    dealDamage(target, amount, isCrit, attacker) {
        target.takeDamage(amount);
        if (attacker) attacker.totalDamageDealt += amount;

        const x = target.container.x + (Math.random() - 0.5) * 10;
        const y = target.container.y - 20;

        if (isCrit) {
            DamagePopup.showCritical(this, x, y, amount);
            this.cameras.main.shake(80, 0.003);
        } else {
            DamagePopup.show(this, x, y, amount, 0xffffff, false);
        }
        this.showKnockback(target, attacker);
    }

    dealSkillDamage(caster, target, amount, skillName) {
        target.takeDamage(amount);
        if (caster) caster.totalDamageDealt += amount;
        const x = target.container.x + (Math.random() - 0.5) * 10;
        const y = target.container.y - 20;
        DamagePopup.show(this, x, y, amount, 0xff88ff, false);
    }

    showKnockback(target, attacker) {
        if (!target.alive || !attacker) return;
        const dir = target.container.x > attacker.container.x ? 1 : -1;
        this.tweens.add({
            targets: target.container,
            x: target.container.x + dir * 6,
            duration: 60, yoyo: true, ease: 'Power1'
        });
    }

    showAttackEffect(fromX, fromY, toX, toY) {
        const spark = this.add.circle(toX, toY - 5, 6, 0xffffff, 0.8).setDepth(50);
        this.tweens.add({
            targets: spark, alpha: 0, scaleX: 2, scaleY: 2,
            duration: 200, onComplete: () => spark.destroy()
        });
    }

    showProjectile(fromX, fromY, toX, toY, color) {
        const proj = this.add.circle(fromX, fromY - 10, 4, color, 1).setDepth(50);
        this.tweens.add({
            targets: proj, x: toX, y: toY - 10,
            duration: 300, ease: 'Power1', onComplete: () => proj.destroy()
        });
    }

    showAoEEffect(cx, cy, radius, color) {
        const circle = this.add.circle(cx, cy, radius, color || 0xaa44ff, 0.3).setDepth(50);
        this.tweens.add({
            targets: circle, alpha: 0, scaleX: 1.5, scaleY: 1.5,
            duration: 600, onComplete: () => circle.destroy()
        });
        for (let i = 0; i < 8; i++) {
            const px = cx + (Math.random() - 0.5) * radius;
            const py = cy + (Math.random() - 0.5) * 30;
            const p = this.add.circle(px, py, 3, 0xff8844, 1).setDepth(51);
            this.tweens.add({
                targets: p, y: py - 30 - Math.random() * 20, alpha: 0,
                duration: 400 + Math.random() * 300, onComplete: () => p.destroy()
            });
        }
    }

    showSkillEffect(caster, text, color) {
        const t = this.add.text(caster.container.x, caster.container.y - 50, text, {
            fontSize: '14px', fontFamily: 'monospace',
            color: Phaser.Display.Color.IntegerToColor(color).rgba,
            fontStyle: 'bold', stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(100);
        this.tweens.add({
            targets: t, y: caster.container.y - 80, alpha: 0,
            duration: 1200, ease: 'Power2', onComplete: () => t.destroy()
        });
    }

    showHealEffect(target, amount) {
        DamagePopup.show(this, target.container.x, target.container.y - 20, amount, 0x44ff88, true);
    }

    onBattleEnd(victory) {
        this.battleEnded = true;

        const stats = this.combat.getStats();
        let plunderGold = 0;
        stats.forEach(s => { plunderGold += s.plunderBonus || 0; });

        if (victory) {
            this.runState.party = this.runState.party.filter(unit => {
                const battleChar = this.allyChars.find(c => c.unitRef === unit);
                if (battleChar && battleChar.alive) {
                    unit.applyBattleResult(battleChar);
                    return true;
                }
                return false;
            });

            const node = this.mapManager.getNode(this.runState.currentNode);
            const goldReward = (node ? node.gold : 0) + plunderGold;
            this.runState.gold += goldReward;
            this.runState.totalGoldEarned += goldReward;
            this.runState.totalBattles++;

            const msg = `승 리 !  +${goldReward}G`;
            const endText = this.add.text(640, 280, msg, {
                fontSize: '36px', fontFamily: 'monospace',
                color: '#44ff88', fontStyle: 'bold',
                stroke: '#000000', strokeThickness: 6
            }).setOrigin(0.5).setDepth(200).setAlpha(0);

            this.tweens.add({
                targets: endText, alpha: 1, scaleX: 1.2, scaleY: 1.2,
                duration: 500
            });

            const isBoss = node && node.type === 'boss';
            this.time.delayedCall(2500, () => {
                if (isBoss) {
                    this.scene.start('ResultScene', { runState: this.runState, victory: true });
                } else {
                    this.runState.currentNode++;
                    this.scene.start('MapScene', { runState: this.runState });
                }
            });
        } else {
            const endText = this.add.text(640, 300, '전 멸...', {
                fontSize: '40px', fontFamily: 'monospace',
                color: '#ff4444', fontStyle: 'bold',
                stroke: '#000000', strokeThickness: 6
            }).setOrigin(0.5).setDepth(200).setAlpha(0);

            this.tweens.add({
                targets: endText, alpha: 1, scaleX: 1.2, scaleY: 1.2,
                duration: 500
            });

            this.time.delayedCall(2500, () => {
                this.runState.gameOver = true;
                this.scene.start('ResultScene', { runState: this.runState, victory: false });
            });
        }
    }
}
