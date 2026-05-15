class TrainingScene extends Phaser.Scene {
    constructor() { super('TrainingScene'); }

    init(data) { this.gameState = data.gameState; }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
        const gs = this.gameState;

        this.add.text(640, 25, '🏋 훈련소', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        this.add.text(640, 55, '길드 레벨업 시 획득한 훈련 포인트로 전체 용병을 영구 강화합니다', {
            fontSize: '11px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5);

        this._drawContent();
    }

    _drawContent() {
        if (this._objs) this._objs.forEach(o => o.destroy && o.destroy());
        this._objs = [];

        const gs = this.gameState;
        const t = gs.training;

        this._objs.push(this.add.text(640, 85, `보유 포인트: ${gs.trainingPoints}`, {
            fontSize: '16px', fontFamily: 'monospace', color: gs.trainingPoints > 0 ? '#44ff88' : '#ff4444', fontStyle: 'bold'
        }).setOrigin(0.5));

        const cats = [
            {
                key: 'hp', name: '체력 단련', icon: '❤',
                desc: '전체 용병 HP +3% / 포인트',
                color: 0xff4444, textColor: '#ff6666'
            },
            {
                key: 'atk', name: '공격 훈련', icon: '⚔',
                desc: '전체 용병 ATK +3% / 포인트',
                color: 0xff8844, textColor: '#ffaa66'
            },
            {
                key: 'survival', name: '방어 훈련', icon: '🛡',
                desc: '전체 용병 DEF +2 / 포인트',
                color: 0x4488ff, textColor: '#6699ff'
            },
            {
                key: 'recovery', name: '전투 숙련', icon: '⚡',
                desc: '전체 용병 스킬 쿨다운 -3% / 포인트',
                color: 0xcc44ff, textColor: '#dd66ff'
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
        const gs = this.gameState;
        const level = training[cat.key] || 0;
        const maxLevel = 10;
        const canTrain = gs.trainingPoints > 0 && level < maxLevel;

        const bg = this._addObj(this.add.graphics());
        bg.fillStyle(0x151525, 1);
        bg.fillRoundedRect(cx - w / 2, cy - 120, w, 240, 6);
        bg.lineStyle(2, cat.color, 0.4);
        bg.strokeRoundedRect(cx - w / 2, cy - 120, w, 240, 6);

        this._addObj(this.add.text(cx, cy - 95, cat.icon, {
            fontSize: '28px'
        }).setOrigin(0.5));

        this._addObj(this.add.text(cx, cy - 60, cat.name, {
            fontSize: '15px', fontFamily: 'monospace', color: cat.textColor, fontStyle: 'bold'
        }).setOrigin(0.5));

        this._addObj(this.add.text(cx, cy - 38, cat.desc, {
            fontSize: '10px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5));

        this._addObj(this.add.text(cx, cy - 10, `Lv. ${level} / ${maxLevel}`, {
            fontSize: '18px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5));

        const barW = w - 40;
        const barH = 10;
        const barX = cx - barW / 2;
        const barY = cy + 15;
        const barBg = this._addObj(this.add.rectangle(cx, barY + barH / 2, barW, barH, 0x222233).setDepth(1));
        if (level > 0) {
            const fillW = (level / maxLevel) * barW;
            this._addObj(this.add.rectangle(barX + fillW / 2, barY + barH / 2, fillW, barH, cat.color).setDepth(2));
        }

        const bonusText = this._getBonusText(cat.key, level);
        this._addObj(this.add.text(cx, cy + 40, bonusText, {
            fontSize: '11px', fontFamily: 'monospace', color: '#aaaacc'
        }).setOrigin(0.5));

        if (level >= maxLevel) {
            this._addObj(this.add.text(cx, cy + 80, '최대 레벨', {
                fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44'
            }).setOrigin(0.5));
        } else {
            this._addObj(UIButton.create(this, cx, cy + 80, 120, 30, '훈련 (+1)', {
                color: canTrain ? 0x446644 : 0x333333,
                hoverColor: canTrain ? 0x558855 : 0x333333,
                textColor: canTrain ? '#44ff88' : '#555555',
                fontSize: 12,
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
        const panel = this._addObj(UIPanel.create(this, x, y, 1040, 160, { title: '현재 훈련 효과 미리보기' }));

        if (gs.roster.length === 0) {
            this._addObj(this.add.text(640, y + 80, '로스터에 용병이 없습니다', {
                fontSize: '12px', fontFamily: 'monospace', color: '#555566'
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
                fontSize: '11px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
            }));
            this._addObj(this.add.text(mx, y + 48, `HP:${stats.hp} ATK:${stats.atk} DEF:${stats.def}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#8888aa'
            }));
            if (stats.skillCooldown) {
                this._addObj(this.add.text(mx, y + 64, `스킬CD: ${(stats.skillCooldown / 1000).toFixed(1)}초`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#cc88ff'
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
