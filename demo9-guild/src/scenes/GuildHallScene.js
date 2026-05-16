class GuildHallScene extends Phaser.Scene {
    constructor() { super('GuildHallScene'); }

    init(data) {
        this.gameState = data.gameState;
    }

    create() {
        const gs = this.gameState;
        GuildHallManager.ensureState(gs);

        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);

        this.add.text(640, 25, '🏛 길드 회관', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffcc88', fontStyle: 'bold'
        }).setOrigin(0.5);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            color: 0x335577, hoverColor: 0x446688, textColor: '#cceeff', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        this.add.text(1260, 25, `${gs.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0.5);

        this._drawCategories();
    }

    _drawCategories() {
        const gs = this.gameState;
        const cats = GuildHallManager.CATEGORIES;
        const keys = Object.keys(cats);

        const startX = 40;
        const startY = 60;
        const cardW = 290;
        const cardH = 145;
        const gapX = 15;
        const gapY = 12;
        const cols = 4;

        keys.forEach((key, idx) => {
            const col = idx % cols;
            const row = Math.floor(idx / cols);
            const x = startX + col * (cardW + gapX);
            const y = startY + row * (cardH + gapY);
            this._drawCategoryCard(key, x, y, cardW, cardH);
        });
    }

    _drawCategoryCard(catKey, x, y, w, h) {
        const gs = this.gameState;
        const cat = GuildHallManager.CATEGORIES[catKey];
        const stage = GuildHallManager.getStage(gs, catKey);
        const maxStage = cat.maxStage;
        const check = GuildHallManager.canUpgrade(gs, catKey);

        const bg = this.add.graphics();
        bg.fillStyle(0x151530, 1);
        bg.fillRoundedRect(x, y, w, h, 6);
        bg.lineStyle(1, stage >= maxStage ? 0xffcc44 : 0x334466, 0.8);
        bg.strokeRoundedRect(x, y, w, h, 6);

        // 헤더
        this.add.text(x + 10, y + 10, `${cat.icon} ${cat.code}. ${cat.name}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ccddff', fontStyle: 'bold'
        });

        this.add.text(x + w - 10, y + 10, `${stage}/${maxStage}`, {
            fontSize: '13px', fontFamily: 'monospace',
            color: stage >= maxStage ? '#ffcc44' : '#8899aa'
        }).setOrigin(1, 0);

        // 진행 바
        const barX = x + 10, barY = y + 32, barW = w - 20, barH = 6;
        const barBg = this.add.graphics();
        barBg.fillStyle(0x222244, 1);
        barBg.fillRoundedRect(barX, barY, barW, barH, 2);
        barBg.fillStyle(stage >= maxStage ? 0xffcc44 : 0x4488ff, 1);
        barBg.fillRoundedRect(barX, barY, barW * (stage / maxStage), barH, 2);

        // 현재 효과
        const currentEffect = GuildHallManager.getEffectDescription(catKey, stage);
        if (currentEffect) {
            this.add.text(x + 10, y + 44, `현재: ${currentEffect}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#66aa88'
            });
        }

        // 다음 효과
        const nextEffect = GuildHallManager.getEffectDescription(catKey, stage + 1);
        if (nextEffect && stage < maxStage) {
            this.add.text(x + 10, y + 60, `다음: ${nextEffect}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#8888aa'
            });
        }

        // 업그레이드 버튼 or 상태
        if (stage >= maxStage) {
            this.add.text(x + w / 2, y + h - 25, '✅ 완료', {
                fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
            }).setOrigin(0.5);
        } else if (check.ok) {
            const cost = GuildHallManager.getUpgradeCost(catKey, stage + 1);
            UIButton.create(this, x + w / 2, y + h - 25, 180, 28,
                `업그레이드 (${cost}G)`, {
                    color: 0x448844, hoverColor: 0x55aa55, textColor: '#ffffff', fontSize: 11,
                    onClick: () => {
                        GuildHallManager.upgrade(gs, catKey);
                        UIToast.show(this, `${cat.name} ${cat.code}${stage + 1} 해금!`);
                        this.scene.restart({ gameState: gs });
                    }
                });
        } else {
            const cost = GuildHallManager.getUpgradeCost(catKey, stage + 1);
            this.add.text(x + w / 2, y + h - 35, `${cost}G`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#665544'
            }).setOrigin(0.5);
            this.add.text(x + w / 2, y + h - 18, `🔒 ${check.reason}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#884444'
            }).setOrigin(0.5);
        }

        // 5개 단계 미니 도트
        const dotY = y + 78;
        const dotSpacing = Math.min(20, (w - 20) / maxStage);
        const dotStartX = x + 10;
        const dots = this.add.graphics();
        for (let i = 1; i <= maxStage; i++) {
            const dx = dotStartX + (i - 1) * dotSpacing;
            dots.fillStyle(i <= stage ? 0x44aaff : 0x333355, 1);
            dots.fillCircle(dx + 4, dotY, 3);
        }
    }
}
