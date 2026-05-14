class BPForgeScene extends Phaser.Scene {
    constructor() { super('BPForgeScene'); }

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
        this.cameras.main.setBackgroundColor('#1a1000');
        const cx = 640;
        const round = this.runManager.getCurrentRound();

        this.add.text(cx, 25, `🔨 대장간 — 라운드 ${round.round}`, {
            fontSize: '26px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.goldText = this.add.text(cx, 55, `💰 ${StashManager.getGold()}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44'
        }).setOrigin(0.5);

        this.mode = 'enhance';
        this._drawTabs();
        this._drawContent();
        this._drawContinueButton();
    }

    _drawTabs() {
        const enhTab = this.add.rectangle(540, 90, 180, 36, this.mode === 'enhance' ? 0x443300 : 0x222200)
            .setStrokeStyle(2, 0xffaa44).setInteractive();
        this.add.text(540, 90, '⚒️ 강화', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        const craftTab = this.add.rectangle(740, 90, 180, 36, this.mode === 'craft' ? 0x443300 : 0x222200)
            .setStrokeStyle(2, 0xffaa44).setInteractive();
        this.add.text(740, 90, '🔮 조합', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        enhTab.on('pointerdown', () => { this.mode = 'enhance'; this._refresh(); });
        craftTab.on('pointerdown', () => { this.mode = 'craft'; this._refresh(); });
    }

    _drawContent() {
        if (this.mode === 'enhance') {
            this._drawEnhancePanel();
        } else {
            this._drawCraftPanel();
        }
    }

    _drawEnhancePanel() {
        const cx = 640;
        const stash = StashManager.getStash();
        const equipment = stash.filter(item => {
            const reg = ItemRegistry.get(item.itemId);
            return reg && reg.category === 'equipment';
        });

        if (equipment.length === 0) {
            this.add.text(cx, 300, '강화할 장비가 없습니다', {
                fontSize: '16px', fontFamily: 'monospace', color: '#886644'
            }).setOrigin(0.5);
            return;
        }

        this.add.text(cx, 130, '장비를 선택하세요', {
            fontSize: '14px', fontFamily: 'monospace', color: '#aa8844'
        }).setOrigin(0.5);

        const cols = 5, cellW = 220, cellH = 55, gap = 8;
        const totalW = cols * cellW + (cols - 1) * gap;
        const startX = cx - totalW / 2 + cellW / 2;

        equipment.forEach((item, i) => {
            const col = i % cols;
            const row = Math.floor(i / cols);
            const x = startX + col * (cellW + gap);
            const y = 170 + row * (cellH + gap);
            const reg = ItemRegistry.get(item.itemId) || {};
            const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;
            const enhLevel = item.enhanceLevel || 0;

            const card = this.add.rectangle(x, y, cellW, cellH, 0x221100)
                .setStrokeStyle(2, rarity.borderColor).setInteractive();
            this.add.text(x, y - 10, `${reg.icon || ''} ${reg.name || '???'} +${enhLevel}`, {
                fontSize: '12px', fontFamily: 'monospace', color: rarity.color
            }).setOrigin(0.5);
            this.add.text(x, y + 10, `[${rarity.name}]`, {
                fontSize: '10px', fontFamily: 'monospace', color: rarity.color
            }).setOrigin(0.5);

            card.on('pointerdown', () => {
                this._showEnhanceDetail(item);
            });
        });
    }

    _showEnhanceDetail(item) {
        if (this.detailPopup) this.detailPopup.destroy();

        const cx = 640, cy = 450;
        const popup = this.add.container(cx, cy).setDepth(900);
        this.detailPopup = popup;

        popup.add(this.add.rectangle(0, 0, 450, 220, 0x111100, 0.97).setStrokeStyle(2, 0xffaa44).setInteractive());

        const reg = ItemRegistry.get(item.itemId) || {};
        const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;
        const enhLevel = item.enhanceLevel || 0;
        const preview = BP_ENHANCE.getPreview(item);

        popup.add(this.add.text(0, -90, `${reg.icon || ''} ${reg.name || ''} +${enhLevel}`, {
            fontSize: '16px', fontFamily: 'monospace', color: rarity.color, fontStyle: 'bold'
        }).setOrigin(0.5));

        if (!BP_ENHANCE.canEnhance(item)) {
            popup.add(this.add.text(0, -50, '최대 강화 단계입니다', {
                fontSize: '14px', fontFamily: 'monospace', color: '#886644'
            }).setOrigin(0.5));
        } else if (preview) {
            const statLines = Object.keys(preview.next).map(k =>
                `${k}: ${preview.current[k]} → ${preview.next[k]}`
            ).join('  |  ');
            popup.add(this.add.text(0, -55, statLines, {
                fontSize: '11px', fontFamily: 'monospace', color: '#ccaa66', wordWrap: { width: 400 }
            }).setOrigin(0.5));
            popup.add(this.add.text(0, -25, `비용: 💰${preview.cost}G  |  성공률: ${Math.floor(preview.successRate * 100)}%  |  파괴율: ${Math.floor(preview.breakRate * 100)}%`, {
                fontSize: '12px', fontFamily: 'monospace', color: preview.breakRate > 0 ? '#ff6644' : '#88cc44'
            }).setOrigin(0.5));

            const enhBtn = this.add.rectangle(0, 20, 160, 40, 0x443300).setStrokeStyle(2, 0xffaa44).setInteractive();
            popup.add(enhBtn);
            popup.add(this.add.text(0, 20, '⚒️ 강화!', {
                fontSize: '16px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
            }).setOrigin(0.5));

            enhBtn.on('pointerdown', () => {
                const result = BP_ENHANCE.enhance(item);
                this._showEnhanceResult(result, item);
            });
        }

        const closeBtn = this.add.rectangle(0, 75, 100, 30, 0x332200).setStrokeStyle(1, 0x664400).setInteractive();
        popup.add(closeBtn);
        popup.add(this.add.text(0, 75, '닫기', { fontSize: '12px', fontFamily: 'monospace', color: '#aa8844' }).setOrigin(0.5));
        closeBtn.on('pointerdown', () => { popup.destroy(); this.detailPopup = null; });
    }

    _showEnhanceResult(result, item) {
        if (this.detailPopup) { this.detailPopup.destroy(); this.detailPopup = null; }

        const cx = 640, cy = 400;
        let msg, color;
        if (result.result === 'success') {
            msg = `✅ 강화 성공! +${result.level}`;
            color = '#44ff88';
        } else if (result.result === 'broken') {
            msg = '💥 장비가 파괴되었습니다!';
            color = '#ff2222';
            // Remove from stash
            const stash = StashManager.getStash();
            const idx = stash.findIndex(s => s.id === item.id);
            if (idx >= 0) StashManager.removeFromStash(item.id);
        } else if (result.result === 'fail') {
            msg = '❌ 강화 실패...';
            color = '#ff8844';
        } else {
            msg = result.result;
            color = '#888888';
        }

        const txt = this.add.text(cx, cy, msg, {
            fontSize: '24px', fontFamily: 'monospace', color, fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(950);

        this.tweens.add({
            targets: txt, y: cy - 40, alpha: 0, duration: 1500,
            onComplete: () => { txt.destroy(); this._refresh(); }
        });
    }

    _drawCraftPanel() {
        const cx = 640;
        const stash = StashManager.getStash();
        const equipment = stash.filter(item => {
            const reg = ItemRegistry.get(item.itemId);
            return reg && reg.category === 'equipment' && item.rarity !== 'mythical';
        });

        if (equipment.length < 2) {
            this.add.text(cx, 300, '조합할 장비가 부족합니다 (2개 이상 필요)', {
                fontSize: '14px', fontFamily: 'monospace', color: '#886644'
            }).setOrigin(0.5);
            return;
        }

        this.add.text(cx, 130, '같은 아이템 + 같은 등급 2개를 선택하세요', {
            fontSize: '13px', fontFamily: 'monospace', color: '#aa8844'
        }).setOrigin(0.5);

        this.craftSlot1 = null;
        this.craftSlot2 = null;

        // Slots display
        this.slot1Rect = this.add.rectangle(480, 520, 180, 60, 0x221100).setStrokeStyle(2, 0x664400);
        this.slot1Text = this.add.text(480, 520, '슬롯 1: 비어있음', {
            fontSize: '12px', fontFamily: 'monospace', color: '#664400'
        }).setOrigin(0.5);

        this.add.text(640, 520, '＋', { fontSize: '24px', fontFamily: 'monospace', color: '#ffaa44' }).setOrigin(0.5);

        this.slot2Rect = this.add.rectangle(800, 520, 180, 60, 0x221100).setStrokeStyle(2, 0x664400);
        this.slot2Text = this.add.text(800, 520, '슬롯 2: 비어있음', {
            fontSize: '12px', fontFamily: 'monospace', color: '#664400'
        }).setOrigin(0.5);

        this.craftResultText = this.add.text(640, 570, '', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffaa44'
        }).setOrigin(0.5);

        // Item grid
        const cols = 5, cellW = 220, cellH = 50, gap = 6;
        const totalW = cols * cellW + (cols - 1) * gap;
        const startX = cx - totalW / 2 + cellW / 2;

        equipment.forEach((item, i) => {
            const col = i % cols;
            const row = Math.floor(i / cols);
            const x = startX + col * (cellW + gap);
            const y = 170 + row * (cellH + gap);
            const reg = ItemRegistry.get(item.itemId) || {};
            const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;

            const card = this.add.rectangle(x, y, cellW, cellH, 0x221100)
                .setStrokeStyle(2, rarity.borderColor).setInteractive();
            this.add.text(x, y, `${reg.icon || ''} ${reg.name || '???'} [${rarity.name}]`, {
                fontSize: '11px', fontFamily: 'monospace', color: rarity.color
            }).setOrigin(0.5);

            card.on('pointerdown', () => {
                this._selectCraftItem(item);
            });
        });
    }

    _selectCraftItem(item) {
        const reg = ItemRegistry.get(item.itemId) || {};
        const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;
        const label = `${reg.icon || ''} ${reg.name || '???'}`;

        if (!this.craftSlot1) {
            this.craftSlot1 = item;
            this.slot1Text.setText(label);
            this.slot1Rect.setStrokeStyle(2, rarity.borderColor);
        } else if (!this.craftSlot2) {
            if (item.id === this.craftSlot1.id) return;
            this.craftSlot2 = item;
            this.slot2Text.setText(label);
            this.slot2Rect.setStrokeStyle(2, rarity.borderColor);
            this._tryShowCraftPreview();
        } else {
            this.craftSlot1 = item;
            this.craftSlot2 = null;
            this.slot1Text.setText(label);
            this.slot1Rect.setStrokeStyle(2, rarity.borderColor);
            this.slot2Text.setText('슬롯 2: 비어있음');
            this.slot2Rect.setStrokeStyle(2, 0x664400);
            this.craftResultText.setText('');
        }
    }

    _tryShowCraftPreview() {
        const check = BP_CRAFT.canCombine(this.craftSlot1, this.craftSlot2);
        if (!check.can) {
            const reasons = {
                different_items: '같은 아이템이 아닙니다',
                different_rarity: '같은 등급이 아닙니다',
                max_rarity: '신화는 조합 불가',
                missing_item: '아이템을 선택하세요'
            };
            this.craftResultText.setText(`❌ ${reasons[check.reason] || '조합 불가'}`).setColor('#ff4444');
            return;
        }

        const preview = BP_CRAFT.getPreview(this.craftSlot1, this.craftSlot2);
        const nextRarity = FARMING.RARITIES[preview.rarity];
        this.craftResultText.setText(`→ ${preview.name} [${nextRarity.name}]  💰${preview.cost}G`).setColor(nextRarity.color);

        // Craft button
        if (this.craftBtn) this.craftBtn.destroy();
        if (this.craftBtnText) this.craftBtnText.destroy();
        this.craftBtn = this.add.rectangle(640, 610, 160, 40, 0x443300).setStrokeStyle(2, 0xffaa44).setInteractive();
        this.craftBtnText = this.add.text(640, 610, '🔮 조합!', {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.craftBtn.on('pointerdown', () => {
            const result = BP_CRAFT.combine(this.craftSlot1, this.craftSlot2);
            if (result.result === 'success') {
                StashManager.removeFromStash(this.craftSlot1.id);
                StashManager.removeFromStash(this.craftSlot2.id);
                StashManager.addToStash(result.newItem);
                const nr = FARMING.RARITIES[result.newItem.rarity];
                const resultReg = ItemRegistry.get(result.newItem.itemId) || result.newItem;
                const msg = this.add.text(640, 500, `✨ ${resultReg.name} [${nr.name}] 조합 성공!`, {
                    fontSize: '20px', fontFamily: 'monospace', color: nr.color, fontStyle: 'bold',
                    stroke: '#000000', strokeThickness: 4
                }).setOrigin(0.5).setDepth(950);
                this.tweens.add({
                    targets: msg, y: 460, alpha: 0, duration: 2000,
                    onComplete: () => { msg.destroy(); this._refresh(); }
                });
            } else {
                this.craftResultText.setText(`❌ ${result.reason || '조합 실패'}`).setColor('#ff4444');
            }
        });
    }

    _drawContinueButton() {
        const btn = this.add.rectangle(640, 680, 250, 45, 0x443300)
            .setStrokeStyle(3, 0xffaa44).setInteractive();
        this.add.text(640, 680, '▶ 계속', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);

        btn.on('pointerover', () => btn.setFillStyle(0x664400));
        btn.on('pointerout', () => btn.setFillStyle(0x443300));
        btn.on('pointerdown', () => {
            FarmingUI.hideTooltip();
            const nextRound = this.runManager.advanceRound();
            if (!nextRound) {
                this.scene.start('BPResultScene', {
                    victory: true, dangerLevel: this.dangerSystem.level,
                    drops: this.dropSystem.collectedDrops, allies: this.prevAllies,
                    party: this.partyKeys, time: this.battleTime
                });
                return;
            }
            const data = {
                party: this.partyKeys, dangerSystem: this.dangerSystem,
                dropSystem: this.dropSystem, runManager: this.runManager,
                appliedDrops: this.appliedDrops, allies: this.prevAllies, time: this.battleTime
            };
            const sceneMap = { battle: 'BPBattleScene', boss: 'BPBattleScene', shop: 'BPShopScene', forge: 'BPForgeScene', event: 'BPEventScene' };
            this.scene.start(sceneMap[nextRound.type] || 'BPBattleScene', data);
        });
    }

    _refresh() {
        FarmingUI.hideTooltip();
        this.scene.restart({
            party: this.partyKeys, dangerSystem: this.dangerSystem,
            dropSystem: this.dropSystem, runManager: this.runManager,
            appliedDrops: this.appliedDrops, allies: this.prevAllies, time: this.battleTime
        });
    }
}
