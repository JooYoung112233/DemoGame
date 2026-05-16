class DeployScene extends Phaser.Scene {
    constructor() { super('DeployScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.selectedZone = data.selectedZone || null;
        this.deployedIds = data.deployedIds || [];
        this.deployMode = data.deployMode || 'main';  // 'main' | 'sub'
    }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
        this._drawUI();
    }

    _drawUI() {
        const gs = this.gameState;

        this.add.text(640, 20, '출발 게이트', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        UIButton.create(this, 80, 20, 100, 30, '← 마을', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        // 메인/서브 모드 토글
        const isMain = this.deployMode === 'main';
        const activeExp = (gs.activeExpeditions || []).length;
        const maxSlots = ExpeditionManager.getMaxSlots(gs);
        UIButton.create(this, 970, 20, 130, 30, '⚔ 메인 도전', {
            color: isMain ? 0x884422 : 0x333344,
            hoverColor: 0xaa5533, textColor: isMain ? '#ffcc88' : '#888899', fontSize: 12,
            onClick: () => { if (!isMain) { this.deployMode = 'main'; this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: 'main' }); } }
        });
        UIButton.create(this, 1120, 20, 140, 30, `📦 서브 파견 ${activeExp}/${maxSlots}`, {
            color: !isMain ? 0x224488 : 0x333344,
            hoverColor: 0x3355aa, textColor: !isMain ? '#88ccff' : '#888899', fontSize: 12,
            onClick: () => { if (isMain) { this.deployMode = 'sub'; this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: 'sub' }); } }
        });

        const modeDesc = isMain
            ? '메인 도전: 직접 전투. 스킬 발동 가능, 보상 1.5배, 구역 레벨업'
            : '서브 파견: 시간 경과형 자동. 일반공격만, 깬 레벨까지만 파밍';
        this.add.text(640, 47, modeDesc, {
            fontSize: '11px', fontFamily: 'monospace', color: isMain ? '#ffaa66' : '#88aaff'
        }).setOrigin(0.5);

        this.add.text(30, 55, '구역 선택', {
            fontSize: '14px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
        });

        const zoneStartX = 30;
        let zoneX = zoneStartX;
        ZONE_KEYS.forEach(key => {
            this._drawZoneCard(key, zoneX, 80, 390, 160);
            zoneX += 410;
        });

        this.add.text(30, 255, '파티 편성', {
            fontSize: '14px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
        });

        const maxDeploy = GuildManager.getMaxDeploy(gs);
        this.add.text(130, 257, `(${this.deployedIds.length}/${maxDeploy})`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#888899'
        });

        this._drawDeploySlots(30, 280, maxDeploy);
        this._drawRosterPick(30, 440);
        this._drawDepartButton();
    }

    _drawZoneCard(zoneKey, x, y, w, h) {
        const zone = ZONE_DATA[zoneKey];
        const gs = this.gameState;
        const isLocked = gs.zoneLevel[zoneKey] === 0;
        const isSelected = this.selectedZone === zoneKey;

        const bg = this.add.graphics();
        if (isLocked) {
            bg.fillStyle(0x181822, 1);
            bg.fillRoundedRect(x, y, w, h, 5);
            bg.lineStyle(1, 0x333344, 0.5);
            bg.strokeRoundedRect(x, y, w, h, 5);
        } else {
            bg.fillStyle(isSelected ? 0x223344 : 0x151525, 1);
            bg.fillRoundedRect(x, y, w, h, 5);
            bg.lineStyle(2, isSelected ? zone.color : 0x333355, isSelected ? 0.9 : 0.5);
            bg.strokeRoundedRect(x, y, w, h, 5);
        }

        this.add.text(x + w / 2, y + 20, zone.icon, { fontSize: '28px' }).setOrigin(0.5);
        this.add.text(x + w / 2, y + 55, zone.name, {
            fontSize: '14px', fontFamily: 'monospace',
            color: isLocked ? '#555566' : zone.textColor, fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(x + w / 2, y + 75, zone.subtitle, {
            fontSize: '11px', fontFamily: 'monospace', color: '#777788'
        }).setOrigin(0.5);

        if (isLocked) {
            this.add.text(x + w / 2, y + 100, `🔒 Lv.${zone.unlockLevel} 필요`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#555566'
            }).setOrigin(0.5);
        } else {
            const zLv = gs.zoneLevel[zoneKey];
            const rounds = getMaxRounds(zLv);
            this.add.text(x + w / 2, y + 100, `구역 Lv.${zLv}  |  ${rounds}라운드`, {
                fontSize: '11px', fontFamily: 'monospace', color: '#888899'
            }).setOrigin(0.5);
            this.add.text(x + w / 2, y + 120, zone.desc, {
                fontSize: '9px', fontFamily: 'monospace', color: '#667788',
                wordWrap: { width: w - 20 }, align: 'center'
            }).setOrigin(0.5);

            const hitZone = this.add.zone(x + w / 2, y + h / 2, w, h).setInteractive({ useHandCursor: true });
            hitZone.on('pointerdown', () => {
                this.selectedZone = zoneKey;
                this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds });
            });
        }
    }

    _drawDeploySlots(x, y, maxDeploy) {
        const gs = this.gameState;
        const slotW = 230;

        for (let i = 0; i < maxDeploy; i++) {
            const sx = x + i * (slotW + 10);
            const merc = gs.roster.find(m => m.id === this.deployedIds[i]);

            const bg = this.add.graphics();
            bg.fillStyle(merc ? 0x1a2a3a : 0x151525, 1);
            bg.fillRoundedRect(sx, y, slotW, 140, 4);
            bg.lineStyle(1, merc ? 0x446688 : 0x333355, 0.6);
            bg.strokeRoundedRect(sx, y, slotW, 140, 4);

            if (merc) {
                const base = merc.getBaseClass();
                const rarity = RARITY_DATA[merc.rarity];
                const stats = merc.getStats();

                this.add.text(sx + 10, y + 8, `${base.icon} ${merc.name}`, {
                    fontSize: '12px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
                });
                this.add.text(sx + slotW - 10, y + 8, `Lv.${merc.level}`, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa'
                }).setOrigin(1, 0);
                this.add.text(sx + 10, y + 28, `${base.name} [${rarity.name}]`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#777788'
                });
                this.add.text(sx + 10, y + 46, `HP:${merc.currentHp}/${stats.hp}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#8888aa'
                });
                this.add.text(sx + 10, y + 62, `ATK:${stats.atk} DEF:${stats.def} SPD:${stats.moveSpeed}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#8888aa'
                });

                const hpRatio = merc.currentHp / stats.hp;
                const hpBar = this.add.graphics();
                hpBar.fillStyle(0x333344, 1);
                hpBar.fillRect(sx + 10, y + 80, slotW - 20, 4);
                const hpColor = hpRatio > 0.6 ? 0x44ff88 : hpRatio > 0.3 ? 0xffaa44 : 0xff4444;
                hpBar.fillStyle(hpColor, 1);
                hpBar.fillRect(sx + 10, y + 80, (slotW - 20) * hpRatio, 4);

                const traits = merc.traits.map(t => {
                    const sym = t.type === 'positive' ? '✦' : t.type === 'legendary' ? '★' : '✧';
                    return sym + t.name;
                }).join(' ');
                this.add.text(sx + 10, y + 92, traits, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#667788',
                    wordWrap: { width: slotW - 20 }
                });

                UIButton.create(this, sx + slotW / 2, y + 125, 80, 22, '해제', {
                    color: 0x555555, hoverColor: 0x666666, textColor: '#cccccc', fontSize: 10,
                    onClick: () => {
                        this.deployedIds = this.deployedIds.filter(id => id !== merc.id);
                        this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds });
                    }
                });
            } else {
                this.add.text(sx + slotW / 2, y + 70, `슬롯 ${i + 1}`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#444455'
                }).setOrigin(0.5);
            }
        }
    }

    _drawRosterPick(x, y) {
        const gs = this.gameState;
        const available = gs.roster.filter(m =>
            m.isDeployable() &&
            !this.deployedIds.includes(m.id) &&
            !ExpeditionManager.isOnExpedition(gs, m.id)
        );

        this.add.text(x, y, '대기 용병 (클릭하여 편성)', {
            fontSize: '12px', fontFamily: 'monospace', color: '#888899'
        });

        if (available.length === 0) {
            this.add.text(x + 200, y + 60, '편성 가능한 용병이 없습니다', {
                fontSize: '12px', fontFamily: 'monospace', color: '#555566'
            });
            return;
        }

        let cx = x;
        const maxDeploy = GuildManager.getMaxDeploy(gs);
        available.forEach(merc => {
            const base = merc.getBaseClass();
            const rarity = RARITY_DATA[merc.rarity];
            const cardW = 150;
            const canAdd = this.deployedIds.length < maxDeploy;

            const bg = this.add.graphics();
            bg.fillStyle(0x151525, 1);
            bg.fillRoundedRect(cx, y + 22, cardW, 55, 3);
            bg.lineStyle(1, rarity.color, 0.3);
            bg.strokeRoundedRect(cx, y + 22, cardW, 55, 3);

            this.add.text(cx + 8, y + 28, `${base.icon} ${merc.name}`, {
                fontSize: '11px', fontFamily: 'monospace', color: rarity.textColor
            });
            this.add.text(cx + 8, y + 44, `Lv.${merc.level} ${base.name}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#777788'
            });
            this.add.text(cx + 8, y + 58, `HP:${merc.currentHp}/${merc.getStats().hp}`, {
                fontSize: '9px', fontFamily: 'monospace', color: '#8888aa'
            });

            if (canAdd) {
                const hitZone = this.add.zone(cx + cardW / 2, y + 49, cardW, 55).setInteractive({ useHandCursor: true });
                hitZone.on('pointerdown', () => {
                    this.deployedIds.push(merc.id);
                    this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds });
                });
            }

            cx += cardW + 10;
        });
    }

    _drawDepartButton() {
        const gs = this.gameState;
        const isMain = this.deployMode === 'main';
        const activeExp = (gs.activeExpeditions || []).length;
        const maxSlots = ExpeditionManager.getMaxSlots(gs);
        const slotsFull = !isMain && activeExp >= maxSlots;

        const canDepart = this.selectedZone && this.deployedIds.length > 0 && !slotsFull;
        const btnLabel = isMain ? '출발 (메인 전투)' : `파견 시작 (서브)`;

        UIButton.create(this, 640, 690, 220, 40, btnLabel, {
            color: canDepart ? (isMain ? 0xaa4422 : 0x4488cc) : 0x333333,
            hoverColor: isMain ? 0xcc5533 : 0x55aaee,
            textColor: canDepart ? '#ffffff' : '#555555',
            fontSize: 16,
            disabled: !canDepart,
            onClick: () => {
                const party = this.deployedIds.map(id => gs.roster.find(m => m.id === id)).filter(Boolean);

                if (isMain) {
                    // 메인 전투 (기존 흐름)
                    gs.runCount++;
                    SaveManager.save(gs);
                    const sceneMap = {
                        bloodpit: 'BattleScene',
                        cargo: 'CargoBattleScene',
                        blackout: 'BlackoutBattleScene'
                    };
                    const targetScene = sceneMap[this.selectedZone] || 'BattleScene';
                    this.scene.start(targetScene, {
                        gameState: gs,
                        zoneKey: this.selectedZone,
                        party
                    });
                } else {
                    // 서브 파견 (시간 경과형)
                    const exp = ExpeditionManager.dispatch(gs, this.selectedZone, party);
                    if (!exp) {
                        UIToast.show(this, '파견 실패 (슬롯/구역 확인)', { color: '#ff6644' });
                        return;
                    }
                    const mins = Math.ceil(exp.durationMs / 60000);
                    UIToast.show(this, `${ZONE_DATA[this.selectedZone].name} 파견 시작 (~${mins}분)`, { color: '#88ccff' });
                    SaveManager.save(gs);
                    this.scene.start('TownScene', { gameState: gs });
                }
            }
        });

        // 안내 메시지
        let hint = '';
        if (!this.selectedZone) hint = '구역을 선택하세요';
        else if (this.deployedIds.length === 0) hint = '용병을 편성하세요';
        else if (slotsFull) hint = `파견 슬롯 가득 (${activeExp}/${maxSlots})`;
        else if (!isMain && gs.zoneLevel[this.selectedZone] === 0) hint = '미클리어 구역엔 서브 파견 불가';

        if (hint) {
            this.add.text(640, 660, hint, {
                fontSize: '11px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5);
        }
    }
}
