const BP_MILESTONES = {
    tank: {
        5:  { name: '철벽', desc: 'DEF +15%', apply(c) { c.def = Math.floor(c.def * 1.15); } },
        10: { name: '보복', desc: '가시 반사 +10', apply(c) { c.thorns = (c.thorns || 0) + 10; } },
        15: { name: '불굴', desc: 'HP +20%, 흡혈 5%', apply(c) { c.maxHp = Math.floor(c.maxHp * 1.2); c.hp = c.maxHp; c.lifesteal = (c.lifesteal || 0) + 0.05; } },
        20: { name: '수호신', desc: 'DEF +25%, 가시 +20', apply(c) { c.def = Math.floor(c.def * 1.25); c.thorns = (c.thorns || 0) + 20; } }
    },
    melee_dps: {
        5:  { name: '날카로움', desc: 'ATK +10%', apply(c) { c.atk = Math.floor(c.atk * 1.10); } },
        10: { name: '급소', desc: '크리율 +10%, 크리뎀 +0.3', apply(c) { c.critRate = Math.min(0.9, c.critRate + 0.10); c.critDmg += 0.3; } },
        15: { name: '광전사', desc: 'ATK +20%, 공속 +15%', apply(c) { c.atk = Math.floor(c.atk * 1.20); c.attackSpeed = Math.floor(c.attackSpeed * 0.85); } },
        20: { name: '처형자', desc: 'ATK +30%, 크리율 +15%', apply(c) { c.atk = Math.floor(c.atk * 1.30); c.critRate = Math.min(0.9, c.critRate + 0.15); } }
    },
    ranged_dps: {
        5:  { name: '집중', desc: '크리율 +8%', apply(c) { c.critRate = Math.min(0.9, c.critRate + 0.08); } },
        10: { name: '관통', desc: 'ATK +15%, 방무시 10%', apply(c) { c.atk = Math.floor(c.atk * 1.15); c.defReduction = (c.defReduction || 0) + 0.10; } },
        15: { name: '속사', desc: '공속 +20%, 크리뎀 +0.5', apply(c) { c.attackSpeed = Math.floor(c.attackSpeed * 0.80); c.critDmg += 0.5; } },
        20: { name: '저격수', desc: 'ATK +25%, 크리율 +20%', apply(c) { c.atk = Math.floor(c.atk * 1.25); c.critRate = Math.min(0.9, c.critRate + 0.20); } }
    },
    healer: {
        5:  { name: '축복', desc: '힐량 +30%', apply(c) { c.healAmount = Math.floor(c.healAmount * 1.30); } },
        10: { name: '보호막', desc: '아군 전원 DEF +10%', applyParty(allies) { allies.forEach(a => { a.def = Math.floor(a.def * 1.10); }); } },
        15: { name: '성역', desc: '힐량 +50%, 힐쿨 -20%', apply(c) { c.healAmount = Math.floor(c.healAmount * 1.50); c.healCooldown = Math.floor(c.healCooldown * 0.80); } },
        20: { name: '대사제', desc: '아군 전원 HP +15%, 흡혈 3%', applyParty(allies) { allies.forEach(a => { a.maxHp = Math.floor(a.maxHp * 1.15); a.hp = Math.min(a.maxHp, a.hp + Math.floor(a.maxHp * 0.15)); a.lifesteal = (a.lifesteal || 0) + 0.03; }); } }
    }
};

const BP_MILESTONE_ROLE_MAP = {
    tank: 'tank',
    warrior: 'tank', paladin: 'tank', guardian: 'tank', warlord: 'tank',
    rogue: 'melee_dps', assassin: 'melee_dps', deathshadow: 'melee_dps',
    duelist: 'melee_dps', blademaster: 'melee_dps', reaper: 'melee_dps',
    berserker: 'melee_dps', gladiator: 'melee_dps', ravager: 'melee_dps',
    mage: 'ranged_dps', warlock: 'ranged_dps', arcanist: 'ranged_dps',
    elementalist: 'ranged_dps', pyromancer: 'ranged_dps', cryomancer: 'ranged_dps',
    ranger: 'ranged_dps', marksman: 'ranged_dps', beastmaster: 'ranged_dps',
    priest: 'healer', shaman: 'healer', druid: 'healer',
    bishop: 'healer', oracle: 'healer', spiritwalker: 'healer',
    templar: 'tank', crusader: 'tank', vanguard: 'tank'
};
