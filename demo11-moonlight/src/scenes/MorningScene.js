class MorningScene extends Phaser.Scene {
    constructor() { super('MorningScene'); }

    create() {
        const gs = window.gameState;
        const cx = 640;
        const MARGIN = 50;
        const CONTENT_W = 1280 - MARGIN * 2;

        this.add.rectangle(cx, 360, 1280, 720, 0x1a1510);
        this.add.rectangle(cx, 30, 1280, 60, 0x2a2015, 0.8);
        this.add.rectangle(cx, 200, 300, 200, 0xffeecc, 0.04);

        this.add.text(cx, 18, `Day ${gs.day} - 아침`, {
            fontSize: '26px', fontFamily: 'monospace', color: '#ffddaa',
            stroke: '#000', strokeThickness: 3,
        }).setOrigin(0.5);

        this.add.text(cx, 45, `${gs.gold}G  |  평판 ${gs.reputation}`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#887766',
        }).setOrigin(0.5);

        this.scrollY = 0;
        this.contentContainer = this.add.container(0, 0);
        let y = 80;

        if (gs.commissionResults.length > 0) {
            y = this._drawCommissionResults(gs, cx, y, CONTENT_W);
        } else if (gs.day === 1) {
            const t = this.add.text(cx, y + 20, '첫 날입니다. 재료를 가지고 장사를 시작하세요!', {
                fontSize: '15px', fontFamily: 'monospace', color: '#aaaaaa',
            }).setOrigin(0.5);
            this.contentContainer.add(t);
            y += 55;
        } else {
            const t = this.add.text(cx, y + 20, '어젯밤 의뢰가 없었습니다.', {
                fontSize: '14px', fontFamily: 'monospace', color: '#666666',
            }).setOrigin(0.5);
            this.contentContainer.add(t);
            y += 55;
        }

        const newOrders = gs.generateOrders();
        if (newOrders.length > 0) {
            y = this._drawNewOrders(gs, cx, y, CONTENT_W, newOrders);
        }

        if (gs.activeOrders.length > 0) {
            y = this._drawActiveOrders(gs, cx, y, CONTENT_W);
        }

        const btnY = Math.max(y + 40, 620);
        new UIButton(this, cx, Math.min(btnY, 670), '가게 열기 (제작 & 판매)', {
            width: 320, height: 50, bg: 0x2a2210, hoverBg: 0x3a3320,
            textColor: '#ffcc88', fontSize: '17px',
            onClick: () => this.scene.start('ShopScene'),
        });
    }

    _drawCommissionResults(gs, cx, y, contentW) {
        const panelW = Math.min(contentW, 700);
        const panelLeft = cx - panelW / 2 + 30;

        const header = this.add.text(cx, y, '어젯밤 의뢰 결과', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffcc88',
        }).setOrigin(0.5);
        this.contentContainer.add(header);
        y += 30;

        gs.commissionResults.forEach((result, ri) => {
            const panelBg = this.add.rectangle(cx, y + 10, panelW, 0, result.success ? 0x1a2a1a : 0x2a1a1a, 0.9);
            panelBg.setStrokeStyle(1, result.success ? 0x2a4a2a : 0x4a2a2a);
            this.contentContainer.add(panelBg);

            let ly = y;
            result.log.forEach((entry) => {
                const text = typeof entry === 'string' ? entry : entry.text;
                const color = typeof entry === 'string' ? '#cccccc' : (entry.color || '#cccccc');
                if (!text) return;

                const t = this.add.text(panelLeft, ly, text, {
                    fontSize: '12px', fontFamily: 'monospace', color,
                }).setAlpha(0);
                this.contentContainer.add(t);

                const delay = typeof entry === 'object' ? entry.delay : 0;
                this.tweens.add({ targets: t, alpha: 1, duration: 300, delay: delay + ri * 1500 });
                ly += 18;
            });

            const panelH = ly - y + 20;
            panelBg.setSize(panelW, panelH);
            panelBg.setPosition(cx, y + panelH / 2);

            if (result.success) {
                result.items.forEach(item => gs.addItems([item]));
            }
            if (result.gold > 0) gs.gold += result.gold;

            y = ly + 25;
        });

        return y;
    }

    _drawNewOrders(gs, cx, y, contentW, newOrders) {
        const panelW = Math.min(contentW, 700);
        const panelLeft = cx - panelW / 2 + 20;

        y += 10;
        const header = this.add.text(cx, y, '새로운 주문 요청', {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffaa66',
        }).setOrigin(0.5);
        this.contentContainer.add(header);
        y += 30;

        newOrders.forEach(order => {
            const itemData = ITEM_DATA[order.item];
            const rowH = 52;
            const panel = this.add.rectangle(cx, y + rowH / 2, panelW, rowH, 0x1a1a25, 0.9);
            panel.setStrokeStyle(1, 0x2a2a45);
            this.contentContainer.add(panel);

            const npcText = this.add.text(panelLeft, y + 8, `${order.icon} ${order.npc}: "${order.dialog}"`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#ddddcc',
                wordWrap: { width: panelW - 130 },
            });
            this.contentContainer.add(npcText);

            const detailText = this.add.text(panelLeft, y + 28, `  ${itemData.icon} ${itemData.name} x${order.qty}  |  보상 ${order.reward}G  |  마감 Day ${order.deadlineDay}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa',
            });
            this.contentContainer.add(detailText);

            const btnX = cx + panelW / 2 - 50;
            new UIButton(this, btnX, y + rowH / 2, '수락', {
                width: 70, height: 30, bg: 0x224422, hoverBg: 0x336633,
                textColor: '#44ff88', fontSize: '12px',
                onClick: () => {
                    gs.acceptOrder(order);
                    panel.setFillStyle(0x1a2a1a);
                    Toast.show(this, `주문 수락! ${itemData.name} x${order.qty}`, { color: '#44ff88', y: y + rowH / 2 });
                }
            });

            y += rowH + 8;
        });

        return y;
    }

    _drawActiveOrders(gs, cx, y, contentW) {
        const panelW = Math.min(contentW, 700);
        const panelLeft = cx - panelW / 2 + 20;

        y += 10;
        const header = this.add.text(cx, y, '진행 중인 주문', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffaa44',
        }).setOrigin(0.5);
        this.contentContainer.add(header);
        y += 25;

        gs.activeOrders.forEach((order, idx) => {
            const itemData = ITEM_DATA[order.item];
            const have = gs.getItemCount(order.item);
            const daysLeft = order.deadlineDay - gs.day;
            const urgentColor = daysLeft <= 1 ? '#ff4444' : '#aaaaaa';

            const orderText = this.add.text(panelLeft, y, `${order.icon} ${itemData.icon} ${itemData.name} x${order.qty} (보유: ${have})  |  ${order.reward}G  |  D-${daysLeft}`, {
                fontSize: '12px', fontFamily: 'monospace', color: urgentColor,
            });
            this.contentContainer.add(orderText);

            if (have >= order.qty) {
                const btnX = cx + panelW / 2 - 50;
                new UIButton(this, btnX, y + 8, '납품', {
                    width: 70, height: 26, bg: 0x224422, hoverBg: 0x336633,
                    textColor: '#ffcc44', fontSize: '12px',
                    onClick: () => {
                        if (gs.fulfillOrder(idx)) {
                            Toast.show(this, `+${order.reward}G! 납품 완료!`, { color: '#ffcc44' });
                            this.scene.restart();
                        }
                    }
                });
            }

            y += 28;
        });

        return y;
    }
}
