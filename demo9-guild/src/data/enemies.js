const ENEMY_DATA = {
    // --- BLOOD PIT 적 ---
    runner: {
        name: '러너', role: 'melee', color: 0xcc4444, zone: 'bloodpit',
        hp: 70, atk: 10, def: 2, attackSpeed: 1300, range: 50, moveSpeed: 110,
        critRate: 0.05, critDmg: 1.5
    },
    bruiser: {
        name: '브루저', role: 'melee', color: 0x884422, zone: 'bloodpit',
        hp: 220, atk: 20, def: 10, attackSpeed: 2200, range: 55, moveSpeed: 50,
        critRate: 0.05, critDmg: 1.5
    },
    spitter: {
        name: '스피터', role: 'ranged', color: 0x44aa44, zone: 'bloodpit',
        hp: 60, atk: 14, def: 2, attackSpeed: 1600, range: 250, moveSpeed: 60,
        critRate: 0.08, critDmg: 1.5, bleedChance: 0.15
    },
    summoner: {
        name: '소환사', role: 'ranged', color: 0x8844cc, zone: 'bloodpit',
        hp: 90, atk: 8, def: 3, attackSpeed: 2000, range: 200, moveSpeed: 40,
        critRate: 0.05, critDmg: 1.3
    },
    elite_runner: {
        name: '정예 러너', role: 'melee', color: 0xff4444, zone: 'bloodpit',
        hp: 280, atk: 25, def: 7, attackSpeed: 1000, range: 50, moveSpeed: 130,
        critRate: 0.15, critDmg: 1.8, isElite: true
    },
    elite_bruiser: {
        name: '정예 브루저', role: 'melee', color: 0xcc6633, zone: 'bloodpit',
        hp: 600, atk: 38, def: 16, attackSpeed: 2000, range: 55, moveSpeed: 55,
        critRate: 0.10, critDmg: 1.6, isElite: true
    },
    pitlord: {
        name: '핏로드', role: 'melee', color: 0xff0000, zone: 'bloodpit',
        hp: 1500, atk: 45, def: 15, attackSpeed: 1800, range: 60, moveSpeed: 70,
        critRate: 0.15, critDmg: 2.0, isBoss: true
    },

    // --- CARGO 적 ---
    mechling: {
        name: '기계 졸병', role: 'melee', color: 0xcc8844, zone: 'cargo',
        hp: 90, atk: 12, def: 6, attackSpeed: 1500, range: 50, moveSpeed: 80,
        critRate: 0.05, critDmg: 1.4
    },
    turret: {
        name: '자동 포탑', role: 'ranged', color: 0xddaa33, zone: 'cargo',
        hp: 120, atk: 18, def: 12, attackSpeed: 1800, range: 300, moveSpeed: 0,
        critRate: 0.10, critDmg: 1.6
    },
    shielder: {
        name: '방패병', role: 'melee', color: 0x6688aa, zone: 'cargo',
        hp: 300, atk: 10, def: 20, attackSpeed: 2200, range: 50, moveSpeed: 45,
        critRate: 0.03, critDmg: 1.3
    },
    bomber: {
        name: '폭파병', role: 'ranged', color: 0xff8800, zone: 'cargo',
        hp: 50, atk: 30, def: 1, attackSpeed: 2500, range: 200, moveSpeed: 90,
        critRate: 0.20, critDmg: 2.0
    },
    elite_shielder: {
        name: '정예 방패병', role: 'melee', color: 0x88aacc, zone: 'cargo',
        hp: 650, atk: 22, def: 28, attackSpeed: 2000, range: 55, moveSpeed: 50,
        critRate: 0.08, critDmg: 1.5, isElite: true
    },
    elite_turret: {
        name: '정예 포탑', role: 'ranged', color: 0xffcc44, zone: 'cargo',
        hp: 350, atk: 35, def: 18, attackSpeed: 1400, range: 350, moveSpeed: 0,
        critRate: 0.15, critDmg: 1.8, isElite: true
    },
    ironclad: {
        name: '아이언클래드', role: 'melee', color: 0xff8844, zone: 'cargo',
        hp: 1800, atk: 35, def: 25, attackSpeed: 2200, range: 60, moveSpeed: 40,
        critRate: 0.10, critDmg: 1.8, isBoss: true
    },

    // --- BLACKOUT 적 ---
    wraith: {
        name: '레이스', role: 'melee', color: 0x8844cc, zone: 'blackout',
        hp: 55, atk: 16, def: 1, attackSpeed: 1000, range: 50, moveSpeed: 140,
        critRate: 0.15, critDmg: 2.0
    },
    cursed_mage: {
        name: '저주술사', role: 'ranged', color: 0xaa44ff, zone: 'blackout',
        hp: 70, atk: 22, def: 3, attackSpeed: 2000, range: 280, moveSpeed: 50,
        critRate: 0.12, critDmg: 1.8, bleedChance: 0.25
    },
    bone_golem: {
        name: '뼈 골렘', role: 'melee', color: 0xccccaa, zone: 'blackout',
        hp: 400, atk: 18, def: 8, attackSpeed: 2500, range: 55, moveSpeed: 35,
        critRate: 0.05, critDmg: 1.5
    },
    shade: {
        name: '그림자', role: 'melee', color: 0x443366, zone: 'blackout',
        hp: 40, atk: 25, def: 0, attackSpeed: 800, range: 50, moveSpeed: 160,
        critRate: 0.25, critDmg: 2.2
    },
    elite_wraith: {
        name: '정예 레이스', role: 'melee', color: 0xbb66ff, zone: 'blackout',
        hp: 350, atk: 35, def: 5, attackSpeed: 900, range: 50, moveSpeed: 150,
        critRate: 0.25, critDmg: 2.2, isElite: true
    },
    elite_cursed: {
        name: '정예 저주술사', role: 'ranged', color: 0xcc88ff, zone: 'blackout',
        hp: 250, atk: 42, def: 8, attackSpeed: 1600, range: 300, moveSpeed: 55,
        critRate: 0.18, critDmg: 2.0, isElite: true, bleedChance: 0.35
    },
    lich_king: {
        name: '리치 킹', role: 'ranged', color: 0x8844ff, zone: 'blackout',
        hp: 1200, atk: 50, def: 10, attackSpeed: 1600, range: 250, moveSpeed: 60,
        critRate: 0.20, critDmg: 2.5, isBoss: true, bleedChance: 0.30
    },

    // --- BLACKOUT 추가 적 ---
    curse_avatar: {
        name: '저주 화신', role: 'melee', color: 0xcc00ff, zone: 'blackout',
        hp: 800, atk: 45, def: 12, attackSpeed: 1100, range: 55, moveSpeed: 120,
        critRate: 0.20, critDmg: 2.0, isElite: true
    },
    mansion_lord: {
        name: '저택 주인', role: 'melee', color: 0xff00cc, zone: 'blackout',
        hp: 2000, atk: 55, def: 18, attackSpeed: 1800, range: 60, moveSpeed: 50,
        critRate: 0.18, critDmg: 2.2, isBoss: true, bleedChance: 0.20
    }
};

function getMaxRounds(zoneLevel) {
    // v2 밸런스: 구역 레벨 1-99 지원. 라운드는 cap 15.
    // Lv1=3, Lv2=4, Lv3=5, Lv5=6, Lv10=8, Lv20=10, Lv50=14, Lv99=15
    if (zoneLevel <= 5) return 3 + (zoneLevel - 1);  // 3-7
    if (zoneLevel <= 20) return 7 + Math.floor((zoneLevel - 5) / 3);  // 8-12
    return Math.min(15, 12 + Math.floor((zoneLevel - 20) / 15));  // 12-15
}

function getEnemyComposition(round, zoneLevel, zoneKey) {
    const maxRounds = getMaxRounds(zoneLevel);
    const progress = round / maxRounds;
    // v2 밸런스: Lv1 0.75배, Lv2 0.90배, Lv3+ 점진 증가, Lv99 약 6.7배
    let scaleMult;
    if (zoneLevel === 1) scaleMult = 0.75;
    else if (zoneLevel === 2) scaleMult = 0.90;
    else scaleMult = 1 + (zoneLevel - 3) * 0.06;
    let composition;

    if (zoneKey === 'cargo') {
        composition = _getCargoComposition(round, maxRounds, progress, zoneLevel);
    } else if (zoneKey === 'blackout') {
        composition = _getBlackoutComposition(round, maxRounds, progress, zoneLevel);
    } else {
        composition = _getBloodpitComposition(round, maxRounds, progress, zoneLevel);
    }

    return { composition, scaleMult };
}

function _getBloodpitComposition(round, maxRounds, progress, zoneLevel) {
    if (round >= maxRounds) {
        if (zoneLevel <= 1) return [{ type: 'runner', count: 2 }];
        if (zoneLevel <= 2) return [{ type: 'runner', count: 3 }];
        return [{ type: 'elite_runner', count: 1 }, { type: 'runner', count: 2 }];
    }
    if (progress <= 0.25) {
        return [{ type: 'runner', count: 1 + Math.min(round, 2) }];
    }
    if (progress <= 0.5) {
        const comp = [{ type: 'runner', count: 2 }, { type: 'spitter', count: 1 }];
        if (zoneLevel >= 2) comp.push({ type: 'bruiser', count: 1 });
        if (zoneLevel >= 3) comp.push({ type: 'summoner', count: 1 });
        return comp;
    }
    if (progress <= 0.75) {
        const comp = [{ type: 'bruiser', count: 1 }, { type: 'spitter', count: 1 }, { type: 'runner', count: 2 }];
        if (zoneLevel >= 2) comp.push({ type: 'summoner', count: 1 });
        if (zoneLevel >= 3) comp[0].count += 1;
        return comp;
    }
    const comp = [{ type: 'bruiser', count: 2 }, { type: 'spitter', count: 1 }];
    if (zoneLevel >= 2) comp.push({ type: 'elite_runner', count: 1 });
    if (zoneLevel >= 3) comp.push({ type: 'summoner', count: 1 });
    return comp;
}

function _getCargoComposition(round, maxRounds, progress, zoneLevel) {
    if (round >= maxRounds) {
        if (zoneLevel <= 1) return [{ type: 'mechling', count: 2 }];
        if (zoneLevel <= 2) return [{ type: 'shielder', count: 1 }, { type: 'turret', count: 1 }];
        return [{ type: 'elite_shielder', count: 1 }, { type: 'turret', count: 2 }];
    }
    if (progress <= 0.25) {
        return [{ type: 'mechling', count: 2 + Math.min(round, 1) }];
    }
    if (progress <= 0.5) {
        const comp = [{ type: 'mechling', count: 2 }, { type: 'turret', count: 1 }];
        if (zoneLevel >= 2) comp.push({ type: 'shielder', count: 1 });
        return comp;
    }
    if (progress <= 0.75) {
        const comp = [{ type: 'shielder', count: 1 }, { type: 'turret', count: 1 }, { type: 'mechling', count: 2 }];
        if (zoneLevel >= 2) comp.push({ type: 'bomber', count: 1 });
        if (zoneLevel >= 3) comp.push({ type: 'elite_turret', count: 1 });
        return comp;
    }
    const comp = [{ type: 'shielder', count: 2 }, { type: 'bomber', count: 1 }];
    if (zoneLevel >= 2) comp.push({ type: 'elite_shielder', count: 1 });
    if (zoneLevel >= 3) comp.push({ type: 'turret', count: 2 });
    return comp;
}

function _getBlackoutComposition(round, maxRounds, progress, zoneLevel) {
    if (round >= maxRounds) {
        if (zoneLevel <= 1) return [{ type: 'wraith', count: 2 }];
        if (zoneLevel <= 2) return [{ type: 'wraith', count: 2 }, { type: 'cursed_mage', count: 1 }];
        return [{ type: 'elite_wraith', count: 1 }, { type: 'cursed_mage', count: 2 }];
    }
    if (progress <= 0.25) {
        return [{ type: 'wraith', count: 2 + Math.min(round, 1) }];
    }
    if (progress <= 0.5) {
        const comp = [{ type: 'wraith', count: 2 }, { type: 'cursed_mage', count: 1 }];
        if (zoneLevel >= 2) comp.push({ type: 'bone_golem', count: 1 });
        return comp;
    }
    if (progress <= 0.75) {
        const comp = [{ type: 'bone_golem', count: 1 }, { type: 'cursed_mage', count: 1 }, { type: 'shade', count: 2 }];
        if (zoneLevel >= 2) comp.push({ type: 'wraith', count: 1 });
        if (zoneLevel >= 3) comp.push({ type: 'elite_cursed', count: 1 });
        return comp;
    }
    const comp = [{ type: 'bone_golem', count: 1 }, { type: 'shade', count: 2 }, { type: 'cursed_mage', count: 1 }];
    if (zoneLevel >= 2) comp.push({ type: 'elite_wraith', count: 1 });
    if (zoneLevel >= 3) comp.push({ type: 'elite_cursed', count: 1 });
    return comp;
}

function isBossRound(round, zoneLevel) {
    return round >= getMaxRounds(zoneLevel);
}

function getZoneBoss(zoneKey) {
    const bosses = { bloodpit: 'pitlord', cargo: 'ironclad', blackout: 'lich_king' };
    return bosses[zoneKey] || 'pitlord';
}

function getBossScaleMult(zoneLevel) {
    const table = { 1: 0.5, 2: 0.65, 3: 0.8, 4: 0.9, 5: 1.0 };
    return table[zoneLevel] || (1.0 + (zoneLevel - 5) * 0.1);
}
