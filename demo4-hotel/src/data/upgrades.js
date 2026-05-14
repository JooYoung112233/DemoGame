const HOTEL_UPGRADES = [
    {
        id: 'dmg_up',
        name: '공격력 강화',
        desc: '모든 공격 피해 +3',
        color: '#ff6644',
        apply(state) {
            state.bonusDmg = (state.bonusDmg || 0) + 3;
        }
    },
    {
        id: 'hit_rate_up',
        name: '명중 강화',
        desc: '모든 명중률 +5%',
        color: '#ffaa44',
        apply(state) {
            state.bonusHitRate = (state.bonusHitRate || 0) + 0.05;
        }
    },
    {
        id: 'crit_chance',
        name: '치명타 확률',
        desc: '치명타 확률 +10%',
        color: '#ffff44',
        apply(state) {
            state.critRate = (state.critRate || 0.05) + 0.1;
        }
    },
    {
        id: 'crit_damage',
        name: '치명타 피해',
        desc: '치명타 배율 +0.5x',
        color: '#ffcc00',
        apply(state) {
            state.critDmg = (state.critDmg || 1.5) + 0.5;
        }
    },
    {
        id: 'max_hp_up',
        name: '체력 강화',
        desc: '최대 HP +20 및 회복',
        color: '#44ff44',
        apply(state) {
            state.maxHp += 20;
            state.hp = Math.min(state.hp + 20, state.maxHp);
        }
    },
    {
        id: 'stamina_up',
        name: '스태미나 강화',
        desc: '최대 스태미나 +15',
        color: '#44ccff',
        apply(state) {
            state.maxStamina += 15;
            state.stamina = Math.min(state.stamina + 15, state.maxStamina);
        }
    },
    {
        id: 'stamina_regen',
        name: '스태미나 회복',
        desc: '매 턴 스태미나 +3 회복',
        color: '#88ccff',
        apply(state) {
            state.staminaRegen = (state.staminaRegen || 0) + 3;
        }
    },
    {
        id: 'dodge_up',
        name: '회피 강화',
        desc: '회피 성공률 +10%',
        color: '#cc88ff',
        apply(state) {
            state.bonusDodge = (state.bonusDodge || 0) + 0.10;
        }
    },
    {
        id: 'defend_up',
        name: '방어 강화',
        desc: '방어 시 피해 감소 60% → 70%',
        color: '#88aaff',
        apply(state) {
            state.defendReduction = (state.defendReduction || 0.5) + 0.1;
        }
    },
    {
        id: 'counter',
        name: '반격',
        desc: '방어 성공 시 30% 확률 반격 (5~10 피해)',
        color: '#ff8844',
        apply(state) {
            state.counterRate = (state.counterRate || 0) + 0.30;
            state.counterDmgMin = 5;
            state.counterDmgMax = 10;
        }
    },
    {
        id: 'poison',
        name: '독 공격',
        desc: '공격 성공 시 독 부여 (3턴간 턴당 3 피해)',
        color: '#44ff00',
        apply(state) {
            state.poisonDmg = (state.poisonDmg || 0) + 3;
            state.poisonDuration = 3;
        }
    },
    {
        id: 'lifesteal',
        name: '흡혈',
        desc: '가한 피해의 10%를 HP 회복',
        color: '#ff4488',
        apply(state) {
            state.lifesteal = (state.lifesteal || 0) + 0.10;
        }
    },
    {
        id: 'kill_heal',
        name: '처치 흡수',
        desc: '적 처치 시 HP 8 회복',
        color: '#88ff88',
        apply(state) {
            state.onKillHeal = (state.onKillHeal || 0) + 8;
        }
    },
    {
        id: 'focus_boost',
        name: '집중 강화',
        desc: '집중 성공 시 3배 피해 (기본 2배)',
        color: '#aaddff',
        apply(state) {
            state.focusMultiplier = (state.focusMultiplier || 2) + 1;
        }
    },
    {
        id: 'low_hp_dmg',
        name: '배수진',
        desc: 'HP 30% 이하 시 피해 +50%',
        color: '#ff0044',
        apply(state) {
            state.desperatePower = true;
        }
    },
    {
        id: 'glass_cannon',
        name: '유리 대포',
        desc: 'HP -30%, 모든 피해 +8',
        color: '#ff2244',
        apply(state) {
            state.maxHp = Math.floor(state.maxHp * 0.7);
            state.hp = Math.min(state.hp, state.maxHp);
            state.bonusDmg = (state.bonusDmg || 0) + 8;
        }
    },
    {
        id: 'lucky',
        name: '행운',
        desc: '모든 행동 성공률 +3%, 치명타 +5%',
        color: '#ffdd88',
        apply(state) {
            state.bonusHitRate = (state.bonusHitRate || 0) + 0.03;
            state.critRate = (state.critRate || 0.05) + 0.05;
        }
    },
    {
        id: 'stamina_efficiency',
        name: '효율 강화',
        desc: '모든 스태미나 소모 -2',
        color: '#66ddff',
        apply(state) {
            state.staminaDiscount = (state.staminaDiscount || 0) + 2;
        }
    },
    {
        id: 'dodge_counter',
        name: '회피 반격',
        desc: '회피 성공 시 자동 반격 (8~15 피해)',
        color: '#ff8800',
        apply(state) {
            state.dodgeCounter = true;
            state.dodgeCounterMin = 8;
            state.dodgeCounterMax = 15;
        }
    },
    {
        id: 'shield',
        name: '보호막',
        desc: '3층마다 전투 시작 시 피격 1회 무효화',
        color: '#44aaff',
        apply(state) {
            state.hasShield = true;
        }
    }
];
