const POSITIVE_TRAITS = [
    { id: 'strong_body',      name: '강인한 체격',    desc: 'HP +15%',              apply: s => { s.hp = Math.floor(s.hp * 1.15); } },
    { id: 'sharp_sense',      name: '날카로운 감각',  desc: 'CRIT +10%',            apply: s => { s.critRate += 0.10; } },
    { id: 'battle_instinct',  name: '전투 본능',      desc: 'ATK +10%',             apply: s => { s.atk = Math.floor(s.atk * 1.10); } },
    { id: 'iron_wall',        name: '철벽 수비',      desc: 'DEF +15%',             apply: s => { s.def = Math.floor(s.def * 1.15); } },
    { id: 'swift_feet',       name: '신속한 발놀림',  desc: 'SPD +15%',             apply: s => { s.moveSpeed = Math.floor(s.moveSpeed * 1.15); } },
    { id: 'quick_recovery',   name: '빠른 회복',      desc: '전투 후 HP 5% 회복',   apply: () => {} },
    { id: 'blood_fighter',    name: '피의 투사',      desc: 'Blood Pit ATK +20%',   apply: () => {}, zone: 'bloodpit' },
    { id: 'arena_hero',       name: '투기장 영웅',    desc: '적 처치 시 HP 3% 회복', apply: () => {}, zone: 'bloodpit' },
    { id: 'cargo_expert',     name: '화물 전문가',    desc: 'Cargo DEF +15%',       apply: () => {}, zone: 'cargo' },
    { id: 'guard_instinct',   name: '수호 본능',      desc: 'Cargo 위기 시 이동',    apply: () => {}, zone: 'cargo' },
    { id: 'dark_friend',      name: '어둠의 친구',    desc: 'Blackout CRIT +15%',   apply: () => {}, zone: 'blackout' },
    { id: 'curse_resist',     name: '저주 내성',      desc: '저주 상승 -30%',        apply: () => {}, zone: 'blackout' },
    { id: 'revenge',          name: '복수의 화신',    desc: '아군 사망 시 ATK +10%', apply: () => {} },
    { id: 'crisis_fighter',   name: '위기의 투사',    desc: 'HP 30%↓ ATK/CRIT ↑',  apply: () => {} },
    { id: 'cool_head',        name: '냉철한 판단',    desc: '보스전 페널티 무효',     apply: () => {} },
    { id: 'veteran',          name: '노련한 생존자',  desc: '런 시작 HP +10%',       apply: s => { s.hp = Math.floor(s.hp * 1.10); } },
    { id: 'fast_hands',       name: '빠른 손',        desc: '스킬 쿨 -15%',         apply: s => { s.skillCooldown = Math.floor((s.skillCooldown || 0) * 0.85); } },
    { id: 'comrade_love',     name: '동료 사랑',      desc: '힐 +5% 추가',          apply: () => {} },
    { id: 'loot_hunter',      name: '전리품 사냥꾼',  desc: '드랍 확률 +10%',        apply: () => {} },
    { id: 'negotiator',       name: '협상가',         desc: '귀환 시 골드 +5%',      apply: () => {} }
];

const NEGATIVE_TRAITS = [
    { id: 'weak_body',        name: '허약한 체질',    desc: 'HP -10%',              apply: s => { s.hp = Math.floor(s.hp * 0.90); } },
    { id: 'dull_sense',       name: '둔한 감각',      desc: 'CRIT -10%',            apply: s => { s.critRate = Math.max(0, s.critRate - 0.10); } },
    { id: 'slow_feet',        name: '느린 발',        desc: 'SPD -15%',             apply: s => { s.moveSpeed = Math.floor(s.moveSpeed * 0.85); } },
    { id: 'dull_attack',      name: '무딘 공격',      desc: 'ATK -8%',              apply: s => { s.atk = Math.floor(s.atk * 0.92); } },
    { id: 'thin_armor',       name: '얇은 갑옷',      desc: 'DEF -10%',             apply: s => { s.def = Math.floor(s.def * 0.90); } },
    { id: 'blood_phobia',     name: '피 공포증',      desc: 'Blood Pit ATK -15%',   apply: () => {}, zone: 'bloodpit' },
    { id: 'claustrophobia',   name: '폐쇄 공포증',    desc: 'Blackout SPD -20%',    apply: () => {}, zone: 'blackout' },
    { id: 'no_tech',          name: '기계치',         desc: 'Cargo DEF 보너스 없음', apply: () => {}, zone: 'cargo' },
    { id: 'coward',           name: '겁쟁이',         desc: '아군 사망 시 스탯 -10%', apply: () => {} },
    { id: 'greedy',           name: '욕심쟁이',       desc: '보안 컨테이너 -1칸',    apply: () => {} },
    { id: 'stubborn',         name: '고집쟁이',       desc: '시너지 효과 -5%',       apply: () => {} },
    { id: 'scarred',          name: '상처투성이',     desc: '런 시작 HP -10%',       apply: s => { s.hp = Math.floor(s.hp * 0.90); } },
    { id: 'unlucky',          name: '불운아',         desc: '드랍 희귀도 -1 확률',    apply: () => {} },
    { id: 'lone_wolf',        name: '단독 행동',      desc: '아군 3명↑ ATK -5%',    apply: () => {} },
    { id: 'slow_learner',     name: '더딘 성장',      desc: 'XP 획득 -10%',         apply: () => {} }
];

const LEGENDARY_TRAITS = {
    warrior:   { id: 'unyielding',    name: '불굴의 방패',    desc: 'HP 20%↓ 사망 대신 HP 1 (런당 1회)' },
    rogue:     { id: 'shadow_step',   name: '그림자 발걸음',  desc: '첫 공격 크리 + 출혈 확정' },
    mage:      { id: 'mana_overload', name: '마력 과부하',    desc: '스킬 추가 폭발, 쿨 +5초' },
    archer:    { id: 'hawks_eye',     name: '매의 눈',        desc: '사거리 +30%, 크리 시 2연타' },
    priest:    { id: 'holy_sacrifice',name: '성스러운 희생',  desc: '아군 사망 직전 즉시 힐 (쿨 무시)' },
    alchemist: { id: 'elixir_master', name: '비약의 대가',    desc: '물약 효과 +50%, 지속 2배' }
};

function getRandomTraits(rarity, classKey) {
    const rarityInfo = RARITY_DATA[rarity];
    const traits = [];
    const usedIds = new Set();

    const posCount = rarityInfo.posTraits;
    const available = [...POSITIVE_TRAITS];
    for (let i = 0; i < posCount; i++) {
        const pool = available.filter(t => !usedIds.has(t.id));
        if (pool.length === 0) break;
        const picked = pool[Math.floor(Math.random() * pool.length)];
        traits.push({ ...picked, type: 'positive' });
        usedIds.add(picked.id);
    }

    let negCount = rarityInfo.negTraits;
    if (negCount > 0 && negCount < 1) {
        negCount = Math.random() < negCount ? 1 : 0;
    }
    const negAvailable = [...NEGATIVE_TRAITS];
    for (let i = 0; i < negCount; i++) {
        const pool = negAvailable.filter(t => !usedIds.has(t.id));
        if (pool.length === 0) break;
        const picked = pool[Math.floor(Math.random() * pool.length)];
        traits.push({ ...picked, type: 'negative' });
        usedIds.add(picked.id);
    }

    if (rarity === 'legendary' && LEGENDARY_TRAITS[classKey]) {
        const lt = LEGENDARY_TRAITS[classKey];
        traits.push({ ...lt, type: 'legendary' });
    }

    return traits;
}
