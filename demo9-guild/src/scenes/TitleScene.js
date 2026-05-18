class TitleScene extends Phaser.Scene {
    constructor() { super('TitleScene'); }

    create() {
        const cx = 640, cy = 360;
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        // 배경
        this.add.rectangle(cx, cy, 1280, 720, T.bg || 0x1a1510);

        // 비네팅
        const v = this.add.graphics();
        v.fillStyle(0x000000, 0.2);
        v.fillRect(0, 0, 1280, 6); v.fillRect(0, 714, 1280, 6);
        v.fillRect(0, 0, 6, 720); v.fillRect(1274, 0, 6, 720);

        // 코너 장식
        const c = this.add.graphics();
        c.lineStyle(1.5, T.ornament || 0x8a7a4a, 0.25);
        c.lineBetween(0, 25, 25, 0); c.lineBetween(0, 40, 40, 0);
        c.lineBetween(1255, 0, 1280, 25); c.lineBetween(1240, 0, 1280, 40);
        c.lineBetween(0, 695, 25, 720); c.lineBetween(0, 680, 40, 720);
        c.lineBetween(1255, 720, 1280, 695); c.lineBetween(1240, 720, 1280, 680);

        // 중앙 장식 프레임
        const frame = this.add.graphics();
        frame.lineStyle(1, T.panelStroke || 0x5a4a2a, 0.3);
        frame.strokeRoundedRect(cx - 220, 120, 440, 530, 12);
        frame.lineStyle(0.5, T.ornament || 0x8a7a4a, 0.15);
        frame.strokeRoundedRect(cx - 216, 124, 432, 522, 10);

        // 아이콘
        this.add.text(cx, 175, '⚔', { fontSize: '64px' }).setOrigin(0.5);

        // 제목
        this.add.text(cx, 255, '용병 길드', {
            fontSize: '40px', fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5);

        // 장식 구분선
        const dl = this.add.graphics();
        dl.lineStyle(1, T.ornament || 0x8a7a4a, 0.3);
        dl.lineBetween(cx - 120, 285, cx + 120, 285);
        this.add.text(cx, 285, '◆', {
            fontSize: '8px', fontFamily: T.fontFamily || 'monospace',
            color: T.textAccent || '#cc8833'
        }).setOrigin(0.5);

        // 부제
        this.add.text(cx, 305, '판타지 로그라이크 루트 & 길드 경영', {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5);

        const hasSave = SaveManager.hasSave();

        // 새 게임 버튼
        UIButton.create(this, cx, 380, 220, 44, '새 게임', {
            variant: hasSave ? 'ghost' : 'primary',
            fontSize: 16,
            onClick: () => {
                if (hasSave) {
                    this._showConfirm();
                } else {
                    this._startNewGame();
                }
            }
        });

        // 이어하기 버튼
        if (hasSave) {
            UIButton.create(this, cx, 440, 220, 44, '이어하기', {
                variant: 'primary',
                fontSize: 16,
                onClick: () => this._continueGame()
            });
        }

        // 테스트 모드
        UIButton.create(this, cx, hasSave ? 520 : 460, 280, 36, '🧪 테스트 모드 (컨텐츠 풀 해금)', {
            variant: 'danger',
            fontSize: 12,
            onClick: () => this._startTestMode()
        });
        this.add.text(cx, hasSave ? 550 : 490, '길드 Lv.8 · 50,000G · 용병 8명 · 모든 시설 해금 · 구역 Lv.5', {
            fontSize: '9px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5);

        // 하단
        this.add.text(cx, 660, 'Demo 9 — Guild Management Roguelike', {
            fontSize: '10px', fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5).setAlpha(0.5);
    }

    /** 테스트 모드 — 컨텐츠 다 열린 풍족한 상태로 시작 */
    _startTestMode() {
        const hasSave = SaveManager.hasSave();
        if (hasSave) {
            UIModal.confirm(this, {
                title: '⚠ 기존 저장 덮어쓰기',
                message: ['테스트 모드로 시작하면', '현재 저장이 삭제됩니다'],
                confirmLabel: '확인 (덮어쓰기)',
                cancelLabel: '취소',
                danger: true,
                onConfirm: () => this._applyTestMode()
            });
            return;
        }
        this._applyTestMode();
    }

    _applyTestMode() {
        SaveManager.deleteSave();
        const gs = GuildManager.createDefaultState();

        // === 길드 ===
        gs.guildLevel = 8;
        gs.guildXp = 2500;
        gs.gold = 50000;

        // === 모든 시설 해금 ===
        gs.unlockedFacilities = FACILITY_KEYS.slice();

        // === 모든 구역 Lv.5 ===
        gs.zoneLevel = { bloodpit: 5, cargo: 5, blackout: 5 };
        gs.cargoFloor = { maxUnlocked: 10, currentFloor: 1 };

        // === 길드 회관 ===
        gs.guildHall = {
            operations: 3, infrastructure: 3, recovery: 3, automation: 3,
            intel: 3, pit_control: 3, cargo_control: 3, dark_control: 3
        };
        gs.guildReputation = 50;

        // === 메인 클리어 누적 ===
        const subClears = GuildManager.SUB_UNLOCK_CLEARS || 3;
        ['bloodpit', 'cargo', 'blackout'].forEach(z => {
            for (let lv = 1; lv <= 5; lv++) {
                gs.zoneClearCount[`${z}_${lv}`] = subClears;
            }
        });

        // === 훈련 포인트 ===
        gs.trainingPoints = 30;

        // === 용병 8명 ===
        const testRoster = [
            { cls: 'warrior',   rarity: 'rare',      level: 8 },
            { cls: 'warrior',   rarity: 'uncommon',  level: 6 },
            { cls: 'rogue',     rarity: 'epic',      level: 7 },
            { cls: 'archer',    rarity: 'rare',      level: 6 },
            { cls: 'mage',      rarity: 'epic',      level: 7 },
            { cls: 'priest',    rarity: 'rare',      level: 6 },
            { cls: 'alchemist', rarity: 'uncommon',  level: 5 },
            { cls: 'rogue',     rarity: 'legendary', level: 8 }
        ];
        testRoster.forEach(spec => {
            const traits = (typeof getRandomTraits === 'function') ? getRandomTraits(spec.rarity, spec.cls) : [];
            const name = (typeof generateMercName === 'function') ? generateMercName() : `${spec.cls}_${Math.random().toString(36).slice(2,6)}`;
            const merc = new Mercenary(spec.cls, spec.rarity, name, traits);
            merc.level = spec.level;
            merc.xp = 0;
            merc._maxHp = merc.getStats().hp;
            merc.currentHp = merc._maxHp;
            ['bloodpit', 'cargo', 'blackout'].forEach(z => {
                if (merc.affinityLevel) merc.affinityLevel[z] = 2;
                if (merc.affinityXp) merc.affinityXp[z] = 0;
                if (merc.affinityPoints) merc.affinityPoints[z] = 2;
            });
            gs.roster.push(merc);
        });

        MercenaryManager.generateRecruitPool(gs);

        if (typeof BondManager !== 'undefined' && gs.roster.length >= 4) {
            const corePartyMercs = gs.roster.slice(0, 4);
            for (let i = 0; i < 10; i++) {
                BondManager.updateBonds(gs, corePartyMercs, true, 'main');
            }
            if (gs.roster[4]) {
                const subParty = [gs.roster[0], gs.roster[1], gs.roster[4]];
                for (let i = 0; i < 4; i++) {
                    BondManager.updateBonds(gs, subParty, true, 'sub');
                }
            }
        }

        if (typeof generateItem === 'function') {
            const zones = ['bloodpit', 'cargo', 'blackout'];
            for (let i = 0; i < 15; i++) {
                const zone = zones[i % 3];
                const itm = generateItem(zone, gs.guildLevel, 2);
                if (itm) StorageManager.addItem(gs, itm);
            }
        }

        GuildManager.addMessage(gs, '🧪 테스트 모드 시작 — 모든 컨텐츠 해금');
        SaveManager.save(gs);
        this.scene.start('TownScene', { gameState: gs });
    }

    _startNewGame() {
        SaveManager.deleteSave();
        const gameState = GuildManager.createDefaultState();
        // 온보딩: 고정 스타터 파티 4명 (전사+도적+사제+궁수) 즉시 로스터 배치
        const starterParty = MercenaryManager.generateStarterParty();
        starterParty.forEach(merc => gameState.roster.push(merc));
        MercenaryManager.generateRecruitPool(gameState);
        GuildManager.addMessage(gameState, '길드가 설립되었습니다. 초기 용병 4명이 배치되었습니다!');
        gameState._isNewGame = true;  // 첫 플레이 시퀀스 트리거
        SaveManager.save(gameState);
        this.scene.start('TownScene', { gameState });
    }

    _continueGame() {
        const gameState = SaveManager.load();
        if (!gameState) {
            this._startNewGame();
            return;
        }
        this.scene.start('TownScene', { gameState });
    }

    _showConfirm() {
        UIModal.confirm(this, {
            title: '기존 저장 삭제',
            message: '기존 저장을 삭제하고 새로 시작합니다.',
            confirmLabel: '삭제 후 시작',
            cancelLabel: '취소',
            danger: true,
            onConfirm: () => this._startNewGame()
        });
    }
}
