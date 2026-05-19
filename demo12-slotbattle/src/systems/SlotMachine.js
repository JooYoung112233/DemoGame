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

        this._animateSequential(0, callback);
    }

    _animateSequential(reelIndex, callback) {
        if (reelIndex >= this.reelCount) {
            this.spinning = false;
            if (callback) callback(this.results);
            return;
        }

        const scene = this.scene;
        const reel = this.reelContainers[reelIndex];
        if (!reel) {
            this._animateSequential(reelIndex + 1, callback);
            return;
        }

        const symbolTexts = reel.getAll();
        const finalSymbol = this.results[reelIndex];
        const totalTicks = 12 + reelIndex * 4;
        let tick = 0;

        scene.time.addEvent({
            delay: 45,
            repeat: totalTicks - 1,
            callback: () => {
                tick++;

                const randIdx = Phaser.Math.Between(0, this.symbolPool.length - 1);
                const randSym = this.symbolPool[randIdx];
                const symData = SYMBOL_DATA[randSym];
                if (symbolTexts[0]) symbolTexts[0].setText(symData.icon);
                if (symbolTexts[1]) {
                    symbolTexts[1].setText(symData.name);
                    symbolTexts[1].setColor('#666666');
                }

                reel.setScale(1);
                reel.y += (tick % 2 === 0 ? 2 : -2);

                if (tick === totalTicks) {
                    const fData = SYMBOL_DATA[finalSymbol];
                    if (symbolTexts[0]) symbolTexts[0].setText(fData.icon);
                    if (symbolTexts[1]) {
                        symbolTexts[1].setText(fData.name);
                        symbolTexts[1].setColor('#' + fData.color.toString(16).padStart(6, '0'));
                    }

                    reel.y = reel.getData('originY') || reel.y;

                    scene.tweens.add({
                        targets: reel, scaleX: 1.2, scaleY: 1.2,
                        duration: 100, yoyo: true, ease: 'Back.easeOut',
                        onComplete: () => {
                            reel.setScale(1);
                            scene.cameras.main.shake(50, 0.003);
                            scene.time.delayedCall(200, () => {
                                this._animateSequential(reelIndex + 1, callback);
                            });
                        }
                    });
                }
            }
        });
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
