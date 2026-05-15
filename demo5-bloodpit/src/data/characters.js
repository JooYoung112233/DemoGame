const BP_ALLIES = {
    // ─── Tier 1: Base Classes ───
    warrior: { name: '전사', tier: 1, hp: 450, atk: 22, def: 12, attackSpeed: 1800, range: 60, moveSpeed: 85, critRate: 0.08, critDmg: 1.5, color: 0x4488ff, role: 'tank', bleedChance: 0, dodgeRate: 0.05 },
    rogue: { name: '도적', tier: 1, hp: 160, atk: 35, def: 3, attackSpeed: 1000, range: 55, moveSpeed: 140, critRate: 0.25, critDmg: 2.2, color: 0xff4444, role: 'dps', bleedChance: 0.15, dodgeRate: 0.15 },
    priest: { name: '사제', tier: 1, hp: 200, atk: 12, def: 5, attackSpeed: 2500, range: 200, moveSpeed: 70, critRate: 0.05, critDmg: 1.5, color: 0x44ff88, role: 'healer', bleedChance: 0, dodgeRate: 0.05 },
    mage: { name: '마법사', tier: 1, hp: 140, atk: 50, def: 2, attackSpeed: 2600, range: 220, moveSpeed: 65, critRate: 0.12, critDmg: 1.8, color: 0xaa44ff, role: 'dps', bleedChance: 0, dodgeRate: 0.05 },
    ranger: { name: '궁수', tier: 1, hp: 170, atk: 30, def: 3, attackSpeed: 1600, range: 200, moveSpeed: 90, critRate: 0.18, critDmg: 2.0, color: 0x88cc44, role: 'dps', bleedChance: 0, dodgeRate: 0.10 },
    paladin: { name: '성기사', tier: 1, hp: 380, atk: 18, def: 10, attackSpeed: 2000, range: 60, moveSpeed: 75, critRate: 0.06, critDmg: 1.5, color: 0xffdd44, role: 'tank', bleedChance: 0, dodgeRate: 0.03 },
    assassin: { name: '암살자', tier: 1, hp: 120, atk: 45, def: 1, attackSpeed: 800, range: 55, moveSpeed: 160, critRate: 0.30, critDmg: 2.5, color: 0x8844aa, role: 'dps', bleedChance: 0.20, dodgeRate: 0.20 },
    shaman: { name: '주술사', tier: 1, hp: 220, atk: 15, def: 4, attackSpeed: 2200, range: 180, moveSpeed: 68, critRate: 0.08, critDmg: 1.5, color: 0x44aaaa, role: 'healer', bleedChance: 0, dodgeRate: 0.05 },

    // ─── Tier 2: Advanced Classes ───
    berserker: { name: '광전사', tier: 2, hp: 520, atk: 40, def: 8, attackSpeed: 1500, range: 65, moveSpeed: 100, critRate: 0.15, critDmg: 1.8, color: 0x6699ff, role: 'tank', bleedChance: 0.10, dodgeRate: 0.08, lifesteal: 0.12 },
    guardian: { name: '수호자', tier: 2, hp: 600, atk: 20, def: 18, attackSpeed: 2000, range: 60, moveSpeed: 70, critRate: 0.06, critDmg: 1.5, color: 0x3366cc, role: 'tank', bleedChance: 0, dodgeRate: 0.03 },
    blade_master: { name: '검성', tier: 2, hp: 210, atk: 48, def: 5, attackSpeed: 850, range: 60, moveSpeed: 150, critRate: 0.32, critDmg: 2.5, color: 0xff6666, role: 'dps', bleedChance: 0.20, dodgeRate: 0.18 },
    shadow: { name: '그림자', tier: 2, hp: 140, atk: 38, def: 2, attackSpeed: 900, range: 55, moveSpeed: 170, critRate: 0.35, critDmg: 2.8, color: 0xcc3355, role: 'dps', bleedChance: 0.10, dodgeRate: 0.30 },
    bishop: { name: '주교', tier: 2, hp: 260, atk: 18, def: 7, attackSpeed: 2200, range: 220, moveSpeed: 75, critRate: 0.08, critDmg: 1.5, color: 0x66ffaa, role: 'healer', bleedChance: 0, dodgeRate: 0.08 },
    battle_priest: { name: '전투사제', tier: 2, hp: 320, atk: 25, def: 9, attackSpeed: 1800, range: 80, moveSpeed: 80, critRate: 0.10, critDmg: 1.6, color: 0x33cc88, role: 'healer', bleedChance: 0.05, dodgeRate: 0.06 },
    archmage: { name: '대마법사', tier: 2, hp: 180, atk: 70, def: 3, attackSpeed: 2400, range: 240, moveSpeed: 70, critRate: 0.15, critDmg: 2.0, color: 0xcc66ff, role: 'dps', bleedChance: 0, dodgeRate: 0.08 },
    warlock: { name: '흑마법사', tier: 2, hp: 200, atk: 55, def: 4, attackSpeed: 2200, range: 200, moveSpeed: 65, critRate: 0.10, critDmg: 1.8, color: 0x9933cc, role: 'dps', bleedChance: 0.15, dodgeRate: 0.06, lifesteal: 0.08 },
    sniper: { name: '저격수', tier: 2, hp: 200, atk: 42, def: 4, attackSpeed: 1800, range: 280, moveSpeed: 85, critRate: 0.25, critDmg: 2.8, color: 0xaaee55, role: 'dps', bleedChance: 0, dodgeRate: 0.12 },
    wind_ranger: { name: '바람궁수', tier: 2, hp: 190, atk: 32, def: 3, attackSpeed: 1200, range: 220, moveSpeed: 110, critRate: 0.20, critDmg: 2.0, color: 0x77cc33, role: 'dps', bleedChance: 0, dodgeRate: 0.18 },
    crusader: { name: '십자군', tier: 2, hp: 480, atk: 25, def: 14, attackSpeed: 1800, range: 65, moveSpeed: 80, critRate: 0.10, critDmg: 1.6, color: 0xffee66, role: 'tank', bleedChance: 0, dodgeRate: 0.05 },
    templar: { name: '기사단장', tier: 2, hp: 420, atk: 22, def: 12, attackSpeed: 1900, range: 60, moveSpeed: 78, critRate: 0.08, critDmg: 1.5, color: 0xddcc44, role: 'tank', bleedChance: 0, dodgeRate: 0.04 },
    phantom: { name: '환영', tier: 2, hp: 150, atk: 58, def: 2, attackSpeed: 700, range: 60, moveSpeed: 180, critRate: 0.38, critDmg: 3.0, color: 0xaa66cc, role: 'dps', bleedChance: 0.25, dodgeRate: 0.30 },
    nightblade: { name: '나이트블레이드', tier: 2, hp: 130, atk: 50, def: 1, attackSpeed: 750, range: 55, moveSpeed: 165, critRate: 0.28, critDmg: 2.6, color: 0x7744aa, role: 'dps', bleedChance: 0.30, dodgeRate: 0.22, lifesteal: 0.05 },
    witch_doctor: { name: '위치닥터', tier: 2, hp: 280, atk: 22, def: 6, attackSpeed: 2000, range: 200, moveSpeed: 72, critRate: 0.10, critDmg: 1.6, color: 0x66cccc, role: 'healer', bleedChance: 0.15, dodgeRate: 0.08 },
    spirit_walker: { name: '영혼술사', tier: 2, hp: 240, atk: 18, def: 5, attackSpeed: 2100, range: 190, moveSpeed: 70, critRate: 0.08, critDmg: 1.5, color: 0x44bbbb, role: 'healer', bleedChance: 0, dodgeRate: 0.10 },

    // ─── Tier 3: Master Classes ───
    warlord: { name: '워로드', tier: 3, hp: 700, atk: 55, def: 10, attackSpeed: 1400, range: 70, moveSpeed: 110, critRate: 0.20, critDmg: 2.0, color: 0x5588ee, role: 'tank', bleedChance: 0.15, dodgeRate: 0.10, lifesteal: 0.15 },
    iron_wall: { name: '철벽', tier: 3, hp: 850, atk: 22, def: 25, attackSpeed: 2200, range: 60, moveSpeed: 60, critRate: 0.05, critDmg: 1.5, color: 0x2255aa, role: 'tank', bleedChance: 0, dodgeRate: 0.02 },
    sword_saint: { name: '검신', tier: 3, hp: 250, atk: 65, def: 6, attackSpeed: 750, range: 65, moveSpeed: 160, critRate: 0.40, critDmg: 3.0, color: 0xff5555, role: 'dps', bleedChance: 0.25, dodgeRate: 0.22 },
    reaper: { name: '사신', tier: 3, hp: 160, atk: 50, def: 2, attackSpeed: 600, range: 60, moveSpeed: 190, critRate: 0.45, critDmg: 3.5, color: 0xcc2244, role: 'dps', bleedChance: 0.15, dodgeRate: 0.40 },
    high_priest: { name: '대사제', tier: 3, hp: 320, atk: 22, def: 9, attackSpeed: 2000, range: 240, moveSpeed: 78, critRate: 0.10, critDmg: 1.5, color: 0x55ffbb, role: 'healer', bleedChance: 0, dodgeRate: 0.10 },
    inquisitor: { name: '심판관', tier: 3, hp: 400, atk: 35, def: 12, attackSpeed: 1600, range: 100, moveSpeed: 85, critRate: 0.15, critDmg: 1.8, color: 0x22bb88, role: 'healer', bleedChance: 0.10, dodgeRate: 0.08 },
    void_mage: { name: '공허술사', tier: 3, hp: 220, atk: 90, def: 4, attackSpeed: 2200, range: 260, moveSpeed: 72, critRate: 0.18, critDmg: 2.2, color: 0xbb55ee, role: 'dps', bleedChance: 0, dodgeRate: 0.10 },
    blood_warlock: { name: '혈마법사', tier: 3, hp: 260, atk: 72, def: 5, attackSpeed: 2000, range: 220, moveSpeed: 68, critRate: 0.12, critDmg: 2.0, color: 0x882299, role: 'dps', bleedChance: 0.20, dodgeRate: 0.08, lifesteal: 0.12 },
    marksman: { name: '명사수', tier: 3, hp: 240, atk: 55, def: 5, attackSpeed: 1600, range: 320, moveSpeed: 90, critRate: 0.30, critDmg: 3.2, color: 0x99dd44, role: 'dps', bleedChance: 0, dodgeRate: 0.15 },
    tempest: { name: '폭풍궁수', tier: 3, hp: 220, atk: 40, def: 4, attackSpeed: 1000, range: 240, moveSpeed: 120, critRate: 0.22, critDmg: 2.2, color: 0x66bb22, role: 'dps', bleedChance: 0, dodgeRate: 0.22 },
    holy_knight: { name: '성기사단장', tier: 3, hp: 600, atk: 32, def: 18, attackSpeed: 1700, range: 70, moveSpeed: 85, critRate: 0.12, critDmg: 1.7, color: 0xeedd33, role: 'tank', bleedChance: 0, dodgeRate: 0.06 },
    avenger: { name: '복수자', tier: 3, hp: 500, atk: 30, def: 15, attackSpeed: 1800, range: 65, moveSpeed: 82, critRate: 0.10, critDmg: 1.6, color: 0xccbb22, role: 'tank', bleedChance: 0.08, dodgeRate: 0.05 },
    death_shadow: { name: '죽음의그림자', tier: 3, hp: 180, atk: 75, def: 3, attackSpeed: 650, range: 65, moveSpeed: 200, critRate: 0.42, critDmg: 3.5, color: 0x9955bb, role: 'dps', bleedChance: 0.30, dodgeRate: 0.35, lifesteal: 0.10 },
    venom_lord: { name: '맹독군주', tier: 3, hp: 170, atk: 62, def: 2, attackSpeed: 700, range: 60, moveSpeed: 175, critRate: 0.32, critDmg: 2.8, color: 0x663399, role: 'dps', bleedChance: 0.40, dodgeRate: 0.25, lifesteal: 0.08 },
    arch_shaman: { name: '대주술사', tier: 3, hp: 340, atk: 28, def: 7, attackSpeed: 1900, range: 220, moveSpeed: 75, critRate: 0.12, critDmg: 1.7, color: 0x55dddd, role: 'healer', bleedChance: 0.20, dodgeRate: 0.10 },
    soul_keeper: { name: '영혼수호자', tier: 3, hp: 300, atk: 22, def: 6, attackSpeed: 2000, range: 210, moveSpeed: 72, critRate: 0.10, critDmg: 1.5, color: 0x33aaaa, role: 'healer', bleedChance: 0, dodgeRate: 0.12, lifesteal: 0.06 }
};

const BP_CLASS_POOL = ['warrior', 'rogue', 'priest', 'mage', 'ranger', 'paladin', 'assassin', 'shaman'];

// 3-tier branching promotion tree
const BP_PROMOTIONS = {
    // Tier 1 → Tier 2 (choose one of two paths)
    warrior:  { paths: [
        { advancedClass: 'berserker', cost: 200, minLevel: 5, label: '광전사 (공격형)' },
        { advancedClass: 'guardian', cost: 200, minLevel: 5, label: '수호자 (방어형)' }
    ]},
    rogue:    { paths: [
        { advancedClass: 'blade_master', cost: 250, minLevel: 5, label: '검성 (크리형)' },
        { advancedClass: 'shadow', cost: 250, minLevel: 5, label: '그림자 (회피형)' }
    ]},
    priest:   { paths: [
        { advancedClass: 'bishop', cost: 250, minLevel: 5, label: '주교 (순수힐)' },
        { advancedClass: 'battle_priest', cost: 250, minLevel: 5, label: '전투사제 (하이브리드)' }
    ]},
    mage:     { paths: [
        { advancedClass: 'archmage', cost: 300, minLevel: 5, label: '대마법사 (순수딜)' },
        { advancedClass: 'warlock', cost: 300, minLevel: 5, label: '흑마법사 (흡혈형)' }
    ]},
    ranger:   { paths: [
        { advancedClass: 'sniper', cost: 250, minLevel: 5, label: '저격수 (원거리)' },
        { advancedClass: 'wind_ranger', cost: 250, minLevel: 5, label: '바람궁수 (속사형)' }
    ]},
    paladin:  { paths: [
        { advancedClass: 'crusader', cost: 300, minLevel: 5, label: '십자군 (철벽형)' },
        { advancedClass: 'templar', cost: 300, minLevel: 5, label: '기사단장 (균형형)' }
    ]},
    assassin: { paths: [
        { advancedClass: 'phantom', cost: 300, minLevel: 5, label: '환영 (회피+크리)' },
        { advancedClass: 'nightblade', cost: 300, minLevel: 5, label: '나이트블레이드 (출혈)' }
    ]},
    shaman:   { paths: [
        { advancedClass: 'witch_doctor', cost: 250, minLevel: 5, label: '위치닥터 (디버프)' },
        { advancedClass: 'spirit_walker', cost: 250, minLevel: 5, label: '영혼술사 (회복)' }
    ]},

    // Tier 2 → Tier 3
    berserker:    { paths: [{ advancedClass: 'warlord', cost: 500, minLevel: 10, label: '워로드' }] },
    guardian:     { paths: [{ advancedClass: 'iron_wall', cost: 500, minLevel: 10, label: '철벽' }] },
    blade_master: { paths: [{ advancedClass: 'sword_saint', cost: 600, minLevel: 10, label: '검신' }] },
    shadow:       { paths: [{ advancedClass: 'reaper', cost: 600, minLevel: 10, label: '사신' }] },
    bishop:       { paths: [{ advancedClass: 'high_priest', cost: 500, minLevel: 10, label: '대사제' }] },
    battle_priest: { paths: [{ advancedClass: 'inquisitor', cost: 500, minLevel: 10, label: '심판관' }] },
    archmage:     { paths: [{ advancedClass: 'void_mage', cost: 600, minLevel: 10, label: '공허술사' }] },
    warlock:      { paths: [{ advancedClass: 'blood_warlock', cost: 600, minLevel: 10, label: '혈마법사' }] },
    sniper:       { paths: [{ advancedClass: 'marksman', cost: 500, minLevel: 10, label: '명사수' }] },
    wind_ranger:  { paths: [{ advancedClass: 'tempest', cost: 500, minLevel: 10, label: '폭풍궁수' }] },
    crusader:     { paths: [{ advancedClass: 'holy_knight', cost: 600, minLevel: 10, label: '성기사단장' }] },
    templar:      { paths: [{ advancedClass: 'avenger', cost: 600, minLevel: 10, label: '복수자' }] },
    phantom:      { paths: [{ advancedClass: 'death_shadow', cost: 600, minLevel: 10, label: '죽음의그림자' }] },
    nightblade:   { paths: [{ advancedClass: 'venom_lord', cost: 600, minLevel: 10, label: '맹독군주' }] },
    witch_doctor: { paths: [{ advancedClass: 'arch_shaman', cost: 500, minLevel: 10, label: '대주술사' }] },
    spirit_walker: { paths: [{ advancedClass: 'soul_keeper', cost: 500, minLevel: 10, label: '영혼수호자' }] }
};

const BP_ADVANCED_CLASSES = [];
Object.values(BP_PROMOTIONS).forEach(p => p.paths.forEach(path => {
    if (!BP_ADVANCED_CLASSES.includes(path.advancedClass)) BP_ADVANCED_CLASSES.push(path.advancedClass);
}));

const BP_NAME_POOL = {
    prefix: ['강철의', '피바람', '어둠의', '붉은', '그림자', '황금', '냉혈', '폭풍의', '고독한', '잔인한', '맹독의', '불꽃', '서리', '천둥', '대지의'],
    suffix: ['바르크', '세라', '칸', '리라', '도르', '미르', '제인', '곤', '아르', '텔라', '비스', '크론', '릭스', '오딘', '발라']
};

const BP_ENEMIES = {
    runner: { name: '러너', type: 'runner', hp: 100, atk: 12, def: 2, attackSpeed: 1400, range: 55, moveSpeed: 130, critRate: 0.05, critDmg: 1.5, color: 0x88aa44, bleedChance: 0, dodgeRate: 0.1 },
    bruiser: { name: '브루저', type: 'bruiser', hp: 300, atk: 18, def: 10, attackSpeed: 2200, range: 60, moveSpeed: 50, critRate: 0.05, critDmg: 1.5, color: 0x886622, bleedChance: 0, dodgeRate: 0 },
    spitter: { name: '스피터', type: 'spitter', hp: 80, atk: 22, def: 1, attackSpeed: 2000, range: 200, moveSpeed: 60, critRate: 0.08, critDmg: 1.5, color: 0x44aa88, bleedChance: 0.2, dodgeRate: 0.05 },
    nest: { name: '둥지', type: 'nest', hp: 250, atk: 0, def: 5, attackSpeed: 99999, range: 0, moveSpeed: 0, critRate: 0, critDmg: 1, color: 0x664444, bleedChance: 0, dodgeRate: 0, isNest: true }
};

const BP_ELITES = {
    elite_runner: { name: '엘리트 러너', type: 'elite_runner', hp: 350, atk: 28, def: 5, attackSpeed: 1200, range: 60, moveSpeed: 150, critRate: 0.15, critDmg: 2.0, color: 0xccff44, bleedChance: 0.1, dodgeRate: 0.2 },
    elite_bruiser: { name: '엘리트 브루저', type: 'elite_bruiser', hp: 800, atk: 35, def: 15, attackSpeed: 2000, range: 65, moveSpeed: 55, critRate: 0.1, critDmg: 1.8, color: 0xcc8822, bleedChance: 0, dodgeRate: 0 }
};

const BP_BOSS = {
    pitlord: { name: '핏로드', type: 'boss', hp: 2000, atk: 55, def: 15, attackSpeed: 1600, range: 80, moveSpeed: 70, critRate: 0.2, critDmg: 2.5, color: 0xff2244, bleedChance: 0.3, dodgeRate: 0.1 }
};
