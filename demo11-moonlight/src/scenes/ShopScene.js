class ShopScene extends Phaser.Scene {
    constructor() { super('ShopScene'); }

    create() {
        this.gs = window.gameState;
        this.displayItems = [];
        this.salesLog = [];
        this.customerIndex = 0;
        this.totalCustomers = this.gs.getCustomerCount();
        this.phase = 'prep';
        this.haggleCount = 0;
        this.currentCustomer = null;
        this.currentSlot = null;
        this.pendingAction = null;
        this.isCrafting = false;

        const MARGIN = 30;
        const GAP = 12;
        const TOTAL_W = 1280 - MARGIN * 2;
        const TOP_BAR_H = 40;
        const PANEL_TOP = TOP_BAR_H + 10;
        const PANEL_H = 310;
        const CUST_TOP = PANEL_TOP + PANEL_H + 10;
        const CUST_H = 200;

        const col3W = (TOTAL_W - GAP * 2) / 3;
        this.panelW = col3W;
        this.col1X = MARGIN + col3W / 2;
        this.col2X = MARGIN + col3W + GAP + col3W / 2;
        this.col3X = MARGIN + (col3W + GAP) * 2 + col3W / 2;
        this.panelTop = PANEL_TOP;
        this.panelH = PANEL_H;
        this.custTop = CUST_TOP;
        this.custH = CUST_H;

        this._drawBg();
        this._drawInfoBar(TOP_BAR_H);
        this._drawCraftPanel();
        this._drawShopDisplay();
        this._drawInventory();
        this._drawCustomerArea();
    }

    _drawBg() {
        this.add.rectangle(640, 360, 1280, 720, 0x0e0e1a);
        for (let i = 0; i < 20; i++) {
            const s = this.add.circle(Phaser.Math.Between(0, 1280), Phaser.Math.Between(0, 150),
                Phaser.Math.Between(1, 2), 0xffffff, Phaser.Math.FloatBetween(0.05, 0.3));
            this.tweens.add({ targets: s, alpha: 0.05, duration: Phaser.Math.Between(2000, 5000), yoyo: true, repeat: -1 });
        }
    }

    _drawInfoBar(barH) {
        this.add.rectangle(640, barH / 2, 1280, barH, 0x151520);
        const infoY = barH / 2 - 8;
        this.goldLabel = this.add.text(40, infoY, '', { fontSize: '14px', fontFamily: 'monospace', color: '#ffcc44' });
        this.repLabel = this.add.text(200, infoY, '', { fontSize: '14px', fontFamily: 'monospace', color: '#44ccff' });
        this.phaseLabel = this.add.text(400, infoY, '', { fontSize: '14px', fontFamily: 'monospace', color: '#aaa' });
        this.customerLabel = this.add.text(600, infoY, '', { fontSize: '14px', fontFamily: 'monospace', color: '#888' });
        const cat = CATEGORIES[this.gs.demandCategory];
        this.add.text(800, infoY, `수요: ${cat ? cat.name : ''}`, { fontSize: '13px', fontFamily: 'monospace', color: '#88aa88' });
        this._updateInfo();
    }

    _updateInfo() {
        this.goldLabel.setText(`${this.gs.gold}G`);
        this.repLabel.setText(`평판 ${this.gs.reputation}`);
        this.phaseLabel.setText(this.phase === 'prep' ? '준비 중' : this.phase === 'selling' ? '영업 중' : '마감');
        this.customerLabel.setText(this.phase === 'selling' ? `${this.customerIndex}/${this.totalCustomers}` : '');
    }

    // === 제작 패널 ===
    _drawCraftPanel() {
        const px = this.col1X, py = this.panelTop + this.panelH / 2;
        this.add.rectangle(px, py, this.panelW, this.panelH, 0x12121e, 0.95).setStrokeStyle(1, 0x222240);
        this.add.text(px, this.panelTop + 18, '제작', { fontSize: '16px', fontFamily: 'monospace', color: '#ff8844' }).setOrigin(0.5);

        this.craftContainer = this.add.container(0, 0);
        this.craftOverlay = this.add.container(0, 0).setVisible(false).setDepth(5);
        this._refreshCraft();
    }

    _refreshCraft() {
        this.craftContainer.removeAll(true);
        const recipes = Object.entries(RECIPE_DATA).filter(([, r]) => this.gs.day >= r.unlockDay);
        const px = this.col1X;
        const leftEdge = px - this.panelW / 2 + 15;

        if (recipes.length === 0) {
            const t = this.add.text(px, this.panelTop + this.panelH / 2, '해금된 레시피 없음', {
                fontSize: '12px', fontFamily: 'monospace', color: '#555',
            }).setOrigin(0.5);
            this.craftContainer.add(t);
            return;
        }

        let y = this.panelTop + 38;
        const itemW = this.panelW - 20;
        recipes.forEach(([id, recipe]) => {
            const resultData = ITEM_DATA[recipe.result];
            const canCraft = recipe.ingredients.every(ing => this.gs.getItemCount(ing.id) >= ing.count);

            const bg = this.add.rectangle(px, y + 18, itemW, 38, canCraft ? 0x1a2a1a : 0x1a1a22, 0.9);
            bg.setStrokeStyle(1, canCraft ? 0x2a4a2a : 0x222235);
            this.craftContainer.add(bg);

            const ingStr = recipe.ingredients.map(ing => {
                const d = ITEM_DATA[ing.id];
                const have = this.gs.getItemCount(ing.id);
                return `${d.icon}${have}/${ing.count}`;
            }).join(' ');

            const label = this.add.text(leftEdge, y + 6, `${resultData.icon} ${resultData.name}`, {
                fontSize: '12px', fontFamily: 'monospace', color: canCraft ? '#ddd' : '#666',
            });
            const ings = this.add.text(leftEdge, y + 24, ingStr, {
                fontSize: '10px', fontFamily: 'monospace', color: '#888',
            });
            this.craftContainer.add([label, ings]);

            if (canCraft && this.phase === 'prep' && !this.isCrafting) {
                bg.setInteractive({ useHandCursor: true });
                bg.on('pointerover', () => bg.setFillStyle(0x2a3a2a));
                bg.on('pointerout', () => bg.setFillStyle(0x1a2a1a));
                bg.on('pointerdown', () => this._startCrafting(recipe, resultData, recipe.result));
            }

            y += 44;
        });
    }

    _startCrafting(recipe, resultData, resultId) {
        if (this.isCrafting) return;
        this.isCrafting = true;

        recipe.ingredients.forEach(ing => this.gs.removeItem(ing.id, ing.count));
        this._refreshCraft();
        this._refreshInventory();

        this.craftOverlay.removeAll(true);
        this.craftOverlay.setVisible(true);

        const cx = 640, cy = 360;
        const ovBg = this.add.rectangle(cx, cy, 1280, 720, 0x000000, 0.6);
        const panel = this.add.rectangle(cx, cy, 420, 240, 0x12121e, 0.98).setStrokeStyle(2, 0x3a3a60);
        const title = this.add.text(cx, cy - 90, `${resultData.icon} ${resultData.name} 제작 중...`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ff8844',
        }).setOrigin(0.5);

        const barW = 320, barH = 20;
        const barBg = this.add.rectangle(cx, cy - 40, barW, barH, 0x222235).setStrokeStyle(1, 0x3a3a60);
        const barFill = this.add.rectangle(cx - barW / 2, cy - 40, 0, barH - 4, 0xff8844);
        barFill.setOrigin(0, 0.5);

        const progressText = this.add.text(cx, cy - 15, '', {
            fontSize: '11px', fontFamily: 'monospace', color: '#888',
        }).setOrigin(0.5);

        this.craftOverlay.add([ovBg, panel, title, barBg, barFill, progressText]);

        const CRAFT_TIME = 2000;
        this.tweens.add({
            targets: barFill,
            width: barW - 4,
            duration: CRAFT_TIME,
            ease: 'Linear',
            onUpdate: (tween) => {
                const p = Math.round(tween.progress * 100);
                progressText.setText(`${p}%`);
            },
            onComplete: () => {
                this._startTimingGame(resultData, resultId);
            },
        });

        const skipBtn = new UIButton(this, cx, cy + 40, '자동 제작 (1개)', {
            width: 180, height: 34, bg: 0x222240, hoverBg: 0x333355,
            textColor: '#888', fontSize: '12px',
            onClick: () => {
                this.tweens.killAll();
                this._finishCraft(resultData, resultId, 'auto');
            },
        });
        this.craftOverlay.add(skipBtn.container);
    }

    _startTimingGame(resultData, resultId) {
        this.craftOverlay.removeAll(true);

        const cx = 640, cy = 360;
        const ovBg = this.add.rectangle(cx, cy, 1280, 720, 0x000000, 0.6);
        const panel = this.add.rectangle(cx, cy, 420, 260, 0x12121e, 0.98).setStrokeStyle(2, 0x3a3a60);
        const title = this.add.text(cx, cy - 100, `${resultData.icon} 타이밍 클릭!`, {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffcc44',
        }).setOrigin(0.5);
        const hint = this.add.text(cx, cy - 75, '노란 구간에서 클릭! 중앙이면 Perfect!', {
            fontSize: '11px', fontFamily: 'monospace', color: '#888',
        }).setOrigin(0.5);

        const barW = 340, barH = 30;
        const barBg = this.add.rectangle(cx, cy - 20, barW, barH, 0x222235).setStrokeStyle(1, 0x3a3a60);

        const goodW = barW * 0.4;
        const goodZone = this.add.rectangle(cx, cy - 20, goodW, barH - 4, 0x444422, 0.6);

        const perfectW = barW * 0.1;
        const perfectZone = this.add.rectangle(cx, cy - 20, perfectW, barH - 4, 0x886622, 0.8);

        const cursor = this.add.rectangle(cx - barW / 2, cy - 20, 4, barH + 6, 0xff4444);

        const resultText = this.add.text(cx, cy + 30, '', {
            fontSize: '20px', fontFamily: 'monospace', color: '#fff',
        }).setOrigin(0.5);

        this.craftOverlay.add([ovBg, panel, title, hint, barBg, goodZone, perfectZone, cursor, resultText]);

        const speed = 1200;
        const cursorTween = this.tweens.add({
            targets: cursor,
            x: cx + barW / 2,
            duration: speed,
            yoyo: true,
            repeat: -1,
            ease: 'Linear',
        });

        let clicked = false;

        const clickZone = this.add.rectangle(cx, cy, 420, 260, 0x000000, 0.01);
        clickZone.setInteractive({ useHandCursor: true });
        this.craftOverlay.add(clickZone);

        clickZone.on('pointerdown', () => {
            if (clicked) return;
            clicked = true;
            cursorTween.stop();

            const cursorPos = cursor.x;
            const dist = Math.abs(cursorPos - cx);
            const halfGoodW = goodW / 2;
            const halfPerfW = perfectW / 2;

            let grade;
            if (dist <= halfPerfW) {
                grade = 'perfect';
                cursor.setFillStyle(0xffcc44);
                resultText.setText('PERFECT!').setColor('#ffcc44');
            } else if (dist <= halfGoodW) {
                grade = 'good';
                cursor.setFillStyle(0x44ff88);
                resultText.setText('Good!').setColor('#44ff88');
            } else {
                grade = 'miss';
                cursor.setFillStyle(0xff4444);
                resultText.setText('Miss...').setColor('#ff6666');
            }

            this.time.delayedCall(800, () => this._finishCraft(resultData, resultId, grade));
        });

        this.time.delayedCall(5000, () => {
            if (!clicked) {
                clicked = true;
                cursorTween.stop();
                resultText.setText('자동 제작').setColor('#888888');
                this.time.delayedCall(500, () => this._finishCraft(resultData, resultId, 'auto'));
            }
        });
    }

    _finishCraft(resultData, resultId, grade) {
        let qty = 1;
        let msg = '';

        if (grade === 'perfect') {
            qty = 2;
            msg = `${resultData.icon} ${resultData.name} x2 제작!`;
        } else if (grade === 'good') {
            qty = 1;
            msg = `${resultData.icon} ${resultData.name} 제작 완료!`;
        } else if (grade === 'miss') {
            qty = 1;
            msg = `${resultData.icon} ${resultData.name} 제작... (아쉽게 1개)`;
        } else {
            qty = 1;
            msg = `${resultData.icon} ${resultData.name} 자동 제작 1개`;
        }

        this.gs.addItems([{ id: resultId, qty }]);

        this.craftOverlay.removeAll(true);
        this.craftOverlay.setVisible(false);
        this.isCrafting = false;

        const color = grade === 'perfect' ? '#ffcc44' : grade === 'good' ? '#44ff88' : '#aaaaaa';
        Toast.show(this, msg, { color });

        this._refreshCraft();
        this._refreshInventory();
    }

    // === 진열대 ===
    _drawShopDisplay() {
        const px = this.col2X, py = this.panelTop + this.panelH / 2;
        this.add.rectangle(px, py, this.panelW, this.panelH, 0x12121e, 0.95).setStrokeStyle(1, 0x222240);
        this.add.text(px, this.panelTop + 18, '진열대 (인벤에서 클릭)', { fontSize: '13px', fontFamily: 'monospace', color: '#ffeebb' }).setOrigin(0.5);

        this.displaySlots = [];
        const slotW = 105, slotH = 85;
        const cols = 3;
        const gridW = cols * slotW + (cols - 1) * 8;
        const gridLeft = px - gridW / 2;

        for (let i = 0; i < this.gs.shopSlots; i++) {
            const col = i % cols, row = Math.floor(i / cols);
            const sx = gridLeft + col * (slotW + 8) + slotW / 2;
            const sy = this.panelTop + 50 + row * (slotH + 10) + slotH / 2;

            const slot = this.add.rectangle(sx, sy, slotW, slotH, 0x181830);
            slot.setStrokeStyle(1, 0x2a2a50);
            const icon = this.add.text(sx, sy - 18, '', { fontSize: '26px' }).setOrigin(0.5);
            const name = this.add.text(sx, sy + 10, '', { fontSize: '10px', fontFamily: 'monospace', color: '#ccc' }).setOrigin(0.5);
            const price = this.add.text(sx, sy + 26, '', { fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44' }).setOrigin(0.5);
            this.displaySlots.push({ bg: slot, icon, name, price, item: null, setPrice: 0 });
        }
    }

    // === 인벤토리 ===
    _drawInventory() {
        const px = this.col3X, py = this.panelTop + this.panelH / 2;
        this.add.rectangle(px, py, this.panelW, this.panelH, 0x12121e, 0.95).setStrokeStyle(1, 0x222240);
        this.add.text(px, this.panelTop + 18, '인벤토리', { fontSize: '14px', fontFamily: 'monospace', color: '#44ff88' }).setOrigin(0.5);
        this.invContainer = this.add.container(0, 0);
        this._refreshInventory();
    }

    _refreshInventory() {
        this.invContainer.removeAll(true);
        const items = this.gs.inventory.filter(i => i.qty > 0);
        const px = this.col3X;
        const cols = 3;
        const cellW = (this.panelW - 20) / cols;
        const gridLeft = px - this.panelW / 2 + 10;

        items.forEach((item, idx) => {
            const data = ITEM_DATA[item.id];
            if (!data) return;
            const col = idx % cols, row = Math.floor(idx / cols);
            const x = gridLeft + col * cellW;
            const y = this.panelTop + 38 + row * 48;
            const cellCx = x + cellW / 2;

            const bg = this.add.rectangle(cellCx, y + 16, cellW - 6, 40, 0x181830);
            bg.setStrokeStyle(1, 0x222240);
            bg.setInteractive({ useHandCursor: true });
            const t = this.add.text(x + 4, y + 4, `${data.icon}${data.name}`, { fontSize: '10px', fontFamily: 'monospace', color: '#ccc' });
            const q = this.add.text(x + 4, y + 20, `x${item.qty} (${data.basePrice}G)`, { fontSize: '9px', fontFamily: 'monospace', color: '#888' });
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

    // === 손님 영역 ===
    _drawCustomerArea() {
        const cx = 640;
        const custW = 1280 - 60;
        const custCy = this.custTop + this.custH / 2;

        this.add.rectangle(cx, custCy, custW, this.custH, 0x10101e, 0.95).setStrokeStyle(1, 0x222240);

        const iconX = cx - custW / 2 + 60;
        this.custIcon = this.add.text(iconX, custCy - 30, '', { fontSize: '40px' }).setOrigin(0.5);
        this.custName = this.add.text(iconX, custCy + 6, '', { fontSize: '12px', fontFamily: 'monospace', color: '#ddd' }).setOrigin(0.5);
        this.custBudget = this.add.text(iconX, custCy + 22, '', { fontSize: '10px', fontFamily: 'monospace', color: '#888' }).setOrigin(0.5);

        this.dialogText = this.add.text(cx + 30, custCy - 20, '', {
            fontSize: '14px', fontFamily: 'monospace', color: '#eeddcc', wordWrap: { width: 480 }, align: 'center',
        }).setOrigin(0.5);
        this.custWant = this.add.text(cx + 30, custCy + 10, '', { fontSize: '12px', fontFamily: 'monospace', color: '#aaa' }).setOrigin(0.5);

        const btnY = custCy + 55;
        this.startBtn = new UIButton(this, cx, btnY, '영업 시작!', {
            width: 200, height: 40, bg: 0x2a2210, hoverBg: 0x3a3320,
            textColor: '#ffcc88', fontSize: '15px',
            onClick: () => this._startSelling(),
        });

        this.actionContainer = this.add.container(0, 0).setVisible(false);
        this.acceptBtn = new UIButton(this, cx - 80, btnY, '판매', {
            width: 120, height: 36, bg: 0x224422, hoverBg: 0x336633,
            textColor: '#44ff88', fontSize: '14px', onClick: () => this._accept(),
        });
        this.rejectBtn = new UIButton(this, cx + 80, btnY, '거절', {
            width: 120, height: 36, bg: 0x442222, hoverBg: 0x663333,
            textColor: '#ff4444', fontSize: '14px', onClick: () => this._reject(),
        });
        this.actionContainer.add([this.acceptBtn.container, this.rejectBtn.container]);

        this.nightBtn = new UIButton(this, cx + custW / 2 - 120, btnY, '의뢰서 작성', {
            width: 160, height: 36, bg: 0x1a1a2a, hoverBg: 0x2a2a3a,
            textColor: '#aaaaff', fontSize: '13px',
            onClick: () => {
                this._returnUnsold();
                this.scene.start('CommissionScene');
            },
        });
    }

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

        // --- Customer entrance animation (icon slides in from right) ---
        this.custIcon.setText(c.icon);
        this.custIcon.setAlpha(0);
        const origIconX = this.custIcon.x;
        this.custIcon.setX(origIconX + 120);
        this.tweens.add({ targets: this.custIcon, x: origIconX, alpha: 1, duration: 400, ease: 'Back.easeOut' });

        this.custName.setText(c.name);
        this.custName.setAlpha(0);
        this.tweens.add({ targets: this.custName, alpha: 1, duration: 300, delay: 200 });

        this.custBudget.setText(`예산: ~${c.budget}G`);
        this.custBudget.setAlpha(0);
        this.tweens.add({ targets: this.custBudget, alpha: 1, duration: 300, delay: 300 });

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
        this.custWant.setText(`${data.icon} ${data.name} - ${slot.setPrice}G`);

        if (result.action === 'buy') {
            this.dialogText.setText(`"${data.name}! ${slot.setPrice}G요? 좋아요, 살게요!"`);
            this.actionContainer.setVisible(true);
            this.acceptBtn.label.setText(`${slot.setPrice}G`);
            this.pendingAction = { action: 'buy', price: slot.setPrice };
        } else if (result.action === 'haggle') {
            this.dialogText.setText(`"${slot.setPrice}G는 좀... ${result.offer}G 어때요?"`);
            this.actionContainer.setVisible(true);
            this.acceptBtn.label.setText(`${result.offer}G`);
            this.pendingAction = { action: 'haggle', price: result.offer };
        } else {
            this.dialogText.setText(`"${result.reason}..."`);
            this.actionContainer.setVisible(false);
            // --- Subtle shake on dialog text ---
            this._shakeDialog();
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

        // --- Coin scatter effect ---
        const coinX = this.currentSlot.bg.x;
        const coinY = this.currentSlot.bg.y;
        for (let i = 0; i < 8; i++) {
            const coin = this.add.circle(coinX, coinY, Phaser.Math.Between(2, 4), 0xffcc44, 0.9);
            this.tweens.add({
                targets: coin,
                x: coinX + Phaser.Math.Between(-60, 60),
                y: coinY - Phaser.Math.Between(30, 80),
                alpha: 0, duration: Phaser.Math.Between(400, 700),
                delay: Phaser.Math.Between(0, 150),
                ease: 'Quad.easeOut',
                onComplete: () => coin.destroy(),
            });
        }

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
            this.acceptBtn.label.setText(`${newOffer}G`);
            this.pendingAction = { action: 'haggle', price: newOffer };
            return;
        }
        this.dialogText.setText('"아쉽네요..."');
        // --- Subtle shake on dialog text ---
        this._shakeDialog();
        ShopSystem.failedSale(this.gs);
        this._updateInfo();
        this.time.delayedCall(800, () => this._nextCustomer());
    }

    _shakeDialog() {
        const origX = this.dialogText.x;
        this.tweens.add({
            targets: this.dialogText, x: origX + 4, duration: 50, yoyo: true, repeat: 3,
            onComplete: () => this.dialogText.setX(origX),
        });
    }

    _endSelling() {
        this.phase = 'done';
        this._returnUnsold();
        const totalSales = this.salesLog.reduce((s, l) => s + l.price, 0);
        this._updateInfo();

        const cx = 640;
        this.add.rectangle(cx, 360, 1280, 720, 0x000000, 0.7).setDepth(10);
        this.add.rectangle(cx, 340, 420, 300, 0x12121e, 0.98).setDepth(11).setStrokeStyle(2, 0x3a3a60);

        this.add.text(cx, 210, '영업 종료', { fontSize: '22px', fontFamily: 'monospace', color: '#ffeebb' }).setOrigin(0.5).setDepth(11);
        let y = 250;
        if (!this.salesLog.length) {
            this.add.text(cx, y, '오늘은 판매가 없었습니다.', { fontSize: '13px', fontFamily: 'monospace', color: '#666' }).setOrigin(0.5).setDepth(11);
            y += 25;
        } else {
            this.salesLog.forEach(l => {
                this.add.text(cx, y, `${l.item} - ${l.price}G`, { fontSize: '13px', fontFamily: 'monospace', color: '#ddd' }).setOrigin(0.5).setDepth(11);
                y += 22;
            });
        }
        this.add.text(cx, y + 10, `총 매출: ${totalSales}G`, { fontSize: '17px', fontFamily: 'monospace', color: '#ffcc44' }).setOrigin(0.5).setDepth(11);

        new UIButton(this, cx, y + 55, '의뢰서 작성', {
            width: 220, height: 44, bg: 0x1a1a2a, hoverBg: 0x2a2a3a,
            textColor: '#aaaaff', fontSize: '15px',
            onClick: () => this.scene.start('CommissionScene'),
        }).container.setDepth(11);
    }
}
