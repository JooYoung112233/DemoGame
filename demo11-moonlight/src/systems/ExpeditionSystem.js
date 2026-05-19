class ExpeditionSystem {
    static run(zoneId) {
        const zone = ZONE_DATA[zoneId];
        if (!zone) return { items: [], log: ['알 수 없는 구역'] };

        const log = [];
        const items = [];
        const count = Phaser.Math.Between(zone.dropCount.min, zone.dropCount.max);

        log.push(`${zone.name}에 원정을 떠났습니다...`);

        const totalWeight = zone.drops.reduce((s, d) => s + d.weight, 0);

        for (let i = 0; i < count; i++) {
            let roll = Math.random() * totalWeight;
            for (const drop of zone.drops) {
                roll -= drop.weight;
                if (roll <= 0) {
                    const data = ITEM_DATA[drop.id];
                    items.push({ id: drop.id, qty: 1 });
                    log.push(`  ${data.icon} ${data.name}을(를) 발견!`);
                    break;
                }
            }
        }

        const consolidated = {};
        items.forEach(item => {
            if (consolidated[item.id]) consolidated[item.id].qty++;
            else consolidated[item.id] = { id: item.id, qty: 1 };
        });

        log.push(`총 ${count}개 아이템 획득!`);

        return { items: Object.values(consolidated), log };
    }
}
