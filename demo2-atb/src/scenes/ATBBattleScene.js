class ATBBattleScene extends Phaser.Scene {
    constructor() { super({ key: 'ATBBattleScene' }); }

    init(data) {
        this.partyConfig = data.party || ['tank', 'rogue', 'priest', 'mage'];
        this.enemyConfig = data.enemies || ATB_DEFAULT_ENEMIES;
        this.gameSpeed = data.speed || 1.5;
    }

    create() {
        this.createBackground();
        this.atb = new ATBSystem(this);
        this.battleTime = 0;
        this.battleEnded = false;
        this.spawnAllies();
        this.spawnEnemies();
        this.createUI();
        this.createTurnOrderBar();
    }

    createBackground() {
        const g = this.add.graphics();
        for (let i = 0; i < 360; i++) {
            const ratio = i / 360;
            g.fillStyle(Phaser.Display.Color.GetColor(
                Math.floor(25 + ratio * 15), Math.floor(20 + ratio * 25), Math.floor(50 + ratio * 35)));
            g.fillRect(0, i, 1280, 1);
        }
        g.fillStyle(0x2a2a3a);
        g.fillRect(0, 460, 1280, 260);
        g.lineStyle(2, 0x3a3a5a);
        g.lineBetween(0, 460, 1280, 460);
    }

    spawnAllies() {
        const startX = 200, spacing = 90, y = 430;
        this.partyConfig.forEach((key, i) => {
            const data = ATB_CHARS[key];
            if (!data) return;
            const char = new ATBCharacter(this, data, key, 'ally', startX + i * spacing, y);
            const skills = this.getSkillsFor(key);
            char.assignSkills(skills);
            this.atb.addCharacter(char);
        });
    }

    spawnEnemies() {
        const startX = 1080, spacing = 90, y = 430;
        this.enemyConfig.forEach((key, i) => {
            const data = ATB_CHARS[key];
            if (!data) return;
            const char = new ATBCharacter(this, data, key, 'enemy', startX - i * spacing, y);
            this.atb.addCharacter(char);
        });
    }

    getSkillsFor(key) {
        switch (key) {
            case 'tank': return ['taunt', 'stun'];
            case 'rogue': return ['bleed'];
            case 'priest': return ['heal'];
            case 'mage': return ['meteor'];
            default: return [];
        }
    }

    createUI() {
        this.timeText = this.add.text(640, 15, '00:00', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffffff',
            stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(20);

        this.turnText = this.add.text(640, 38, 'Turn: 0', {
            fontSize: '12px', fontFamily: 'monospace', color: '#ffcc00',
            stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5).setDepth(20);

        this.speedText = this.add.text(1220, 15, 'x' + this.gameSpeed, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffcc00',
            stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5).setDepth(20);

        const speedBtn = this.add.rectangle(1260, 15, 40, 24, 0x333355)
            .setStrokeStyle(1, 0x5566aa).setInteractive({ useHandCursor: true }).setDepth(20);
        this.add.text(1260, 15, 'SPD', { fontSize: '10px', fontFamily: 'monospace', color: '#aaaacc' })
            .setOrigin(0.5).setDepth(21);
        speedBtn.on('pointerdown', () => {
            if (this.gameSpeed === 1) this.gameSpeed = 1.5;
            else if (this.gameSpeed === 1.5) this.gameSpeed = 2;
            else if (this.gameSpeed === 2) this.gameSpeed = 3;
            else this.gameSpeed = 1;
            this.speedText.setText('x' + this.gameSpeed);
        });
    }

    createTurnOrderBar() {
        this.turnOrderBg = this.add.rectangle(640, 695, 1240, 40, 0x111122, 0.8).setDepth(20);
        this.add.text(80, 695, '행동 순서:', {
            fontSize: '11px', fontFamily: 'monospace', color: '#888'
        }).setOrigin(0, 0.5).setDepth(21);
        this.turnOrderTexts = [];
    }

    update(time, rawDelta) {
        if (this.battleEnded) return;
        const delta = rawDelta * this.gameSpeed;
        this.battleTime += delta;
        this.atb.update(delta);

        const mins = Math.floor(this.battleTime / 60000);
        const secs = Math.floor((this.battleTime % 60000) / 1000);
        this.timeText.setText(String(mins).padStart(2, '0') + ':' + String(secs).padStart(2, '0'));
        this.turnText.setText('Turn: ' + this.atb.totalTurns);

        this.updateTurnOrder();
    }

    updateTurnOrder() {
        this.turnOrderTexts.forEach(t => t.destroy());
        this.turnOrderTexts = [];

        const alive = this.atb.characters.filter(c => c.alive);
        const sorted = alive.map(c => ({
            name: c.name,
            team: c.team,
            eta: c.speed > 0 ? (c.maxGauge - c.gauge) / c.speed : 999
        })).sort((a, b) => a.eta - b.eta).slice(0, 8);

        sorted.forEach((c, i) => {
            const x = 160 + i * 130;
            const color = c.team === 'ally' ? '#44ff88' : '#ff6644';
            const t = this.add.text(x, 695, c.name, {
                fontSize: '12px', fontFamily: 'monospace', color, fontStyle: 'bold',
                stroke: '#000000', strokeThickness: 2
            }).setOrigin(0.5).setDepth(22);
            this.turnOrderTexts.push(t);
        });
    }

    applyDamage(target, amount, isCrit, attacker) {
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
        const dir = attacker ? (target.container.x > attacker.container.x ? 1 : -1) : 1;
        this.tweens.add({ targets: target.container, x: target.container.x + dir * 6, duration: 60, yoyo: true });
    }

    showSkillText(caster, text, color) {
        const t = this.add.text(caster.container.x, caster.container.y - 55, text, {
            fontSize: '14px', fontFamily: 'monospace',
            color: Phaser.Display.Color.IntegerToColor(color).rgba,
            fontStyle: 'bold', stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(100);
        this.tweens.add({ targets: t, y: caster.container.y - 85, alpha: 0, duration: 1200, onComplete: () => t.destroy() });
    }

    showDmgPopup(x, y, amount, color, isHeal) { DamagePopup.show(this, x, y, amount, color, isHeal); }

    showAoeEffect(cx, cy) {
        const circle = this.add.circle(cx, cy, 100, 0xaa44ff, 0.3).setDepth(50);
        this.tweens.add({ targets: circle, alpha: 0, scaleX: 1.5, scaleY: 1.5, duration: 500, onComplete: () => circle.destroy() });
        for (let i = 0; i < 6; i++) {
            const p = this.add.circle(cx + (Math.random()-0.5)*100, cy + (Math.random()-0.5)*20, 3, 0xff8844).setDepth(51);
            this.tweens.add({ targets: p, y: p.y - 30, alpha: 0, duration: 400 + Math.random()*300, onComplete: () => p.destroy() });
        }
    }

    onBattleEnd(victory) {
        this.battleEnded = true;
        const msg = victory ? '승 리 !' : '패 배...';
        const color = victory ? '#44ff88' : '#ff4444';
        const t = this.add.text(640, 300, msg, {
            fontSize: '40px', fontFamily: 'monospace', color, fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 6
        }).setOrigin(0.5).setDepth(200).setAlpha(0);
        this.tweens.add({ targets: t, alpha: 1, scaleX: 1.2, scaleY: 1.2, duration: 500 });
        this.time.delayedCall(2000, () => {
            this.scene.start('ATBResultScene', {
                victory, stats: this.atb.getStats(), battleTime: this.battleTime,
                totalTurns: this.atb.totalTurns, party: this.partyConfig, enemies: this.enemyConfig
            });
        });
    }
}
