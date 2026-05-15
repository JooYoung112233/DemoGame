class TempleScene extends Phaser.Scene {
    constructor() { super('TempleScene'); }

    init(data) { this.gameState = data.gameState; }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
        const gs = this.gameState;

        this.add.text(640, 25, '⛪ 신전', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.goldText = this.add.text(1260, 25, `${gs.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        this.add.text(640, 55, '신의 축복으로 용병을 치유하고, 출발 전 버프를 구매합니다', {
            fontSize: '11px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5);

        this._drawContent();
    }

    _drawContent() {
        if (this._objs) this._objs.forEach(o => o.destroy && o.destroy());
        this._objs = [];
        const gs = this.gameState;

        this._drawHealSection(gs, 40, 85, 390);
        this._drawRevivalSection(gs, 440, 85, 370);
        this._drawBlessSection(gs, 820, 85, 440);
    }

    _add(o) { if (!this._objs) this._objs = []; this._objs.push(o); return o; }

    _drawHealSection(gs, x, y, w) {
        this._add(UIPanel.create(this, x, y, w, 560, { title: '전체 치유' }));

        const injured = gs.roster.filter(m => m.alive && m.currentHp < m.getStats().hp);

        this._add(this.add.text(x + w / 2, y + 30, `부상 용병: ${injured.length}명`, {
            fontSize: '12px', fontFamily: 'monospace', color: injured.length > 0 ? '#ff8844' : '#44ff88'
        }).setOrigin(0.5));

        if (injured.length > 0) {
            const healAllCost = injured.length * 20;
            this._add(UIButton.create(this, x + w / 2, y + 58, 200, 30, `전체 치유 (${healAllCost}G)`, {
                color: gs.gold >= healAllCost ? 0x446644 : 0x333333,
                hoverColor: gs.gold >= healAllCost ? 0x558855 : 0x333333,
                textColor: gs.gold >= healAllCost ? '#44ff88' : '#555555',
                fontSize: 12,
                onClick: () => {
                    if (gs.gold < healAllCost) return;
                    GuildManager.spendGold(gs, healAllCost);
                    injured.forEach(m => { m.currentHp = m.getStats().hp; });
                    GuildManager.addMessage(gs, `신전 전체 치유 (${injured.length}명)`);
                    SaveManager.save(gs);
                    UIToast.show(this, `${injured.length}명 전원 치유!`, { color: '#44ff88' });
                    this.goldText.setText(`${gs.gold}G`);
                    this._drawContent();
                }
            }));
        }

        let cy = y + 95;
        gs.roster.forEach(merc => {
            if (!merc.alive) return;
            if (cy > y + 540) return;
            const stats = merc.getStats();
            const base = merc.getBaseClass();
            const hpRatio = merc.currentHp / stats.hp;
            const isHurt = merc.currentHp < stats.hp;
            const healCost = 20;

            const bg = this._add(this.add.graphics());
            bg.fillStyle(isHurt ? 0x2a1a1a : 0x1a2a1a, 1);
            bg.fillRoundedRect(x + 10, cy, w - 20, 44, 3);

            this._add(this.add.text(x + 20, cy + 6, `${base.icon} ${merc.name}`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
            }));

            const hpColor = hpRatio > 0.6 ? '#44ff88' : hpRatio > 0.3 ? '#ffaa44' : '#ff4444';
            this._add(this.add.text(x + 200, cy + 6, `HP: ${merc.currentHp}/${stats.hp}`, {
                fontSize: '11px', fontFamily: 'monospace', color: hpColor
            }));

            const barW = 150, barH = 6;
            this._add(this.add.rectangle(x + 200 + barW / 2, cy + 28, barW, barH, 0x331111));
            if (hpRatio > 0) {
                this._add(this.add.rectangle(x + 200 + (barW * hpRatio) / 2, cy + 28, barW * hpRatio, barH,
                    hpRatio > 0.6 ? 0x44ff88 : hpRatio > 0.3 ? 0xffaa44 : 0xff4444));
            }

            if (isHurt) {
                this._add(UIButton.create(this, x + w - 60, cy + 22, 80, 24, `치유 ${healCost}G`, {
                    color: gs.gold >= healCost ? 0x446644 : 0x333333,
                    hoverColor: gs.gold >= healCost ? 0x558855 : 0x333333,
                    textColor: gs.gold >= healCost ? '#44ff88' : '#555555',
                    fontSize: 10,
                    onClick: () => {
                        if (gs.gold < healCost) return;
                        GuildManager.spendGold(gs, healCost);
                        merc.currentHp = stats.hp;
                        SaveManager.save(gs);
                        UIToast.show(this, `${merc.name} 치유 완료!`, { color: '#44ff88' });
                        this.goldText.setText(`${gs.gold}G`);
                        this._drawContent();
                    }
                }));
            } else {
                this._add(this.add.text(x + w - 60, cy + 14, '만렙', {
                    fontSize: '10px', fontFamily: 'monospace', color: '#44ff88'
                }).setOrigin(0.5));
            }

            cy += 50;
        });
    }

    _drawRevivalSection(gs, x, y, w) {
        this._add(UIPanel.create(this, x, y, w, 560, { title: '부활권 (500G)' }));

        this._add(this.add.text(x + w / 2, y + 30, '용병 1명에게 부활권을 장착합니다', {
            fontSize: '10px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5));
        this._add(this.add.text(x + w / 2, y + 46, '사망 시: HP 50% 부활, 장비 전량 손실', {
            fontSize: '10px', fontFamily: 'monospace', color: '#ff8888'
        }).setOrigin(0.5));

        let cy = y + 70;
        const revivalCost = 500;

        gs.roster.forEach(merc => {
            if (!merc.alive) return;
            if (cy > y + 530) return;
            const base = merc.getBaseClass();
            const hasRevival = !!merc.revivalToken;
            const canBuy = !hasRevival && gs.gold >= revivalCost;

            const bg = this._add(this.add.graphics());
            bg.fillStyle(hasRevival ? 0x2a2a1a : 0x1a1a2a, 1);
            bg.fillRoundedRect(x + 10, cy, w - 20, 44, 3);
            bg.lineStyle(1, hasRevival ? 0x886644 : 0x333355, 0.3);
            bg.strokeRoundedRect(x + 10, cy, w - 20, 44, 3);

            this._add(this.add.text(x + 20, cy + 6, `${base.icon} ${merc.name}`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
            }));

            const rarityColor = RARITY_DATA[merc.rarity]?.color || '#aaaaaa';
            this._add(this.add.text(x + 20, cy + 24, `Lv.${merc.level} ${RARITY_DATA[merc.rarity]?.name || merc.rarity}`, {
                fontSize: '9px', fontFamily: 'monospace', color: rarityColor
            }));

            if (hasRevival) {
                this._add(this.add.text(x + w - 60, cy + 14, '✨ 부활권', {
                    fontSize: '11px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
                }).setOrigin(0.5));
            } else {
                this._add(UIButton.create(this, x + w - 60, cy + 22, 80, 24, `${revivalCost}G`, {
                    color: canBuy ? 0x664422 : 0x333333,
                    hoverColor: canBuy ? 0x885533 : 0x333333,
                    textColor: canBuy ? '#ffaa44' : '#555555',
                    fontSize: 10,
                    onClick: () => {
                        if (!canBuy) return;
                        GuildManager.spendGold(gs, revivalCost);
                        merc.revivalToken = true;
                        GuildManager.addMessage(gs, `${merc.name}에게 부활권 장착`);
                        SaveManager.save(gs);
                        UIToast.show(this, `${merc.name}에게 부활권 장착!`, { color: '#ffaa44' });
                        this.goldText.setText(`${gs.gold}G`);
                        this._drawContent();
                    }
                }));
            }
            cy += 50;
        });
    }

    _drawBlessSection(gs, x, y, w) {
        this._add(UIPanel.create(this, x, y, w, 560, { title: '출발 축복 (다음 전투 적용)' }));

        if (!gs.blessings) gs.blessings = [];

        const blessings = [
            { id: 'bless_atk', name: '힘의 축복', desc: '전투 시작 ATK +10%', cost: 200, icon: '⚔', color: '#ff8844' },
            { id: 'bless_def', name: '보호의 축복', desc: '전투 시작 DEF +10%', cost: 200, icon: '🛡', color: '#4488ff' },
            { id: 'bless_hp', name: '생명의 축복', desc: '전투 시작 HP +10%', cost: 200, icon: '❤', color: '#ff4444' },
            { id: 'bless_crit', name: '행운의 축복', desc: 'CRIT +10%, 크리피해 +20%', cost: 300, icon: '🎯', color: '#ffcc44' },
            { id: 'bless_speed', name: '신속의 축복', desc: '이동/공격속도 +10%', cost: 250, icon: '⚡', color: '#cc44ff' }
        ];

        let cy = y + 35;
        blessings.forEach(bless => {
            const active = gs.blessings.includes(bless.id);
            const canBuy = !active && gs.gold >= bless.cost;

            const bg = this._add(this.add.graphics());
            bg.fillStyle(active ? 0x1a2a3a : 0x1a1a2e, 1);
            bg.fillRoundedRect(x + 10, cy, w - 20, 60, 3);
            bg.lineStyle(1, active ? 0x4488ff : 0x333355, 0.4);
            bg.strokeRoundedRect(x + 10, cy, w - 20, 60, 3);

            this._add(this.add.text(x + 25, cy + 8, bless.icon, { fontSize: '18px' }));

            this._add(this.add.text(x + 55, cy + 8, bless.name, {
                fontSize: '13px', fontFamily: 'monospace', color: bless.color, fontStyle: 'bold'
            }));

            this._add(this.add.text(x + 55, cy + 28, bless.desc, {
                fontSize: '10px', fontFamily: 'monospace', color: '#888899'
            }));

            this._add(this.add.text(x + 55, cy + 42, `${bless.cost}G`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#ffcc44'
            }));

            if (active) {
                this._add(this.add.text(x + w - 60, cy + 22, '✓ 활성', {
                    fontSize: '11px', fontFamily: 'monospace', color: '#44ff88'
                }).setOrigin(0.5));
            } else {
                this._add(UIButton.create(this, x + w - 60, cy + 30, 80, 26, '구매', {
                    color: canBuy ? 0x446644 : 0x333333,
                    hoverColor: canBuy ? 0x558855 : 0x333333,
                    textColor: canBuy ? '#44ff88' : '#555555',
                    fontSize: 11,
                    onClick: () => {
                        if (!canBuy) return;
                        GuildManager.spendGold(gs, bless.cost);
                        gs.blessings.push(bless.id);
                        GuildManager.addMessage(gs, `${bless.name} 구매!`);
                        SaveManager.save(gs);
                        UIToast.show(this, `${bless.name} 활성화!`, { color: bless.color });
                        this.goldText.setText(`${gs.gold}G`);
                        this._drawContent();
                    }
                }));
            }
            cy += 68;
        });

        this._add(this.add.text(x + w / 2, cy + 10, '축복은 다음 전투 1회에만 적용됩니다', {
            fontSize: '10px', fontFamily: 'monospace', color: '#666677'
        }).setOrigin(0.5));
    }
}
