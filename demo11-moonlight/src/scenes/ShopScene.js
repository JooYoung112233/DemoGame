class ShopScene extends Phaser.Scene {
    constructor() { super('ShopScene'); }

    create() {
        this.gs = window.gameState;
        this.customers = [];
        this.currentCustomer = null;
        this.displayItems = [];
        this.salesLog = [];
        this.customerIndex = 0;
        this.totalCustomers = this.gs.getCustomerCount();
        this.phase = 'setup'; // setup → selling → done
        this.haggleCount = 0;

        this._buildBackground();
        this._buildInfoBar();
        this._buildShopDisplay();
        this._buildInventoryPanel();
        this._buildCustomerArea();
        this._buildActionArea();
        this._buildStartButton();
    }

    _buildBackground() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a2a, 0.5);

        for (let i = 0; i < 30; i++) {
            const star = this.add.circle(
                Phaser.Math.Between(0, 1280),
                Phaser.Math.Between(0, 200),
                Phaser.Math.Between(1, 2),
                0xffffff, Phaser.Math.FloatBetween(0.1, 0.5)
            );
            this.tweens.add({
                targets: star, alpha: 0.1, duration: Phaser.Math.Between(1000, 3000),
                yoyo: true, repeat: -1,
            });
        }

        this.add.circle(1150, 60, 30, 0xffeebb, 0.3);
        this.add.circle(1150, 60, 22, 0xffeebb, 0.5);
    }

    _buildInfoBar() {
        this.add.rectangle(640, 22, 1280, 44, 0x151530);
        this.dayLabel = this.add.text(15, 10, `📅 Day ${this.gs.day}`, {
            fontSize: '15px', fontFamily: 'monospace', color: '#ffeebb'
        });
        this.goldLabel = this.add.text(150, 10, `💰 ${this.gs.gold}G`, {
            fontSize: '15px', fontFamily: 'monospace', color: '#ffcc44'
        });
        this.repLabel = this.add.text(300, 10, `⭐ 평판 ${this.gs.reputation}`, {
            fontSize: '15px', fontFamily: 'monospace', color: '#44ccff'
        });
        this.customerCountLabel = this.add.text(470, 10, '', {
            fontSize: '15px', fontFamily: 'monospace', color: '#aaa'
        });
        const cat = CATEGORIES[this.gs.demandCategory];
        this.add.text(700, 10, `📈 수요: ${cat ? cat.name : ''}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#88aa88'
        });
    }

    _buildShopDisplay() {
        this.add.text(160, 55, '🏪 진열대', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffeebb'
        }).setOrigin(0.5);

        this.displaySlots = [];
        for (let i = 0; i < this.gs.shopSlots; i++) {
            const col = i % 3;
            const row = Math.floor(i / 3);
            const sx = 60 + col * 140;
            const sy = 90 + row * 120;

            const slot = this.add.rectangle(sx + 50, sy + 40, 120, 90, 0x1a1a35);
            slot.setStrokeStyle(1, 0x3a3a60);
            slot.setInteractive({ useHandCursor: true });

            const iconText = this.add.text(sx + 50, sy + 25, '', {
                fontSize: '28px'
            }).setOrigin(0.5);
            const nameText = this.add.text(sx + 50, sy + 52, '', {
                fontSize: '11px', fontFamily: 'monospace', color: '#ccc'
            }).setOrigin(0.5);
            const priceText = this.add.text(sx + 50, sy + 70, '', {
                fontSize: '13px', fontFamily: 'monospace', color: '#ffcc44'
            }).setOrigin(0.5);

            this.displaySlots.push({ bg: slot, icon: iconText, name: nameText, price: priceText, item: null, setPrice: 0 });
        }
    }

    _buildInventoryPanel() {
        this.add.rectangle(880, 240, 380, 360, 0x111125, 0.9);
        this.add.rectangle(880, 240, 380, 360).setStrokeStyle(1, 0x2a2a50);
        this.add.text(880, 68, '🎒 인벤토리 (클릭하여 진열)', {
            fontSize: '14px', fontFamily: 'monospace', color: '#888'
        }).setOrigin(0.5);

        this.invContainer = this.add.container(0, 0);
        this._refreshInventory();
    }

    _refreshInventory() {
        this.invContainer.removeAll(true);
        const items = this.gs.inventory.filter(i => i.qty > 0);

        items.forEach((item, idx) => {
            const data = ITEM_DATA[item.id];
            if (!data) return;
            const col = idx % 3;
            const row = Math.floor(idx / 3);
            const x = 720 + col * 120;
            const y = 90 + row * 55;

            const bg = this.add.rectangle(x + 45, y + 18, 110, 45, 0x1a1a35);
            bg.setStrokeStyle(1, 0x2a2a50);
            bg.setInteractive({ useHandCursor: true });

            const icon = this.add.text(x + 8, y + 8, `${data.icon}`, { fontSize: '18px' });
            const txt = this.add.text(x + 30, y + 5, data.name, {
                fontSize: '11px', fontFamily: 'monospace', color: '#ccc'
            });
            const qty = this.add.text(x + 30, y + 22, `x${item.qty}  (${data.basePrice}G)`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#888'
            });

            bg.on('pointerover', () => bg.setFillStyle(0x2a2a45));
            bg.on('pointerout', () => bg.setFillStyle(0x1a1a35));
            bg.on('pointerdown', () => {
                if (this.phase !== 'setup') return;
                this._addToDisplay(item.id, data);
            });

            this.invContainer.add([bg, icon, txt, qty]);
        });
    }

    _addToDisplay(itemId, data) {
        const emptySlot = this.displaySlots.find(s => !s.item);
        if (!emptySlot) {
            Toast.show(this, '진열대가 가득 찼습니다!', { color: '#ff4444' });
            return;
        }
        if (!this.gs.removeItem(itemId)) {
            Toast.show(this, '아이템이 부족합니다!', { color: '#ff4444' });
            return;
        }

        const demandMult = this.gs.getDemandMultiplier(data.category);
        const suggestedPrice = Math.round(data.basePrice * demandMult);

        emptySlot.item = itemId;
        emptySlot.setPrice = suggestedPrice;
        emptySlot.icon.setText(data.icon);
        emptySlot.name.setText(data.name);
        emptySlot.price.setText(`${suggestedPrice}G`);
        emptySlot.bg.setFillStyle(0x222245);

        emptySlot.bg.off('pointerdown');
        emptySlot.bg.on('pointerdown', () => {
            if (this.phase !== 'setup') return;
            this._adjustPrice(emptySlot, data);
        });

        this._refreshInventory();
        Toast.show(this, `${data.name} 진열! (${suggestedPrice}G)`, { color: '#44ff88', duration: 1000 });
    }

    _adjustPrice(slot, data) {
        const prices = [
            Math.round(data.basePrice * 0.7),
            Math.round(data.basePrice * 0.85),
            data.basePrice,
            Math.round(data.basePrice * 1.15),
            Math.round(data.basePrice * 1.3),
            Math.round(data.basePrice * 1.5),
        ];

        const currentIdx = prices.indexOf(slot.setPrice);
        const nextIdx = (currentIdx + 1) % prices.length;
        slot.setPrice = prices[nextIdx];
        slot.price.setText(`${slot.setPrice}G`);

        const ratio = slot.setPrice / data.basePrice;
        if (ratio > 1.3) slot.price.setColor('#ff4444');
        else if (ratio > 1.1) slot.price.setColor('#ffcc44');
        else slot.price.setColor('#44ff88');
    }

    _buildCustomerArea() {
        this.add.rectangle(400, 540, 760, 200, 0x111125, 0.9);
        this.add.rectangle(400, 540, 760, 200).setStrokeStyle(1, 0x2a2a50);

        this.customerIcon = this.add.text(80, 470, '', { fontSize: '48px' }).setOrigin(0.5);
        this.customerName = this.add.text(80, 510, '', {
            fontSize: '14px', fontFamily: 'monospace', color: '#fff'
        }).setOrigin(0.5);
        this.customerBudget = this.add.text(80, 530, '', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aaa'
        }).setOrigin(0.5);

        this.dialogText = this.add.text(400, 480, '', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ddd',
            wordWrap: { width: 500 }, align: 'center',
        }).setOrigin(0.5);

        this.customerWantIcon = this.add.text(400, 520, '', {
            fontSize: '14px', fontFamily: 'monospace', color: '#aaa',
        }).setOrigin(0.5);
    }

    _buildActionArea() {
        this.actionContainer = this.add.container(0, 0).setVisible(false);

        this.acceptBtn = new UIButton(this, 300, 600, '✅ 판매', {
            width: 140, height: 44, bg: 0x224422, hoverBg: 0x336633,
            textColor: '#44ff88', fontSize: '15px',
            onClick: () => this._handleAccept(),
        });

        this.rejectBtn = new UIButton(this, 500, 600, '❌ 거절', {
            width: 140, height: 44, bg: 0x442222, hoverBg: 0x663333,
            textColor: '#ff4444', fontSize: '15px',
            onClick: () => this._handleReject(),
        });

        this.actionContainer.add([this.acceptBtn.container, this.rejectBtn.container]);
    }

    _buildStartButton() {
        this.startBtn = new UIButton(this, 400, 600, '🏪 영업 시작!', {
            width: 260, height: 50, bg: 0x2a2a1a, hoverBg: 0x3a3a2a,
            textColor: '#ffcc44', fontSize: '18px',
            onClick: () => this._startSelling(),
        });

        this.backBtn = new UIButton(this, 100, 680, '← 돌아가기', {
            width: 160, height: 40, bg: 0x333333, hoverBg: 0x555555,
            textColor: '#aaa', fontSize: '14px',
            onClick: () => {
                this._returnUnsoldItems();
                this.scene.start('HubScene');
            },
        });
    }

    _returnUnsoldItems() {
        this.displaySlots.forEach(slot => {
            if (slot.item) {
                this.gs.addItems([{ id: slot.item, qty: 1 }]);
                slot.item = null;
            }
        });
    }

    _startSelling() {
        const displayed = this.displaySlots.filter(s => s.item);
        if (displayed.length === 0) {
            Toast.show(this, '진열된 아이템이 없습니다!', { color: '#ff4444' });
            return;
        }

        this.phase = 'selling';
        this.startBtn.setVisible(false);
        this.customerIndex = 0;
        this._nextCustomer();
    }

    _nextCustomer() {
        if (this.customerIndex >= this.totalCustomers) {
            this._endDay();
            return;
        }

        const displayed = this.displaySlots.filter(s => s.item);
        if (displayed.length === 0) {
            this._endDay();
            return;
        }

        this.customerIndex++;
        this.haggleCount = 0;
        this.customerCountLabel.setText(`👥 ${this.customerIndex}/${this.totalCustomers}`);

        this.currentCustomer = ShopSystem.generateCustomer(this.gs);
        const c = this.currentCustomer;

        this.customerIcon.setText(c.icon);
        this.customerName.setText(c.name);
        this.customerBudget.setText(`예산: ~${c.budget}G`);

        const wantedSlot = this._pickWantedItem(c);
        if (!wantedSlot) {
            this.dialogText.setText('"원하는 물건이 없네요..."');
            this.customerWantIcon.setText('');
            this.actionContainer.setVisible(false);
            this.time.delayedCall(1200, () => this._nextCustomer());
            return;
        }

        this.currentSlot = wantedSlot;
        const data = ITEM_DATA[wantedSlot.item];
        const result = ShopSystem.evaluateItem(c, wantedSlot.item, wantedSlot.setPrice, this.gs);

        this.customerWantIcon.setText(`${data.icon} ${data.name} — ${wantedSlot.setPrice}G`);

        if (result.action === 'buy') {
            this.dialogText.setText(`"${data.name}! 좋아요, ${wantedSlot.setPrice}G에 살게요!"`);
            this.actionContainer.setVisible(true);
            this.acceptBtn.label.setText(`✅ ${wantedSlot.setPrice}G 판매`);
            this.pendingAction = { action: 'buy', price: wantedSlot.setPrice };
        } else if (result.action === 'haggle') {
            this.dialogText.setText(`"${data.name}... ${wantedSlot.setPrice}G는 좀 비싸네요. ${result.offer}G는 어때요?"`);
            this.actionContainer.setVisible(true);
            this.acceptBtn.label.setText(`✅ ${result.offer}G 수락`);
            this.pendingAction = { action: 'haggle', price: result.offer };
        } else {
            this.dialogText.setText(`"${result.reason}... 다음에 올게요."`);
            this.actionContainer.setVisible(false);
            ShopSystem.failedSale(this.gs);
            this._updateLabels();
            this.time.delayedCall(1200, () => this._nextCustomer());
        }
    }

    _pickWantedItem(customer) {
        const displayed = this.displaySlots.filter(s => s.item);
        if (displayed.length === 0) return null;

        if (customer.preferredCategories) {
            const preferred = displayed.filter(s => {
                const data = ITEM_DATA[s.item];
                return data && customer.preferredCategories.includes(data.category);
            });
            if (preferred.length > 0) {
                return preferred[Phaser.Math.Between(0, preferred.length - 1)];
            }
        }

        return displayed[Phaser.Math.Between(0, displayed.length - 1)];
    }

    _handleAccept() {
        if (!this.pendingAction || !this.currentSlot) return;
        this.actionContainer.setVisible(false);

        const price = this.pendingAction.price;
        ShopSystem.completeSale(this.gs, this.currentSlot.item, price);

        const data = ITEM_DATA[this.currentSlot.item];
        this.salesLog.push({ item: data.name, price });
        Toast.show(this, `+${price}G! ${data.name} 판매 완료`, { color: '#ffcc44', duration: 1500 });

        this.currentSlot.item = null;
        this.currentSlot.icon.setText('');
        this.currentSlot.name.setText('');
        this.currentSlot.price.setText('');
        this.currentSlot.bg.setFillStyle(0x1a1a35);

        this._updateLabels();
        this.time.delayedCall(800, () => this._nextCustomer());
    }

    _handleReject() {
        this.actionContainer.setVisible(false);

        if (this.pendingAction.action === 'haggle' && this.haggleCount < 1) {
            this.haggleCount++;
            const newOffer = Math.floor(this.pendingAction.price * 1.1);
            this.dialogText.setText(`"그럼... ${newOffer}G까지는 가능해요. 마지막이에요!"`);
            this.actionContainer.setVisible(true);
            this.acceptBtn.label.setText(`✅ ${newOffer}G 수락`);
            this.pendingAction = { action: 'haggle', price: newOffer };
            return;
        }

        this.dialogText.setText('"아쉽네요, 다음에 올게요."');
        ShopSystem.failedSale(this.gs);
        this._updateLabels();
        this.time.delayedCall(1000, () => this._nextCustomer());
    }

    _updateLabels() {
        this.goldLabel.setText(`💰 ${this.gs.gold}G`);
        this.repLabel.setText(`⭐ 평판 ${this.gs.reputation}`);
    }

    _endDay() {
        this.phase = 'done';
        this._returnUnsoldItems();

        const totalSales = this.salesLog.reduce((s, l) => s + l.price, 0);
        this.gs.advanceDay();

        this.dialogText.setText('');
        this.customerWantIcon.setText('');
        this.customerIcon.setText('🌅');
        this.customerName.setText('영업 종료');
        this.customerBudget.setText('');
        this.actionContainer.setVisible(false);

        const overlay = this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.7).setDepth(10);
        const panel = this.add.rectangle(640, 340, 420, 320, 0x151530, 0.98).setDepth(11);
        panel.setStrokeStyle(2, 0x4466aa);

        const title = this.add.text(640, 210, '🌙 오늘의 정산', {
            fontSize: '24px', fontFamily: 'monospace', color: '#ffeebb',
        }).setOrigin(0.5).setDepth(11);

        let y = 260;
        if (this.salesLog.length === 0) {
            this.add.text(640, y, '오늘은 판매가 없었습니다...', {
                fontSize: '14px', fontFamily: 'monospace', color: '#666',
            }).setOrigin(0.5).setDepth(11);
            y += 30;
        } else {
            this.salesLog.forEach(log => {
                this.add.text(640, y, `${log.item} — ${log.price}G`, {
                    fontSize: '14px', fontFamily: 'monospace', color: '#ddd',
                }).setOrigin(0.5).setDepth(11);
                y += 24;
            });
        }

        this.add.text(640, y + 15, `총 매출: ${totalSales}G`, {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffcc44',
        }).setOrigin(0.5).setDepth(11);

        this.add.text(640, y + 40, `보유 골드: ${this.gs.gold}G  |  평판: ${this.gs.reputation}`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#888',
        }).setOrigin(0.5).setDepth(11);

        const doneBtn = new UIButton(this, 640, y + 85, '다음 날로 →', {
            width: 200, height: 48, bg: 0x224422, hoverBg: 0x336633,
            textColor: '#44ff88', fontSize: '16px',
            onClick: () => this.scene.start('HubScene'),
        });
        doneBtn.container.setDepth(11);
    }
}
