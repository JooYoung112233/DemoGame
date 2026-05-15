class RecruitScene extends Phaser.Scene {
    constructor() { super('RecruitScene'); }

    init(data) { this.gameState = data.gameState; }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
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
        const totalW = gs.recruitPool.length * (cardW + 15) - 15;
        const startX = 640 - totalW / 2;

        gs.recruitPool.forEach((merc, idx) => {
            this._drawRecruitCard(merc, startX + idx * (cardW + 15), 80, cardW);
        });
    }

    _drawRecruitCard(merc, x, y, w) {
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

        let cy = y + 15;

        this.add.text(x + w / 2, cy, base.icon, { fontSize: '32px' }).setOrigin(0.5);
        cy += 40;

        this.add.text(x + w / 2, cy, merc.name, {
            fontSize: '13px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }).setOrigin(0.5);
        cy += 20;

        this.add.text(x + w / 2, cy, `${base.name}  [${rarity.name}]`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5);
        cy += 25;

        const line = this.add.graphics();
        line.lineStyle(1, 0x333355, 0.5);
        line.lineBetween(x + 10, cy, x + w - 10, cy);
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
            this.add.text(x + 15, cy, s, {
                fontSize: '11px', fontFamily: 'monospace', color: '#aaaacc'
            });
            cy += 17;
        });
        cy += 5;

        const line2 = this.add.graphics();
        line2.lineStyle(1, 0x333355, 0.5);
        line2.lineBetween(x + 10, cy, x + w - 10, cy);
        cy += 10;

        this.add.text(x + 15, cy, '특성:', {
            fontSize: '11px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
        });
        cy += 18;

        merc.traits.forEach(trait => {
            let color = '#44cc44';
            let sym = '✦';
            if (trait.type === 'negative') { color = '#ff6666'; sym = '✧'; }
            if (trait.type === 'legendary') { color = '#ffaa00'; sym = '★'; }

            this.add.text(x + 15, cy, `${sym} ${trait.name}`, {
                fontSize: '10px', fontFamily: 'monospace', color
            });
            this.add.text(x + 15, cy + 13, `  ${trait.desc}`, {
                fontSize: '9px', fontFamily: 'monospace', color: '#777788'
            });
            cy += 28;
        });

        cy = y + 530 - 50;

        const line3 = this.add.graphics();
        line3.lineStyle(1, 0x333355, 0.5);
        line3.lineBetween(x + 10, cy, x + w - 10, cy);
        cy += 12;

        const disabled = !canAfford || rosterFull;
        let btnLabel = `고용 (${cost}G)`;
        if (rosterFull) btnLabel = '로스터 가득';
        else if (!canAfford) btnLabel = `${cost}G 필요`;

        UIButton.create(this, x + w / 2, cy + 12, w - 20, 30, btnLabel, {
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
    }
}
