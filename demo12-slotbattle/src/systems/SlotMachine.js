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

        for (let i = 0; i < this.reelCount; i++) {
            const reel = this.reelContainers[i];
            if (!reel) continue;
            const texts = reel.getAll();
            if (texts[0]) texts[0].setText('⬜');
            if (texts[1]) { texts[1].setText(''); texts[1].setColor('#666666'); }
        }

        this._revealReel(0, callback);
    }

    _revealReel(idx, callback) {
        if (idx >= this.reelCount) {
            this.spinning = false;
            if (callback) callback(this.results);
            return;
        }

        const scene = this.scene;
        const reel = this.reelContainers[idx];
        if (!reel) { this._revealReel(idx + 1, callback); return; }

        const texts = reel.getAll();
        const finalSym = this.results[idx];
        const ticks = 6;
        let count = 0;

        scene.time.addEvent({
            delay: 35,
            repeat: ticks - 1,
            callback: () => {
                count++;
                const r = this.symbolPool[Phaser.Math.Between(0, this.symbolPool.length - 1)];
                const sd = SYMBOL_DATA[r];
                if (texts[0]) texts[0].setText(sd.icon);

                if (count === ticks) {
                    const fd = SYMBOL_DATA[finalSym];
                    texts[0].setText(fd.icon);
                    if (texts[1]) {
                        texts[1].setText(fd.name);
                        texts[1].setColor('#' + fd.color.toString(16).padStart(6, '0'));
                    }

                    scene.tweens.add({
                        targets: reel, scaleX: 1.3, scaleY: 1.3,
                        duration: 60, yoyo: true, ease: 'Back.easeOut',
                        onComplete: () => {
                            reel.setScale(1);
                            scene.cameras.main.shake(40, 0.004);
                            this._revealReel(idx + 1, callback);
                        }
                    });
                }
            }
        });
    }

    evaluateCombo(results) {
        for (const combo of COMBO_DATA) {
            if (combo.matchType === 'allDifferent') {
                const unique = new Set(results);
                if (unique.size === results.length && !results.includes('gem')) return combo;
                continue;
            }

            if (combo.matchType === 'includes') {
                const has = combo.symbols.every(s => results.includes(s) || results.includes('gem'));
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
                        used[j] = true; found = true; break;
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
