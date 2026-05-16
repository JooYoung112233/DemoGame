class AutomationManager {
    static ensureState(gs) {
        if (!gs.automationSettings) {
            gs.automationSettings = {
                autoEquipMode: 'manual',
                autoSellRules: {
                    sellCommon: false,
                    sellUncommon: false,
                    sellDuplicates: false,
                    sellOnOverflow: false
                },
                autoCollect: false,
                autoEquipOnDispatch: false,
                autoRedispatch: false,
                autoConsign: false,
                autoHeal: false,
                fullAuto: false
            };
        }
    }

    static isUnlocked(gs, feature) {
        GuildHallManager.ensureState(gs);
        const stage = gs.guildHall.automation || 0;
        const requirements = {
            autoEquip: 1,
            autoSell: 2,
            autoCollect: 3,
            autoEquipOnDispatch: 4,
            autoRedispatch: 5,
            autoConsign: 6,
            autoHeal: 7,
            fullAuto: 8
        };
        return stage >= (requirements[feature] || 99);
    }

    // === D1: 자동 장착 ===

    static autoEquipAll(gs, mode) {
        if (!AutomationManager.isUnlocked(gs, 'autoEquip')) return [];
        AutomationManager.ensureState(gs);

        const changes = [];
        for (const merc of gs.roster) {
            if (!merc.alive || merc.injured) continue;
            if (ExpeditionManager.isOnExpedition(gs, merc.id)) continue;
            const equipped = AutomationManager._autoEquipMerc(gs, merc, mode);
            if (equipped.length > 0) changes.push({ merc, equipped });
        }
        if (changes.length > 0) SaveManager.save(gs);
        return changes;
    }

    static _autoEquipMerc(gs, merc, mode) {
        const equipped = [];
        for (const slot of ['weapon', 'armor', 'accessory']) {
            const candidates = StorageManager.getEquippableItems(gs, slot);
            if (candidates.length === 0) continue;

            const best = AutomationManager._pickBest(merc, candidates, slot, mode);
            if (!best) continue;

            const currentScore = AutomationManager._scoreItem(merc, merc.equipment[slot], slot, mode);
            const bestScore = AutomationManager._scoreItem(merc, best, slot, mode);
            if (bestScore <= currentScore) continue;

            const prev = merc.equipment[slot];
            if (prev && prev.cursed) continue;

            // 기존 장비 보관함으로
            if (prev) {
                merc.equipment[slot] = null;
                StorageManager.addItem(gs, prev);
            }
            // 새 장비 착용
            StorageManager.removeItem(gs, best.id);
            merc.equip(best);
            equipped.push({ slot, item: best, prev });
        }
        return equipped;
    }

    static _pickBest(merc, candidates, slot, mode) {
        let best = null, bestScore = -Infinity;
        for (const item of candidates) {
            if (item.locked) continue;
            const score = AutomationManager._scoreItem(merc, item, slot, mode);
            if (score > bestScore) { bestScore = score; best = item; }
        }
        return best;
    }

    static _scoreItem(merc, item, slot, mode) {
        if (!item) return 0;
        const s = item.stats || {};
        const p = item.penalty || {};
        const atk = (s.atk || 0) - Math.abs(p.atk || 0);
        const def = (s.def || 0) - Math.abs(p.def || 0);
        const hp = (s.hp || 0) - Math.abs(p.hp || 0);
        const crit = (s.critRate || 0) * 100;

        switch (mode || 'class') {
            case 'atk': return atk * 3 + crit * 2 + hp * 0.1 + def * 0.1;
            case 'hpdef': return hp * 2 + def * 3 + atk * 0.1;
            case 'balanced': return atk + def + hp * 0.5 + crit;
            case 'class': default: {
                const weights = {
                    warrior:   { atk: 1.0, def: 2.5, hp: 2.0, crit: 0.5 },
                    rogue:     { atk: 2.5, def: 0.5, hp: 0.5, crit: 3.0 },
                    mage:      { atk: 3.0, def: 0.3, hp: 0.5, crit: 2.0 },
                    archer:    { atk: 2.5, def: 0.5, hp: 0.5, crit: 2.5 },
                    priest:    { atk: 0.5, def: 1.5, hp: 2.5, crit: 0.3 },
                    alchemist: { atk: 1.5, def: 1.5, hp: 1.5, crit: 1.0 }
                }[merc.classKey] || { atk: 1, def: 1, hp: 1, crit: 1 };
                return atk * weights.atk + def * weights.def + hp * weights.hp + crit * weights.crit;
            }
        }
    }

    // === D2: 자동 판매 ===

    static runAutoSell(gs) {
        if (!AutomationManager.isUnlocked(gs, 'autoSell')) return [];
        AutomationManager.ensureState(gs);
        const rules = gs.automationSettings.autoSellRules;
        const sold = [];

        const toSell = [];
        for (const item of gs.storage) {
            if (item.type !== 'equipment') continue;
            if (item.locked) continue;
            if (rules.sellCommon && item.rarity === 'common') toSell.push(item);
            else if (rules.sellUncommon && item.rarity === 'uncommon') toSell.push(item);
            else if (rules.sellDuplicates && AutomationManager._isDuplicate(gs, item)) toSell.push(item);
        }

        // overflow 룰
        if (rules.sellOnOverflow) {
            const cap = GuildManager.getStorageCapacity(gs);
            if (gs.storage.length >= Math.floor(cap * 0.8)) {
                for (const item of gs.storage) {
                    if (toSell.includes(item)) continue;
                    if (item.locked) continue;
                    if (item.type !== 'equipment') continue;
                    if (item.rarity === 'common') toSell.push(item);
                }
            }
        }

        for (const item of toSell) {
            const result = StorageManager.sellItem(gs, item.id);
            if (result.success) sold.push(result);
        }
        return sold;
    }

    static _isDuplicate(gs, item) {
        const same = gs.storage.filter(i =>
            i.type === 'equipment' && i.slot === item.slot &&
            i.baseId === item.baseId && i.id !== item.id
        );
        return same.length > 0;
    }

    // === D3: 자동 회수 ===

    static runAutoCollect(gs) {
        if (!AutomationManager.isUnlocked(gs, 'autoCollect')) return [];
        AutomationManager.ensureState(gs);
        if (!gs.automationSettings.autoCollect) return [];

        const pending = gs.pendingResults || [];
        const collected = [];
        const ids = pending.map(r => r.id);
        for (const id of ids) {
            const r = ExpeditionManager.collectResult(gs, id);
            if (r) collected.push(r);
        }
        if (collected.length > 0) SaveManager.save(gs);
        return collected;
    }

    // === D4: 파견 출발 자동 최적화 ===

    static autoEquipForDispatch(gs, party) {
        if (!AutomationManager.isUnlocked(gs, 'autoEquipOnDispatch')) return;
        AutomationManager.ensureState(gs);
        if (!gs.automationSettings.autoEquipOnDispatch) return;

        const mode = gs.automationSettings.autoEquipMode || 'class';
        for (const merc of party) {
            AutomationManager._autoEquipMerc(gs, merc, mode);
        }
    }

    // === D5: 자동 재파견 ===

    static runAutoRedispatch(gs) {
        if (!AutomationManager.isUnlocked(gs, 'autoRedispatch')) return [];
        AutomationManager.ensureState(gs);
        if (!gs.automationSettings.autoRedispatch) return [];

        const dispatched = [];
        const pending = gs.pendingResults || [];

        for (const result of [...pending]) {
            if (!result.success) continue;
            const party = gs.roster.filter(m =>
                result.partyIds.includes(m.id) && m.alive && !m.injured &&
                !ExpeditionManager.isOnExpedition(gs, m.id)
            );
            if (party.length === 0) continue;
            if (gs.activeExpeditions.length >= ExpeditionManager.getMaxSlots(gs)) break;

            const exp = ExpeditionManager.dispatch(gs, result.zoneKey, party, result.zoneLevel);
            if (exp) dispatched.push(exp);
        }
        if (dispatched.length > 0) SaveManager.save(gs);
        return dispatched;
    }

    // === D6: 자동 위탁 ===

    static runAutoConsign(gs) {
        if (!AutomationManager.isUnlocked(gs, 'autoConsign')) return [];
        AutomationManager.ensureState(gs);
        if (!gs.automationSettings.autoConsign) return [];

        const consigned = [];
        const rules = gs.automationSettings.autoSellRules;

        for (const item of [...gs.storage]) {
            if (item.type !== 'equipment') continue;
            if (item.locked) continue;
            if (item.rarity === 'common' || item.rarity === 'uncommon') {
                const removed = StorageManager.removeItem(gs, item.id);
                if (removed) {
                    const value = Math.floor((removed.value || 50) * 0.9);
                    GuildManager.addGold(gs, value);
                    consigned.push(removed);
                    GuildManager.addMessage(gs, `위탁 판매: ${removed.name} (+${value}G)`);
                }
            }
        }
        if (consigned.length > 0) SaveManager.save(gs);
        return consigned;
    }

    // === D7: 자동 치유 ===

    static runAutoHeal(gs) {
        if (!AutomationManager.isUnlocked(gs, 'autoHeal')) return [];
        AutomationManager.ensureState(gs);
        if (!gs.automationSettings.autoHeal) return [];

        const healed = [];

        // 부상자 자동 치유 (HP 회복)
        for (const merc of gs.roster) {
            if (!merc.alive) continue;
            const stats = merc.getStats();
            if (merc.currentHp < stats.hp) {
                const healCost = Math.floor((stats.hp - merc.currentHp) * 0.5);
                if (gs.gold >= healCost) {
                    GuildManager.spendGold(gs, healCost);
                    merc.currentHp = stats.hp;
                    merc.injured = false;
                    merc.injuredUntilMs = 0;
                    healed.push({ merc, cost: healCost });
                }
            }
        }

        // 사망자 자동 부활
        if (gs.fallenMercs && gs.fallenMercs.length > 0) {
            for (const merc of [...gs.fallenMercs]) {
                const cost = GuildManager.getRevivalCost(merc);
                if (gs.gold >= cost) {
                    GuildManager.reviveMerc(gs, merc.id);
                    healed.push({ merc, cost, revived: true });
                }
            }
        }

        if (healed.length > 0) SaveManager.save(gs);
        return healed;
    }

    // === D8: 완전 자동화 (마을 진입 시 전체 실행) ===

    static runFullAuto(gs) {
        if (!AutomationManager.isUnlocked(gs, 'fullAuto')) return;
        AutomationManager.ensureState(gs);
        if (!gs.automationSettings.fullAuto) return;

        AutomationManager.runAutoCollect(gs);
        AutomationManager.runAutoHeal(gs);
        AutomationManager.runAutoSell(gs);
        AutomationManager.runAutoConsign(gs);
        AutomationManager.autoEquipAll(gs, gs.automationSettings.autoEquipMode || 'class');
        AutomationManager.runAutoRedispatch(gs);
    }
}
