class ExpeditionSystem {
    static run(zoneId, adventurer, gameState) {
        const zone = ZONE_DATA[zoneId];
        if (!zone) return { success: false, items: [], log: ['알 수 없는 구역'], gold: 0, advEvents: [] };

        const log = [];
        const items = [];
        const advEvents = []; // 모험가 성장/부상/퀘스트 이벤트

        // 레벨 보너스 적용된 스탯 가져오기
        const stats = gameState ? gameState.getAdvEffectiveStats(adventurer) : {
            successRate: adventurer.successRate,
            lootMult: adventurer.lootMult,
        };
        const level = gameState ? gameState.getAdvLevel(adventurer.id) : 1;

        log.push({ text: `${adventurer.icon} ${adventurer.name}(Lv.${level})에게 ${zone.name} 원정을 의뢰했다.`, delay: 0 });
        log.push({ text: `💰 의뢰 비용: ${adventurer.cost}G`, delay: 600 });
        log.push({ text: '...', delay: 1200 });

        const roll = Math.random();
        const success = roll < stats.successRate;

        if (!success) {
            const failType = Math.random();
            if (failType < 0.4) {
                log.push({ text: `😰 "몬스터를 만났습니다... 빈손으로 돌아왔습니다."`, delay: 2200, color: '#ff6666' });
            } else if (failType < 0.7) {
                // 부상 발생
                const injuryDays = Phaser.Math.Between(2, 3);
                log.push({ text: `🤕 "부상을 입고 돌아왔습니다." (${injuryDays}일간 휴식 필요)`, delay: 2200, color: '#ff6666' });

                if (gameState) {
                    gameState.injureAdv(adventurer.id, injuryDays);
                    advEvents.push({ type: 'injury', advId: adventurer.id, days: injuryDays });
                }

                const pityDrop = ExpeditionSystem._rollDrop(zone, adventurer);
                if (pityDrop) {
                    items.push(pityDrop);
                    const d = ITEM_DATA[pityDrop.id];
                    log.push({ text: `  하지만 ${d.icon} ${d.name}을(를) 겨우 건졌다.`, delay: 3000, color: '#aaaaaa' });
                }
            } else {
                log.push({ text: `😞 "길을 잃어서 시간만 낭비했습니다..."`, delay: 2200, color: '#ff6666' });
            }

            // 실패해도 소량 경험치
            if (gameState) {
                const newLevel = gameState.addAdvExp(adventurer.id, 2);
                if (newLevel) {
                    log.push({ text: `⬆️ ${adventurer.name} Lv.${newLevel} 달성!`, delay: 3200, color: '#44ccff' });
                    advEvents.push({ type: 'levelup', advId: adventurer.id, newLevel });
                }
            }

            log.push({ text: '⚠️ 원정 실패 — 의뢰 비용은 돌아오지 않는다.', delay: 3500, color: '#ff4444' });
            return { success: false, items, log, gold: 0, advEvents };
        }

        // === 성공 ===
        log.push({ text: `📍 ${zone.name}에 도착...`, delay: 2000 });

        const baseCount = Phaser.Math.Between(zone.dropCount.min, zone.dropCount.max);
        const finalCount = Math.round(baseCount * stats.lootMult);
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

        // 퀘스트 보상 보너스 카테고리 체크
        if (gameState) {
            const state = gameState.getAdvState(adventurer.id);
            if (state?.questComplete && adventurer.quest?.reward?.bonusCategory) {
                const bc = adventurer.quest.reward.bonusCategory;
                const bonusDrops = zone.drops.filter(d => {
                    const data = ITEM_DATA[d.id];
                    return data && data.category === bc;
                });
                if (bonusDrops.length > 0 && Math.random() < 0.5) {
                    const pick = bonusDrops[Phaser.Math.Between(0, bonusDrops.length - 1)];
                    items.push({ id: pick.id, qty: 1 });
                    log.push({ text: `  🏆 퀘스트 보너스! ${ITEM_DATA[pick.id].icon} ${ITEM_DATA[pick.id].name} 추가`, delay, color: '#ff88ff' });
                    delay += 600;
                }
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

        // 경험치 & 퀘스트 진행
        if (gameState) {
            const expGain = 5 + zone.difficulty * 3;
            const newLevel = gameState.addAdvExp(adventurer.id, expGain);
            delay += 400;
            log.push({ text: `  📈 경험치 +${expGain}`, delay, color: '#44ccff' });

            if (newLevel) {
                delay += 400;
                log.push({ text: `  ⬆️ ${adventurer.name} Lv.${newLevel} 달성!`, delay, color: '#ffcc44' });
                advEvents.push({ type: 'levelup', advId: adventurer.id, newLevel });
            }

            const questResult = gameState.advanceQuestProgress(adventurer.id, zoneId);
            if (questResult === 'complete') {
                delay += 500;
                log.push({ text: `  🏆 퀘스트 「${adventurer.quest.name}」 완료!`, delay, color: '#ff88ff' });
                log.push({ text: `  → ${adventurer.quest.rewardDesc}`, delay: delay + 300, color: '#ff88ff' });
                advEvents.push({ type: 'quest_complete', advId: adventurer.id, questName: adventurer.quest.name });
            } else if (questResult === 'progress') {
                const state = gameState.getAdvState(adventurer.id);
                delay += 400;
                log.push({ text: `  📜 퀘스트 진행: ${state.questProgress}/${adventurer.quest.required}`, delay, color: '#aaaaff' });
            }
        }

        let bonusGold = 0;
        if (Math.random() < 0.15) {
            bonusGold = Phaser.Math.Between(5, 20);
            delay += 500;
            log.push({ text: `🪙 +${bonusGold}G (현장에서 주운 동전)`, delay, color: '#ffcc44' });
        }

        return { success: true, items: result, log, gold: bonusGold, advEvents };
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
