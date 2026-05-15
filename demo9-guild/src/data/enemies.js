const ENEMY_DATA = {
    runner: {
        name: '러너', role: 'melee', color: 0xcc4444,
        hp: 70, atk: 10, def: 2, attackSpeed: 1300, range: 50, moveSpeed: 110,
        critRate: 0.05, critDmg: 1.5
    },
    bruiser: {
        name: '브루저', role: 'melee', color: 0x884422,
        hp: 220, atk: 20, def: 10, attackSpeed: 2200, range: 55, moveSpeed: 50,
        critRate: 0.05, critDmg: 1.5
    },
    spitter: {
        name: '스피터', role: 'ranged', color: 0x44aa44,
        hp: 60, atk: 14, def: 2, attackSpeed: 1600, range: 250, moveSpeed: 60,
        critRate: 0.08, critDmg: 1.5, bleedChance: 0.15
    },
    summoner: {
        name: '소환사', role: 'ranged', color: 0x8844cc,
        hp: 90, atk: 8, def: 3, attackSpeed: 2000, range: 200, moveSpeed: 40,
        critRate: 0.05, critDmg: 1.3
    },
    elite_runner: {
        name: '정예 러너', role: 'melee', color: 0xff4444,
        hp: 280, atk: 25, def: 7, attackSpeed: 1000, range: 50, moveSpeed: 130,
        critRate: 0.15, critDmg: 1.8, isElite: true
    },
    elite_bruiser: {
        name: '정예 브루저', role: 'melee', color: 0xcc6633,
        hp: 600, atk: 38, def: 16, attackSpeed: 2000, range: 55, moveSpeed: 55,
        critRate: 0.10, critDmg: 1.6, isElite: true
    },
    pitlord: {
        name: '핏로드', role: 'melee', color: 0xff0000,
        hp: 1500, atk: 45, def: 15, attackSpeed: 1800, range: 60, moveSpeed: 70,
        critRate: 0.15, critDmg: 2.0, isBoss: true
    }
};

function getMaxRounds(zoneLevel) {
    return Math.min(10, 4 + Math.floor((zoneLevel - 1) * 1.5));
}

function getEnemyComposition(round, zoneLevel) {
    const maxRounds = getMaxRounds(zoneLevel);
    const progress = round / maxRounds;
    const scaleMult = 1 + (zoneLevel - 1) * 0.1;
    let composition;

    if (round >= maxRounds) {
        // boss round
        composition = [{ type: 'elite_runner', count: 1 }, { type: 'runner', count: 2 }];
    } else if (progress <= 0.25) {
        // early
        const r = Math.min(round, 2);
        composition = [{ type: 'runner', count: 1 + r }];
    } else if (progress <= 0.5) {
        // mid-early
        composition = [{ type: 'runner', count: 2 }, { type: 'spitter', count: 1 }];
        if (zoneLevel >= 2) composition.push({ type: 'bruiser', count: 1 });
        if (zoneLevel >= 3) composition.push({ type: 'summoner', count: 1 });
    } else if (progress <= 0.75) {
        // mid-late
        composition = [{ type: 'bruiser', count: 1 }, { type: 'spitter', count: 1 }, { type: 'runner', count: 2 }];
        if (zoneLevel >= 2) composition.push({ type: 'summoner', count: 1 });
        if (zoneLevel >= 3) composition[0].count += 1;
    } else {
        // pre-boss
        composition = [{ type: 'bruiser', count: 2 }, { type: 'spitter', count: 1 }];
        if (zoneLevel >= 2) composition.push({ type: 'elite_runner', count: 1 });
        if (zoneLevel >= 3) composition.push({ type: 'summoner', count: 1 });
    }

    return { composition, scaleMult };
}

function isBossRound(round, zoneLevel) {
    return round >= getMaxRounds(zoneLevel);
}
