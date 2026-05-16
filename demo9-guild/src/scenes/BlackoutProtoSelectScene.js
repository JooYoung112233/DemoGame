/**
 * Blackout 전투 프로토타입 선택 씬.
 *
 * 데모 단계: 전투 자체의 재미를 검증하기 위해 3개의 독립 전투 엔진을
 * 골라서 플레이해볼 수 있게 한다. 탐색 시스템과는 분리.
 *
 *  - Grid  : 마인스위퍼 그리드 위에서 전투 (탐색-전투 통합)
 *  - Lane  : 빛/어둠 듀얼 트랙 (포지셔닝 게임)
 *  - Rune  : 룬 시퀀스 봉인 (퍼즐형)
 */
class BlackoutProtoSelectScene extends Phaser.Scene {
    constructor() { super('BlackoutProtoSelectScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.party = data.party || [];
        this.zoneKey = data.zoneKey || 'blackout';
    }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x05050f);

        // 배경 안개
        const gfx = this.add.graphics();
        for (let i = 0; i < 30; i++) {
            const fx = Phaser.Math.Between(0, 1280);
            const fy = Phaser.Math.Between(0, 720);
            gfx.fillStyle(0x6644aa, 0.04);
            gfx.fillCircle(fx, fy, Phaser.Math.Between(40, 120));
        }

        this.add.text(640, 50, '🔦 Blackout — 전투 프로토타입', {
            fontSize: '24px', fontFamily: 'monospace', color: '#bb88ff', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(640, 85, '저주받은 저택에서 어떤 전투를 시험해볼지 선택', {
            fontSize: '12px', fontFamily: 'monospace', color: '#776699'
        }).setOrigin(0.5);

        // 파티 표시
        this._drawPartyRow(640, 130);

        // 3개 카드
        this._drawCard(180, 250, '그리드 통합', '🗺',
            '마인스위퍼 그리드 위에서 직접 전투.\n적은 어둠에 숨고, 횃불로 비추는\n셀만 보인다. 위치 잡기가 전부.',
            ['• 5×5 셀, 턴제 SPD', '• 횃불 든 용병 인접 = 빛', '• 어둠 적은 위험 숫자로만'],
            0x4477cc,
            () => this._launch('BlackoutGridScene'));

        this._drawCard(530, 250, '빛/어둠 듀얼', '🌗',
            '빛 트랙과 어둠 트랙.\n빛은 표적, 어둠은 잠복.\n트랙 전환과 차징의 게임.',
            ['• 2 트랙 (빛/어둠)', '• 어둠 차징 = 다음 ×2', '• 클래스마다 트랙 적성'],
            0xaa44cc,
            () => this._launch('BlackoutLaneScene'));

        this._drawCard(880, 250, '봉인 의식', '🔮',
            'HP를 깎는 게 아니라\n룬 시퀀스를 순서대로 입력해\n적을 봉인한다. 횃불 카운트.',
            ['• 적마다 룬 시퀀스', '• 클래스 ↔ 룬 종류', '• 횃불 꺼지면 시퀀스 안 보임'],
            0xcc44aa,
            () => this._launch('BlackoutRuneScene'));

        // 안내문
        this.add.text(640, 600, '※ 프로토타입 단계: 보상/저주 누적 없음. 단순히 전투만 시험.', {
            fontSize: '12px', fontFamily: 'monospace', color: '#665577'
        }).setOrigin(0.5);

        this.add.text(640, 625, '플레이 후 느낌이 가장 좋은 방향으로 정식 전투를 만들 예정.', {
            fontSize: '11px', fontFamily: 'monospace', color: '#554466'
        }).setOrigin(0.5);

        // 돌아가기
        UIButton.create(this, 100, 35, 130, 30, '← 출발 게이트', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaccee', fontSize: 12,
            onClick: () => this.scene.start('DeployScene', {
                gameState: this.gameState,
                selectedZone: this.zoneKey,
                deployedIds: this.party.map(m => m.id)
            })
        });
    }

    _drawPartyRow(cx, cy) {
        const w = 600, h = 70;
        this.add.rectangle(cx, cy, w, h, 0x0a0a1a, 0.7).setStrokeStyle(1, 0x332244, 0.5);
        this.add.text(cx - w / 2 + 10, cy - h / 2 + 6, '편성된 파티', {
            fontSize: '10px', fontFamily: 'monospace', color: '#776699'
        });

        const slotW = 130;
        const startX = cx - (this.party.length - 1) * slotW / 2;
        this.party.forEach((merc, i) => {
            const sx = startX + i * slotW;
            const base = merc.getBaseClass();
            const stats = merc.getStats();
            const hpRatio = merc.currentHp / stats.hp;
            const rolePos = (merc.classKey === 'warrior' || merc.classKey === 'rogue') ? '전열' : '후열';

            this.add.circle(sx, cy, 14, base.color, 0.9);
            this.add.text(sx, cy, base.icon, { fontSize: '14px' }).setOrigin(0.5);
            this.add.text(sx, cy + 22, merc.name, {
                fontSize: '10px', fontFamily: 'monospace', color: '#ccccdd'
            }).setOrigin(0.5);
            this.add.text(sx, cy + 35, `${rolePos} Lv.${merc.level}`, {
                fontSize: '9px', fontFamily: 'monospace', color: '#888899'
            }).setOrigin(0.5);
        });
    }

    _drawCard(x, y, title, icon, desc, bullets, accent, onPick) {
        const w = 250, h = 310;
        const bg = this.add.graphics();
        bg.fillStyle(0x111122, 1);
        bg.fillRoundedRect(x, y, w, h, 8);
        bg.lineStyle(2, accent, 0.7);
        bg.strokeRoundedRect(x, y, w, h, 8);

        this.add.text(x + w / 2, y + 30, icon, { fontSize: '40px' }).setOrigin(0.5);
        this.add.text(x + w / 2, y + 80, title, {
            fontSize: '16px', fontFamily: 'monospace',
            color: `#${accent.toString(16).padStart(6, '0')}`,
            fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(x + w / 2, y + 120, desc, {
            fontSize: '11px', fontFamily: 'monospace', color: '#aaaabb',
            align: 'center', wordWrap: { width: w - 30 }
        }).setOrigin(0.5);

        bullets.forEach((b, i) => {
            this.add.text(x + 16, y + 180 + i * 18, b, {
                fontSize: '10px', fontFamily: 'monospace', color: '#8899aa'
            });
        });

        UIButton.create(this, x + w / 2, y + h - 30, w - 30, 36, '플레이', {
            color: accent, hoverColor: 0xffffff, textColor: '#ffffff', fontSize: 13,
            onClick: onPick
        });
    }

    _launch(sceneKey) {
        // 파티 currentHp 풀로 회복 (프로토타입이라 깨끗하게 시작)
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
