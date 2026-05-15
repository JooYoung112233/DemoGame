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
        this._drops = null;
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
            const cardWidth = 280, cardHeight = 210, gap = 30;
            const totalWidth = 3 * cardWidth + 2 * gap;
            const startX = cx - totalWidth / 2 + cardWidth / 2;

            drops.forEach((drop, i) => {
                const x = startX + i * (cardWidth + gap);
                const y = 220;

                const { container, bg, selectBtn } = IconRenderer.drawItemCard(this, x, y, drop, cardWidth, cardHeight);

                const doSelect = () => {
                    this.appliedDrops.push(drop);
                    this.dropSystem.collectedDrops.push(drop);
                    if (drop.type === 'passive') this.dropSystem.collectedPassiveIds.push(drop.id);
                    this._picked = true;
                    this._drawAll();
                };
                bg.on('pointerdown', doSelect);
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

        // ── Raid Inventory & Secure Container & Escape ──
        this._drawRaidInventory(cx);
    }

    _drawRaidInventory(cx) {
        const raidInv = BP_FARMING.getRaidInventory();
        const secure = StashManager.getSecureContainer();
        const baseY = 350;
        const panelH = 260;

        // ── 하단 패널 배경 ──
        this.add.rectangle(cx, baseY + panelH / 2, 1240, panelH, 0x0d0505)
            .setStrokeStyle(1, 0x331111);

        // ══════ 좌측: 레이드 인벤토리 ══════
        const invX = 40;
        const invW = 520;
        const invContentH = panelH - 50;

        this.add.text(invX + invW / 2, baseY + 10, `🎒 레이드 인벤토리 (${raidInv.length})`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#aa8866', fontStyle: 'bold'
        }).setOrigin(0.5);

        // 스크롤 가능한 마스크 영역
        const maskY = baseY + 32;
        const maskH = invContentH - 10;

        // 인벤토리 컨테이너 (스크롤용)
        const invContainer = this.add.container(0, 0);

        if (raidInv.length === 0) {
            invContainer.add(this.add.text(invX + invW / 2, maskY + 30, '비어있음', {
                fontSize: '12px', fontFamily: 'monospace', color: '#444444'
            }).setOrigin(0.5));
        } else {
            const itemH = 34;
            const itemW = invW - 10;

            raidInv.forEach((item, i) => {
                const iy = maskY + i * (itemH + 3);
                const reg = ItemRegistry.get(item.itemId);
                const name = reg ? reg.name : item.itemId;
                const rarity = FARMING.RARITIES[item.rarity] || FARMING.RARITIES.common;
                const enhLabel = item.enhanceLevel ? `+${item.enhanceLevel}` : '';

                const row = this.add.rectangle(invX + invW / 2, iy + itemH / 2, itemW, itemH, 0x150808)
                    .setStrokeStyle(1, rarity.borderColor, 0.4).setInteractive();
                invContainer.add(row);

                // 아이콘
                const iconGfx = this.add.graphics();
                if (reg && reg.category === 'equipment') {
                    IconRenderer.drawWeaponIcon(iconGfx, item.itemId, invX + 22, iy + itemH / 2, 18);
                } else {
                    IconRenderer.drawConsumableIcon(iconGfx, item.itemId, invX + 22, iy + itemH / 2, 18);
                }
                invContainer.add(iconGfx);

                // 이름 + 등급
                invContainer.add(this.add.text(invX + 42, iy + itemH / 2, `${name}${enhLabel}  [${rarity.name}]`, {
                    fontSize: '12px', fontFamily: 'monospace', color: rarity.color
                }).setOrigin(0, 0.5));

                // 보안 이동 버튼
                const secBtn = this.add.text(invX + itemW - 10, iy + itemH / 2, '🔒이동', {
                    fontSize: '10px', fontFamily: 'monospace', color: '#4488ff',
                    backgroundColor: '#111133', padding: { x: 4, y: 2 }
                }).setOrigin(1, 0.5).setInteractive();
                invContainer.add(secBtn);

                // 호버 툴팁
                row.on('pointerover', () => {
                    row.setFillStyle(0x221212);
                    this._showItemTooltip(item, reg, rarity, invX + invW / 2, iy - 5);
                });
                row.on('pointerout', () => {
                    row.setFillStyle(0x150808);
                    this._hideItemTooltip();
                });

                secBtn.on('pointerover', () => secBtn.setColor('#88ccff'));
                secBtn.on('pointerout', () => secBtn.setColor('#4488ff'));
                secBtn.on('pointerdown', () => {
                    this._hideItemTooltip();
                    if (BP_FARMING.moveToSecure(i)) {
                        this._drawAll();
                    }
                });
            });

            // 스크롤 처리
            const totalContentH = raidInv.length * 37;
            if (totalContentH > maskH) {
                const maskShape = this.make.graphics();
                maskShape.fillRect(invX, maskY, invW, maskH);
                const mask = maskShape.createGeometryMask();
                invContainer.setMask(mask);

                // 스크롤바 배경
                const sbX = invX + invW + 4;
                this.add.rectangle(sbX, maskY + maskH / 2, 6, maskH, 0x221111).setStrokeStyle(1, 0x331111);

                // 스크롤바 핸들
                const handleH = Math.max(30, (maskH / totalContentH) * maskH);
                const handle = this.add.rectangle(sbX, maskY + handleH / 2, 6, handleH, 0x664444)
                    .setInteractive();
                this._invScrollHandle = handle;
                this._invScrollMax = totalContentH - maskH;

                // 마우스 휠 스크롤
                this.input.on('wheel', (pointer, gameObjects, dx, dy) => {
                    if (pointer.x >= invX && pointer.x <= invX + invW + 20 &&
                        pointer.y >= maskY && pointer.y <= maskY + maskH) {
                        const scrollAmount = dy > 0 ? 37 : -37;
                        const newY = Phaser.Math.Clamp(
                            invContainer.y - scrollAmount,
                            -this._invScrollMax, 0
                        );
                        invContainer.y = newY;

                        // 핸들 위치 업데이트
                        const scrollRatio = -newY / this._invScrollMax;
                        handle.y = maskY + handleH / 2 + scrollRatio * (maskH - handleH);
                    }
                });

                // 스크롤 힌트
                this.add.text(invX + invW / 2, maskY + maskH + 8, '▼ 스크롤로 더보기', {
                    fontSize: '9px', fontFamily: 'monospace', color: '#553333'
                }).setOrigin(0.5);
            }
        }

        // ══════ 우측: 보안 컨테이너 ══════
        const secX = 640;
        const secW = 580;

        this.add.text(secX + secW / 2, baseY + 10, `🔒 보안 컨테이너 (${secure.filter(s => s).length}/3)`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#44aaff', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.add.text(secX + secW / 2, baseY + 30, '사망/탈출실패 시 보존됨', {
            fontSize: '10px', fontFamily: 'monospace', color: '#336688'
        }).setOrigin(0.5);

        const slotW = 170;
        const slotH = 55;
        const slotGap = 10;
        const slotsTotal = FARMING.SECURE_CONTAINER_SIZE * slotW + (FARMING.SECURE_CONTAINER_SIZE - 1) * slotGap;
        const slotStartX = secX + (secW - slotsTotal) / 2 + slotW / 2;

        for (let i = 0; i < FARMING.SECURE_CONTAINER_SIZE; i++) {
            const sx = slotStartX + i * (slotW + slotGap);
            const sy = baseY + 70;
            const slot = secure[i];

            const bg = this.add.rectangle(sx, sy, slotW, slotH, slot ? 0x112244 : 0x0a0a1a)
                .setStrokeStyle(2, slot ? 0x4488ff : 0x222244).setInteractive();

            if (slot) {
                const reg = ItemRegistry.get(slot.itemId);
                const name = reg ? reg.name : slot.itemId;
                const rarity = FARMING.RARITIES[slot.rarity] || FARMING.RARITIES.common;
                const enhLabel = slot.enhanceLevel ? `+${slot.enhanceLevel}` : '';

                // 아이콘
                const iconGfx = this.add.graphics();
                if (reg && reg.category === 'equipment') {
                    IconRenderer.drawWeaponIcon(iconGfx, slot.itemId, sx - slotW / 2 + 20, sy, 18);
                } else {
                    IconRenderer.drawConsumableIcon(iconGfx, slot.itemId, sx - slotW / 2 + 20, sy, 18);
                }

                this.add.text(sx + 5, sy - 8, `${name}${enhLabel}`, {
                    fontSize: '11px', fontFamily: 'monospace', color: rarity.color, fontStyle: 'bold'
                }).setOrigin(0.5, 0.5);
                this.add.text(sx + 5, sy + 10, `[${rarity.name}]`, {
                    fontSize: '9px', fontFamily: 'monospace', color: rarity.color, alpha: 0.7
                }).setOrigin(0.5, 0.5);

                bg.on('pointerover', () => {
                    bg.setFillStyle(0x1a3355);
                    this._showItemTooltip(slot, reg, rarity, sx, sy - 45);
                });
                bg.on('pointerout', () => {
                    bg.setFillStyle(0x112244);
                    this._hideItemTooltip();
                });
                bg.on('pointerdown', () => {
                    this._hideItemTooltip();
                    BP_FARMING.removeFromSecure(i);
                    this._drawAll();
                });
            } else {
                this.add.text(sx, sy, '빈 슬롯', {
                    fontSize: '11px', fontFamily: 'monospace', color: '#222244'
                }).setOrigin(0.5);
            }
        }

        // ── 탈출 버튼 (보안 컨테이너 아래) ──
        if (this.runManager && this.dangerSystem.level < 20) {
            const escY = baseY + 145;
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

            const escColor = hasKit ? 0x224422 : 0x181818;
            const escBorder = hasKit ? 0x44aa44 : 0x333333;
            const escBtn = this.add.rectangle(secX + secW / 2, escY, 360, 40, escColor)
                .setStrokeStyle(2, escBorder).setInteractive();
            this.add.text(secX + secW / 2, escY, escLabel, {
                fontSize: '13px', fontFamily: 'monospace', color: hasKit ? '#44ff88' : '#777777'
            }).setOrigin(0.5);

            escBtn.on('pointerover', () => escBtn.setFillStyle(hasKit ? 0x335533 : 0x222222));
            escBtn.on('pointerout', () => escBtn.setFillStyle(escColor));
            escBtn.on('pointerdown', () => { this._hideItemTooltip(); this._escape(hasKit, successRate); });

            this.add.text(secX + secW / 2, escY + 28, '🔒 보안 컨테이너 아이템은 항상 보존됩니다', {
                fontSize: '9px', fontFamily: 'monospace', color: '#336688'
            }).setOrigin(0.5);
        }
    }

    _showItemTooltip(item, reg, rarity, tx, ty) {
        this._hideItemTooltip();
        const tooltip = this.add.container(tx, ty).setDepth(1000);
        this._tooltip = tooltip;

        const name = reg ? reg.name : (item.itemId || '???');
        const enhLabel = item.enhanceLevel ? ` +${item.enhanceLevel}` : '';
        const lines = [`${name}${enhLabel}  [${rarity.name}]`];

        if (reg && reg.desc) lines.push(reg.desc);
        if (reg && reg.stats) {
            const statParts = [];
            if (reg.stats.atk) statParts.push(`ATK+${reg.stats.atk}`);
            if (reg.stats.def) statParts.push(`DEF+${reg.stats.def}`);
            if (reg.stats.hp) statParts.push(`HP+${reg.stats.hp}`);
            if (reg.stats.critRate) statParts.push(`CRIT+${Math.floor(reg.stats.critRate * 100)}%`);
            if (reg.stats.attackSpeed) statParts.push(`공속+${reg.stats.attackSpeed}ms`);
            if (statParts.length > 0) lines.push(statParts.join('  '));
        }
        if (reg && reg.slot) {
            const slotLabel = { weapon: '무기', armor: '방어구', accessory: '악세서리' }[reg.slot] || reg.slot;
            lines.push(`슬롯: ${slotLabel}`);
        }

        const maxW = Math.max(...lines.map(l => l.length)) * 8 + 24;
        const tipH = lines.length * 18 + 16;

        tooltip.add(this.add.rectangle(0, 0, maxW, tipH, 0x111111, 0.95)
            .setStrokeStyle(2, rarity.borderColor));

        lines.forEach((line, i) => {
            const color = i === 0 ? rarity.color : '#bbbbbb';
            const size = i === 0 ? '12px' : '10px';
            tooltip.add(this.add.text(0, -tipH / 2 + 12 + i * 18, line, {
                fontSize: size, fontFamily: 'monospace', color
            }).setOrigin(0.5, 0));
        });
    }

    _hideItemTooltip() {
        if (this._tooltip) {
            this._tooltip.destroy();
            this._tooltip = null;
        }
    }

    // _drawEscapeButton removed — now integrated into _drawRaidInventory

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
