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
            actions.push({ type: 'combo', name: combo.name, desc: combo.desc, effect: boosted });
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
                        const def = enemy.block > 0 ? enemy.block : enemy.defense;
                        const dmg = e.pierce ? e.damage : Math.max(1, e.damage - def);
                        if (enemy.block > 0) {
                            const absorbed = Math.min(enemy.block, dmg);
                            enemy.block -= absorbed;
                            const remain = dmg - absorbed;
                            if (remain > 0) enemy.hp -= remain;
                            log.push({ type: 'damage', value: dmg, target: enemy.name, targetIdx: enemy.index });
                        } else {
                            enemy.hp -= dmg;
                            log.push({ type: 'damage', value: dmg, target: enemy.name, targetIdx: enemy.index });
                        }
                    }
                } else if (e.hits) {
                    for (let h = 0; h < e.hits; h++) {
                        const alive = enemies.filter(en => en.hp > 0);
                        if (alive.length === 0) break;
                        const target = alive[Phaser.Math.Between(0, alive.length - 1)];
                        const def = target.block > 0 ? target.block : target.defense;
                        const dmg = e.pierce ? e.damage : Math.max(1, e.damage - def);
                        this._dealDamageToEnemy(target, dmg, log);
                    }
                } else {
                    // single target — NO overflow
                    const alive = enemies.filter(en => en.hp > 0);
                    if (alive.length > 0) {
                        const target = alive[0];
                        const def = target.block > 0 ? target.block : target.defense;
                        const dmg = e.pierce ? e.damage : Math.max(1, e.damage - def);
                        this._dealDamageToEnemy(target, dmg, log);
                    }
                }

                if (e.burn) {
                    const alive = enemies.filter(en => en.hp > 0);
                    for (const t of (e.aoe ? alive : alive.slice(0, 1))) {
                        t.burn = (t.burn || 0) + e.burn;
                        log.push({ type: 'burn', value: e.burn, target: t.name });
                    }
                }
                if (e.poison) {
                    const alive = enemies.filter(en => en.hp > 0);
                    for (const t of (e.aoe ? alive : alive.slice(0, 1))) {
                        t.poison = (t.poison || 0) + e.poison;
                        log.push({ type: 'poison', value: e.poison, target: t.name });
                    }
                }
            }

            if (e.slow) {
                const alive = enemies.filter(en => en.hp > 0);
                for (const t of (e.aoe ? alive : alive.slice(0, 1))) {
                    t.cooldownTimer = (t.cooldownTimer || 0) + e.slow;
                    log.push({ type: 'slow', value: e.slow, target: t.name });
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

    _dealDamageToEnemy(target, dmg, log) {
        if (target.block > 0) {
            const absorbed = Math.min(target.block, dmg);
            target.block -= absorbed;
            const remain = dmg - absorbed;
            if (absorbed > 0) log.push({ type: 'enemyBlocked', value: absorbed, target: target.name, targetIdx: target.index });
            if (remain > 0) {
                target.hp -= remain;
                log.push({ type: 'damage', value: remain, target: target.name, targetIdx: target.index });
            }
        } else {
            target.hp -= dmg;
            log.push({ type: 'damage', value: dmg, target: target.name, targetIdx: target.index });
        }
    }

    // ── ENEMY TURN ─────────────────────────────────

    tickEnemyCooldowns(enemies) {
        const actors = [];
        for (const enemy of enemies) {
            if (enemy.hp <= 0) continue;
            enemy.cooldownTimer--;
            if (enemy.cooldownTimer <= 0) {
                actors.push(enemy);
                enemy.cooldownTimer = enemy.cooldown;
            }
        }
        return actors;
    }

    rollEnemyIntent(enemy) {
        const intents = enemy.intents || [{ type: 'attack', weight: 1 }];
        const totalWeight = intents.reduce((sum, i) => sum + i.weight, 0);
        let roll = Math.random() * totalWeight;
        for (const intent of intents) {
            roll -= intent.weight;
            if (roll <= 0) return intent;
        }
        return intents[0];
    }

    enemyAct(actors, player, allEnemies) {
        const log = [];

        for (const enemy of actors) {
            if (enemy.hp <= 0) continue;

            // DoTs first
            if (enemy.burn && enemy.burn > 0) {
                enemy.hp -= enemy.burn;
                log.push({ type: 'dot', dotType: 'burn', value: enemy.burn, target: enemy.name, targetIdx: enemy.index });
                enemy.burn--;
            }
            if (enemy.poison && enemy.poison > 0) {
                enemy.hp -= enemy.poison;
                log.push({ type: 'dot', dotType: 'poison', value: enemy.poison, target: enemy.name, targetIdx: enemy.index });
            }
            if (enemy.hp <= 0) continue;

            const intent = enemy.nextIntent || this.rollEnemyIntent(enemy);

            switch (intent.type) {
                case 'attack': {
                    const dmg = this._calcPlayerDmg(enemy.attack, player);
                    log.push(...dmg.log);
                    break;
                }
                case 'bleed': {
                    const dmg = this._calcPlayerDmg(Math.floor(enemy.attack * 0.6), player);
                    log.push(...dmg.log);
                    player.bleed = (player.bleed || 0) + (intent.value || 3);
                    log.push({ type: 'statusApplied', status: 'bleed', value: intent.value || 3, from: enemy.name });
                    break;
                }
                case 'attackBleed': {
                    const dmg = this._calcPlayerDmg(enemy.attack, player);
                    log.push(...dmg.log);
                    player.bleed = (player.bleed || 0) + (intent.bleed || 3);
                    log.push({ type: 'statusApplied', status: 'bleed', value: intent.bleed || 3, from: enemy.name });
                    break;
                }
                case 'attackBurn': {
                    const dmg = this._calcPlayerDmg(enemy.attack, player);
                    log.push(...dmg.log);
                    player.burn = (player.burn || 0) + (intent.burn || 3);
                    log.push({ type: 'statusApplied', status: 'burn', value: intent.burn || 3, from: enemy.name });
                    break;
                }
                case 'lifesteal': {
                    const dmg = this._calcPlayerDmg(enemy.attack, player);
                    log.push(...dmg.log);
                    const healed = Math.min(dmg.dealt, enemy.maxHp - enemy.hp);
                    if (healed > 0) {
                        enemy.hp += healed;
                        log.push({ type: 'enemyHeal', value: healed, target: enemy.name, targetIdx: enemy.index });
                    }
                    break;
                }
                case 'doubleStrike': {
                    for (let i = 0; i < 2; i++) {
                        const dmg = this._calcPlayerDmg(Math.floor(enemy.attack * 0.6), player);
                        log.push(...dmg.log);
                    }
                    break;
                }
                case 'defend': {
                    enemy.block = (enemy.block || 0) + (intent.block || 8);
                    log.push({ type: 'enemyDefend', value: intent.block || 8, target: enemy.name, targetIdx: enemy.index });
                    break;
                }
                case 'buff': {
                    if (intent.atkUp) {
                        enemy.attack += intent.atkUp;
                        log.push({ type: 'enemyBuff', stat: 'attack', value: intent.atkUp, target: enemy.name, targetIdx: enemy.index });
                    }
                    break;
                }
                case 'healAll': {
                    const allies = allEnemies.filter(e => e.hp > 0);
                    for (const ally of allies) {
                        const healed = Math.min(intent.heal || 10, ally.maxHp - ally.hp);
                        if (healed > 0) {
                            ally.hp += healed;
                            log.push({ type: 'enemyHeal', value: healed, target: ally.name, targetIdx: ally.index });
                        }
                    }
                    break;
                }
            }

            // roll next intent for display
            enemy.nextIntent = this.rollEnemyIntent(enemy);
        }

        // player bleed tick
        if (player.bleed && player.bleed > 0) {
            player.hp -= player.bleed;
            log.push({ type: 'playerDot', dotType: 'bleed', value: player.bleed });
            player.bleed--;
        }
        if (player.burn && player.burn > 0) {
            player.hp -= player.burn;
            log.push({ type: 'playerDot', dotType: 'burn', value: player.burn });
            player.burn--;
        }

        return log;
    }

    _calcPlayerDmg(atk, player) {
        const log = [];
        let dmg = atk;
        let dealt = 0;
        if (player.block > 0) {
            const blocked = Math.min(player.block, dmg);
            dmg -= blocked;
            player.block -= blocked;
            if (blocked > 0) log.push({ type: 'blocked', value: blocked });
        }
        if (dmg > 0) {
            player.hp -= dmg;
            dealt = dmg;
            log.push({ type: 'playerHit', value: dmg });
        }
        return { log, dealt };
    }
}
