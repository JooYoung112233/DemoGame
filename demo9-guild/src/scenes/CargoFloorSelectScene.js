// ══════════════════════════════════════════════════════════════════
// CargoFloorSelectScene — 층 선택 + 모디파이어 선택 (나락식)
// ══════════════════════════════════════════════════════════════════

class CargoFloorSelectScene extends Phaser.Scene {
    constructor() { super('CargoFloorSelectScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.party = data.party;
        this.phase = 'floor'; // 'floor' → 'modifier' → start battle
    }

    create() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        this.add.rectangle(640, 360, 1280, 720, T.bg || 0x1a1510);

        // 비네팅
        const v = this.add.graphics();
        v.fillStyle(0x000000, 0.15);
        v.fillRect(0, 0, 1280, 4); v.fillRect(0, 716, 1280, 4);

        // 현재 해금된 최대 층
        const gs = this.gameState;
        if (!gs.cargoFloor) gs.cargoFloor = { maxUnlocked: 1, currentFloor: 1 };

        this.maxUnlocked = gs.cargoFloor.maxUnlocked || 1;
        this.selectedFloor = this.maxUnlocked;

        this._drawFloorSelect();
    }

    // ─── 층 선택 UI ──────────────────────────────────────────────
    _drawFloorSelect() {
        this._clearUI();
        this._uiElements = [];
        const _ui = (obj) => { this._uiElements.push(obj); return obj; };
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        _ui(this.add.text(640, 40, '◈  🚂 Cargo — 층 선택  ◈', {
            fontSize: '22px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold',
            stroke: '#000', strokeThickness: 2
        }).setOrigin(0.5));

        _ui(this.add.text(640, 75, `해금된 최고 층: ${this.maxUnlocked}`, {
            fontSize: '12px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        }).setOrigin(0.5));

        // 마일스톤 표시
        const milestones = [10, 25, 50, 99];
        const mileY = 110;
        _ui(this.add.text(640, mileY, '마일스톤: ' + milestones.map(m => {
            const reached = this.maxUnlocked >= m;
            return `${reached ? '✅' : '⬜'} ${m}층`;
        }).join('  '), {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5));

        // 층 선택 표시
        const sliderY = 180;
        _ui(this.add.text(640, sliderY, `도전 층: ${this.selectedFloor}`, {
            fontSize: '28px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5));

        const milestone = isCargoMilestone(this.selectedFloor);
        if (milestone) {
            _ui(this.add.text(640, sliderY + 35, `⭐ ${milestone.name}`, {
                fontSize: '14px', fontFamily: T.fontFamily || 'monospace',
                color: T.textGold || '#ffcc44', fontStyle: 'bold'
            }).setOrigin(0.5));
        }

        // 스케일링 정보
        const scaling = getCargoFloorScaling(this.selectedFloor);
        _ui(this.add.text(640, sliderY + 60, `적 HP ×${scaling.hpMult.toFixed(2)} | ATK ×${scaling.atkMult.toFixed(2)} | 외벽 HP ${scaling.wallHp}`, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5));

        // +/- 버튼
        if (this.selectedFloor > 1) {
            _ui(UIButton.create(this, 460, sliderY, 60, 36, '◀ -1', {
                variant: 'ghost', fontSize: 13,
                onClick: () => { this.selectedFloor = Math.max(1, this.selectedFloor - 1); this._drawFloorSelect(); }
            }));
            _ui(UIButton.create(this, 380, sliderY, 60, 36, '◀◀ -10', {
                variant: 'ghost', fontSize: 10,
                onClick: () => { this.selectedFloor = Math.max(1, this.selectedFloor - 10); this._drawFloorSelect(); }
            }));
        }
        if (this.selectedFloor < this.maxUnlocked) {
            _ui(UIButton.create(this, 820, sliderY, 60, 36, '+1 ▶', {
                variant: 'ghost', fontSize: 13,
                onClick: () => { this.selectedFloor = Math.min(this.maxUnlocked, this.selectedFloor + 1); this._drawFloorSelect(); }
            }));
            _ui(UIButton.create(this, 900, sliderY, 60, 36, '+10 ▶▶', {
                variant: 'ghost', fontSize: 10,
                onClick: () => { this.selectedFloor = Math.min(this.maxUnlocked, this.selectedFloor + 10); this._drawFloorSelect(); }
            }));
        }

        // 최고 층 바로가기
        if (this.maxUnlocked > 1 && this.selectedFloor !== this.maxUnlocked) {
            _ui(UIButton.create(this, 640, sliderY + 90, 140, 30, `최고 층 (${this.maxUnlocked})`, {
                variant: 'primary', fontSize: 11,
                onClick: () => { this.selectedFloor = this.maxUnlocked; this._drawFloorSelect(); }
            }));
        }

        // 파티 정보
        const partyY = 350;
        _ui(this.add.text(640, partyY, '◆ 파티 구성 ◆', {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace',
            color: T.textAccent || '#cc8833', fontStyle: 'bold'
        }).setOrigin(0.5));

        this.party.forEach((merc, idx) => {
            const stats = merc.getStats();
            const mx = 400 + idx * 160;
            _ui(this.add.text(mx, partyY + 30, `${merc.name}`, {
                fontSize: '12px', fontFamily: T.fontFamily || 'monospace',
                color: T.textPrimary || '#e8d8c0', fontStyle: 'bold'
            }).setOrigin(0.5));
            _ui(this.add.text(mx, partyY + 48, `Lv.${merc.level} ${merc.classKey}`, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5));
            _ui(this.add.text(mx, partyY + 64, `HP:${stats.hp} ATK:${stats.atk} DEF:${stats.def}`, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
                color: T.textSecondary || '#b8a888'
            }).setOrigin(0.5));
        });

        // 출발 버튼
        _ui(UIButton.create(this, 640, 550, 200, 44, '▶ 모디파이어 선택', {
            variant: 'primary', fontSize: 15,
            onClick: () => this._showModifierSelect()
        }));

        // 뒤로가기
        _ui(UIButton.create(this, 640, 610, 120, 30, '← 마을', {
            variant: 'ghost', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: this.gameState })
        }));
    }

    // ─── 모디파이어 선택 UI ─────────────────────────────────────
    _showModifierSelect() {
        this._clearUI();
        this._uiElements = [];
        const _ui = (obj) => { this._uiElements.push(obj); return obj; };
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        this.phase = 'modifier';

        this.positiveChoices = generatePositiveModifierChoices(false);
        this.negativeModifier = pickNegativeModifier();
        this.selectedPositive = null;

        _ui(this.add.text(640, 30, '◈  🎲 모디파이어 선택  ◈', {
            fontSize: '20px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5));

        _ui(this.add.text(640, 60, `${this.selectedFloor}층 도전`, {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5));

        // 양성 모디파이어 (3택1)
        _ui(this.add.text(640, 100, '✨ 양성 모디파이어 — 1개 선택', {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSuccess || '#66bb55'
        }).setOrigin(0.5));

        const cardW = 300, cardH = 100, gap = 30;
        const totalW = 3 * cardW + 2 * gap;
        const startX = 640 - totalW / 2 + cardW / 2;

        this.positiveChoices.forEach((mod, idx) => {
            const cx = startX + idx * (cardW + gap);
            const cy = 190;

            const gfx = _ui(this.add.graphics());
            const drawModCard = (fill, stroke, strokeAlpha) => {
                gfx.clear();
                gfx.fillStyle(fill, 1);
                gfx.fillRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
                gfx.lineStyle(2, stroke, strokeAlpha);
                gfx.strokeRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
            };
            drawModCard(0x1a2a18, 0x44aa44, 0.5);

            _ui(this.add.text(cx, cy - 25, mod.name, {
                fontSize: '15px', fontFamily: T.fontFamily || 'monospace',
                color: T.textSuccess || '#66bb55', fontStyle: 'bold'
            }).setOrigin(0.5));

            _ui(this.add.text(cx, cy + 5, mod.desc, {
                fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
                color: T.textSecondary || '#b8a888',
                wordWrap: { width: cardW - 20 }, align: 'center'
            }).setOrigin(0.5));

            const hitZone = _ui(this.add.zone(cx, cy, cardW, cardH).setInteractive({ useHandCursor: true }));
            hitZone.on('pointerdown', () => {
                this.selectedPositive = mod;
                this._drawModifierConfirm();
            });
            hitZone.on('pointerover', () => drawModCard(0x2a3a28, 0x66cc66, 1));
            hitZone.on('pointerout', () => drawModCard(0x1a2a18, 0x44aa44, 0.5));
        });

        // 음성 모디파이어 (강제)
        const negY = 340;
        _ui(this.add.text(640, negY, '💀 음성 모디파이어 — 강제 부여', {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace',
            color: T.textDanger || '#cc4422'
        }).setOrigin(0.5));

        const negGfx = _ui(this.add.graphics());
        negGfx.fillStyle(0x2a1a14, 1);
        negGfx.fillRoundedRect(640 - 200, negY + 15, 400, 70, 8);
        negGfx.lineStyle(2, 0xcc4422, 0.5);
        negGfx.strokeRoundedRect(640 - 200, negY + 15, 400, 70, 8);

        _ui(this.add.text(640, negY + 35, this.negativeModifier.name, {
            fontSize: '15px', fontFamily: T.fontFamily || 'monospace',
            color: T.textDanger || '#cc4422', fontStyle: 'bold'
        }).setOrigin(0.5));

        _ui(this.add.text(640, negY + 58, this.negativeModifier.desc, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        }).setOrigin(0.5));

        // 뒤로가기
        _ui(UIButton.create(this, 640, 550, 120, 30, '← 층 선택', {
            variant: 'ghost', fontSize: 12,
            onClick: () => { this.phase = 'floor'; this._drawFloorSelect(); }
        }));
    }

    // ─── 모디파이어 확인 후 출발 ────────────────────────────────
    _drawModifierConfirm() {
        this._clearUI();
        this._uiElements = [];
        const _ui = (obj) => { this._uiElements.push(obj); return obj; };
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        _ui(this.add.text(640, 80, '◈  ⚔ 전투 준비 완료  ◈', {
            fontSize: '22px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5));

        _ui(this.add.text(640, 120, `${this.selectedFloor}층 도전`, {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace',
            color: T.textAccent || '#cc8833'
        }).setOrigin(0.5));

        // 선택 결과 표시
        _ui(this.add.text(640, 180, `✨ ${this.selectedPositive.name}`, {
            fontSize: '15px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSuccess || '#66bb55', fontStyle: 'bold'
        }).setOrigin(0.5));
        _ui(this.add.text(640, 205, this.selectedPositive.desc, {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        }).setOrigin(0.5));

        _ui(this.add.text(640, 260, `💀 ${this.negativeModifier.name}`, {
            fontSize: '15px', fontFamily: T.fontFamily || 'monospace',
            color: T.textDanger || '#cc4422', fontStyle: 'bold'
        }).setOrigin(0.5));
        _ui(this.add.text(640, 285, this.negativeModifier.desc, {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        }).setOrigin(0.5));

        // 출발
        _ui(UIButton.create(this, 640, 400, 220, 50, '▶ 전투 시작!', {
            variant: 'danger', fontSize: 17,
            onClick: () => this._startBattle()
        }));

        // 다시 선택
        _ui(UIButton.create(this, 640, 470, 140, 30, '← 다시 선택', {
            variant: 'ghost', fontSize: 12,
            onClick: () => this._showModifierSelect()
        }));
    }

    _startBattle() {
        this.scene.start('CargoBattleScene', {
            gameState: this.gameState,
            party: this.party,
            floor: this.selectedFloor,
            positiveModifier: this.selectedPositive,
            negativeModifier: this.negativeModifier
        });
    }

    _clearUI() {
        if (this._uiElements) {
            this._uiElements.forEach(obj => { if (obj && obj.destroy) obj.destroy(); });
        }
        this._uiElements = [];
    }
}
