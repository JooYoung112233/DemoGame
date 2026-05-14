const SKILL_DATA = {
    shield_block: {
        name: '방패막기', cooldown: 8000, duration: 4000,
        desc: '4초간 받는 피해 40% 감소',
        execute(caster, targets, scene) {
            caster.statusEffects.push({
                type: 'shield_block', duration: 4000, damageReduction: 0.4
            });
            scene.showSkillEffect(caster, '방패막기', 0x4488ff);
        }
    },
    holy_strike: {
        name: '신성일격', cooldown: 10000,
        desc: '전방 적 2체에 ATK×1.8 피해 + 자힐 30',
        execute(caster, targets, scene) {
            const enemies = targets.filter(t => t.team !== caster.team && t.hp > 0)
                .sort((a, b) => Math.abs(a.x - caster.x) - Math.abs(b.x - caster.x))
                .slice(0, 2);
            const dmg = Math.floor(caster.atk * 1.8);
            enemies.forEach(e => scene.dealSkillDamage(caster, e, dmg, '신성일격'));
            caster.hp = Math.min(caster.maxHp, caster.hp + 30);
            scene.showSkillEffect(caster, '신성일격', 0xffff88);
        }
    },
    frenzy: {
        name: '광기', cooldown: 10000, duration: 5000,
        desc: '5초간 공격속도 2배, 받는 피해 20% 증가',
        execute(caster, targets, scene) {
            caster.statusEffects.push({
                type: 'frenzy', duration: 5000,
                atkSpeedMult: 0.5, damageTakenMult: 1.2
            });
            scene.showSkillEffect(caster, '광기!', 0xff4444);
        }
    },
    war_cry: {
        name: '전장의함성', cooldown: 12000, duration: 5000,
        desc: '아군 전체 ATK +20% (5초)',
        execute(caster, targets, scene) {
            const allies = targets.filter(t => t.team === caster.team && t.hp > 0);
            allies.forEach(a => {
                a.statusEffects.push({
                    type: 'war_cry', duration: 5000, atkBonusPct: 0.20
                });
            });
            scene.showSkillEffect(caster, '함성!', 0xffaa44);
        }
    },
    precise_shot: {
        name: '정밀사격', cooldown: 6000,
        desc: '다음 공격 확정 크리티컬 + 1.5배',
        execute(caster, targets, scene) {
            caster.statusEffects.push({
                type: 'precise_shot', duration: 99999, guaranteedCrit: true, critBonus: 1.5, oneShot: true
            });
            scene.showSkillEffect(caster, '정밀사격', 0x66aa22);
        }
    },
    pierce: {
        name: '꿰뚫기', cooldown: 8000,
        desc: '최대 3체 관통 ATK×1.2',
        execute(caster, targets, scene) {
            const enemies = targets.filter(t => t.team !== caster.team && t.hp > 0)
                .sort((a, b) => Math.abs(a.x - caster.x) - Math.abs(b.x - caster.x))
                .slice(0, 3);
            const dmg = Math.floor(caster.atk * 1.2);
            enemies.forEach(e => scene.dealSkillDamage(caster, e, dmg, '꿰뚫기'));
            scene.showSkillEffect(caster, '꿰뚫기', 0x44cc00);
        }
    },
    fire_arrow: {
        name: '화염화살', cooldown: 7000,
        desc: '대상에게 화상 DoT (4초간 초당 6)',
        execute(caster, targets, scene) {
            const enemy = targets.filter(t => t.team !== caster.team && t.hp > 0)
                .sort((a, b) => Math.abs(a.x - caster.x) - Math.abs(b.x - caster.x))[0];
            if (!enemy) return;
            const dmg = Math.floor(caster.atk * 0.8);
            scene.dealSkillDamage(caster, enemy, dmg, '화염화살');
            enemy.statusEffects.push({
                type: 'burn', duration: 4000, tickRate: 1000, damagePerTick: 6,
                tickTimer: 0
            });
            scene.showSkillEffect(caster, '화염화살', 0xff8844);
        }
    },
    firestorm: {
        name: '화염폭풍', cooldown: 12000,
        desc: '반경 150 AoE ATK×1.5 + 화상',
        execute(caster, targets, scene) {
            const enemy = targets.filter(t => t.team !== caster.team && t.hp > 0)
                .sort((a, b) => Math.abs(a.x - caster.x) - Math.abs(b.x - caster.x))[0];
            if (!enemy) return;
            const cx = enemy.x;
            const radius = 150;
            const dmg = Math.floor(caster.atk * 1.5);
            targets.filter(t => t.team !== caster.team && t.hp > 0).forEach(e => {
                if (Math.abs(e.x - cx) <= radius) {
                    scene.dealSkillDamage(caster, e, dmg, '화염폭풍');
                    e.statusEffects.push({
                        type: 'burn', duration: 3000, tickRate: 1000, damagePerTick: 8,
                        tickTimer: 0
                    });
                }
            });
            scene.showAoEEffect(cx, 440, radius, 0xff4400);
        }
    },
    heal: {
        name: '치유', cooldown: 7000,
        desc: 'HP 가장 낮은 아군 HP 50 회복',
        execute(caster, targets, scene) {
            const allies = targets.filter(t => t.team === caster.team && t.hp > 0 && t.hp < t.maxHp);
            if (allies.length === 0) return;
            allies.sort((a, b) => (a.hp / a.maxHp) - (b.hp / b.maxHp));
            const target = allies[0];
            const healAmt = 50;
            target.hp = Math.min(target.maxHp, target.hp + healAmt);
            scene.showHealEffect(target, healAmt);
        }
    },
    grand_heal: {
        name: '대치유', cooldown: 10000,
        desc: '아군 전체 HP 35 회복',
        execute(caster, targets, scene) {
            const allies = targets.filter(t => t.team === caster.team && t.hp > 0);
            allies.forEach(a => {
                a.hp = Math.min(a.maxHp, a.hp + 35);
                scene.showHealEffect(a, 35);
            });
        }
    },
    judgment: {
        name: '심판', cooldown: 6000,
        desc: '주변 적 1체에 ATK×1.5 피해',
        execute(caster, targets, scene) {
            const enemy = targets.filter(t => t.team !== caster.team && t.hp > 0)
                .sort((a, b) => Math.abs(a.x - caster.x) - Math.abs(b.x - caster.x))[0];
            if (!enemy) return;
            const dmg = Math.floor(caster.atk * 1.5);
            scene.dealSkillDamage(caster, enemy, dmg, '심판');
            scene.showSkillEffect(caster, '심판', 0xaadd44);
        }
    },
    divine_justice: {
        name: '정의집행', cooldown: 8000,
        desc: '적 1체 ATK×2.5 + 자힐 20',
        execute(caster, targets, scene) {
            const enemy = targets.filter(t => t.team !== caster.team && t.hp > 0)
                .sort((a, b) => Math.abs(a.x - caster.x) - Math.abs(b.x - caster.x))[0];
            if (!enemy) return;
            const dmg = Math.floor(caster.atk * 2.5);
            scene.dealSkillDamage(caster, enemy, dmg, '정의집행');
            caster.hp = Math.min(caster.maxHp, caster.hp + 20);
            scene.showSkillEffect(caster, '정의집행', 0xddff44);
        }
    },
    vital_strike: {
        name: '급소찌르기', cooldown: 5000,
        desc: '대상에게 ATK×2 피해',
        execute(caster, targets, scene) {
            const enemy = targets.filter(t => t.team !== caster.team && t.hp > 0)
                .sort((a, b) => Math.abs(a.x - caster.x) - Math.abs(b.x - caster.x))[0];
            if (!enemy) return;
            const dmg = Math.floor(caster.atk * 2);
            scene.dealSkillDamage(caster, enemy, dmg, '급소');
            scene.showSkillEffect(caster, '급소!', 0x8822aa);
        }
    },
    death_blow: {
        name: '사신의일격', cooldown: 8000,
        desc: 'HP 30%↓ 적에게 3배 피해',
        execute(caster, targets, scene) {
            const enemies = targets.filter(t => t.team !== caster.team && t.hp > 0);
            const lowHp = enemies.filter(e => e.hp / e.maxHp <= 0.3);
            const target = lowHp.length > 0 ? lowHp[0] : enemies.sort((a, b) => (a.hp / a.maxHp) - (b.hp / b.maxHp))[0];
            if (!target) return;
            const mult = (target.hp / target.maxHp <= 0.3) ? 3 : 1.5;
            const dmg = Math.floor(caster.atk * mult);
            scene.dealSkillDamage(caster, target, dmg, '사신');
            scene.showSkillEffect(caster, '사신의일격', 0x6600cc);
        }
    },
    poison_arrow: {
        name: '독화살', cooldown: 6000,
        desc: '대상에게 독 (5초간 초당 5)',
        execute(caster, targets, scene) {
            const enemy = targets.filter(t => t.team !== caster.team && t.hp > 0)
                .sort((a, b) => Math.abs(a.x - caster.x) - Math.abs(b.x - caster.x))[0];
            if (!enemy) return;
            const dmg = Math.floor(caster.atk * 0.6);
            scene.dealSkillDamage(caster, enemy, dmg, '독화살');
            enemy.statusEffects.push({
                type: 'poison', duration: 5000, tickRate: 1000, damagePerTick: 5,
                tickTimer: 0
            });
            scene.showSkillEffect(caster, '독화살', 0xaa66cc);
        }
    },
    plunder: {
        name: '약탈', cooldown: 10000,
        desc: '적 처치시 골드 +15',
        execute(caster, targets, scene) {
            const enemy = targets.filter(t => t.team !== caster.team && t.hp > 0)
                .sort((a, b) => (a.hp / a.maxHp) - (b.hp / b.maxHp))[0];
            if (!enemy) return;
            const dmg = Math.floor(caster.atk * 1.8);
            scene.dealSkillDamage(caster, enemy, dmg, '약탈');
            caster.plunderBonus = (caster.plunderBonus || 0) + 15;
            scene.showSkillEffect(caster, '약탈!', 0xcc88ee);
        }
    },

    enemy_war_cry: {
        name: '산적 함성', cooldown: 12000, duration: 4000,
        desc: '아군 전체 ATK +15%',
        execute(caster, targets, scene) {
            const allies = targets.filter(t => t.team === caster.team && t.hp > 0);
            allies.forEach(a => {
                a.statusEffects.push({ type: 'war_cry', duration: 4000, atkBonusPct: 0.15 });
            });
            scene.showSkillEffect(caster, '함성!', 0xcc6622);
        }
    },
    enemy_dark_bolt: {
        name: '암흑탄', cooldown: 8000,
        desc: '적 1체에 ATK×1.5',
        execute(caster, targets, scene) {
            const enemy = targets.filter(t => t.team !== caster.team && t.hp > 0)
                .sort((a, b) => Math.abs(a.x - caster.x) - Math.abs(b.x - caster.x))[0];
            if (!enemy) return;
            scene.dealSkillDamage(caster, enemy, Math.floor(caster.atk * 1.5), '암흑탄');
        }
    },
    enemy_dark_wave: {
        name: '암흑파동', cooldown: 15000,
        desc: '전체 적에게 ATK×0.8 피해',
        execute(caster, targets, scene) {
            const enemies = targets.filter(t => t.team !== caster.team && t.hp > 0);
            const dmg = Math.floor(caster.atk * 0.8);
            enemies.forEach(e => scene.dealSkillDamage(caster, e, dmg, '암흑파동'));
            scene.showAoEEffect(caster.x, 440, 200, 0x8822aa);
        }
    },
    enemy_self_heal: {
        name: '어둠재생', cooldown: 20000,
        desc: '자신 HP 100 회복',
        execute(caster, targets, scene) {
            caster.hp = Math.min(caster.maxHp, caster.hp + 100);
            scene.showHealEffect(caster, 100);
        }
    }
};
