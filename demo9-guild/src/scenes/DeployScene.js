class DeployScene extends Phaser.Scene {
    constructor() { super('DeployScene'); }

    preload() {
        // 액션 아이콘 — DeployScene 미리보기용 (PNG 없으면 이모지 폴백)
        if (typeof ActionIcons !== 'undefined') {
            ActionIcons.preload(this);
        }
    }

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
        this._drawSynergyPreview(maxDeploy);
        this._drawRosterPick(30, 440);
        this._drawSavedParties(30, 595);
        this._drawDepartButton();
    }

    /** 편성된 용병들의 활성 시너지 미리보기 — 슬롯 위 가로 막대 */
    _drawSynergyPreview(maxDeploy) {
        const gs = this.gameState;
        const deployed = this.deployedIds
            .map(id => gs.roster.find(m => m.id === id))
            .filter(Boolean);
        const classKeys = deployed.map(m => m.classKey);

        // 가로 막대 — 슬롯 영역 위쪽 (y=215~272)
        const px = 30, py = 215, pw = 1220, ph = 56;

        const bg = this.add.graphics();
        bg.fillStyle(0x141426, 1);
        bg.fillRoundedRect(px, py, pw, ph, 6);
        bg.lineStyle(2, 0x665588, 0.7);
        bg.strokeRoundedRect(px, py, pw, ph, 6);

        this.add.text(px + 12, py + 6, '✨ 활성 시너지', {
            fontSize: '12px', fontFamily: 'monospace', color: '#ccaaff', fontStyle: 'bold'
        });

        if (typeof getActiveSynergies !== 'function' || deployed.length === 0) {
            this.add.text(px + pw/2, py + ph/2 + 4, '용병을 편성하면 발동 가능한 시너지가 표시됩니다', {
                fontSize: '11px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5);
            return;
        }

        const active = getActiveSynergies(classKeys);
        if (active.length === 0) {
            const close = this._findCloseSynergies(classKeys);
            if (close.length === 0) {
                this.add.text(px + pw/2, py + ph/2 + 4, '현재 발동 가능한 시너지 없음 — 다양한 클래스 조합 시도', {
                    fontSize: '11px', fontFamily: 'monospace', color: '#666677'
                }).setOrigin(0.5);
            } else {
                this.add.text(px + 110, py + 6, ' — 1명 추가 시 발동:', {
                    fontSize: '10px', fontFamily: 'monospace', color: '#888899', fontStyle: 'italic'
                });
                // 가로로 chip 형태
                let chipX = px + 12;
                close.slice(0, 5).forEach(c => {
                    const label = `${c.name} (+${this._classIcon(c.missingClass)})`;
                    const txt = this.add.text(chipX, py + 30, label, {
                        fontSize: '10px', fontFamily: 'monospace', color: '#88aacc',
                        backgroundColor: '#222244', padding: { x: 6, y: 2 }
                    });
                    chipX += txt.width + 8;
                    if (chipX > px + pw - 100) return;
                });
            }
            return;
        }

        // 활성 시너지 — 가로로 chip
        let chipX = px + 110;
        active.forEach(syn => {
            const typeColor = syn.type === 5 ? '#ffcc44' : syn.type === 3 ? '#ff88cc' : '#88ccff';
            const bgColor = syn.type === 5 ? '#553311' : syn.type === 3 ? '#441133' : '#113344';
            const typeBadge = syn.type === 5 ? '⭐5인' : syn.type === 3 ? '★3인' : '✦2인';

            const chip = this.add.text(chipX, py + 8, `${typeBadge} ${syn.name}`, {
                fontSize: '11px', fontFamily: 'monospace', color: typeColor, fontStyle: 'bold',
                backgroundColor: bgColor, padding: { x: 8, y: 3 }
            });
            this.add.text(chipX, py + 30, syn.desc, {
                fontSize: '9px', fontFamily: 'monospace', color: '#aaaacc',
                wordWrap: { width: 280 }
            });
            chipX += Math.max(chip.width, 200) + 16;
            if (chipX > px + pw - 50) return;
        });
    }

    _classIcon(classKey) {
        return (CLASS_DATA[classKey]?.icon || '?') + (CLASS_DATA[classKey]?.name || '');
    }

    /** 한 명 더 추가하면 발동되는 시너지 후보 */
    _findCloseSynergies(currentClasses) {
        if (typeof SYNERGY_DATA !== 'object') return [];
        const classSet = new Set(currentClasses);
        const candidates = [];
        for (const [key, syn] of Object.entries(SYNERGY_DATA)) {
            if (syn.type !== 2 && syn.type !== 3) continue;
            const missing = syn.classes.filter(c => !classSet.has(c));
            if (missing.length === 1) {
                candidates.push({
                    name: syn.name,
                    desc: syn.desc,
                    missingClass: missing[0]
                });
            }
        }
        return candidates;
    }

    _drawSavedParties(x, y) {
        const gs = this.gameState;
        if (!gs.savedParties) gs.savedParties = [];

        this.add.text(x, y, '편성 저장 (클릭하여 불러오기 / 우클릭하여 덮어쓰기)', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aaaacc', fontStyle: 'bold'
        });

        const SLOT_COUNT = 4;
        const slotW = 195, slotH = 45, gap = 10;
        for (let i = 0; i < SLOT_COUNT; i++) {
            const sx = x + i * (slotW + gap);
            const saved = gs.savedParties[i];
            const hasSaved = !!saved;

            const bg = this.add.graphics();
            bg.fillStyle(hasSaved ? 0x223344 : 0x1a1a2a, 1);
            bg.fillRoundedRect(sx, y + 18, slotW, slotH, 4);
            bg.lineStyle(1, hasSaved ? 0x5588aa : 0x444455, hasSaved ? 0.7 : 0.4);
            bg.strokeRoundedRect(sx, y + 18, slotW, slotH, 4);

            if (hasSaved) {
                // 살아있는 멤버만 카운트
                const aliveMembers = saved.mercIds.filter(id => gs.roster.find(m => m.id === id && m.alive));
                this.add.text(sx + 8, y + 23, saved.name || `편성 ${i + 1}`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#aaccff', fontStyle: 'bold'
                });
                this.add.text(sx + 8, y + 42, `${aliveMembers.length}/${saved.mercIds.length}명 출전 가능`, {
                    fontSize: '10px', fontFamily: 'monospace', color: aliveMembers.length === saved.mercIds.length ? '#88ccaa' : '#aa8888'
                });
            } else {
                this.add.text(sx + slotW / 2, y + 40, `슬롯 ${i + 1} (빈 슬롯)`, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#666677'
                }).setOrigin(0.5);
            }

            const hit = this.add.zone(sx + slotW / 2, y + 18 + slotH / 2, slotW, slotH).setInteractive();
            hit.on('pointerdown', (pointer) => {
                const isRight = pointer.rightButtonDown && pointer.rightButtonDown();
                if (isRight || pointer.event && pointer.event.button === 2) {
                    // 우클릭: 현재 편성으로 덮어쓰기
                    this._savePartySlot(i);
                } else {
                    // 좌클릭: 불러오기 (또는 빈 슬롯이면 저장)
                    if (hasSaved) this._loadPartySlot(i);
                    else this._savePartySlot(i);
                }
            });
            // 우클릭 컨텍스트 메뉴 비활성화
            this.input.mouse.disableContextMenu();
        }
    }

    _savePartySlot(idx) {
        const gs = this.gameState;
        if (this.deployedIds.length === 0) {
            UIToast.show(this, '편성된 용병이 없습니다', { color: '#ff6644' });
            return;
        }
        if (!gs.savedParties) gs.savedParties = [];
        gs.savedParties[idx] = {
            name: `편성 ${idx + 1}`,
            mercIds: this.deployedIds.slice()
        };
        SaveManager.save(gs);
        UIToast.show(this, `슬롯 ${idx + 1}에 저장됨`, { color: '#88ccff' });
        this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: this.deployMode });
    }

    _loadPartySlot(idx) {
        const gs = this.gameState;
        const saved = gs.savedParties[idx];
        if (!saved) return;
        // 살아있고 파견 안 중인 용병만
        const validIds = saved.mercIds.filter(id => {
            const m = gs.roster.find(r => r.id === id);
            return m && m.alive && !ExpeditionManager.isOnExpedition(gs, id);
        });
        if (validIds.length === 0) {
            UIToast.show(this, '저장된 용병들이 모두 출전 불가', { color: '#ff6644' });
            return;
        }
        const maxDeploy = GuildManager.getMaxDeploy(gs);
        this.deployedIds = validIds.slice(0, maxDeploy);
        UIToast.show(this, `편성 ${idx + 1} 불러옴 (${this.deployedIds.length}명)`, { color: '#88ccff' });
        this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: this.deployMode });
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
            this.add.text(x + w / 2, y + 118, zone.desc, {
                fontSize: '9px', fontFamily: 'monospace', color: '#667788',
                wordWrap: { width: w - 20 }, align: 'center'
            }).setOrigin(0.5);

            // 서브 파견 마스터리 진행 표시 (현재 레벨 기준)
            const clears = GuildManager.getZoneClearCount(gs, zoneKey, zLv);
            const needed = GuildManager.SUB_UNLOCK_CLEARS;
            const maxUnlocked = GuildManager.getMaxUnlockedSubLevel(gs, zoneKey);
            let subText;
            let subColor;
            if (clears >= needed) {
                subText = `📦 서브 해금됨 (최대 Lv.${maxUnlocked})`;
                subColor = '#88ccff';
            } else {
                subText = `🔓 서브 해금까지 ${clears}/${needed}`;
                subColor = '#aaaaaa';
            }
            this.add.text(x + w / 2, y + 140, subText, {
                fontSize: '10px', fontFamily: 'monospace', color: subColor, fontStyle: 'bold'
            }).setOrigin(0.5);

            const hitZone = this.add.zone(x + w / 2, y + h / 2, w, h).setInteractive({ useHandCursor: true });
            hitZone.on('pointerdown', () => {
                this.selectedZone = zoneKey;
                this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: this.deployMode });
            });
        }
    }

    _drawDeploySlots(x, y, maxDeploy) {
        const gs = this.gameState;
        const slotW = 230;

        // 다키스트 포지션 안내 (포지션 1이 가장 우측 = 전열)
        const positionInfo = this.add.text(x, y - 18, '편성 순서: ← 좌측이 후열(원거리), 우측이 전열(근접)', {
            fontSize: '11px', fontFamily: 'monospace', color: '#aaccff', fontStyle: 'italic'
        });

        for (let i = 0; i < maxDeploy; i++) {
            const sx = x + i * (slotW + 10);
            const merc = gs.roster.find(m => m.id === this.deployedIds[i]);
            const position = maxDeploy - i;   // 좌측이 후열(높은 번호), 우측이 전열(낮은 번호)

            const bg = this.add.graphics();
            bg.fillStyle(merc ? 0x1a2a3a : 0x151525, 1);
            bg.fillRoundedRect(sx, y, slotW, 165, 4);
            bg.lineStyle(1, merc ? 0x446688 : 0x333355, 0.6);
            bg.strokeRoundedRect(sx, y, slotW, 165, 4);

            // 포지션 번호 (좌상단)
            const posColor = position <= 2 ? '#ff8866' : '#88ccff';
            const posLabel = position <= 2 ? `전열 [${position}]` : `후열 [${position}]`;
            this.add.text(sx + 8, y + 5, posLabel, {
                fontSize: '11px', fontFamily: 'monospace', color: posColor, fontStyle: 'bold'
            });

            if (merc) {
                const base = merc.getBaseClass();
                const rarity = RARITY_DATA[merc.rarity];
                const stats = merc.getStats();

                this.add.text(sx + slotW - 10, y + 5, `Lv.${merc.level}`, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa'
                }).setOrigin(1, 0);
                this.add.text(sx + 10, y + 22, `${base.icon} ${merc.name}`, {
                    fontSize: '12px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
                });
                this.add.text(sx + 10, y + 40, `${base.name} HP:${merc.currentHp}/${stats.hp}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#8888aa'
                });
                this.add.text(sx + 10, y + 55, `ATK:${stats.atk} DEF:${stats.def} SPD:${stats.moveSpeed}`, {
                    fontSize: '10px', fontFamily: 'monospace', color: '#8888aa'
                });

                // 액션 미리보기 (현재 포지션에서 가능한지 표시)
                if (typeof getClassActions === 'function') {
                    const actions = getClassActions(merc.classKey);
                    let ay = y + 72;
                    actions.forEach(action => {
                        const canUse = action.casterPositions.includes(position);
                        const mark = canUse ? '✓' : '✗';
                        const color = canUse ? '#88ccaa' : '#aa6666';
                        const posStr = action.casterPositions.join(',');

                        // 사용 가능 마크 (✓/✗)
                        this.add.text(sx + 10, ay, mark, {
                            fontSize: '9px', fontFamily: 'monospace', color
                        });
                        // 액션 아이콘 (작게 12×12)
                        if (typeof ActionIcons !== 'undefined') {
                            const iconObj = ActionIcons.render(this, sx + 26, ay + 6, action, 12);
                            if (iconObj) {
                                if (!canUse) iconObj.setAlpha(0.4);
                            }
                        }
                        this.add.text(sx + 35, ay, `${action.name} [P${posStr}]`, {
                            fontSize: '9px', fontFamily: 'monospace', color
                        });
                        ay += 12;
                    });
                }

                // 좌/우 이동 버튼
                if (i > 0) {
                    UIButton.create(this, sx + 25, y + 148, 35, 22, '◀', {
                        color: 0x445566, hoverColor: 0x556677, textColor: '#aaccee', fontSize: 12,
                        onClick: () => this._swapDeployed(i, i - 1)
                    });
                }
                if (i < maxDeploy - 1 && this.deployedIds[i + 1]) {
                    UIButton.create(this, sx + 65, y + 148, 35, 22, '▶', {
                        color: 0x445566, hoverColor: 0x556677, textColor: '#aaccee', fontSize: 12,
                        onClick: () => this._swapDeployed(i, i + 1)
                    });
                }
                UIButton.create(this, sx + slotW - 50, y + 148, 80, 22, '해제', {
                    color: 0x555555, hoverColor: 0x666666, textColor: '#cccccc', fontSize: 10,
                    onClick: () => {
                        this.deployedIds = this.deployedIds.filter(id => id !== merc.id);
                        this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: this.deployMode });
                    }
                });
            } else {
                this.add.text(sx + slotW / 2, y + 80, '(빈 슬롯)', {
                    fontSize: '12px', fontFamily: 'monospace', color: '#444455'
                }).setOrigin(0.5);
            }
        }
    }

    /** 슬롯 idx1과 idx2 위치 스왑 */
    _swapDeployed(idx1, idx2) {
        const gs = this.gameState;
        const arr = this.deployedIds.slice();
        // 양쪽 다 채워있어야 swap. 한쪽 빈 슬롯이면 한쪽으로 이동
        const tmp = arr[idx1];
        arr[idx1] = arr[idx2];
        arr[idx2] = tmp;
        this.deployedIds = arr.filter(id => id);
        this.scene.restart({ gameState: gs, selectedZone: this.selectedZone, deployedIds: this.deployedIds, deployMode: this.deployMode });
    }

    _drawRosterPick(x, y) {
        const gs = this.gameState;
        const available = gs.roster.filter(m =>
            m.alive &&
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

        let canDepart = this.selectedZone && this.deployedIds.length > 0 && !slotsFull;
        // 서브 모드: 마스터리 해금 체크
        if (canDepart && !isMain && this.selectedZone) {
            const maxUnlocked = GuildManager.getMaxUnlockedSubLevel(gs, this.selectedZone);
            if (maxUnlocked === 0) canDepart = false;
        }
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
                    // 메인 전투 — BP는 다키스트 (ManualBattleScene), 다른 구역은 기존
                    gs.runCount++;
                    SaveManager.save(gs);
                    const sceneMap = {
                        bloodpit: 'ManualBattleScene',   // v3 다키스트 적용
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
        else if (!isMain) {
            const maxUnlocked = GuildManager.getMaxUnlockedSubLevel(gs, this.selectedZone);
            if (maxUnlocked === 0) {
                const zLv = gs.zoneLevel[this.selectedZone];
                const clears = GuildManager.getZoneClearCount(gs, this.selectedZone, zLv);
                hint = `🔓 서브 해금 필요 (메인 ${clears}/${GuildManager.SUB_UNLOCK_CLEARS}회 클리어)`;
            }
        }

        if (hint) {
            this.add.text(640, 660, hint, {
                fontSize: '11px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5);
        }
    }
}
