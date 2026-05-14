const SKILL_DATA = {
    taunt: {
        name: '도발',
        cooldown: 8000,
        duration: 3000,
        description: '적들이 탱커를 공격하도록 강제',
        owner: 'tank',
        type: 'buff',
        execute(caster, targets, scene) {
            const enemies = targets.filter(t => t.team !== caster.team && t.alive);
            enemies.forEach(enemy => {
                enemy.statusEffects.push({
                    type: 'taunt',
                    source: caster,
                    duration: this.duration,
                    elapsed: 0
                });
            });
            scene.showSkillText(caster, '도발!', 0x4488ff);
        }
    },
    bleed: {
        name: '출혈',
        cooldown: 5000,
        duration: 4000,
        tickRate: 500,
        damagePerTick: 8,
        description: '대상에게 출혈을 부여하여 지속 피해',
        owner: 'rogue',
        type: 'debuff',
        execute(caster, targets, scene) {
            const target = targets.find(t => t.team !== caster.team && t.alive);
            if (!target) return;
            const existing = target.statusEffects.find(e => e.type === 'bleed');
            if (existing) {
                existing.stacks = Math.min((existing.stacks || 1) + 1, 5);
                existing.elapsed = 0;
            } else {
                target.statusEffects.push({
                    type: 'bleed',
                    source: caster,
                    duration: this.duration,
                    elapsed: 0,
                    tickRate: this.tickRate,
                    tickElapsed: 0,
                    damagePerTick: this.damagePerTick,
                    stacks: 1
                });
            }
            scene.showSkillText(caster, '출혈!', 0xff4444);
        }
    },
    heal: {
        name: '치유',
        cooldown: 7000,
        healAmount: 60,
        description: 'HP가 가장 낮은 아군을 회복',
        owner: 'priest',
        type: 'heal',
        execute(caster, targets, scene) {
            const allies = targets.filter(t => t.team === caster.team && t.alive && t !== caster);
            if (allies.length === 0) return;
            const lowest = allies.reduce((a, b) => (a.hp / a.maxHp) < (b.hp / b.maxHp) ? a : b);
            const healed = Math.min(this.healAmount, lowest.maxHp - lowest.hp);
            lowest.hp += healed;
            scene.showDamagePopup(lowest.container.x, lowest.container.y - 30, healed, 0x44ff88, true);
            scene.showSkillText(caster, '치유!', 0x44ff88);
        }
    },
    shield: {
        name: '보호막',
        cooldown: 12000,
        shieldAmount: 40,
        duration: 5000,
        description: '아군에게 피해를 흡수하는 보호막 부여',
        owner: 'priest',
        type: 'buff',
        execute(caster, targets, scene) {
            const allies = targets.filter(t => t.team === caster.team && t.alive && t !== caster);
            if (allies.length === 0) return;
            const lowest = allies.reduce((a, b) => (a.hp / a.maxHp) < (b.hp / b.maxHp) ? a : b);
            lowest.statusEffects.push({
                type: 'shield',
                source: caster,
                duration: this.duration,
                elapsed: 0,
                amount: this.shieldAmount
            });
            scene.showSkillText(caster, '보호막!', 0x88ccff);
        }
    },
    meteor: {
        name: '메테오',
        cooldown: 10000,
        damage: 70,
        radius: 120,
        description: '넓은 범위에 강력한 마법 공격',
        owner: 'mage',
        type: 'aoe',
        execute(caster, targets, scene) {
            const enemies = targets.filter(t => t.team !== caster.team && t.alive);
            if (enemies.length === 0) return;
            const centerTarget = enemies[Math.floor(enemies.length / 2)];
            const cx = centerTarget.container.x;
            enemies.forEach(enemy => {
                const dist = Math.abs(enemy.container.x - cx);
                if (dist <= this.radius) {
                    const dmg = Math.max(1, this.damage - enemy.def);
                    scene.dealDamage(enemy, dmg, true);
                }
            });
            scene.showAoeEffect(cx, centerTarget.container.y, this.radius);
            scene.showSkillText(caster, '메테오!', 0xaa44ff);
        }
    }
};
