class ATBCharacter {
    constructor(scene, data, key, team, x, y) {
        this.scene = scene;
        this.key = key;
        this.name = data.name;
        this.team = team;
        this.alive = true;

        this.maxHp = data.hp;
        this.hp = data.hp;
        this.atk = data.atk;
        this.def = data.def;
        this.speed = data.speed;
        this.critRate = data.critRate;
        this.critDmg = data.critDmg;
        this.color = data.color;
        this.role = data.role;

        this.gauge = 0;
        this.maxGauge = 1000;
        this.turnCount = 0;
        this.totalDamageDealt = 0;
        this.stunTurns = 0;
        this.statusEffects = [];
        this.forcedTarget = null;
        this.forcedTargetTurns = 0;
        this.currentTarget = null;

        this.skills = [];
        this.skillCooldowns = {};

        this.container = scene.add.container(x, y).setDepth(5);
        this.gfx = scene.add.graphics();
        this.container.add(this.gfx);
        this.drawCharacter(false);

        this.healthBar = new HealthBar(scene, x, y - 35, 44, 5, this.maxHp);
        this.gaugeBar = new GaugeBar(scene, x, y - 28, 44, 3, 0xffcc00);

        this.nameText = scene.add.text(x, y - 46, data.name, {
            fontSize: '10px', fontFamily: 'monospace', color: '#ffffff',
            stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5).setDepth(15);

        this.turnIndicator = scene.add.text(x, y + 22, '', {
            fontSize: '9px', fontFamily: 'monospace', color: '#ffcc00',
            stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5).setDepth(15);
    }

    assignSkills(skillKeys) {
        this.skills = skillKeys.map(k => ATB_SKILLS[k]).filter(Boolean);
        this.skills.forEach(s => { this.skillCooldowns[s.name] = 0; });
    }

    drawCharacter(acting) {
        this.gfx.clear();
        const flip = this.team === 'enemy' ? -1 : 1;
        const r = (this.color >> 16) & 0xff;
        const g = (this.color >> 8) & 0xff;
        const b = this.color & 0xff;
        const dark = Phaser.Display.Color.GetColor(Math.floor(r*0.6), Math.floor(g*0.6), Math.floor(b*0.6));
        const light = Phaser.Display.Color.GetColor(Math.min(255,r+60), Math.min(255,g+60), Math.min(255,b+60));

        this.gfx.fillStyle(0x000000, 0.3);
        this.gfx.fillEllipse(0, 14, 20, 6);
        this.gfx.fillStyle(this.color);
        this.gfx.fillRect(-7, -4, 14, 14);
        this.gfx.fillStyle(dark);
        this.gfx.fillRect(-7, -4, 14, 3);
        this.gfx.fillStyle(0xffcc99);
        this.gfx.fillRect(-4, -14, 8, 8);
        this.gfx.fillStyle(0xffffff);
        this.gfx.fillRect(flip * 1, -12, 3, 3);
        this.gfx.fillStyle(0x333333);
        this.gfx.fillRect(flip * 2, -12, 2, 2);
        this.gfx.fillStyle(dark);
        this.gfx.fillRect(-5, -16, 10, 3);
        this.gfx.fillRect(-4, 10, 3, 5);
        this.gfx.fillRect(2, 10, 3, 5);
        this.gfx.fillStyle(0x664422);
        this.gfx.fillRect(-4, 14, 4, 2);
        this.gfx.fillRect(2, 14, 4, 2);

        if (acting) {
            this.gfx.fillStyle(light);
            this.gfx.fillRect(flip * 10, -8, 8 * flip, 3);
        } else {
            this.gfx.fillStyle(0xaaaaaa);
            if (this.role === 'tank') { this.gfx.fillRect(-flip * 9, -4, 3, 12); }
            else if (this.role === 'healer' || this.key === 'mage') {
                this.gfx.fillRect(flip * 8, -10, 2, 16);
                this.gfx.fillStyle(light);
                this.gfx.fillEllipse(flip * 9, -12, 6, 6);
            } else { this.gfx.fillRect(flip * 8, -2, 6 * flip, 2); }
        }

        if (this.stunTurns > 0) {
            this.gfx.fillStyle(0xffcc00, 0.6);
            this.gfx.fillCircle(0, -20, 4);
        }
    }

    updateUI() {
        this.healthBar.update(this.hp);
        this.healthBar.setPosition(this.container.x, this.container.y - 35);
        this.gaugeBar.update(this.gauge / this.maxGauge);
        this.gaugeBar.setPosition(this.container.x, this.container.y - 28);
        this.nameText.setPosition(this.container.x, this.container.y - 46);
        this.turnIndicator.setPosition(this.container.x, this.container.y + 22);
    }

    findTarget(all) {
        if (this.forcedTarget && this.forcedTarget.alive) return this.forcedTarget;
        const enemies = all.filter(c => c.team !== this.team && c.alive);
        if (!enemies.length) return null;
        return enemies.reduce((a, b) => Math.abs(this.container.x - a.container.x) < Math.abs(this.container.x - b.container.x) ? a : b);
    }

    takeDamage(amount) {
        this.hp = Math.max(0, this.hp - amount);
        if (this.hp <= 0) this.die();
    }

    die() {
        this.alive = false;
        this.scene.tweens.add({ targets: this.container, alpha: 0, y: this.container.y + 10, duration: 500 });
        this.healthBar.destroy();
        this.gaugeBar.destroy();
        this.nameText.destroy();
        this.turnIndicator.destroy();
    }

    destroy() {
        if (this.container) this.container.destroy();
        if (this.healthBar) this.healthBar.destroy();
        if (this.gaugeBar) this.gaugeBar.destroy();
        if (this.nameText) this.nameText.destroy();
        if (this.turnIndicator) this.turnIndicator.destroy();
    }
}
