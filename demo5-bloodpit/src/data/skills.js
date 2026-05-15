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
                e.bleeding = true;
                e.bleedTimer = 6000;
                e.bleedTickTimer = 1000;
                e.bleedDamage = Math.max(1, Math.floor(caster.atk * 0.4));
                e.defReduction = (e.defReduction || 0) + 0.4;
                scene.time.delayedCall(6000, () => { e.defReduction = Math.max(0, (e.defReduction || 0) - 0.4); });
                DamagePopup.show(scene, e.container.x, e.container.y - 20, '역병!', 0x66cccc, false);
            });
        }
    },

    // ─── Tier 2 skills (missing ones) ───
    guardian: {
        name: '철벽 방어', cooldown: 10000, desc: '자신 DEF +20 (8초) + 모든 아군 받는 피해 -20% (5초)',
        execute(caster, allies, scene) {
            caster.def += 20;
            scene.time.delayedCall(8000, () => { caster.def = Math.max(0, caster.def - 20); });
            allies.forEach(a => {
                if (!a.alive) return;
                a.damageReduction = (a.damageReduction || 0) + 0.2;
                scene.time.delayedCall(5000, () => { a.damageReduction = Math.max(0, (a.damageReduction || 0) - 0.2); });
            });
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '철벽!', 0x3366cc, true);
        }
    },
    shadow: {
        name: '그림자 일격', cooldown: 5000, desc: '가장 약한 적에게 ATK×3.5 + 3초 은신(회피+50%)',
        execute(caster, enemies, scene) {
            const target = enemies.filter(e => e.alive).sort((a, b) => a.hp - b.hp)[0];
            if (!target) return;
            const dmg = Math.floor(caster.atk * 3.5);
            target.takeDamage(dmg, caster);
            DamagePopup.showCritical(scene, target.container.x, target.container.y - 20, dmg);
            caster.dodgeRate += 0.5;
            scene.time.delayedCall(3000, () => { caster.dodgeRate = Math.max(0, caster.dodgeRate - 0.5); });
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '은신!', 0xcc3355, true);
        }
    },
    battle_priest: {
        name: '전투 축복', cooldown: 8000, desc: '모든 아군 HP 20% 회복 + ATK +20% (5초)',
        execute(caster, allies, scene) {
            allies.forEach(a => {
                if (!a.alive) return;
                const heal = Math.floor(a.maxHp * 0.2);
                a.hp = Math.min(a.maxHp, a.hp + heal);
                const origAtk = a.atk;
                a.atk = Math.floor(a.atk * 1.2);
                scene.time.delayedCall(5000, () => { a.atk = origAtk; });
                DamagePopup.show(scene, a.container.x, a.container.y - 30, heal, 0x33cc88, true);
            });
        }
    },
    warlock: {
        name: '생명 흡수', cooldown: 9000, desc: '모든 적에게 ATK×2 + 피해량의 30% 자신 회복',
        execute(caster, enemies, scene) {
            let totalDmg = 0;
            enemies.forEach(e => {
                if (!e.alive) return;
                const dmg = Math.floor(caster.atk * 2);
                e.takeDamage(dmg, caster);
                totalDmg += dmg;
                DamagePopup.show(scene, e.container.x, e.container.y - 20, dmg, 0x9933cc, false);
            });
            const heal = Math.floor(totalDmg * 0.3);
            caster.hp = Math.min(caster.maxHp, caster.hp + heal);
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, heal, 0x44ff88, true);
        }
    },
    wind_ranger: {
        name: '질풍 사격', cooldown: 6000, desc: '모든 적에게 ATK×1.2 3연사 + 공속 +30% (4초)',
        execute(caster, enemies, scene) {
            const alive = enemies.filter(e => e.alive);
            for (let volley = 0; volley < 3; volley++) {
                alive.forEach(e => {
                    if (!e.alive) return;
                    const dmg = Math.floor(caster.atk * 1.2);
                    e.takeDamage(dmg, caster);
                    DamagePopup.show(scene, e.container.x + (Math.random()-0.5)*15, e.container.y - 20 - volley*8, dmg, 0x77cc33, false);
                });
            }
            const origSpd = caster.attackSpeed;
            caster.attackSpeed = Math.floor(caster.attackSpeed * 0.7);
            scene.time.delayedCall(4000, () => { caster.attackSpeed = origSpd; });
        }
    },
    templar: {
        name: '심판의 일격', cooldown: 10000, desc: 'HP 가장 높은 적에게 ATK×4 + 모든 아군 DEF +5 (5초)',
        execute(caster, enemies, scene) {
            const target = enemies.filter(e => e.alive).sort((a, b) => b.hp - a.hp)[0];
            if (target) {
                const dmg = Math.floor(caster.atk * 4);
                target.takeDamage(dmg, caster);
                DamagePopup.showCritical(scene, target.container.x, target.container.y - 20, dmg);
            }
            const allies = scene.allies || [];
            allies.forEach(a => {
                if (!a || !a.alive) return;
                a.def += 5;
                scene.time.delayedCall(5000, () => { a.def = Math.max(0, a.def - 5); });
            });
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '심판!', 0xddcc44, true);
        }
    },
    nightblade: {
        name: '독날 연무', cooldown: 5500, desc: '랜덤 적 4회 ATK×1.8 + 각 50% 출혈 부여',
        execute(caster, enemies, scene) {
            const alive = enemies.filter(e => e.alive);
            if (alive.length === 0) return;
            for (let i = 0; i < 4; i++) {
                const target = alive[Math.floor(Math.random() * alive.length)];
                if (!target || !target.alive) continue;
                const dmg = Math.floor(caster.atk * 1.8);
                target.takeDamage(dmg, caster);
                DamagePopup.show(scene, target.container.x + (Math.random()-0.5)*20, target.container.y - 20 - i*8, dmg, 0x7744aa, false);
                if (Math.random() < 0.5 && !target.bleeding) {
                    target.bleeding = true;
                    target.bleedTimer = 4000;
                    target.bleedTickTimer = 1000;
                    target.bleedDamage = Math.max(1, Math.floor(caster.atk * 0.3));
                }
            }
        }
    },
    spirit_walker: {
        name: '영혼 치유', cooldown: 10000, desc: 'HP 가장 낮은 아군 HP 50% 회복 + 전원 HP 10% 회복',
        execute(caster, allies, scene) {
            const alive = allies.filter(a => a.alive).sort((a, b) => (a.hp/a.maxHp) - (b.hp/b.maxHp));
            if (alive.length > 0) {
                const weakest = alive[0];
                const bigHeal = Math.floor(weakest.maxHp * 0.5);
                weakest.hp = Math.min(weakest.maxHp, weakest.hp + bigHeal);
                DamagePopup.show(scene, weakest.container.x, weakest.container.y - 30, bigHeal, 0x44ffff, true);
            }
            allies.forEach(a => {
                if (!a.alive) return;
                const heal = Math.floor(a.maxHp * 0.1);
                a.hp = Math.min(a.maxHp, a.hp + heal);
            });
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '영혼치유!', 0x44bbbb, true);
        }
    },

    // ─── Tier 3 skills ───
    warlord: {
        name: '전쟁의 포효', cooldown: 8000, desc: '모든 아군 ATK +40% (6초) + 자신 HP 20% 회복 + 적 전체 ATK×2',
        execute(caster, enemies, scene) {
            const allies = scene.allies || [];
            allies.forEach(a => {
                if (!a || !a.alive) return;
                const origAtk = a.atk;
                a.atk = Math.floor(a.atk * 1.4);
                scene.time.delayedCall(6000, () => { a.atk = origAtk; });
            });
            caster.hp = Math.min(caster.maxHp, caster.hp + Math.floor(caster.maxHp * 0.2));
            const dmg = Math.floor(caster.atk * 2);
            enemies.forEach(e => {
                if (!e.alive) return;
                e.takeDamage(dmg, caster);
                DamagePopup.show(scene, e.container.x, e.container.y - 20, dmg, 0x5588ee, false);
            });
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '포효!', 0x5588ee, true);
        }
    },
    iron_wall: {
        name: '절대 방어', cooldown: 12000, desc: '자신 DEF +30 (8초) + 모든 아군 받는 피해 -30% (6초) + 가시 +15',
        execute(caster, allies, scene) {
            caster.def += 30;
            scene.time.delayedCall(8000, () => { caster.def = Math.max(0, caster.def - 30); });
            allies.forEach(a => {
                if (!a.alive) return;
                a.damageReduction = (a.damageReduction || 0) + 0.3;
                scene.time.delayedCall(6000, () => { a.damageReduction = Math.max(0, (a.damageReduction || 0) - 0.3); });
            });
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '절대방어!', 0x2255aa, true);
        }
    },
    sword_saint: {
        name: '천검만화', cooldown: 6000, desc: '모든 적에게 ATK×2 (각각 70% 크리티컬) + 크리 시 추가 ATK×1',
        execute(caster, enemies, scene) {
            enemies.forEach(e => {
                if (!e.alive) return;
                const isCrit = Math.random() < 0.7;
                let dmg = Math.floor(caster.atk * 2 * (isCrit ? caster.critDmg : 1));
                if (isCrit) dmg += Math.floor(caster.atk);
                e.takeDamage(dmg, caster);
                if (isCrit) DamagePopup.showCritical(scene, e.container.x, e.container.y - 20, dmg);
                else DamagePopup.show(scene, e.container.x, e.container.y - 20, dmg, 0xff5555, false);
            });
        }
    },
    reaper: {
        name: '사신의 낫', cooldown: 5000, desc: 'HP 25%이하 적 전원 즉사, 나머지에게 ATK×3',
        execute(caster, enemies, scene) {
            enemies.forEach(e => {
                if (!e.alive) return;
                if (e.hp / e.maxHp <= 0.25) {
                    e.takeDamage(e.hp, caster);
                    DamagePopup.show(scene, e.container.x, e.container.y - 20, '즉사!', 0xcc2244, false);
                } else {
                    const dmg = Math.floor(caster.atk * 3);
                    e.takeDamage(dmg, caster);
                    DamagePopup.show(scene, e.container.x, e.container.y - 20, dmg, 0xcc2244, false);
                }
            });
        }
    },
    high_priest: {
        name: '기적', cooldown: 12000, desc: '모든 아군 HP 50% 회복 + 사망 아군 1명 부활(50%HP) + DEF +8 (6초)',
        execute(caster, allies, scene) {
            let revived = false;
            allies.forEach(a => {
                if (a.alive) {
                    const heal = Math.floor(a.maxHp * 0.5);
                    a.hp = Math.min(a.maxHp, a.hp + heal);
                    a.def += 8;
                    scene.time.delayedCall(6000, () => { a.def = Math.max(0, a.def - 8); });
                    DamagePopup.show(scene, a.container.x, a.container.y - 30, heal, 0x55ffbb, true);
                } else if (!revived) {
                    a.alive = true;
                    a.hp = Math.floor(a.maxHp * 0.5);
                    revived = true;
                    DamagePopup.show(scene, a.container.x, a.container.y - 30, '기적!', 0xffff44, true);
                }
            });
        }
    },
    inquisitor: {
        name: '정의 심판', cooldown: 9000, desc: '모든 적에게 ATK×2.5 + 아군 전원 HP 15% 회복 + DEF 감소 30%',
        execute(caster, enemies, scene) {
            const dmg = Math.floor(caster.atk * 2.5);
            enemies.forEach(e => {
                if (!e.alive) return;
                e.takeDamage(dmg, caster);
                e.defReduction = (e.defReduction || 0) + 0.3;
                scene.time.delayedCall(6000, () => { e.defReduction = Math.max(0, (e.defReduction || 0) - 0.3); });
                DamagePopup.show(scene, e.container.x, e.container.y - 20, dmg, 0x22bb88, false);
            });
            const allies = scene.allies || [];
            allies.forEach(a => {
                if (!a || !a.alive) return;
                const heal = Math.floor(a.maxHp * 0.15);
                a.hp = Math.min(a.maxHp, a.hp + heal);
            });
        }
    },
    void_mage: {
        name: '공허 폭발', cooldown: 10000, desc: '모든 적에게 ATK×4 + 5초간 공허(받는 피해 +30%)',
        execute(caster, enemies, scene) {
            const dmg = Math.floor(caster.atk * 4);
            enemies.forEach(e => {
                if (!e.alive) return;
                e.takeDamage(dmg, caster);
                e.damageAmplify = (e.damageAmplify || 0) + 0.3;
                scene.time.delayedCall(5000, () => { e.damageAmplify = Math.max(0, (e.damageAmplify || 0) - 0.3); });
                DamagePopup.show(scene, e.container.x, e.container.y - 20, dmg, 0xbb55ee, false);
            });
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '공허!', 0xbb55ee, true);
        }
    },
    blood_warlock: {
        name: '혈의 계약', cooldown: 9000, desc: '자신 HP 20% 소모 → 모든 적에게 소모량×3 + 흡혈 15%로 전원 회복',
        execute(caster, enemies, scene) {
            const cost = Math.floor(caster.maxHp * 0.2);
            caster.hp = Math.max(1, caster.hp - cost);
            const dmg = cost * 3;
            let totalDmg = 0;
            enemies.forEach(e => {
                if (!e.alive) return;
                e.takeDamage(dmg, caster);
                totalDmg += dmg;
                DamagePopup.show(scene, e.container.x, e.container.y - 20, dmg, 0x882299, false);
            });
            const heal = Math.floor(totalDmg * 0.15);
            const allies = scene.allies || [];
            allies.forEach(a => {
                if (!a || !a.alive) return;
                a.hp = Math.min(a.maxHp, a.hp + Math.floor(heal / allies.length));
            });
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '혈계약!', 0x882299, true);
        }
    },
    marksman: {
        name: '헤드샷', cooldown: 7000, desc: 'HP 가장 높은 적에게 ATK×5.5 (DEF 무시, 100% 크리) + 즉사(HP 10%이하)',
        execute(caster, enemies, scene) {
            const target = enemies.filter(e => e.alive).sort((a, b) => b.hp - a.hp)[0];
            if (!target) return;
            if (target.hp / target.maxHp <= 0.10) {
                target.hp = 0;
                target.alive = false;
                DamagePopup.show(scene, target.container.x, target.container.y - 20, '헤드샷!', 0x99dd44, false);
            } else {
                const dmg = Math.floor(caster.atk * 5.5 * caster.critDmg);
                target.hp -= dmg;
                if (target.hp <= 0) { target.hp = 0; target.alive = false; }
                DamagePopup.showCritical(scene, target.container.x, target.container.y - 20, dmg);
            }
            const line = scene.add.line(0, 0, caster.container.x, caster.container.y - 10, target.container.x, target.container.y - 10, 0x99dd44, 0.9).setDepth(50).setOrigin(0, 0);
            scene.tweens.add({ targets: line, alpha: 0, duration: 500, onComplete: () => line.destroy() });
        }
    },
    tempest: {
        name: '폭풍의 화살', cooldown: 6000, desc: '모든 적에게 ATK×1.5 4연사 + 이속 -30% (4초)',
        execute(caster, enemies, scene) {
            for (let v = 0; v < 4; v++) {
                enemies.forEach(e => {
                    if (!e.alive) return;
                    const dmg = Math.floor(caster.atk * 1.5);
                    e.takeDamage(dmg, caster);
                    DamagePopup.show(scene, e.container.x + (Math.random()-0.5)*15, e.container.y - 20 - v*6, dmg, 0x66bb22, false);
                });
            }
            enemies.forEach(e => {
                if (!e.alive) return;
                const origSpd = e.moveSpeed;
                e.moveSpeed = Math.floor(e.moveSpeed * 0.7);
                scene.time.delayedCall(4000, () => { e.moveSpeed = origSpd; });
            });
        }
    },
    holy_knight: {
        name: '성광 폭발', cooldown: 10000, desc: '모든 적에게 ATK×3 + 아군 전원 HP 25% 회복 + DEF +10 (6초)',
        execute(caster, enemies, scene) {
            const dmg = Math.floor(caster.atk * 3);
            enemies.forEach(e => {
                if (!e.alive) return;
                e.takeDamage(dmg, caster);
                DamagePopup.show(scene, e.container.x, e.container.y - 20, dmg, 0xeedd33, false);
            });
            const allies = scene.allies || [];
            allies.forEach(a => {
                if (!a || !a.alive) return;
                const heal = Math.floor(a.maxHp * 0.25);
                a.hp = Math.min(a.maxHp, a.hp + heal);
                a.def += 10;
                scene.time.delayedCall(6000, () => { a.def = Math.max(0, a.def - 10); });
            });
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '성광!', 0xeedd33, true);
        }
    },
    avenger: {
        name: '복수의 칼날', cooldown: 9000, desc: '잃은 HP% 비례 ATK×(2+3×잃은%) 전체 공격 + 자신 HP 15% 회복',
        execute(caster, enemies, scene) {
            const hpLost = 1 - (caster.hp / caster.maxHp);
            const mult = 2 + hpLost * 3;
            const dmg = Math.floor(caster.atk * mult);
            enemies.forEach(e => {
                if (!e.alive) return;
                e.takeDamage(dmg, caster);
                DamagePopup.show(scene, e.container.x, e.container.y - 20, dmg, 0xccbb22, false);
            });
            caster.hp = Math.min(caster.maxHp, caster.hp + Math.floor(caster.maxHp * 0.15));
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '복수!', 0xccbb22, true);
        }
    },
    death_shadow: {
        name: '죽음의 춤', cooldown: 4000, desc: '랜덤 적 6회 ATK×2.2 (각각 크리 판정) + 킬 시 HP 10% 회복',
        execute(caster, enemies, scene) {
            const alive = enemies.filter(e => e.alive);
            if (alive.length === 0) return;
            for (let i = 0; i < 6; i++) {
                const target = alive[Math.floor(Math.random() * alive.length)];
                if (!target || !target.alive) continue;
                const isCrit = Math.random() < caster.critRate;
                const dmg = Math.floor(caster.atk * 2.2 * (isCrit ? caster.critDmg : 1));
                target.takeDamage(dmg, caster);
                if (isCrit) DamagePopup.showCritical(scene, target.container.x + (Math.random()-0.5)*20, target.container.y - 20 - i*6, dmg);
                else DamagePopup.show(scene, target.container.x + (Math.random()-0.5)*20, target.container.y - 20 - i*6, dmg, 0x9955bb, false);
                if (!target.alive) {
                    caster.hp = Math.min(caster.maxHp, caster.hp + Math.floor(caster.maxHp * 0.1));
                }
            }
        }
    },
    venom_lord: {
        name: '맹독 폭풍', cooldown: 5500, desc: '모든 적에게 ATK×2 + 맹독(ATK×0.6/초, 6초) + 출혈',
        execute(caster, enemies, scene) {
            const dmg = Math.floor(caster.atk * 2);
            enemies.forEach(e => {
                if (!e.alive) return;
                e.takeDamage(dmg, caster);
                e.bleeding = true;
                e.bleedTimer = 6000;
                e.bleedTickTimer = 1000;
                e.bleedDamage = Math.max(1, Math.floor(caster.atk * 0.6));
                DamagePopup.show(scene, e.container.x, e.container.y - 20, dmg, 0x663399, false);
            });
            DamagePopup.show(scene, caster.container.x, caster.container.y - 30, '맹독!', 0x663399, true);
        }
    },
    arch_shaman: {
        name: '대지의 축복', cooldown: 10000, desc: '모든 적 ATK/공속 -30% (6초) + 아군 전원 HP 20% 회복 + 출혈 부여',
        execute(caster, enemies, scene) {
            enemies.forEach(e => {
                if (!e.alive) return;
                const origAtk = e.atk;
                const origSpd = e.attackSpeed;
                e.atk = Math.floor(e.atk * 0.7);
                e.attackSpeed = Math.floor(e.attackSpeed * 1.3);
                scene.time.delayedCall(6000, () => { e.atk = origAtk; e.attackSpeed = origSpd; });
                if (!e.bleeding) {
                    e.bleeding = true;
                    e.bleedTimer = 5000;
                    e.bleedTickTimer = 1000;
                    e.bleedDamage = Math.max(1, Math.floor(caster.atk * 0.4));
                }
                DamagePopup.show(scene, e.container.x, e.container.y - 20, '약화!', 0x55dddd, false);
            });
            const allies = scene.allies || [];
            allies.forEach(a => {
                if (!a || !a.alive) return;
                const heal = Math.floor(a.maxHp * 0.2);
                a.hp = Math.min(a.maxHp, a.hp + heal);
            });
        }
    },
    soul_keeper: {
        name: '영혼 결속', cooldown: 11000, desc: '사망 아군 전원 부활(40%HP) + 생존 아군 HP 30% 회복',
        execute(caster, allies, scene) {
            allies.forEach(a => {
                if (a.alive) {
                    const heal = Math.floor(a.maxHp * 0.3);
                    a.hp = Math.min(a.maxHp, a.hp + heal);
                    DamagePopup.show(scene, a.container.x, a.container.y - 30, heal, 0x33aaaa, true);
                } else {
                    a.alive = true;
                    a.hp = Math.floor(a.maxHp * 0.4);
                    DamagePopup.show(scene, a.container.x, a.container.y - 30, '결속!', 0xffff44, true);
                }
            });
        }
    }
};
