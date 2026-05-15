class RunSimulator {
    static simulate(gameState, zoneKey, party) {
        const zone = ZONE_DATA[zoneKey];
        const zoneLevel = gameState.zoneLevel[zoneKey] || 1;
        const partySize = party.length;

        const successRate = Math.min(0.85, 0.55 + partySize * 0.08 - zoneLevel * 0.02);
        const success = Math.random() < successRate;

        const rounds = 3 + Math.floor(Math.random() * (success ? 5 : 3));
        const goldBase = zone.baseGoldReward + zoneLevel * 15;
        const xpBase = zone.baseXpReward + zoneLevel * 8;

        const goldEarned = success
            ? Math.floor(goldBase * (0.8 + Math.random() * 0.6) * rounds * 0.3)
            : Math.floor(goldBase * 0.3 * (0.5 + Math.random() * 0.5));

        const xpEarned = success
            ? Math.floor(xpBase * (0.8 + Math.random() * 0.4))
            : Math.floor(xpBase * 0.4);

        const casualties = [];
        const survivors = [];
        const events = [];

        events.push(`${zone.name} 진입...`);

        for (let r = 1; r <= rounds; r++) {
            const roundEvents = RunSimulator._simulateRound(r, party, zone, zoneLevel, casualties, survivors);
            events.push(...roundEvents);
        }

        party.forEach(merc => {
            if (!casualties.find(c => c.id === merc.id) && !survivors.find(s => s.id === merc.id)) {
                const stats = merc.getStats();
                merc.currentHp = Math.max(1, merc.currentHp - Math.floor(stats.hp * (0.1 + Math.random() * 0.3)));
                survivors.push(merc);
            }
        });

        const lootCount = success
            ? zone.lootCount.min + Math.floor(Math.random() * (zone.lootCount.max - zone.lootCount.min + 1))
            : Math.max(0, Math.floor(Math.random() * 2));

        const loot = [];
        for (let i = 0; i < lootCount; i++) {
            loot.push(generateItem(zoneKey, gameState.guildLevel));
        }

        if (success) {
            events.push('--- 탐사 성공! 귀환 ---');
        } else {
            events.push('--- 탐사 실패... 긴급 철수 ---');
        }

        return { success, rounds, goldEarned, xpEarned, casualties, survivors, loot, events, zoneKey };
    }

    static _simulateRound(round, party, zone, zoneLevel, casualties, survivors) {
        const events = [];
        const alive = party.filter(m => !casualties.find(c => c.id === m.id));

        if (alive.length === 0) return events;

        const encounterRoll = Math.random();
        if (encounterRoll < 0.7) {
            events.push(`라운드 ${round}: 적 조우!`);
            const deathChance = zone.deathChance + (zoneLevel - 1) * 0.02;
            for (const merc of alive) {
                if (Math.random() < deathChance) {
                    casualties.push(merc);
                    events.push(`  ☠ ${merc.name} 전사...`);
                } else {
                    const stats = merc.getStats();
                    const dmg = Math.floor(stats.hp * (0.05 + Math.random() * 0.2));
                    merc.currentHp = Math.max(1, merc.currentHp - dmg);
                }
            }
        } else if (encounterRoll < 0.85) {
            events.push(`라운드 ${round}: 보물 발견!`);
        } else {
            events.push(`라운드 ${round}: 안전 구역 통과`);
            alive.forEach(m => {
                const stats = m.getStats();
                m.currentHp = Math.min(stats.hp, m.currentHp + Math.floor(stats.hp * 0.1));
            });
        }

        return events;
    }
}
