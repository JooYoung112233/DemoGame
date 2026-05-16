/**
 * 후보 3: 봉인 의식 전투 (Blackout Prototype).
 *
 * 핵심 메카닉:
 *  - HP를 깎는 게 아니라 적의 약점 룬 시퀀스를 순서대로 입력해 봉인
 *  - 각 적: 3~5개 룬 시퀀스 (예: 🔥 → 💧 → ⚡)
 *  - 매 라운드 횃불 -1, 0 되면 시퀀스 안 보임 (실패)
 *  - 매 라운드 적이 무작위 아군 1명 공격
 *  - 잘못된 룬 입력 = 적이 즉시 카운터 (×2 데미지)
 *
 * 클래스 ↔ 입력 가능 룬:
 *  - warrior  : 🛡 (방패) — 강격, 카운터 흡수
 *  - rogue    : 🗡 (단검) — 어둠 룬 1회 입력 가능
 *  - mage     : 🔥💧❄ (원소 3종)
 *  - archer   : 🎯 (정밀) — 한 단계 건너뛰기 가능
 *  - priest   : ✨🩸 (정화/봉인)
 *  - alchemist: 💧🔥 (수액/폭발)
 *
 * 친밀도/협력 보너스 (간단화):
 *  - 같은 라운드 2명 연속 입력 시 콤보 +20% 봉인 효율 (다음 룬 자동 강조)
 */
class BlackoutRuneScene extends Phaser.Scene {
    constructor() { super('BlackoutRuneScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.party = data.party || [];
        this.zoneKey = data.zoneKey || 'blackout';
        this.zoneLevel = this.gameState.zoneLevel[this.zoneKey] || 1;
    }

    create() {
        this.MAX_TORCH = 12;
        this.torchTurns = this.MAX_TORCH;
        this.round = 1;
        this.gameOver = false;
        this.selectedMerc = null;
        this.comboChain = []; // 이번 라운드 입력한 멤버 id 순서
        this.actionsThisRound = {}; // mercId → 이번 라운드 행동했나

        this._sceneObjects = [];
        this._unitObjects = [];
        this._inputPanelObjects = [];
        this._enemyObjects = [];
        this._hudObjects = [];

        this.RUNES = {
            shield:  { icon: '🛡', name: '방패', color: 0xcc8844 },
            dagger:  { icon: '🗡', name: '단검', color: 0xaa44cc },
            fire:    { icon: '🔥', name: '불',   color: 0xff4422 },
            water:   { icon: '💧', name: '물',   color: 0x44aaff },
            ice:     { icon: '❄', name: '얼음', color: 0x88ccff },
            precise: { icon: '🎯', name: '정밀', color: 0x88cc44 },
            holy:    { icon: '✨', name: '정화', color: 0xffcc88 },
            seal:    { icon: '🩸', name: '봉인', color: 0xcc4488 },
            dark:    { icon: '🌑', name: '어둠', color: 0x6644aa }
        };

        this.CLASS_RUNES = {
            warrior:   ['shield'],
            rogue:     ['dagger', 'dark'],
            mage:      ['fire', 'water', 'ice'],
            archer:    ['precise'],
            priest:    ['holy', 'seal'],
            alchemist: ['water', 'fire']
        };

        this._drawBackground();
        this._spawnEnemies();
        this._initParty();
        this._drawHUD();
        this._drawEnemies();
        this._drawParty();
        this._drawInputPanel();

        this._showAnnounce('🔮 봉인 의식 — 룬 시퀀스를 완성하라', 0xbb88ff);
        this.time.delayedCall(800, () => this._tryAutoSelectMerc());
    }

    // ==================== INIT ====================

    _spawnEnemies() {
        this.enemies = [];
        const allRunes = ['shield', 'dagger', 'fire', 'water', 'ice', 'precise', 'holy', 'seal', 'dark'];
        // 1-2 적 (보스급)
        const enemyCount = this.zoneLevel >= 5 ? 2 : 1;
        for (let i = 0; i < enemyCount; i++) {
            const seqLen = 3 + Math.floor(this.zoneLevel / 3); // 3~5
            const sequence = [];
            for (let j = 0; j < seqLen; j++) {
                sequence.push(allRunes[Math.floor(Math.random() * allRunes.length)]);
            }
            this.enemies.push({
                id: 'enemy_' + i,
                name: `저주받은 영혼 ${i + 1}`,
                sequence,
                progress: 0,    // 입력 완료한 룬 인덱스
                sealed: false,
                atk: 12 + this.zoneLevel * 2,
                icon: '👹',
                color: 0x884466
            });
        }
    }

    _initParty() {
        // currentHp 보전
        this.party.forEach(m => {
            if (m.currentHp === undefined) m.currentHp = m.getStats().hp;
        });
    }

    // ==================== LOGIC ====================

    _availableMercs() {
        return this.party.filter(m => m.alive && m.currentHp > 0 && !this.actionsThisRound[m.id]);
    }

    _tryAutoSelectMerc() {
        if (this.gameOver) return;
        const avail = this._availableMercs();
        if (avail.length === 0) {
            this._endRound();
            return;
        }
        // 자동 선택 X — 사용자가 클릭하도록 안내
        this.selectedMerc = null;
        this._drawHUD();
        this._drawParty();
        this._drawInputPanel();
    }

    _selectMerc(merc) {
        if (this.actionsThisRound[merc.id]) return;
        this.selectedMerc = merc;
        this._drawHUD();
        this._drawParty();
        this._drawEnemies();
        this._drawInputPanel();
    }

    _inputRune(runeKey) {
        if (!this.selectedMerc) return;
        const aliveEnemies = this.enemies.filter(e => !e.sealed);
        if (aliveEnemies.length === 0) return;

        // 어느 적에게 입력할지 — 자동: 첫 번째 미봉인 적
        // (UX 단순화. 추후 적 선택 가능하게 확장)
        const target = aliveEnemies[0];
        const requiredRune = target.sequence[target.progress];

        if (runeKey === requiredRune) {
            // 정답
            target.progress++;
            this.actionsThisRound[this.selectedMerc.id] = true;
            this.comboChain.push(this.selectedMerc.id);

            const comboBonus = this.comboChain.length >= 2;
            this._showFloatText(this._enemyX(target), 220, comboBonus ? '✓ 콤보!' : '✓', '#88ff88');

            if (target.progress >= target.sequence.length) {
                target.sealed = true;
                this._showFloatText(this._enemyX(target), 250, `봉인 완료`, '#ffcc44');
                this._showAnnounce(`✨ ${target.name} 봉인!`, 0xffcc44);
            }

            this._drawEnemies();
            this._drawParty();

            // 모든 적 봉인?
            if (this.enemies.every(e => e.sealed)) {
                this.time.delayedCall(800, () => this._endBattle(true));
                return;
            }

            // 다음 멤버 선택 유도
            this.selectedMerc = null;
            this.time.delayedCall(400, () => this._tryAutoSelectMerc());
        } else {
            // 오답 — 적 카운터
            this._showFloatText(this._enemyX(target), 220, '✗ 카운터!', '#ff4444');
            this.actionsThisRound[this.selectedMerc.id] = true;
            this.comboChain = []; // 콤보 끊김
            this._counterAttack(target, this.selectedMerc, 2.0);

            this.selectedMerc = null;
            this._drawParty();
            this._drawEnemies();
            this.time.delayedCall(600, () => {
                if (this._checkBattleEnd()) return;
                this._tryAutoSelectMerc();
            });
        }
    }

    _endRound() {
        if (this.gameOver) return;
        // 적 일반 공격 (랜덤 아군)
        const aliveAllies = this.party.filter(m => m.alive && m.currentHp > 0);
        const aliveEnemies = this.enemies.filter(e => !e.sealed);
        if (aliveAllies.length > 0 && aliveEnemies.length > 0) {
            aliveEnemies.forEach(e => {
                const target = aliveAllies[Math.floor(Math.random() * aliveAllies.length)];
                this._counterAttack(e, target, 1.0);
            });
        }
        if (this._checkBattleEnd()) return;

        // 라운드 종료 — 횃불 소진
        this.torchTurns--;
        this.round++;
        this.actionsThisRound = {};
        this.comboChain = [];

        if (this.torchTurns <= 0) {
            this._showAnnounce('🌑 횃불이 꺼졌다... 룬이 안 보인다', 0x6644aa);
            this.time.delayedCall(1200, () => this._endBattle(false));
            return;
        }

        this._showAnnounce(`라운드 ${this.round} (횃불 ${this.torchTurns})`, 0xaaaaff);
        this._drawHUD();
        this._drawEnemies();
        this._drawParty();
        this.time.delayedCall(600, () => this._tryAutoSelectMerc());
    }

    _counterAttack(enemy, target, mult) {
        const stats = target.getStats();
        const rawDmg = enemy.atk * mult * (Math.random() * 0.2 + 0.9);
        const finalDmg = Math.max(1, Math.floor(rawDmg * (1 - stats.def / (stats.def + 80))));
        target.currentHp = Math.max(0, target.currentHp - finalDmg);
        if (target.currentHp <= 0) {
            target.alive = false;
        }
        this._showFloatText(this._partyX(target), 510, `-${finalDmg}`, '#ff4444');
    }

    _checkBattleEnd() {
        const aliveAllies = this.party.filter(m => m.alive && m.currentHp > 0).length;
        const aliveEnemies = this.enemies.filter(e => !e.sealed).length;
        if (aliveEnemies === 0) {
            this._endBattle(true);
            return true;
        }
        if (aliveAllies === 0) {
            this._endBattle(false);
            return true;
        }
        return false;
    }

    _endBattle(victory) {
        this.gameOver = true;
        this._clearGroup(this._inputPanelObjects);
        const overlay = this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.6).setDepth(200);
        const panel = this.add.graphics().setDepth(201);
        panel.fillStyle(0x111122, 0.95);
        panel.fillRoundedRect(440, 220, 400, 280, 10);
        panel.lineStyle(2, victory ? 0xffcc44 : 0xff4444, 0.8);
        panel.strokeRoundedRect(440, 220, 400, 280, 10);
        this.add.text(640, 270, victory ? '✨ 봉인 성공' : '💀 의식 실패', {
            fontSize: '32px', fontFamily: 'monospace',
            color: victory ? '#ffcc44' : '#ff4444', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(202);
        this.add.text(640, 330, victory ? '모든 영혼을 봉인했다' : (this.torchTurns <= 0 ? '횃불이 꺼졌다' : '파티가 무너졌다'), {
            fontSize: '14px', fontFamily: 'monospace', color: '#ccccdd'
        }).setOrigin(0.5).setDepth(202);
        this.add.text(640, 370, `라운드: ${this.round}  |  횃불: ${this.torchTurns}/${this.MAX_TORCH}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5).setDepth(202);

        UIButton.create(this, 640, 430, 240, 38, '프로토타입 메뉴로', {
            color: 0xcc44aa, hoverColor: 0xee55cc, textColor: '#ffffff', fontSize: 14, depth: 202,
            onClick: () => this.scene.start('BlackoutProtoSelectScene', {
                gameState: this.gameState, party: this.party, zoneKey: this.zoneKey
            })
        });
        UIButton.create(this, 640, 475, 240, 30, '마을로 돌아가기', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaccee', fontSize: 12, depth: 202,
            onClick: () => this.scene.start('TownScene', { gameState: this.gameState })
        });
    }

    // ==================== POSITIONING ====================

    _enemyX(enemy) {
        const idx = this.enemies.indexOf(enemy);
        const startX = 640 - (this.enemies.length - 1) * 200 / 2;
        return startX + idx * 200;
    }

    _partyX(merc) {
        const idx = this.party.indexOf(merc);
        const startX = 640 - (this.party.length - 1) * 130 / 2;
        return startX + idx * 130;
    }

    // ==================== DRAW ====================

    _drawBackground() {
        const gfx = this.add.graphics();
        gfx.fillGradientStyle(0x0a0820, 0x0a0820, 0x05050f, 0x05050f);
        gfx.fillRect(0, 0, 1280, 720);
        // 의식 원
        gfx.lineStyle(2, 0xcc44aa, 0.1);
        gfx.strokeCircle(640, 360, 280);
        gfx.lineStyle(1, 0xcc44aa, 0.15);
        gfx.strokeCircle(640, 360, 220);
        gfx.strokeCircle(640, 360, 150);
        // 안개
        for (let i = 0; i < 25; i++) {
            gfx.fillStyle(0xaa44cc, 0.04);
            gfx.fillCircle(Phaser.Math.Between(0, 1280), Phaser.Math.Between(0, 720), Phaser.Math.Between(40, 100));
        }
    }

    _drawHUD() {
        this._clearGroup(this._hudObjects);
        const _h = (o) => { this._hudObjects.push(o); return o; };

        _h(this.add.rectangle(640, 35, 1280, 70, 0x0a0a14, 0.9).setDepth(100));
        _h(this.add.text(20, 18, `🔮 봉인 의식 — 라운드 ${this.round}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#cc44aa', fontStyle: 'bold'
        }).setDepth(101));
        _h(this.add.text(20, 38, `구역 Lv.${this.zoneLevel} | 룬 시퀀스 입력으로 적 봉인`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#776688'
        }).setDepth(101));

        // 횃불 카운터
        const torchPct = this.torchTurns / this.MAX_TORCH;
        const torchColor = torchPct > 0.5 ? '#ffcc88' : torchPct > 0.25 ? '#ff8844' : '#ff4422';
        _h(this.add.text(640, 18, `🔦 횃불 ${this.torchTurns}/${this.MAX_TORCH} 라운드`, {
            fontSize: '14px', fontFamily: 'monospace', color: torchColor, fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(101));
        // 횃불 게이지
        const barX = 540, barY = 42, barW = 200, barH = 6;
        _h(this.add.rectangle(barX + barW / 2, barY + barH / 2, barW, barH, 0x331122).setDepth(101));
        const fillW = Math.max(0, barW * torchPct);
        if (fillW > 0) {
            _h(this.add.rectangle(barX + fillW / 2, barY + barH / 2, fillW, barH,
                torchPct > 0.5 ? 0xffcc88 : torchPct > 0.25 ? 0xff8844 : 0xff4422).setDepth(102));
        }

        // 콤보 표시
        if (this.comboChain.length >= 2) {
            _h(this.add.text(1260, 18, `🔗 콤보 ×${this.comboChain.length}`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
            }).setOrigin(1, 0).setDepth(101));
        }
        const avail = this._availableMercs().length;
        _h(this.add.text(1260, 38, `남은 행동: ${avail}/${this.party.filter(m => m.alive).length}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#aaaabb'
        }).setOrigin(1, 0).setDepth(101));

        _h(UIButton.create(this, 70, 700, 110, 25, '← 프로토 메뉴', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaccee', fontSize: 10, depth: 110,
            onClick: () => this.scene.start('BlackoutProtoSelectScene', {
                gameState: this.gameState, party: this.party, zoneKey: this.zoneKey
            })
        }));

        // 라운드 종료 버튼 (수동) — 행동 다 했을 때만
        if (avail === 0 && !this.gameOver) {
            _h(UIButton.create(this, 1180, 700, 100, 25, '라운드 진행 →', {
                color: 0x884488, hoverColor: 0xaa55aa, textColor: '#ffffff', fontSize: 10, depth: 110,
                onClick: () => this._endRound()
            }));
        } else if (!this.gameOver) {
            _h(UIButton.create(this, 1180, 700, 100, 25, '⏭ 라운드 종료', {
                color: 0x445566, hoverColor: 0x556677, textColor: '#aaccee', fontSize: 10, depth: 110,
                onClick: () => this._endRound()
            }));
        }
    }

    _drawEnemies() {
        this._clearGroup(this._enemyObjects);
        const _e = (o) => { this._enemyObjects.push(o); return o; };

        this.enemies.forEach((enemy, idx) => {
            const ex = this._enemyX(enemy);
            const ey = 200;

            // 본체
            _e(this.add.circle(ex, ey, 40, enemy.color, enemy.sealed ? 0.3 : 0.85).setDepth(20));
            _e(this.add.text(ex, ey, enemy.sealed ? '🔒' : enemy.icon, { fontSize: '32px' }).setOrigin(0.5).setDepth(21));
            _e(this.add.text(ex, ey - 56, enemy.name, {
                fontSize: '11px', fontFamily: 'monospace', color: enemy.sealed ? '#666677' : '#ccaabb', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(22));

            // 룬 시퀀스 표시 (횃불 켜져있을 때만)
            const torchOn = this.torchTurns > 0;
            const seqY = ey + 56;
            const runeSize = 38;
            const startX = ex - (enemy.sequence.length - 1) * (runeSize + 6) / 2;

            enemy.sequence.forEach((runeKey, i) => {
                const rx = startX + i * (runeSize + 6);
                const rune = this.RUNES[runeKey];
                const isDone = i < enemy.progress;
                const isNext = i === enemy.progress && !enemy.sealed;

                const bg = _e(this.add.graphics().setDepth(22));
                if (isDone) {
                    bg.fillStyle(rune.color, 0.4);
                } else if (isNext && torchOn) {
                    bg.fillStyle(rune.color, 0.7);
                    // 강조 펄스
                    const pulse = _e(this.add.rectangle(rx, seqY, runeSize + 6, runeSize + 6, rune.color, 0).setStrokeStyle(2, 0xffffff, 0.8).setDepth(22));
                    this.tweens.add({ targets: pulse, scale: 1.1, duration: 500, yoyo: true, repeat: -1 });
                } else if (torchOn) {
                    bg.fillStyle(0x222233, 0.7);
                } else {
                    bg.fillStyle(0x111122, 0.9);
                }
                bg.fillRoundedRect(rx - runeSize / 2, seqY - runeSize / 2, runeSize, runeSize, 4);
                bg.lineStyle(1, isNext ? 0xffffff : 0x444455, isNext ? 0.9 : 0.5);
                bg.strokeRoundedRect(rx - runeSize / 2, seqY - runeSize / 2, runeSize, runeSize, 4);

                if (torchOn) {
                    _e(this.add.text(rx, seqY, isDone ? '✓' : rune.icon, {
                        fontSize: isDone ? '18px' : '20px',
                        color: isDone ? '#88ff88' : '#ffffff'
                    }).setOrigin(0.5).setDepth(23));
                } else {
                    _e(this.add.text(rx, seqY, '?', {
                        fontSize: '18px', fontFamily: 'monospace', color: '#332244'
                    }).setOrigin(0.5).setDepth(23));
                }
            });

            // 진행 표시
            _e(this.add.text(ex, seqY + 35, `${enemy.progress}/${enemy.sequence.length} 봉인`, {
                fontSize: '10px', fontFamily: 'monospace',
                color: enemy.sealed ? '#88ff88' : '#aaaabb'
            }).setOrigin(0.5).setDepth(22));
        });
    }

    _drawParty() {
        this._clearGroup(this._unitObjects);
        const _u = (o) => { this._unitObjects.push(o); return o; };

        this.party.forEach((merc, i) => {
            const x = this._partyX(merc);
            const y = 450;
            const base = merc.getBaseClass();
            const isDead = !merc.alive || merc.currentHp <= 0;
            const isActed = this.actionsThisRound[merc.id];
            const isSelected = this.selectedMerc && this.selectedMerc.id === merc.id;

            // 강조
            if (isSelected) {
                const ring = _u(this.add.circle(x, y, 38, 0xbb88ff, 0).setStrokeStyle(3, 0xbb88ff, 0.9).setDepth(19));
                this.tweens.add({ targets: ring, scale: 1.15, duration: 500, yoyo: true, repeat: -1 });
            }

            // 본체
            _u(this.add.circle(x, y, 26, isDead ? 0x333333 : base.color, isDead ? 0.3 : (isActed ? 0.4 : 0.85)).setDepth(20));
            _u(this.add.text(x, y, isDead ? '💀' : base.icon, { fontSize: '22px' }).setOrigin(0.5).setDepth(21));
            _u(this.add.text(x, y - 38, merc.name, {
                fontSize: '10px', fontFamily: 'monospace',
                color: isDead ? '#444444' : (isActed ? '#666677' : '#aaccee'),
                fontStyle: isSelected ? 'bold' : ''
            }).setOrigin(0.5).setDepth(22));
            _u(this.add.text(x, y - 24, `Lv.${merc.level}`, {
                fontSize: '9px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5).setDepth(22));

            // HP바
            const stats = merc.getStats();
            const hpRatio = merc.currentHp / stats.hp;
            const barW = 70, barH = 5;
            _u(this.add.rectangle(x, y + 30, barW, barH, 0x220011).setDepth(22));
            const hpColor = hpRatio > 0.6 ? 0x44ff44 : hpRatio > 0.3 ? 0xffaa44 : 0xff4444;
            const fillW = Math.max(0, barW * hpRatio);
            if (fillW > 0) {
                _u(this.add.rectangle(x - barW / 2 + fillW / 2, y + 30, fillW, barH, hpColor).setDepth(23));
            }
            _u(this.add.text(x, y + 42, `${merc.currentHp}/${stats.hp}`, {
                fontSize: '9px', fontFamily: 'monospace', color: '#ccccdd'
            }).setOrigin(0.5).setDepth(23));

            // 입력 가능 룬 미리보기
            const runes = this.CLASS_RUNES[merc.classKey] || [];
            let rx = x - (runes.length - 1) * 12;
            runes.forEach(rk => {
                _u(this.add.text(rx, y + 56, this.RUNES[rk].icon, {
                    fontSize: '12px'
                }).setOrigin(0.5).setDepth(22).setAlpha(isActed ? 0.3 : 1));
                rx += 24;
            });

            // 클릭존
            if (!isDead && !isActed) {
                const z = _u(this.add.zone(x, y, 80, 80).setInteractive({ useHandCursor: true }).setDepth(50));
                z.on('pointerdown', () => this._selectMerc(merc));
            }

            // 상태 라벨
            if (isActed && !isDead) {
                _u(this.add.text(x, y + 75, '행동 완료', {
                    fontSize: '9px', fontFamily: 'monospace', color: '#666677'
                }).setOrigin(0.5).setDepth(22));
            }
        });
    }

    _drawInputPanel() {
        this._clearGroup(this._inputPanelObjects);
        const _a = (o) => { this._inputPanelObjects.push(o); return o; };

        const panelY = 555;
        _a(this.add.rectangle(640, panelY + 50, 1180, 90, 0x0a0a14, 0.95).setDepth(99));
        _a(this.add.graphics().setDepth(99)
            .lineStyle(1, 0x332244, 0.7)
            .strokeRoundedRect(50, panelY, 1180, 90, 6));

        if (!this.selectedMerc) {
            _a(this.add.text(640, panelY + 50, '↓ 행동할 용병을 선택하세요', {
                fontSize: '14px', fontFamily: 'monospace', color: '#aaaabb'
            }).setOrigin(0.5).setDepth(100));
            return;
        }

        const merc = this.selectedMerc;
        const base = merc.getBaseClass();
        _a(this.add.text(70, panelY + 12, `${base.icon} ${merc.name} — 입력할 룬 선택`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#ffcc88', fontStyle: 'bold'
        }).setDepth(100));

        // 다음 필요 룬 힌트
        const firstEnemy = this.enemies.find(e => !e.sealed);
        if (firstEnemy) {
            const needRune = firstEnemy.sequence[firstEnemy.progress];
            const need = this.RUNES[needRune];
            const torchOn = this.torchTurns > 0;
            _a(this.add.text(70, panelY + 32, torchOn
                ? `${firstEnemy.name} 다음 필요: ${need.icon} ${need.name}`
                : `${firstEnemy.name} 다음 필요: ??? (횃불 꺼짐)`, {
                fontSize: '11px', fontFamily: 'monospace', color: torchOn ? '#bbaacc' : '#552266'
            }).setDepth(100));
        }

        const runes = this.CLASS_RUNES[merc.classKey] || [];
        const btnW = 110, btnH = 56, gap = 12;
        const totalW = runes.length * btnW + (runes.length - 1) * gap;
        const startX = 640 - totalW / 2 + btnW / 2;

        runes.forEach((rk, i) => {
            const r = this.RUNES[rk];
            const bx = startX + i * (btnW + gap);
            const by = panelY + 60;
            _a(UIButton.create(this, bx, by, btnW, btnH, `${r.icon}\n${r.name}`, {
                color: r.color, hoverColor: 0xffffff, textColor: '#ffffff', fontSize: 14, depth: 100,
                onClick: () => this._inputRune(rk)
            }));
        });

        // 우측: 취소
        _a(UIButton.create(this, 1170, panelY + 60, 100, 30, '취소', {
            color: 0x444455, hoverColor: 0x555566, textColor: '#aaccee', fontSize: 11, depth: 100,
            onClick: () => { this.selectedMerc = null; this._drawHUD(); this._drawParty(); this._drawInputPanel(); }
        }));
    }

    // ==================== UTIL ====================

    _showAnnounce(text, color) {
        const t = this.add.text(640, 130, text, {
            fontSize: '22px', fontFamily: 'monospace',
            color: `#${color.toString(16).padStart(6, '0')}`, fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(300);
        this.tweens.add({ targets: t, alpha: 0, y: 100, duration: 1600, onComplete: () => t.destroy() });
    }

    _showFloatText(x, y, text, color) {
        const t = this.add.text(x, y, text, {
            fontSize: '14px', fontFamily: 'monospace', color, fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(250);
        this.tweens.add({ targets: t, alpha: 0, y: y - 30, duration: 900, onComplete: () => t.destroy() });
    }

    _clearGroup(arr) {
        for (const o of arr) { if (o && o.destroy) o.destroy(); }
        arr.length = 0;
    }
}
