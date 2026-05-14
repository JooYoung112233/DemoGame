const BO_SKILLS = {
    scout: {
        name: '섬광 돌진', cooldown: 5000,
        execute(caster, targets, scene) {
            const t = targets.filter(e => e.alive)[0];
            if (!t) return;
            const origX = caster.container.x;
            const dmg = Math.floor(caster.atk * 2);
            scene.tweens.add({
                targets: caster.container, x: t.container.x - 20, duration: 100,
                onComplete: () => {
                    t.takeDamage(dmg, caster);
                    DamagePopup.showCritical(scene, t.container.x, t.container.y - 20, dmg);
                    const flash = scene.add.circle(t.container.x, t.container.y, 8, 0xffffff, 0.9).setDepth(50);
                    scene.tweens.add({ targets: flash, alpha: 0, scaleX: 3, scaleY: 3, duration: 200, onComplete: () => flash.destroy() });
                    scene.tweens.add({ targets: caster.container, x: origX, duration: 150 });
                }
            });
        }
    },
    breacher: {
        name: '방패 타격', cooldown: 7000,
        execute(caster, targets, scene) {
            const t = targets.filter(e => e.alive)[0];
            if (!t) return;
            const dmg = Math.max(1, caster.atk - t.def);
            t.takeDamage(dmg, caster);
            t.stunTimer = 1500;
            DamagePopup.show(scene, t.container.x, t.container.y - 30, '스턴!', 0xffcc00, false);
        }
    },
    hacker: {
        name: '시스템 해킹', cooldown: 6000,
        execute(caster, targets, scene) {
            targets.forEach(e => {
                if (!e.alive) return;
                const origAtk = e.atk;
                e.atk = Math.floor(e.atk * 0.8);
                scene.time.delayedCall(4000, () => { if (e.alive) e.atk = origAtk; });
            });
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '해킹!', 0x44ffaa, false);
            const wave = scene.add.circle(caster.container.x, caster.container.y, 6, 0x44ffaa, 0.6).setDepth(50);
            scene.tweens.add({ targets: wave, scaleX: 6, scaleY: 6, alpha: 0, duration: 400, onComplete: () => wave.destroy() });
        }
    },
    medic: {
        name: '나노 자극', cooldown: 10000,
        execute(caster, allies, scene) {
            const wounded = allies.filter(a => a.alive && a !== caster);
            if (!wounded.length) return;
            const t = wounded.reduce((a, b) => a.hp / a.maxHp < b.hp / b.maxHp ? a : b);
            const heal = Math.floor(t.maxHp * 0.25);
            t.hp = Math.min(t.maxHp, t.hp + heal);
            DamagePopup.show(scene, t.container.x, t.container.y - 30, heal, 0x44ff88, true);
            const origSpd = t.attackSpeed;
            t.attackSpeed = Math.floor(t.attackSpeed * 0.7);
            scene.time.delayedCall(5000, () => { if (t.alive) t.attackSpeed = origSpd; });
            DamagePopup.show(scene, t.container.x, t.container.y - 45, '가속!', 0x88ffcc, false);
        }
    }
};
