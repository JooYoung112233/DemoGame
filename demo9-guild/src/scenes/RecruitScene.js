class RecruitScene extends Phaser.Scene {
    constructor() { super('RecruitScene'); }

    init(data) { this.gameState = data.gameState; }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
        this._scrollX = 0;
        this._cardContainer = null;
        this._scrollThumb = null;
        this._drawUI();
    }

    _drawUI() {
        const gs = this.gameState;

        this.add.text(640, 25, '용병 모집소', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(1260, 25, `${gs.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0);

        const maxRoster = GuildManager.getMaxRoster(gs);
        this.add.text(640, 50, `로스터: ${gs.roster.length}/${maxRoster}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        const rerollCost = 50;
        const canReroll = gs.gold >= rerollCost;
        UIButton.create(this, 1160, 50, 140, 28, `리롤 (${rerollCost}G)`, {
            color: canReroll ? 0x446688 : 0x333333,
            hoverColor: 0x5588aa,
            textColor: canReroll ? '#ccddee' : '#555555',
            fontSize: 12,
            disabled: !canReroll,
            onClick: () => {
                if (GuildManager.spendGold(gs, rerollCost)) {
                    MercenaryManager.generateRecruitPool(gs);
                    GuildManager.addMessage(gs, `모집 풀 갱신 (-${rerollCost}G)`);
                    SaveManager.save(gs);
                    this.scene.restart({ gameState: gs });
                }
            }
        });

        if (gs.recruitPool.length === 0) {
            this.add.text(640, 360, '모집 가능한 용병이 없습니다\n런을 진행하면 새로운 용병이 등장합니다', {
                fontSize: '14px', fontFamily: 'monospace', color: '#555566', align: 'center'
            }).setOrigin(0.5);
            return;
        }

        const cardW = 220;
        const gap = 15;
        const totalW = gs.recruitPool.length * (cardW + gap) - gap;
        const viewW = 1240; // 좌우 여백 20씩
        const viewX = 20;

        // 스크롤이 필요한 경우에만 컨테이너+마스크 사용
        const needsScroll = totalW > viewW;

        const container = this.add.container(0, 0);
        this._cardContainer = container;
        this._totalContentW = totalW;
        this._viewW = viewW;
        this._viewX = viewX;

        // 카드 배치 — 컨테이너 내부 좌표
        const startX = needsScroll ? viewX : (640 - totalW / 2);
        gs.recruitPool.forEach((merc, idx) => {
            this._drawRecruitCard(container, merc, startX + idx * (cardW + gap), 80, cardW);
        });

        if (needsScroll) {
            // 마스크
            const maskShape = this.make.graphics({ add: false });
            maskShape.fillStyle(0xffffff);
            maskShape.fillRect(viewX, 75, viewW, 560);
            const mask = maskShape.createGeometryMask();
            container.setMask(mask);

            // 하단 가로 스크롤바 트랙
            const trackY = 645;
            const trackBg = this.add.graphics();
            trackBg.fillStyle(0x222233, 0.5);
            trackBg.fillRoundedRect(viewX, trackY, viewW, 8, 4);

            this._scrollThumb = this.add.graphics();
            this._scrollThumb.setDepth(10);
            this._scrollTrackY = trackY;

            this._updateScroll();

            // 스크롤 힌트 화살표
            if (totalW > viewW) {
                this.add.text(viewX + viewW + 5, 360, '▶', {
                    fontSize: '16px', fontFamily: 'monospace', color: '#555566'
                }).setOrigin(0, 0.5);
                this.add.text(viewX - 5, 360, '◀', {
                    fontSize: '16px', fontFamily: 'monospace', color: '#555566'
                }).setOrigin(1, 0.5);
            }

            // 휠 이벤트 (가로 스크롤)
            this.input.on('wheel', (pointer, gameObjects, dx, dy) => {
                this._scrollX += dy * 0.8;
                this._clampScroll();
                this._updateScroll();
            });

            // 드래그 스크롤
            this._isDragging = false;
            this._dragStartX = 0;
            this._dragScrollStart = 0;

            this.input.on('pointerdown', (pointer) => {
                if (pointer.worldY >= 80 && pointer.worldY <= 640) {
                    this._isDragging = true;
                    this._dragStartX = pointer.worldX;
                    this._dragScrollStart = this._scrollX;
                }
            });
            this.input.on('pointermove', (pointer) => {
                if (this._isDragging && pointer.isDown) {
                    const dx = this._dragStartX - pointer.worldX;
                    if (Math.abs(dx) > 5) {
                        this._scrollX = this._dragScrollStart + dx;
                        this._clampScroll();
                        this._updateScroll();
                    }
                }
            });
            this.input.on('pointerup', () => { this._isDragging = false; });
        }
    }

    _clampScroll() {
        const maxScroll = Math.max(0, this._totalContentW - this._viewW);
        this._scrollX = Math.max(0, Math.min(this._scrollX, maxScroll));
    }

    _updateScroll() {
        if (this._cardContainer) {
            this._cardContainer.x = -this._scrollX;
        }
        if (this._scrollThumb && this._totalContentW > this._viewW) {
            const ratio = this._viewW / this._totalContentW;
            const thumbW = Math.max(30, this._viewW * ratio);
            const maxScroll = this._totalContentW - this._viewW;
            const scrollRatio = maxScroll > 0 ? this._scrollX / maxScroll : 0;
            const thumbX = this._viewX + scrollRatio * (this._viewW - thumbW);

            this._scrollThumb.clear();
            this._scrollThumb.fillStyle(0xffffff, 0.4);
            this._scrollThumb.fillRoundedRect(thumbX, this._scrollTrackY, thumbW, 8, 4);
        }
    }

    _drawRecruitCard(container, merc, x, y, w) {
        const gs = this.gameState;
        const base = merc.getBaseClass();
        const rarity = RARITY_DATA[merc.rarity];
        const stats = merc.getStats();
        const cost = merc.getHireCost();
        const canAfford = gs.gold >= cost;
        const rosterFull = gs.roster.length >= GuildManager.getMaxRoster(gs);

        const bg = this.add.graphics();
        bg.fillStyle(0x151525, 1);
        bg.fillRoundedRect(x, y, w, 530, 5);
        bg.lineStyle(2, rarity.color, 0.5);
        bg.strokeRoundedRect(x, y, w, 530, 5);
        container.add(bg);

        let cy = y + 15;

        container.add(this.add.text(x + w / 2, cy, base.icon, { fontSize: '32px' }).setOrigin(0.5));
        cy += 40;

        container.add(this.add.text(x + w / 2, cy, merc.name, {
            fontSize: '13px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }).setOrigin(0.5));
        cy += 20;

        container.add(this.add.text(x + w / 2, cy, `${base.name}  [${rarity.name}]`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5));
        cy += 25;

        const line = this.add.graphics();
        line.lineStyle(1, 0x333355, 0.5);
        line.lineBetween(x + 10, cy, x + w - 10, cy);
        container.add(line);
        cy += 10;

        const statLines = [
            `HP     ${stats.hp}`,
            `ATK    ${stats.atk}`,
            `DEF    ${stats.def}`,
            `SPD    ${stats.moveSpeed}`,
            `CRIT   ${Math.floor(stats.critRate * 100)}%`,
            `범위   ${stats.range}`
        ];
        statLines.forEach(s => {
            container.add(this.add.text(x + 15, cy, s, {
                fontSize: '11px', fontFamily: 'monospace', color: '#aaaacc'
            }));
            cy += 17;
        });
        cy += 5;

        const line2 = this.add.graphics();
        line2.lineStyle(1, 0x333355, 0.5);
        line2.lineBetween(x + 10, cy, x + w - 10, cy);
        container.add(line2);
        cy += 10;

        container.add(this.add.text(x + 15, cy, '특성:', {
            fontSize: '11px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
        }));
        cy += 18;

        merc.traits.forEach(trait => {
            let color = '#44cc44';
            let sym = '✦';
            if (trait.type === 'negative') { color = '#ff6666'; sym = '✧'; }
            if (trait.type === 'legendary') { color = '#ffaa00'; sym = '★'; }

            container.add(this.add.text(x + 15, cy, `${sym} ${trait.name}`, {
                fontSize: '10px', fontFamily: 'monospace', color
            }));
            container.add(this.add.text(x + 15, cy + 13, `  ${trait.desc}`, {
                fontSize: '9px', fontFamily: 'monospace', color: '#777788'
            }));
            cy += 28;
        });

        cy = y + 530 - 50;

        const line3 = this.add.graphics();
        line3.lineStyle(1, 0x333355, 0.5);
        line3.lineBetween(x + 10, cy, x + w - 10, cy);
        container.add(line3);
        cy += 12;

        const disabled = !canAfford || rosterFull;
        let btnLabel = `고용 (${cost}G)`;
        if (rosterFull) btnLabel = '로스터 가득';
        else if (!canAfford) btnLabel = `${cost}G 필요`;

        const btn = UIButton.create(this, x + w / 2, cy + 12, w - 20, 30, btnLabel, {
            color: disabled ? 0x444444 : 0xffaa44,
            hoverColor: 0xffcc66,
            textColor: disabled ? '#666666' : '#000000',
            fontSize: 12,
            disabled,
            onClick: () => {
                const result = MercenaryManager.hire(gs, merc.id);
                if (result.success) {
                    UIToast.show(this, result.msg);
                } else {
                    UIToast.show(this, result.msg, { color: '#ff6666' });
                }
                this.scene.restart({ gameState: gs });
            }
        });
        container.add(btn);
    }
}
