class TitleScene extends Phaser.Scene {
    constructor() { super('TitleScene'); }

    create() {
        const cx = 640, cy = 360;

        this.add.rectangle(cx, cy, 1280, 720, 0x0a0a1a);

        this.add.text(cx, 160, '⚔', { fontSize: '64px' }).setOrigin(0.5);
        this.add.text(cx, 240, '용병 길드', {
            fontSize: '40px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);
        this.add.text(cx, 290, '판타지 로그라이크 루트 & 길드 경영', {
            fontSize: '14px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5);

        const hasSave = SaveManager.hasSave();

        UIButton.create(this, cx, 400, 200, 44, '새 게임', {
            color: 0x446688, hoverColor: 0x5588aa, textColor: '#ffffff',
            fontSize: 16,
            onClick: () => {
                if (hasSave) {
                    this._showConfirm();
                } else {
                    this._startNewGame();
                }
            }
        });

        if (hasSave) {
            UIButton.create(this, cx, 460, 200, 44, '이어하기', {
                color: 0xffaa44, hoverColor: 0xffcc66, textColor: '#000000',
                fontSize: 16,
                onClick: () => this._continueGame()
            });
        }

        // === 테스트 모드 — 컨텐츠 다 열린 상태로 시작 ===
        UIButton.create(this, cx, hasSave ? 530 : 470, 280, 38, '🧪 테스트 모드 (컨텐츠 풀 해금)', {
            color: 0x884466, hoverColor: 0xaa5577, textColor: '#ffffff', fontSize: 13,
            onClick: () => this._startTestMode()
        });
        this.add.text(cx, hasSave ? 562 : 502, '길드 Lv.8 · 50,000G · 용병 8명 · 모든 시설 해금 · 구역 Lv.5', {
            fontSize: '10px', fontFamily: 'monospace', color: '#998899'
        }).setOrigin(0.5);

        this.add.text(cx, 650, 'Demo 9 — Guild Management Roguelike', {
            fontSize: '11px', fontFamily: 'monospace', color: '#444466'
        }).setOrigin(0.5);
    }

    /** 테스트 모드 — 컨텐츠 다 열린 풍족한 상태로 시작 */
    _startTestMode() {
        const hasSave = SaveManager.hasSave();
        if (hasSave) {
            // 저장 덮어쓰기 경고
            const cx = 640, cy = 360;
            const overlay = this.add.rectangle(cx, cy, 1280, 720, 0x000000, 0.7).setDepth(100).setInteractive();
            const panel = UIPanel.create(this, cx - 200, cy - 70, 400, 140, { title: '⚠ 기존 저장이 덮어써집니다' });
            panel.setDepth(101);
            const info = this.add.text(cx, cy - 10, '테스트 모드로 시작하면\n현재 저장이 삭제됩니다', {
                fontSize: '12px', fontFamily: 'monospace', color: '#ccccdd', align: 'center'
            }).setOrigin(0.5).setDepth(102);
            const yes = UIButton.create(this, cx - 70, cy + 30, 120, 34, '확인 (덮어쓰기)', {
                color: 0x884466, hoverColor: 0xaa5577, textColor: '#ffffff', fontSize: 11, depth: 102,
                onClick: () => { overlay.destroy(); panel.destroy(); info.destroy(); yes.destroy(); no.destroy(); this._applyTestMode(); }
            });
            const no = UIButton.create(this, cx + 70, cy + 30, 100, 34, '취소', {
                color: 0x555555, hoverColor: 0x777777, textColor: '#ffffff', fontSize: 11, depth: 102,
                onClick: () => { overlay.destroy(); panel.destroy(); info.destroy(); yes.destroy(); no.destroy(); }
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

        // === 메인 클리어 누적 (서브 파견 해금) ===
        const subClears = GuildManager.SUB_UNLOCK_CLEARS || 3;
        ['bloodpit', 'cargo', 'blackout'].forEach(z => {
            for (let lv = 1; lv <= 5; lv++) {
                gs.zoneClearCount[`${z}_${lv}`] = subClears;
            }
        });

        // === 훈련 포인트 ===
        gs.trainingPoints = 30;

        // === 용병 8명 — 클래스/희귀도/레벨 다양하게 ===
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
            // 레벨 업
            merc.level = spec.level;
            merc.xp = 0;
            merc._maxHp = merc.getStats().hp;
            merc.currentHp = merc._maxHp;
            // 친화도 — 레벨 2 + 포인트 2 (트리 노드 찍어볼 수 있게)
            ['bloodpit', 'cargo', 'blackout'].forEach(z => {
                if (merc.affinityLevel) merc.affinityLevel[z] = 2;
                if (merc.affinityXp) merc.affinityXp[z] = 0;
                if (merc.affinityPoints) merc.affinityPoints[z] = 2;
            });
            gs.roster.push(merc);
        });

        // === 모집 풀도 채워둠 ===
        MercenaryManager.generateRecruitPool(gs);

        // === 본드 시드 — 일부 페어에 높은 본드 (테스트용) ===
        // 첫 4명을 자주 같이 출전한 것처럼 본드 누적
        if (typeof BondManager !== 'undefined' && gs.roster.length >= 4) {
            const corePartyMercs = gs.roster.slice(0, 4);
            // 메인 전투 성공 10회분 가량 누적
            for (let i = 0; i < 10; i++) {
                BondManager.updateBonds(gs, corePartyMercs, true, 'main');
            }
            // 5번째와 1-2번째도 약간
            if (gs.roster[4]) {
                const subParty = [gs.roster[0], gs.roster[1], gs.roster[4]];
                for (let i = 0; i < 4; i++) {
                    BondManager.updateBonds(gs, subParty, true, 'sub');
                }
            }
        }

        // === 보관함에 다양한 장비/소재 ===
        if (typeof generateItem === 'function') {
            const zones = ['bloodpit', 'cargo', 'blackout'];
            for (let i = 0; i < 15; i++) {
                const zone = zones[i % 3];
                const itm = generateItem(zone, gs.guildLevel, 2);  // 희귀도 보너스 +2
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
        MercenaryManager.generateRecruitPool(gameState);
        GuildManager.addMessage(gameState, '길드가 설립되었습니다. 용병을 모집하세요!');
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
        const cx = 640, cy = 360;
        const overlay = this.add.rectangle(cx, cy, 1280, 720, 0x000000, 0.7).setDepth(100).setInteractive();
        const panel = UIPanel.create(this, cx - 160, cy - 60, 320, 120, { title: '기존 저장을 삭제하시겠습니까?' });
        panel.setDepth(101);

        const yesBtn = UIButton.create(this, cx - 60, cy + 20, 100, 34, '삭제 후 시작', {
            color: 0xff4444, hoverColor: 0xff6666, textColor: '#ffffff', fontSize: 12,
            onClick: () => { overlay.destroy(); panel.destroy(); yesBtn.destroy(); noBtn.destroy(); this._startNewGame(); }
        });
        yesBtn.setDepth(102);

        const noBtn = UIButton.create(this, cx + 60, cy + 20, 100, 34, '취소', {
            color: 0x555555, hoverColor: 0x777777, textColor: '#ffffff', fontSize: 12,
            onClick: () => { overlay.destroy(); panel.destroy(); yesBtn.destroy(); noBtn.destroy(); }
        });
        noBtn.setDepth(102);
    }
}
