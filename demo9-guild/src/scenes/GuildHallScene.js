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

        // === 트리 레이아웃 — 지그재그(snake) 4 티어 × 3 스테이지 ===
        // 티어 1 (스테이지 1-3): L→R
        // 티어 2 (스테이지 4-6): R→L
        // 티어 3 (스테이지 7-9): L→R
        // 티어 4 (스테이지 10-12): R→L
        const cardW = 240, cardH = 88;
        const gapH = 22, gapV = 38;
        const rowW = 3 * cardW + 2 * gapH;          // 762
        const startX = baseX + (1020 - rowW) / 2;   // 우측 영역 중앙
        const startY = baseY + 110;

        // 각 스테이지의 위치 미리 계산
        const positions = {};
        for (let s = 1; s <= 12; s++) {
            const tier = Math.ceil(s / 3);          // 1-4
            const inTier = (s - 1) % 3;             // 0-2
            const reversed = (tier % 2 === 0);      // 짝수 티어는 R→L
            const col = reversed ? (2 - inTier) : inTier;
            const x = startX + col * (cardW + gapH);
            const y = startY + (tier - 1) * (cardH + gapV);
            positions[s] = { x, y, tier, col };
        }

        // === 티어 헤더 + 연결선 먼저 그림 ===
        const tierGates = [
            { tier: 1, label: '티어 1 — 기본' },
            { tier: 2, label: '티어 2 — 길드 Lv.2+' },
            { tier: 3, label: '티어 3 — 평판 15+ / 길드 Lv.4+' },
            { tier: 4, label: '티어 4 — 평판 35+ / 길드 Lv.6+' }
        ];
        tierGates.forEach(t => {
            const tY = startY + (t.tier - 1) * (cardH + gapV);
            this.add.text(baseX, tY + cardH / 2, t.label, {
                fontSize: '10px', fontFamily: 'monospace',
                color: this._tierUnlocked(gs, t.tier) ? '#aaccdd' : '#555566',
                fontStyle: 'bold'
            }).setOrigin(0, 0.5);
        });

        // 연결선 그리기 (스테이지 1→2→3→4→...→12)
        for (let s = 1; s < 12; s++) {
            const from = positions[s];
            const to = positions[s + 1];
            const isReached = s < curStage;       // 둘 다 구매됨
            const isCurrent = s === curStage;     // 다음 단계로 진행 가능
            this._drawConnection(from, to, cardW, cardH, isReached, isCurrent, data.color);
        }

        // === 노드 그림 ===
        for (let s = 1; s <= 12; s++) {
            const p = positions[s];
            this._drawStageCard(data, s, p.x, p.y, cardW, cardH, curStage);
        }
    }

    /** 두 노드 사이 연결선 — 수평이면 직선, 수직이면 ㄱ자/ㄴ자 꺾임 */
    _drawConnection(from, to, cw, ch, isReached, isCurrent, accentColor) {
        const g = this.add.graphics();
        const color = isReached ? accentColor : isCurrent ? 0xffaa44 : 0x333344;
        const alpha = isReached ? 0.9 : isCurrent ? 0.7 : 0.4;
        const thickness = isReached || isCurrent ? 3 : 2;
        g.lineStyle(thickness, color, alpha);

        if (from.tier === to.tier) {
            // 같은 티어 — 수평 직선 (양옆 가운데 연결)
            const isLR = to.x > from.x;
            const x1 = isLR ? from.x + cw : from.x;
            const x2 = isLR ? to.x : to.x + cw;
            const y = from.y + ch / 2;
            g.lineBetween(x1, y, x2, y);
            // 화살표 머리
            const arrowX = isLR ? x2 - 6 : x2 + 6;
            g.fillStyle(color, alpha);
            g.fillTriangle(
                isLR ? x2 : x2,            y,
                isLR ? x2 - 8 : x2 + 8,    y - 5,
                isLR ? x2 - 8 : x2 + 8,    y + 5
            );
        } else {
            // 티어 변경 — 수직 드롭 (같은 x 위치)
            const x = from.x + cw / 2;
            const y1 = from.y + ch;
            const y2 = to.y;
            g.lineBetween(x, y1, x, y2);
            // 화살표 머리 (아래쪽)
            g.fillStyle(color, alpha);
            g.fillTriangle(x, y2, x - 5, y2 - 8, x + 5, y2 - 8);
        }
    }

    _tierUnlocked(gs, tier) {
        const gate = getGuildHallStageGate((tier - 1) * 3 + 1);
        return gs.guildLevel >= gate.guildLv && (gs.guildReputation || 0) >= gate.rep;
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
        const isFinish = stage === 12;

        const bg = this.add.graphics();
        if (isPurchased) {
            bg.fillStyle(isFinish ? 0x3a2a0a : 0x1a3a1a, 1);
            bg.fillRoundedRect(x, y, w, h, 8);
            bg.lineStyle(2, isFinish ? 0xffcc44 : 0x44ff88, 0.9);
            bg.strokeRoundedRect(x, y, w, h, 8);
        } else if (canBuy) {
            bg.fillStyle(0x2a2a1a, 1);
            bg.fillRoundedRect(x, y, w, h, 8);
            bg.lineStyle(2, 0xffaa44, 0.95);
            bg.strokeRoundedRect(x, y, w, h, 8);
        } else if (isNext) {
            bg.fillStyle(0x1a1a26, 1);
            bg.fillRoundedRect(x, y, w, h, 8);
            bg.lineStyle(1, 0x666677, 0.7);
            bg.strokeRoundedRect(x, y, w, h, 8);
        } else {
            bg.fillStyle(0x111118, 1);
            bg.fillRoundedRect(x, y, w, h, 8);
            bg.lineStyle(1, 0x333344, 0.4);
            bg.strokeRoundedRect(x, y, w, h, 8);
        }

        // 단계 원형 배지 (좌측)
        const badgeSize = 26;
        const bcx = x + 18, bcy = y + 18;
        const badgeBg = this.add.graphics();
        const badgeColor = isPurchased ? (isFinish ? 0xffcc44 : 0x44ff88) : canBuy ? 0xffaa44 : 0x444455;
        badgeBg.fillStyle(badgeColor, 0.9);
        badgeBg.fillCircle(bcx, bcy, badgeSize / 2);
        badgeBg.lineStyle(2, 0xffffff, 0.4);
        badgeBg.strokeCircle(bcx, bcy, badgeSize / 2);

        const badgeText = isFinish ? '🏆' : `${stage}`;
        this.add.text(bcx, bcy, badgeText, {
            fontSize: isFinish ? '13px' : '12px', fontFamily: 'monospace',
            color: isPurchased ? '#000000' : canBuy ? '#000000' : '#888899', fontStyle: 'bold'
        }).setOrigin(0.5);

        // 이름 + 설명 (배지 우측)
        const txtX = x + 38;
        const nameColor = isPurchased ? (isFinish ? '#ffe088' : '#aaffcc') : canBuy ? '#ffeecc' : '#777788';
        this.add.text(txtX, y + 6, stageData.name, {
            fontSize: '11px', fontFamily: 'monospace', color: nameColor, fontStyle: 'bold'
        });
        this.add.text(txtX, y + 22, stageData.desc, {
            fontSize: '9px', fontFamily: 'monospace',
            color: isPurchased ? '#88aa99' : '#7788aa',
            wordWrap: { width: w - 50 }
        });

        // 비용 / 상태 (우상단)
        if (isPurchased) {
            this.add.text(x + w - 8, y + 6, '✓', {
                fontSize: '14px', fontFamily: 'monospace', color: '#88ffaa', fontStyle: 'bold'
            }).setOrigin(1, 0);
        } else {
            this.add.text(x + w - 8, y + 6, `${cost}G`, {
                fontSize: '10px', fontFamily: 'monospace',
                color: gs.gold >= cost ? '#ffcc44' : '#aa6644', fontStyle: 'bold'
            }).setOrigin(1, 0);
        }

        // 잠금/구매 — 카드 하단 작게
        if (!isPurchased && (!gateOK || isLocked)) {
            const cy = y + h - 14;
            if (!gateOK) {
                const txt = [];
                if (gs.guildLevel < gate.guildLv) txt.push(`길드 Lv.${gate.guildLv}`);
                if ((gs.guildReputation || 0) < gate.rep) txt.push(`평판 ${gate.rep}`);
                this.add.text(x + 8, cy, `🔒 ${txt.join(' / ')}`, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#aa6666'
                });
            } else if (isLocked) {
                this.add.text(x + 8, cy, `이전 단계 먼저`, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#666677'
                });
            }
        }

        // 구매 버튼 — 카드 우하단
        if (canBuy) {
            UIButton.create(this, x + w - 50, y + h - 14, 90, 20, `🏛 구매`, {
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
