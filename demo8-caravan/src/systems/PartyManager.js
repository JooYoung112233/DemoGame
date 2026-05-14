class PartyManager {
    static recruit(runState, classKey) {
        const data = UNIT_DATA[classKey];
        if (!data) return { success: false, msg: '유닛 데이터 없음' };
        if (runState.party.length >= runState.maxPartySize) return { success: false, msg: '파티가 가득 찼다!' };
        if (runState.gold < data.cost) return { success: false, msg: '골드가 부족하다!' };

        runState.gold -= data.cost;
        const unit = new Unit(classKey);
        runState.party.push(unit);
        runState.totalRecruits++;
        return { success: true, unit, msg: `${data.name} 고용! (-${data.cost}G)` };
    }

    static advance(runState, unit, targetClassKey) {
        const cost = unit.getAdvanceCost(targetClassKey);
        if (runState.gold < cost) return { success: false, msg: '골드가 부족하다!' };

        runState.gold -= cost;
        const trait = unit.advance(targetClassKey);
        const traitType = GOOD_TRAITS.includes(trait) ? '좋은' : '나쁜';
        return {
            success: true,
            trait,
            msg: `${unit.getName()}(으)로 전직! [${trait.name}] (${traitType} 특성) (-${cost}G)`
        };
    }

    static applyBattleTraits(runState) {
        runState.party.forEach(unit => {
            const stats = unit.getStats();
            const resilient = unit.traits.find(t => t.id === 'resilient');
            if (resilient) {
                unit.currentHp = Math.min(stats.hp, unit.currentHp + Math.floor(stats.hp * 0.1));
            }
            const jinxed = unit.traits.find(t => t.id === 'jinxed');
            if (jinxed) {
                unit.currentHp = Math.max(1, unit.currentHp - Math.floor(stats.hp * 0.05));
            }
        });
    }
}
