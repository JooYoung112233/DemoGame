class BPDropScene extends Phaser.Scene {
    constructor() { super('BPDropScene'); }

    init(data) {
        this.partyIds = data.party;
        this.dangerSystem = data.dangerSystem;
        this.dropSystem = data.dropSystem;
        this.runManager = data.runManager;
        this.appliedDrops = data.appliedDrops || [];
        this.prevAllies = data.allies || [];
        this.battleTime = data.time || 0;
        this._picked = false;
    }

    create() {
        this.cameras.main.setBackgroundColor('#1a0a0a');
        this.cameras.main.fadeIn(300);
        this._drawAll();
    }

    _drawAll() {
        this.children.removeAll();
        const cx = 640;
        const clearedLevel = this.dangerSystem.level;
        const round = this.runManager ? this.runManager.getCurrentRound() : null;
        const roundNum = round ? round.round : clearedLevel;

        this.add.text(cx, 22, `라운드 ${roundNum} 클리어!`, {
            fontSize: '24px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
        }).setOrigin(0.5);

        const survivorCount = this.prevAllies.filter(a => a.alive).length;
        this.add.text(cx, 50, `생존: ${survivorCount}/${this.prevAllies.length}  |  💰 ${StashManager.getGold()}G`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa6666'
        }).setOrigin(0.5);

        if (this.runManager) {
            const preview = this.runManager.getRoundPreview(3);
            if (preview.length > 0) {
                const previewStr = preview.map(r => `${this.runManager.getRoundIcon(r.type)}${this.runManager.getRoundLabel(r.type)}`).join('  ');
                this.add.text(cx, 68, `다음: ${previewStr}`, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#666688'
                }).setOrigin(0.5);
            }
        }

        // drop card selection (only if not picked yet)
        if (!this._picked) {
            this.add.text(cx, 90, '보상을 선택하세요', {
                fontSize: '14px', fontFamily: 'monospace', color: '#cc8888'
            }).setOrigin(0.5);

            const drops = this._drops || (this._drops = this.dropSystem.generateRoundDrops(clearedLevel));
            const cardWidth = 280, cardHeight = 200, gap = 30;
            const totalWidth = 3 * cardWidth + 2 * gap;
            const startX = cx - totalWidth / 2 + cardWidth / 2;

            drops.forEach((drop, i) => {
                const x = startX + i * (cardWidth + gap);
                const y = 220;
                const rarity = BP_RARITIES[drop.rarity];

                const card = this.add.rectangle(x, y, cardWidth, cardHeight, 0x221111)
                    .setStrokeStyle(3, rarity.borderColor).setInteractive();
                this.add.text(x, y - 78, `[${rarity.name}]`, {
                    fontSize: '11px', fontFamily: 'monospace', color: rarity.color
                }).setOrigin(0.5);
                this.add.text(x, y - 58, drop.name, {
                    fontSize: '16px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
                }).setOrigin(0.5);
                const typeLabel = { weapon: '⚔️ 무기', passive: '🔮 패시브', consumable: '🧪 소비' }[drop.type];
                this.add.text(x, y - 38, typeLabel, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#888888'
                }).setOrigin(0.5);
                this.add.text(x, y - 5, drop.desc, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#cccccc',
                    wordWrap: { width: cardWidth - 30 }, align: 'center'
                }).setOrigin(0.5);

                if (drop.tag) {
                    const tagColor = { bleed: '#ff4444', dodge: '#44ffff', corpse: '#ff8844' }[drop.tag] || '#888888';
                    this.add.text(x, y + 28, `[${drop.tag} 빌드]`, {
                        fontSize: '10px', fontFamily: 'monospace', color: tagColor
                    }).setOrigin(0.5);
                }

                const selectBtn = this.add.rectangle(x, y + 65, 130, 32, rarity.borderColor, 0.8).setInteractive();
                this.add.text(x, y + 65, '선택', {
                    fontSize: '14px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
                }).setOrigin(0.5);

                card.on('pointerover', () => card.setFillStyle(0x331818));
                card.on('pointerout', () => card.setFillStyle(0x221111));

                const doSelect = () => {
                    this.appliedDrops.push(drop);
                    this.dropSystem.collectedDrops.push(drop);
                    if (drop.type === 'passive') this.dropSystem.collectedPassiveIds.push(drop.id);
                    this._picked = true;
                    this._drawAll();
                };
                card.on('pointerdown', doSelect);
                selectBtn.on('pointerdown', doSelect);
            });
        } else {
            // after picking, show "continue" button
            this.add.text(cx, 90, '보상 선택 완료', {
                fontSize: '14px', fontFamily: 'monospace', color: '#44ff88'
            }).setOrigin(0.5);

            const contBtn = this.add.rectangle(cx, 200, 220, 45, 0x882222).setStrokeStyle(2, 0xff4444).setInteractive();
            this.add.text(cx, 200, '▶ 다음 라운드', {
                fontSize: '16px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            }).setOrigin(0.5);
            contBtn.on('pointerover', () => contBtn.setFillStyle(0xaa3333));
            contBtn.on('pointerout', () => contBtn.setFillStyle(0x882222));
            contBtn.on('pointerdown', () => this._goNext());
        }

        // synergies
        const syns = this.dropSystem.getSynergyInfo();
        if (syns.length > 0) {
            const synLabels = syns.map(s => `${s.name}(${s.count})`).join('  |  ');
            this.add.text(cx, 340, synLabels, {
                fontSize: '11px', fontFamily: 'monospace', color: '#ffaa44'
            }).setOrigin(0.5);
        }

        // ── Raid Inventory & Secure Container ──
        this._drawRaidInventory(cx);

        // ── Escape ──
        if (this.runManager && this.dangerSystem.level < 20) {
            this._drawEscapeButton(cx);
        }
    }

    _drawRaidInventory(cx) {
        const raidInv = BP_FARMING.getRaidInventory();
        const secure = StashManager.getSecureContainer();
        const baseY = 380;

        this.add.text(200, baseY, `🎒 레이드 인벤토리 (${raidInv.length})`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa8866'
        });

        if (raidInv.length === 0) {
            this.add.text(200, baseY + 22, '비어있음', {
                fontSize: '10px', fontFamily: 'monospace', color: '#555555'
            });
        } else {
            raidInv.forEach((item, i) => {
                if (i >= 8) return;
                const x = 200 + (i % 4) * 140;
                const y = baseY + 22 + Math.floor(i / 4) * 28;
                const reg = ItemRegistry.get(item.itemId);
                const name = reg ? reg.name : item.itemId;
                const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;

                const itemText = this.add.text(x, y, `${reg ? reg.icon || '' : ''} ${name}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: rarity.color
                }).setInteractive();

                itemText.on('pointerdown', () => {
                    if (BP_FARMING.moveToSecure(i)) {
                        this._drawAll();
                    }
                });
                itemText.on('pointerover', () => itemText.setColor('#ffffff'));
                itemText.on('pointerout', () => itemText.setColor(rarity.color));
            });
        }

        // secure container
        this.add.text(830, baseY, `🔒 보안 컨테이너 (${secure.filter(s => s).length}/3)`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#44aaff'
        });
        this.add.text(830, baseY + 16, '사망/탈출실패 시 보존됨', {
            fontSize: '9px', fontFamily: 'monospace', color: '#336688'
        });

        for (let i = 0; i < FARMING.SECURE_CONTAINER_SIZE; i++) {
            const sx = 830 + i * 140;
            const sy = baseY + 38;
            const slot = secure[i];

            const bg = this.add.rectangle(sx + 55, sy, 120, 26, slot ? 0x112244 : 0x111122)
                .setStrokeStyle(1, slot ? 0x4488ff : 0x333366).setInteractive();

            if (slot) {
                const reg = ItemRegistry.get(slot.itemId);
                const name = reg ? reg.name : slot.itemId;
                const rarity = FARMING.RARITIES[slot.rarity] || FARMING.RARITIES.common;
                this.add.text(sx + 55, sy, `${reg ? reg.icon || '' : ''} ${name}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: rarity.color
                }).setOrigin(0.5);

                bg.on('pointerdown', () => {
                    BP_FARMING.removeFromSecure(i);
                    this._drawAll();
                });
            } else {
                this.add.text(sx + 55, sy, '빈 슬롯', {
                    fontSize: '10px', fontFamily: 'monospace', color: '#333366'
                }).setOrigin(0.5);
            }
        }
    }

    _drawEscapeButton(cx) {
        const y = 480;
        const kitItem = this._findEscapeKit();
        const hasKit = !!kitItem;

        let escLabel, successRate;
        if (hasKit) {
            const rIdx = FARMING.RARITY_ORDER.indexOf(kitItem.rarity || 'uncommon');
            successRate = [0.50, 0.65, 0.80, 0.92, 1.0, 1.0, 1.0][rIdx] || 0.50;
            escLabel = `🚪 탈출킷 사용 (성공률 ${Math.floor(successRate * 100)}%)`;
        } else {
            successRate = 0;
            escLabel = '🚪 탈출 (탈출킷 없음: 보상 절반 손실)';
        }

        const escColor = hasKit ? 0x224422 : 0x222222;
        const escBorder = hasKit ? 0x44aa44 : 0x444444;
        const escBtn = this.add.rectangle(cx, y, 320, 38, escColor)
            .setStrokeStyle(2, escBorder).setInteractive();
        this.add.text(cx, y, escLabel, {
            fontSize: '12px', fontFamily: 'monospace', color: hasKit ? '#44ff88' : '#888888'
        }).setOrigin(0.5);

        escBtn.on('pointerover', () => escBtn.setFillStyle(hasKit ? 0x335533 : 0x333333));
        escBtn.on('pointerout', () => escBtn.setFillStyle(escColor));
        escBtn.on('pointerdown', () => this._escape(hasKit, successRate));

        this.add.text(cx, y + 28, '🔒 보안 컨테이너 아이템은 항상 보존됩니다', {
            fontSize: '9px', fontFamily: 'monospace', color: '#336688'
        }).setOrigin(0.5);
    }

    _findEscapeKit() {
        const raidInv = BP_FARMING.getRaidInventory();
        return raidInv.find(item => item.itemId === 'escape_kit') || null;
    }

    _escape(hasKit, successRate) {
        if (hasKit) {
            BP_FARMING.consumeEscapeKit();
        }

        // secure container items always saved
        const secureItems = StashManager.moveSecureToStash();

        const raidInv = BP_FARMING.getRaidInventory();

        if (hasKit && Math.random() < successRate) {
            // success: keep all raid inventory
            raidInv.forEach(item => StashManager.addToStash(item));
            BP_FARMING.raidInventory = [];
            StashManager.recordRaid({ extracted: true, totalValue: StashManager.getGold() });
            this.scene.start('BPResultScene', {
                victory: false, escaped: true,
                dangerLevel: this.dangerSystem.level,
                drops: this.dropSystem.collectedDrops,
                allies: this.prevAllies, party: this.partyIds, time: this.battleTime
            });
        } else if (hasKit) {
            // kit used but failed: lose 30% of raid items
            const loseCount = Math.ceil(raidInv.length * 0.3);
            const shuffled = [...raidInv].sort(() => Math.random() - 0.5);
            const lost = shuffled.slice(0, loseCount);
            const kept = shuffled.slice(loseCount);
            kept.forEach(item => StashManager.addToStash(item));
            BP_FARMING.raidInventory = [];
            StashManager.recordRaid({ extracted: true, totalValue: StashManager.getGold() });

            this._showEscapeResult(false, lost.length);
        } else {
            // no kit: lose half
            const halfIdx = Math.ceil(raidInv.length / 2);
            raidInv.slice(0, halfIdx).forEach(item => StashManager.addToStash(item));
            BP_FARMING.raidInventory = [];
            StashManager.recordRaid({ extracted: true, totalValue: StashManager.getGold() });
            this.scene.start('BPResultScene', {
                victory: false, escaped: true,
                dangerLevel: this.dangerSystem.level,
                drops: this.dropSystem.collectedDrops,
                allies: this.prevAllies, party: this.partyIds, time: this.battleTime
            });
        }
    }

    _showEscapeResult(success, lostCount) {
        const msg = `⚠️ 탈출킷 실패! 아이템 ${lostCount}개 손실...`;
        const txt = this.add.text(640, 360, msg, {
            fontSize: '20px', fontFamily: 'monospace', color: '#ff6644', fontStyle: 'bold',
            stroke: '#000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(3000);
        this.tweens.add({
            targets: txt, y: 330, alpha: 0, duration: 2500,
            onComplete: () => {
                txt.destroy();
                this.scene.start('BPResultScene', {
                    victory: false, escaped: true,
                    dangerLevel: this.dangerSystem.level,
                    drops: this.dropSystem.collectedDrops,
                    allies: this.prevAllies, party: this.partyIds, time: this.battleTime
                });
            }
        });
    }

    _goNext() {
        if (!this.runManager) {
            this.scene.start('BPChoiceScene', {
                party: this.partyIds, dangerSystem: this.dangerSystem,
                dropSystem: this.dropSystem, appliedDrops: this.appliedDrops,
                allies: this.prevAllies, time: this.battleTime
            });
            return;
        }

        const nextRound = this.runManager.advanceRound();
        if (!nextRound) {
            BP_FARMING.getRaidInventory().forEach(item => StashManager.addToStash(item));
            BP_FARMING.raidInventory = [];
            StashManager.moveSecureToStash();
            StashManager.recordRaid({ extracted: true, totalValue: StashManager.getGold() });
            this.scene.start('BPResultScene', {
                victory: true,
                dangerLevel: this.dangerSystem.level,
                drops: this.dropSystem.collectedDrops,
                allies: this.prevAllies,
                party: this.partyIds,
                time: this.battleTime
            });
            return;
        }

        this.dangerSystem.level = nextRound.dangerLevel;

        const data = {
            party: this.partyIds,
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
        this.scene.start(sceneMap[nextRound.type] || 'BPBattleScene', data);
    }
}
