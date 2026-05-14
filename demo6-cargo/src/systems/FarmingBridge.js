const LC_FARMING = {
    STORAGE_KEY: 'lc_farming_v1',

    MATERIALS: {
        scrap: { name: '고철', icon: '🔩', desc: '기본 재료' },
        circuit: { name: '회로', icon: '⚡', desc: '전자 부품' },
        core: { name: '코어', icon: '💎', desc: '희귀 재료' }
    },

    GEAR: [
        { id: 'armor_vest', name: '방탄 조끼', desc: '전원 DEF +5', cost: { scrap: 8, circuit: 2 },
            apply(allies) { allies.forEach(a => a.def += 5); } },
        { id: 'scope', name: '조준경', desc: '사수 크리 +10%', cost: { scrap: 5, circuit: 4 },
            apply(allies) { allies.forEach(a => { if (a.role === 'dps') a.critRate += 0.10; }); } },
        { id: 'stim_pack', name: '전투 자극제', desc: '전원 공속 -10%', cost: { scrap: 6, circuit: 3 },
            apply(allies) { allies.forEach(a => a.attackSpeed = Math.floor(a.attackSpeed * 0.9)); } },
        { id: 'plating', name: '강화 장갑', desc: '탱커 HP +20%', cost: { scrap: 10, core: 1 },
            apply(allies) { allies.forEach(a => { if (a.role === 'tank') { const b = Math.floor(a.maxHp * 0.2); a.maxHp += b; a.hp += b; } }); } },
        { id: 'med_upgrade', name: '의료 강화', desc: '힐러 회복 +25%', cost: { circuit: 6, core: 1 },
            apply(allies) { allies.forEach(a => { if (a.role === 'healer') a.atk = Math.floor(a.atk * 1.25); }); } },
        { id: 'ammo_belt', name: '탄약대', desc: '전원 ATK +10%', cost: { scrap: 12, circuit: 4, core: 2 },
            apply(allies) { allies.forEach(a => a.atk = Math.floor(a.atk * 1.1)); } }
    ],

    load() {
        try {
            const raw = localStorage.getItem(this.STORAGE_KEY);
            if (raw) return JSON.parse(raw);
        } catch (e) {}
        return { scrap: 0, circuit: 0, core: 0, equipped: [], totalRuns: 0, bestGrade: 'D' };
    },

    save(data) {
        localStorage.setItem(this.STORAGE_KEY, JSON.stringify(data));
    },

    rollMaterial(round) {
        const r = Math.random();
        const coreChance = Math.min(0.03 + round * 0.02, 0.15);
        const circuitChance = 0.25;
        if (r < coreChance) return 'core';
        if (r < coreChance + circuitChance) return 'circuit';
        return 'scrap';
    },

    addRunRewards(enemiesKilled, round, grade) {
        const data = this.load();
        for (let i = 0; i < enemiesKilled; i++) {
            if (Math.random() < 0.5) {
                const mat = this.rollMaterial(round);
                data[mat] = (data[mat] || 0) + 1;
            }
        }
        if (grade === 'S') { data.core = (data.core || 0) + 2; data.circuit = (data.circuit || 0) + 3; }
        else if (grade === 'A') { data.core = (data.core || 0) + 1; data.circuit = (data.circuit || 0) + 2; }
        else if (grade === 'B') { data.circuit = (data.circuit || 0) + 2; }
        data.totalRuns = (data.totalRuns || 0) + 1;
        if (this.gradeRank(grade) > this.gradeRank(data.bestGrade || 'D')) data.bestGrade = grade;
        this.save(data);
        return data;
    },

    gradeRank(g) { return { S: 4, A: 3, B: 2, C: 1, D: 0 }[g] || 0; },

    canCraft(gearId) {
        const gear = this.GEAR.find(g => g.id === gearId);
        if (!gear) return false;
        const data = this.load();
        if ((data.equipped || []).includes(gearId)) return false;
        for (const [mat, cost] of Object.entries(gear.cost)) {
            if ((data[mat] || 0) < cost) return false;
        }
        return true;
    },

    craft(gearId) {
        const gear = this.GEAR.find(g => g.id === gearId);
        if (!gear || !this.canCraft(gearId)) return false;
        const data = this.load();
        for (const [mat, cost] of Object.entries(gear.cost)) {
            data[mat] -= cost;
        }
        data.equipped = data.equipped || [];
        data.equipped.push(gearId);
        this.save(data);
        return true;
    },

    applyEquippedGear(allies) {
        const data = this.load();
        (data.equipped || []).forEach(gearId => {
            const gear = this.GEAR.find(g => g.id === gearId);
            if (gear && gear.apply) gear.apply(allies);
        });
    },

    getEquippedGear() {
        const data = this.load();
        return (data.equipped || []).map(id => this.GEAR.find(g => g.id === id)).filter(Boolean);
    }
};
