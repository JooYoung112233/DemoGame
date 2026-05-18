class EliteRecruitScene extends Phaser.Scene {
    constructor() { super('EliteRecruitScene'); }

    init(data) { this.gameState = data.gameState; }

    create() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        this.add.rectangle(640, 360, 1280, 720, T.bg || 0x1a1510);
        const gs = this.gameState;

        // Header bg
        this.add.rectangle(640, 27, 1280, 55, T.headerBg || 0x1e1810).setOrigin(0.5);

        // Ornament lines
        const ornLine = this.add.graphics();
        ornLine.lineStyle(1, T.ornament || 0x8a7a4a, 0.4);
        ornLine.lineBetween(0, 54, 1280, 54);
        ornLine.lineStyle(1, T.divider || 0x5a4a2a, 0.3);
        ornLine.lineBetween(0, 56, 1280, 56);

        this.add.text(640, 25, '◈ 👑 고급 모집소 ◈', {
            fontSize: '20px', fontFamily: T.fontFamily || 'monospace', color: T.textGold || '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.goldText = this.add.text(1260, 25, `${gs.gold}G`, {
            fontSize: '16px', fontFamily: T.fontFamily || 'monospace', color: T.textGold || '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            variant: 'ghost', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        this.add.text(640, 55, '고등급 용병만 등장합니다 — 희귀 이상 보장, 전설 확률 증가', {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
        }).setOrigin(0.5);

        if (!gs.elitePool || gs.elitePool.length === 0) {
            this._refreshPool(gs);
        }

        this._drawPool(gs);
    }

    _refreshPool(gs) {
        gs.elitePool = [];
        const count = 3;
        for (let i = 0; i < count; i++) {
            const roll = Math.random();
            let rarity;
            if (roll < 0.1) rarity = 'legendary';
            else if (roll < 0.35) rarity = 'epic';
            else rarity = 'rare';

            const classKey = CLASS_KEYS[Math.floor(Math.random() * CLASS_KEYS.length)];
            const traits = getRandomTraits(rarity, classKey);
            const name = generateMercName();
            const merc = new Mercenary(classKey, rarity, name, traits);
            gs.elitePool.push(merc);
        }
        SaveManager.save(gs);
    }

    _drawPool(gs) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const pool = gs.elitePool || [];
        const maxRoster = GuildManager.getMaxRoster(gs);
        const rosterFull = gs.roster.length >= maxRoster;

        this.add.text(640, 80, `로스터: ${gs.roster.length}/${maxRoster}`, {
            fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: rosterFull ? '#ff4444' : (T.textMuted || '#887860')
        }).setOrigin(0.5);

        const refreshCost = 300;
        UIButton.create(this, 640, 670, 180, 30, `풀 갱신 (${refreshCost}G)`, {
            color: gs.gold >= refreshCost ? 0x443322 : 0x333333,
            hoverColor: gs.gold >= refreshCost ? 0x554433 : 0x333333,
            textColor: gs.gold >= refreshCost ? '#ffcc88' : '#555555',
            fontSize: 12,
            onClick: () => {
                if (gs.gold < refreshCost) { UIToast.show(this, '골드 부족', { color: '#ff6666' }); return; }
                GuildManager.spendGold(gs, refreshCost);
                this._refreshPool(gs);
                this.scene.restart({ gameState: gs });
            }
        });

        if (pool.length === 0) {
            this.add.text(640, 360, '모집 풀이 비었습니다', {
                fontSize: '14px', fontFamily: T.fontFamily || 'monospace', color: '#555566'
            }).setOrigin(0.5);
            return;
        }

        const cardW = 350, cardH = 480, gap = 30;
        const totalW = pool.length * cardW + (pool.length - 1) * gap;
        const startX = 640 - totalW / 2;

        pool.forEach((merc, idx) => {
            const cx = startX + idx * (cardW + gap) + cardW / 2;
            this._drawMercCard(gs, merc, idx, cx, 340, cardW, cardH, rosterFull);
        });
    }

    _drawMercCard(gs, merc, idx, cx, cy, w, h, rosterFull) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const base = merc.getBaseClass();
        const stats = merc.getStats();
        const rarData = RARITY_DATA[merc.rarity];
        const rarItem = ITEM_RARITY[merc.rarity] || ITEM_RARITY.common;
        const cost = merc.getHireCost() * 2;
        const canHire = !rosterFull && gs.gold >= cost;

        const bg = this.add.graphics();
        bg.fillStyle(T.panelFill || 0x2a2218, 1);
        bg.fillRoundedRect(cx - w / 2, cy - h / 2, w, h, 6);
        bg.lineStyle(2, rarItem.color, 0.6);
        bg.strokeRoundedRect(cx - w / 2, cy - h / 2, w, h, 6);

        let ty = cy - h / 2 + 15;

        this.add.text(cx, ty, `[${rarItem.name}]`, {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: rarItem.textColor
        }).setOrigin(0.5);
        ty += 22;

        this.add.text(cx, ty, `${base.icon} ${merc.name}`, {
            fontSize: '16px', fontFamily: T.fontFamily || 'monospace', color: '#ffffff', fontStyle: 'bold'
        }).setOrigin(0.5);
        ty += 22;

        this.add.text(cx, ty, `${base.name} Lv.${merc.level}`, {
            fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: T.textPrimary || '#e8d8c0'
        }).setOrigin(0.5);
        ty += 25;

        const statLines = [
            `HP: ${stats.hp}`,
            `ATK: ${stats.atk}  DEF: ${stats.def}`,
            `SPD: ${stats.moveSpeed}  RANGE: ${stats.range}`,
            `CRIT: ${Math.round(stats.critRate * 100)}%  x${stats.critDmg.toFixed(1)}`,
            `공속: ${(stats.attackSpeed / 1000).toFixed(1)}초`
        ];
        statLines.forEach(line => {
            this.add.text(cx, ty, line, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textSecondary || '#b8a888'
            }).setOrigin(0.5);
            ty += 18;
        });

        ty += 5;
        if (base.skillName) {
            this.add.text(cx, ty, `스킬: ${base.skillName}`, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: '#cc88ff', fontStyle: 'bold'
            }).setOrigin(0.5);
            ty += 15;
            this.add.text(cx, ty, base.skillDesc || '', {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: '#9966cc',
                wordWrap: { width: w - 30 }, align: 'center'
            }).setOrigin(0.5);
            ty += 25;
        }

        if (merc.traits.length > 0) {
            ty += 5;
            merc.traits.forEach(trait => {
                const tColor = trait.type === 'positive' ? '#44cc44' : trait.type === 'legendary' ? '#ffaa00' : '#ff6666';
                this.add.text(cx, ty, `${trait.name}: ${trait.desc}`, {
                    fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: tColor
                }).setOrigin(0.5);
                ty += 16;
            });
        }

        this.add.text(cx, cy + h / 2 - 55, `고용비: ${cost}G`, {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace', color: T.textGold || '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5);

        UIButton.create(this, cx, cy + h / 2 - 25, 140, 32, '고용', {
            color: canHire ? 0x886644 : 0x333333,
            hoverColor: canHire ? 0xaa8866 : 0x333333,
            textColor: canHire ? '#ffeecc' : '#555555',
            fontSize: 13,
            onClick: () => {
                if (!canHire) {
                    if (rosterFull) UIToast.show(this, '로스터가 가득 찼습니다', { color: '#ff6666' });
                    else UIToast.show(this, '골드가 부족합니다', { color: '#ff6666' });
                    return;
                }
                GuildManager.spendGold(gs, cost);
                gs.roster.push(merc);
                gs.elitePool.splice(idx, 1);
                GuildManager.addMessage(gs, `👑 ${merc.name} [${rarItem.name}] 고용!`);
                SaveManager.save(gs);
                UIToast.show(this, `${merc.name} 고용!`, { color: rarItem.textColor });
                this.scene.restart({ gameState: gs });
            }
        });
    }
}
