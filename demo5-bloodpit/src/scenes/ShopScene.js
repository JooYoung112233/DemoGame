class BPShopScene extends Phaser.Scene {
    constructor() { super('BPShopScene'); }

    init(data) {
        this.partyKeys = data.party;
        this.dangerSystem = data.dangerSystem;
        this.dropSystem = data.dropSystem;
        this.runManager = data.runManager;
        this.appliedDrops = data.appliedDrops || [];
        this.prevAllies = data.allies || [];
        this.battleTime = data.time || 0;
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a1a0a');
        const cx = 640;
        const round = this.runManager.getCurrentRound();

        this.add.text(cx, 25, `🛒 상점 — 라운드 ${round.round}`, {
            fontSize: '26px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(cx, 55, `💰 ${StashManager.getGold()}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44'
        }).setOrigin(0.5);

        this.shopItems = this._generateShopItems(round.dangerLevel);
        this._drawShopItems();
        this._drawReviveSection();
        this._drawRecruitSection();
        this._drawContinueButton();
    }

    _generateShopItems(danger) {
        const count = 5 + Math.floor(Math.random() * 3);
        const items = [];
        for (let i = 0; i < count; i++) {
            const generated = LootGenerator.generate('demo5', danger, 1);
            if (generated.length > 0) {
                const item = generated[0];
                const basePrice = ItemRegistry.getValue(item);
                const discount = this.runManager.shopDiscount || 0;
                item.shopPrice = Math.floor(basePrice * (1 - discount) * 1.5);
                items.push(item);
            }
        }
        return items;
    }

    _drawShopItems() {
        const cx = 640;
        const startY = 100;
        const cols = 4;
        const cellW = 280, cellH = 60, gap = 10;
        const totalW = cols * cellW + (cols - 1) * gap;
        const startX = cx - totalW / 2 + cellW / 2;

        this.shopItemCards = [];

        this.shopItems.forEach((item, i) => {
            const col = i % cols;
            const row = Math.floor(i / cols);
            const x = startX + col * (cellW + gap);
            const y = startY + row * (cellH + gap);
            const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;

            const card = this.add.rectangle(x, y, cellW, cellH, 0x112211)
                .setStrokeStyle(2, rarity.borderColor).setInteractive();

            const reg = ItemRegistry.get(item.itemId) || item;
            const icon = reg.icon || '';
            const enhLabel = item.enhanceLevel ? ` +${item.enhanceLevel}` : '';
            this.add.text(x - cellW / 2 + 10, y - 12, `${icon} ${reg.name}${enhLabel}`, {
                fontSize: '13px', fontFamily: 'monospace', color: rarity.color, fontStyle: 'bold'
            });
            this.add.text(x + cellW / 2 - 10, y - 12, `💰${item.shopPrice}G`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44'
            }).setOrigin(1, 0);
            this.add.text(x - cellW / 2 + 10, y + 8, `[${rarity.name}]`, {
                fontSize: '10px', fontFamily: 'monospace', color: rarity.color
            });

            card.on('pointerover', () => {
                card.setFillStyle(0x223322);
                FarmingUI.showTooltip(this, item, x, y - 40);
            });
            card.on('pointerout', () => {
                card.setFillStyle(0x112211);
                FarmingUI.hideTooltip();
            });
            card.on('pointerdown', () => {
                this._buyItem(item, i);
            });

            this.shopItemCards.push({ card, item });
        });
    }

    _buyItem(item, idx) {
        const gold = StashManager.getGold();
        if (gold < item.shopPrice) return;
        StashManager.spendGold(item.shopPrice);
        StashManager.addToStash({ itemId: item.itemId, rarity: item.rarity, enhanceLevel: item.enhanceLevel || 0 });
        this.shopItems.splice(idx, 1);
        FarmingUI.hideTooltip();
        this._refreshScene();
    }

    _drawReviveSection() {
        const deadChars = BP_ROSTER.roster.filter(c => c.status !== 'ready');
        if (deadChars.length === 0) return;

        const y = 320;
        this.add.text(640, y, '── 부활 서비스 ──', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff8888'
        }).setOrigin(0.5);

        deadChars.forEach((char, i) => {
            const cx = 320 + i * 200;
            const cost = BP_ROSTER.getReviveCost(char);
            const base = BP_ALLIES[char.classKey];
            const statusLabel = char.status === 'dead' ? '💀 사망' : '🩹 부상';

            const btn = this.add.rectangle(cx, y + 40, 180, 50, 0x221111)
                .setStrokeStyle(2, 0xff4444).setInteractive();
            this.add.text(cx, y + 30, `${base.name} ${char.name}`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#ff8888'
            }).setOrigin(0.5);
            this.add.text(cx, y + 48, `${statusLabel} → 💰${cost}G 부활`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#ffaa44'
            }).setOrigin(0.5);

            btn.on('pointerdown', () => {
                const result = BP_ROSTER.reviveCharacter(char.id, cost);
                if (result.success) this._refreshScene();
            });
        });
    }

    _drawRecruitSection() {
        if (BP_ROSTER.roster.length >= BP_ROSTER.maxRoster) return;

        const y = 430;
        this.add.text(640, y, '── 징집 ──', {
            fontSize: '14px', fontFamily: 'monospace', color: '#88aaff'
        }).setOrigin(0.5);

        if (!this._recruits) {
            this._recruits = BP_ROSTER.generateRecruits(3);
        }

        this._recruits.forEach((recruit, i) => {
            const cx = 280 + i * 240;
            const base = BP_ALLIES[recruit.classKey];
            const btn = this.add.rectangle(cx, y + 45, 220, 60, 0x111122)
                .setStrokeStyle(2, base.color).setInteractive();
            this.add.text(cx, y + 28, `${base.name} — ${recruit.name}`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#ffffff'
            }).setOrigin(0.5);
            this.add.text(cx, y + 44, `HP:${recruit.baseStats.hp} ATK:${recruit.baseStats.atk} DEF:${recruit.baseStats.def}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#aaaaaa'
            }).setOrigin(0.5);
            this.add.text(cx, y + 58, `💰${recruit.hireCost}G`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#ffcc44'
            }).setOrigin(0.5);

            btn.on('pointerdown', () => {
                const result = BP_ROSTER.hireCharacter(recruit);
                if (result.success) {
                    this._recruits.splice(i, 1);
                    this._refreshScene();
                }
            });
        });
    }

    _drawContinueButton() {
        const btn = this.add.rectangle(640, 660, 250, 50, 0x225522)
            .setStrokeStyle(3, 0x44ff44).setInteractive();
        this.add.text(640, 660, '▶ 계속', {
            fontSize: '22px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);

        btn.on('pointerover', () => btn.setFillStyle(0x337733));
        btn.on('pointerout', () => btn.setFillStyle(0x225522));
        btn.on('pointerdown', () => {
            FarmingUI.hideTooltip();
            this._goNext();
        });
    }

    _goNext() {
        const nextRound = this.runManager.advanceRound();
        if (!nextRound) {
            this.scene.start('BPResultScene', {
                victory: true, dangerLevel: this.dangerSystem.level,
                drops: this.dropSystem.collectedDrops, allies: this.prevAllies,
                party: this.partyKeys, time: this.battleTime
            });
            return;
        }
        this._routeToScene(nextRound);
    }

    _routeToScene(round) {
        const data = {
            party: this.partyKeys,
            dangerSystem: this.dangerSystem,
            dropSystem: this.dropSystem,
            runManager: this.runManager,
            appliedDrops: this.appliedDrops,
            allies: this.prevAllies,
            time: this.battleTime
        };
        const sceneMap = {
            battle: 'BPBattleScene',
            boss: 'BPBattleScene',
            shop: 'BPShopScene',
            forge: 'BPForgeScene',
            event: 'BPEventScene'
        };
        this.scene.start(sceneMap[round.type] || 'BPBattleScene', data);
    }

    _refreshScene() {
        FarmingUI.hideTooltip();
        this.scene.restart({
            party: this.partyKeys,
            dangerSystem: this.dangerSystem,
            dropSystem: this.dropSystem,
            runManager: this.runManager,
            appliedDrops: this.appliedDrops,
            allies: this.prevAllies,
            time: this.battleTime
        });
    }
}
