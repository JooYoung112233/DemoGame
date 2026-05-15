class BPCombatSystem {
    constructor(scene) {
        this.scene = scene;
        this.nestSpawnTimer = 0;
        this.nestSpawnCooldown = 5000;
    }

    update(delta, allies, enemies) {
        const all = [...allies, ...enemies];
        allies.forEach(a => a.update(delta, all));
        enemies.forEach(e => {
            if (e.isNest) {
                this.updateNest(delta, e, enemies, allies);
            }
            e.update(delta, all);
        });
    }

    updateNest(delta, nest, enemies, allies) {
        if (!nest.alive) return;
        this.nestSpawnTimer += delta;
        if (this.nestSpawnTimer >= this.nestSpawnCooldown) {
            this.nestSpawnTimer = 0;
            if (enemies.filter(e => e.alive && !e.isNest).length < 12) {
                const scale = this.scene.dangerSystem ? this.scene.dangerSystem.getEnemyScale() : 1;
                const baseRunner = BP_ENEMIES.runner;
                const runnerData = {
                    ...baseRunner,
                    hp: Math.floor(baseRunner.hp * scale),
                    atk: Math.floor(baseRunner.atk * scale),
                    def: Math.floor(baseRunner.def * scale)
                };
                const x = nest.container.x + Phaser.Math.Between(-30, 30);
                const y = nest.container.y + Phaser.Math.Between(-10, 10);
                const spawned = new BPCharacter(this.scene, runnerData, 'enemy', x, y);
                enemies.push(spawned);

                const flash = this.scene.add.circle(x, y, 15, 0xff4444, 0.6).setDepth(50);
                this.scene.tweens.add({ targets: flash, alpha: 0, scaleX: 2, scaleY: 2, duration: 300, onComplete: () => flash.destroy() });
            }
        }
    }
}
