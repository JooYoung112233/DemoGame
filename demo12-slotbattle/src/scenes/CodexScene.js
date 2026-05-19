class CodexScene extends Phaser.Scene {
    constructor() {
        super('CodexScene');
    }

    init(data) {
        this.playerState = data.playerState;
    }

    create() {
        const W = 1280, H = 720;

        this.add.rectangle(W / 2, H / 2, W, H, 0x000000, 0.85).setInteractive();

        this.add.text(W / 2, 30, '📚 심볼 도감', {
            fontSize: '24px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);

        const allSymbols = Object.values(SYMBOL_DATA);
        const discovered = this.playerState.codex || {};
        const total = allSymbols.length;
        const found = Object.keys(discovered).length;

        this.add.text(W / 2, 60, `발견: ${found} / ${total}`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#44ff88'
        }).setOrigin(0.5);

        const cols = 5;
        const cardW = 200, cardH = 120, gap = 16;
        const startX = W / 2 - (cols * cardW + (cols - 1) * gap) / 2 + cardW / 2;
        const startY = 100;

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

            if (isFound) {
                this.add.text(x - cardW / 2 + 15, y + 12, sym.icon, { fontSize: '28px' });
                this.add.text(x - cardW / 2 + 55, y + 10, sym.name, {
                    fontSize: '16px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
                });
                this.add.text(x - cardW / 2 + 55, y + 32, sym.desc, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#999999',
                    wordWrap: { width: cardW - 70 }
                });

                const effectStr = this._effectStr(sym.effect);
                this.add.text(x - cardW / 2 + 55, y + 55, effectStr, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#44ff88'
                });

                const count = this.playerState.symbolPool.filter(id => id === sym.id).length;
                if (count > 0) {
                    this.add.text(x + cardW / 2 - 10, y + 10, `x${count}`, {
                        fontSize: '13px', fontFamily: 'monospace', color: '#ffcc00', fontStyle: 'bold'
                    }).setOrigin(1, 0);
                }

                this.add.text(x - cardW / 2 + 15, y + cardH - 18, `Tier ${sym.tier}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#666666'
                });
            } else {
                this.add.text(x, y + cardH / 2, '???', {
                    fontSize: '20px', fontFamily: 'monospace', color: '#333333'
                }).setOrigin(0.5);
            }
        }

        const closeBtn = this.add.text(W - 20, 20, '✕ 닫기', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ff4444',
            backgroundColor: '#330000', padding: { x: 10, y: 6 }
        }).setOrigin(1, 0).setInteractive({ useHandCursor: true });
        closeBtn.on('pointerdown', () => {
            this.scene.resume('BattleScene');
            this.scene.stop();
        });
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
