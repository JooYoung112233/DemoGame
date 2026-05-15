const SYNERGY_DATA = {
    // --- 2인 시너지 ---
    holy_war: {
        name: '성전', type: 2,
        classes: ['warrior', 'priest'],
        desc: '전사 DEF +15%, 사제 힐량 +20%',
        apply(units) {
            units.forEach(u => {
                if (u.classKey === 'warrior') u.def = Math.floor(u.def * 1.15);
                if (u.classKey === 'priest') u.healMult = (u.healMult || 1) + 0.20;
            });
        }
    },
    dark_arts: {
        name: '암흑술', type: 2,
        classes: ['rogue', 'mage'],
        desc: '출혈/마법 피해 +15%',
        apply(units) {
            units.forEach(u => {
                if (u.classKey === 'rogue' || u.classKey === 'mage') {
                    u.atk = Math.floor(u.atk * 1.15);
                }
            });
        }
    },
    poison_arrow: {
        name: '독화살', type: 2,
        classes: ['archer', 'alchemist'],
        desc: '궁수 공격 독 부여 30%',
        apply(units) {
            units.forEach(u => {
                if (u.classKey === 'archer') u.bleedChance = (u.bleedChance || 0) + 0.30;
            });
        }
    },
    vanguard: {
        name: '전위대', type: 2,
        classes: ['warrior', 'rogue'],
        desc: '근접 공격력 +10%',
        apply(units) {
            units.forEach(u => {
                if (u.range <= 100) u.atk = Math.floor(u.atk * 1.10);
            });
        }
    },
    mystic_barrier: {
        name: '신비 결계', type: 2,
        classes: ['mage', 'priest'],
        desc: '런 시작 시 전체 보호막',
        apply(units) {
            units.forEach(u => {
                u.shield = (u.shield || 0) + Math.floor(u.maxHp * 0.10);
            });
        }
    },
    rapid_fire: {
        name: '속사', type: 2,
        classes: ['archer', 'rogue'],
        desc: '둘 다 공격속도 +15%',
        apply(units) {
            units.forEach(u => {
                if (u.classKey === 'archer' || u.classKey === 'rogue') {
                    u.attackSpeed = Math.floor(u.attackSpeed * 0.85);
                }
            });
        }
    },
    medical_team: {
        name: '의료팀', type: 2,
        classes: ['alchemist', 'priest'],
        desc: '힐량 +15%, 상태이상 저항 +20%',
        apply(units) {
            units.forEach(u => {
                if (u.classKey === 'alchemist' || u.classKey === 'priest') {
                    u.healMult = (u.healMult || 1) + 0.15;
                }
                u.statusResist = (u.statusResist || 0) + 0.20;
            });
        }
    },

    // --- 3인 시너지 ---
    iron_company: {
        name: '철벽 용병단', type: 3,
        classes: ['warrior', 'rogue', 'priest'],
        desc: '전체 HP +15%, 사망 시 1회 HP 10% 유지',
        apply(units) {
            units.forEach(u => {
                u.maxHp = Math.floor(u.maxHp * 1.15);
                u.hp = Math.min(u.hp + Math.floor(u.maxHp * 0.15), u.maxHp);
                u.deathSave = true;
            });
        }
    },
    ranged_battery: {
        name: '원거리 포격대', type: 3,
        classes: ['mage', 'archer', 'alchemist'],
        desc: '원거리 사거리 +20%, ATK +10%',
        apply(units) {
            units.forEach(u => {
                if (u.range > 100) {
                    u.range = Math.floor(u.range * 1.20);
                    u.atk = Math.floor(u.atk * 1.10);
                }
            });
        }
    },
    royal_party: {
        name: '왕도 파티', type: 3,
        classes: ['warrior', 'mage', 'priest'],
        desc: '매 전투 시작 시 전체 HP 10% 회복',
        apply(units) {
            units.forEach(u => {
                u.hp = Math.min(u.maxHp, u.hp + Math.floor(u.maxHp * 0.10));
            });
        }
    },
    assassin_squad: {
        name: '암살 전문대', type: 3,
        classes: ['rogue', 'archer', 'alchemist'],
        desc: '크리 확률 +20%, 크리 피해 +30%',
        apply(units) {
            units.forEach(u => {
                u.critRate = Math.min(0.8, u.critRate + 0.20);
                u.critDmg += 0.30;
            });
        }
    },
    guardian_order: {
        name: '수호 결사', type: 3,
        classes: ['warrior', 'priest', 'alchemist'],
        desc: '전체 DEF +20%, 매 10초 HP 소량 회복',
        apply(units) {
            units.forEach(u => {
                u.def = Math.floor(u.def * 1.20);
                u.passiveRegen = (u.passiveRegen || 0) + 0.02;
            });
        }
    },

    // --- 5인 보너스 ---
    all_round: {
        name: '올라운드', type: 5,
        check(classKeys) {
            const unique = new Set(classKeys);
            return unique.size >= 5;
        },
        desc: '전체 스탯 +5%, 길드 XP +10%',
        apply(units) {
            units.forEach(u => {
                u.atk = Math.floor(u.atk * 1.05);
                u.def = Math.floor(u.def * 1.05);
                u.maxHp = Math.floor(u.maxHp * 1.05);
                u.moveSpeed = Math.floor(u.moveSpeed * 1.05);
            });
        },
        xpBonus: 0.10
    },
    specialist: {
        name: '전문 특화', type: 5,
        check(classKeys) {
            const counts = {};
            classKeys.forEach(k => { counts[k] = (counts[k] || 0) + 1; });
            return Object.entries(counts).find(([, c]) => c >= 3);
        },
        desc: '해당 클래스 스탯 +25%, 나머지 -10%',
        apply(units, classKeys) {
            const counts = {};
            classKeys.forEach(k => { counts[k] = (counts[k] || 0) + 1; });
            const specClass = Object.entries(counts).find(([, c]) => c >= 3)?.[0];
            if (!specClass) return;
            units.forEach(u => {
                if (u.classKey === specClass) {
                    u.atk = Math.floor(u.atk * 1.25);
                    u.def = Math.floor(u.def * 1.25);
                    u.maxHp = Math.floor(u.maxHp * 1.25);
                } else {
                    u.atk = Math.floor(u.atk * 0.90);
                    u.def = Math.floor(u.def * 0.90);
                }
            });
        }
    }
};

function getActiveSynergies(partyClassKeys) {
    const active = [];
    const classSet = new Set(partyClassKeys);

    for (const [key, syn] of Object.entries(SYNERGY_DATA)) {
        if (syn.type === 2 || syn.type === 3) {
            if (syn.classes.every(c => classSet.has(c))) {
                active.push({ key, ...syn });
            }
        } else if (syn.type === 5 && syn.check) {
            if (syn.check(partyClassKeys)) {
                active.push({ key, ...syn });
            }
        }
    }
    return active;
}

function applySynergies(units, partyClassKeys) {
    const active = getActiveSynergies(partyClassKeys);
    for (const syn of active) {
        syn.apply(units, partyClassKeys);
    }
    return active;
}
