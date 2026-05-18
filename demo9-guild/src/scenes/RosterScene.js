class RosterScene extends Phaser.Scene {
    constructor() { super('RosterScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.selectedMercId = data.selectedMercId || null;
    }

    create() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        this.add.rectangle(640, 360, 1280, 720, T.bg || 0x1a1510);

        // 비네팅 + 코너 장식 (SceneBase._drawBackground 패턴)
        const vignette = this.add.graphics();
        vignette.fillStyle(0x000000, 0.15);
        vignette.fillRect(0, 0, 1280, 4);
        vignette.fillRect(0, 716, 1280, 4);
        vignette.fillRect(0, 0, 4, 720);
        vignette.fillRect(1276, 0, 4, 720);

        const corner = this.add.graphics();
        corner.lineStyle(1, T.ornament || 0x8a7a4a, 0.15);
        corner.lineBetween(0, 20, 20, 0);
        corner.lineBetween(0, 30, 30, 0);
        corner.lineBetween(1260, 0, 1280, 20);
        corner.lineBetween(1250, 0, 1280, 30);
        corner.lineBetween(0, 700, 20, 720);
        corner.lineBetween(0, 690, 30, 720);
        corner.lineBetween(1260, 720, 1280, 700);
        corner.lineBetween(1250, 720, 1280, 690);

        this._drawUI();
    }

    _drawUI() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;

        // 선택된 용병 있으면 상세 화면, 없으면 카드 그리드
        if (this.selectedMercId) {
            const merc = gs.roster.find(m => m.id === this.selectedMercId);
            if (merc && merc.alive) {
                this._drawMercDetailView(merc);
                return;
            }
        }

        // === 카드 그리드 화면 ===
        this._drawHeader('로스터 관리', {
            backTarget: 'TownScene',
            backLabel: '← 마을',
        });

        const commonCount = gs.roster.filter(m => m.rarity === 'common' && m.alive).length;
        if (commonCount > 0) {
            UIButton.create(this, 1100, 27, 130, 28, `일반 ${commonCount}명 정리`, {
                variant: 'danger',
                fontSize: 11,
                onClick: () => this._confirmBulkDismiss('common', `일반 등급 용병 ${commonCount}명`, m => m.rarity === 'common' && m.alive)
            });
        }

        if (gs.roster.length === 0) {
            this.add.text(640, 360, '로스터가 비어있습니다 — 모집소에서 고용하세요', {
                fontSize: `${(T.fontSize && T.fontSize.body) || 12}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5);
            return;
        }

        this.add.text(640, 60, '용병 카드를 클릭하여 상세 보기 / 장비 착용 / 해고', {
            fontSize: `${(T.fontSize && T.fontSize.body) || 12}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        }).setOrigin(0.5);

        this._drawMercGrid(gs);
    }

    /** 공용 헤더 (SceneBase._drawBase 패턴) */
    _drawHeader(title, opts = {}) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const {
            backTarget = 'TownScene',
            backLabel = '← 마을',
            backOnClick = null,
        } = opts;

        const headerH = T.headerHeight || 55;
        const headerBg = this.add.graphics();
        headerBg.fillStyle(T.headerBg || 0x1e1810, 1);
        headerBg.fillRect(0, 0, 1280, headerH);
        // 하단 장식선
        headerBg.lineStyle(1, T.ornament || 0x8a7a4a, T.ornamentAlpha || 0.4);
        headerBg.lineBetween(0, headerH, 1280, headerH);
        // 이중선
        headerBg.lineStyle(0.5, T.divider || 0x5a4a2a, 0.3);
        headerBg.lineBetween(0, headerH + 2, 1280, headerH + 2);

        // 제목
        if (title) {
            this.add.text(640, 27, title, {
                fontSize: `${(T.fontSize && T.fontSize.title) || 20}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textGold || '#ffcc44',
                fontStyle: 'bold',
                stroke: '#000000',
                strokeThickness: 2
            }).setOrigin(0.5);

            const tw = title.length * 12;
            this.add.text(640 - tw / 2 - 20, 27, '◈', {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                color: T.textAccent || '#cc8833'
            }).setOrigin(0.5);
            this.add.text(640 + tw / 2 + 20, 27, '◈', {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                color: T.textAccent || '#cc8833'
            }).setOrigin(0.5);
        }

        // 뒤로가기
        if (backTarget || backOnClick) {
            UIButton.create(this, 65, 27, 100, 28, backLabel, {
                variant: 'ghost',
                fontSize: (T.fontSize && T.fontSize.body) || 12,
                onClick: backOnClick || (() => this.scene.start(backTarget, { gameState: gs }))
            });
        }

        // 골드
        this._goldText = this.add.text(1260, 20, `💰 ${(gs.gold || 0).toLocaleString()}G`, {
            fontSize: `${(T.fontSize && T.fontSize.header) || 16}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44',
            fontStyle: 'bold'
        }).setOrigin(1, 0);
    }

    /** 용병 상세 화면 — 좌(스탯), 중(장비+슬롯), 우(보관함 후보) */
    _drawMercDetailView(merc) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const base = merc.getBaseClass();
        const rarity = RARITY_DATA[merc.rarity];

        // 헤더
        this._drawHeader(`${base.icon} ${merc.name} — Lv.${merc.level} ${base.name} [${rarity.name}]`, {
            backTarget: null,
            backLabel: '← 로스터',
            backOnClick: () => {
                this.selectedMercId = null;
                this.scene.restart({ gameState: gs });
            }
        });

        // 3패널 레이아웃
        this._drawMercInfoPanel(merc, 20, 65, 400, 640);    // 좌: 스탯/특성/스킬/친화도
        this._drawMercEquipPanel(merc, 440, 65, 380, 640);  // 중: 장비 슬롯
        this._drawMercInventoryPanel(merc, 840, 65, 420, 640); // 우: 보관함 후보
    }

    _drawMercGrid(gs) {
        const cardW = 230, cardH = 140, gap = 12;
        const cols = 5;
        const totalW = cols * cardW + (cols - 1) * gap;
        const startX = (1280 - totalW) / 2;
        const startY = 90;

        gs.roster.forEach((merc, idx) => {
            const col = idx % cols;
            const row = Math.floor(idx / cols);
            const x = startX + col * (cardW + gap);
            const y = startY + row * (cardH + gap);
            this._drawMercCard(merc, x, y, cardW, cardH);
        });
    }

    _drawMercCard(merc, x, y, w, h) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const base = merc.getBaseClass();
        const rarity = RARITY_DATA[merc.rarity];
        const stats = merc.getStats();
        const isHurt = merc.currentHp < stats.hp;

        // === 카드를 컨테이너에 묶어서 처리 (UIButton 패턴) ===
        const container = this.add.container(0, 0);

        const bg = this.add.graphics();
        const drawBg = (fill, stroke) => {
            bg.clear();
            bg.fillStyle(fill, 1);
            bg.fillRoundedRect(x, y, w, h, 5);
            bg.lineStyle(2, stroke, 0.8);
            bg.strokeRoundedRect(x, y, w, h, 5);
        };
        drawBg(T.panelFill || 0x2a2218, rarity.color);
        container.add(bg);

        // 헤더
        container.add(this.add.text(x + 10, y + 8, `${base.icon} ${merc.name}`, {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }));
        container.add(this.add.text(x + w - 10, y + 8, `Lv.${merc.level}`, {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textSecondary || '#b8a888'
        }).setOrigin(1, 0));
        container.add(this.add.text(x + 10, y + 26, `${base.name} [${rarity.name}]`, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
        }));

        // HP바
        const hpRatio = merc.currentHp / stats.hp;
        const hpBar = this.add.graphics();
        hpBar.fillStyle(0x331111, 1);
        hpBar.fillRect(x + 10, y + 46, w - 20, 6);
        const hpColor = hpRatio > 0.6 ? 0x44ff88 : hpRatio > 0.3 ? 0xffaa44 : 0xff4444;
        hpBar.fillStyle(hpColor, 1);
        hpBar.fillRect(x + 10, y + 46, (w - 20) * hpRatio, 6);
        container.add(hpBar);
        container.add(this.add.text(x + w / 2, y + 49, `HP ${merc.currentHp}/${stats.hp}`, {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: '#ffffff', stroke: '#000', strokeThickness: 1
        }).setOrigin(0.5));

        // 스탯 한줄
        container.add(this.add.text(x + 10, y + 60, `ATK ${stats.atk}  DEF ${stats.def}  SPD ${stats.moveSpeed}`, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textPrimary || '#e8d8c0'
        }));

        // 장착 표시
        const equipCount = ['weapon','armor','accessory'].filter(s => merc.equipment[s]).length;
        container.add(this.add.text(x + 10, y + 78, `🎽 ${equipCount}/3`, {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: equipCount === 3 ? '#88ffcc' : (T.textSecondary || '#b8a888')
        }));

        // 파견 중 표시
        const gs = this.gameState;
        if (ExpeditionManager.isOnExpedition(gs, merc.id)) {
            const exp = (gs.activeExpeditions || []).find(e => e.partyIds.includes(merc.id));
            const zoneName = exp ? ZONE_DATA[exp.zoneKey].name : '파견';
            container.add(this.add.text(x + w - 10, y + 78, `📦 ${zoneName}`, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textBlue || '#6699cc'
            }).setOrigin(1, 0));
        }

        // 특성 (1-2개만)
        const traitsStr = merc.traits.slice(0, 2).map(t => {
            const sym = t.type === 'positive' ? '✦' : t.type === 'legendary' ? '★' : '✧';
            return sym + t.name;
        }).join(' ');
        if (traitsStr) {
            container.add(this.add.text(x + 10, y + 96, traitsStr, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860',
                wordWrap: { width: w - 20 }
            }));
        }

        // 하단 클릭 안내
        container.add(this.add.text(x + w/2, y + h - 14, '🖱 클릭', {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
        }).setOrigin(0.5));

        // === 클릭 영역 — UIButton과 동일하게 zone 사용 ===
        const hitZone = this.add.zone(x + w/2, y + h/2, w, h)
            .setInteractive({ useHandCursor: true });
        hitZone.setDepth(1000);
        container.add(hitZone);

        hitZone.on('pointerover', () => drawBg(T.cardHover || 0x3a3020, T.ornament || 0x8a7a4a));
        hitZone.on('pointerout', () => drawBg(T.panelFill || 0x2a2218, rarity.color));
        hitZone.on('pointerdown', () => {
            drawBg(0xffcc44, 0xffffff);
        });
        hitZone.on('pointerup', () => {
            this._selectedSlot = 'weapon';
            this.scene.start('RosterScene', { gameState: this.gameState, selectedMercId: merc.id });
        });
    }

    /** 용병 통합 모달 — 상세/장비/해고 모두 */
    _openMercModal(merc) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        this.selectedMercId = merc.id;
        // 풀스크린 모달
        this._mercModalObjs = [];
        const overlay = this.add.rectangle(640, 360, 1280, 720, T.modalOverlay || 0x000000, T.modalOverlayAlpha || 0.7).setDepth(80);
        overlay.setInteractive();
        this._mercModalObjs.push(overlay);

        // 큰 박스 1180x640
        const mx = 50, my = 40, mw = 1180, mh = 640;
        const bg = this.add.graphics().setDepth(81);
        bg.fillStyle(T.panelFill || 0x2a2218, 1);
        bg.fillRoundedRect(mx, my, mw, mh, 8);
        const rarity = RARITY_DATA[merc.rarity];
        bg.lineStyle(3, rarity.color, 0.9);
        bg.strokeRoundedRect(mx, my, mw, mh, 8);
        this._mercModalObjs.push(bg);

        // 닫기 버튼
        this._mercModalObjs.push(UIButton.create(this, mx + mw - 50, my + 30, 70, 30, '닫기', {
            variant: 'ghost',
            fontSize: 12, depth: 82,
            onClick: () => this._closeMercModal()
        }));

        // 모달 컨텐츠
        this._drawDetailPanel(merc, mx + 10, my + 10);
    }

    _closeMercModal() {
        if (this._mercModalObjs) {
            this._mercModalObjs.forEach(o => o.destroy && o.destroy());
            this._mercModalObjs = null;
        }
        this.scene.restart({ gameState: this.gameState });
    }

    _drawListItem(merc, x, y, w) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const base = merc.getBaseClass();
        const rarity = RARITY_DATA[merc.rarity];
        const stats = merc.getStats();
        const isSelected = merc.id === this.selectedMercId;

        const bg = this.add.graphics();
        bg.fillStyle(isSelected ? (T.cardHover || 0x3a3020) : (T.panelFill || 0x2a2218), 1);
        bg.fillRoundedRect(x, y, w, 60, 3);
        bg.lineStyle(1, isSelected ? (T.ornament || 0x8a7a4a) : rarity.color, isSelected ? 0.9 : 0.3);
        bg.strokeRoundedRect(x, y, w, 60, 3);

        this.add.text(x + 8, y + 6, `${base.icon} ${merc.name}`, {
            fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: rarity.textColor, fontStyle: 'bold'
        });
        this.add.text(x + w - 8, y + 6, `Lv.${merc.level} ${base.name}`, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
        }).setOrigin(1, 0);

        this.add.text(x + 8, y + 25, `HP:${merc.currentHp}/${stats.hp} ATK:${stats.atk} DEF:${stats.def}`, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textSecondary || '#b8a888'
        });

        const hpRatio = merc.currentHp / stats.hp;
        const hpBar = this.add.graphics();
        hpBar.fillStyle(0x333344, 1);
        hpBar.fillRect(x + 8, y + 44, w - 16, 4);
        const hpColor = hpRatio > 0.6 ? 0x44ff88 : hpRatio > 0.3 ? 0xffaa44 : 0xff4444;
        hpBar.fillStyle(hpColor, 1);
        hpBar.fillRect(x + 8, y + 44, (w - 16) * hpRatio, 4);

        const hitZone = this.add.zone(x + w / 2, y + 30, w, 60).setInteractive({ useHandCursor: true });
        hitZone.on('pointerdown', () => {
            this.selectedMercId = merc.id;
            this.scene.restart({ gameState: this.gameState });
        });
    }

    /** 좌측 패널 — 스탯/특성/스킬/친화도/하단 버튼 (자동장착/치료/해고) */
    _drawMercInfoPanel(merc, x, y, w, h) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const base = merc.getBaseClass();
        const rarity = RARITY_DATA[merc.rarity];
        const stats = merc.getStats();

        UIPanel.create(this, x, y, w, h, {
            title: '상세 정보',
            ornament: true,
        });

        let cy = y + 35;
        // XP
        const xpLabel = merc.level >= 10 ? 'MAX' : `${merc.xp}/${merc.getXpToNextLevel()}`;
        this.add.text(x + 15, cy, `XP: ${xpLabel}`, {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textBlue || '#6699cc'
        });
        cy += 22;

        // 스탯
        this.add.text(x + 15, cy, '── 스탯 ──', {
            fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: T.textGold || '#ffcc44', fontStyle: 'bold'
        });
        cy += 18;
        const statLabels = [
            ['HP', `${merc.currentHp}/${stats.hp}`], ['ATK', stats.atk],
            ['DEF', stats.def], ['SPD', stats.moveSpeed],
            ['CRIT', `${Math.floor(stats.critRate * 100)}%`], ['범위', stats.range]
        ];
        statLabels.forEach((s, i) => {
            const col = i % 2;
            const row = Math.floor(i / 2);
            this.add.text(x + 25 + col * 175, cy + row * 18, `${s[0]}: ${s[1]}`, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textPrimary || '#e8d8c0'
            });
        });
        cy += 60;

        // 특성
        this.add.text(x + 15, cy, '── 특성 ──', {
            fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: T.textGold || '#ffcc44', fontStyle: 'bold'
        });
        cy += 18;
        if (!merc.traits || merc.traits.length === 0) {
            this.add.text(x + 25, cy, '없음', {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
            });
            cy += 16;
        } else {
            merc.traits.forEach(t => {
                let color = '#44cc44', sym = '✦';
                if (t.type === 'negative') { color = '#ff6666'; sym = '✧'; }
                if (t.type === 'legendary') { color = '#ffaa00'; sym = '★'; }
                this.add.text(x + 25, cy, `${sym} ${t.name}`, {
                    fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color, fontStyle: 'bold'
                });
                this.add.text(x + 40, cy + 14, t.desc, {
                    fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860',
                    wordWrap: { width: w - 60 }
                });
                cy += 32;
            });
        }
        cy += 5;

        // 스킬
        if (merc.level >= 5) {
            this.add.text(x + 15, cy, '── 스킬 ──', {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: T.textGold || '#ffcc44', fontStyle: 'bold'
            });
            cy += 18;
            this.add.text(x + 25, cy, base.skillName, {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: '#44aaff', fontStyle: 'bold'
            });
            this.add.text(x + 25, cy + 14, base.skillDesc, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860',
                wordWrap: { width: w - 40 }
            });
            cy += 36;
        }

        // 친화도
        this.add.text(x + 15, cy, '── 친화도 ──', {
            fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: T.textGold || '#ffcc44', fontStyle: 'bold'
        });
        cy += 18;
        ZONE_KEYS.forEach(zoneKey => {
            if (gs.zoneLevel[zoneKey] === 0) return;
            const zone = ZONE_DATA[zoneKey];
            const aLv = merc.affinityLevel?.[zoneKey] || 0;
            const aPts = merc.affinityPoints?.[zoneKey] || 0;
            const ptsLabel = aPts > 0 ? ` [${aPts}P!]` : '';
            this.add.text(x + 25, cy, `${zone.icon} ${zone.name}: Lv.${aLv}${ptsLabel}`, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: aPts > 0 ? (T.textGold || '#ffcc44') : zone.textColor
            });
            cy += 16;
        });

        UIButton.create(this, x + w/2, cy + 10, 200, 26, '🌳 친화도 트리', {
            variant: 'info',
            fontSize: 11,
            onClick: () => this.scene.start('AffinityScene', { gameState: gs, mercId: merc.id })
        });

        // 하단 버튼 (자동장착 / 치료 / 해고)
        const btnY = y + h - 50;
        const isHurt = merc.currentHp < stats.hp;

        UIButton.create(this, x + 80, btnY, 140, 32, '💊 치료 30G', {
            variant: 'primary',
            fontSize: 11,
            disabled: gs.gold < 30 || !isHurt,
            onClick: () => {
                if (GuildManager.spendGold(gs, 30)) {
                    merc.fullHeal();
                    GuildManager.addMessage(gs, `${merc.name} 치료 완료`);
                    SaveManager.save(gs);
                    this.scene.restart({ gameState: gs, selectedMercId: merc.id });
                }
            }
        });

        const rarityMult = { common: 1.0, uncommon: 1.2, rare: 1.5, epic: 2.0, legendary: 2.5 }[merc.rarity] || 1.0;
        const severance = Math.floor(50 * merc.level * rarityMult);
        UIButton.create(this, x + w - 100, btnY, 180, 32, `🚪 해고 +${severance}G`, {
            variant: 'danger',
            fontSize: 11,
            onClick: () => this._confirmDismiss(merc, severance)
        });
    }

    /** 중앙 패널 — 장비 슬롯 (선택 가능, 클릭 시 우측 보관함 후보 갱신) */
    _drawMercEquipPanel(merc, x, y, w, h) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;

        UIPanel.create(this, x, y, w, h, {
            title: '장비',
            ornament: true,
        });

        if (!this._selectedSlot) this._selectedSlot = 'weapon';

        let cy = y + 40;
        const slots = [
            { key: 'weapon',    name: '⚔ 무기' },
            { key: 'armor',     name: '🛡 방어구' },
            { key: 'accessory', name: '💍 장신구' }
        ];
        slots.forEach(slotInfo => {
            const eq = merc.equipment[slotInfo.key];
            const isSelected = this._selectedSlot === slotInfo.key;

            const bg = this.add.graphics();
            bg.fillStyle(isSelected ? (T.cardSelected || 0x4a3a20) : (T.cardFill || 0x231e14), 1);
            bg.fillRoundedRect(x + 10, cy, w - 20, 90, 5);
            bg.lineStyle(2, isSelected ? (T.textGold || 0xffcc44) : (T.cardStroke || 0x4a3c28), isSelected ? 0.9 : 0.6);
            bg.strokeRoundedRect(x + 10, cy, w - 20, 90, 5);

            this.add.text(x + 20, cy + 8, slotInfo.name, {
                fontSize: '14px', fontFamily: T.fontFamily || 'monospace', color: T.textGold || '#ffcc44', fontStyle: 'bold'
            });

            if (eq) {
                const ir = ITEM_RARITY[eq.rarity];
                this.add.text(x + 20, cy + 30, `${eq.name}`, {
                    fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: ir.textColor, fontStyle: 'bold'
                });
                this.add.text(x + 20, cy + 46, `[${ir.name}]`, {
                    fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: ir.textColor
                });
                const statStr = Object.entries(eq.stats || {}).map(([k,v]) => `${k}+${v}`).join(' ');
                this.add.text(x + 20, cy + 62, statStr, {
                    fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textPrimary || '#e8d8c0'
                });

                if (!eq.cursed) {
                    UIButton.create(this, x + w - 60, cy + 60, 90, 24, '해제', {
                        variant: 'danger',
                        fontSize: 10,
                        depth: 5,
                        onClick: () => {
                            const item = merc.unequip(slotInfo.key);
                            if (item) StorageManager.addItem(gs, item);
                            SaveManager.save(gs);
                            this.scene.restart({ gameState: gs, selectedMercId: merc.id });
                        }
                    });
                } else {
                    this.add.text(x + w - 60, cy + 60, '🔒 저주', {
                        fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: '#cc44ff', fontStyle: 'bold'
                    }).setOrigin(0.5);
                }
            } else {
                this.add.text(x + 20, cy + 45, '(비어있음 — 오른쪽에서 선택)', {
                    fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
                });
            }

            const hit = this.add.zone(x + w/2, cy + 45, w - 20, 90).setInteractive({ useHandCursor: true });
            hit.on('pointerdown', () => {
                this._selectedSlot = slotInfo.key;
                this.scene.restart({ gameState: gs, selectedMercId: merc.id });
            });

            cy += 100;
        });

        // 자동 장착
        UIButton.create(this, x + w/2, y + h - 50, 240, 36, '🤖 최적 장비 자동 장착', {
            variant: 'info',
            fontSize: 12,
            onClick: () => {
                let count = 0;
                ['weapon', 'armor', 'accessory'].forEach(slot => {
                    const candidates = StorageManager.getEquippableItems(gs, slot);
                    if (candidates.length === 0) return;
                    candidates.sort((a, b) => {
                        const sum = e => Object.values(e.stats || {}).reduce((s, v) => s + (typeof v === 'number' ? v : 0), 0);
                        return sum(b) - sum(a);
                    });
                    const best = candidates[0];
                    const current = merc.equipment[slot];
                    const sum = e => e ? Object.values(e.stats || {}).reduce((s, v) => s + (typeof v === 'number' ? v : 0), 0) : 0;
                    if (sum(best) > sum(current)) {
                        StorageManager.removeItem(gs, best.id);
                        const prev = merc.equip(best);
                        if (prev) StorageManager.addItem(gs, prev);
                        count++;
                    }
                });
                if (count > 0) {
                    SaveManager.save(gs);
                    UIToast.show(this, `${count}개 슬롯 자동 장착!`, { color: '#88ccff' });
                    this.scene.restart({ gameState: gs, selectedMercId: merc.id });
                } else {
                    UIToast.show(this, '더 좋은 장비 없음', { color: T.textSecondary || '#b8a888' });
                }
            }
        });
    }

    /** 우측 패널 — 선택된 슬롯의 보관함 후보 */
    _drawMercInventoryPanel(merc, x, y, w, h) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const slot = this._selectedSlot || 'weapon';
        const slotNames = { weapon: '무기', armor: '방어구', accessory: '장신구' };
        const items = StorageManager.getEquippableItems(gs, slot);

        UIPanel.create(this, x, y, w, h, {
            title: `보관함 ${slotNames[slot]} (${items.length})`,
            ornament: true,
        });

        if (items.length === 0) {
            this.add.text(x + w/2, y + 100, `${slotNames[slot]} 장비 없음`, {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
            }).setOrigin(0.5);
            this.add.text(x + w/2, y + 130, '좌측 슬롯 클릭으로 다른 슬롯 보기', {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
            }).setOrigin(0.5);
            return;
        }

        items.sort((a, b) => {
            const sum = e => Object.values(e.stats || {}).reduce((s, v) => s + (typeof v === 'number' ? v : 0), 0);
            return sum(b) - sum(a);
        });

        const current = merc.equipment[slot];
        const sumStats = e => e ? Object.values(e.stats || {}).reduce((s, v) => s + (typeof v === 'number' ? v : 0), 0) : 0;

        let cy = y + 40;
        items.slice(0, 11).forEach(item => {
            if (cy > y + h - 40) return;
            const ir = ITEM_RARITY[item.rarity];
            const isUpgrade = sumStats(item) > sumStats(current);

            const bg = this.add.graphics();
            bg.fillStyle(isUpgrade ? 0x1a2a1a : (T.cardFill || 0x231e14), 1);
            bg.fillRoundedRect(x + 10, cy, w - 20, 48, 4);
            bg.lineStyle(1, ir.color, 0.5);
            bg.strokeRoundedRect(x + 10, cy, w - 20, 48, 4);

            this.add.text(x + 18, cy + 4, item.name, {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: ir.textColor, fontStyle: 'bold'
            });
            const statStr = Object.entries(item.stats || {}).map(([k,v]) => `${k}+${v}`).join(' ');
            this.add.text(x + 18, cy + 21, `[${ir.name}] ${statStr}`, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textPrimary || '#e8d8c0'
            });
            this.add.text(x + 18, cy + 34, `가치 ${item.value}G`, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
            });

            if (isUpgrade && current) {
                this.add.text(x + w - 100, cy + 8, '↑업그레이드', {
                    fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textSuccess || '#66bb55', fontStyle: 'bold'
                });
            }

            UIButton.create(this, x + w - 50, cy + 30, 70, 22, '장착', {
                variant: 'primary',
                fontSize: 10,
                onClick: () => {
                    StorageManager.removeItem(gs, item.id);
                    const prev = merc.equip(item);
                    if (prev) StorageManager.addItem(gs, prev);
                    SaveManager.save(gs);
                    this.scene.restart({ gameState: gs, selectedMercId: merc.id });
                }
            });

            cy += 54;
        });
    }

    /** 기존 _drawDetailPanel — 사용 안 함 (제거 대신 stub) */
    _drawDetailPanel(merc, x, y) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const base = merc.getBaseClass();
        const rarity = RARITY_DATA[merc.rarity];
        const stats = merc.getStats();
        const w = 920;

        UIPanel.create(this, x, y, w, 640, { ornament: true });

        let cy = y + 15;

        this.add.text(x + 20, cy, `${base.icon} ${merc.name}`, {
            fontSize: '18px', fontFamily: T.fontFamily || 'monospace', color: rarity.textColor, fontStyle: 'bold'
        });
        this.add.text(x + w - 20, cy, `Lv.${merc.level} ${base.name} [${rarity.name}]`, {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
        }).setOrigin(1, 0);
        cy += 30;

        const xpLabel = merc.level >= 10 ? 'MAX' : `${merc.xp}/${merc.getXpToNextLevel()}`;
        this.add.text(x + 20, cy, `XP: ${xpLabel}`, {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textBlue || '#6699cc'
        });
        cy += 25;

        const statLabels = [
            ['HP', `${merc.currentHp}/${stats.hp}`],
            ['ATK', stats.atk], ['DEF', stats.def],
            ['SPD', stats.moveSpeed], ['CRIT', `${Math.floor(stats.critRate * 100)}%`],
            ['범위', stats.range], ['공속', `${stats.attackSpeed}ms`]
        ];
        const statCols = 4;
        statLabels.forEach((s, i) => {
            const col = i % statCols;
            const row = Math.floor(i / statCols);
            this.add.text(x + 20 + col * 115, cy + row * 20, `${s[0]}: ${s[1]}`, {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: T.textPrimary || '#e8d8c0'
            });
        });
        cy += 50;

        const line1 = this.add.graphics();
        line1.lineStyle(1, T.panelStroke || 0x5a4a2a, 0.5);
        line1.lineBetween(x + 15, cy, x + w - 15, cy);
        cy += 12;

        this.add.text(x + 20, cy, '특성', {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace', color: T.textPrimary || '#e8d8c0', fontStyle: 'bold'
        });
        cy += 20;
        merc.traits.forEach(trait => {
            let color = '#44cc44';
            let sym = '✦';
            if (trait.type === 'negative') { color = '#ff6666'; sym = '✧'; }
            if (trait.type === 'legendary') { color = '#ffaa00'; sym = '★'; }
            this.add.text(x + 25, cy, `${sym} ${trait.name} — ${trait.desc}`, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color
            });
            cy += 18;
        });
        cy += 10;

        if (merc.level >= 5) {
            this.add.text(x + 20, cy, `스킬: ${base.skillName}`, {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: '#44aaff', fontStyle: 'bold'
            });
            this.add.text(x + 25, cy + 18, base.skillDesc, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: T.textBlue || '#6699cc'
            });
            cy += 40;
        }

        const line2 = this.add.graphics();
        line2.lineStyle(1, T.panelStroke || 0x5a4a2a, 0.5);
        line2.lineBetween(x + 15, cy, x + w - 15, cy);
        cy += 12;

        this.add.text(x + 20, cy, '장비', {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace', color: T.textPrimary || '#e8d8c0', fontStyle: 'bold'
        });
        cy += 22;

        ['weapon', 'armor', 'accessory'].forEach(slot => {
            const slotNames = { weapon: '무기', armor: '방어구', accessory: '장신구' };
            const eq = merc.equipment[slot];
            const label = eq ? `${slotNames[slot]}: ${eq.name} [${ITEM_RARITY[eq.rarity].name}]` : `${slotNames[slot]}: (비어있음)`;
            const color = eq ? ITEM_RARITY[eq.rarity].textColor : (T.textMuted || '#887860');

            this.add.text(x + 25, cy, label, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color
            });

            if (eq) {
                const statStr = Object.entries(eq.stats || {}).map(([k, v]) => `${k}+${v}`).join(' ');
                if (statStr) {
                    this.add.text(x + 25, cy + 14, `  ${statStr}`, {
                        fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
                    });
                }

                if (eq.cursed) {
                    this.add.text(x + w - 80, cy + 4, '🔒 저주', {
                        fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: '#cc44ff', fontStyle: 'bold'
                    });
                    if (eq.curseDebuffDesc) {
                        this.add.text(x + 25, cy + 26, `  ⚠ ${eq.curseDebuffDesc}`, {
                            fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: '#cc4488'
                        });
                    }
                } else {
                    UIButton.create(this, x + w - 80, cy + 8, 60, 22, '해제', {
                        variant: 'ghost',
                        fontSize: 10,
                        onClick: () => {
                            const item = merc.unequip(slot);
                            if (item) StorageManager.addItem(gs, item);
                            SaveManager.save(gs);
                            this.scene.restart({ gameState: gs });
                        }
                    });
                }
            } else {
                const equippable = StorageManager.getEquippableItems(gs, slot);
                if (equippable.length > 0) {
                    equippable.sort((a, b) => {
                        const sum = e => Object.values(e.stats || {}).reduce((s, v) => s + (typeof v === 'number' ? v : 0), 0);
                        return sum(b) - sum(a);
                    });
                    UIButton.create(this, x + w - 80, cy + 8, 75, 22, `장착(${equippable.length})`, {
                        variant: 'primary',
                        fontSize: 10,
                        onClick: () => this._showEquipMenu(merc, slot, equippable)
                    });
                } else {
                    this.add.text(x + w - 80, cy + 14, '(보관 없음)', {
                        fontSize: '9px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
                    }).setOrigin(0, 0.5);
                }
            }
            cy += 32;
        });

        const line3 = this.add.graphics();
        line3.lineStyle(1, T.panelStroke || 0x5a4a2a, 0.5);
        line3.lineBetween(x + 15, cy + 5, x + w - 15, cy + 5);
        cy += 15;

        this.add.text(x + 20, cy, '구역 친화도', {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace', color: T.textPrimary || '#e8d8c0', fontStyle: 'bold'
        });
        cy += 20;
        ZONE_KEYS.forEach(zoneKey => {
            if (gs.zoneLevel[zoneKey] === 0) return;
            const zone = ZONE_DATA[zoneKey];
            const aLv = merc.affinityLevel?.[zoneKey] || 0;
            const aPts = merc.affinityPoints?.[zoneKey] || 0;
            const aXp = merc.affinityXp?.[zoneKey] || 0;
            const needed = aLv >= 5 ? 'MAX' : getAffinityXpNeeded(aLv);
            const ptsLabel = aPts > 0 ? ` [${aPts}P 사용가능]` : '';
            this.add.text(x + 25, cy, `${zone.icon} ${zone.name}: Lv.${aLv} (${aLv >= 5 ? 'MAX' : `${aXp}/${needed}`})${ptsLabel}`, {
                fontSize: '11px', fontFamily: T.fontFamily || 'monospace', color: aPts > 0 ? (T.textGold || '#ffcc44') : zone.textColor
            });
            cy += 16;
        });

        UIButton.create(this, x + w / 2, cy + 14, 200, 28, '🌳 친화도 트리 열기', {
            variant: 'info',
            fontSize: 11,
            onClick: () => this.scene.start('AffinityScene', { gameState: gs, mercId: merc.id })
        });

        // 패널 하단 고정 위치 (절대 좌표) — 친화도 길이와 무관
        const btnY = y + 595;
        const isHurt = merc.currentHp < merc.getStats().hp;
        UIButton.create(this, x + 90, btnY, 150, 32, '🤖 자동 장착', {
            variant: 'info',
            fontSize: 11,
            onClick: () => {
                let count = 0;
                ['weapon', 'armor', 'accessory'].forEach(slot => {
                    const candidates = StorageManager.getEquippableItems(gs, slot);
                    if (candidates.length === 0) return;
                    candidates.sort((a, b) => {
                        const sum = e => Object.values(e.stats || {}).reduce((s, v) => s + (typeof v === 'number' ? v : 0), 0);
                        return sum(b) - sum(a);
                    });
                    const best = candidates[0];
                    const current = merc.equipment[slot];
                    const sum = e => e ? Object.values(e.stats || {}).reduce((s, v) => s + (typeof v === 'number' ? v : 0), 0) : 0;
                    if (sum(best) > sum(current)) {
                        StorageManager.removeItem(gs, best.id);
                        const prev = merc.equip(best);
                        if (prev) StorageManager.addItem(gs, prev);
                        count++;
                    }
                });
                if (count > 0) {
                    SaveManager.save(gs);
                    UIToast.show(this, `${count}개 슬롯 자동 장착!`, { color: '#88ccff' });
                    this.scene.restart({ gameState: gs });
                } else {
                    UIToast.show(this, '더 좋은 장비 없음', { color: T.textSecondary || '#b8a888' });
                }
            }
        });

        UIButton.create(this, x + 270, btnY, 140, 32, `💊 치료 (30G)`, {
            variant: 'primary',
            fontSize: 11,
            disabled: gs.gold < 30 || !isHurt,
            onClick: () => {
                if (GuildManager.spendGold(gs, 30)) {
                    merc.fullHeal();
                    GuildManager.addMessage(gs, `${merc.name} 치료 완료 (-30G)`);
                    SaveManager.save(gs);
                    this.scene.restart({ gameState: gs });
                }
            }
        });

        const rarityMult = { common: 1.0, uncommon: 1.2, rare: 1.5, epic: 2.0, legendary: 2.5 }[merc.rarity] || 1.0;
        const severance = Math.floor(50 * merc.level * rarityMult);
        UIButton.create(this, x + w - 110, btnY, 180, 32, `🚪 해고 (+${severance}G)`, {
            variant: 'danger',
            fontSize: 11,
            onClick: () => this._confirmDismiss(merc, severance)
        });
    }

    _confirmDismiss(merc, severance) {
        const gs = this.gameState;
        UIModal.confirm(this, {
            title: '해고 확인',
            message: [
                `${merc.name} (Lv.${merc.level})를 해고합니다`,
                `퇴직금: +${severance}G  |  장비는 보관함으로 회수`,
                '',
                '⚠ 영구 해고 — 복구 불가'
            ],
            confirmLabel: '해고 확정',
            cancelLabel: '취소',
            danger: true,
            onConfirm: () => {
                MercenaryManager.dismiss(gs, merc.id);
                this.selectedMercId = null;
                this.scene.restart({ gameState: gs });
            }
        });
    }

    _confirmBulkDismiss(key, label, filter) {
        const gs = this.gameState;
        const targets = gs.roster.filter(filter);
        const totalSeverance = targets.reduce((s, m) => {
            const mult = { common: 1.0, uncommon: 1.2, rare: 1.5, epic: 2.0, legendary: 2.5 }[m.rarity] || 1.0;
            return s + Math.floor(50 * m.level * mult);
        }, 0);

        UIModal.confirm(this, {
            title: '일괄 해고',
            message: [
                `${label}을(를) 모두 해고합니다 (${targets.length}명)`,
                `총 퇴직금: +${totalSeverance}G  |  장비는 보관함으로`,
                '',
                '⚠ 영구 해고 — 복구 불가'
            ],
            confirmLabel: '전원 해고',
            cancelLabel: '취소',
            danger: true,
            width: 480,
            onConfirm: () => {
                const r = MercenaryManager.dismissAll(gs, filter);
                UIToast.show(this, `${r.count}명 해고 +${r.totalSeverance}G`, { color: '#ffcc88' });
                this.scene.restart({ gameState: gs });
            }
        });
    }

    /**
     * 장비 선택 모달 — 슬롯 클릭 시 장착 가능한 아이템 목록 표시.
     */
    _showEquipMenu(merc, slot, items) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        // 오버레이
        const overlay = this.add.rectangle(640, 360, 1280, 720, T.modalOverlay || 0x000000, T.modalOverlayAlpha || 0.7).setDepth(100);
        const modalW = 600, modalH = Math.min(560, 80 + items.length * 56);
        const modalX = 640 - modalW / 2, modalY = 360 - modalH / 2;
        const modalObjs = [overlay];

        const bg = this.add.graphics().setDepth(101);
        bg.fillStyle(T.panelFill || 0x2a2218, 1);
        bg.fillRoundedRect(modalX, modalY, modalW, modalH, 6);
        // 내부 글로우
        bg.fillStyle(0xffffff, 0.03);
        bg.fillRoundedRect(modalX + 2, modalY + 2, modalW - 4, 30, { tl: 4, tr: 4, bl: 0, br: 0 });
        // 테두리
        bg.lineStyle(2, T.modalStroke || 0x8a7a4a, 0.8);
        bg.strokeRoundedRect(modalX, modalY, modalW, modalH, 6);
        // 제목 구분선
        bg.lineStyle(1, T.divider || 0x5a4a2a, 0.5);
        bg.lineBetween(modalX + 12, modalY + 42, modalX + modalW - 12, modalY + 42);
        modalObjs.push(bg);

        const slotNames = { weapon: '무기', armor: '방어구', accessory: '장신구' };
        modalObjs.push(this.add.text(modalX + modalW / 2, modalY + 20, `${slotNames[slot]} 선택 (${items.length}개)`, {
            fontSize: `${(T.fontSize && T.fontSize.header) || 16}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44',
            fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(101));

        modalObjs.push(UIButton.create(this, modalX + modalW - 50, modalY + 20, 60, 24, '닫기', {
            variant: 'ghost',
            fontSize: 11, depth: 101,
            onClick: () => modalObjs.forEach(o => o.destroy && o.destroy())
        }));

        let cy = modalY + 55;
        items.slice(0, 8).forEach(item => {
            const ir = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
            const rowBg = this.add.graphics().setDepth(101);
            rowBg.fillStyle(T.cardFill || 0x231e14, 1);
            rowBg.fillRoundedRect(modalX + 12, cy, modalW - 24, 48, 3);
            rowBg.lineStyle(1, ir.color, 0.4);
            rowBg.strokeRoundedRect(modalX + 12, cy, modalW - 24, 48, 3);
            modalObjs.push(rowBg);

            modalObjs.push(this.add.text(modalX + 24, cy + 5, item.name, {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace', color: ir.textColor, fontStyle: 'bold'
            }).setDepth(102));
            const statStr = Object.entries(item.stats || {}).map(([k, v]) => `${k}+${v}`).join(' ');
            modalObjs.push(this.add.text(modalX + 24, cy + 24, `[${ir.name}] ${statStr}`, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
            }).setDepth(102));

            modalObjs.push(UIButton.create(this, modalX + modalW - 60, cy + 24, 80, 26, '장착', {
                variant: 'primary',
                fontSize: 11, depth: 102,
                onClick: () => {
                    StorageManager.removeItem(gs, item.id);
                    const prev = merc.equip(item);
                    if (prev) StorageManager.addItem(gs, prev);
                    SaveManager.save(gs);
                    modalObjs.forEach(o => o.destroy && o.destroy());
                    this.scene.restart({ gameState: gs });
                }
            }));
            cy += 56;
        });
        if (items.length > 8) {
            modalObjs.push(this.add.text(modalX + modalW / 2, cy + 8, `... +${items.length - 8}개 더`, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace', color: T.textMuted || '#887860'
            }).setOrigin(0.5).setDepth(101));
        }
    }
}
