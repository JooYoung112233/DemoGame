/**
 * Blackout 전투 프로토타입 선택 씬.
 */
class BlackoutProtoSelectScene extends Phaser.Scene {
    constructor() { super('BlackoutProtoSelectScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.party = data.party || [];
        this.zoneKey = data.zoneKey || 'blackout';
    }

    create() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        this.add.rectangle(640, 360, 1280, 720, 0x0a0810);

        // 배경 안개
        const gfx = this.add.graphics();
        for (let i = 0; i < 30; i++) {
            const fx = Phaser.Math.Between(0, 1280);
            const fy = Phaser.Math.Between(0, 720);
            gfx.fillStyle(0x6644aa, 0.03);
            gfx.fillCircle(fx, fy, Phaser.Math.Between(40, 120));
        }

        // 헤더
        const hBg = this.add.graphics();
        hBg.fillStyle(T.headerBg || 0x1e1810, 0.8);
        hBg.fillRect(0, 0, 1280, 55);
        hBg.lineStyle(1, 0x6644aa, 0.4);
        hBg.lineBetween(0, 55, 1280, 55);

        this.add.text(640, 27, '◈  🔦 Blackout — 전투 프로토타입  ◈', {
            fontSize: '20px', fontFamily: T.fontFamily || 'monospace',
            color: '#bb88ff', fontStyle: 'bold',
            stroke: '#000', strokeThickness: 2
        }).setOrigin(0.5);

        this.add.text(640, 75, '저주받은 저택에서 어떤 전투를 시험해볼지 선택', {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5);

        // 파티 표시
        this._drawPartyRow(640, 120);

        // 3개 카드
        this._drawCard(180, 230, '그리드 통합', '🗺',
            '마인스위퍼 그리드 위에서 직접 전투.\n적은 어둠에 숨고, 횃불로 비추는\n셀만 보인다. 위치 잡기가 전부.',
            ['• 5×5 셀, 턴제 SPD', '• 횃불 든 용병 인접 = 빛', '• 어둠 적은 위험 숫자로만'],
            0x4477cc,
            () => this._launch('BlackoutGridScene'));

        this._drawCard(530, 230, '빛/어둠 듀얼', '🌗',
            '빛 트랙과 어둠 트랙.\n빛은 표적, 어둠은 잠복.\n트랙 전환과 차징의 게임.',
            ['• 2 트랙 (빛/어둠)', '• 어둠 차징 = 다음 ×2', '• 클래스마다 트랙 적성'],
            0xaa44cc,
            () => this._launch('BlackoutLaneScene'));

        this._drawCard(880, 230, '봉인 의식', '🔮',
            'HP를 깎는 게 아니라\n룬 시퀀스를 순서대로 입력해\n적을 봉인한다. 횃불 카운트.',
            ['• 적마다 룬 시퀀스', '• 클래스 ↔ 룬 종류', '• 횃불 꺼지면 시퀀스 안 보임'],
            0xcc44aa,
            () => this._launch('BlackoutRuneScene'));

        // 안내문
        this.add.text(640, 600, '※ 프로토타입 단계: 보상/저주 누적 없음. 단순히 전투만 시험.', {
            fontSize: '11px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5);

        this.add.text(640, 625, '플레이 후 느낌이 가장 좋은 방향으로 정식 전투를 만들 예정.', {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5).setAlpha(0.7);

        // 돌아가기
        UIButton.create(this, 100, 27, 130, 28, '← 출발 게이트', {
            variant: 'ghost', fontSize: 11,
            onClick: () => this.scene.start('DeployScene', {
                gameState: this.gameState,
                selectedZone: this.zoneKey,
                deployedIds: this.party.map(m => m.id)
            })
        });
    }

    _drawPartyRow(cx, cy) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const w = 600, h = 65;
        const bg = this.add.graphics();
        bg.fillStyle(T.panelFill || 0x2a2218, 0.7);
        bg.fillRoundedRect(cx - w / 2, cy - h / 2, w, h, 6);
        bg.lineStyle(1, 0x332244, 0.5);
        bg.strokeRoundedRect(cx - w / 2, cy - h / 2, w, h, 6);

        this.add.text(cx - w / 2 + 10, cy - h / 2 + 6, '편성된 파티', {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        });

        const slotW = 130;
        const startX = cx - (this.party.length - 1) * slotW / 2;
        this.party.forEach((merc, i) => {
            const sx = startX + i * slotW;
            const base = merc.getBaseClass();
            const stats = merc.getStats();
            const rolePos = (merc.classKey === 'warrior' || merc.classKey === 'rogue') ? '전열' : '후열';

            this.add.circle(sx, cy - 2, 14, base.color, 0.9);
            this.add.text(sx, cy - 2, base.icon, { fontSize: '14px' }).setOrigin(0.5);
            this.add.text(sx, cy + 18, merc.name, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
                color: T.textPrimary || '#e8d8c0'
            }).setOrigin(0.5);
            this.add.text(sx, cy + 30, `${rolePos} Lv.${merc.level}`, {
                fontSize: '8px', fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5);
        });
    }

    _drawCard(x, y, title, icon, desc, bullets, accent, onPick) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const w = 250, h = 320;
        const bg = this.add.graphics();

        const drawCardBg = (fill, strokeAlpha) => {
            bg.clear();
            bg.fillStyle(fill, 1);
            bg.fillRoundedRect(x, y, w, h, 8);
            bg.fillStyle(0xffffff, 0.03);
            bg.fillRect(x + 2, y + 2, w - 4, 20);
            bg.lineStyle(2, accent, strokeAlpha);
            bg.strokeRoundedRect(x, y, w, h, 8);
        };
        drawCardBg(T.cardFill || 0x231e14, 0.6);

        this.add.text(x + w / 2, y + 30, icon, { fontSize: '40px' }).setOrigin(0.5);
        this.add.text(x + w / 2, y + 80, title, {
            fontSize: '15px', fontFamily: T.fontFamily || 'monospace',
            color: `#${accent.toString(16).padStart(6, '0')}`,
            fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(x + w / 2, y + 120, desc, {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888',
            align: 'center', wordWrap: { width: w - 30 }
        }).setOrigin(0.5);

        bullets.forEach((b, i) => {
            this.add.text(x + 16, y + 185 + i * 16, b, {
                fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            });
        });

        UIButton.create(this, x + w / 2, y + h - 30, w - 30, 34, '플레이', {
            color: accent, hoverColor: 0xffffff, textColor: '#ffffff', fontSize: 12,
            onClick: onPick
        });

        // 카드 호버
        const hitZone = this.add.zone(x + w / 2, y + h / 2 - 15, w, h - 50).setInteractive({ useHandCursor: true });
        hitZone.on('pointerover', () => drawCardBg(T.cardHover || 0x3a3020, 1));
        hitZone.on('pointerout', () => drawCardBg(T.cardFill || 0x231e14, 0.6));
        hitZone.on('pointerdown', onPick);
    }

    _launch(sceneKey) {
        this.party.forEach(m => {
            m.currentHp = m.getStats().hp;
            m.alive = true;
        });

        this.scene.start(sceneKey, {
            gameState: this.gameState,
            party: this.party,
            zoneKey: this.zoneKey
        });
    }
}
