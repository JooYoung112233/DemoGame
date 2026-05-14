const BP_FARMING = {
    DEMO_ID: 'demo5',
    raidInventory: [],

    init() {
        StashManager.load(this.DEMO_ID);
        this.raidInventory = [];
    },

    applyLoadoutToAllies(allies) {
        const stats = StashManager.getLoadoutStats();
        if (Object.keys(stats).length === 0) return;

        allies.forEach(a => {
            if (stats.atk) a.atk += stats.atk;
            if (stats.def) a.def += stats.def;
            if (stats.hp) { a.maxHp += stats.hp; a.hp += stats.hp; }
            if (stats.critRate) a.critRate = Math.min(0.8, a.critRate + stats.critRate);
            if (stats.critDmg) a.critDmg += stats.critDmg;
            if (stats.bleedChance) a.bleedChance = (a.bleedChance || 0) + stats.bleedChance;
            if (stats.dodgeRate) a.dodgeRate = Math.min(0.6, (a.dodgeRate || 0) + stats.dodgeRate);
            if (stats.lifesteal) a.lifesteal = (a.lifesteal || 0) + stats.lifesteal;
            if (stats.thorns) a.thorns = (a.thorns || 0) + stats.thorns;
            if (stats.moveSpeed) a.moveSpeed += stats.moveSpeed;
        });
    },

    rollFarmingDrop(dangerLevel) {
        const drops = LootGenerator.generate(this.DEMO_ID, dangerLevel, 1);
        if (drops.length > 0) {
            this.raidInventory.push(drops[0]);
            return drops[0];
        }
        return null;
    },

    onExtract() {
        const gained = [...this.raidInventory];
        let totalGold = 0;

        gained.forEach(item => {
            const val = ItemRegistry.getValue(item);
            totalGold += val;
            StashManager.addToStash(item);
        });

        StashManager.addGold(totalGold);

        StashManager.moveSecureToStash();

        StashManager.recordRaid({ extracted: true, totalValue: totalGold });

        const result = { gained, lost: [], gold: totalGold };
        this.raidInventory = [];
        return result;
    },

    onDeath() {
        const halfIdx = Math.ceil(this.raidInventory.length / 2);
        const kept = this.raidInventory.slice(0, halfIdx);
        const lost = this.raidInventory.slice(halfIdx);

        let totalGold = 0;
        kept.forEach(item => {
            const val = ItemRegistry.getValue(item);
            totalGold += val;
            StashManager.addToStash(item);
        });
        StashManager.addGold(totalGold);

        const secureItems = StashManager.moveSecureToStash();

        const lostLoadout = StashManager.loseLoadout();
        const allLost = [...lost, ...lostLoadout.map(e => {
            const reg = ItemRegistry.get(e.itemId);
            return reg ? { ...reg, rarity: e.rarity } : e;
        })];

        StashManager.recordRaid({ extracted: false, totalValue: 0 });

        const result = {
            gained: [...kept, ...secureItems.map(s => {
                const reg = ItemRegistry.get(s.itemId);
                return reg ? { ...reg, rarity: s.rarity } : s;
            })],
            lost: allLost,
            gold: totalGold
        };
        this.raidInventory = [];
        return result;
    },

    moveToSecure(raidItemIndex) {
        const secureSlots = StashManager.getSecureContainer();
        const emptyIdx = secureSlots.findIndex(s => s === null);
        if (emptyIdx === -1) return false;
        if (raidItemIndex < 0 || raidItemIndex >= this.raidInventory.length) return false;

        const item = this.raidInventory.splice(raidItemIndex, 1)[0];
        StashManager.setSecureSlot(emptyIdx, item);
        return true;
    },

    removeFromSecure(slotIndex) {
        const secureSlots = StashManager.getSecureContainer();
        if (slotIndex < 0 || slotIndex >= FARMING.SECURE_CONTAINER_SIZE) return false;
        const slot = secureSlots[slotIndex];
        if (!slot) return false;

        const regItem = ItemRegistry.get(slot.itemId);
        if (regItem) {
            this.raidInventory.push({ ...regItem, rarity: slot.rarity });
        }
        StashManager.setSecureSlot(slotIndex, null);
        return true;
    },

    hasEscapeKit() {
        return this.raidInventory.some(item => item.itemId === 'escape_kit');
    },

    consumeEscapeKit() {
        const idx = this.raidInventory.findIndex(item => item.itemId === 'escape_kit');
        if (idx === -1) return false;
        this.raidInventory.splice(idx, 1);
        return true;
    },

    dismantleRaidItem(raidItemIndex) {
        if (raidItemIndex < 0 || raidItemIndex >= this.raidInventory.length) return null;
        const item = this.raidInventory.splice(raidItemIndex, 1)[0];
        const gold = ItemRegistry.getValue(item);
        const escapeKitChance = this._getEscapeKitChance(item);
        const gotKit = Math.random() < escapeKitChance;

        StashManager.addGold(gold);

        const result = { gold, gotKit, dismantled: item };
        if (gotKit) {
            const kitItem = ItemRegistry.get('escape_kit');
            if (kitItem) {
                this.raidInventory.push({ ...kitItem, rarity: 'uncommon' });
            }
        }
        return result;
    },

    dismantleStashItem(stashId) {
        const entry = StashManager.getStash().find(s => s.id === stashId);
        if (!entry) return null;
        const regItem = ItemRegistry.get(entry.itemId);
        if (!regItem) return null;

        const item = { ...regItem, rarity: entry.rarity };
        const gold = ItemRegistry.getValue(item);
        const escapeKitChance = this._getEscapeKitChance(item);
        const gotKit = Math.random() < escapeKitChance;

        StashManager.removeFromStash(stashId);
        StashManager.addGold(gold);

        const result = { gold, gotKit, dismantled: item };
        if (gotKit) {
            const kitItem = ItemRegistry.get('escape_kit');
            if (kitItem) {
                StashManager.addToStash({ ...kitItem, rarity: 'uncommon' });
            }
        }
        return result;
    },

    _getEscapeKitChance(item) {
        const rarityIdx = FARMING.RARITY_ORDER.indexOf(item.rarity);
        // common 10%, uncommon 15%, rare 25%, epic 35%, legendary 50%
        return [0.10, 0.15, 0.25, 0.35, 0.50][rarityIdx] || 0.10;
    },

    getRaidInventory() {
        return this.raidInventory;
    },

    useConsumable(itemIndex, allies, enemies, scene) {
        if (itemIndex < 0 || itemIndex >= this.raidInventory.length) return false;
        const item = this.raidInventory[itemIndex];
        const reg = ItemRegistry.get(item.itemId);
        if (!reg || reg.category !== 'consumable' || item.itemId === 'escape_kit') return false;

        // Remove from inventory
        this.raidInventory.splice(itemIndex, 1);

        // Apply effect
        switch (item.itemId) {
            case 'pit_bandage':
                allies.forEach(a => {
                    if (a.alive) {
                        const h = Math.floor(a.maxHp * 0.25);
                        a.hp = Math.min(a.maxHp, a.hp + h);
                        DamagePopup.show(scene, a.container.x, a.container.y - 30, h, 0x44ff88, true);
                    }
                });
                break;
            case 'fire_bomb':
                enemies.forEach(e => {
                    if (e.alive) {
                        e.takeDamage(100, null);
                        DamagePopup.show(scene, e.container.x, e.container.y - 20, 100, 0xff6644, false);
                    }
                });
                scene.cameras.main.shake(100, 0.003);
                break;
            case 'war_potion':
                allies.forEach(a => {
                    if (a.alive) {
                        const bonus = Math.floor(a.atk * 0.3);
                        a.atk += bonus;
                        DamagePopup.show(scene, a.container.x, a.container.y - 30, 'ATK↑', 0xffaa44, true);
                    }
                });
                break;
        }
        return true;
    },

    hasLoadout() {
        const lo = StashManager.getLoadout();
        return lo.weapon || lo.armor || lo.accessory;
    }
};
