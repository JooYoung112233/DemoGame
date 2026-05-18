class AuctionScene extends Phaser.Scene {
    constructor() { super('AuctionScene'); }

    init(data) { this.gameState = data.gameState; }

    create() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        // ── 배경 ──
        this.add.rectangle(640, 360, 1280, 720, T.bg || 0x1a1510);

        const gs = this.gameState;

        // ── 헤더 바 ──
        const headerBg = this.add.graphics();
        headerBg.fillStyle(T.headerBg || 0x1e1810, 1);
        headerBg.fillRect(0, 0, 1280, T.headerHeight || 55);
        // 헤더 하단 장식선
        headerBg.lineStyle(1, T.ornament || 0x8a7a4a, T.ornamentAlpha || 0.4);
        headerBg.lineBetween(0, (T.headerHeight || 55) - 1, 1280, (T.headerHeight || 55) - 1);
        headerBg.lineStyle(1, T.divider || 0x5a4a2a, 0.3);
        headerBg.lineBetween(0, (T.headerHeight || 55), 1280, (T.headerHeight || 55));

        this.add.text(640, 25, '◈ 경매장 ◈', {
            fontSize: `${(T.fontSize && T.fontSize.title) || 20}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44',
            fontStyle: 'bold'
        }).setOrigin(0.5);

        this.goldText = this.add.text(1260, 25, `${gs.gold}G`, {
            fontSize: `${(T.fontSize && T.fontSize.header) || 16}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44',
            fontStyle: 'bold'
        }).setOrigin(1, 0);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            variant: 'ghost',
            fontSize: (T.fontSize && T.fontSize.body) || 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        this.tab = 'buy';
        this.filterType = 'all';
        this.sortBy = 'price';
        this.scrollOffset = 0;
        if (!gs.auctionStock || gs.auctionStock.length === 0) this._refreshStock();
        if (!gs.auctionHistory) gs.auctionHistory = [];
        if (!gs.consignedItems) gs.consignedItems = [];
        if (!gs.autoAuctionSlots) gs.autoAuctionSlots = 2;
        if (typeof initMarketTrends === 'function') initMarketTrends(gs);

        this._drawTabs();
        this._drawContent();
    }

    _drawTabs() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        if (this._tabObjs) this._tabObjs.forEach(o => o.destroy && o.destroy());
        this._tabObjs = [];
        const tabs = [
            { key: 'buy', label: '구매', x: 380 },
            { key: 'sell', label: '판매', x: 490 },
            { key: 'consign', label: '위탁', x: 600 },
            { key: 'bid', label: '입찰', x: 710 },
            { key: 'history', label: '거래 내역', x: 830 }
        ];
        tabs.forEach(t => {
            const active = this.tab === t.key;
            this._tabObjs.push(UIButton.create(this, t.x, 60, 110, 28, t.label, {
                color: active ? (T.buttonPrimary || 0x8a6a2a) : (T.panelFill || 0x2a2218),
                hoverColor: active ? (T.buttonPrimary || 0x8a6a2a) : (T.cardHover || 0x3a3020),
                textColor: active ? (T.buttonText || '#f0e8d0') : (T.textMuted || '#887860'),
                fontSize: (T.fontSize && T.fontSize.body) || 12,
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
        else if (this.tab === 'consign') this._drawConsignTab();
        else if (this.tab === 'bid') this._drawBidTab();
        else this._drawHistoryTab();
    }

    _refreshStock() {
        const gs = this.gameState;
        const stock = [];
        const count = 6 + Math.floor(Math.random() * 4);
        for (let i = 0; i < count; i++) {
            // 20% 확률로 경매장 전용 아이템
            let item;
            if (Math.random() < 0.20 && typeof generateAuctionItem === 'function') {
                item = generateAuctionItem();
                item.auctionPrice = item.value;
                item.isAuctionExclusive = true;
            } else {
                item = generateItem('common', gs.guildLevel, Math.random() < 0.2 ? 1 : 0);
                const marketMod = typeof getMarketPriceModifier === 'function' ? getMarketPriceModifier(gs, item.type) : 1.0;
                const baseMult = (1.2 + Math.random() * 0.8) * marketMod;
                item.auctionPrice = Math.floor(item.value * baseMult);
            }
            if (!item.isAuctionExclusive && Math.random() < 0.15) {
                item.isHotDeal = true;
                item.auctionPrice = Math.floor(item.auctionPrice * 0.6);
            }
            if (!item.isAuctionExclusive && Math.random() < 0.1) {
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
            // 입찰에도 경매장 전용 등장
            let item;
            if (Math.random() < 0.30 && typeof generateAuctionItem === 'function') {
                item = generateAuctionItem();
            } else {
                item = generateItem('common', gs.guildLevel, 1 + Math.floor(Math.random() * 2));
            }
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
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;

        this._drawFilterBar(90);

        const stock = this._getFilteredStock(gs.auctionStock || []);

        const refreshCost = 80 + gs.guildLevel * 20;
        this._add(UIButton.create(this, 1180, 90, 140, 26, `갱신 (${refreshCost}G)`, {
            variant: 'primary',
            fontSize: (T.fontSize && T.fontSize.caption) || 11,
            disabled: gs.gold < refreshCost,
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
            fontSize: `${(T.fontSize && T.fontSize.caption) || 11}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }));

        if (gs.marketTrends) {
            const trendIcons = { '-1': '▼', '0': '—', '1': '▲' };
            const trendColors = { '-1': '#44ff88', '0': '#888888', '1': '#ff6644' };
            const supplyLabels = { surplus: '과잉', normal: '보통', shortage: '부족' };
            let tx = 350;
            for (const cat of ['equipment', 'material', 'consumable']) {
                const t = gs.marketTrends[cat];
                const catNames = { equipment: '장비', material: '소재', consumable: '소비' };
                const icon = trendIcons[t.trend] || '—';
                const col = trendColors[t.trend] || '#888888';
                const pct = Math.round((t.modifier - 1) * 100);
                const pctStr = pct >= 0 ? `+${pct}%` : `${pct}%`;
                this._add(this.add.text(tx, 107, `${catNames[cat]}: ${icon}${pctStr} (${supplyLabels[t.supply]})`, {
                    fontSize: `${(T.fontSize && T.fontSize.tiny) || 9}px`,
                    fontFamily: T.fontFamily || 'monospace',
                    color: col
                }));
                tx += 180;
            }
        }

        if (stock.length === 0) {
            this._add(this.add.text(640, 400, '해당 조건의 매물이 없습니다', {
                fontSize: `${(T.fontSize && T.fontSize.header) || 14}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
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
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
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
                color: active ? (T.buttonPrimary || 0x8a6a2a) : (T.cardFill || 0x231e14),
                hoverColor: active ? (T.buttonPrimary || 0x8a6a2a) : (T.cardHover || 0x3a3020),
                textColor: active ? (T.buttonText || '#f0e8d0') : (T.textMuted || '#887860'),
                fontSize: (T.fontSize && T.fontSize.caption) || 10,
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
                color: active ? (T.buttonPrimary || 0x8a6a2a) : (T.cardFill || 0x231e14),
                hoverColor: active ? (T.buttonPrimary || 0x8a6a2a) : (T.cardHover || 0x3a3020),
                textColor: active ? (T.buttonText || '#f0e8d0') : (T.textMuted || '#887860'),
                fontSize: (T.fontSize && T.fontSize.caption) || 10,
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
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const cap = GuildManager.getStorageCapacity(gs);
        const canBuy = gs.gold >= item.auctionPrice && gs.storage.length < cap;

        const bg = this._add(this.add.graphics());
        bg.fillStyle(item.isHotDeal ? 0x2a1a14 : (T.cardFill || 0x231e14), 1);
        bg.fillRoundedRect(x, y, w, 58, T.borderRadius || 4);
        bg.lineStyle(1, item.isHotDeal ? 0xff6644 : rarity.color, item.isHotDeal ? 0.6 : 0.3);
        bg.strokeRoundedRect(x, y, w, 58, T.borderRadius || 4);

        const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
        this._add(this.add.text(x + 12, y + 8, typeIcons[item.type] || '?', { fontSize: '16px' }));

        let nameStr = item.name;
        if (item.isBulk) nameStr += ` ×${item.bulkCount}`;
        this._add(this.add.text(x + 38, y + 8, nameStr, {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }));

        const tagX = x + 38;
        let tagOff = 0;
        if (item.isHotDeal) {
            this._add(this.add.text(x + 38 + nameStr.length * 8 + 10, y + 10, '🔥 특가', {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: '#ff6644', fontStyle: 'bold'
            }));
        }
        if (item.isBulk) {
            this._add(this.add.text(x + 38 + nameStr.length * 8 + 10 + (item.isHotDeal ? 55 : 0), y + 10, '📦 묶음', {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: '#44aaff'
            }));
        }

        this._add(this.add.text(x + 38, y + 30, `[${rarity.name}] ${item.desc || ''}`, {
            fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }));

        if (item.stats) {
            const statStr = Object.entries(item.stats).map(([k, v]) =>
                typeof v === 'number' && v < 1 ? `${k}+${Math.round(v * 100)}%` : `${k}+${v}`
            ).join('  ');
            this._add(this.add.text(x + 400, y + 10, statStr, {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 11}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textSecondary || '#b8a888'
            }));
        }

        const marketValue = item.value * (item.isBulk ? item.bulkCount : 1);
        const priceRatio = item.auctionPrice / marketValue;
        const priceColor = priceRatio <= 0.7 ? '#44ff88' : priceRatio <= 1.0 ? '#ffcc44' : '#ff8866';

        this._add(this.add.text(x + w - 220, y + 8, `${item.auctionPrice}G`, {
            fontSize: '16px', fontFamily: T.fontFamily || 'monospace', color: priceColor, fontStyle: 'bold'
        }));

        const valueLabel = priceRatio <= 0.7 ? '매우 저렴' : priceRatio <= 1.0 ? '적정가' : priceRatio <= 1.5 ? '약간 비쌈' : '비쌈';
        const valueLabelColor = priceRatio <= 0.7 ? '#44ff88' : priceRatio <= 1.0 ? '#aaaa88' : '#ff8866';
        this._add(this.add.text(x + w - 220, y + 32, `시세 대비: ${valueLabel}`, {
            fontSize: `${(T.fontSize && T.fontSize.tiny) || 9}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: valueLabelColor
        }));

        this._add(UIButton.create(this, x + w - 55, y + 30, 90, 28, '구매', {
            variant: 'primary',
            fontSize: (T.fontSize && T.fontSize.body) || 12,
            disabled: !canBuy,
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
                if (typeof processMerchantAction === 'function') {
                    processMerchantAction(gs, item.type === 'equipment' ? 'buy_equipment' : 'buy');
                }
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
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const feeTable = { common: 10, uncommon: 15, rare: 20, epic: 25, legendary: 30 };

        this._drawFilterBar(90);

        this._add(this.add.text(640, 112, '판매 수수료: 일반 10% | 고급 15% | 희귀 20% | 에픽 25% | 전설 30%', {
            fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5));

        // --- Bulk sell buttons ---
        const allSellable = gs.storage.filter(i => i.value > 0);
        const commonItems = allSellable.filter(i => i.rarity === 'common');
        const materialItems = allSellable.filter(i => i.type === 'material');

        const calcBulkTotal = (items) => items.reduce((sum, item) => {
            const fee = feeTable[item.rarity] || 10;
            return sum + Math.floor(item.value * (1 - fee / 100));
        }, 0);

        const commonTotal = calcBulkTotal(commonItems);
        const materialTotal = calcBulkTotal(materialItems);

        if (!this._bulkSellConfirm) this._bulkSellConfirm = {};

        // "일반 전체 판매" button
        const commonLabel = this._bulkSellConfirm.common
            ? `정말 판매? (${commonItems.length}개 → +${commonTotal}G)`
            : `일반 전체 판매 (${commonItems.length}개 / +${commonTotal}G)`;
        const commonEnabled = commonItems.length > 0;
        this._add(UIButton.create(this, 200, 132, 280, 26, commonLabel, {
            variant: this._bulkSellConfirm.common ? 'danger' : 'primary',
            fontSize: (T.fontSize && T.fontSize.caption) || 10,
            disabled: !commonEnabled,
            onClick: () => {
                if (!commonEnabled) return;
                if (!this._bulkSellConfirm.common) {
                    this._bulkSellConfirm.common = true;
                    this._clearContent();
                    this._drawContent();
                    return;
                }
                // Execute bulk sell
                let totalGold = 0;
                const ids = commonItems.map(i => i.id);
                ids.forEach(id => {
                    const item = gs.storage.find(i => i.id === id);
                    if (!item) return;
                    const fee = feeTable[item.rarity] || 10;
                    const price = Math.floor(item.value * (1 - fee / 100));
                    totalGold += price;
                    StorageManager.removeItem(gs, id);
                });
                GuildManager.addGold(gs, totalGold);
                gs.auctionHistory = gs.auctionHistory || [];
                gs.auctionHistory.unshift({ action: 'sell', name: `일반 일괄 (${ids.length}개)`, price: totalGold, rarity: 'common', time: Date.now() });
                if (gs.auctionHistory.length > 20) gs.auctionHistory.length = 20;
                GuildManager.addMessage(gs, `경매장 일괄 판매: 일반 ${ids.length}개 (+${totalGold}G)`);
                if (typeof updateMarketTrends === 'function') updateMarketTrends(gs, 'bulk_sell');
                SaveManager.save(gs);
                UIToast.show(this, `일반 ${ids.length}개 일괄 판매! +${totalGold}G`, { color: '#ffcc44' });
                this._bulkSellConfirm = {};
                this.goldText.setText(`${gs.gold}G`);
                this._clearContent();
                this._drawContent();
            }
        }));

        // "소재 전체 판매" button
        const matLabel = this._bulkSellConfirm.material
            ? `정말 판매? (${materialItems.length}개 → +${materialTotal}G)`
            : `소재 전체 판매 (${materialItems.length}개 / +${materialTotal}G)`;
        const matEnabled = materialItems.length > 0;
        this._add(UIButton.create(this, 500, 132, 280, 26, matLabel, {
            variant: this._bulkSellConfirm.material ? 'danger' : 'primary',
            fontSize: (T.fontSize && T.fontSize.caption) || 10,
            disabled: !matEnabled,
            onClick: () => {
                if (!matEnabled) return;
                if (!this._bulkSellConfirm.material) {
                    this._bulkSellConfirm.material = true;
                    this._clearContent();
                    this._drawContent();
                    return;
                }
                let totalGold = 0;
                const ids = materialItems.map(i => i.id);
                ids.forEach(id => {
                    const item = gs.storage.find(i => i.id === id);
                    if (!item) return;
                    const fee = feeTable[item.rarity] || 10;
                    const price = Math.floor(item.value * (1 - fee / 100));
                    totalGold += price;
                    StorageManager.removeItem(gs, id);
                });
                GuildManager.addGold(gs, totalGold);
                gs.auctionHistory = gs.auctionHistory || [];
                gs.auctionHistory.unshift({ action: 'sell', name: `소재 일괄 (${ids.length}개)`, price: totalGold, rarity: 'common', time: Date.now() });
                if (gs.auctionHistory.length > 20) gs.auctionHistory.length = 20;
                GuildManager.addMessage(gs, `경매장 일괄 판매: 소재 ${ids.length}개 (+${totalGold}G)`);
                if (typeof updateMarketTrends === 'function') updateMarketTrends(gs, 'bulk_sell');
                SaveManager.save(gs);
                UIToast.show(this, `소재 ${ids.length}개 일괄 판매! +${totalGold}G`, { color: '#ffcc44' });
                this._bulkSellConfirm = {};
                this.goldText.setText(`${gs.gold}G`);
                this._clearContent();
                this._drawContent();
            }
        }));

        // --- Filtered item list ---
        const sellable = this._getFilteredSellable(gs);

        if (sellable.length === 0) {
            this._add(this.add.text(640, 400, '판매할 아이템이 없습니다', {
                fontSize: `${(T.fontSize && T.fontSize.header) || 14}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5));
            return;
        }

        let cy = 158;
        sellable.forEach(item => {
            if (cy > 650) return;
            this._drawSellRow(item, 40, cy, 1200);
            cy += 60;
        });

        // --- Total value footer ---
        const totalValue = allSellable.reduce((sum, item) => {
            const fee = feeTable[item.rarity] || 10;
            return sum + Math.floor(item.value * (1 - fee / 100));
        }, 0);
        const footerBg = this._add(this.add.graphics());
        footerBg.fillStyle(T.headerBg || 0x1e1810, 1);
        footerBg.fillRoundedRect(40, 672, 1200, 30, T.borderRadius || 4);
        footerBg.lineStyle(1, T.panelStroke || 0x5a4a2a, 0.5);
        footerBg.strokeRoundedRect(40, 672, 1200, 30, T.borderRadius || 4);
        this._add(this.add.text(640, 687, `보관함 전체 매각 시: ${totalValue}G  (아이템 ${allSellable.length}개)`, {
            fontSize: `${(T.fontSize && T.fontSize.body) || 12}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44',
            fontStyle: 'bold'
        }).setOrigin(0.5));
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
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const feePercent = { common: 10, uncommon: 15, rare: 20, epic: 25, legendary: 30 }[item.rarity] || 10;
        const marketMod = typeof getMarketPriceModifier === 'function' ? getMarketPriceModifier(gs, item.type) : 1.0;
        const sellPrice = Math.floor(item.value * (1 - feePercent / 100) * marketMod);

        const bg = this._add(this.add.graphics());
        bg.fillStyle(T.cardFill || 0x231e14, 1);
        bg.fillRoundedRect(x, y, w, 50, T.borderRadius || 4);
        bg.lineStyle(1, rarity.color, 0.3);
        bg.strokeRoundedRect(x, y, w, 50, T.borderRadius || 4);

        const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
        this._add(this.add.text(x + 12, y + 8, typeIcons[item.type] || '?', { fontSize: '14px' }));

        this._add(this.add.text(x + 38, y + 8, `${item.name} [${rarity.name}]`, {
            fontSize: `${(T.fontSize && T.fontSize.body) || 12}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: rarity.textColor, fontStyle: 'bold'
        }));

        this._add(this.add.text(x + 38, y + 28, `${item.desc || ''} ${item.stats ? Object.entries(item.stats).map(([k, v]) => typeof v === 'number' && v < 1 ? `${k}+${Math.round(v * 100)}%` : `${k}+${v}`).join(' ') : ''}`, {
            fontSize: `${(T.fontSize && T.fontSize.tiny) || 9}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }));

        this._add(this.add.text(x + w - 250, y + 8, `시세: ${item.value}G`, {
            fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }));

        this._add(this.add.text(x + w - 250, y + 26, `수수료 ${feePercent}% → +${sellPrice}G`, {
            fontSize: `${(T.fontSize && T.fontSize.caption) || 11}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44',
            fontStyle: 'bold'
        }));

        // Sell button
        this._add(UIButton.create(this, x + w - 55, y + 26, 90, 26, '판매', {
            variant: 'primary',
            fontSize: (T.fontSize && T.fontSize.caption) || 11,
            onClick: () => {
                StorageManager.removeItem(gs, item.id);
                GuildManager.addGold(gs, sellPrice);

                gs.auctionHistory = gs.auctionHistory || [];
                gs.auctionHistory.unshift({ action: 'sell', name: item.name, price: sellPrice, rarity: item.rarity, time: Date.now() });
                if (gs.auctionHistory.length > 20) gs.auctionHistory.length = 20;

                GuildManager.addMessage(gs, `경매장 판매: ${item.name} (+${sellPrice}G)`);
                if (typeof processMerchantAction === 'function') {
                    processMerchantAction(gs, item.type === 'equipment' ? 'sell_equipment' : 'sell');
                }
                SaveManager.save(gs);
                UIToast.show(this, `${item.name} 판매! +${sellPrice}G`, { color: '#ffcc44' });
                this.goldText.setText(`${gs.gold}G`);
                this._clearContent();
                this._drawContent();
            }
        }));

        // Haggling button — only for equipment items, once per item
        if (item.type === 'equipment' && !item._haggled) {
            this._add(UIButton.create(this, x + w - 155, y + 26, 80, 26, '흥정', {
                variant: 'info',
                fontSize: (T.fontSize && T.fontSize.caption) || 11,
                onClick: () => {
                    item._haggled = true;
                    const roll = Math.random();
                    let multiplier, resultMsg, resultColor;
                    if (roll < 0.5) {
                        // 50% chance: 1.2x price
                        multiplier = 1.2;
                        const bonus = Math.round((multiplier - 1) * 100);
                        resultMsg = `흥정 성공! +${bonus}%`;
                        resultColor = '#44ff88';
                    } else if (roll < 0.8) {
                        // 30% chance: same price
                        multiplier = 1.0;
                        resultMsg = '흥정 무승부... 가격 변동 없음';
                        resultColor = '#aaaaaa';
                    } else {
                        // 20% chance: 0.9x price
                        multiplier = 0.9;
                        const penalty = Math.round((1 - multiplier) * 100);
                        resultMsg = `흥정 실패... -${penalty}%`;
                        resultColor = '#ff6666';
                    }
                    item._haggleMultiplier = multiplier;
                    SaveManager.save(gs);
                    UIToast.show(this, resultMsg, { color: resultColor });
                    this._clearContent();
                    this._drawContent();
                }
            }));
        } else if (item.type === 'equipment' && item._haggled) {
            // Show haggle result indicator
            const mult = item._haggleMultiplier || 1.0;
            const hagglePrice = Math.floor(sellPrice * mult);
            const diffG = hagglePrice - sellPrice;
            const haggleLabel = diffG > 0 ? `흥정가 +${diffG}G` : diffG < 0 ? `흥정가 ${diffG}G` : '흥정가 동일';
            const haggleColor = diffG > 0 ? '#44ff88' : diffG < 0 ? '#ff6666' : '#aaaaaa';
            this._add(this.add.text(x + w - 190, y + 30, haggleLabel, {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: haggleColor, fontStyle: 'bold'
            }));

            // Override the sell button price if haggled — re-draw sell button with haggle price
            // We already drew the default sell button above, so we override by adding a new one on top
            this._add(UIButton.create(this, x + w - 55, y + 26, 90, 26, `판매 ${hagglePrice}G`, {
                variant: 'primary',
                fontSize: (T.fontSize && T.fontSize.caption) || 10,
                onClick: () => {
                    StorageManager.removeItem(gs, item.id);
                    GuildManager.addGold(gs, hagglePrice);

                    gs.auctionHistory = gs.auctionHistory || [];
                    gs.auctionHistory.unshift({ action: 'sell', name: item.name, price: hagglePrice, rarity: item.rarity, time: Date.now() });
                    if (gs.auctionHistory.length > 20) gs.auctionHistory.length = 20;

                    GuildManager.addMessage(gs, `경매장 흥정 판매: ${item.name} (+${hagglePrice}G)`);
                    SaveManager.save(gs);
                    UIToast.show(this, `${item.name} 흥정 판매! +${hagglePrice}G`, { color: '#ffcc44' });
                    this.goldText.setText(`${gs.gold}G`);
                    this._clearContent();
                    this._drawContent();
                }
            }));
        }
    }

    // --- BID TAB ---
    _drawBidTab() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const bids = gs.auctionBids || [];
        const active = bids.filter(b => !b.resolved);

        this._add(this.add.text(640, 95, '입찰 — 경쟁 입찰로 고급 아이템을 저렴하게 획득하세요', {
            fontSize: `${(T.fontSize && T.fontSize.caption) || 11}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        }).setOrigin(0.5));

        if (active.length === 0) {
            this._add(this.add.text(640, 400, '현재 진행 중인 입찰이 없습니다\n구매 탭에서 목록을 갱신하면 새 입찰이 등장합니다', {
                fontSize: '13px', fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860', align: 'center'
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
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const item = bid.item;
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const isLeading = bid.playerBid > 0 && bid.playerBid >= bid.currentBid;
        const minBid = bid.currentBid + bid.minIncrement;
        const canBid = gs.gold >= minBid;

        const bg = this._add(this.add.graphics());
        bg.fillStyle(isLeading ? 0x1a2a1a : (T.cardFill || 0x231e14), 1);
        bg.fillRoundedRect(x, y, w, 125, T.borderRadius || 6);
        bg.lineStyle(2, isLeading ? 0x44ff88 : rarity.color, 0.5);
        bg.strokeRoundedRect(x, y, w, 125, T.borderRadius || 6);

        const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
        this._add(this.add.text(x + 15, y + 12, typeIcons[item.type] || '?', { fontSize: '18px' }));

        this._add(this.add.text(x + 45, y + 12, item.name, {
            fontSize: '15px', fontFamily: T.fontFamily || 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }));

        this._add(this.add.text(x + 45, y + 34, `[${rarity.name}] ${item.desc || ''}`, {
            fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }));

        if (item.stats) {
            const statStr = Object.entries(item.stats).map(([k, v]) =>
                typeof v === 'number' && v < 1 ? `${k}+${Math.round(v * 100)}%` : `${k}+${v}`
            ).join('  ');
            this._add(this.add.text(x + 45, y + 52, statStr, {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 11}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textSecondary || '#b8a888'
            }));
        }

        this._add(this.add.text(x + w - 350, y + 12, `현재 입찰가: ${bid.currentBid}G`, {
            fontSize: '14px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold'
        }));

        this._add(this.add.text(x + w - 350, y + 34, `시세: ${item.value}G  |  경쟁자: ${bid.npcBidders}명  |  남은 라운드: ${bid.roundsLeft}`, {
            fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        }));

        if (isLeading) {
            this._add(this.add.text(x + w - 350, y + 52, '✓ 최고 입찰자', {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 11}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textSuccess || '#66bb55', fontStyle: 'bold'
            }));
        }

        this._add(this.add.text(x + w - 350, y + 70, `최소 입찰: ${minBid}G`, {
            fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textDanger || '#cc4422'
        }));

        this._add(UIButton.create(this, x + w - 200, y + 95, 120, 28, `입찰 (${minBid}G)`, {
            variant: 'primary',
            fontSize: (T.fontSize && T.fontSize.caption) || 11,
            disabled: !canBid,
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
            variant: 'primary',
            fontSize: (T.fontSize && T.fontSize.caption) || 11,
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
            fontSize: `${(T.fontSize && T.fontSize.tiny) || 9}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textAccent || '#cc8833'
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

    // --- CONSIGN TAB ---
    _drawConsignTab() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const maxSlots = gs.autoAuctionSlots || 2;
        const consigned = gs.consignedItems || [];

        this._add(UIPanel.create(this, 40, 90, 1200, 600, { title: `위탁 판매 (${consigned.length}/${maxSlots} 슬롯)` }));

        this._add(this.add.text(640, 120, '아이템을 등록하면 다음 전투 출발 시 자동으로 판매를 시도합니다', {
            fontSize: `${(T.fontSize && T.fontSize.caption) || 11}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        }).setOrigin(0.5));

        const favorability = gs.merchantFavor?.auction || 0;
        const sellChance = Math.min(90, 50 + favorability * 5);
        const priceAccuracy = Math.min(95, 70 + favorability * 3);
        this._add(this.add.text(640, 138, `판매 확률: ${sellChance}%  |  시세 적중률: ${priceAccuracy}%  |  상인 호감도: ${favorability}`, {
            fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textSuccess || '#66bb55'
        }).setOrigin(0.5));

        let cy = 160;
        consigned.forEach((entry, idx) => {
            if (cy > 430) return;
            const rarity = ITEM_RARITY[entry.item.rarity] || ITEM_RARITY.common;
            const bg = this._add(this.add.graphics());
            bg.fillStyle(0x1a2a1a, 1);
            bg.fillRoundedRect(60, cy, 1160, 50, T.borderRadius || 4);
            bg.lineStyle(1, rarity.color, 0.4);
            bg.strokeRoundedRect(60, cy, 1160, 50, T.borderRadius || 4);

            const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
            this._add(this.add.text(80, cy + 8, typeIcons[entry.item.type] || '?', { fontSize: '14px' }));
            this._add(this.add.text(106, cy + 8, `${entry.item.name} [${rarity.name}]`, {
                fontSize: `${(T.fontSize && T.fontSize.body) || 12}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: rarity.textColor, fontStyle: 'bold'
            }));
            this._add(this.add.text(106, cy + 28, `시세: ${entry.item.value}G  |  희망가: ${entry.desiredPrice}G`, {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textSecondary || '#b8a888'
            }));

            this._add(UIButton.create(this, 1160, cy + 25, 80, 26, '회수', {
                variant: 'danger',
                fontSize: (T.fontSize && T.fontSize.caption) || 11,
                onClick: () => {
                    gs.consignedItems.splice(idx, 1);
                    StorageManager.addItem(gs, entry.item);
                    SaveManager.save(gs);
                    UIToast.show(this, `${entry.item.name} 회수!`);
                    this._clearContent();
                    this._drawContent();
                }
            }));

            cy += 56;
        });

        if (consigned.length >= maxSlots) {
            this._add(this.add.text(640, cy + 20, '슬롯이 가득 찼습니다 (길드 레벨 UP 또는 이벤트로 확장)', {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 11}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textAccent || '#cc8833'
            }).setOrigin(0.5));
        }

        cy = Math.max(cy + 10, 440);
        this._add(this.add.text(640, cy, '── 보관함에서 위탁할 아이템 선택 ──', {
            fontSize: `${(T.fontSize && T.fontSize.body) || 12}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        }).setOrigin(0.5));
        cy += 20;

        const sellable = gs.storage.filter(i => i.value > 0);
        if (sellable.length === 0) {
            this._add(this.add.text(640, cy + 30, '보관함에 아이템이 없습니다', {
                fontSize: `${(T.fontSize && T.fontSize.body) || 12}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5));
            return;
        }

        sellable.forEach(item => {
            if (cy > 650) return;
            const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
            const canConsign = consigned.length < maxSlots;
            const desiredPrice = Math.floor(item.value * (0.9 + Math.random() * 0.3));

            const bg = this._add(this.add.graphics());
            bg.fillStyle(T.cardFill || 0x231e14, 1);
            bg.fillRoundedRect(60, cy, 1160, 42, 3);

            const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
            this._add(this.add.text(80, cy + 6, typeIcons[item.type] || '?', { fontSize: '13px' }));
            this._add(this.add.text(106, cy + 6, `${item.name} [${rarity.name}]`, {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 11}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: rarity.textColor
            }));
            this._add(this.add.text(106, cy + 24, `시세: ${item.value}G`, {
                fontSize: `${(T.fontSize && T.fontSize.tiny) || 9}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }));

            this._add(UIButton.create(this, 1160, cy + 21, 80, 26, '위탁', {
                variant: 'primary',
                fontSize: (T.fontSize && T.fontSize.caption) || 11,
                disabled: !canConsign,
                onClick: () => {
                    if (!canConsign) { UIToast.show(this, '슬롯 부족', { color: '#ff6666' }); return; }
                    StorageManager.removeItem(gs, item.id);
                    const price = Math.floor(item.value * (0.9 + Math.random() * 0.3));
                    gs.consignedItems.push({ item, desiredPrice: price, registeredAt: Date.now() });
                    SaveManager.save(gs);
                    UIToast.show(this, `${item.name} 위탁 등록! (희망가 ${price}G)`, { color: '#44ff88' });
                    this._clearContent();
                    this._drawContent();
                }
            }));

            cy += 48;
        });
    }

    static processConsignments(gs) {
        if (!gs.consignedItems || gs.consignedItems.length === 0) return [];
        const results = [];
        const favorability = gs.merchantFavor?.auction || 0;
        const sellChance = Math.min(90, 50 + favorability * 5);
        const priceAccuracy = Math.min(95, 70 + favorability * 3);
        const remaining = [];

        for (const entry of gs.consignedItems) {
            const roll = Math.random() * 100;
            if (roll < sellChance) {
                const accuracyRoll = Math.random() * 100;
                let finalPrice;
                if (accuracyRoll < priceAccuracy) {
                    finalPrice = entry.desiredPrice;
                } else {
                    finalPrice = Math.floor(entry.desiredPrice * (0.6 + Math.random() * 0.4));
                }
                finalPrice = Math.max(1, finalPrice);
                GuildManager.addGold(gs, finalPrice);
                results.push({ item: entry.item, price: finalPrice, sold: true });
                gs.auctionHistory = gs.auctionHistory || [];
                gs.auctionHistory.unshift({ action: 'consign_sell', name: entry.item.name, price: finalPrice, time: Date.now() });
                if (gs.auctionHistory.length > 20) gs.auctionHistory.length = 20;
                GuildManager.addMessage(gs, `위탁 판매: ${entry.item.name} (+${finalPrice}G)`);
            } else {
                remaining.push(entry);
                results.push({ item: entry.item, sold: false });
            }
        }
        gs.consignedItems = remaining;
        return results;
    }

    // --- HISTORY TAB ---
    _drawHistoryTab() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const history = gs.auctionHistory || [];

        this._add(this.add.text(640, 95, '최근 거래 내역', {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        }).setOrigin(0.5));

        if (history.length === 0) {
            this._add(this.add.text(640, 400, '거래 내역이 없습니다', {
                fontSize: `${(T.fontSize && T.fontSize.header) || 14}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5));
            return;
        }

        let cy = 120;
        const actionLabels = {
            buy: '구매', sell: '판매', buyout: '즉시구매',
            bid_win: '낙찰', bid_lose: '패찰', consign_sell: '위탁판매'
        };
        const actionColors = {
            buy: '#44aaff', sell: '#ffcc44', buyout: '#ffaa44',
            bid_win: '#44ff88', bid_lose: '#ff6666', consign_sell: '#88ffaa'
        };

        history.forEach((h, idx) => {
            if (cy > 680) return;
            const bg = this._add(this.add.graphics());
            bg.fillStyle(idx % 2 === 0 ? (T.panelFill || 0x2a2218) : (T.cardFill || 0x231e14), 1);
            bg.fillRect(100, cy, 1080, 28);

            const label = actionLabels[h.action] || h.action;
            this._add(this.add.text(120, cy + 6, `[${label}]`, {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 11}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: actionColors[h.action] || '#888888', fontStyle: 'bold'
            }));

            this._add(this.add.text(230, cy + 6, h.name, {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 11}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textPrimary || '#e8d8c0'
            }));

            const sign = h.action === 'sell' ? '+' : '-';
            const priceCol = h.action === 'sell' ? (T.textGold || '#ffcc44') : h.action === 'bid_lose' ? '#ff6666' : (T.textPrimary || '#e8d8c0');
            this._add(this.add.text(700, cy + 6, `${sign}${h.price}G`, {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 11}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: priceCol
            }));

            cy += 32;
        });
    }
}
