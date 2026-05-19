// 슬롯머신 시스템
class SlotMachine {
    constructor(scene) {
        this.scene = scene;
        this.reelCount = 3;
        this.results = [];
        this.spinning = false;
        this.reelContainers = [];
        this.symbolPool = [];
    }

    setSymbolPool(pool) {
        this.symbolPool = pool.slice();
    }

    spin(callback) {
        if (this.spinning || this.symbolPool.length === 0) return;
        this.spinning = true;
        this.results = [];

        for (let i = 0; i < this.reelCount; i++) {
            const idx = Phaser.Math.Between(0, this.symbolPool.length - 1);
            this.results.push(this.symbolPool[idx]);
        }

        this._animateSpin(callback);
    }

    _animateSpin(callback) {
        const scene = this.scene;
        let completed = 0;

        for (let i = 0; i < this.reelCount; i++) {
            const reel = this.reelContainers[i];
            if (!reel) { completed++; continue; }

            const symbolTexts = reel.getAll();
            const finalSymbol = this.results[i];
            const delay = i * 200;
            const spinCycles = 8 + i * 4;
            let tick = 0;

            scene.time.addEvent({
                delay: 50,
                repeat: spinCycles - 1,
                startAt: delay,
                callback: () => {
                    tick++;
                    const randIdx = Phaser.Math.Between(0, this.symbolPool.length - 1);
                    const randSym = this.symbolPool[randIdx];
                    const symData = SYMBOL_DATA[randSym];
                    if (symbolTexts[0]) symbolTexts[0].setText(symData.icon);
                    if (symbolTexts[1]) symbolTexts[1].setText(symData.name);

                    if (tick === spinCycles) {
                        const fData = SYMBOL_DATA[finalSymbol];
                        symbolTexts[0].setText(fData.icon);
                        symbolTexts[1].setText(fData.name);

                        scene.tweens.add({
                            targets: reel,
                            scaleX: 1.15, scaleY: 1.15,
                            duration: 80,
                            yoyo: true,
                            onComplete: () => {
                                completed++;
                                if (completed === this.reelCount) {
                                    this.spinning = false;
                                    if (callback) callback(this.results);
                                }
                            }
                        });
                    }
                }
            });
        }
    }

    evaluateCombo(results) {
        const sorted = results.slice().sort();

        for (const combo of COMBO_DATA) {
            if (combo.matchType === 'allDifferent') {
                const unique = new Set(results);
                if (unique.size === results.length && !results.includes('gem')) {
                    return combo;
                }
                continue;
            }

            if (combo.matchType === 'includes') {
                const has = combo.symbols.every(s =>
                    results.includes(s) || results.includes('gem')
                );
                if (has) return combo;
                continue;
            }

            // exact match (triple)
            const comboSorted = combo.symbols.slice().sort();
            const wildResults = results.map(r => r === 'gem' ? null : r);

            let match = true;
            const used = new Array(results.length).fill(false);
            for (const needed of comboSorted) {
                let found = false;
                for (let j = 0; j < wildResults.length; j++) {
                    if (!used[j] && (wildResults[j] === needed || wildResults[j] === null)) {
                        used[j] = true;
                        found = true;
                        break;
                    }
                }
                if (!found) { match = false; break; }
            }
            if (match) return combo;
        }

        return null;
    }

    getIndividualEffects(results) {
        const effects = { damage: 0, block: 0, heal: 0, gold: 0, selfDamage: 0 };
        for (const symId of results) {
            const sym = SYMBOL_DATA[symId];
            if (!sym) continue;
            const e = sym.effect;
            if (e.damage) effects.damage += e.damage;
            if (e.block) effects.block += e.block;
            if (e.heal) effects.heal += e.heal;
            if (e.gold) effects.gold += e.gold;
            if (e.selfDamage) effects.selfDamage += e.selfDamage;
        }
        return effects;
    }
}
