class AuctionScene extends Phaser.Scene {
    constructor() { super('AuctionScene'); }

    init(data) { this.gameState = data.gameState; }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
        const gs = this.gameState;

        this.add.text(640, 25, '🏛 경매장', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.goldText = this.add.text(1260, 25, `${gs.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        this.tab = 'buy';
        if (!gs.auctionStock) this._refreshStock();
        this._drawTabs();
        this._drawContent();
    }

    _drawTabs() {
        if (this._tabObjs) this._tabObjs.forEach(o => o.destroy && o.destroy());
        this._tabObjs = [];
        const tabs = [
            { key: 'buy', label: '구매', x: 500 },
            { key: 'sell', label: '판매', x: 640 },
            { key: 'refresh', label: '새로고침', x: 780 }
        ];
        tabs.forEach(t => {
            if (t.key === 'refresh') {
                this._tabObjs.push(UIButton.create(this, t.x, 60, 130, 28, `${t.label} (100G)`, {
                    color: 0x443322, hoverColor: 0x554433, textColor: '#ffcc88', fontSize: 12,
                    onClick: () => {
                        if (this.gameState.gold < 100) {
                            UIToast.show(this, '골드가 부족합니다', { color: '#ff6666' });
                            return;
                        }
                        GuildManager.spendGold(this.gameState, 100);
                        this._refreshStock();
                        this.goldText.setText(`${this.gameState.gold}G`);
                        this._clearContent();
                        this._drawContent();
                        UIToast.show(this, '경매 목록 갱신!');
                    }
                }));
            } else {
                const active = this.tab === t.key;
                this._tabObjs.push(UIButton.create(this, t.x, 60, 130, 28, t.label, {
                    color: active ? 0x445588 : 0x222233,
                    hoverColor: active ? 0x445588 : 0x333344,
                    textColor: active ? '#ffffff' : '#888899',
                    fontSize: 12,
                    onClick: () => {
                        this.tab = t.key;
                        this._clearContent();
                        this._drawTabs();
                        this._drawContent();
                    }
                }));
            }
        });
    }

    _clearContent() {
        if (this._contentObjs) this._contentObjs.forEach(o => o.destroy && o.destroy());
        this._contentObjs = [];
    }

    _add(obj) { this._contentObjs.push(obj); return obj; }

    _drawContent() {
        this._clearContent();
        if (this.tab === 'buy') this._drawBuyTab();
        else this._drawSellTab();
    }

    _refreshStock() {
        const gs = this.gameState;
        const stock = [];
        const count = 4 + Math.floor(Math.random() * 3);
        for (let i = 0; i < count; i++) {
            const item = generateItem('common', gs.guildLevel, 0);
            item.auctionPrice = Math.floor(item.value * (1.2 + Math.random() * 0.6));
            stock.push(item);
        }
        gs.auctionStock = stock;
    }

    _drawBuyTab() {
        const gs = this.gameState;
        const stock = gs.auctionStock || [];

        this._add(this.add.text(640, 90, `경매 매물 (${stock.length}건)`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5));

        if (stock.length === 0) {
            this._add(this.add.text(640, 360, '매물이 없습니다\n새로고침으로 목록을 갱신하세요', {
                fontSize: '14px', fontFamily: 'monospace', color: '#555566', align: 'center'
            }).setOrigin(0.5));
            return;
        }

        let cy = 115;
        stock.forEach((item, idx) => {
            if (cy > 660) return;
            this._drawBuyRow(item, idx, 80, cy, 1120);
            cy += 62;
        });
    }

    _drawBuyRow(item, idx, x, y, w) {
        const gs = this.gameState;
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const cap = GuildManager.getStorageCapacity(gs);
        const canBuy = gs.gold >= item.auctionPrice && gs.storage.length < cap;

        const bg = this._add(this.add.graphics());
        bg.fillStyle(canBuy ? 0x1a1a2e : 0x111122, 1);
        bg.fillRoundedRect(x, y, w, 52, 3);
        bg.lineStyle(1, rarity.color, 0.3);
        bg.strokeRoundedRect(x, y, w, 52, 3);

        const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
        this._add(this.add.text(x + 10, y + 8, typeIcons[item.type] || '?', { fontSize: '14px' }));

        this._add(this.add.text(x + 35, y + 8, item.name, {
            fontSize: '13px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }));

        this._add(this.add.text(x + 35, y + 28, `[${rarity.name}] ${item.desc || ''}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#667788'
        }));

        if (item.stats) {
            const statStr = Object.entries(item.stats).map(([k, v]) =>
                typeof v === 'number' && v < 1 ? `${k}+${Math.round(v * 100)}%` : `${k}+${v}`
            ).join('  ');
            this._add(this.add.text(x + 350, y + 8, statStr, {
                fontSize: '10px', fontFamily: 'monospace', color: '#8888aa'
            }));
        }

        this._add(this.add.text(x + w - 180, y + 10, `${item.auctionPrice}G`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }));

        this._add(UIButton.create(this, x + w - 50, y + 28, 80, 26, '구매', {
            color: canBuy ? 0x446644 : 0x333333,
            hoverColor: canBuy ? 0x558855 : 0x333333,
            textColor: canBuy ? '#44ff88' : '#555555',
            fontSize: 11,
            onClick: () => {
                if (!canBuy) return;
                GuildManager.spendGold(gs, item.auctionPrice);
                delete item.auctionPrice;
                StorageManager.addItem(gs, item);
                gs.auctionStock.splice(idx, 1);
                GuildManager.addMessage(gs, `경매장에서 ${item.name} 구매`);
                SaveManager.save(gs);
                UIToast.show(this, `${item.name} 구매!`, { color: '#44ff88' });
                this.goldText.setText(`${gs.gold}G`);
                this._clearContent();
                this._drawContent();
            }
        }));
    }

    _drawSellTab() {
        const gs = this.gameState;
        const sellable = gs.storage.filter(i => i.type !== 'material' || i.value > 0);

        this._add(this.add.text(640, 90, '보관함 아이템을 경매가로 판매합니다 (시세 80%)', {
            fontSize: '12px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5));

        if (sellable.length === 0) {
            this._add(this.add.text(640, 360, '판매할 아이템이 없습니다', {
                fontSize: '14px', fontFamily: 'monospace', color: '#555566'
            }).setOrigin(0.5));
            return;
        }

        let cy = 115;
        sellable.forEach((item) => {
            if (cy > 660) return;
            this._drawSellRow(item, 80, cy, 1120);
            cy += 55;
        });
    }

    _drawSellRow(item, x, y, w) {
        const gs = this.gameState;
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const sellPrice = Math.floor(item.value * 0.8);

        const bg = this._add(this.add.graphics());
        bg.fillStyle(0x1a1a2e, 1);
        bg.fillRoundedRect(x, y, w, 46, 3);
        bg.lineStyle(1, rarity.color, 0.3);
        bg.strokeRoundedRect(x, y, w, 46, 3);

        const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
        this._add(this.add.text(x + 10, y + 8, typeIcons[item.type] || '?', { fontSize: '14px' }));

        this._add(this.add.text(x + 35, y + 8, `${item.name} [${rarity.name}]`, {
            fontSize: '12px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }));

        this._add(this.add.text(x + 35, y + 26, item.desc || '', {
            fontSize: '10px', fontFamily: 'monospace', color: '#667788'
        }));

        if (item.stats) {
            const statStr = Object.entries(item.stats).map(([k, v]) =>
                typeof v === 'number' && v < 1 ? `${k}+${Math.round(v * 100)}%` : `${k}+${v}`
            ).join('  ');
            this._add(this.add.text(x + 350, y + 8, statStr, {
                fontSize: '10px', fontFamily: 'monospace', color: '#8888aa'
            }));
        }

        this._add(this.add.text(x + w - 180, y + 10, `+${sellPrice}G`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffcc44'
        }));

        this._add(UIButton.create(this, x + w - 50, y + 24, 80, 24, '판매', {
            color: 0x886644, hoverColor: 0xaa8866, textColor: '#ffeecc', fontSize: 11,
            onClick: () => {
                StorageManager.removeItem(gs, item.id);
                GuildManager.addGold(gs, sellPrice);
                GuildManager.addMessage(gs, `경매장 판매: ${item.name} (+${sellPrice}G)`);
                SaveManager.save(gs);
                UIToast.show(this, `${item.name} 판매! +${sellPrice}G`, { color: '#ffcc44' });
                this.goldText.setText(`${gs.gold}G`);
                this._clearContent();
                this._drawContent();
            }
        }));
    }
}
