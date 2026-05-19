class MorningScene extends Phaser.Scene {
    constructor() { super('MorningScene'); }

    create() {
        const gs = window.gameState;
        const cx = 640;

        // 아침 배경 — 따뜻한 색
        this.add.rectangle(cx, 360, 1280, 720, 0x1a1510);
        this.add.rectangle(cx, 50, 1280, 100, 0x2a2015, 0.8);

        // 창문 빛
        this.add.rectangle(cx, 200, 300, 200, 0xffeecc, 0.04);

        this.add.text(cx, 30, `☀️ Day ${gs.day} — 아침`, {
            fontSize: '28px', fontFamily: 'monospace', color: '#ffddaa',
            stroke: '#000', strokeThickness: 3,
        }).setOrigin(0.5);

        this.add.text(cx, 60, `💰 ${gs.gold}G  |  ⭐ 평판 ${gs.reputation}`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#887766',
        }).setOrigin(0.5);

        let y = 100;

        // === 의뢰 결과 ===
        if (gs.commissionResults.length > 0) {
            this.add.text(cx, y, '📦 어젯밤 의뢰 결과', {
                fontSize: '20px', fontFamily: 'monospace', color: '#ffcc88',
            }).setOrigin(0.5);
            y += 35;

            gs.commissionResults.forEach((result, ri) => {
                const panel = this.add.rectangle(cx, y + 30, 580, 0, result.success ? 0x1a2a1a : 0x2a1a1a, 0.9);
                panel.setStrokeStyle(1, result.success ? 0x2a4a2a : 0x4a2a2a);

                let ly = y;
                const lines = [];

                result.log.forEach((entry, i) => {
                    const text = typeof entry === 'string' ? entry : entry.text;
                    const color = typeof entry === 'string' ? '#cccccc' : (entry.color || '#cccccc');
                    if (!text) return;

                    const t = this.add.text(380, ly, text, {
                        fontSize: '12px', fontFamily: 'monospace', color,
                    }).setAlpha(0);
                    lines.push(t);

                    const delay = typeof entry === 'object' ? entry.delay : i * 400;
                    this.tweens.add({ targets: t, alpha: 1, duration: 300, delay: delay + ri * 1500 });
                    ly += 18;
                });

                panel.setSize(580, ly - y + 15);
                panel.setPosition(cx, y + (ly - y) / 2 + 5);

                if (result.success) {
                    result.items.forEach(item => gs.addItems([item]));
                }
                if (result.gold > 0) gs.gold += result.gold;

                y = ly + 25;
            });
        } else if (gs.day === 1) {
            this.add.text(cx, y + 20, '🏠 첫 날입니다. 재료를 가지고 장사를 시작하세요!', {
                fontSize: '15px', fontFamily: 'monospace', color: '#aaaaaa',
            }).setOrigin(0.5);
            y += 60;
        } else {
            this.add.text(cx, y + 20, '📭 어젯밤 의뢰가 없었습니다.', {
                fontSize: '14px', fontFamily: 'monospace', color: '#666666',
            }).setOrigin(0.5);
            y += 60;
        }

        // === 새 주문 ===
        const newOrders = gs.generateOrders();
        if (newOrders.length > 0) {
            y += 10;
            this.add.text(cx, y, '📋 새로운 주문 요청', {
                fontSize: '20px', fontFamily: 'monospace', color: '#ffaa66',
            }).setOrigin(0.5);
            y += 35;

            newOrders.forEach(order => {
                const itemData = ITEM_DATA[order.item];
                const panel = this.add.rectangle(cx, y + 25, 560, 50, 0x1a1a25, 0.9);
                panel.setStrokeStyle(1, 0x2a2a45);

                this.add.text(390, y + 8, `${order.icon} ${order.npc}: "${order.dialog}"`, {
                    fontSize: '13px', fontFamily: 'monospace', color: '#ddddcc',
                });
                this.add.text(390, y + 28, `  → ${itemData.icon} ${itemData.name} x${order.qty}  |  보상 ${order.reward}G  |  마감 Day ${order.deadlineDay}`, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa',
                });

                new UIButton(this, 870, y + 25, '수락', {
                    width: 70, height: 30, bg: 0x224422, hoverBg: 0x336633,
                    textColor: '#44ff88', fontSize: '12px',
                    onClick: () => {
                        gs.acceptOrder(order);
                        panel.setFillStyle(0x1a2a1a);
                        Toast.show(this, `주문 수락! ${itemData.name} x${order.qty}`, { color: '#44ff88', y: y + 25 });
                    }
                });

                y += 60;
            });
        }

        // === 진행 중인 주문 표시 ===
        if (gs.activeOrders.length > 0) {
            y += 10;
            this.add.text(cx, y, '📌 진행 중인 주문', {
                fontSize: '16px', fontFamily: 'monospace', color: '#ffaa44',
            }).setOrigin(0.5);
            y += 25;

            gs.activeOrders.forEach((order, idx) => {
                const itemData = ITEM_DATA[order.item];
                const have = gs.getItemCount(order.item);
                const daysLeft = order.deadlineDay - gs.day;
                const urgentColor = daysLeft <= 1 ? '#ff4444' : '#aaaaaa';

                this.add.text(390, y, `${order.icon} ${itemData.icon} ${itemData.name} x${order.qty} (보유: ${have})  |  ${order.reward}G  |  D-${daysLeft}`, {
                    fontSize: '12px', fontFamily: 'monospace', color: urgentColor,
                });

                if (have >= order.qty) {
                    new UIButton(this, 870, y + 8, '납품', {
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

                y += 25;
            });
        }

        // 다음 단계 버튼
        y = Math.max(y + 30, 600);
        new UIButton(this, cx, Math.min(y, 660), '🏪 가게 열기 (제작 & 판매) →', {
            width: 340, height: 50, bg: 0x2a2210, hoverBg: 0x3a3320,
            textColor: '#ffcc88', fontSize: '17px',
            onClick: () => this.scene.start('ShopScene'),
        });
    }
}
