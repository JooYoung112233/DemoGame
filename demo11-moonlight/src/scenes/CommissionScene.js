class CommissionScene extends Phaser.Scene {
    constructor() { super('CommissionScene'); }

    create() {
        this.gs = window.gameState;
        const cx = 640;

        // 밤 배경
        this.add.rectangle(cx, 360, 1280, 720, 0x080812);
        for (let i = 0; i < 40; i++) {
            const s = this.add.circle(Phaser.Math.Between(0, 1280), Phaser.Math.Between(0, 300),
                Phaser.Math.Between(1, 2), 0xffffff, Phaser.Math.FloatBetween(0.05, 0.35));
            this.tweens.add({ targets: s, alpha: 0.03, duration: Phaser.Math.Between(2000, 5000), yoyo: true, repeat: -1 });
        }
        this.add.circle(1100, 80, 30, 0xffeebb, 0.25);

        this.add.text(cx, 30, `🌙 Day ${this.gs.day} — 밤: 의뢰서 작성`, {
            fontSize: '26px', fontFamily: 'monospace', color: '#aaaaff',
            stroke: '#000', strokeThickness: 3,
        }).setOrigin(0.5);
        this.add.text(cx, 60, `💰 ${this.gs.gold}G  |  내일 아침에 결과가 도착합니다`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#666688',
        }).setOrigin(0.5);

        this.selectedAdventurer = null;
        this.selectedZone = null;
        this.commissions = [...this.gs.pendingCommissions];

        this._drawAdventurers();
        this._drawZones();
        this._drawCommissionList();
        this._drawButtons();
    }

    _drawAdventurers() {
        this.add.text(200, 90, '🧑 모험가 선택', { fontSize: '16px', fontFamily: 'monospace', color: '#ffcc88' }).setOrigin(0.5);
        const advs = this.gs.getAvailableAdventurers();
        this.advButtons = [];

        advs.forEach((adv, i) => {
            const y = 120 + i * 52;
            const bg = this.add.rectangle(200, y + 18, 350, 42, 0x151525, 0.95);
            bg.setStrokeStyle(1, 0x222240);
            bg.setInteractive({ useHandCursor: true });

            this.add.text(35, y + 5, `${adv.icon} ${adv.name}`, {
                fontSize: '13px', fontFamily: 'monospace', color: '#ddd',
            });
            this.add.text(35, y + 22, `${adv.cost}G | 성공 ${Math.round(adv.successRate * 100)}% | ×${adv.lootMult}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#888',
            });
            if (adv.bonusCategory) {
                const catName = CATEGORIES[adv.bonusCategory]?.name || '';
                this.add.text(280, y + 5, `+${catName}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: CATEGORIES[adv.bonusCategory]?.color || '#aaa',
                });
            }

            bg.on('pointerover', () => { if (this.selectedAdventurer !== adv) bg.setFillStyle(0x1a1a35); });
            bg.on('pointerout', () => { if (this.selectedAdventurer !== adv) bg.setFillStyle(0x151525); });
            bg.on('pointerdown', () => {
                this.advButtons.forEach(b => b.bg.setFillStyle(0x151525));
                this.selectedAdventurer = adv;
                bg.setFillStyle(0x1a2a3a);
            });

            this.advButtons.push({ bg, adv });
        });
    }

    _drawZones() {
        this.add.text(640, 90, '🗺️ 구역 선택', { fontSize: '16px', fontFamily: 'monospace', color: '#ffcc88' }).setOrigin(0.5);
        const allZones = Object.entries(ZONE_DATA);
        this.zoneButtons = [];

        allZones.forEach(([id, zone], i) => {
            const y = 120 + i * 52;
            const unlocked = this.gs.day >= zone.unlockDay;
            const bg = this.add.rectangle(640, y + 18, 350, 42, unlocked ? 0x151525 : 0x101018, 0.95);
            bg.setStrokeStyle(1, unlocked ? 0x222240 : 0x181825);

            this.add.text(475, y + 5, `${zone.icon} ${zone.name}`, {
                fontSize: '13px', fontFamily: 'monospace', color: unlocked ? '#ddd' : '#444',
            });
            const stars = '★'.repeat(zone.difficulty) + '☆'.repeat(4 - zone.difficulty);
            this.add.text(475, y + 22, stars, {
                fontSize: '10px', fontFamily: 'monospace', color: unlocked ? '#888' : '#333',
            });
            const drops = zone.drops.slice(0, 5).map(d => ITEM_DATA[d.id]?.icon || '?').join('');
            this.add.text(680, y + 12, drops, { fontSize: '12px' });

            if (unlocked) {
                bg.setInteractive({ useHandCursor: true });
                bg.on('pointerover', () => { if (this.selectedZone !== id) bg.setFillStyle(0x1a1a35); });
                bg.on('pointerout', () => { if (this.selectedZone !== id) bg.setFillStyle(0x151525); });
                bg.on('pointerdown', () => {
                    this.zoneButtons.forEach(b => b.bg.setFillStyle(b.unlocked ? 0x151525 : 0x101018));
                    this.selectedZone = id;
                    bg.setFillStyle(0x1a2a3a);
                    this._checkCanAdd();
                });
            } else {
                this.add.text(780, y + 12, `Day ${zone.unlockDay}+`, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#ff4444',
                });
            }

            this.zoneButtons.push({ bg, id, unlocked });
        });
    }

    _drawCommissionList() {
        this.add.rectangle(1060, 300, 300, 460, 0x10101e, 0.95).setStrokeStyle(1, 0x222240);
        this.add.text(1060, 85, '📜 의뢰 목록', { fontSize: '16px', fontFamily: 'monospace', color: '#aaaaff' }).setOrigin(0.5);
        this.commListContainer = this.add.container(0, 0);
        this._refreshCommList();
    }

    _refreshCommList() {
        this.commListContainer.removeAll(true);
        if (this.commissions.length === 0) {
            const t = this.add.text(1060, 200, '의뢰 없음\n\n모험가와 구역을\n선택하세요', {
                fontSize: '12px', fontFamily: 'monospace', color: '#444', align: 'center',
            }).setOrigin(0.5);
            this.commListContainer.add(t);
            return;
        }

        let y = 110;
        this.commissions.forEach((c, idx) => {
            const zone = ZONE_DATA[c.zoneId];
            const bg = this.add.rectangle(1060, y + 18, 270, 40, 0x151525);
            bg.setStrokeStyle(1, 0x222240);

            const t = this.add.text(940, y + 5, `${c.adventurer.icon} ${c.adventurer.name} → ${zone.icon} ${zone.name}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#ccc',
            });
            const cost = this.add.text(940, y + 20, `💰 ${c.adventurer.cost}G`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#ffcc44',
            });

            const delBtn = this.add.text(1180, y + 12, '✕', {
                fontSize: '14px', fontFamily: 'monospace', color: '#ff4444',
            }).setOrigin(0.5).setInteractive({ useHandCursor: true });
            delBtn.on('pointerdown', () => {
                this.gs.gold += c.adventurer.cost;
                this.commissions.splice(idx, 1);
                this._refreshCommList();
                this._updateCostLabel();
            });

            this.commListContainer.add([bg, t, cost, delBtn]);
            y += 48;
        });
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
        this.addBtn = new UIButton(this, 440, 430, '📝 의뢰 추가', {
            width: 200, height: 44, bg: 0x1a1a3a, hoverBg: 0x2a2a4a,
            textColor: '#aaaaff', fontSize: '15px',
            onClick: () => this._addCommission(),
        });

        this.totalCostLabel = this.add.text(640, 470, '', {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffcc44',
        }).setOrigin(0.5);
        this._updateCostLabel();

        new UIButton(this, 640, 560, '💤 잠들기 → 다음 날', {
            width: 280, height: 50, bg: 0x2a2210, hoverBg: 0x3a3320,
            textColor: '#ffeebb', fontSize: '17px',
            onClick: () => {
                this.gs.pendingCommissions = this.commissions;
                this.gs.advanceDay();
                this.scene.start('MorningScene');
            },
        });

        this.add.text(640, 610, '의뢰 없이 잠들어도 됩니다', {
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
    }

    _updateCostLabel() {
        const total = this.commissions.reduce((s, c) => s + c.adventurer.cost, 0);
        this.totalCostLabel.setText(`의뢰 ${this.commissions.length}건 | 총 비용: ${total}G | 잔여: ${this.gs.gold}G`);
    }
}
