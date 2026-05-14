const GOOD_TRAITS = [
    { id: 'tough',     name: '강인함',  desc: '최대HP +15%',              apply: s => { s.hp = Math.floor(s.hp * 1.15); } },
    { id: 'sharp',     name: '예리함',  desc: 'ATK +10%',                apply: s => { s.atk = Math.floor(s.atk * 1.10); } },
    { id: 'swift',     name: '민첩함',  desc: '공속 10% 빠름',            apply: s => { s.attackSpeed = Math.floor(s.attackSpeed * 0.90); } },
    { id: 'resilient', name: '회복력',  desc: '전투시작시 HP 10% 회복',    apply: null },
    { id: 'lucky',     name: '행운',    desc: '크리확률 +10%',            apply: s => { s.critRate = Math.min(1, s.critRate + 0.10); } },
    { id: 'veteran',   name: '숙련됨',  desc: 'DEF +20%',                apply: s => { s.def = Math.floor(s.def * 1.20); } },
    { id: 'focused',   name: '집중력',  desc: '스킬쿨 15% 감소',          apply: null },
    { id: 'brave',     name: '용감함',  desc: 'HP 30%↓시 ATK +25%',      apply: null },
    { id: 'enduring',  name: '끈질김',  desc: '크리피해 20% 감소',         apply: null },
    { id: 'inspiring', name: '고무적',  desc: '인접 아군 ATK +5',         apply: null }
];

const BAD_TRAITS = [
    { id: 'fragile',  name: '허약함',   desc: '최대HP -10%',              apply: s => { s.hp = Math.floor(s.hp * 0.90); } },
    { id: 'clumsy',   name: '둔함',     desc: '공속 10% 느림',            apply: s => { s.attackSpeed = Math.floor(s.attackSpeed * 1.10); } },
    { id: 'cowardly', name: '겁쟁이',   desc: 'HP 50%↓시 ATK -15%',      apply: null },
    { id: 'unlucky',  name: '불운',     desc: '크리확률 -5%',             apply: s => { s.critRate = Math.max(0, s.critRate - 0.05); } },
    { id: 'reckless', name: '무모함',   desc: 'DEF -15%',                apply: s => { s.def = Math.floor(s.def * 0.85); } },
    { id: 'lazy',     name: '태만',     desc: '스킬쿨 20% 증가',          apply: null },
    { id: 'jinxed',   name: '저주받음', desc: '전투시작시 HP -5%',         apply: null }
];

const ALL_TRAITS = [...GOOD_TRAITS, ...BAD_TRAITS];

function rollRandomTrait() {
    if (Math.random() < 0.6) {
        return GOOD_TRAITS[Math.floor(Math.random() * GOOD_TRAITS.length)];
    } else {
        return BAD_TRAITS[Math.floor(Math.random() * BAD_TRAITS.length)];
    }
}
