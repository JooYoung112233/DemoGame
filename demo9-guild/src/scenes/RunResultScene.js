class RunResultScene extends Phaser.Scene {
    constructor() { super('RunResultScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.result = data.result;
    }

    create() {
        const gs = this.gameState;
        const r = this.result;
        const zone = ZONE_DATA[r.zoneKey];

        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);

        const banner = r.success ? '탐사 성공!' : '탐사 실패...';
        const bannerColor = r.success ? '#44ff88' : '#ff4444';
        this.add.text(640, 40, banner, {
            fontSize: '28px', fontFamily: 'monospace', color: bannerColor, fontStyle: 'bold'
        }).setOrigin(0.5);

        const zoneInfo = r.zoneLevelUp
            ? `${zone.name} — ${r.rounds}라운드  |  구역 레벨 업!`
            : `${zone.name} — ${r.rounds}라운드`;
        this.add.text(640, 75, zoneInfo, {
            fontSize: '13px', fontFamily: 'monospace', color: r.zoneLevelUp ? '#ffaa44' : '#888899'
        }).setOrigin(0.5);

        let cy = 110;

        const rewardPanel = UIPanel.create(this, 40, cy, 560, 200, { title: '보상' });

        this.add.text(70, cy + 35, `골드: +${r.goldEarned}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        });
        this.add.text(70, cy + 60, `길드 XP: +${r.xpEarned}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#4488ff'
        });

        this.add.text(70, cy + 90, '전리품:', {
            fontSize: '13px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
        });

        if (r.loot.length === 0) {
            this.add.text(80, cy + 112, '(없음)', {
                fontSize: '12px', fontFamily: 'monospace', color: '#555566'
            });
        } else {
            r.loot.forEach((item, idx) => {
                const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
                this.add.text(80, cy + 112 + idx * 20, `• ${item.name} [${rarity.name}]`, {
                    fontSize: '11px', fontFamily: 'monospace', color: rarity.textColor
                });
            });
        }

        const casualtyPanel = UIPanel.create(this, 620, cy, 620, 200, { title: '용병 상태' });

        if (r.casualties.length > 0) {
            this.add.text(650, cy + 35, '전사자 (영구 사망):', {
                fontSize: '13px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
            });
            r.casualties.forEach((merc, idx) => {
                const base = merc.getBaseClass();
                this.add.text(660, cy + 58 + idx * 20, `☠ ${merc.name} (${base.name} Lv.${merc.level})`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#ff6666'
                });
            });
        } else {
            this.add.text(930, cy + 100, '전원 생존!', {
                fontSize: '14px', fontFamily: 'monospace', color: '#44ff88'
            }).setOrigin(0.5);
        }

        if (r.survivors.length > 0) {
            const survY = cy + 35 + (r.casualties.length > 0 ? r.casualties.length * 20 + 30 : 0);
            this.add.text(650, survY, '생존자:', {
                fontSize: '12px', fontFamily: 'monospace', color: '#aaaacc'
            });
            r.survivors.forEach((merc, idx) => {
                const base = merc.getBaseClass();
                const stats = merc.getStats();
                const hpRatio = merc.currentHp / stats.hp;
                const hpColor = hpRatio > 0.6 ? '#44ff88' : hpRatio > 0.3 ? '#ffaa44' : '#ff4444';
                this.add.text(660, survY + 20 + idx * 18, `${base.icon} ${merc.name} HP:${merc.currentHp}/${stats.hp}`, {
                    fontSize: '11px', fontFamily: 'monospace', color: hpColor
                });
            });
        }

        cy = 330;
        const xpPanel = UIPanel.create(this, 40, cy, 1200, 130, { title: '용병 성장' });

        const allParty = [...r.survivors, ...r.casualties];
        let xpX = 60;
        r.survivors.forEach(merc => {
            const bossBonus = r.success ? 15 : 0;
            const xpGain = Math.floor(r.xpEarned * 0.5 + r.rounds * 5 + bossBonus);
            const leveled = merc.gainXp(xpGain);
            const base = merc.getBaseClass();

            const affinityGain = Math.floor(10 + r.rounds * 3 + (r.success ? 10 : 0));
            const affinityLeveled = merc.gainAffinityXp(r.zoneKey, affinityGain);

            let text = `${base.icon} ${merc.name}: +${xpGain} XP`;
            if (leveled) text += ` → Lv.${merc.level}!`;
            text += `  |  친화도 +${affinityGain}`;
            if (affinityLeveled) text += ` → 친화 Lv.${merc.affinityLevel[r.zoneKey]}!`;

            this.add.text(xpX, cy + 35, text, {
                fontSize: '11px', fontFamily: 'monospace',
                color: leveled || affinityLeveled ? '#ffaa44' : '#aaaacc'
            });
            xpX = 60;
            cy += 18;
        });

        this._applyResults();

        if (this._consignResults && this._consignResults.length > 0) {
            const sold = this._consignResults.filter(c => c.sold);
            const unsold = this._consignResults.filter(c => !c.sold);
            const totalGold = sold.reduce((s, c) => s + c.price, 0);
            if (sold.length > 0 || unsold.length > 0) {
                cy = Math.max(cy + 20, 480);
                const panelH = 30 + this._consignResults.length * 18 + 10;
                UIPanel.create(this, 40, cy, 1200, Math.min(panelH, 120), { title: `위탁 판매 결과 (+${totalGold}G)` });
                let conY = cy + 30;
                this._consignResults.forEach(cr => {
                    const label = cr.sold ? `✓ ${cr.item.name} → ${cr.price}G` : `✗ ${cr.item.name} — 미판매`;
                    this.add.text(70, conY, label, {
                        fontSize: '10px', fontFamily: 'monospace', color: cr.sold ? '#88ffaa' : '#886666'
                    });
                    conY += 18;
                });
            }
        }

        UIButton.create(this, 640, 660, 200, 44, '본진 복귀', {
            color: 0xffaa44, hoverColor: 0xffcc66, textColor: '#000000', fontSize: 16,
            onClick: () => {
                SaveManager.save(gs);
                this.scene.start('EventScene', { gameState: gs });
            }
        });
    }

    _applyResults() {
        const gs = this.gameState;
        const r = this.result;

        GuildManager.addGold(gs, r.goldEarned);
        GuildManager.addMessage(gs, `${ZONE_DATA[r.zoneKey].name} ${r.success ? '성공' : '실패'} — +${r.goldEarned}G`);

        GuildManager.addXp(gs, r.xpEarned);

        if (r.zoneLevelUp && r.zoneKey) {
            gs.zoneLevel[r.zoneKey] = (gs.zoneLevel[r.zoneKey] || 1) + 1;
            GuildManager.addMessage(gs, `${ZONE_DATA[r.zoneKey].name} 구역 Lv.${gs.zoneLevel[r.zoneKey]} 달성!`);
        }

        r.loot.forEach(item => {
            if (!StorageManager.addItem(gs, item)) {
                GuildManager.addMessage(gs, `보관함 가득! ${item.name} 손실`);
            }
        });

        r.casualties.forEach(merc => {
            merc.alive = false;
            for (const slot of ['weapon', 'armor', 'accessory']) {
                merc.equipment[slot] = null;
            }
            gs.roster = gs.roster.filter(m => m.id !== merc.id);
            GuildManager.addMessage(gs, `${merc.name} 영구 사망`);
        });

        this._consignResults = AuctionScene.processConsignments(gs);

        SaveManager.save(gs);
    }
}
