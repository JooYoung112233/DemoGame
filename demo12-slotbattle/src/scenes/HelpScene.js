class HelpScene extends Phaser.Scene {
    constructor() {
        super('HelpScene');
    }

    init(data) {
        this.playerState = data.playerState;
    }

    create() {
        const W = 1280, H = 720;

        const overlay = this.add.rectangle(W / 2, H / 2, W, H, 0x000000, 0.85)
            .setInteractive();

        this.add.text(W / 2, 30, '📖 도움말 — 조합 목록', {
            fontSize: '24px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.detailContainer = null;
        this.scrollY = 0;
        this.listContainer = this.add.container(0, 0);
        this._drawComboList();

        const closeBtn = this.add.text(W - 20, 20, '✕ 닫기', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ff4444',
            backgroundColor: '#330000', padding: { x: 10, y: 6 }
        }).setOrigin(1, 0).setInteractive({ useHandCursor: true }).setDepth(200);
        closeBtn.on('pointerdown', () => {
            this.scene.resume('BattleScene');
            this.scene.stop();
        });
    }

    _drawComboList() {
        const W = 1280;
        const combos = COMBO_DATA;
        const startY = 70;
        const cardH = 60;

        for (let i = 0; i < combos.length; i++) {
            const combo = combos[i];
            const y = startY + i * (cardH + 6);
            const upgLvl = (this.playerState.comboUpgrades && this.playerState.comboUpgrades[combo.id]) || 0;

            const bg = this.add.graphics();
            bg.fillStyle(0x1a1a35, 1);
            bg.lineStyle(1, 0x334466, 0.6);
            bg.fillRoundedRect(60, y, W - 120, cardH, 8);
            bg.strokeRoundedRect(60, y, W - 120, cardH, 8);
            this.listContainer.add(bg);

            const name = this.add.text(80, y + 8, combo.name, {
                fontSize: '15px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            });
            this.listContainer.add(name);

            const recipe = this.add.text(80, y + 30, `조합: ${combo.recipe || ''}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa'
            });
            this.listContainer.add(recipe);

            const desc = this.add.text(400, y + 8, combo.desc, {
                fontSize: '12px', fontFamily: 'monospace', color: '#999999',
                wordWrap: { width: 400 }
            });
            this.listContainer.add(desc);

            const effectStr = this._effectToString(combo.effect, upgLvl);
            const effect = this.add.text(400, y + 30, effectStr, {
                fontSize: '11px', fontFamily: 'monospace', color: '#44ff88'
            });
            this.listContainer.add(effect);

            if (upgLvl > 0) {
                const lvl = this.add.text(W - 80, y + cardH / 2, `Lv.${upgLvl}`, {
                    fontSize: '14px', fontFamily: 'monospace', color: '#aa88ff', fontStyle: 'bold'
                }).setOrigin(0.5);
                this.listContainer.add(lvl);
            }

            const hitArea = this.add.rectangle(W / 2, y + cardH / 2, W - 120, cardH, 0x000000, 0)
                .setInteractive({ useHandCursor: true });
            this.listContainer.add(hitArea);
            hitArea.on('pointerdown', () => this._showDetail(combo, upgLvl));
        }

        const symY = startY + combos.length * (cardH + 6) + 20;
        const symHeader = this.add.text(W / 2, symY, '🎰 심볼 목록', {
            fontSize: '22px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.listContainer.add(symHeader);

        const symbols = Object.values(SYMBOL_DATA);
        const symCardH = 90;
        const symCardW = W - 120;

        for (let i = 0; i < symbols.length; i++) {
            const sym = symbols[i];
            const sy = symY + 40 + i * (symCardH + 8);

            // card background
            const symBg = this.add.graphics();
            symBg.fillStyle(0x1a1a35, 1);
            symBg.lineStyle(1, sym.color || 0x334466, 0.6);
            symBg.fillRoundedRect(60, sy, symCardW, symCardH, 8);
            symBg.strokeRoundedRect(60, sy, symCardW, symCardH, 8);
            this.listContainer.add(symBg);

            // 3x icon
            const iconText = this.add.text(80, sy + 8, sym.icon, { fontSize: '36px' });
            this.listContainer.add(iconText);

            // 3x name
            const nameText = this.add.text(130, sy + 8, sym.name, {
                fontSize: '24px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            });
            this.listContainer.add(nameText);

            // description
            const descText = this.add.text(130, sy + 40, sym.desc, {
                fontSize: '13px', fontFamily: 'monospace', color: '#999999',
                wordWrap: { width: 500 }
            });
            this.listContainer.add(descText);

            // effect
            const effectStr = this._symEffectStr(sym.effect);
            const effText = this.add.text(130, sy + 60, effectStr, {
                fontSize: '13px', fontFamily: 'monospace', color: '#44ff88'
            });
            this.listContainer.add(effText);

            // tier badge
            const tierColors = { 1: '#888888', 2: '#4488ff', 3: '#aa88ff' };
            const tierText = this.add.text(symCardW + 30, sy + symCardH / 2, `Tier ${sym.tier}`, {
                fontSize: '12px', fontFamily: 'monospace', color: tierColors[sym.tier] || '#888888'
            }).setOrigin(0.5);
            this.listContainer.add(tierText);
        }

        // calculate total content height for scroll
        const totalH = symY + 40 + symbols.length * (symCardH + 8) + 50;
        const maxScrollDown = Math.max(0, totalH - H + 30);

        this.input.on('wheel', (pointer, gameObjects, deltaX, deltaY) => {
            this.scrollY -= deltaY * 0.5;
            this.scrollY = Phaser.Math.Clamp(this.scrollY, -maxScrollDown, 0);
            this.listContainer.y = this.scrollY;
        });
    }

    _showDetail(combo, upgLvl) {
        if (this.detailContainer) this.detailContainer.destroy();
        const W = 1280, H = 720;

        this.detailContainer = this.add.container(0, 0).setDepth(150);
        const bg = this.add.rectangle(W / 2, H / 2, 500, 350, 0x111133, 1)
            .setStrokeStyle(2, 0x4466aa);
        this.detailContainer.add(bg);

        const name = this.add.text(W / 2, H / 2 - 140, combo.name, {
            fontSize: '24px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.detailContainer.add(name);

        const recipe = this.add.text(W / 2, H / 2 - 105, `필요 조합: ${combo.recipe || ''}`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc00'
        }).setOrigin(0.5);
        this.detailContainer.add(recipe);

        const desc = this.add.text(W / 2, H / 2 - 75, combo.desc, {
            fontSize: '14px', fontFamily: 'monospace', color: '#cccccc'
        }).setOrigin(0.5);
        this.detailContainer.add(desc);

        const baseEffect = this._effectToString(combo.effect, 0);
        this.detailContainer.add(this.add.text(W / 2, H / 2 - 40, `기본 효과: ${baseEffect}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#44ff88'
        }).setOrigin(0.5));

        if (upgLvl > 0) {
            const upgEffect = this._effectToString(combo.effect, upgLvl);
            this.detailContainer.add(this.add.text(W / 2, H / 2 - 10, `강화 Lv.${upgLvl}: ${upgEffect}`, {
                fontSize: '14px', fontFamily: 'monospace', color: '#aa88ff'
            }).setOrigin(0.5));
        }

        if (combo.matchType === 'includes') {
            this.detailContainer.add(this.add.text(W / 2, H / 2 + 30, '* 나머지 1칸은 아무 심볼이어도 됩니다', {
                fontSize: '12px', fontFamily: 'monospace', color: '#888888'
            }).setOrigin(0.5));
        }
        if (combo.matchType === 'allDifferent') {
            this.detailContainer.add(this.add.text(W / 2, H / 2 + 30, '* 3개 슬롯이 모두 다른 심볼이면 발동', {
                fontSize: '12px', fontFamily: 'monospace', color: '#888888'
            }).setOrigin(0.5));
        }

        this.detailContainer.add(this.add.text(W / 2, H / 2 + 60, '💎 보석은 와일드카드로 어떤 심볼이든 대체 가능', {
            fontSize: '11px', fontFamily: 'monospace', color: '#44ccff'
        }).setOrigin(0.5));

        const closeBtn = this.add.text(W / 2, H / 2 + 120, '[ 닫기 ]', {
            fontSize: '16px', fontFamily: 'monospace', color: '#aaaaaa',
            backgroundColor: '#222244', padding: { x: 16, y: 6 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });
        closeBtn.on('pointerdown', () => {
            this.detailContainer.destroy();
            this.detailContainer = null;
        });
        this.detailContainer.add(closeBtn);
    }

    _symEffectStr(effect) {
        const parts = [];
        if (effect.damage) parts.push(`⚔️${effect.damage}`);
        if (effect.block) parts.push(`🛡️${effect.block}`);
        if (effect.heal) parts.push(`❤️${effect.heal}`);
        if (effect.gold) parts.push(`🪙${effect.gold}`);
        if (effect.aoe) parts.push('(전체)');
        if (effect.hits) parts.push(`(${effect.hits}연타)`);
        if (effect.pierce) parts.push('(관통)');
        if (effect.burn) parts.push(`🔥화상${effect.burn}`);
        if (effect.poison) parts.push(`☠️독${effect.poison}`);
        if (effect.slow) parts.push(`❄️둔화${effect.slow}`);
        if (effect.selfDamage) parts.push(`💀자해${effect.selfDamage}`);
        return parts.join(' ') || '-';
    }

    _effectToString(effect, upgLvl) {
        const mult = 1 + (upgLvl || 0) * 0.2;
        const parts = [];
        if (effect.damage) parts.push(`⚔️${Math.floor(effect.damage * mult)}`);
        if (effect.block) parts.push(`🛡️${Math.floor(effect.block * mult)}`);
        if (effect.heal) parts.push(`❤️${Math.floor(effect.heal * mult)}`);
        if (effect.gold) parts.push(`🪙${Math.floor(effect.gold * mult)}`);
        if (effect.burn) parts.push(`🔥화상${effect.burn}`);
        if (effect.poison) parts.push(`☠️독${effect.poison}`);
        if (effect.slow) parts.push(`❄️둔화${effect.slow}`);
        if (effect.aoe) parts.push('(전체)');
        if (effect.hits) parts.push(`(${effect.hits}연타)`);
        return parts.join(' ');
    }
}
