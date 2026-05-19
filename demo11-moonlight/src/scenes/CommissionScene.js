class CommissionScene extends Phaser.Scene {
    constructor() { super('CommissionScene'); }

    create() {
        this.gs = window.gameState;
        const cx = 640;
        const MARGIN = 30;
        const GAP = 12;
        const TOTAL_W = 1280 - MARGIN * 2;
        const col3W = (TOTAL_W - GAP * 2) / 3;

        this.col1X = MARGIN + col3W / 2;
        this.col2X = MARGIN + col3W + GAP + col3W / 2;
        this.col3X = MARGIN + (col3W + GAP) * 2 + col3W / 2;
        this.colW = col3W;
        this.colLeft1 = MARGIN + 15;
        this.colLeft2 = MARGIN + col3W + GAP + 15;
        this.colLeft3 = MARGIN + (col3W + GAP) * 2 + 15;

        this.add.rectangle(cx, 360, 1280, 720, 0x080812);
        for (let i = 0; i < 40; i++) {
            const s = this.add.circle(Phaser.Math.Between(0, 1280), Phaser.Math.Between(0, 300),
                Phaser.Math.Between(1, 2), 0xffffff, Phaser.Math.FloatBetween(0.05, 0.35));
            this.tweens.add({ targets: s, alpha: 0.03, duration: Phaser.Math.Between(2000, 5000), yoyo: true, repeat: -1 });
        }
        this.add.circle(1100, 80, 30, 0xffeebb, 0.25);

        this.add.text(cx, 22, `Day ${this.gs.day} - 밤: 의뢰서 작성`, {
            fontSize: '24px', fontFamily: 'monospace', color: '#aaaaff',
            stroke: '#000', strokeThickness: 3,
        }).setOrigin(0.5);

        this.goldLabel = this.add.text(cx, 50, '', {
            fontSize: '12px', fontFamily: 'monospace', color: '#666688',
        }).setOrigin(0.5);
        this._updateGoldLabel();

        this.selectedAdventurer = null;
        this.selectedZone = null;
        this.commissions = [...this.gs.pendingCommissions];

        this._drawAdventurers();
        this._drawZones();
        this._drawCommissionList();
        this._drawButtons();
    }

    _updateGoldLabel() {
        this.goldLabel.setText(`${this.gs.gold}G  |  내일 아침에 결과가 도착합니다`);
    }

    _drawAdventurers() {
        const panelH = 370;
        const panelY = 70 + panelH / 2;
        this.add.rectangle(this.col1X, panelY, this.colW, panelH, 0x10101e, 0.95).setStrokeStyle(1, 0x222240);
        this.add.text(this.col1X, 82, '모험가 선택', { fontSize: '15px', fontFamily: 'monospace', color: '#ffcc88' }).setOrigin(0.5);

        const advs = this.gs.getAvailableAdventurers();
        this.advButtons = [];
        const itemW = this.colW - 20;

        advs.forEach((adv, i) => {
            const y = 105 + i * 50;
            const bg = this.add.rectangle(this.col1X, y + 18, itemW, 42, 0x151525, 0.95);
            bg.setStrokeStyle(1, 0x222240);
            bg.setInteractive({ useHandCursor: true });

            this.add.text(this.colLeft1, y + 5, `${adv.icon} ${adv.name}`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#ddd',
            });
            this.add.text(this.colLeft1, y + 21, `${adv.cost}G | 성공 ${Math.round(adv.successRate * 100)}% | x${adv.lootMult}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#888',
            });
            if (adv.bonusCategory) {
                const catName = CATEGORIES[adv.bonusCategory]?.name || '';
                this.add.text(this.col1X + this.colW / 2 - 20, y + 5, `+${catName}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: CATEGORIES[adv.bonusCategory]?.color || '#aaa',
                }).setOrigin(1, 0);
            }

            bg.on('pointerover', () => { if (this.selectedAdventurer !== adv) bg.setFillStyle(0x1a1a35); });
            bg.on('pointerout', () => { if (this.selectedAdventurer !== adv) bg.setFillStyle(0x151525); });
            bg.on('pointerdown', () => {
                this.advButtons.forEach(b => b.bg.setFillStyle(0x151525).setStrokeStyle(1, 0x222240));
                this.selectedAdventurer = adv;
                bg.setFillStyle(0x1a2a3a).setStrokeStyle(2, 0x44aaff);
            });

            this.advButtons.push({ bg, adv });
        });
    }

    _drawZones() {
        const panelH = 370;
        const panelY = 70 + panelH / 2;
        this.add.rectangle(this.col2X, panelY, this.colW, panelH, 0x10101e, 0.95).setStrokeStyle(1, 0x222240);
        this.add.text(this.col2X, 82, '구역 선택', { fontSize: '15px', fontFamily: 'monospace', color: '#ffcc88' }).setOrigin(0.5);

        const allZones = Object.entries(ZONE_DATA);
        this.zoneButtons = [];
        const itemW = this.colW - 20;

        allZones.forEach(([id, zone], i) => {
            const y = 105 + i * 55;
            const unlocked = this.gs.day >= zone.unlockDay;
            const bg = this.add.rectangle(this.col2X, y + 18, itemW, 45, unlocked ? 0x151525 : 0x101018, 0.95);
            bg.setStrokeStyle(1, unlocked ? 0x222240 : 0x181825);

            this.add.text(this.colLeft2, y + 5, `${zone.icon} ${zone.name}`, {
                fontSize: '12px', fontFamily: 'monospace', color: unlocked ? '#ddd' : '#444',
            });
            const stars = '★'.repeat(zone.difficulty) + '☆'.repeat(4 - zone.difficulty);
            this.add.text(this.colLeft2, y + 22, stars, {
                fontSize: '10px', fontFamily: 'monospace', color: unlocked ? '#888' : '#333',
            });
            const drops = zone.drops.slice(0, 5).map(d => ITEM_DATA[d.id]?.icon || '?').join('');
            this.add.text(this.col2X + this.colW / 2 - 20, y + 12, drops, { fontSize: '11px' }).setOrigin(1, 0);

            if (unlocked) {
                bg.setInteractive({ useHandCursor: true });
                bg.on('pointerover', () => { if (this.selectedZone !== id) bg.setFillStyle(0x1a1a35); });
                bg.on('pointerout', () => { if (this.selectedZone !== id) bg.setFillStyle(0x151525); });
                bg.on('pointerdown', () => {
                    this.zoneButtons.forEach(b => {
                        b.bg.setFillStyle(b.unlocked ? 0x151525 : 0x101018);
                        b.bg.setStrokeStyle(1, b.unlocked ? 0x222240 : 0x181825);
                    });
                    this.selectedZone = id;
                    bg.setFillStyle(0x1a2a3a).setStrokeStyle(2, 0x44aaff);
                    this._checkCanAdd();
                });
            } else {
                this.add.text(this.col2X + this.colW / 2 - 20, y + 25, `Day ${zone.unlockDay}+`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#ff4444',
                }).setOrigin(1, 0);
            }

            this.zoneButtons.push({ bg, id, unlocked });
        });
    }

    _drawCommissionList() {
        const panelH = 370;
        const panelY = 70 + panelH / 2;
        this.add.rectangle(this.col3X, panelY, this.colW, panelH, 0x10101e, 0.95).setStrokeStyle(1, 0x222240);
        this.add.text(this.col3X, 82, '의뢰 목록', { fontSize: '15px', fontFamily: 'monospace', color: '#aaaaff' }).setOrigin(0.5);

        this.commCountLabel = this.add.text(this.col3X, 100, '', {
            fontSize: '10px', fontFamily: 'monospace', color: '#666',
        }).setOrigin(0.5);

        this.commListContainer = this.add.container(0, 0);
        this._refreshCommList();
    }

    _refreshCommList() {
        this.commListContainer.removeAll(true);
        const itemW = this.colW - 20;

        this.commCountLabel.setText(`${this.commissions.length} / 3건`);

        if (this.commissions.length === 0) {
            const t = this.add.text(this.col3X, 220, '의뢰 없음\n\n모험가와 구역을\n선택 후 추가', {
                fontSize: '12px', fontFamily: 'monospace', color: '#444', align: 'center',
            }).setOrigin(0.5);
            this.commListContainer.add(t);
            return;
        }

        let y = 115;
        this.commissions.forEach((c, idx) => {
            const zone = ZONE_DATA[c.zoneId];

            const bg = this.add.rectangle(this.col3X, y + 30, itemW, 62, 0x151530, 0.95);
            bg.setStrokeStyle(1, 0x3344aa);

            const numBadge = this.add.rectangle(this.colLeft3 + 10, y + 15, 22, 22, 0x3344aa);
            const numText = this.add.text(this.colLeft3 + 10, y + 15, `${idx + 1}`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#fff',
            }).setOrigin(0.5);

            const advText = this.add.text(this.colLeft3 + 28, y + 10, `${c.adventurer.icon} ${c.adventurer.name}`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#ddd',
            });
            const zoneText = this.add.text(this.colLeft3 + 28, y + 27, `${zone.icon} ${zone.name}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#aaa',
            });
            const costText = this.add.text(this.colLeft3 + 28, y + 43, `${c.adventurer.cost}G | 성공 ${Math.round(c.adventurer.successRate * 100)}%`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#ffcc44',
            });

            const delBtn = this.add.text(this.col3X + this.colW / 2 - 22, y + 15, '✕', {
                fontSize: '16px', fontFamily: 'monospace', color: '#ff4444',
            }).setOrigin(0.5).setInteractive({ useHandCursor: true });
            delBtn.on('pointerover', () => delBtn.setColor('#ff8888'));
            delBtn.on('pointerout', () => delBtn.setColor('#ff4444'));
            delBtn.on('pointerdown', () => {
                this.gs.gold += c.adventurer.cost;
                this.commissions.splice(idx, 1);
                this._refreshCommList();
                this._updateCostLabel();
                this._updateGoldLabel();
            });

            this.commListContainer.add([bg, numBadge, numText, advText, zoneText, costText, delBtn]);
            y += 70;
        });

        // Total cost summary at bottom of list
        const total = this.commissions.reduce((s, c) => s + c.adventurer.cost, 0);
        const summary = this.add.text(this.col3X, y + 15, `총 의뢰비: ${total}G`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44',
        }).setOrigin(0.5);
        this.commListContainer.add(summary);
    }

    _checkCanAdd() {
        if (!this.selectedAdventurer || !this.selectedZone) return false;
        if (!this.selectedAdventurer.zones.includes(this.selectedZone)) {
            Toast.show(this, '이 모험가는 해당 구역에 갈 수 없습니다!', { color: '#ff4444' });
            return false;
        }
        return true;
    }

    _drawButtons() {
        const cx = 640;
        const btnAreaY = 478;

        // Back to shop button
        new UIButton(this, 100, btnAreaY, '가게로 돌아가기', {
            width: 170, height: 38, bg: 0x222235, hoverBg: 0x333350,
            textColor: '#aaaacc', fontSize: '12px',
            onClick: () => this.scene.start('ShopScene'),
        });

        this.addBtn = new UIButton(this, cx, btnAreaY, '의뢰 추가', {
            width: 200, height: 44, bg: 0x1a1a3a, hoverBg: 0x2a2a4a,
            textColor: '#aaaaff', fontSize: '15px',
            onClick: () => this._addCommission(),
        });

        this.totalCostLabel = this.add.text(cx, btnAreaY + 35, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffcc44',
        }).setOrigin(0.5);
        this._updateCostLabel();

        new UIButton(this, cx, btnAreaY + 90, '잠들기 - 다음 날', {
            width: 260, height: 48, bg: 0x2a2210, hoverBg: 0x3a3320,
            textColor: '#ffeebb', fontSize: '16px',
            onClick: () => {
                this.gs.pendingCommissions = this.commissions;
                this.gs.advanceDay();
                this.scene.start('MorningScene');
            },
        });

        this.add.text(cx, btnAreaY + 130, '의뢰 없이 잠들어도 됩니다', {
            fontSize: '11px', fontFamily: 'monospace', color: '#444',
        }).setOrigin(0.5);
    }

    _addCommission() {
        if (!this.selectedAdventurer || !this.selectedZone) {
            Toast.show(this, '모험가와 구역을 모두 선택하세요!', { color: '#ff4444' });
            return;
        }
        if (!this.selectedAdventurer.zones.includes(this.selectedZone)) {
            Toast.show(this, `${this.selectedAdventurer.name}은(는) 이 구역에 갈 수 없습니다!`, { color: '#ff4444' });
            return;
        }
        if (this.gs.gold < this.selectedAdventurer.cost) {
            Toast.show(this, '골드가 부족합니다!', { color: '#ff4444' });
            return;
        }
        if (this.commissions.length >= 3) {
            Toast.show(this, '하루 최대 3건까지만 의뢰 가능!', { color: '#ff4444' });
            return;
        }

        this.gs.gold -= this.selectedAdventurer.cost;
        this.commissions.push({ zoneId: this.selectedZone, adventurer: { ...this.selectedAdventurer } });
        Toast.show(this, `의뢰 추가! -${this.selectedAdventurer.cost}G`, { color: '#aaaaff' });
        this._refreshCommList();
        this._updateCostLabel();
        this._updateGoldLabel();
    }

    _updateCostLabel() {
        const total = this.commissions.reduce((s, c) => s + c.adventurer.cost, 0);
        this.totalCostLabel.setText(`의뢰 ${this.commissions.length}건 | 총 비용: ${total}G | 잔여: ${this.gs.gold}G`);
    }
}
