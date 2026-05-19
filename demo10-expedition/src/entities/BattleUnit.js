class BattleUnit {
    constructor(scene, data, gridX, gridY, isPlayer) {
        this.scene = scene;
        this.data = { ...data };
        this.gridX = gridX;
        this.gridY = gridY;
        this.isPlayer = isPlayer;
        this.hp = data.hp;
        this.maxHp = data.maxHp;
        this.ap = data.ap;
        this.maxAp = data.ap;
        this.alive = true;
        this.buffs = [];
        this.overwatch = false;

        this.container = scene.add.container(0, 0);
        this.draw();
        this.updatePosition();
    }

    draw() {
        this.container.removeAll(true);
        const cellSize = this.scene.CELL_SIZE || 64;
        const s = Math.floor(cellSize * 0.42);

        const body = this.scene.add.graphics();
        const c = this.data.color;
        body.fillStyle(c, 1);
        body.fillRoundedRect(-s / 2, -s / 2, s, s, 4);
        body.lineStyle(2, this.isPlayer ? 0xffffff : 0xff4444, 0.6);
        body.strokeRoundedRect(-s / 2, -s / 2, s, s, 4);

        const shadow = this.scene.add.graphics();
        shadow.fillStyle(0x000000, 0.3);
        shadow.fillEllipse(0, s / 2 + 2, s - 4, 6);

        const nameText = this.scene.add.text(0, -s / 2 - 14, this.data.name, {
            fontSize: '10px', fontFamily: 'monospace', color: '#ffffff',
            stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5);

        const barW = Math.floor(s * 0.95);
        const hpBarBg = this.scene.add.graphics();
        hpBarBg.fillStyle(0x333333, 0.8);
        hpBarBg.fillRect(-barW / 2, s / 2 + 4, barW, 4);

        this.hpBar = this.scene.add.graphics();
        this.hpBarWidth = barW;
        this.updateHpBar();

        this.apPips = this.scene.add.graphics();
        this.drawApPips();

        this.container.add([shadow, body, hpBarBg, this.hpBar, nameText, this.apPips]);
    }

    drawApPips() {
        if (!this.apPips) return;
        this.apPips.clear();
        const cellSize = this.scene.CELL_SIZE || 64;
        const s = Math.floor(cellSize * 0.42);
        const pipSize = Math.max(4, Math.floor(cellSize * 0.07));
        const pipGap = Math.floor(pipSize * 0.5);
        const totalW = this.maxAp * pipSize + (this.maxAp - 1) * pipGap;
        const startX = -totalW / 2;
        const py = s / 2 + 11;

        for (let i = 0; i < this.maxAp; i++) {
            const px = startX + i * (pipSize + pipGap);
            if (i < this.ap) {
                this.apPips.fillStyle(0xffcc44, 1);
                this.apPips.fillRoundedRect(px, py, pipSize, pipSize, 1);
                this.apPips.lineStyle(1, 0xffee88, 0.6);
                this.apPips.strokeRoundedRect(px, py, pipSize, pipSize, 1);
            } else {
                this.apPips.fillStyle(0x333333, 0.6);
                this.apPips.fillRoundedRect(px, py, pipSize, pipSize, 1);
                this.apPips.lineStyle(1, 0x555555, 0.3);
                this.apPips.strokeRoundedRect(px, py, pipSize, pipSize, 1);
            }
        }
    }

    updateHpBar() {
        if (!this.hpBar) return;
        this.hpBar.clear();
        const ratio = Math.max(0, this.hp / this.maxHp);
        const color = ratio > 0.5 ? 0x44ff44 : ratio > 0.25 ? 0xffcc44 : 0xff4444;
        const cellSize = this.scene.CELL_SIZE || 64;
        const s = Math.floor(cellSize * 0.42);
        const barW = this.hpBarWidth || 28;
        this.hpBar.fillStyle(color, 1);
        this.hpBar.fillRect(-barW / 2, s / 2 + 4, barW * ratio, 4);
    }

    updatePosition() {
        const pos = this.scene.gridToWorld(this.gridX, this.gridY);
        this.container.setPosition(pos.x, pos.y);
        this.container.setDepth(this.gridY * 10 + 5);
    }

    animateMoveTo(gx, gy, onComplete) {
        this.gridX = gx;
        this.gridY = gy;
        const pos = this.scene.gridToWorld(gx, gy);
        this.scene.tweens.add({
            targets: this.container,
            x: pos.x, y: pos.y,
            duration: 200,
            ease: 'Power2',
            onComplete: () => {
                this.container.setDepth(gy * 10 + 5);
                if (onComplete) onComplete();
            }
        });
    }

    takeDamage(amount) {
        const def = this.getEffectiveDef();
        const dmg = Math.max(1, amount - def);
        this.hp = Math.max(0, this.hp - dmg);
        this.updateHpBar();

        this.scene.tweens.add({
            targets: this.container,
            x: this.container.x + (this.isPlayer ? -5 : 5),
            duration: 50, yoyo: true, repeat: 2
        });

        this.scene.showDamage(this.container.x, this.container.y - 20, dmg);

        if (this.hp <= 0) this.die();
        return dmg;
    }

    heal(amount) {
        const old = this.hp;
        this.hp = Math.min(this.maxHp, this.hp + amount);
        this.updateHpBar();
        this.scene.showHeal(this.container.x, this.container.y - 20, this.hp - old);
    }

    getEffectiveDef() {
        let d = this.data.def;
        this.buffs.forEach(b => { if (b.effect === 'defUp') d += b.value; });
        return d;
    }

    useAP(cost) {
        this.ap = Math.max(0, this.ap - cost);
        this.drawApPips();
    }

    resetAP() {
        this.ap = this.maxAp;
        this.drawApPips();
        this.buffs = this.buffs.filter(b => {
            if (b.duration !== undefined) {
                b.duration--;
                return b.duration > 0;
            }
            return false;
        });
    }

    addBuff(buff) {
        this.buffs.push({ ...buff });
    }

    die() {
        this.alive = false;
        this.scene.tweens.add({
            targets: this.container,
            alpha: 0, scaleX: 0.5, scaleY: 0.5,
            duration: 300,
            onComplete: () => this.container.setVisible(false)
        });
    }

    setHighlight(on) {
        if (on) {
            this.container.setScale(1.1);
        } else {
            this.container.setScale(1.0);
        }
    }

    destroy() {
        this.container.destroy();
    }
}
