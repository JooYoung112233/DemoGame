const LC_SKILLS = {
    guard: {
        name: '도발', cooldown: 8000, desc: '적 공격을 자신에게 집중',
        execute(caster, enemies, scene) {
            enemies.forEach(e => { if (e.alive) e.target = caster; });
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '도발!', 0x4488ff, false);
        }
    },
    gunner: {
        name: '연사', cooldown: 6000, desc: '3연속 공격',
        execute(caster, enemies, scene) {
            const target = enemies.filter(e => e.alive)[0];
            if (!target) return;
            for (let i = 0; i < 3; i++) {
                scene.time.delayedCall(i * 200, () => {
                    if (!target.alive) return;
                    const dmg = Math.max(1, caster.atk - target.def);
                    target.takeDamage(dmg, caster);
                    DamagePopup.show(scene, target.container.x + i*5, target.container.y - 20 - i*8, dmg, 0xff6644, false);
                });
            }
        }
    },
    medic: {
        name: '응급처치', cooldown: 10000, desc: '아군 전체 HP 20% 회복',
        execute(caster, allies, scene) {
            allies.forEach(a => {
                if (!a.alive) return;
                const heal = Math.floor(a.maxHp * 0.2);
                a.hp = Math.min(a.maxHp, a.hp + heal);
                DamagePopup.show(scene, a.container.x, a.container.y - 30, heal, 0x44ff88, true);
            });
        }
    },
    engineer: {
        name: '긴급 수리', cooldown: 12000, desc: '현재 칸 HP 15% 복구',
        execute(caster, car, scene) {
            if (!car) return;
            const heal = Math.floor(car.maxHp * 0.15);
            car.hp = Math.min(car.maxHp, car.hp + heal);
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '수리+' + heal, 0xffcc44, true);
        }
    }
};
