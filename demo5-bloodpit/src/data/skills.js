const BP_SKILLS = {
    warrior: {
        name: '방패 강타', cooldown: 8000, desc: '모든 적 방어력 30% 감소 (5초)',
        execute(caster, enemies, scene) {
            enemies.forEach(e => {
                if (!e.alive) return;
                e.defReduction = (e.defReduction || 0) + 0.3;
                scene.time.delayedCall(5000, () => { e.defReduction = Math.max(0, (e.defReduction || 0) - 0.3); });
            });
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '방어↓', 0x4488ff, false);
        }
    },
    rogue: {
        name: '급소 공격', cooldown: 6000, desc: '가장 체력 낮은 적에게 ATK×3 크리티컬 공격',
        execute(caster, enemies, scene) {
            const target = enemies.filter(e => e.alive).sort((a, b) => a.hp - b.hp)[0];
            if (!target) return;
            const dmg = Math.floor(caster.atk * 3);
            target.takeDamage(dmg, caster);
            DamagePopup.showCritical(scene, target.container.x, target.container.y - 20, dmg);
        }
    },
    priest: {
        name: '전체 회복', cooldown: 10000, desc: '모든 아군 HP 25% 회복',
        execute(caster, allies, scene) {
            allies.forEach(a => {
                if (!a.alive) return;
                const heal = Math.floor(a.maxHp * 0.25);
                a.hp = Math.min(a.maxHp, a.hp + heal);
                DamagePopup.show(scene, a.container.x, a.container.y - 30, heal, 0x44ff88, true);
            });
        }
    },
    mage: {
        name: '메테오', cooldown: 12000, desc: '모든 적에게 ATK×2 범위 피해',
        execute(caster, enemies, scene) {
            const dmg = Math.floor(caster.atk * 2);
            enemies.forEach(e => {
                if (!e.alive) return;
                e.takeDamage(dmg, caster);
                DamagePopup.show(scene, e.container.x, e.container.y - 20, dmg, 0xaa44ff, false);
            });
        }
    },
    ranger: {
        name: '화살비', cooldown: 9000, desc: '랜덤 적 3명에게 ATK×1.5 피해',
        execute(caster, enemies, scene) {
            const alive = enemies.filter(e => e.alive);
            const targets = Phaser.Utils.Array.Shuffle(alive).slice(0, 3);
            targets.forEach(t => {
                const dmg = Math.floor(caster.atk * 1.5);
                t.takeDamage(dmg, caster);
                DamagePopup.show(scene, t.container.x, t.container.y - 20, dmg, 0x88cc44, false);
                const arrow = scene.add.triangle(caster.container.x, caster.container.y - 10, 0, 6, 3, 0, 6, 6, 0x88cc44).setDepth(50);
                scene.tweens.add({ targets: arrow, x: t.container.x, y: t.container.y - 10, duration: 200, onComplete: () => arrow.destroy() });
            });
        }
    },
    paladin: {
        name: '신성 보호막', cooldown: 12000, desc: '모든 아군 DEF +8 (6초) + HP 10% 회복',
        execute(caster, allies, scene) {
            allies.forEach(a => {
                if (!a.alive) return;
                a.def += 8;
                scene.time.delayedCall(6000, () => { a.def = Math.max(0, a.def - 8); });
                const heal = Math.floor(a.maxHp * 0.10);
                a.hp = Math.min(a.maxHp, a.hp + heal);
                DamagePopup.show(scene, a.container.x, a.container.y - 30, '보호막', 0xffdd44, true);
            });
        }
    },
    assassin: {
        name: '암살', cooldown: 5000, desc: '가장 체력 낮은 적에게 ATK×4, HP 20%이하 즉사',
        execute(caster, enemies, scene) {
            const target = enemies.filter(e => e.alive).sort((a, b) => a.hp - b.hp)[0];
            if (!target) return;
            const hpRatio = target.hp / target.maxHp;
            if (hpRatio <= 0.2) {
                const dmg = target.hp;
                target.takeDamage(dmg, caster);
                DamagePopup.show(scene, target.container.x, target.container.y - 30, '즉사!', 0x8844aa, false);
            } else {
                const dmg = Math.floor(caster.atk * 4);
                target.takeDamage(dmg, caster);
                DamagePopup.showCritical(scene, target.container.x, target.container.y - 20, dmg);
            }
        }
    },
    shaman: {
        name: '저주', cooldown: 10000, desc: '모든 적 공속 -30% (5초) + 출혈',
        execute(caster, enemies, scene) {
            enemies.forEach(e => {
                if (!e.alive) return;
                const origSpd = e.attackSpeed;
                e.attackSpeed = Math.floor(e.attackSpeed * 1.3);
                scene.time.delayedCall(5000, () => { e.attackSpeed = origSpd; });
                if (!e.bleeding) {
                    e.bleeding = true;
                    e.bleedTimer = 5000;
                    e.bleedTickTimer = 1000;
                    e.bleedDamage = Math.max(1, Math.floor(caster.atk * 0.3));
                }
                DamagePopup.show(scene, e.container.x, e.container.y - 20, '저주!', 0x44aaaa, false);
            });
        }
    },

    // --- Advanced Class Skills ---
    berserker: {
        name: '광폭', cooldown: 7000, desc: 'ATK +60%, 공속 -30% (6초) + 잃은 HP% 비례 추가 데미지',
        execute(caster, enemies, scene) {
            const hpRatio = 1 - (caster.hp / caster.maxHp);
            const bonusMult = 0.6 + hpRatio * 0.4;
            const origAtk = caster.atk;
            const origSpd = caster.attackSpeed;
            caster.atk = Math.floor(caster.atk * (1 + bonusMult));
            caster.attackSpeed = Math.floor(caster.attackSpeed * 0.7);
            scene.time.delayedCall(6000, () => { caster.atk = origAtk; caster.attackSpeed = origSpd; });
            // lifesteal burst: heal 15% of max HP
            const heal = Math.floor(caster.maxHp * 0.15);
            caster.hp = Math.min(caster.maxHp, caster.hp + heal);
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '광폭!', 0x6699ff, true);
        }
    },
    blade_master: {
        name: '만검귀환', cooldown: 6000, desc: '모든 적에게 ATK×1.5, 50% 확률로 크리티컬',
        execute(caster, enemies, scene) {
            enemies.forEach(e => {
                if (!e.alive) return;
                const isCrit = Math.random() < 0.5;
                const dmg = Math.floor(caster.atk * (isCrit ? 1.5 * caster.critDmg : 1.5));
                e.takeDamage(dmg, caster);
                if (isCrit) {
                    DamagePopup.showCritical(scene, e.container.x, e.container.y - 20, dmg);
                } else {
                    DamagePopup.show(scene, e.container.x, e.container.y - 20, dmg, 0xff6666, false);
                }
            });
        }
    },
    bishop: {
        name: '성스러운 빛', cooldown: 9000, desc: '모든 아군 HP 35% 회복 + DEF +5 (5초) + 사망 아군 1명 부활(30%HP)',
        execute(caster, allies, scene) {
            let revived = false;
            allies.forEach(a => {
                if (a.alive) {
                    const heal = Math.floor(a.maxHp * 0.35);
                    a.hp = Math.min(a.maxHp, a.hp + heal);
                    a.def += 5;
                    scene.time.delayedCall(5000, () => { a.def = Math.max(0, a.def - 5); });
                    DamagePopup.show(scene, a.container.x, a.container.y - 30, heal, 0x66ffaa, true);
                } else if (!revived) {
                    a.alive = true;
                    a.hp = Math.floor(a.maxHp * 0.3);
                    revived = true;
                    DamagePopup.show(scene, a.container.x, a.container.y - 30, '부활!', 0xffff44, true);
                }
            });
        }
    },
    archmage: {
        name: '멸절의 화염', cooldown: 11000, desc: '모든 적에게 ATK×3 + 3초간 화상(ATK×0.5/초)',
        execute(caster, enemies, scene) {
            const dmg = Math.floor(caster.atk * 3);
            enemies.forEach(e => {
                if (!e.alive) return;
                e.takeDamage(dmg, caster);
                DamagePopup.show(scene, e.container.x, e.container.y - 20, dmg, 0xcc66ff, false);
                // burn DoT
                if (!e.bleeding) {
                    e.bleeding = true;
                    e.bleedTimer = 3000;
                    e.bleedTickTimer = 1000;
                    e.bleedDamage = Math.max(1, Math.floor(caster.atk * 0.5));
                }
            });
        }
    },
    sniper: {
        name: '관통 사격', cooldown: 8000, desc: 'HP 가장 높은 적에게 ATK×5 관통(DEF 무시) + 100% 크리티컬',
        execute(caster, enemies, scene) {
            const target = enemies.filter(e => e.alive).sort((a, b) => b.hp - a.hp)[0];
            if (!target) return;
            const dmg = Math.floor(caster.atk * 5 * caster.critDmg);
            // bypass def: direct hp reduction
            target.hp -= dmg;
            if (target.hp <= 0) { target.hp = 0; target.alive = false; }
            DamagePopup.showCritical(scene, target.container.x, target.container.y - 20, dmg);
            // visual: line from caster to target
            const line = scene.add.line(0, 0, caster.container.x, caster.container.y - 10, target.container.x, target.container.y - 10, 0xaaee55, 0.8).setDepth(50).setOrigin(0, 0);
            scene.tweens.add({ targets: line, alpha: 0, duration: 400, onComplete: () => line.destroy() });
        }
    },
    crusader: {
        name: '성전 선포', cooldown: 11000, desc: '모든 아군 ATK +30%, DEF +10 (7초) + 자신 HP 20% 회복',
        execute(caster, allies, scene) {
            allies.forEach(a => {
                if (!a.alive) return;
                const origAtk = a.atk;
                a.atk = Math.floor(a.atk * 1.3);
                a.def += 10;
                scene.time.delayedCall(7000, () => { a.atk = origAtk; a.def = Math.max(0, a.def - 10); });
                DamagePopup.show(scene, a.container.x, a.container.y - 30, '성전!', 0xffee66, true);
            });
            const heal = Math.floor(caster.maxHp * 0.2);
            caster.hp = Math.min(caster.maxHp, caster.hp + heal);
        }
    },
    phantom: {
        name: '그림자 난무', cooldown: 4500, desc: '랜덤 적 5회 ATK×2 공격 (각각 크리티컬 판정)',
        execute(caster, enemies, scene) {
            const alive = enemies.filter(e => e.alive);
            if (alive.length === 0) return;
            for (let i = 0; i < 5; i++) {
                const target = alive[Math.floor(Math.random() * alive.length)];
                if (!target || !target.alive) continue;
                const isCrit = Math.random() < caster.critRate;
                const dmg = Math.floor(caster.atk * 2 * (isCrit ? caster.critDmg : 1));
                target.takeDamage(dmg, caster);
                if (isCrit) {
                    DamagePopup.showCritical(scene, target.container.x + (Math.random() - 0.5) * 20, target.container.y - 20 - i * 8, dmg);
                } else {
                    DamagePopup.show(scene, target.container.x + (Math.random() - 0.5) * 20, target.container.y - 20 - i * 8, dmg, 0xaa66cc, false);
                }
            }
        }
    },
    witch_doctor: {
        name: '역병', cooldown: 9000, desc: '모든 적에게 맹독(ATK×0.4/초, 6초) + DEF -40% (6초)',
        execute(caster, enemies, scene) {
            enemies.forEach(e => {
                if (!e.alive) return;
                // poison DoT
                e.bleeding = true;
                e.bleedTimer = 6000;
                e.bleedTickTimer = 1000;
                e.bleedDamage = Math.max(1, Math.floor(caster.atk * 0.4));
                // def reduction
                e.defReduction = (e.defReduction || 0) + 0.4;
                scene.time.delayedCall(6000, () => { e.defReduction = Math.max(0, (e.defReduction || 0) - 0.4); });
                DamagePopup.show(scene, e.container.x, e.container.y - 20, '역병!', 0x66cccc, false);
            });
        }
    }
};
