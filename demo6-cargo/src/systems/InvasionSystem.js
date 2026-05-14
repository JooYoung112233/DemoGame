class InvasionSystem {
    constructor() {
        this.currentRound = 0;
        this.maxRound = 10;
        this.region = 'ruins';
    }

    nextRound() {
        this.currentRound++;
        if (this.currentRound > 5) this.region = 'snow';
        return this.currentRound;
    }

    getScale() {
        return 1 + (this.currentRound - 1) * 0.12;
    }

    generateInvasion(cars) {
        const scale = this.getScale();
        const invasion = [];

        cars.forEach((car, idx) => {
            if (car.destroyed) {
                invasion.push({ carIndex: idx, enemies: [], entryType: 'none' });
                return;
            }

            const baseChance = 0.5 + this.currentRound * 0.03;
            const importanceBonus = car.function === 'power' ? 0.15 : car.function === 'ammo' ? 0.10 : 0;
            const doorBrokenBonus = car.doorBroken ? 0.15 : 0;
            const invasionChance = Math.min(baseChance + importanceBonus + doorBrokenBonus, 0.95);

            if (Math.random() > invasionChance) {
                invasion.push({ carIndex: idx, enemies: [], entryType: 'none' });
                return;
            }

            const enemies = [];
            const count = Math.min(1 + Math.floor(this.currentRound / 3), 4);

            for (let i = 0; i < count; i++) {
                const type = this.pickEnemyType();
                const base = LC_ENEMIES[type];
                enemies.push({
                    ...base,
                    hp: Math.floor(base.hp * scale),
                    atk: Math.floor(base.atk * scale),
                    def: Math.floor(base.def * scale),
                    type: type
                });
            }

            const entryTypes = ['창문 돌파', '천장 침입', '문 파괴', '외부 매달림'];
            const entryType = entryTypes[Math.floor(Math.random() * entryTypes.length)];

            invasion.push({ carIndex: idx, enemies, entryType });
        });

        return invasion;
    }

    pickEnemyType() {
        const r = Math.random();
        if (this.currentRound <= 3) {
            return r < 0.7 ? 'crawler' : r < 0.9 ? 'tanker' : 'explosive';
        } else if (this.currentRound <= 6) {
            return r < 0.4 ? 'crawler' : r < 0.65 ? 'tanker' : r < 0.85 ? 'jumper' : 'explosive';
        } else {
            return r < 0.3 ? 'crawler' : r < 0.5 ? 'tanker' : r < 0.75 ? 'jumper' : 'explosive';
        }
    }

    isLastRound() {
        return this.currentRound >= this.maxRound;
    }

    getRegionEffect() {
        if (this.region === 'ruins') {
            return { name: '폐허 도시', desc: '근접 적 증가', color: '#aa8866' };
        } else {
            return { name: '설원', desc: '전투 속도 -20%', color: '#88ddff', spdPenalty: 0.20 };
        }
    }
}
