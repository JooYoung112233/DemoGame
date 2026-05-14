class BattleScene extends Phaser.Scene {
    constructor() {
        super({ key: 'BattleScene' });
    }

    init(data) {
        this.partyConfig = data.party || ['tank', 'rogue', 'priest', 'mage'];
        this.enemyConfig = data.enemies || DEFAULT_ENEMIES;
        this.gameSpeed = data.speed || 1.5;
    }

    create() {
        this.createBackground();

        this.combat = new CombatSystem(this);
        this.battleTime = 0;
        this.battleEnded = false;

        this.spawnAllies();
        this.spawnEnemies();
        this.createUI();
    }

    createBackground() {
        const g = this.add.graphics();

        for (let i = 0; i < 360; i++) {
            const ratio = i / 360;
            const r = Math.floor(30 + ratio * 20);
            const gr = Math.floor(30 + ratio * 30);
            const b = Math.floor(60 + ratio * 40);
            g.fillStyle(Phaser.Display.Color.GetColor(r, gr, b));
            g.fillRect(0, i, 1280, 1);
        }

        g.fillStyle(0x2a3a2a);
        g.fillRect(0, 460, 1280, 260);

        g.fillStyle(0x3a4a3a);
        for (let i = 0; i < 50; i++) {
            const x = Math.random() * 1280;
            const y = 460 + Math.random() * 260;
            g.fillRect(x, y, 2 + Math.random() * 4, 1);
        }

        g.lineStyle(1, 0x555555, 0.3);
        g.lineBetween(640, 400, 640, 500);

        g.lineStyle(2, 0x4a5a4a);
        g.lineBetween(0, 460, 1280, 460);

        this.add.text(200, 470, 'ALLY', {
            fontSize: '12px', fontFamily: 'monospace', color: '#335533', alpha: 0.5
        });
        this.add.text(1020, 470, 'ENEMY', {
            fontSize: '12px', fontFamily: 'monospace', color: '#553333', alpha: 0.5
        });
    }

    spawnAllies() {
        const startX = 180;
        const spacing = 80;
        const y = 440;

        this.partyConfig.forEach((charKey, i) => {
            const data = CHAR_DATA[charKey];
            if (!data) return;
            const x = startX + i * spacing;
            const char = new Character(this, data, 'ally', x, y);
            const skills = SkillSystem.getSkillsForCharacter(charKey);
            char.assignSkills(skills);
            this.combat.addCharacter(char);
        });
    }

    spawnEnemies() {
        const startX = 1100;
        const spacing = 80;
        const y = 440;

        this.enemyConfig.forEach((charKey, i) => {
            const data = CHAR_DATA[charKey];
            if (!data) return;
            const x = startX - i * spacing;
            const char = new Character(this, data, 'enemy', x, y);
            this.combat.addCharacter(char);
        });
    }

    createUI() {
        this.timeText = this.add.text(640, 20, '00:00', {
            fontSize: '18px',
            fontFamily: 'monospace',
            color: '#ffffff',
            stroke: '#000000',
            strokeThickness: 3
        }).setOrigin(0.5).setDepth(20);

        this.speedText = this.add.text(1220, 20, 'x' + this.gameSpeed, {
            fontSize: '14px',
            fontFamily: 'monospace',
            color: '#ffcc00',
            stroke: '#000000',
            strokeThickness: 2
        }).setOrigin(0.5).setDepth(20);

        const speedBtn = this.add.rectangle(1260, 20, 40, 24, 0x333355)
            .setStrokeStyle(1, 0x5566aa)
            .setInteractive({ useHandCursor: true })
            .setDepth(20);
        this.add.text(1260, 20, 'SPD', {
            fontSize: '10px', fontFamily: 'monospace', color: '#aaaacc'
        }).setOrigin(0.5).setDepth(21);

        speedBtn.on('pointerdown', () => {
            if (this.gameSpeed === 1) this.gameSpeed = 1.5;
            else if (this.gameSpeed === 1.5) this.gameSpeed = 2;
            else this.gameSpeed = 1;
            this.speedText.setText('x' + this.gameSpeed);
        });

        this.allyCountText = this.add.text(20, 20, '', {
            fontSize: '14px',
            fontFamily: 'monospace',
            color: '#44ff88',
            stroke: '#000000',
            strokeThickness: 2
        }).setDepth(20);

        this.enemyCountText = this.add.text(1260, 50, '', {
            fontSize: '14px',
            fontFamily: 'monospace',
            color: '#ff6644',
            stroke: '#000000',
            strokeThickness: 2
        }).setOrigin(1, 0).setDepth(20);
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
        this.allyCountText.setText('ALLY: ' + allyAlive + '/' + this.partyConfig.length);
        this.enemyCountText.setText('ENEMY: ' + enemyAlive + '/' + this.enemyConfig.length);
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

    showKnockback(target, attacker) {
        if (!target.alive || !attacker) return;
        const dir = target.container.x > attacker.container.x ? 1 : -1;
        this.tweens.add({
            targets: target.container,
            x: target.container.x + dir * 6,
            duration: 60,
            yoyo: true,
            ease: 'Power1'
        });
    }

    showAttackEffect(fromX, fromY, toX, toY) {
        const spark = this.add.circle(toX, toY - 5, 6, 0xffffff, 0.8).setDepth(50);
        this.tweens.add({
            targets: spark,
            alpha: 0,
            scaleX: 2,
            scaleY: 2,
            duration: 200,
            onComplete: () => spark.destroy()
        });
    }

    showProjectile(fromX, fromY, toX, toY, color) {
        const proj = this.add.circle(fromX, fromY - 10, 4, color, 1).setDepth(50);
        this.tweens.add({
            targets: proj,
            x: toX,
            y: toY - 10,
            duration: 300,
            ease: 'Power1',
            onComplete: () => proj.destroy()
        });
    }

    showAoeEffect(cx, cy, radius) {
        const circle = this.add.circle(cx, cy, radius, 0xaa44ff, 0.3).setDepth(50);
        this.tweens.add({
            targets: circle,
            alpha: 0,
            scaleX: 1.5,
            scaleY: 1.5,
            duration: 600,
            onComplete: () => circle.destroy()
        });

        for (let i = 0; i < 8; i++) {
            const px = cx + (Math.random() - 0.5) * radius;
            const py = cy + (Math.random() - 0.5) * 30;
            const p = this.add.circle(px, py, 3, 0xff8844, 1).setDepth(51);
            this.tweens.add({
                targets: p,
                y: py - 30 - Math.random() * 20,
                alpha: 0,
                duration: 400 + Math.random() * 300,
                onComplete: () => p.destroy()
            });
        }
    }

    showSkillText(caster, text, color) {
        const t = this.add.text(caster.container.x, caster.container.y - 50, text, {
            fontSize: '14px',
            fontFamily: 'monospace',
            color: Phaser.Display.Color.IntegerToColor(color).rgba,
            fontStyle: 'bold',
            stroke: '#000000',
            strokeThickness: 3
        }).setOrigin(0.5).setDepth(100);

        this.tweens.add({
            targets: t,
            y: caster.container.y - 80,
            alpha: 0,
            duration: 1200,
            ease: 'Power2',
            onComplete: () => t.destroy()
        });
    }

    showDamagePopup(x, y, amount, color, isHeal) {
        DamagePopup.show(this, x, y, amount, color, isHeal);
    }

    onBattleEnd(victory) {
        this.battleEnded = true;

        const msg = victory ? '승 리 !' : '패 배...';
        const color = victory ? '#44ff88' : '#ff4444';

        const endText = this.add.text(640, 300, msg, {
            fontSize: '40px',
            fontFamily: 'monospace',
            color: color,
            fontStyle: 'bold',
            stroke: '#000000',
            strokeThickness: 6
        }).setOrigin(0.5).setDepth(200).setAlpha(0);

        this.tweens.add({
            targets: endText,
            alpha: 1,
            scaleX: 1.2,
            scaleY: 1.2,
            duration: 500,
            yoyo: false
        });

        this.time.delayedCall(2000, () => {
            this.scene.start('ResultScene', {
                victory: victory,
                stats: this.combat.getStats(),
                battleTime: this.battleTime,
                party: this.partyConfig,
                enemies: this.enemyConfig
            });
        });
    }
}
