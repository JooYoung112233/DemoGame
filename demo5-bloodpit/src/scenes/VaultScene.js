class BPVaultScene extends Phaser.Scene {
    constructor() { super('BPVaultScene'); }

    init(data) {
        this.partyIds = data.party;
        this.dangerSystem = data.dangerSystem;
        this.dropSystem = data.dropSystem;
        this.runManager = data.runManager;
        this.appliedDrops = data.appliedDrops || [];
        this.prevAllies = data.allies || [];
        this.battleTime = data.time || 0;
        this.keycard = data.keycard;
        this._rewards = null;
        this._picked = false;
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');
        this.cameras.main.fadeIn(400);
        this._generateRewards();
        this._drawAll();
    }

    _generateRewards() {
        const tier = this._getKeyTier();
        this._rewards = [];
        const rewardCount = tier >= 3 ? 4 : 3;

        for (let i = 0; i < rewardCount; i++) {
            const roll = Math.random();
            if (roll < 0.25) {
                this._rewards.push(this._generateRecruit(tier));
            } else if (roll < 0.55) {
                this._rewards.push(this._generateEquipment(tier));
            } else if (roll < 0.80) {
                this._rewards.push(this._generateGold(tier));
            } else {
                this._rewards.push(this._generateEquipment(tier));
            }
        }
    }

    _getKeyTier() {
        if (!this.keycard) return 0;
        const tierMap = { 'vault_key_uncommon': 0, 'vault_key_rare': 1, 'vault_key_epic': 2, 'vault_key_legendary': 3 };
        return tierMap[this.keycard.itemId] || 0;
    }

    _generateRecruit(tier) {
        const gradePool = [
            [{ grade: 'rare', label: '★희귀', color: '#4488ff', mult: 1.2, p: 0.5 },
             { grade: 'epic', label: '★에픽', color: '#aa44ff', mult: 1.35, p: 0.4 },
             { grade: 'legendary', label: '★전설', color: '#ffaa00', mult: 1.5, p: 0.1 }],
            [{ grade: 'epic', label: '★에픽', color: '#aa44ff', mult: 1.35, p: 0.5 },
             { grade: 'legendary', label: '★전설', color: '#ffaa00', mult: 1.5, p: 0.4 },
             { grade: 'rare', label: '★희귀', color: '#4488ff', mult: 1.2, p: 0.1 }],
            [{ grade: 'epic', label: '★에픽', color: '#aa44ff', mult: 1.35, p: 0.3 },
             { grade: 'legendary', label: '★전설', color: '#ffaa00', mult: 1.5, p: 0.6 },
             { grade: 'rare', label: '★희귀', color: '#4488ff', mult: 1.2, p: 0.1 }],
            [{ grade: 'legendary', label: '★전설', color: '#ffaa00', mult: 1.5, p: 0.7 },
             { grade: 'epic', label: '★에픽', color: '#aa44ff', mult: 1.35, p: 0.3 }]
        ];
        const pool = gradePool[Math.min(tier, 3)];
        const r = Math.random();
        let cum = 0;
        let picked = pool[0];
        for (const g of pool) { cum += g.p; if (r < cum) { picked = g; break; } }

        const classKey = BP_CLASS_POOL[Math.floor(Math.random() * BP_CLASS_POOL.length)];
        const base = BP_ALLIES[classKey];
        const level = Math.min(3 + tier, 5);
        const name = BP_NAME_POOL.prefix[Math.floor(Math.random() * BP_NAME_POOL.prefix.length)] + ' ' +
                     BP_NAME_POOL.suffix[Math.floor(Math.random() * BP_NAME_POOL.suffix.length)];

        return {
            rewardType: 'recruit',
            classKey, name, level,
            grade: picked.grade,
            gradeLabel: picked.label,
            gradeColor: picked.color,
            statMult: picked.mult,
            baseName: base.name,
            icon: '👤'
        };
    }

    _generateEquipment(tier) {
        const rarityByTier = [
            ['rare', 'epic'],
            ['epic', 'legendary'],
            ['legendary', 'ancient'],
            ['ancient', 'mythical']
        ];
        const pool = rarityByTier[Math.min(tier, 3)];
        const rarity = Math.random() < 0.6 ? pool[0] : pool[1];

        const equipItems = ItemRegistry.getByDemo('demo5').filter(i =>
            i.category === 'equipment' && i.rarity === rarity
        );
        if (equipItems.length === 0) {
            return this._generateGold(tier);
        }
        const item = equipItems[Math.floor(Math.random() * equipItems.length)];
        const rarityInfo = FARMING.RARITIES[rarity] || FARMING.RARITIES.common;
        const stars = FARMING.getStars(rarity);

        return {
            rewardType: 'equipment',
            itemId: item.itemId,
            rarity: rarity,
            name: item.name,
            desc: item.desc,
            icon: item.icon,
            slot: item.slot,
            stars: stars,
            rarityName: rarityInfo.name,
            rarityColor: rarityInfo.color,
            rarityBorder: rarityInfo.borderColor
        };
    }

    _generateGold(tier) {
        const base = [100, 200, 400, 800];
        const amount = base[Math.min(tier, 3)] + Math.floor(Math.random() * base[Math.min(tier, 3)] * 0.5);
        return {
            rewardType: 'gold',
            amount: amount,
            icon: '💰',
            name: `${amount} 골드`,
            desc: '금화 더미'
        };
    }

    _drawAll() {
        this.children.removeAll();
        const cx = 640;

        // vault title
        const keyReg = this.keycard ? ItemRegistry.get(this.keycard.itemId) : null;
        const keyRarity = this.keycard ? (FARMING.RARITIES[this.keycard.rarity] || FARMING.RARITIES.common) : null;
        const keyStars = this.keycard ? FARMING.getStars(this.keycard.rarity) : '';

        this.add.text(cx, 30, '🏛️ 보물 금고', {
            fontSize: '28px', fontFamily: 'monospace', color: '#ffdd44', fontStyle: 'bold'
        }).setOrigin(0.5);

        if (keyRarity) {
            this.add.text(cx, 60, `${keyStars} [${keyRarity.name}] ${keyReg ? keyReg.name : '키카드'} 사용`, {
                fontSize: '13px', fontFamily: 'monospace', color: keyRarity.color
            }).setOrigin(0.5);
        }

        this.add.text(cx, 85, '💰 ' + StashManager.getGold() + 'G', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa8866'
        }).setOrigin(0.5);

        if (!this._picked) {
            this.add.text(cx, 110, '보상을 하나 선택하세요', {
                fontSize: '14px', fontFamily: 'monospace', color: '#ccaa44'
            }).setOrigin(0.5);

            const rewards = this._rewards;
            const cardW = 260, cardH = 220, gap = 25;
            const totalW = rewards.length * cardW + (rewards.length - 1) * gap;
            const startX = cx - totalW / 2 + cardW / 2;

            rewards.forEach((rw, i) => {
                const x = startX + i * (cardW + gap);
                const y = 260;
                this._drawRewardCard(x, y, cardW, cardH, rw, i);
            });
        } else {
            this.add.text(cx, 140, '보상 획득 완료!', {
                fontSize: '18px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
            }).setOrigin(0.5);

            const contBtn = this.add.rectangle(cx, 220, 240, 50, 0x882222).setStrokeStyle(2, 0xff4444).setInteractive();
            this.add.text(cx, 220, '▶ 계속하기', {
                fontSize: '16px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            }).setOrigin(0.5);
            contBtn.on('pointerover', () => contBtn.setFillStyle(0xaa3333));
            contBtn.on('pointerout', () => contBtn.setFillStyle(0x882222));
            contBtn.on('pointerdown', () => this._continue());
        }

        // decorative sparkles
        for (let i = 0; i < 15; i++) {
            const sx = Math.random() * 1280;
            const sy = Math.random() * 720;
            const star = this.add.text(sx, sy, '✦', {
                fontSize: (8 + Math.random() * 10) + 'px', fontFamily: 'monospace',
                color: ['#ffdd44', '#ff8844', '#44aaff', '#ff44aa'][Math.floor(Math.random() * 4)]
            }).setAlpha(0.2 + Math.random() * 0.3);
            this.tweens.add({
                targets: star, alpha: 0, duration: 1500 + Math.random() * 2000,
                yoyo: true, repeat: -1, delay: Math.random() * 1500
            });
        }
    }

    _drawRewardCard(x, y, w, h, rw, idx) {
        let borderColor = 0xffdd44;
        let bgColor = 0x111122;

        if (rw.rewardType === 'equipment') {
            borderColor = rw.rarityBorder || 0xffdd44;
        } else if (rw.rewardType === 'recruit') {
            const colorMap = { legendary: 0xffaa00, epic: 0xaa44ff, rare: 0x4488ff };
            borderColor = colorMap[rw.grade] || 0xffdd44;
        } else {
            borderColor = 0xffaa44;
        }

        const card = this.add.rectangle(x, y, w, h, bgColor).setStrokeStyle(3, borderColor).setInteractive();

        let ty = y - h / 2 + 20;

        if (rw.rewardType === 'recruit') {
            this.add.text(x, ty, '👤 영웅 징집', { fontSize: '11px', fontFamily: 'monospace', color: '#888888' }).setOrigin(0.5);
            ty += 18;
            this.add.text(x, ty, `[${rw.gradeLabel}]`, { fontSize: '13px', fontFamily: 'monospace', color: rw.gradeColor, fontStyle: 'bold' }).setOrigin(0.5);
            ty += 20;
            this.add.text(x, ty, `${rw.baseName} Lv.${rw.level}`, { fontSize: '15px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold' }).setOrigin(0.5);
            ty += 18;
            this.add.text(x, ty, rw.name, { fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa' }).setOrigin(0.5);
            ty += 22;
            this.add.text(x, ty, `스탯 ×${rw.statMult}`, { fontSize: '12px', fontFamily: 'monospace', color: rw.gradeColor }).setOrigin(0.5);
        } else if (rw.rewardType === 'equipment') {
            this.add.text(x, ty, rw.stars || '', { fontSize: '12px', fontFamily: 'monospace', color: rw.rarityColor }).setOrigin(0.5);
            ty += 16;
            this.add.text(x, ty, `[${rw.rarityName}]`, { fontSize: '11px', fontFamily: 'monospace', color: rw.rarityColor }).setOrigin(0.5);
            ty += 20;
            this.add.text(x, ty, `${rw.icon} ${rw.name}`, { fontSize: '15px', fontFamily: 'monospace', color: rw.rarityColor, fontStyle: 'bold' }).setOrigin(0.5);
            ty += 18;
            const slotLabel = { weapon: '무기', armor: '방어구', accessory: '악세서리' }[rw.slot] || '';
            this.add.text(x, ty, slotLabel, { fontSize: '11px', fontFamily: 'monospace', color: '#888888' }).setOrigin(0.5);
            ty += 20;
            this.add.text(x, ty, rw.desc, {
                fontSize: '11px', fontFamily: 'monospace', color: '#cccccc',
                wordWrap: { width: w - 30 }, align: 'center'
            }).setOrigin(0.5);
        } else {
            this.add.text(x, ty, '💰', { fontSize: '24px', fontFamily: 'monospace' }).setOrigin(0.5);
            ty += 30;
            this.add.text(x, ty, `${rw.amount} 골드`, { fontSize: '18px', fontFamily: 'monospace', color: '#ffdd44', fontStyle: 'bold' }).setOrigin(0.5);
            ty += 22;
            this.add.text(x, ty, '금화 더미를 발견했다!', { fontSize: '11px', fontFamily: 'monospace', color: '#aa8866' }).setOrigin(0.5);
        }

        const btnY = y + h / 2 - 28;
        const btn = this.add.rectangle(x, btnY, 120, 30, borderColor, 0.8).setInteractive();
        this.add.text(x, btnY, '획득', { fontSize: '14px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold' }).setOrigin(0.5);

        card.on('pointerover', () => card.setFillStyle(0x1a1a33));
        card.on('pointerout', () => card.setFillStyle(bgColor));

        const doSelect = () => {
            this._claimReward(rw);
            this._picked = true;
            this._drawAll();
        };
        card.on('pointerdown', doSelect);
        btn.on('pointerdown', doSelect);
    }

    _claimReward(rw) {
        if (rw.rewardType === 'gold') {
            StashManager.addGold(rw.amount);
        } else if (rw.rewardType === 'equipment') {
            BP_FARMING.addToRaidInventory({ itemId: rw.itemId, rarity: rw.rarity, enhanceLevel: 0 });
        } else if (rw.rewardType === 'recruit') {
            if (BP_ROSTER.roster.length < BP_ROSTER.maxRoster) {
                const char = BP_ROSTER._createCharacter(rw.classKey, rw.level);
                const m = rw.statMult;
                const v = () => 0.9 + Math.random() * 0.2;
                char.baseStats.hp = Math.floor(char.baseStats.hp * m * v());
                char.baseStats.atk = Math.floor(char.baseStats.atk * m * v());
                char.baseStats.def = Math.floor(char.baseStats.def * m * v());
                char.baseStats.critRate = Math.min(0.8, +(char.baseStats.critRate * (0.9 + m * 0.1 + Math.random() * 0.1)).toFixed(3));
                char.baseStats.dodgeRate = Math.min(0.6, +(char.baseStats.dodgeRate * (0.9 + m * 0.1 + Math.random() * 0.1)).toFixed(3));
                char.grade = rw.grade;
                char.gradeLabel = rw.gradeLabel;
                char.gradeColor = rw.gradeColor;
                char.name = rw.name;
                BP_ROSTER.roster.push(char);
                BP_ROSTER.save();
            } else {
                StashManager.addGold(200);
            }
        }
    }

    _continue() {
        this.cameras.main.fadeOut(300, 0, 0, 0, (cam, progress) => {
            if (progress === 1) {
                this.scene.start('BPDropScene', {
                    party: this.partyIds,
                    dangerSystem: this.dangerSystem,
                    dropSystem: this.dropSystem,
                    runManager: this.runManager,
                    appliedDrops: this.appliedDrops,
                    allies: this.prevAllies,
                    time: this.battleTime,
                    skipDropCards: true
                });
            }
        });
    }
}
