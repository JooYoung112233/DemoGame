class BPChoiceScene extends Phaser.Scene {
    constructor() { super('BPChoiceScene'); }

    init(data) {
        this.partyKeys = data.party;
        this.dangerSystem = data.dangerSystem;
        this.dropSystem = data.dropSystem;
        this.appliedDrops = data.appliedDrops || [];
        this.prevAllies = data.allies || [];
        this.battleTime = data.time || 0;
    }

    create() {
        this.cameras.main.setBackgroundColor('#1a0a0a');
        const cx = 640;
        this.actionPopup = null;
        this.dismantleResultPopup = null;

        const nextLevel = this.dangerSystem.level + 1;
        const isBoss = nextLevel >= this.dangerSystem.maxLevel;

        this.add.text(cx, 40, isBoss ? '⚠️ 다음은 보스 라운드!' : `다음 위험도: ${nextLevel}`, {
            fontSize: '28px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
        }).setOrigin(0.5);

        const raidInv = BP_FARMING.getRaidInventory();
        const hasKit = BP_FARMING.hasEscapeKit();
        this.add.text(cx, 75, `인게임 보상: ${this.dropSystem.getCollectedCount()}개  |  파밍: ${raidInv.length}개  |  전멸 시 장비+절반 손실`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#aa6666'
        }).setOrigin(0.5);

        const scale = (1 + nextLevel * 0.15).toFixed(2);
        const rareChance = Math.min(10 + nextLevel * 4, 40);
        this.add.text(cx, 100, `적 강도: ×${scale}  |  희귀+ 드랍: ${rareChance}%  |  탈출킷: ${hasKit ? '✅' : '❌'}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#886666'
        }).setOrigin(0.5);

        // 계속 전투 버튼
        const continueBtn = this.add.rectangle(cx - 200, 185, 310, 90, 0x882222).setStrokeStyle(3, 0xff4444).setInteractive();
        this.add.text(cx - 200, 165, '⚔️ 계속 전투', {
            fontSize: '24px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.add.text(cx - 200, 193, '위험도 상승 + 더 좋은 드랍', { fontSize: '12px', fontFamily: 'monospace', color: '#ff8888' }).setOrigin(0.5);
        this.add.text(cx - 200, 211, '탈출킷도 드랍될 수 있음', { fontSize: '11px', fontFamily: 'monospace', color: '#ffaa44' }).setOrigin(0.5);

        continueBtn.on('pointerover', () => continueBtn.setFillStyle(0xaa3333));
        continueBtn.on('pointerout', () => continueBtn.setFillStyle(0x882222));
        continueBtn.on('pointerdown', () => {
            FarmingUI.hideTooltip();
            this.dangerSystem.addKill();
            this.scene.start('BPBattleScene', {
                party: this.partyKeys,
                dangerSystem: this.dangerSystem,
                dropSystem: this.dropSystem,
                appliedDrops: this.appliedDrops
            });
        });

        // 탈출 버튼
        const escapeBtnColor = hasKit ? 0x224422 : 0x222222;
        const escapeBorderColor = hasKit ? 0x44ff44 : 0x444444;
        const escapeBtn = this.add.rectangle(cx + 200, 185, 310, 90, escapeBtnColor).setStrokeStyle(3, escapeBorderColor).setInteractive();
        this.add.text(cx + 200, 162, '🚪 탈출', {
            fontSize: '24px', fontFamily: 'monospace', color: hasKit ? '#ffffff' : '#666666', fontStyle: 'bold'
        }).setOrigin(0.5);

        if (hasKit) {
            this.add.text(cx + 200, 190, '탈출킷 사용하여 탈출', { fontSize: '12px', fontFamily: 'monospace', color: '#88ff88' }).setOrigin(0.5);
            this.add.text(cx + 200, 208, '보상 + 장비 전부 보존', { fontSize: '11px', fontFamily: 'monospace', color: '#66cc66' }).setOrigin(0.5);
        } else {
            this.add.text(cx + 200, 190, '탈출킷이 없습니다!', { fontSize: '13px', fontFamily: 'monospace', color: '#ff6644' }).setOrigin(0.5);
            this.add.text(cx + 200, 208, '아이템 분해 시 확률로 획득', { fontSize: '10px', fontFamily: 'monospace', color: '#886666' }).setOrigin(0.5);
        }

        escapeBtn.on('pointerover', () => escapeBtn.setFillStyle(hasKit ? 0x335533 : 0x2a2a2a));
        escapeBtn.on('pointerout', () => escapeBtn.setFillStyle(escapeBtnColor));
        escapeBtn.on('pointerdown', () => {
            if (!BP_FARMING.hasEscapeKit()) return;
            FarmingUI.hideTooltip();
            BP_FARMING.consumeEscapeKit();
            this.scene.start('BPResultScene', {
                victory: true,
                escaped: true,
                dangerLevel: this.dangerSystem.level,
                drops: this.dropSystem.collectedDrops,
                allies: this.prevAllies,
                party: this.partyKeys,
                time: this.battleTime
            });
        });

        // 보안 컨테이너
        this.secureUI = FarmingUI.drawSecureContainer(this, cx, 295, raidInv, (slotIdx, existingSlot) => {
            if (existingSlot) {
                BP_FARMING.removeFromSecure(slotIdx);
                this.fullRefresh();
            }
        });

        // 파밍 인벤토리
        this.hintText = null;
        this.raidInvUI = null;
        if (raidInv.length > 0) {
            this.hintText = this.add.text(cx, 348, '아이템 클릭 → 🔒보안 이동 / 🔨분해(골드+탈출킷 확률)', {
                fontSize: '10px', fontFamily: 'monospace', color: '#888844'
            }).setOrigin(0.5);

            this.raidInvUI = FarmingUI.drawRaidInventory(this, cx, 400, raidInv, (item, idx) => {
                FarmingUI.hideTooltip();
                this.showItemActionPopup(item, idx);
            });
        }

        // 기존 인게임 드랍
        if (this.dropSystem.collectedDrops.length > 0) {
            this.add.text(cx, 520, '── 인게임 보상 ──', {
                fontSize: '12px', fontFamily: 'monospace', color: '#664444'
            }).setOrigin(0.5);
            const names = this.dropSystem.collectedDrops.map(d => d.name);
            this.add.text(cx, 542, names.join('  |  '), {
                fontSize: '11px', fontFamily: 'monospace', color: '#886666',
                wordWrap: { width: 1000 }, align: 'center'
            }).setOrigin(0.5);
        }

        // 시너지
        const syns = this.dropSystem.getSynergyInfo();
        if (syns.length > 0) {
            this.add.text(cx, 580, '── 활성 시너지 ──', {
                fontSize: '12px', fontFamily: 'monospace', color: '#664444'
            }).setOrigin(0.5);
            syns.forEach((s, i) => {
                this.add.text(cx, 600 + i * 18, `${s.name} (${s.count}개)`, {
                    fontSize: '12px', fontFamily: 'monospace', color: s.color
                }).setOrigin(0.5);
            });
        }
    }

    showItemActionPopup(item, idx) {
        this.closeActionPopup();
        const cx = 640;
        const regItem = ItemRegistry.get(item.itemId) || item;
        const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;
        const value = ItemRegistry.getValue(item);
        const kitChance = BP_FARMING._getEscapeKitChance(item);

        const popup = this.add.container(cx, 360).setDepth(900);
        this.actionPopup = popup;

        // 배경
        popup.add(this.add.rectangle(0, 0, 400, 180, 0x111111, 0.95).setStrokeStyle(2, rarity.borderColor).setInteractive());

        // 아이템 정보
        popup.add(this.add.text(0, -70, `${regItem.icon||''} ${regItem.name} [${rarity.name}]`, {
            fontSize: '14px', fontFamily: 'monospace', color: rarity.color, fontStyle: 'bold'
        }).setOrigin(0.5));

        // 보안 이동 버튼
        const secureSlots = StashManager.getSecureContainer();
        const hasEmptySecure = secureSlots.some(s => s === null);
        const secureBtnColor = hasEmptySecure ? 0x1a1a00 : 0x222222;

        const secureBtn = this.add.rectangle(-100, -15, 170, 50, secureBtnColor).setStrokeStyle(2, hasEmptySecure ? 0xffaa00 : 0x444444).setInteractive();
        popup.add(secureBtn);
        popup.add(this.add.text(-100, -22, '🔒 보안 이동', {
            fontSize: '13px', fontFamily: 'monospace', color: hasEmptySecure ? '#ffaa00' : '#666666', fontStyle: 'bold'
        }).setOrigin(0.5));
        popup.add(this.add.text(-100, -3, hasEmptySecure ? '사망해도 보존' : '보안칸 꽉 참', {
            fontSize: '10px', fontFamily: 'monospace', color: hasEmptySecure ? '#aa8800' : '#444444'
        }).setOrigin(0.5));

        if (hasEmptySecure) {
            secureBtn.on('pointerover', () => secureBtn.setFillStyle(0x2a2a00));
            secureBtn.on('pointerout', () => secureBtn.setFillStyle(0x1a1a00));
            secureBtn.on('pointerdown', () => {
                BP_FARMING.moveToSecure(idx);
                this.closeActionPopup();
                this.fullRefresh();
            });
        }

        // 분해 버튼
        const dismantleBtn = this.add.rectangle(100, -15, 170, 50, 0x1a0a0a).setStrokeStyle(2, 0xff6644).setInteractive();
        popup.add(dismantleBtn);
        popup.add(this.add.text(100, -25, '🔨 분해', {
            fontSize: '13px', fontFamily: 'monospace', color: '#ff6644', fontStyle: 'bold'
        }).setOrigin(0.5));
        popup.add(this.add.text(100, -5, `💰${value}G + 탈출킷 ${Math.floor(kitChance*100)}%`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#cc8844'
        }).setOrigin(0.5));

        dismantleBtn.on('pointerover', () => dismantleBtn.setFillStyle(0x2a1515));
        dismantleBtn.on('pointerout', () => dismantleBtn.setFillStyle(0x1a0a0a));
        dismantleBtn.on('pointerdown', () => {
            const result = BP_FARMING.dismantleRaidItem(idx);
            this.closeActionPopup();
            if (result) {
                this.showDismantleResult(result);
            }
        });

        // 취소 버튼
        const cancelBtn = this.add.rectangle(0, 55, 120, 35, 0x332222).setStrokeStyle(1, 0x664444).setInteractive();
        popup.add(cancelBtn);
        popup.add(this.add.text(0, 55, '취소', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa8888'
        }).setOrigin(0.5));
        cancelBtn.on('pointerdown', () => this.closeActionPopup());
    }

    showDismantleResult(result) {
        this.closeDismantleResult();
        const cx = 640;
        const popup = this.add.container(cx, 360).setDepth(950).setAlpha(0).setScale(0.8);
        this.dismantleResultPopup = popup;

        const regItem = ItemRegistry.get(result.dismantled.itemId) || result.dismantled;
        const h = result.gotKit ? 130 : 100;
        popup.add(this.add.rectangle(0, 0, 350, h, 0x111111, 0.95).setStrokeStyle(2, result.gotKit ? 0x44ff88 : 0xff6644));

        popup.add(this.add.text(0, -h/2 + 20, `🔨 ${regItem.name} 분해!`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff6644', fontStyle: 'bold'
        }).setOrigin(0.5));

        popup.add(this.add.text(0, -h/2 + 45, `💰 +${result.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5));

        if (result.gotKit) {
            popup.add(this.add.text(0, -h/2 + 72, '🚪 탈출킷 획득!', {
                fontSize: '18px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
            }).setOrigin(0.5));
            popup.add(this.add.text(0, -h/2 + 95, '이제 탈출할 수 있습니다!', {
                fontSize: '11px', fontFamily: 'monospace', color: '#88cc88'
            }).setOrigin(0.5));
        } else {
            popup.add(this.add.text(0, -h/2 + 72, '탈출킷은 나오지 않았습니다...', {
                fontSize: '12px', fontFamily: 'monospace', color: '#886666'
            }).setOrigin(0.5));
        }

        this.tweens.add({
            targets: popup, alpha: 1, scale: 1, duration: 300, ease: 'Back.easeOut'
        });

        this.time.delayedCall(result.gotKit ? 2000 : 1500, () => {
            this.closeDismantleResult();
            this.fullRefresh();
        });
    }

    closeActionPopup() {
        if (this.actionPopup) {
            this.actionPopup.destroy();
            this.actionPopup = null;
        }
    }

    closeDismantleResult() {
        if (this.dismantleResultPopup) {
            this.dismantleResultPopup.destroy();
            this.dismantleResultPopup = null;
        }
    }

    fullRefresh() {
        // 씬 전체 재생성으로 탈출킷 상태 등 반영
        FarmingUI.hideTooltip();
        this.scene.restart({
            party: this.partyKeys,
            dangerSystem: this.dangerSystem,
            dropSystem: this.dropSystem,
            appliedDrops: this.appliedDrops,
            allies: this.prevAllies,
            time: this.battleTime
        });
    }
}
