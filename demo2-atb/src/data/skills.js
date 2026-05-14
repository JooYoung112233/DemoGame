const ATB_SKILLS = {
    taunt: {
        name: '도발',
        cooldownTurns: 3,
        duration: 2,
        owner: 'tank',
        execute(caster, targets, scene) {
            const enemies = targets.filter(t => t.team !== caster.team && t.alive);
            enemies.forEach(e => {
                e.forcedTarget = caster;
                e.forcedTargetTurns = this.duration;
            });
            scene.showSkillText(caster, '도발!', 0x4488ff);
        }
    },
    bleed: {
        name: '출혈',
        cooldownTurns: 2,
        durationTurns: 3,
        dmgPerTurn: 15,
        owner: 'rogue',
        execute(caster, targets, scene) {
            const t = caster.currentTarget;
            if (!t || !t.alive) return;
            const existing = t.statusEffects.find(e => e.type === 'bleed');
            if (existing) {
                existing.stacks = Math.min((existing.stacks || 1) + 1, 5);
                existing.turnsLeft = this.durationTurns;
            } else {
                t.statusEffects.push({ type: 'bleed', stacks: 1, turnsLeft: this.durationTurns, dmgPerTurn: this.dmgPerTurn });
            }
            scene.showSkillText(caster, '출혈!', 0xff4444);
        }
    },
    heal: {
        name: '치유',
        cooldownTurns: 3,
        healAmount: 80,
        owner: 'priest',
        execute(caster, targets, scene) {
            const allies = targets.filter(t => t.team === caster.team && t.alive && t !== caster);
            if (!allies.length) return;
            const lowest = allies.reduce((a, b) => (a.hp / a.maxHp) < (b.hp / b.maxHp) ? a : b);
            const healed = Math.min(this.healAmount, lowest.maxHp - lowest.hp);
            lowest.hp += healed;
            scene.showDmgPopup(lowest.container.x, lowest.container.y - 30, healed, 0x44ff88, true);
            scene.showSkillText(caster, '치유!', 0x44ff88);
        }
    },
    meteor: {
        name: '메테오',
        cooldownTurns: 4,
        damage: 80,
        owner: 'mage',
        execute(caster, targets, scene) {
            const enemies = targets.filter(t => t.team !== caster.team && t.alive);
            if (!enemies.length) return;
            enemies.forEach(e => {
                const dmg = Math.max(1, this.damage - e.def);
                scene.applyDamage(e, dmg, false, caster);
            });
            const cx = enemies.reduce((s, e) => s + e.container.x, 0) / enemies.length;
            scene.showAoeEffect(cx, 420);
            scene.showSkillText(caster, '메테오!', 0xaa44ff);
        }
    },
    stun: {
        name: '기절',
        cooldownTurns: 4,
        owner: 'tank',
        execute(caster, targets, scene) {
            const t = caster.currentTarget;
            if (!t || !t.alive) return;
            t.stunTurns = 1;
            scene.showSkillText(caster, '기절!', 0xffcc00);
        }
    }
};
