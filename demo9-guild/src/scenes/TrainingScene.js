class TrainingScene extends Phaser.Scene {
    constructor() { super('TrainingScene'); }

    init(data) { this.gameState = data.gameState; }

    create() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        this.add.rectangle(640, 360, 1280, 720, T.bg || 0x1a1510);
        const gs = this.gameState;

        // ── 헤더 배경 ──
        const headerBg = this.add.graphics();
        headerBg.fillStyle(T.headerBg || 0x1e1810, 1);
        headerBg.fillRect(0, 0, 1280, T.headerHeight || 55);
        // 하단 장식선
        headerBg.lineStyle(1, T.ornament || 0x8a7a4a, T.ornamentAlpha || 0.4);
        headerBg.lineBetween(0, (T.headerHeight || 55) - 1, 1280, (T.headerHeight || 55) - 1);
        // 상단 미세 금선
        headerBg.lineStyle(1, T.ornament || 0x8a7a4a, 0.15);
        headerBg.lineBetween(0, 1, 1280, 1);

        this.add.text(640, 25, '◈  훈련소  ◈', {
            fontSize: `${(T.fontSize && T.fontSize.title) || 20}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44',
            fontStyle: 'bold'
        }).setOrigin(0.5);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            variant: 'ghost',
            fontSize: (T.fontSize && T.fontSize.body) || 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        this.add.text(640, (T.headerHeight || 55) + 5, '길드 레벨업 시 획득한 훈련 포인트로 전체 용병을 영구 강화합니다', {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
        }).setOrigin(0.5);

        this._drawContent();
    }

    _drawContent() {
        if (this._objs) this._objs.forEach(o => o.destroy && o.destroy());
        this._objs = [];

        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const t = gs.training;

        this._objs.push(this.add.text(640, 85, `보유 포인트: ${gs.trainingPoints}`, {
            fontSize: '16px', fontFamily: T.fontFamily || 'monospace',
            color: gs.trainingPoints > 0 ? '#44ff88' : '#ff4444', fontStyle: 'bold'
        }).setOrigin(0.5));

        const cats = [
            {
                key: 'hp', name: '체력 단련', icon: '❤',
                desc: '전체 용병 HP +3% / 포인트',
                color: 0xcc4433, textColor: '#dd6655'
            },
            {
                key: 'atk', name: '공격 훈련', icon: '⚔',
                desc: '전체 용병 ATK +3% / 포인트',
                color: 0xcc7733, textColor: '#ddaa55'
            },
            {
                key: 'survival', name: '방어 훈련', icon: '🛡',
                desc: '전체 용병 DEF +2 / 포인트',
                color: 0x4477aa, textColor: '#6699bb'
            },
            {
                key: 'recovery', name: '전투 숙련', icon: '⚡',
                desc: '전체 용병 스킬 쿨다운 -3% / 포인트',
                color: 0x9944aa, textColor: '#bb66cc'
            }
        ];

        const cardW = 260;
        const gap = 20;
        const totalW = cats.length * cardW + (cats.length - 1) * gap;
        const startX = 640 - totalW / 2;

        cats.forEach((cat, idx) => {
            const cx = startX + idx * (cardW + gap) + cardW / 2;
            const cy = 300;
            this._drawTrainingCard(cat, cx, cy, cardW, t);
        });

        this._drawPreview(gs, cats, 120, 500);
    }

    _drawTrainingCard(cat, cx, cy, w, training) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const level = training[cat.key] || 0;
        const maxLevel = 10;
        const canTrain = gs.trainingPoints > 0 && level < maxLevel;

        const bg = this._addObj(this.add.graphics());
        bg.fillStyle(T.cardFill || 0x231e14, 1);
        bg.fillRoundedRect(cx - w / 2, cy - 120, w, 240, T.borderRadius || 6);
        // 상단 미세 글로우
        bg.fillStyle(0xffffff, 0.03);
        bg.fillRect(cx - w / 2 + 2, cy - 119, w - 4, 30);
        // 카테고리 컬러 테두리
        bg.lineStyle(2, cat.color, 0.5);
        bg.strokeRoundedRect(cx - w / 2, cy - 120, w, 240, T.borderRadius || 6);

        this._addObj(this.add.text(cx, cy - 95, cat.icon, {
            fontSize: '28px'
        }).setOrigin(0.5));

        this._addObj(this.add.text(cx, cy - 60, cat.name, {
            fontSize: '15px', fontFamily: T.fontFamily || 'monospace', color: cat.textColor, fontStyle: 'bold'
        }).setOrigin(0.5));

        this._addObj(this.add.text(cx, cy - 38, cat.desc, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
        }).setOrigin(0.5));

        this._addObj(this.add.text(cx, cy - 10, `Lv. ${level} / ${maxLevel}`, {
            fontSize: '18px', fontFamily: T.fontFamily || 'monospace', color: T.textPrimary || '#e8d8c0', fontStyle: 'bold'
        }).setOrigin(0.5));

        const barW = w - 40;
        const barH = 10;
        const barX = cx - barW / 2;
        const barY = cy + 15;
        const barBg = this._addObj(this.add.rectangle(cx, barY + barH / 2, barW, barH, T.panelFill || 0x2a2218).setDepth(1));
        if (level > 0) {
            const fillW = (level / maxLevel) * barW;
            this._addObj(this.add.rectangle(barX + fillW / 2, barY + barH / 2, fillW, barH, cat.color).setDepth(2));
        }

        const bonusText = this._getBonusText(cat.key, level);
        this._addObj(this.add.text(cx, cy + 40, bonusText, {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textPrimary || '#e8d8c0'
        }).setOrigin(0.5));

        if (level >= maxLevel) {
            this._addObj(this.add.text(cx, cy + 80, '최대 레벨', {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: T.textGold || '#ffcc44'
            }).setOrigin(0.5));
        } else {
            this._addObj(UIButton.create(this, cx, cy + 80, 120, 30, '훈련 (+1)', {
                variant: 'primary',
                fontSize: (T.fontSize && T.fontSize.body) || 12,
                disabled: !canTrain,
                onClick: () => {
                    if (!canTrain) return;
                    gs.trainingPoints--;
                    gs.training[cat.key] = (gs.training[cat.key] || 0) + 1;
                    GuildManager.addMessage(gs, `${cat.name} Lv.${gs.training[cat.key]} 달성!`);
                    SaveManager.save(gs);
                    UIToast.show(this, `${cat.name} Lv.${gs.training[cat.key]}!`, { color: cat.textColor });
                    this._drawContent();
                }
            }));
        }
    }

    _getBonusText(key, level) {
        if (level === 0) return '보너스 없음';
        switch (key) {
            case 'hp': return `HP +${level * 3}%`;
            case 'atk': return `ATK +${level * 3}%`;
            case 'survival': return `DEF +${level * 2}`;
            case 'recovery': return `쿨다운 -${level * 3}%`;
            default: return '';
        }
    }

    _drawPreview(gs, cats, x, y) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const panel = this._addObj(UIPanel.create(this, x, y, 1040, 160, { title: '현재 훈련 효과 미리보기' }));

        if (gs.roster.length === 0) {
            this._addObj(this.add.text(640, y + 80, '로스터에 용병이 없습니다', {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
            }).setOrigin(0.5));
            return;
        }

        let mx = x + 20;
        gs.roster.slice(0, 5).forEach(merc => {
            if (!merc.alive) return;
            merc._trainingRef = gs.training;
            const stats = merc.getStats();
            const base = merc.getBaseClass();

            this._addObj(this.add.text(mx, y + 30, `${base.icon} ${merc.name}`, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textPrimary || '#e8d8c0', fontStyle: 'bold'
            }));
            this._addObj(this.add.text(mx, y + 48, `HP:${stats.hp} ATK:${stats.atk} DEF:${stats.def}`, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textSecondary || '#b8a888'
            }));
            if (stats.skillCooldown) {
                this._addObj(this.add.text(mx, y + 64, `스킬CD: ${(stats.skillCooldown / 1000).toFixed(1)}초`, {
                    fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: '#bb88dd'
                }));
            }
            mx += 200;
        });
    }

    _addObj(obj) {
        if (!this._objs) this._objs = [];
        this._objs.push(obj);
        return obj;
    }
}
