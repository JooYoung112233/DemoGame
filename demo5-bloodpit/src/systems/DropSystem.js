class DropSystem {
    constructor() {
        this.collectedDrops = [];
        this.collectedPassiveIds = [];
        this.currentWeapon = BP_WEAPONS[0];
    }

    generateRoundDrops(dangerLevel) {
        return generateDrops(dangerLevel, 3);
    }

    applyDrop(drop, allies, scene) {
        if (drop.type === 'weapon') {
            this.currentWeapon = drop;
            allies.forEach(a => {
                if (a.role === 'dps' || a.role === 'tank') {
                    a.atk += drop.atkBonus;
                    if (drop.spdBonus !== 0) {
                        a.attackSpeed = Math.floor(a.attackSpeed * (1 - drop.spdBonus));
                    }
                    if (drop.rangeBonus) {
                        a.range += drop.rangeBonus;
                    }
                }
            });
            this.collectedDrops.push(drop);
        } else if (drop.type === 'passive') {
            drop.apply(allies);
            this.collectedPassiveIds.push(drop.id);
            this.collectedDrops.push(drop);
        } else if (drop.type === 'consumable') {
            this.collectedDrops.push(drop);
        }
    }

    getCollectedCount() {
        return this.collectedDrops.length;
    }

    getSynergyInfo() {
        const tags = {};
        this.collectedDrops.forEach(d => {
            if (d.tag) tags[d.tag] = (tags[d.tag] || 0) + 1;
        });
        const synergies = [];
        if (tags.bleed >= 2) synergies.push({ name: '출혈 시너지', count: tags.bleed, color: '#ff4444' });
        if (tags.dodge >= 2) synergies.push({ name: '회피 시너지', count: tags.dodge, color: '#44ffff' });
        if (tags.corpse >= 2) synergies.push({ name: '시체 시너지', count: tags.corpse, color: '#ff8844' });
        return synergies;
    }
}
