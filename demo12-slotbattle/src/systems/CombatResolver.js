class CombatResolver {
    constructor() {}

    resolve(slotResults, playerState, enemies, slotMachine, comboUpgrades) {
        const combo = slotMachine.evaluateCombo(slotResults);
        const actions = [];

        if (combo) {
            const upgLvl = (comboUpgrades && comboUpgrades[combo.id]) || 0;
            const mult = 1 + upgLvl * 0.2;
            const boosted = {};
            for (const [k, v] of Object.entries(combo.effect)) {
                boosted[k] = (typeof v === 'number') ? Math.floor(v * mult) : v;
            }
            actions.push({
                type: 'combo', name: combo.name, desc: combo.desc,
                effect: boosted
            });
        } else {
            const individual = slotMachine.getIndividualEffects(slotResults);
            if (individual.damage > 0) actions.push({ type: 'attack', effect: { damage: individual.damage } });
            if (individual.block > 0) actions.push({ type: 'defend', effect: { block: individual.block } });
            if (individual.heal > 0) actions.push({ type: 'heal', effect: { heal: individual.heal } });
            if (individual.gold > 0) actions.push({ type: 'gold', effect: { gold: individual.gold } });
            if (individual.selfDamage > 0) actions.push({ type: 'curse', effect: { selfDamage: individual.selfDamage } });
        }

        return this.applyActions(actions, playerState, enemies);
    }

    applyActions(actions, player, enemies) {
        const log = [];

        for (const action of actions) {
            const e = action.effect;

            if (e.block) {
                player.block += e.block;
                log.push({ type: 'block', value: e.block, name: action.name });
            }

            if (e.heal) {
                const healed = Math.min(e.heal, player.maxHp - player.hp);
                player.hp += healed;
                log.push({ type: 'heal', value: healed, name: action.name });
            }

            if (e.gold) {
                player.gold += e.gold;
                log.push({ type: 'gold', value: e.gold, name: action.name });
            }

            if (e.damage) {
                if (e.aoe) {
                    for (const enemy of enemies) {
                        if (enemy.hp <= 0) continue;
                        const dmg = Math.max(1, e.damage - enemy.defense);
                        enemy.hp -= dmg;
                        log.push({ type: 'damage', value: dmg, target: enemy.name, name: action.name });
                    }
                } else if (e.hits) {
                    const alive = enemies.filter(en => en.hp > 0);
                    for (let h = 0; h < e.hits && alive.length > 0; h++) {
                        const target = alive[Phaser.Math.Between(0, alive.length - 1)];
                        const dmg = Math.max(1, e.damage - target.defense);
                        target.hp -= dmg;
                        log.push({ type: 'damage', value: dmg, target: target.name, name: action.name });
                    }
                } else {
                    const alive = enemies.filter(en => en.hp > 0);
                    if (alive.length > 0) {
                        const target = alive[0];
                        const dmg = Math.max(1, e.damage - target.defense);
                        target.hp -= dmg;
                        log.push({ type: 'damage', value: dmg, target: target.name, name: action.name });
                    }
                }

                if (e.burn) {
                    const alive = enemies.filter(en => en.hp > 0);
                    if (alive.length > 0) {
                        alive[0].burn = (alive[0].burn || 0) + e.burn;
                        log.push({ type: 'burn', value: e.burn, target: alive[0].name });
                    }
                }
                if (e.poison) {
                    const alive = enemies.filter(en => en.hp > 0);
                    if (alive.length > 0) {
                        alive[0].poison = (alive[0].poison || 0) + e.poison;
                        log.push({ type: 'poison', value: e.poison, target: alive[0].name });
                    }
                }
            }

            if (e.thorns) {
                log.push({ type: 'thorns', value: e.thorns, name: action.name });
            }

            if (e.selfDamage) {
                player.hp -= e.selfDamage;
                log.push({ type: 'selfDamage', value: e.selfDamage });
            }
        }

        return { log, actions, hasCombo: actions.some(a => a.type === 'combo') };
    }

    enemyAttack(enemies, player) {
        const log = [];
        for (const enemy of enemies) {
            if (enemy.hp <= 0) continue;

            if (enemy.burn && enemy.burn > 0) {
                enemy.hp -= enemy.burn;
                log.push({ type: 'dot', dotType: 'burn', value: enemy.burn, target: enemy.name });
                enemy.burn--;
            }
            if (enemy.poison && enemy.poison > 0) {
                enemy.hp -= enemy.poison;
                log.push({ type: 'dot', dotType: 'poison', value: enemy.poison, target: enemy.name });
            }

            if (enemy.hp <= 0) continue;

            let dmg = enemy.attack;
            if (player.block > 0) {
                const blocked = Math.min(player.block, dmg);
                dmg -= blocked;
                player.block -= blocked;
                log.push({ type: 'blocked', value: blocked, from: enemy.name });
            }
            if (dmg > 0) {
                player.hp -= dmg;
                log.push({ type: 'playerHit', value: dmg, from: enemy.name });
            }
        }
        player.block = 0;
        return log;
    }
}
