class WaveManager {
    constructor() {
        this.currentWave = 0;
        this.maxWave = 10;
    }

    getWaveEnemies(wave) {
        const enemies = [];
        const scale = 1 + (wave - 1) * 0.12;

        if (wave <= 3) {
            const count = 2 + wave;
            for (let i = 0; i < count; i++) enemies.push(this.scaled('melee', scale));
        } else if (wave <= 6) {
            const meleeCount = 2 + Math.floor(wave / 2);
            const rangedCount = wave - 3;
            for (let i = 0; i < meleeCount; i++) enemies.push(this.scaled('melee', scale));
            for (let i = 0; i < rangedCount; i++) enemies.push(this.scaled('ranged', scale));
        } else if (wave <= 9) {
            const meleeCount = 3 + Math.floor(wave / 3);
            const rangedCount = Math.floor(wave / 2);
            const tankCount = wave - 6;
            for (let i = 0; i < meleeCount; i++) enemies.push(this.scaled('melee', scale));
            for (let i = 0; i < rangedCount; i++) enemies.push(this.scaled('ranged', scale));
            for (let i = 0; i < tankCount; i++) enemies.push(this.scaled('tank', scale));
        } else {
            enemies.push(this.scaled('boss', scale));
            for (let i = 0; i < 3; i++) enemies.push(this.scaled('melee', scale));
            for (let i = 0; i < 2; i++) enemies.push(this.scaled('ranged', scale));
        }

        return enemies;
    }

    scaled(type, scale) {
        const base = WAVE_ENEMIES[type];
        return {
            ...base,
            hp: Math.floor(base.hp * scale),
            atk: Math.floor(base.atk * scale),
            def: Math.floor(base.def * scale)
        };
    }

    nextWave() {
        this.currentWave++;
        return this.currentWave;
    }

    isLastWave() {
        return this.currentWave >= this.maxWave;
    }

    reset() {
        this.currentWave = 0;
    }
}
