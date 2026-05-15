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
        this.filterType = 'all';
        this.sortBy = 'price';
        this.scrollOffset = 0;
        if (!gs.auctionStock || gs.auctionStock.length === 0) this._refreshStock();
        if (!gs.auctionHistory) gs.auctionHistory = [];

        this._drawTabs();
        this._drawContent();
    }

    _drawTabs() {
        if (this._tabObjs) this._tabObjs.forEach(o => o.destroy && o.destroy());
        this._tabObjs = [];
        const tabs = [
            { key: 'buy', label: '구매', x: 440 },
            { key: 'sell', label: '판매', x: 560 },
            { key: 'bid', label: '입찰', x: 680 },
            { key: 'history', label: '거래 내역', x: 800 }
        ];
        tabs.forEach(t => {
            const active = this.tab === t.key;
            this._tabObjs.push(UIButton.create(this, t.x, 60, 110, 28, t.label, {
                color: active ? 0x445588 : 0x222233,
                hoverColor: active ? 0x445588 : 0x333344,
                textColor: active ? '#ffffff' : '#888899',
                fontSize: 12,
                onClick: () => {
                    this.tab = t.key;
                    this.scrollOffset = 0;
                    this._clearContent();
                    this._drawTabs();
                    this._drawContent();
                }
            }));
        });
    }

    _clearContent() {
        if (this._contentObjs) this._contentObjs.forEach(o => o.destroy && o.destroy());
        this._contentObjs = [];
    }

    _add(obj) { if (!this._contentObjs) this._contentObjs = []; this._contentObjs.push(obj); return obj; }

    _drawContent() {
        this._clearContent();
        if (this.tab === 'buy') this._drawBuyTab();
        else if (this.tab === 'sell') this._drawSellTab();
        else if (this.tab === 'bid') this._drawBidTab();
        else this._drawHistoryTab();
    }

    _refreshStock() {
        const gs = this.gameState;
        const stock = [];
        const count = 6 + Math.floor(Math.random() * 4);
        for (let i = 0; i < count; i++) {
            const item = generateItem('common', gs.guildLevel, Math.random() < 0.2 ? 1 : 0);
            const baseMult = 1.2 + Math.random() * 0.8;
            item.auctionPrice = Math.floor(item.value * baseMult);
            if (Math.random() < 0.15) {
                item.isHotDeal = true;
                item.auctionPrice = Math.floor(item.auctionPrice * 0.6);
            }
            if (Math.random() < 0.1) {
                item.isBulk = true;
                item.bulkCount = 2 + Math.floor(Math.random() * 3);
                item.auctionPrice = Math.floor(item.auctionPrice * item.bulkCount * 0.85);
            }
            stock.push(item);
        }
        gs.auctionStock = stock;
        this._generateBidItems(gs);
        SaveManager.save(gs);
    }

    _generateBidItems(gs) {
        gs.auctionBids = [];
        const bidCount = 2 + Math.floor(Math.random() * 2);
        for (let i = 0; i < bidCount; i++) {
            const item = generateItem('common', gs.guildLevel, 1 + Math.floor(Math.random() * 2));
            const startBid = Math.floor(item.value * 0.5);
            gs.auctionBids.push({
                item,
                currentBid: startBid,
                minIncrement: Math.max(10, Math.floor(startBid * 0.15)),
                npcBidders: 1 + Math.floor(Math.random() * 3),
                roundsLeft: 2 + Math.floor(Math.random() * 2),
                playerBid: 0,
                resolved: false
            });
        }
    }

    // --- BUY TAB ---
    _drawBuyTab() {
        const gs = this.gameState;

        this._drawFilterBar(90);

        const stock = this._getFilteredStock(gs.auctionStock || []);

        const refreshCost = 80 + gs.guildLevel * 20;
        this._add(UIButton.create(this, 1180, 90, 140, 26, `갱신 (${refreshCost}G)`, {
            color: gs.gold >= refreshCost ? 0x443322 : 0x333333,
            hoverColor: gs.gold >= refreshCost ? 0x554433 : 0x333333,
            textColor: gs.gold >= refreshCost ? '#ffcc88' : '#555555',
            fontSize: 11,
            onClick: () => {
                if (gs.gold < refreshCost) { UIToast.show(this, '골드 부족', { color: '#ff6666' }); return; }
                GuildManager.spendGold(gs, refreshCost);
                this._refreshStock();
                this.goldText.setText(`${gs.gold}G`);
                this._clearContent();
                this._drawContent();
                UIToast.show(this, '경매 목록 갱신!');
            }
        }));

        this._add(this.add.text(200, 90, `매물 ${stock.length}건`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#666677'
        }));

        if (stock.length === 0) {
            this._add(this.add.text(640, 400, '해당 조건의 매물이 없습니다', {
                fontSize: '14px', fontFamily: 'monospace', color: '#555566'
            }).setOrigin(0.5));
            return;
        }

        let cy = 120;
        stock.forEach((item, idx) => {
            if (cy > 680) return;
            this._drawBuyRow(item, idx, 40, cy, 1200);
            cy += 68;
        });
    }

    _drawFilterBar(y) {
        const filters = [
            { key: 'all', label: '전체' },
            { key: 'equipment', label: '⚔ 장비' },
            { key: 'material', label: '🔧 소재' },
            { key: 'consumable', label: '🧪 소비' }
        ];
        let fx = 40;
        filters.forEach(f => {
            const active = this.filterType === f.key;
            this._add(UIButton.create(this, fx + 40, y, 80, 22, f.label, {
                color: active ? 0x334466 : 0x1a1a2e,
                hoverColor: active ? 0x334466 : 0x222244,
                textColor: active ? '#aaccff' : '#666688',
                fontSize: 10,
                onClick: () => {
                    this.filterType = f.key;
                    this._clearContent();
                    this._drawContent();
                }
            }));
            fx += 90;
        });

        const sorts = [
            { key: 'price', label: '가격순' },
            { key: 'rarity', label: '등급순' },
            { key: 'name', label: '이름순' }
        ];
        fx += 30;
        sorts.forEach(s => {
            const active = this.sortBy === s.key;
            this._add(UIButton.create(this, fx + 35, y, 70, 22, s.label, {
                color: active ? 0x334444 : 0x1a1a2e,
                hoverColor: active ? 0x334444 : 0x222244,
                textColor: active ? '#88ccaa' : '#556666',
                fontSize: 10,
                onClick: () => {
                    this.sortBy = s.key;
                    this._clearContent();
                    this._drawContent();
                }
            }));
            fx += 80;
        });
    }

    _getFilteredStock(stock) {
        let filtered = this.filterType === 'all' ? [...stock] : stock.filter(i => i.type === this.filterType);
        const rarityOrder = { legendary: 0, epic: 1, rare: 2, uncommon: 3, common: 4 };
        if (this.sortBy === 'price') filtered.sort((a, b) => a.auctionPrice - b.auctionPrice);
        else if (this.sortBy === 'rarity') filtered.sort((a, b) => (rarityOrder[a.rarity] || 5) - (rarityOrder[b.rarity] || 5));
        else filtered.sort((a, b) => a.name.localeCompare(b.name));
        return filtered;
    }

    _drawBuyRow(item, idx, x, y, w) {
        const gs = this.gameState;
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const cap = GuildManager.getStorageCapacity(gs);
        const canBuy = gs.gold >= item.auctionPrice && gs.storage.length < cap;

        const bg = this._add(this.add.graphics());
        bg.fillStyle(item.isHotDeal ? 0x2a1a1a : 0x1a1a2e, 1);
        bg.fillRoundedRect(x, y, w, 58, 4);
        bg.lineStyle(1, item.isHotDeal ? 0xff6644 : rarity.color, item.isHotDeal ? 0.6 : 0.3);
        bg.strokeRoundedRect(x, y, w, 58, 4);

        const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
        this._add(this.add.text(x + 12, y + 8, typeIcons[item.type] || '?', { fontSize: '16px' }));

        let nameStr = item.name;
        if (item.isBulk) nameStr += ` ×${item.bulkCount}`;
        this._add(this.add.text(x + 38, y + 8, nameStr, {
            fontSize: '13px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }));

        const tagX = x + 38;
        let tagOff = 0;
        if (item.isHotDeal) {
            this._add(this.add.text(x + 38 + nameStr.length * 8 + 10, y + 10, '🔥 특가', {
                fontSize: '10px', fontFamily: 'monospace', color: '#ff6644', fontStyle: 'bold'
            }));
        }
        if (item.isBulk) {
            this._add(this.add.text(x + 38 + nameStr.length * 8 + 10 + (item.isHotDeal ? 55 : 0), y + 10, '📦 묶음', {
                fontSize: '10px', fontFamily: 'monospace', color: '#44aaff'
            }));
        }

        this._add(this.add.text(x + 38, y + 30, `[${rarity.name}] ${item.desc || ''}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#667788'
        }));

        if (item.stats) {
            const statStr = Object.entries(item.stats).map(([k, v]) =>
                typeof v === 'number' && v < 1 ? `${k}+${Math.round(v * 100)}%` : `${k}+${v}`
            ).join('  ');
            this._add(this.add.text(x + 400, y + 10, statStr, {
                fontSize: '11px', fontFamily: 'monospace', color: '#8888aa'
            }));
        }

        const marketValue = item.value * (item.isBulk ? item.bulkCount : 1);
        const priceRatio = item.auctionPrice / marketValue;
        const priceColor = priceRatio <= 0.7 ? '#44ff88' : priceRatio <= 1.0 ? '#ffcc44' : '#ff8866';

        this._add(this.add.text(x + w - 220, y + 8, `${item.auctionPrice}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: priceColor, fontStyle: 'bold'
        }));

        const valueLabel = priceRatio <= 0.7 ? '매우 저렴' : priceRatio <= 1.0 ? '적정가' : priceRatio <= 1.5 ? '약간 비쌈' : '비쌈';
        const valueLabelColor = priceRatio <= 0.7 ? '#44ff88' : priceRatio <= 1.0 ? '#aaaa88' : '#ff8866';
        this._add(this.add.text(x + w - 220, y + 32, `시세 대비: ${valueLabel}`, {
            fontSize: '9px', fontFamily: 'monospace', color: valueLabelColor
        }));

        this._add(UIButton.create(this, x + w - 55, y + 30, 90, 28, '구매', {
            color: canBuy ? 0x446644 : 0x333333,
            hoverColor: canBuy ? 0x558855 : 0x333333,
            textColor: canBuy ? '#44ff88' : '#555555',
            fontSize: 12,
            onClick: () => {
                if (!canBuy) {
                    if (gs.storage.length >= cap) UIToast.show(this, '보관함이 가득 찼습니다', { color: '#ff6666' });
                    else UIToast.show(this, '골드가 부족합니다', { color: '#ff6666' });
                    return;
                }
                GuildManager.spendGold(gs, item.auctionPrice);
                const realIdx = gs.auctionStock.indexOf(item);
                if (realIdx >= 0) gs.auctionStock.splice(realIdx, 1);

                if (item.isBulk) {
                    for (let b = 0; b < item.bulkCount; b++) {
                        const copy = { ...item, id: Date.now() + b, isBulk: false, bulkCount: undefined };
                        delete copy.auctionPrice;
                        delete copy.isHotDeal;
                        StorageManager.addItem(gs, copy);
                    }
                } else {
                    delete item.auctionPrice;
                    delete item.isHotDeal;
                    delete item.isBulk;
                    StorageManager.addItem(gs, item);
                }

                gs.auctionHistory = gs.auctionHistory || [];
                gs.auctionHistory.unshift({ action: 'buy', name: item.name, price: item.auctionPrice || 0, time: Date.now() });
                if (gs.auctionHistory.length > 20) gs.auctionHistory.length = 20;

                GuildManager.addMessage(gs, `경매장에서 ${item.name}${item.isBulk ? ' 묶음' : ''} 구매`);
                SaveManager.save(gs);
                UIToast.show(this, `${item.name} 구매!`, { color: '#44ff88' });
                this.goldText.setText(`${gs.gold}G`);
                this._clearContent();
                this._drawContent();
            }
        }));
    }

    // --- SELL TAB ---
    _drawSellTab() {
        const gs = this.gameState;

        this._drawFilterBar(90);
        const sellable = this._getFilteredSellable(gs);

        const feeTable = { common: 10, uncommon: 15, rare: 20, epic: 25, legendary: 30 };
        this._add(this.add.text(640, 90, '판매 수수료: 일반 10% | 고급 15% | 희귀 20% | 에픽 25% | 전설 30%', {
            fontSize: '10px', fontFamily: 'monospace', color: '#666677'
        }).setOrigin(0.5));

        if (sellable.length === 0) {
            this._add(this.add.text(640, 400, '판매할 아이템이 없습니다', {
                fontSize: '14px', fontFamily: 'monospace', color: '#555566'
            }).setOrigin(0.5));
            return;
        }

        let cy = 115;
        sellable.forEach(item => {
            if (cy > 680) return;
            this._drawSellRow(item, 40, cy, 1200);
            cy += 60;
        });
    }

    _getFilteredSellable(gs) {
        let items = gs.storage.filter(i => i.value > 0);
        if (this.filterType !== 'all') items = items.filter(i => i.type === this.filterType);
        const rarityOrder = { legendary: 0, epic: 1, rare: 2, uncommon: 3, common: 4 };
        if (this.sortBy === 'price') items.sort((a, b) => b.value - a.value);
        else if (this.sortBy === 'rarity') items.sort((a, b) => (rarityOrder[a.rarity] || 5) - (rarityOrder[b.rarity] || 5));
        else items.sort((a, b) => a.name.localeCompare(b.name));
        return items;
    }

    _drawSellRow(item, x, y, w) {
        const gs = this.gameState;
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const feePercent = { common: 10, uncommon: 15, rare: 20, epic: 25, legendary: 30 }[item.rarity] || 10;
        const sellPrice = Math.floor(item.value * (1 - feePercent / 100));

        const bg = this._add(this.add.graphics());
        bg.fillStyle(0x1a1a2e, 1);
        bg.fillRoundedRect(x, y, w, 50, 4);
        bg.lineStyle(1, rarity.color, 0.3);
        bg.strokeRoundedRect(x, y, w, 50, 4);

        const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
        this._add(this.add.text(x + 12, y + 8, typeIcons[item.type] || '?', { fontSize: '14px' }));

        this._add(this.add.text(x + 38, y + 8, `${item.name} [${rarity.name}]`, {
            fontSize: '12px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }));

        this._add(this.add.text(x + 38, y + 28, `${item.desc || ''} ${item.stats ? Object.entries(item.stats).map(([k, v]) => typeof v === 'number' && v < 1 ? `${k}+${Math.round(v * 100)}%` : `${k}+${v}`).join(' ') : ''}`, {
            fontSize: '9px', fontFamily: 'monospace', color: '#667788'
        }));

        this._add(this.add.text(x + w - 250, y + 8, `시세: ${item.value}G`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#888866'
        }));

        this._add(this.add.text(x + w - 250, y + 26, `수수료 ${feePercent}% → +${sellPrice}G`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }));

        this._add(UIButton.create(this, x + w - 55, y + 26, 90, 26, '판매', {
            color: 0x886644, hoverColor: 0xaa8866, textColor: '#ffeecc', fontSize: 11,
            onClick: () => {
                StorageManager.removeItem(gs, item.id);
                GuildManager.addGold(gs, sellPrice);

                gs.auctionHistory = gs.auctionHistory || [];
                gs.auctionHistory.unshift({ action: 'sell', name: item.name, price: sellPrice, rarity: item.rarity, time: Date.now() });
                if (gs.auctionHistory.length > 20) gs.auctionHistory.length = 20;

                GuildManager.addMessage(gs, `경매장 판매: ${item.name} (+${sellPrice}G)`);
                SaveManager.save(gs);
                UIToast.show(this, `${item.name} 판매! +${sellPrice}G`, { color: '#ffcc44' });
                this.goldText.setText(`${gs.gold}G`);
                this._clearContent();
                this._drawContent();
            }
        }));
    }

    // --- BID TAB ---
    _drawBidTab() {
        const gs = this.gameState;
        const bids = gs.auctionBids || [];
        const active = bids.filter(b => !b.resolved);

        this._add(this.add.text(640, 95, '입찰 — 경쟁 입찰로 고급 아이템을 저렴하게 획득하세요', {
            fontSize: '11px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5));

        if (active.length === 0) {
            this._add(this.add.text(640, 400, '현재 진행 중인 입찰이 없습니다\n구매 탭에서 목록을 갱신하면 새 입찰이 등장합니다', {
                fontSize: '13px', fontFamily: 'monospace', color: '#555566', align: 'center'
            }).setOrigin(0.5));
            return;
        }

        let cy = 125;
        active.forEach((bid, idx) => {
            this._drawBidCard(gs, bid, idx, 60, cy, 1160);
            cy += 140;
        });
    }

    _drawBidCard(gs, bid, idx, x, y, w) {
        const item = bid.item;
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const isLeading = bid.playerBid > 0 && bid.playerBid >= bid.currentBid;
        const minBid = bid.currentBid + bid.minIncrement;
        const canBid = gs.gold >= minBid;

        const bg = this._add(this.add.graphics());
        bg.fillStyle(isLeading ? 0x1a2a1a : 0x1a1a2e, 1);
        bg.fillRoundedRect(x, y, w, 125, 6);
        bg.lineStyle(2, isLeading ? 0x44ff88 : rarity.color, 0.5);
        bg.strokeRoundedRect(x, y, w, 125, 6);

        const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
        this._add(this.add.text(x + 15, y + 12, typeIcons[item.type] || '?', { fontSize: '18px' }));

        this._add(this.add.text(x + 45, y + 12, item.name, {
            fontSize: '15px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }));

        this._add(this.add.text(x + 45, y + 34, `[${rarity.name}] ${item.desc || ''}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#667788'
        }));

        if (item.stats) {
            const statStr = Object.entries(item.stats).map(([k, v]) =>
                typeof v === 'number' && v < 1 ? `${k}+${Math.round(v * 100)}%` : `${k}+${v}`
            ).join('  ');
            this._add(this.add.text(x + 45, y + 52, statStr, {
                fontSize: '11px', fontFamily: 'monospace', color: '#8888aa'
            }));
        }

        this._add(this.add.text(x + w - 350, y + 12, `현재 입찰가: ${bid.currentBid}G`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }));

        this._add(this.add.text(x + w - 350, y + 34, `시세: ${item.value}G  |  경쟁자: ${bid.npcBidders}명  |  남은 라운드: ${bid.roundsLeft}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#888899'
        }));

        if (isLeading) {
            this._add(this.add.text(x + w - 350, y + 52, '✓ 최고 입찰자', {
                fontSize: '11px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
            }));
        }

        this._add(this.add.text(x + w - 350, y + 70, `최소 입찰: ${minBid}G`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#886666'
        }));

        this._add(UIButton.create(this, x + w - 200, y + 95, 120, 28, `입찰 (${minBid}G)`, {
            color: canBid ? 0x446644 : 0x333333,
            hoverColor: canBid ? 0x558855 : 0x333333,
            textColor: canBid ? '#44ff88' : '#555555',
            fontSize: 11,
            onClick: () => {
                if (!canBid) { UIToast.show(this, '골드 부족', { color: '#ff6666' }); return; }
                if (bid.playerBid > 0) {
                    GuildManager.addGold(gs, bid.playerBid);
                }
                GuildManager.spendGold(gs, minBid);
                bid.playerBid = minBid;
                bid.currentBid = minBid;

                this._npcBidResponse(gs, bid);

                SaveManager.save(gs);
                this.goldText.setText(`${gs.gold}G`);
                this._clearContent();
                this._drawContent();
                UIToast.show(this, `${item.name}에 ${minBid}G 입찰!`, { color: '#44aaff' });
            }
        }));

        this._add(UIButton.create(this, x + w - 60, y + 95, 100, 28, '즉시 구매', {
            color: 0x664422, hoverColor: 0x886644, textColor: '#ffcc88', fontSize: 11,
            onClick: () => {
                const buyoutPrice = Math.floor(item.value * 1.5);
                const cap = GuildManager.getStorageCapacity(gs);
                if (gs.storage.length >= cap) { UIToast.show(this, '보관함 가득', { color: '#ff6666' }); return; }
                const totalCost = buyoutPrice - (bid.playerBid || 0);
                if (gs.gold < totalCost) { UIToast.show(this, `골드 부족 (${totalCost}G 필요)`, { color: '#ff6666' }); return; }
                GuildManager.spendGold(gs, totalCost);
                bid.resolved = true;
                StorageManager.addItem(gs, item);
                gs.auctionHistory = gs.auctionHistory || [];
                gs.auctionHistory.unshift({ action: 'buyout', name: item.name, price: buyoutPrice, time: Date.now() });
                GuildManager.addMessage(gs, `입찰 즉시 구매: ${item.name} (${buyoutPrice}G)`);
                SaveManager.save(gs);
                UIToast.show(this, `${item.name} 즉시 구매! -${buyoutPrice}G`, { color: '#ffaa44' });
                this.goldText.setText(`${gs.gold}G`);
                this._clearContent();
                this._drawContent();
            }
        }));

        const buyoutPrice = Math.floor(item.value * 1.5);
        this._add(this.add.text(x + w - 60, y + 70, `즉구가: ${buyoutPrice}G`, {
            fontSize: '9px', fontFamily: 'monospace', color: '#886644'
        }).setOrigin(0.5));
    }

    _npcBidResponse(gs, bid) {
        if (bid.npcBidders <= 0) return;
        const willBid = Math.random() < 0.4 + bid.npcBidders * 0.12;
        if (willBid) {
            const npcBid = bid.currentBid + bid.minIncrement + Math.floor(Math.random() * bid.minIncrement);
            bid.currentBid = npcBid;
            bid.roundsLeft--;
            if (bid.roundsLeft <= 0) {
                bid.resolved = true;
                if (bid.playerBid >= bid.currentBid) {
                    const cap = GuildManager.getStorageCapacity(gs);
                    if (gs.storage.length < cap) {
                        StorageManager.addItem(gs, bid.item);
                        gs.auctionHistory = gs.auctionHistory || [];
                        gs.auctionHistory.unshift({ action: 'bid_win', name: bid.item.name, price: bid.playerBid, time: Date.now() });
                        GuildManager.addMessage(gs, `입찰 낙찰: ${bid.item.name} (${bid.playerBid}G)`);
                        UIToast.show(this, `${bid.item.name} 낙찰!`, { color: '#44ff88' });
                    }
                } else {
                    if (bid.playerBid > 0) GuildManager.addGold(gs, bid.playerBid);
                    gs.auctionHistory = gs.auctionHistory || [];
                    gs.auctionHistory.unshift({ action: 'bid_lose', name: bid.item.name, price: bid.currentBid, time: Date.now() });
                    UIToast.show(this, `${bid.item.name} — 패찰`, { color: '#ff6666' });
                }
            } else {
                UIToast.show(this, `경쟁자가 ${npcBid}G로 입찰!`, { color: '#ff8844' });
            }
        } else {
            bid.roundsLeft--;
            if (bid.roundsLeft <= 0 && bid.playerBid > 0) {
                bid.resolved = true;
                const cap = GuildManager.getStorageCapacity(gs);
                if (gs.storage.length < cap) {
                    StorageManager.addItem(gs, bid.item);
                    gs.auctionHistory = gs.auctionHistory || [];
                    gs.auctionHistory.unshift({ action: 'bid_win', name: bid.item.name, price: bid.playerBid, time: Date.now() });
                    GuildManager.addMessage(gs, `입찰 낙찰: ${bid.item.name} (${bid.playerBid}G)`);
                    UIToast.show(this, `${bid.item.name} 낙찰!`, { color: '#44ff88' });
                }
            }
        }
    }

    // --- HISTORY TAB ---
    _drawHistoryTab() {
        const gs = this.gameState;
        const history = gs.auctionHistory || [];

        this._add(this.add.text(640, 95, '최근 거래 내역', {
            fontSize: '13px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5));

        if (history.length === 0) {
            this._add(this.add.text(640, 400, '거래 내역이 없습니다', {
                fontSize: '14px', fontFamily: 'monospace', color: '#555566'
            }).setOrigin(0.5));
            return;
        }

        let cy = 120;
        const actionLabels = {
            buy: '구매', sell: '판매', buyout: '즉시구매',
            bid_win: '낙찰', bid_lose: '패찰'
        };
        const actionColors = {
            buy: '#44aaff', sell: '#ffcc44', buyout: '#ffaa44',
            bid_win: '#44ff88', bid_lose: '#ff6666'
        };

        history.forEach((h, idx) => {
            if (cy > 680) return;
            const bg = this._add(this.add.graphics());
            bg.fillStyle(idx % 2 === 0 ? 0x151525 : 0x1a1a2e, 1);
            bg.fillRect(100, cy, 1080, 28);

            const label = actionLabels[h.action] || h.action;
            this._add(this.add.text(120, cy + 6, `[${label}]`, {
                fontSize: '11px', fontFamily: 'monospace', color: actionColors[h.action] || '#888888', fontStyle: 'bold'
            }));

            this._add(this.add.text(230, cy + 6, h.name, {
                fontSize: '11px', fontFamily: 'monospace', color: '#cccccc'
            }));

            const sign = h.action === 'sell' ? '+' : '-';
            const priceCol = h.action === 'sell' ? '#ffcc44' : h.action === 'bid_lose' ? '#ff6666' : '#aaaacc';
            this._add(this.add.text(700, cy + 6, `${sign}${h.price}G`, {
                fontSize: '11px', fontFamily: 'monospace', color: priceCol
            }));

            cy += 32;
        });
    }
}
