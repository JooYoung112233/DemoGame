class ExpeditionSystem {
    static run(zoneId, adventurer) {
        const zone = ZONE_DATA[zoneId];
        if (!zone) return { success: false, items: [], log: ['알 수 없는 구역'], gold: 0 };

        const log = [];
        const items = [];

        log.push({ text: `${adventurer.icon} ${adventurer.name}에게 ${zone.name} 원정을 의뢰했다.`, delay: 0 });
        log.push({ text: `💰 의뢰 비용: ${adventurer.cost}G`, delay: 600 });
        log.push({ text: '...', delay: 1200 });

        const roll = Math.random();
        const success = roll < adventurer.successRate;

        if (!success) {
            const failType = Math.random();
            if (failType < 0.4) {
                log.push({ text: `😰 "몬스터를 만났습니다... 빈손으로 돌아왔습니다."`, delay: 2200, color: '#ff6666' });
            } else if (failType < 0.7) {
                log.push({ text: `🤕 "부상을 입고 돌아왔습니다."`, delay: 2200, color: '#ff6666' });
                const pityDrop = ExpeditionSystem._rollDrop(zone, adventurer);
                if (pityDrop) {
                    items.push(pityDrop);
                    const d = ITEM_DATA[pityDrop.id];
                    log.push({ text: `  하지만 ${d.icon} ${d.name}을(를) 겨우 건졌다.`, delay: 3000, color: '#aaaaaa' });
                }
            } else {
                log.push({ text: `😞 "길을 잃어서 시간만 낭비했습니다..."`, delay: 2200, color: '#ff6666' });
            }
            log.push({ text: '⚠️ 원정 실패 — 의뢰 비용은 돌아오지 않는다.', delay: 3500, color: '#ff4444' });
            return { success: false, items, log, gold: 0 };
        }

        log.push({ text: `📍 ${zone.name}에 도착...`, delay: 2000 });

        const baseCount = Phaser.Math.Between(zone.dropCount.min, zone.dropCount.max);
        const finalCount = Math.round(baseCount * adventurer.lootMult);
        let delay = 2800;

        if (Math.random() < 0.3) {
            const events = [
                { text: `🎯 숨겨진 통로를 발견!`, bonus: 2 },
                { text: `🤝 다른 모험가와 협력해서 더 많이 모았다!`, bonus: 1 },
                { text: `🌟 오늘은 운이 좋았다!`, bonus: 1, rare: true },
            ];
            const evt = events[Phaser.Math.Between(0, events.length - 1)];
            log.push({ text: evt.text, delay, color: '#ffcc44' });
            delay += 800;
            for (let i = 0; i < evt.bonus; i++) {
                const drop = ExpeditionSystem._rollDrop(zone, adventurer, evt.rare);
                if (drop) items.push(drop);
            }
        }

        for (let i = 0; i < finalCount; i++) {
            const drop = ExpeditionSystem._rollDrop(zone, adventurer);
            if (drop) items.push(drop);
        }

        if (adventurer.bonusCategory) {
            const bonusDrops = zone.drops.filter(d => {
                const data = ITEM_DATA[d.id];
                return data && data.category === adventurer.bonusCategory;
            });
            if (bonusDrops.length > 0 && Math.random() < 0.5) {
                const pick = bonusDrops[Phaser.Math.Between(0, bonusDrops.length - 1)];
                items.push({ id: pick.id, qty: 1 });
                log.push({ text: `  ${adventurer.icon} 전문 분야 보너스! ${ITEM_DATA[pick.id].icon} ${ITEM_DATA[pick.id].name} 추가`, delay, color: '#44ccff' });
                delay += 600;
            }
        }

        const consolidated = {};
        items.forEach(item => {
            if (consolidated[item.id]) consolidated[item.id].qty += item.qty;
            else consolidated[item.id] = { id: item.id, qty: item.qty };
        });
        const result = Object.values(consolidated);

        result.forEach(item => {
            const data = ITEM_DATA[item.id];
            log.push({ text: `  ${data.icon} ${data.name} x${item.qty}`, delay, color: '#dddddd' });
            delay += 350;
        });

        log.push({ text: '', delay });
        delay += 200;
        log.push({ text: `✅ 원정 성공! 총 ${result.reduce((s, i) => s + i.qty, 0)}개 납품`, delay, color: '#44ff88' });

        let bonusGold = 0;
        if (Math.random() < 0.15) {
            bonusGold = Phaser.Math.Between(5, 20);
            delay += 500;
            log.push({ text: `🪙 +${bonusGold}G (현장에서 주운 동전)`, delay, color: '#ffcc44' });
        }

        return { success: true, items: result, log, gold: bonusGold };
    }

    static _rollDrop(zone, adventurer, forceRare) {
        let drops = zone.drops;
        if (forceRare) {
            const rareDrops = drops.filter(d => {
                const data = ITEM_DATA[d.id];
                return data && (data.category === 'gem' || data.category === 'relic');
            });
            if (rareDrops.length > 0) drops = rareDrops;
        }
        const totalWeight = drops.reduce((s, d) => s + d.weight, 0);
        let roll = Math.random() * totalWeight;
        for (const drop of drops) {
            roll -= drop.weight;
            if (roll <= 0) return { id: drop.id, qty: 1 };
        }
        return null;
    }
}
