class ShopScene extends Phaser.Scene {
    constructor() {
        super('ShopScene');
    }

    init(data) {
        this.round = data.round || 0;
        this.act = data.act != null ? data.act : 0;
        this.map = data.map || null;
        this.playerState = data.playerState;
        this.goldReward = data.goldReward || 0;
        // P0: progressive discard cost — resets each shop visit
        this.discardCount = 0;
        // P1: purchase limits per visit
        this.boughtItems = {};      // item.id → true
        this.boughtCombos = {};     // combo.id → true
        this.boughtRelics = {};     // relic.id → true
        this.boughtSymbols = {};    // shopIdx → true
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a1a');
        this.shopSymbols = this._generateShopSymbols();
        this.shopItems = this._generateShopItems();
        this.shopCombos = this._generateShopCombos();

        this._drawHeader();
        this._drawTabs();
        this.activeTab = 'symbols';
        this._drawContent();
        this._drawPlayerInfo();
        this._drawNextButton();
    }

    _getDiscardCost() {
        return 2 + (this.discardCount + 1); // 1st=3, 2nd=4, 3rd=5...
    }

    _drawHeader() {
        const W = 1280;
        this.add.graphics().fillStyle(0x111128, 1).fillRect(0, 0, W, 60);
        const headerText = this.goldReward > 0
            ? `라운드 클리어!  +${this.goldReward}G`
            : '🏪 소품실';
        this.add.text(W / 2, 18, headerText, {
            fontSize: '20px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.goldText = this.add.text(W / 2, 42, `🪙 ${this.playerState.gold}`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc00'
        }).setOrigin(0.5);
    }

    _drawTabs() {
        const W = 1280;
        const tabs = [
            { key: 'symbols', label: '🎰 심볼', x: W / 2 - 280 },
            { key: 'items', label: '🧪 아이템', x: W / 2 - 90 },
            { key: 'combos', label: '⚡ 조합', x: W / 2 + 90 },
            { key: 'relics', label: '💎 유물', x: W / 2 + 280 },
        ];
        this.tabButtons = [];
        for (const tab of tabs) {
            const btn = this.add.text(tab.x, 78, tab.label, {
                fontSize: '16px', fontFamily: 'monospace', color: '#888888',
                backgroundColor: '#1a1a35', padding: { x: 16, y: 6 }
            }).setOrigin(0.5).setInteractive({ useHandCursor: true });
            btn.tabKey = tab.key;
            btn.on('pointerdown', () => {
                this.activeTab = tab.key;
                this._drawContent();
                this._updateTabs();
            });
            this.tabButtons.push(btn);
        }
        this._updateTabs();
    }

    _updateTabs() {
        for (const btn of this.tabButtons) {
            btn.setColor(btn.tabKey === this.activeTab ? '#ffffff' : '#666666');
        }
    }

    _drawContent() {
        if (this.contentContainer) this.contentContainer.destroy();
        this.contentContainer = this.add.container(0, 0);

        if (this.activeTab === 'symbols') this._drawSymbolShop();
        else if (this.activeTab === 'items') this._drawItemShop();
        else if (this.activeTab === 'combos') this._drawComboShop();
        else if (this.activeTab === 'relics') this._drawRelicShop();
    }

    _drawSymbolShop() {
        const W = 1280, startY = 120;
        this.add.text(W / 2, startY, '심볼 구매 — 대본에 추가', {
            fontSize: '14px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);

        const cols = Math.min(this.shopSymbols.length, 5);
        const startX = W / 2 - (cols - 1) * 130 / 2;

        for (let i = 0; i < this.shopSymbols.length; i++) {
            const sym = SYMBOL_DATA[this.shopSymbols[i]];
            if (!sym) continue;
            const x = startX + i * 130;
            const y = startY + 110;
            const sold = !!this.boughtSymbols[i];

            const bg = this.add.graphics();
            bg.fillStyle(sold ? 0x111118 : 0x1a1a35, 1);
            bg.lineStyle(2, sold ? 0x333333 : sym.color, sold ? 0.3 : 0.6);
            bg.fillRoundedRect(x - 55, y - 60, 110, 150, 10);
            bg.strokeRoundedRect(x - 55, y - 60, 110, 150, 10);
            this.contentContainer.add(bg);

            const icon = this.add.text(x, y - 30, sym.icon, { fontSize: '32px' }).setOrigin(0.5).setAlpha(sold ? 0.3 : 1);
            const name = this.add.text(x, y + 5, sym.name, {
                fontSize: '14px', fontFamily: 'monospace', color: sold ? '#555555' : '#ffffff'
            }).setOrigin(0.5);
            const desc = this.add.text(x, y + 25, sym.desc, {
                fontSize: '10px', fontFamily: 'monospace', color: '#999999',
                wordWrap: { width: 100 }, align: 'center'
            }).setOrigin(0.5);
            const cost = this.add.text(x, y + 55, sold ? '✔ 구매완료' : `🪙${sym.cost}`, {
                fontSize: sold ? '11px' : '14px', fontFamily: 'monospace', color: sold ? '#666666' : '#ffcc00'
            }).setOrigin(0.5);
            this.contentContainer.add([icon, name, desc, cost]);

            if (!sold) {
                const hitArea = this.add.rectangle(x, y + 10, 110, 150, 0x000000, 0)
                    .setInteractive({ useHandCursor: true });
                this.contentContainer.add(hitArea);

                hitArea.on('pointerover', () => {
                    bg.clear().fillStyle(0x2a2a50, 1).lineStyle(2, 0xffffff, 1)
                        .fillRoundedRect(x - 55, y - 60, 110, 150, 10)
                        .strokeRoundedRect(x - 55, y - 60, 110, 150, 10);
                });
                hitArea.on('pointerout', () => {
                    bg.clear().fillStyle(0x1a1a35, 1).lineStyle(2, sym.color, 0.6)
                        .fillRoundedRect(x - 55, y - 60, 110, 150, 10)
                        .strokeRoundedRect(x - 55, y - 60, 110, 150, 10);
                });
                hitArea.on('pointerdown', () => this._buySymbol(sym.id, i));
            }
        }

        // 심볼 버리기 섹션 with progressive cost
        this._drawRemoveSection(startY + 250);
    }

    _drawRemoveSection(y) {
        const W = 1280;
        const cost = this._getDiscardCost();
        this.add.text(W / 2, y, `🗑️ 심볼 버리기 (${cost}G 소모)`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff8844'
        }).setOrigin(0.5);

        const pool = this.playerState.symbolPool;
        const cols = Math.min(pool.length, 10);
        const startX = W / 2 - (cols - 1) * 50 / 2;

        for (let i = 0; i < pool.length; i++) {
            const sym = SYMBOL_DATA[pool[i]];
            if (!sym) continue;
            const col = i % cols;
            const row = Math.floor(i / cols);
            const x = startX + col * 50;
            const sy = y + 35 + row * 50;
            const isSkull = pool[i] === 'dead';

            const btn = this.add.text(x, sy, sym.icon, {
                fontSize: '24px', backgroundColor: '#1a1a35',
                padding: { x: 4, y: 4 }
            }).setOrigin(0.5).setInteractive({ useHandCursor: true });
            this.contentContainer.add(btn);

            // skull lock overlay
            if (isSkull) {
                const lock = this.add.text(x + 12, sy - 12, '🔒', { fontSize: '12px' }).setOrigin(0.5);
                this.contentContainer.add(lock);
            }

            btn.on('pointerdown', () => {
                // P0: skull cannot be discarded
                if (isSkull) {
                    this._showWarning('저주받은 심볼은 제거할 수 없습니다.');
                    return;
                }
                const curCost = this._getDiscardCost();
                if (this.playerState.gold < curCost) {
                    this._showWarning(`골드가 부족합니다 (필요: ${curCost}G)`);
                    return;
                }
                if (this.playerState.symbolPool.length <= 3) return;
                this.playerState.symbolPool.splice(i, 1);
                this.playerState.gold -= curCost;
                this.discardCount++;
                this._refresh();
            });
        }
    }

    _showWarning(msg) {
        const W = 1280;
        if (this._warningText) this._warningText.destroy();
        this._warningText = this.add.text(W / 2, 560, msg, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff4444',
            backgroundColor: '#330000', padding: { x: 12, y: 6 },
            stroke: '#000000', strokeThickness: 2
        }).setOrigin(0.5).setDepth(300);
        this.time.delayedCall(1500, () => {
            if (this._warningText) { this._warningText.destroy(); this._warningText = null; }
        });
    }

    _drawItemShop() {
        const W = 1280, startY = 120;
        this.add.text(W / 2, startY, '아이템 구매 — 즉시 사용', {
            fontSize: '14px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);

        const items = Object.values(SHOP_ITEMS);
        const startX = W / 2 - (items.length - 1) * 140 / 2;

        for (let i = 0; i < items.length; i++) {
            const item = items[i];
            const x = startX + i * 140;
            const y = startY + 120;
            const bought = !!this.boughtItems[item.id];

            const bg = this.add.graphics();
            bg.fillStyle(bought ? 0x111118 : 0x1a1a35, 1);
            bg.lineStyle(2, bought ? 0x333333 : 0x44aa88, bought ? 0.3 : 0.6);
            bg.fillRoundedRect(x - 60, y - 60, 120, 150, 10);
            bg.strokeRoundedRect(x - 60, y - 60, 120, 150, 10);
            this.contentContainer.add(bg);

            const icon = this.add.text(x, y - 30, item.icon, { fontSize: '28px' }).setOrigin(0.5).setAlpha(bought ? 0.3 : 1);
            const name = this.add.text(x, y, item.name, {
                fontSize: '12px', fontFamily: 'monospace', color: bought ? '#555555' : '#ffffff'
            }).setOrigin(0.5);
            const desc = this.add.text(x, y + 22, item.desc, {
                fontSize: '10px', fontFamily: 'monospace', color: '#999999',
                wordWrap: { width: 110 }, align: 'center'
            }).setOrigin(0.5);
            const cost = this.add.text(x, y + 55, bought ? '✔ 구매완료' : `🪙${item.cost}`, {
                fontSize: bought ? '11px' : '14px', fontFamily: 'monospace', color: bought ? '#666666' : '#ffcc00'
            }).setOrigin(0.5);
            this.contentContainer.add([icon, name, desc, cost]);

            if (!bought) {
                const hitArea = this.add.rectangle(x, y + 10, 120, 150, 0x000000, 0)
                    .setInteractive({ useHandCursor: true });
                this.contentContainer.add(hitArea);
                hitArea.on('pointerdown', () => this._buyItem(item));
            }
        }
    }

    _drawComboShop() {
        const W = 1280, startY = 120;
        this.add.text(W / 2, startY, '조합 강화 — 콤보 효과 증폭', {
            fontSize: '14px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);

        const combos = this.shopCombos;
        const cols = Math.min(combos.length, 4);
        const startX = W / 2 - (cols - 1) * 160 / 2;

        for (let i = 0; i < combos.length; i++) {
            const combo = combos[i];
            const lvl = (this.playerState.comboUpgrades[combo.id] || 0);
            const cost = 8 + lvl * 4;
            const x = startX + i * 160;
            const y = startY + 130;
            const bought = !!this.boughtCombos[combo.id];

            const bg = this.add.graphics();
            bg.fillStyle(bought ? 0x111118 : 0x1a1a35, 1);
            bg.lineStyle(2, bought ? 0x333333 : 0xaa44ff, bought ? 0.3 : 0.6);
            bg.fillRoundedRect(x - 70, y - 70, 140, 180, 10);
            bg.strokeRoundedRect(x - 70, y - 70, 140, 180, 10);
            this.contentContainer.add(bg);

            const nameT = this.add.text(x, y - 48, combo.name, {
                fontSize: '14px', fontFamily: 'monospace', color: bought ? '#555555' : '#ffffff', fontStyle: 'bold'
            }).setOrigin(0.5);
            const recipe = this.add.text(x, y - 28, combo.recipe || '', {
                fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa'
            }).setOrigin(0.5);
            const desc = this.add.text(x, y, combo.desc, {
                fontSize: '10px', fontFamily: 'monospace', color: '#999999',
                wordWrap: { width: 130 }, align: 'center'
            }).setOrigin(0.5);
            const lvlText = this.add.text(x, y + 35, `Lv.${lvl}  →  Lv.${lvl + 1}`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#aa88ff'
            }).setOrigin(0.5);
            const bonus = this.add.text(x, y + 55, `효과 +${(lvl + 1) * 20}%`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#44ff88'
            }).setOrigin(0.5);
            const costText = this.add.text(x, y + 78, bought ? '✔ 구매완료' : `🪙${cost}`, {
                fontSize: bought ? '11px' : '14px', fontFamily: 'monospace', color: bought ? '#666666' : '#ffcc00'
            }).setOrigin(0.5);
            this.contentContainer.add([nameT, recipe, desc, lvlText, bonus, costText]);

            if (!bought) {
                const hitArea = this.add.rectangle(x, y + 10, 140, 180, 0x000000, 0)
                    .setInteractive({ useHandCursor: true });
                this.contentContainer.add(hitArea);
                hitArea.on('pointerdown', () => this._buyComboUpgrade(combo.id, cost));
            }
        }
    }

    _drawRelicShop() {
        const W = 1280, startY = 120;
        this.add.text(W / 2, startY, '유물 구매 — 영구 패시브 효과', {
            fontSize: '14px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);

        const ownedIds = this.playerState.relics || [];
        if (!this._shopRelics) {
            this._shopRelics = getRelicChoices(3, ownedIds);
        }
        const relics = this._shopRelics.filter(r => !ownedIds.includes(r.id));
        const startX = W / 2 - (relics.length - 1) * 180 / 2;

        for (let i = 0; i < relics.length; i++) {
            const relic = relics[i];
            const x = startX + i * 180;
            const y = startY + 130;
            const rarityColor = relic.rarity === 'rare' ? 0xffaa00 :
                                relic.rarity === 'uncommon' ? 0x44aaff : 0x888888;
            const bought = !!this.boughtRelics[relic.id];

            const bg = this.add.graphics();
            bg.fillStyle(bought ? 0x111118 : 0x1a1a35, 1);
            bg.lineStyle(2, bought ? 0x333333 : rarityColor, bought ? 0.3 : 0.6);
            bg.fillRoundedRect(x - 70, y - 70, 140, 190, 10);
            bg.strokeRoundedRect(x - 70, y - 70, 140, 190, 10);
            this.contentContainer.add(bg);

            const icon = this.add.text(x, y - 40, relic.icon, { fontSize: '32px' }).setOrigin(0.5).setAlpha(bought ? 0.3 : 1);
            const name = this.add.text(x, y, relic.name, {
                fontSize: '14px', fontFamily: 'monospace', color: bought ? '#555555' : '#ffffff', fontStyle: 'bold'
            }).setOrigin(0.5);
            const desc = this.add.text(x, y + 25, relic.desc, {
                fontSize: '10px', fontFamily: 'monospace', color: '#aaaaaa',
                wordWrap: { width: 120 }, align: 'center'
            }).setOrigin(0.5);
            const cost = this.add.text(x, y + 65, bought ? '✔ 구매완료' : `🪙${relic.shopCost}`, {
                fontSize: bought ? '11px' : '14px', fontFamily: 'monospace', color: bought ? '#666666' : '#ffcc00'
            }).setOrigin(0.5);
            this.contentContainer.add([icon, name, desc, cost]);

            if (!bought) {
                const hitArea = this.add.rectangle(x, y + 15, 140, 190, 0x000000, 0)
                    .setInteractive({ useHandCursor: true });
                this.contentContainer.add(hitArea);
                hitArea.on('pointerover', () => {
                    bg.clear().fillStyle(0x2a2a50, 1).lineStyle(2, 0xffffff, 1)
                        .fillRoundedRect(x - 70, y - 70, 140, 190, 10)
                        .strokeRoundedRect(x - 70, y - 70, 140, 190, 10);
                });
                hitArea.on('pointerout', () => {
                    bg.clear().fillStyle(0x1a1a35, 1).lineStyle(2, rarityColor, 0.6)
                        .fillRoundedRect(x - 70, y - 70, 140, 190, 10)
                        .strokeRoundedRect(x - 70, y - 70, 140, 190, 10);
                });
                hitArea.on('pointerdown', () => {
                    if (this.playerState.gold < relic.shopCost) return;
                    this.playerState.gold -= relic.shopCost;
                    if (!this.playerState.relics) this.playerState.relics = [];
                    this.playerState.relics.push(relic.id);
                    this.boughtRelics[relic.id] = true;
                    this._refresh();
                });
            }
        }

        // owned relics display
        if (ownedIds.length > 0) {
            const oy = startY + 310;
            this.add.text(W / 2, oy, '보유 유물:', {
                fontSize: '13px', fontFamily: 'monospace', color: '#666666'
            }).setOrigin(0.5);
            const relicStr = ownedIds.map(id => {
                const r = RELIC_DATA[id];
                return r ? `${r.icon} ${r.name}` : id;
            }).join('  ');
            const owned = this.add.text(W / 2, oy + 22, relicStr, {
                fontSize: '14px', fontFamily: 'monospace', color: '#cc88ff'
            }).setOrigin(0.5);
            this.contentContainer.add(owned);
        }
    }

    _buySymbol(symbolId, shopIdx) {
        const sym = SYMBOL_DATA[symbolId];
        if (!sym || this.playerState.gold < sym.cost) return;
        this.playerState.gold -= sym.cost;
        this.playerState.symbolPool.push(symbolId);
        this.playerState.codex[symbolId] = true;
        this.boughtSymbols[shopIdx] = true;
        this._refresh();
    }

    _buyItem(item) {
        if (this.boughtItems[item.id]) return;
        if (this.playerState.gold < item.cost) return;
        this.playerState.gold -= item.cost;
        this.boughtItems[item.id] = true;
        const e = item.effect;

        if (e.heal) {
            this.playerState.hp = Math.min(this.playerState.maxHp, this.playerState.hp + e.heal);
        }
        if (e.maxHpUp) {
            this.playerState.maxHp += e.maxHpUp;
            this.playerState.hp += e.maxHpUp;
        }
        if (e.randomSymbol) {
            const available = Object.keys(SYMBOL_DATA).filter(id => id !== 'dead');
            const pick = available[Phaser.Math.Between(0, available.length - 1)];
            this.playerState.symbolPool.push(pick);
            this.playerState.codex[pick] = true;
        }
        if (e.randomComboUpgrade) {
            const combo = COMBO_DATA[Phaser.Math.Between(0, COMBO_DATA.length - 1)];
            this.playerState.comboUpgrades[combo.id] = (this.playerState.comboUpgrades[combo.id] || 0) + 1;
        }
        if (e.removeSkull) {
            const idx = this.playerState.symbolPool.indexOf('dead');
            if (idx !== -1) this.playerState.symbolPool.splice(idx, 1);
        }
        this._refresh();
    }

    _buyComboUpgrade(comboId, cost) {
        if (this.boughtCombos[comboId]) return;
        if (this.playerState.gold < cost) return;
        this.playerState.gold -= cost;
        this.playerState.comboUpgrades[comboId] = (this.playerState.comboUpgrades[comboId] || 0) + 1;
        this.boughtCombos[comboId] = true;
        this._refresh();
    }

    _refresh() {
        this.goldText.setText(`🪙 ${this.playerState.gold}`);
        this._drawContent();
        this._drawPlayerInfo();
    }

    _drawPlayerInfo() {
        if (this.infoContainer) this.infoContainer.destroy();
        this.infoContainer = this.add.container(0, 0);

        const W = 1280, y = 620;
        const bg = this.add.graphics();
        bg.fillStyle(0x111128, 1);
        bg.fillRect(0, y - 5, W, 110);
        this.infoContainer.add(bg);

        const hpRatio = this.playerState.hp / this.playerState.maxHp;
        const hpColor = hpRatio > 0.5 ? '#44cc44' : hpRatio > 0.25 ? '#cccc44' : '#cc4444';
        const hpText = this.add.text(20, y + 5, `❤️ ${this.playerState.hp}/${this.playerState.maxHp}`, {
            fontSize: '14px', fontFamily: 'monospace', color: hpColor
        });
        this.infoContainer.add(hpText);

        const goldText = this.add.text(200, y + 5, `🪙 ${this.playerState.gold}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffcc00'
        });
        this.infoContainer.add(goldText);

        const deckLabel = this.add.text(20, y + 30, `대본 (${this.playerState.symbolPool.length}장):`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#888888'
        });
        this.infoContainer.add(deckLabel);

        const icons = this.playerState.symbolPool.map(id => SYMBOL_DATA[id] ? SYMBOL_DATA[id].icon : '?').join(' ');
        const deckIcons = this.add.text(20, y + 50, icons, {
            fontSize: '16px', wordWrap: { width: W - 40 }
        });
        this.infoContainer.add(deckIcons);
    }

    _drawNextButton() {
        const W = 1280;

        if (this.round >= 10 && !this.map) {
            const winText = this.add.text(W / 2, 560, '🐉 승리!', {
                fontSize: '22px', fontFamily: 'monospace', color: '#ffcc00', fontStyle: 'bold'
            }).setOrigin(0.5);
            const retryBtn = this.add.text(W / 2, 595, '[ 처음부터 ]', {
                fontSize: '20px', fontFamily: 'monospace', color: '#ffcc00',
                backgroundColor: '#2a2a1a', padding: { x: 20, y: 8 }
            }).setOrigin(0.5).setInteractive({ useHandCursor: true });
            retryBtn.on('pointerdown', () => this.scene.start('TitleScene'));
            return;
        }

        const nextLabel = this.map ? '맵으로 ▶' : '다음 라운드 ▶';
        const nextBtn = this.add.text(W - 30, 580, nextLabel, {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffcc00',
            fontStyle: 'bold', backgroundColor: '#2a2a1a',
            padding: { x: 20, y: 8 }
        }).setOrigin(1, 0.5).setInteractive({ useHandCursor: true });
        nextBtn.on('pointerover', () => nextBtn.setColor('#ffffff'));
        nextBtn.on('pointerout', () => nextBtn.setColor('#ffcc00'));
        nextBtn.on('pointerdown', () => {
            if (this.map) {
                this.scene.start('MapScene', {
                    act: this.act,
                    map: this.map,
                    playerState: this.playerState
                });
            } else {
                this.scene.start('BattleScene', {
                    round: this.round + 1,
                    playerState: this.playerState
                });
            }
        });
    }

    _generateShopSymbols() {
        const available = Object.keys(SYMBOL_DATA).filter(id => id !== 'dead');
        return Phaser.Utils.Array.Shuffle(available.slice()).slice(0, 5);
    }

    _generateShopItems() {
        return Object.values(SHOP_ITEMS);
    }

    _generateShopCombos() {
        return Phaser.Utils.Array.Shuffle(COMBO_DATA.slice()).slice(0, 4);
    }
}
