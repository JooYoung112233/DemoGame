class ShopScene extends Phaser.Scene {
    constructor() { super('ShopScene'); }

    create() {
        this.gs = window.gameState;
        this.displayItems = [];
        this.salesLog = [];
        this.customerIndex = 0;
        this.totalCustomers = this.gs.getCustomerCount();
        this.phase = 'prep'; // prep → selling → done
        this.haggleCount = 0;
        this.currentCustomer = null;
        this.currentSlot = null;
        this.pendingAction = null;

        this._drawBg();
        this._drawInfoBar();
        this._drawCraftPanel();
        this._drawShopDisplay();
        this._drawInventory();
        this._drawCustomerArea();
        this._drawActions();
    }

    _drawBg() {
        this.add.rectangle(640, 360, 1280, 720, 0x0e0e1a);
        for (let i = 0; i < 20; i++) {
            const s = this.add.circle(Phaser.Math.Between(0, 1280), Phaser.Math.Between(0, 150),
                Phaser.Math.Between(1, 2), 0xffffff, Phaser.Math.FloatBetween(0.05, 0.3));
            this.tweens.add({ targets: s, alpha: 0.05, duration: Phaser.Math.Between(2000, 5000), yoyo: true, repeat: -1 });
        }
    }

    _drawInfoBar() {
        this.add.rectangle(640, 20, 1280, 40, 0x151520);
        this.goldLabel = this.add.text(15, 8, '', { fontSize: '14px', fontFamily: 'monospace', color: '#ffcc44' });
        this.repLabel = this.add.text(150, 8, '', { fontSize: '14px', fontFamily: 'monospace', color: '#44ccff' });
        this.phaseLabel = this.add.text(320, 8, '', { fontSize: '14px', fontFamily: 'monospace', color: '#aaa' });
        this.customerLabel = this.add.text(550, 8, '', { fontSize: '14px', fontFamily: 'monospace', color: '#888' });
        const cat = CATEGORIES[this.gs.demandCategory];
        this.add.text(750, 8, `📈 수요: ${cat ? cat.name : ''}`, { fontSize: '13px', fontFamily: 'monospace', color: '#88aa88' });
        this._updateInfo();
    }

    _updateInfo() {
        this.goldLabel.setText(`💰 ${this.gs.gold}G`);
        this.repLabel.setText(`⭐ 평판 ${this.gs.reputation}`);
        this.phaseLabel.setText(this.phase === 'prep' ? '🔨 준비 중' : this.phase === 'selling' ? '🏪 영업 중' : '🌙 마감');
        this.customerLabel.setText(this.phase === 'selling' ? `👥 ${this.customerIndex}/${this.totalCustomers}` : '');
    }

    // === 제작 패널 (좌상단) ===
    _drawCraftPanel() {
        this.add.rectangle(160, 195, 290, 310, 0x12121e, 0.95).setStrokeStyle(1, 0x222240);
        this.add.text(160, 50, '🔨 제작', { fontSize: '16px', fontFamily: 'monospace', color: '#ff8844' }).setOrigin(0.5);

        this.craftContainer = this.add.container(0, 0);
        this._refreshCraft();
    }

    _refreshCraft() {
        this.craftContainer.removeAll(true);
        const recipes = Object.entries(RECIPE_DATA).filter(([, r]) => this.gs.day >= r.unlockDay);

        if (recipes.length === 0) {
            const t = this.add.text(160, 180, '해금된 레시피 없음', { fontSize: '12px', fontFamily: 'monospace', color: '#555' }).setOrigin(0.5);
            this.craftContainer.add(t);
            return;
        }

        let y = 72;
        recipes.forEach(([id, recipe]) => {
            const resultData = ITEM_DATA[recipe.result];
            const canCraft = recipe.ingredients.every(ing => this.gs.getItemCount(ing.id) >= ing.count);

            const bg = this.add.rectangle(160, y + 18, 270, 36, canCraft ? 0x1a2a1a : 0x1a1a22, 0.9);
            bg.setStrokeStyle(1, canCraft ? 0x2a4a2a : 0x222235);
            this.craftContainer.add(bg);

            const ingStr = recipe.ingredients.map(ing => {
                const d = ITEM_DATA[ing.id];
                const have = this.gs.getItemCount(ing.id);
                return `${d.icon}${have}/${ing.count}`;
            }).join(' ');

            const label = this.add.text(35, y + 6, `${resultData.icon} ${resultData.name}`, {
                fontSize: '12px', fontFamily: 'monospace', color: canCraft ? '#ddd' : '#666',
            });
            const ings = this.add.text(35, y + 22, ingStr, {
                fontSize: '10px', fontFamily: 'monospace', color: '#888',
            });
            this.craftContainer.add([label, ings]);

            if (canCraft && this.phase === 'prep') {
                bg.setInteractive({ useHandCursor: true });
                bg.on('pointerover', () => bg.setFillStyle(0x2a3a2a));
                bg.on('pointerout', () => bg.setFillStyle(0x1a2a1a));
                bg.on('pointerdown', () => {
                    recipe.ingredients.forEach(ing => this.gs.removeItem(ing.id, ing.count));
                    this.gs.addItems([{ id: recipe.result, qty: 1 }]);
                    Toast.show(this, `${resultData.icon} ${resultData.name} 제작!`, { color: '#ff8844', y: y + 18 });
                    this._refreshCraft();
                    this._refreshInventory();
                });
            }

            y += 42;
        });
    }

    // === 진열대 (중앙 상단) ===
    _drawShopDisplay() {
        this.add.rectangle(540, 195, 380, 310, 0x12121e, 0.95).setStrokeStyle(1, 0x222240);
        this.add.text(540, 50, '🏪 진열대 (인벤에서 클릭하여 진열)', { fontSize: '13px', fontFamily: 'monospace', color: '#ffeebb' }).setOrigin(0.5);

        this.displaySlots = [];
        for (let i = 0; i < this.gs.shopSlots; i++) {
            const col = i % 3, row = Math.floor(i / 3);
            const sx = 385 + col * 115, sy = 75 + row * 110;
            const slot = this.add.rectangle(sx + 42, sy + 38, 100, 85, 0x181830);
            slot.setStrokeStyle(1, 0x2a2a50);
            const icon = this.add.text(sx + 42, sy + 18, '', { fontSize: '26px' }).setOrigin(0.5);
            const name = this.add.text(sx + 42, sy + 45, '', { fontSize: '10px', fontFamily: 'monospace', color: '#ccc' }).setOrigin(0.5);
            const price = this.add.text(sx + 42, sy + 60, '', { fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44' }).setOrigin(0.5);
            this.displaySlots.push({ bg: slot, icon, name, price, item: null, setPrice: 0 });
        }
    }

    // === 인벤토리 (우측) ===
    _drawInventory() {
        this.add.rectangle(920, 195, 340, 310, 0x12121e, 0.95).setStrokeStyle(1, 0x222240);
        this.add.text(920, 50, '🎒 인벤토리', { fontSize: '14px', fontFamily: 'monospace', color: '#44ff88' }).setOrigin(0.5);
        this.invContainer = this.add.container(0, 0);
        this._refreshInventory();
    }

    _refreshInventory() {
        this.invContainer.removeAll(true);
        const items = this.gs.inventory.filter(i => i.qty > 0);
        items.forEach((item, idx) => {
            const data = ITEM_DATA[item.id];
            if (!data) return;
            const col = idx % 3, row = Math.floor(idx / 3);
            const x = 770 + col * 110, y = 72 + row * 50;
            const bg = this.add.rectangle(x + 40, y + 16, 100, 38, 0x181830);
            bg.setStrokeStyle(1, 0x222240);
            bg.setInteractive({ useHandCursor: true });
            const t = this.add.text(x + 5, y + 4, `${data.icon}${data.name}`, { fontSize: '10px', fontFamily: 'monospace', color: '#ccc' });
            const q = this.add.text(x + 5, y + 18, `x${item.qty} (${data.basePrice}G)`, { fontSize: '9px', fontFamily: 'monospace', color: '#888' });
            bg.on('pointerover', () => bg.setFillStyle(0x222240));
            bg.on('pointerout', () => bg.setFillStyle(0x181830));
            bg.on('pointerdown', () => {
                if (this.phase !== 'prep') return;
                this._addToDisplay(item.id, data);
            });
            this.invContainer.add([bg, t, q]);
        });
    }

    _addToDisplay(itemId, data) {
        const slot = this.displaySlots.find(s => !s.item);
        if (!slot) { Toast.show(this, '진열대 가득!', { color: '#ff4444' }); return; }
        if (!this.gs.removeItem(itemId)) return;
        const dm = this.gs.getDemandMultiplier(data.category);
        slot.item = itemId;
        slot.setPrice = Math.round(data.basePrice * dm);
        slot.icon.setText(data.icon);
        slot.name.setText(data.name);
        slot.price.setText(`${slot.setPrice}G`);
        slot.bg.setFillStyle(0x1a1a35);

        slot.bg.setInteractive({ useHandCursor: true });
        slot.bg.off('pointerdown');
        slot.bg.on('pointerdown', () => {
            if (this.phase !== 'prep') return;
            this._cyclePrice(slot, data);
        });
        this._refreshInventory();
    }

    _cyclePrice(slot, data) {
        const mults = [0.7, 0.85, 1.0, 1.15, 1.3, 1.5];
        const prices = mults.map(m => Math.round(data.basePrice * m));
        const ci = prices.indexOf(slot.setPrice);
        slot.setPrice = prices[(ci + 1) % prices.length];
        slot.price.setText(`${slot.setPrice}G`);
        const ratio = slot.setPrice / data.basePrice;
        slot.price.setColor(ratio > 1.3 ? '#ff4444' : ratio > 1.1 ? '#ffcc44' : '#44ff88');
    }

    // === 손님 영역 (하단) ===
    _drawCustomerArea() {
        this.add.rectangle(440, 530, 850, 190, 0x10101e, 0.95).setStrokeStyle(1, 0x222240);
        this.custIcon = this.add.text(60, 460, '', { fontSize: '44px' }).setOrigin(0.5);
        this.custName = this.add.text(60, 500, '', { fontSize: '13px', fontFamily: 'monospace', color: '#ddd' }).setOrigin(0.5);
        this.custBudget = this.add.text(60, 518, '', { fontSize: '11px', fontFamily: 'monospace', color: '#888' }).setOrigin(0.5);
        this.dialogText = this.add.text(440, 470, '', {
            fontSize: '15px', fontFamily: 'monospace', color: '#eeddcc', wordWrap: { width: 500 }, align: 'center',
        }).setOrigin(0.5);
        this.custWant = this.add.text(440, 510, '', { fontSize: '13px', fontFamily: 'monospace', color: '#aaa' }).setOrigin(0.5);

        // 영업 시작 버튼
        this.startBtn = new UIButton(this, 440, 570, '🏪 영업 시작!', {
            width: 220, height: 44, bg: 0x2a2210, hoverBg: 0x3a3320,
            textColor: '#ffcc88', fontSize: '16px',
            onClick: () => this._startSelling(),
        });

        // 판매/거절 버튼
        this.actionContainer = this.add.container(0, 0).setVisible(false);
        this.acceptBtn = new UIButton(this, 340, 570, '✅ 판매', {
            width: 130, height: 40, bg: 0x224422, hoverBg: 0x336633,
            textColor: '#44ff88', fontSize: '14px', onClick: () => this._accept(),
        });
        this.rejectBtn = new UIButton(this, 540, 570, '❌ 거절', {
            width: 130, height: 40, bg: 0x442222, hoverBg: 0x663333,
            textColor: '#ff4444', fontSize: '14px', onClick: () => this._reject(),
        });
        this.actionContainer.add([this.acceptBtn.container, this.rejectBtn.container]);

        // 밤 의뢰 버튼
        this.nightBtn = new UIButton(this, 1160, 680, '🌙 의뢰서 작성 →', {
            width: 200, height: 40, bg: 0x1a1a2a, hoverBg: 0x2a2a3a,
            textColor: '#aaaaff', fontSize: '13px',
            onClick: () => {
                this._returnUnsold();
                this.scene.start('CommissionScene');
            },
        });
    }

    _drawActions() {}

    _returnUnsold() {
        this.displaySlots.forEach(s => {
            if (s.item) { this.gs.addItems([{ id: s.item, qty: 1 }]); s.item = null; }
        });
    }

    _startSelling() {
        const displayed = this.displaySlots.filter(s => s.item);
        if (displayed.length === 0) { Toast.show(this, '진열된 아이템이 없습니다!', { color: '#ff4444' }); return; }
        this.phase = 'selling';
        this.startBtn.setVisible(false);
        this._updateInfo();
        this._nextCustomer();
    }

    _nextCustomer() {
        if (this.customerIndex >= this.totalCustomers || this.displaySlots.every(s => !s.item)) {
            this._endSelling();
            return;
        }
        this.customerIndex++;
        this.haggleCount = 0;
        this._updateInfo();

        this.currentCustomer = ShopSystem.generateCustomer(this.gs);
        const c = this.currentCustomer;
        this.custIcon.setText(c.icon);
        this.custName.setText(c.name);
        this.custBudget.setText(`예산: ~${c.budget}G`);

        const slot = this._pickWanted(c);
        if (!slot) {
            this.dialogText.setText('"흠... 원하는 게 없네요."');
            this.custWant.setText('');
            this.actionContainer.setVisible(false);
            this.time.delayedCall(1200, () => this._nextCustomer());
            return;
        }

        this.currentSlot = slot;
        const data = ITEM_DATA[slot.item];
        const result = ShopSystem.evaluateItem(c, slot.item, slot.setPrice, this.gs);
        this.custWant.setText(`${data.icon} ${data.name} — ${slot.setPrice}G`);

        if (result.action === 'buy') {
            this.dialogText.setText(`"${data.name}! ${slot.setPrice}G요? 좋아요, 살게요!"`);
            this.actionContainer.setVisible(true);
            this.acceptBtn.label.setText(`✅ ${slot.setPrice}G`);
            this.pendingAction = { action: 'buy', price: slot.setPrice };
        } else if (result.action === 'haggle') {
            this.dialogText.setText(`"${slot.setPrice}G는 좀... ${result.offer}G 어때요?"`);
            this.actionContainer.setVisible(true);
            this.acceptBtn.label.setText(`✅ ${result.offer}G`);
            this.pendingAction = { action: 'haggle', price: result.offer };
        } else {
            this.dialogText.setText(`"${result.reason}..."`);
            this.actionContainer.setVisible(false);
            ShopSystem.failedSale(this.gs);
            this._updateInfo();
            this.time.delayedCall(1000, () => this._nextCustomer());
        }
    }

    _pickWanted(customer) {
        const displayed = this.displaySlots.filter(s => s.item);
        if (!displayed.length) return null;
        if (customer.preferredCategories) {
            const pref = displayed.filter(s => customer.preferredCategories.includes(ITEM_DATA[s.item]?.category));
            if (pref.length) return pref[Phaser.Math.Between(0, pref.length - 1)];
            if (Math.random() < 0.4) return null;
        }
        return displayed[Phaser.Math.Between(0, displayed.length - 1)];
    }

    _accept() {
        if (!this.pendingAction || !this.currentSlot) return;
        this.actionContainer.setVisible(false);
        const price = this.pendingAction.price;
        ShopSystem.completeSale(this.gs, this.currentSlot.item, price);
        const data = ITEM_DATA[this.currentSlot.item];
        this.salesLog.push({ item: data.name, price });
        Toast.show(this, `+${price}G!`, { color: '#ffcc44', duration: 1200 });

        this.currentSlot.item = null;
        this.currentSlot.icon.setText('');
        this.currentSlot.name.setText('');
        this.currentSlot.price.setText('');
        this.currentSlot.bg.setFillStyle(0x181830);
        this._updateInfo();
        this.time.delayedCall(700, () => this._nextCustomer());
    }

    _reject() {
        this.actionContainer.setVisible(false);
        if (this.pendingAction?.action === 'haggle' && this.haggleCount < 1) {
            this.haggleCount++;
            const newOffer = Math.floor(this.pendingAction.price * 1.1);
            this.dialogText.setText(`"그럼... ${newOffer}G. 마지막이에요!"`);
            this.actionContainer.setVisible(true);
            this.acceptBtn.label.setText(`✅ ${newOffer}G`);
            this.pendingAction = { action: 'haggle', price: newOffer };
            return;
        }
        this.dialogText.setText('"아쉽네요..."');
        ShopSystem.failedSale(this.gs);
        this._updateInfo();
        this.time.delayedCall(800, () => this._nextCustomer());
    }

    _endSelling() {
        this.phase = 'done';
        this._returnUnsold();
        const totalSales = this.salesLog.reduce((s, l) => s + l.price, 0);
        this._updateInfo();

        const ov = this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.7).setDepth(10);
        const pn = this.add.rectangle(640, 340, 400, 300, 0x12121e, 0.98).setDepth(11).setStrokeStyle(2, 0x3a3a60);

        this.add.text(640, 210, '🌙 영업 종료', { fontSize: '22px', fontFamily: 'monospace', color: '#ffeebb' }).setOrigin(0.5).setDepth(11);
        let y = 250;
        if (!this.salesLog.length) {
            this.add.text(640, y, '오늘은 판매가 없었습니다.', { fontSize: '13px', fontFamily: 'monospace', color: '#666' }).setOrigin(0.5).setDepth(11);
            y += 25;
        } else {
            this.salesLog.forEach(l => {
                this.add.text(640, y, `${l.item} — ${l.price}G`, { fontSize: '13px', fontFamily: 'monospace', color: '#ddd' }).setOrigin(0.5).setDepth(11);
                y += 22;
            });
        }
        this.add.text(640, y + 10, `총 매출: ${totalSales}G`, { fontSize: '17px', fontFamily: 'monospace', color: '#ffcc44' }).setOrigin(0.5).setDepth(11);

        new UIButton(this, 640, y + 55, '🌙 의뢰서 작성 →', {
            width: 220, height: 44, bg: 0x1a1a2a, hoverBg: 0x2a2a3a,
            textColor: '#aaaaff', fontSize: '15px',
            onClick: () => this.scene.start('CommissionScene'),
        }).container.setDepth(11);
    }
}
