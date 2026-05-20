class CodexScene extends Phaser.Scene {
    constructor() {
        super('CodexScene');
    }

    init(data) {
        this.playerState = data.playerState;
        this.activeTab = 'symbols'; // 'symbols' or 'relics'
    }

    create() {
        const W = 1280, H = 720;

        this.add.rectangle(W / 2, H / 2, W, H, 0x000000, 0.85).setInteractive();

        this.add.text(W / 2, 25, '📚 대본 도감', {
            fontSize: '24px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);

        // Tab buttons
        this.symbolTabBtn = this.add.text(W / 2 - 80, 55, '[ 심볼 ]', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold',
            backgroundColor: '#2a2a55', padding: { x: 14, y: 6 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        this.relicTabBtn = this.add.text(W / 2 + 80, 55, '[ 소품 ]', {
            fontSize: '16px', fontFamily: 'monospace', color: '#888888',
            backgroundColor: '#1a1a35', padding: { x: 14, y: 6 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });

        this.symbolTabBtn.on('pointerdown', () => this._switchTab('symbols'));
        this.relicTabBtn.on('pointerdown', () => this._switchTab('relics'));

        // Content container (scrollable)
        this.contentContainer = this.add.container(0, 0);
        this.scrollY = 0;

        this._drawSymbolTab();

        // Close button
        const closeBtn = this.add.text(W - 20, 20, '✕ 닫기', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ff4444',
            backgroundColor: '#330000', padding: { x: 10, y: 6 }
        }).setOrigin(1, 0).setInteractive({ useHandCursor: true }).setDepth(200);
        closeBtn.on('pointerdown', () => {
            this.scene.resume('BattleScene');
            this.scene.stop();
        });

        // Scroll
        this.input.on('wheel', (pointer, gameObjects, deltaX, deltaY) => {
            this.scrollY -= deltaY * 0.5;
            this.scrollY = Phaser.Math.Clamp(this.scrollY, this._minScroll, 0);
            this.contentContainer.y = this.scrollY;
        });
    }

    _switchTab(tab) {
        if (this.activeTab === tab) return;
        this.activeTab = tab;
        this.scrollY = 0;

        // Update tab button styles
        if (tab === 'symbols') {
            this.symbolTabBtn.setColor('#ffffff').setBackgroundColor('#2a2a55');
            this.relicTabBtn.setColor('#888888').setBackgroundColor('#1a1a35');
        } else {
            this.symbolTabBtn.setColor('#888888').setBackgroundColor('#1a1a35');
            this.relicTabBtn.setColor('#ffffff').setBackgroundColor('#2a2a55');
        }

        // Clear and redraw
        this.contentContainer.removeAll(true);
        this.contentContainer.y = 0;

        if (tab === 'symbols') this._drawSymbolTab();
        else this._drawRelicTab();
    }

    _drawSymbolTab() {
        const W = 1280;
        const allSymbols = Object.values(SYMBOL_DATA);
        const discovered = this.playerState.codex || {};
        const total = allSymbols.length;
        const found = Object.keys(discovered).length;

        const countText = this.add.text(W / 2, 85, `발견: ${found} / ${total}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#44ff88'
        }).setOrigin(0.5);
        this.contentContainer.add(countText);

        const cols = 5;
        const cardW = 200, cardH = 120, gap = 16;
        const startX = W / 2 - (cols * cardW + (cols - 1) * gap) / 2 + cardW / 2;
        const startY = 110;

        for (let i = 0; i < allSymbols.length; i++) {
            const sym = allSymbols[i];
            const col = i % cols;
            const row = Math.floor(i / cols);
            const x = startX + col * (cardW + gap);
            const y = startY + row * (cardH + gap);
            const isFound = !!discovered[sym.id];

            const bg = this.add.graphics();
            if (isFound) {
                bg.fillStyle(0x1a1a35, 1);
                bg.lineStyle(2, sym.color, 0.8);
            } else {
                bg.fillStyle(0x111111, 1);
                bg.lineStyle(1, 0x333333, 0.5);
            }
            bg.fillRoundedRect(x - cardW / 2, y, cardW, cardH, 8);
            bg.strokeRoundedRect(x - cardW / 2, y, cardW, cardH, 8);
            this.contentContainer.add(bg);

            if (isFound) {
                this.contentContainer.add(this.add.text(x - cardW / 2 + 15, y + 12, sym.icon, { fontSize: '28px' }));
                this.contentContainer.add(this.add.text(x - cardW / 2 + 55, y + 10, sym.name, {
                    fontSize: '16px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
                }));
                this.contentContainer.add(this.add.text(x - cardW / 2 + 55, y + 32, sym.desc, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#999999',
                    wordWrap: { width: cardW - 70 }
                }));
                const effectStr = this._effectStr(sym.effect);
                this.contentContainer.add(this.add.text(x - cardW / 2 + 55, y + 55, effectStr, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#44ff88'
                }));

                const count = this.playerState.symbolPool.filter(id => id === sym.id).length;
                if (count > 0) {
                    this.contentContainer.add(this.add.text(x + cardW / 2 - 10, y + 10, `x${count}`, {
                        fontSize: '13px', fontFamily: 'monospace', color: '#ffcc00', fontStyle: 'bold'
                    }).setOrigin(1, 0));
                }

                this.contentContainer.add(this.add.text(x - cardW / 2 + 15, y + cardH - 18, `Tier ${sym.tier}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#666666'
                }));
            } else {
                this.contentContainer.add(this.add.text(x, y + cardH / 2, '???', {
                    fontSize: '20px', fontFamily: 'monospace', color: '#333333'
                }).setOrigin(0.5));
            }
        }

        const rows = Math.ceil(allSymbols.length / cols);
        const totalH = startY + rows * (cardH + gap) + 30;
        this._minScroll = Math.min(0, -(totalH - 720 + 30));
    }

    _drawRelicTab() {
        const W = 1280;
        const allRelics = Object.values(RELIC_DATA);
        const ownedRelics = this.playerState.relics || [];
        const ownedCount = ownedRelics.length;

        const countText = this.add.text(W / 2, 85, `보유: ${ownedCount}개`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#aa88ff'
        }).setOrigin(0.5);
        this.contentContainer.add(countText);

        const cols = 4;
        const cardW = 260, cardH = 140, gap = 16;
        const startX = W / 2 - (cols * cardW + (cols - 1) * gap) / 2 + cardW / 2;
        const startY = 110;

        const rarityColors = { common: 0x888888, uncommon: 0x4488ff, rare: 0xaa88ff };
        const rarityLabels = { common: '일반', uncommon: '희귀', rare: '전설' };

        for (let i = 0; i < allRelics.length; i++) {
            const relic = allRelics[i];
            const col = i % cols;
            const row = Math.floor(i / cols);
            const x = startX + col * (cardW + gap);
            const y = startY + row * (cardH + gap);
            const isOwned = ownedRelics.includes(relic.id);

            const bg = this.add.graphics();
            const borderColor = rarityColors[relic.rarity] || 0x333333;
            if (isOwned) {
                bg.fillStyle(0x1a1a35, 1);
                bg.lineStyle(2, borderColor, 0.9);
            } else {
                bg.fillStyle(0x0d0d18, 1);
                bg.lineStyle(1, 0x222233, 0.5);
            }
            bg.fillRoundedRect(x - cardW / 2, y, cardW, cardH, 10);
            bg.strokeRoundedRect(x - cardW / 2, y, cardW, cardH, 10);
            this.contentContainer.add(bg);

            if (isOwned) {
                // Icon
                this.contentContainer.add(this.add.text(x - cardW / 2 + 15, y + 12, relic.icon, { fontSize: '32px' }));
                // Name
                this.contentContainer.add(this.add.text(x - cardW / 2 + 60, y + 10, relic.name, {
                    fontSize: '16px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
                }));
                // Rarity badge
                const rarityColor = { common: '#888888', uncommon: '#4488ff', rare: '#ffaa44' }[relic.rarity];
                this.contentContainer.add(this.add.text(x - cardW / 2 + 60, y + 32, rarityLabels[relic.rarity], {
                    fontSize: '12px', fontFamily: 'monospace', color: rarityColor
                }));
                // Description
                this.contentContainer.add(this.add.text(x - cardW / 2 + 15, y + 55, relic.desc, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#bbbbbb',
                    wordWrap: { width: cardW - 30 }
                }));
                // Shop cost
                this.contentContainer.add(this.add.text(x + cardW / 2 - 10, y + 10, `${relic.shopCost}G`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#ffcc00'
                }).setOrigin(1, 0));
                // Owned badge
                this.contentContainer.add(this.add.text(x - cardW / 2 + 15, y + cardH - 22, '✔ 보유 중', {
                    fontSize: '11px', fontFamily: 'monospace', color: '#44ff88'
                }));
            } else {
                // Silhouette — dim icon + ???
                this.contentContainer.add(this.add.text(x - 10, y + cardH / 2 - 15, relic.icon, {
                    fontSize: '32px'
                }).setOrigin(0.5).setAlpha(0.15));
                this.contentContainer.add(this.add.text(x, y + cardH / 2 + 20, '???', {
                    fontSize: '16px', fontFamily: 'monospace', color: '#333344'
                }).setOrigin(0.5));
                // Rarity hint
                const rarityColor = { common: '#555555', uncommon: '#334466', rare: '#443355' }[relic.rarity];
                this.contentContainer.add(this.add.text(x, y + cardH - 16, rarityLabels[relic.rarity], {
                    fontSize: '10px', fontFamily: 'monospace', color: rarityColor
                }).setOrigin(0.5));
            }
        }

        const rows = Math.ceil(allRelics.length / cols);
        const totalH = startY + rows * (cardH + gap) + 30;
        this._minScroll = Math.min(0, -(totalH - 720 + 30));
    }

    _effectStr(effect) {
        const parts = [];
        if (effect.damage) parts.push(`⚔️${effect.damage}`);
        if (effect.block) parts.push(`🛡️${effect.block}`);
        if (effect.heal) parts.push(`❤️${effect.heal}`);
        if (effect.gold) parts.push(`🪙${effect.gold}`);
        if (effect.aoe) parts.push('(전체)');
        if (effect.hits) parts.push(`(${effect.hits}연타)`);
        if (effect.pierce) parts.push('(관통)');
        if (effect.thorns) parts.push(`(반사${effect.thorns})`);
        if (effect.selfDamage) parts.push(`💀자해${effect.selfDamage}`);
        return parts.join(' ') || '-';
    }
}
