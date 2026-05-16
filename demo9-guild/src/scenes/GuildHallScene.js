/**
 * GuildHallScene — 길드 회관 업그레이드 트리.
 * 8 카테고리 × 12 단계 = 96 업그레이드.
 * 좌측 카테고리 탭 + 중앙 단계별 카드 + 우측 상세 패널.
 */
class GuildHallScene extends Phaser.Scene {
    constructor() { super('GuildHallScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.selectedCategory = data.selectedCategory || 'operations';
    }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a14);
        this._drawUI();
    }

    _drawUI() {
        const gs = this.gameState;
        GuildHallManager.ensureState(gs);

        // 헤더
        this.add.text(640, 22, '🏛 길드 회관', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffcc66', fontStyle: 'bold'
        }).setOrigin(0.5);

        UIButton.create(this, 80, 22, 100, 30, '← 마을', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        // 골드 + 평판
        this.add.text(1260, 18, `${gs.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0);
        this.add.text(1260, 38, `평판 ${gs.guildReputation || 0}/100`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#ccaa66'
        }).setOrigin(1, 0);

        // 카테고리 탭 (좌측, 세로)
        this._drawCategoryTabs();

        // 선택된 카테고리의 단계 트리 (중앙)
        this._drawStageTree();
    }

    _drawCategoryTabs() {
        const gs = this.gameState;
        const startY = 70;
        const tabH = 70;
        const tabW = 220;

        GUILD_HALL_KEYS.forEach((key, idx) => {
            const data = GUILD_HALL_DATA[key];
            const y = startY + idx * (tabH + 4);
            const unlocked = isGuildHallUnlocked(gs, key);
            const isSelected = this.selectedCategory === key;
            const curStage = GuildHallManager.getStage(gs, key);

            const bg = this.add.graphics();
            if (isSelected) {
                bg.fillStyle(data.color, 0.25);
                bg.fillRoundedRect(15, y, tabW, tabH, 5);
                bg.lineStyle(2, data.color, 0.9);
                bg.strokeRoundedRect(15, y, tabW, tabH, 5);
            } else {
                bg.fillStyle(0x141420, 1);
                bg.fillRoundedRect(15, y, tabW, tabH, 5);
                bg.lineStyle(1, unlocked ? 0x333355 : 0x222233, 0.6);
                bg.strokeRoundedRect(15, y, tabW, tabH, 5);
            }

            this.add.text(28, y + 8, data.icon, { fontSize: '18px' });
            this.add.text(58, y + 6, data.name, {
                fontSize: '13px', fontFamily: 'monospace',
                color: unlocked ? data.textColor : '#555566', fontStyle: 'bold'
            });
            this.add.text(58, y + 26, data.desc, {
                fontSize: '9px', fontFamily: 'monospace',
                color: unlocked ? '#888899' : '#444455',
                wordWrap: { width: tabW - 70 }
            });

            // 진행도 바
            const barX = 28, barY = y + tabH - 12, barW = tabW - 26, barH = 5;
            const barBg = this.add.graphics();
            barBg.fillStyle(0x222233, 1);
            barBg.fillRect(barX, barY, barW, barH);
            barBg.fillStyle(data.color, 0.9);
            barBg.fillRect(barX, barY, barW * (curStage / 12), barH);
            this.add.text(barX + barW, barY - 12, `${curStage}/12`, {
                fontSize: '9px', fontFamily: 'monospace', color: data.textColor
            }).setOrigin(1, 0);

            if (!unlocked) {
                this.add.text(barX + 5, barY - 12, '🔒 잠김', {
                    fontSize: '9px', fontFamily: 'monospace', color: '#666677'
                });
            }

            // 클릭
            const hit = this.add.zone(15 + tabW/2, y + tabH/2, tabW, tabH).setInteractive({ useHandCursor: true });
            hit.on('pointerdown', () => {
                this.selectedCategory = key;
                this.scene.restart({ gameState: gs, selectedCategory: key });
            });
        });
    }

    _drawStageTree() {
        const gs = this.gameState;
        const data = GUILD_HALL_DATA[this.selectedCategory];
        if (!data) return;

        const unlocked = isGuildHallUnlocked(gs, this.selectedCategory);
        const curStage = GuildHallManager.getStage(gs, this.selectedCategory);

        // 우측 영역: x=250, w=1020
        const baseX = 250, baseY = 70;
        this.add.text(baseX, baseY, `${data.icon}  ${data.name}`, {
            fontSize: '18px', fontFamily: 'monospace', color: data.textColor, fontStyle: 'bold'
        });
        this.add.text(baseX, baseY + 26, data.desc, {
            fontSize: '11px', fontFamily: 'monospace', color: '#888899'
        });

        if (!unlocked) {
            this.add.text(baseX + 400, 360, '🔒 잠긴 카테고리\n(' + this._unlockHint(data) + ')', {
                fontSize: '16px', fontFamily: 'monospace', color: '#666677',
                align: 'center'
            }).setOrigin(0.5);
            return;
        }

        // 단계 카드 4×3 그리드 (12개)
        const gridX = baseX, gridY = baseY + 60;
        const cardW = 245, cardH = 100, gap = 8;
        const cols = 4;
        for (let s = 1; s <= 12; s++) {
            const col = (s - 1) % cols;
            const row = Math.floor((s - 1) / cols);
            const x = gridX + col * (cardW + gap);
            const y = gridY + row * (cardH + gap);
            this._drawStageCard(data, s, x, y, cardW, cardH, curStage);
        }
    }

    _drawStageCard(data, stage, x, y, w, h, curStage) {
        const gs = this.gameState;
        const stageData = data.stages[stage - 1];
        if (!stageData) return;

        const isPurchased = stage <= curStage;
        const isNext = stage === curStage + 1;
        const isLocked = stage > curStage + 1;
        const cost = getGuildHallStageCost(this.selectedCategory, stage);
        const gate = getGuildHallStageGate(stage);
        const gateOK = gs.guildLevel >= gate.guildLv && (gs.guildReputation || 0) >= gate.rep;
        const canBuy = isNext && gateOK && gs.gold >= cost;

        const bg = this.add.graphics();
        if (isPurchased) {
            bg.fillStyle(0x1a3a1a, 1);
            bg.fillRoundedRect(x, y, w, h, 5);
            bg.lineStyle(2, 0x44ff88, 0.8);
            bg.strokeRoundedRect(x, y, w, h, 5);
        } else if (canBuy) {
            bg.fillStyle(0x2a2a1a, 1);
            bg.fillRoundedRect(x, y, w, h, 5);
            bg.lineStyle(2, 0xffaa44, 0.9);
            bg.strokeRoundedRect(x, y, w, h, 5);
        } else if (isNext) {
            bg.fillStyle(0x1a1a26, 1);
            bg.fillRoundedRect(x, y, w, h, 5);
            bg.lineStyle(1, 0x666677, 0.7);
            bg.strokeRoundedRect(x, y, w, h, 5);
        } else {
            bg.fillStyle(0x111118, 1);
            bg.fillRoundedRect(x, y, w, h, 5);
            bg.lineStyle(1, 0x333344, 0.4);
            bg.strokeRoundedRect(x, y, w, h, 5);
        }

        // 단계 번호 (좌상)
        const stageBadge = stage === 12 ? '🏆 12' : `Lv.${stage}`;
        const badgeColor = isPurchased ? '#88ffaa' : canBuy ? '#ffcc66' : '#666677';
        this.add.text(x + 8, y + 6, stageBadge, {
            fontSize: '11px', fontFamily: 'monospace', color: badgeColor, fontStyle: 'bold'
        });

        // 비용 (우상)
        if (!isPurchased) {
            this.add.text(x + w - 8, y + 6, `${cost}G`, {
                fontSize: '11px', fontFamily: 'monospace',
                color: gs.gold >= cost ? '#ffcc44' : '#aa6644'
            }).setOrigin(1, 0);
        } else {
            this.add.text(x + w - 8, y + 6, '✓ 완료', {
                fontSize: '10px', fontFamily: 'monospace', color: '#88ffaa'
            }).setOrigin(1, 0);
        }

        // 이름
        this.add.text(x + 8, y + 24, stageData.name, {
            fontSize: '12px', fontFamily: 'monospace',
            color: isPurchased ? '#aaccaa' : canBuy ? '#ffeecc' : '#888899', fontStyle: 'bold'
        });

        // 설명
        this.add.text(x + 8, y + 42, stageData.desc, {
            fontSize: '10px', fontFamily: 'monospace',
            color: isPurchased ? '#778877' : '#888899',
            wordWrap: { width: w - 16 }
        });

        // 게이트 조건 (잠금 시)
        if (!isPurchased && (!gateOK || isLocked)) {
            const cy = y + h - 30;
            if (!gateOK) {
                const txt = [];
                if (gs.guildLevel < gate.guildLv) txt.push(`길드 Lv.${gate.guildLv}`);
                if ((gs.guildReputation || 0) < gate.rep) txt.push(`평판 ${gate.rep}`);
                this.add.text(x + 8, cy, `🔒 ${txt.join(' / ')} 필요`, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#aa6666'
                });
            } else if (isLocked) {
                this.add.text(x + 8, cy, `이전 단계 먼저 구매`, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#666677'
                });
            }
        }

        // 구매 버튼
        if (canBuy) {
            UIButton.create(this, x + w/2, y + h - 13, w - 16, 22, `🏛 ${cost}G 구매`, {
                color: 0x665533, hoverColor: 0x886644, textColor: '#ffffff', fontSize: 10,
                onClick: () => {
                    const r = GuildHallManager.purchaseNextStage(gs, this.selectedCategory);
                    if (r.success) {
                        SaveManager.save(gs);
                        UIToast.show(this, `🏛 ${stageData.name} 해금!`, { color: '#88ffaa' });
                        this.scene.restart({ gameState: gs, selectedCategory: this.selectedCategory });
                    } else {
                        UIToast.show(this, r.msg, { color: '#ff8866' });
                    }
                }
            });
        }
    }

    _unlockHint(data) {
        if (typeof data.unlockCondition !== 'string') return '잠긴';
        if (data.unlockCondition.startsWith('zone_clear:')) {
            const zone = data.unlockCondition.split(':')[1];
            const zoneName = ZONE_DATA[zone]?.name || zone;
            return `${zoneName} Lv.1 첫 클리어 필요`;
        }
        return '잠긴';
    }
}
