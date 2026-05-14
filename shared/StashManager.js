const StashManager = {
    _demoId: null,
    _data: null,

    _defaultData() {
        return {
            version: 1,
            stash: [],
            secureContainer: [null, null, null],
            loadout: { weapon: null, armor: null, accessory: null },
            gold: 0,
            stats: { raids: 0, extractions: 0, deaths: 0, bestValue: 0 }
        };
    },

    load(demoId) {
        this._demoId = demoId;
        try {
            const raw = localStorage.getItem('STASH_' + demoId);
            this._data = raw ? JSON.parse(raw) : this._defaultData();
        } catch (e) {
            this._data = this._defaultData();
        }
        if (!this._data.secureContainer) this._data.secureContainer = [null, null, null];
        if (!this._data.loadout) this._data.loadout = { weapon: null, armor: null, accessory: null };
        if (!this._data.stats) this._data.stats = { raids: 0, extractions: 0, deaths: 0, bestValue: 0 };
        return this._data;
    },

    save() {
        if (!this._demoId || !this._data) return;
        localStorage.setItem('STASH_' + this._demoId, JSON.stringify(this._data));
    },

    getData() {
        return this._data;
    },

    // ── Stash CRUD ──

    getStash() {
        return this._data.stash;
    },

    addToStash(item) {
        if (this._data.stash.length >= FARMING.STASH_CAPACITY) return false;
        const entry = {
            id: this._uid(),
            itemId: item.itemId,
            rarity: item.rarity,
            enhanceLevel: item.enhanceLevel || 0,
            timestamp: Date.now()
        };
        this._data.stash.push(entry);
        this.save();
        return true;
    },

    addMultipleToStash(items) {
        let added = 0;
        items.forEach(item => {
            if (this.addToStash(item)) added++;
        });
        return added;
    },

    removeFromStash(id) {
        const idx = this._data.stash.findIndex(s => s.id === id);
        if (idx === -1) return null;
        const removed = this._data.stash.splice(idx, 1)[0];
        this.save();
        return removed;
    },

    findInStash(itemId) {
        return this._data.stash.filter(s => s.itemId === itemId);
    },

    // ── Gold ──

    getGold() {
        return this._data.gold;
    },

    addGold(amount) {
        this._data.gold += amount;
        this.save();
    },

    spendGold(amount) {
        if (this._data.gold < amount) return false;
        this._data.gold -= amount;
        this.save();
        return true;
    },

    // ── Secure Container ──

    getSecureContainer() {
        return this._data.secureContainer;
    },

    setSecureSlot(index, item) {
        if (index < 0 || index >= FARMING.SECURE_CONTAINER_SIZE) return false;
        this._data.secureContainer[index] = item ? {
            id: this._uid(),
            itemId: item.itemId,
            rarity: item.rarity
        } : null;
        this.save();
        return true;
    },

    clearSecureContainer() {
        this._data.secureContainer = [null, null, null];
        this.save();
    },

    moveSecureToStash() {
        const moved = [];
        this._data.secureContainer.forEach((slot, i) => {
            if (slot) {
                const regItem = ItemRegistry.get(slot.itemId);
                if (regItem) {
                    this.addToStash({ ...regItem, rarity: slot.rarity });
                    moved.push(slot);
                }
                this._data.secureContainer[i] = null;
            }
        });
        this.save();
        return moved;
    },

    // ── Loadout ──

    getLoadout() {
        return this._data.loadout;
    },

    equipItem(slot, stashId) {
        const stashEntry = this._data.stash.find(s => s.id === stashId);
        if (!stashEntry) return false;
        const regItem = ItemRegistry.get(stashEntry.itemId);
        if (!regItem || regItem.slot !== slot) return false;

        if (this._data.loadout[slot]) {
            this.unequipItem(slot);
        }

        this._data.loadout[slot] = { ...stashEntry };
        this.removeFromStash(stashId);
        return true;
    },

    unequipItem(slot) {
        const equipped = this._data.loadout[slot];
        if (!equipped) return false;
        const regItem = ItemRegistry.get(equipped.itemId);
        if (regItem) {
            this.addToStash({ ...regItem, rarity: equipped.rarity });
        }
        this._data.loadout[slot] = null;
        this.save();
        return true;
    },

    getLoadoutStats() {
        const totalStats = {};
        Object.values(this._data.loadout).forEach(entry => {
            if (!entry) return;
            const regItem = ItemRegistry.get(entry.itemId);
            if (!regItem || !regItem.stats) return;
            Object.entries(regItem.stats).forEach(([key, val]) => {
                totalStats[key] = (totalStats[key] || 0) + val;
            });
        });
        return totalStats;
    },

    loseLoadout() {
        const lost = [];
        Object.keys(this._data.loadout).forEach(slot => {
            if (this._data.loadout[slot]) {
                lost.push(this._data.loadout[slot]);
                this._data.loadout[slot] = null;
            }
        });
        this.save();
        return lost;
    },

    // ── Stats ──

    getStats() {
        return this._data.stats;
    },

    recordRaid(result) {
        this._data.stats.raids++;
        if (result.extracted) {
            this._data.stats.extractions++;
            if (result.totalValue > this._data.stats.bestValue) {
                this._data.stats.bestValue = result.totalValue;
            }
        } else {
            this._data.stats.deaths++;
        }
        this.save();
    },

    // ── Utility ──

    _uid() {
        return Date.now().toString(36) + Math.random().toString(36).substr(2, 5);
    },

    reset() {
        this._data = this._defaultData();
        this.save();
    }
};
