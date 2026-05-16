// ══════════════════════════════════════════════════════════════════
// CargoBattleScene — 나락식 층 진행 + FTL식 사이드뷰 + 라이트 RTS
// 5분 생존 방어전. 외벽 HP가 0이 되면 실패.
// ══════════════════════════════════════════════════════════════════

class CargoBattleScene extends Phaser.Scene {
    constructor() { super('CargoBattleScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.party = data.party;
        this.floor = data.floor || 1;
        this.positiveModifier = data.positiveModifier;
        this.negativeModifier = data.negativeModifier;

        this.zoneKey = 'cargo';
    }

    create() {
        // ─── 상태 초기화 ─────────────────────────────────────────
        this.battleOver = false;
        this.battleTime = 0;
        this.runDuration = 300000; // 5분 = 300,000ms
        this.speedMultiplier = parseInt(localStorage.getItem('demo9_speed') || '1', 10) || 1;

        // XP / 레벨업
        this.xp = 0;
        this.level = 0;
        this.heldCards = [];
        this.totalKills = 0;
        this.totalEnemiesSpawned = 0;

        // 기차 외벽
        const scaling = getCargoFloorScaling(this.floor);
        this.train = {
            wallMaxHp: scaling.wallHp,
            wallHp: scaling.wallHp,
            wallDef: 0,
            // 카드/모디파이어 효과 슬롯
            autoRepairInterval: 0,
            autoRepairPercent: 0,
            autoRepairTimer: 0,
            noAutoRepair: false,
            barricadeStun: 0,
            wallStun: 0,
            turretActive: false,
            turretDmg: 0,
            turretTimer: 0,
            flameDmg: 0,
            thornPercent: 0,
            trapDot: false,
            trapDuration: 0,
            unsinkable: false,
            unsinkableDuration: 0,
            unsinkableCooldown: 0,
            unsinkableTimer: 0,
            unsinkableActive: false,
            lastDefense: false,
            lastDefenseUsed: false,
            doubleWall: false,
            innerWallHp: 0,
            focusFireBonus: 0,
            periodicHeal: false,
            periodicHealInterval: 0,
            periodicHealPercent: 0,
            periodicHealTimer: 0,
            bloodPact: false,
            bloodPactInterval: 0,
            bloodPactPercent: 0,
            bloodPactTimer: 0,
            selfDestruct: false,
            selfDestructTriggered: false,
            enemyCountMult: 1,
            enemyStatMult: 1,
            enemySpeedMult: 1,
            eliteFreqMult: 1,
            lootBonus: 0,
            legendaryMult: 1,
            normalEffectMult: 1,
            allInNextLevel: false,
            startBonus: false,
            fogWarningReduction: 0
        };

        // RTS 선택 상태
        this.selectedUnit = null;
        this.allies = [];
        this.enemies = [];
        this.allUnits = [];

        // 웨이브 타이머
        this.waveTimer = 0;
        this.waveInterval = 8000; // 8초마다 새 웨이브
        this.waveNumber = 0;

        // 총 드랍
        this.totalGold = 0;
        this.loot = [];

        // ─── 빌드 ────────────────────────────────────────────────
        this._drawBackground();
        this._drawTrainWall();
        this._spawnAllies();
        this._applyModifiers();
        this._drawHUD();
        this._setupInput();
        this._setupDeathCallback();
        this._showStartAnnounce();

        // 시작 보너스 (보급 지원 모디파이어)
        if (this.train.startBonus) {
            this.time.delayedCall(500, () => this._triggerLevelUp());
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // DRAWING
    // ═══════════════════════════════════════════════════════════════

    _drawBackground() {
        const gfx = this.add.graphics();

        // 배경 (어두운 산업 톤)
        gfx.fillGradientStyle(0x0a0a15, 0x0a0a15, 0x151520, 0x151520);
        gfx.fillRect(0, 0, 1280, 720);

        // 기차 (12시 방향 = 상단) — 기차 본체
        gfx.fillStyle(0x2a2520, 1);
        gfx.fillRect(100, 0, 1080, 100); // 기차 본체 (상단)

        // 기차 외벽 (하단 가장자리 = 적이 도달하는 면)
        gfx.fillStyle(0x334433, 1);
        gfx.fillRect(100, 95, 1080, 10); // 외벽 라인

        // 전투 영역 (기차 아래쪽 공간)
        gfx.fillStyle(0x0f0f1a, 1);
        gfx.fillRect(0, 105, 1280, 615);

        // 바닥 그리드 패턴
        gfx.lineStyle(1, 0x1a1a2a, 0.3);
        for (let x = 0; x < 1280; x += 80) {
            gfx.lineBetween(x, 105, x, 720);
        }
        for (let y = 105; y < 720; y += 80) {
            gfx.lineBetween(0, y, 1280, y);
        }

        // 적 진입 방향 표시 (9시=좌, 6시=하, 3시=우)
        gfx.lineStyle(2, 0x442222, 0.3);
        // 좌측 진입 화살표
        gfx.lineBetween(0, 400, 40, 400);
        gfx.lineBetween(40, 400, 30, 395);
        gfx.lineBetween(40, 400, 30, 405);
        // 우측 진입 화살표
        gfx.lineBetween(1280, 400, 1240, 400);
        gfx.lineBetween(1240, 400, 1250, 395);
        gfx.lineBetween(1240, 400, 1250, 405);
        // 하단 진입 화살표
        gfx.lineBetween(640, 720, 640, 680);
        gfx.lineBetween(640, 680, 635, 690);
        gfx.lineBetween(640, 680, 645, 690);
    }

    _drawTrainWall() {
        // 외벽 시각화 (상단 — 기차 하단 가장자리)
        this._wallGfx = this.add.graphics().setDepth(10);
        this._updateWallVisual();
    }

    _updateWallVisual() {
        const gfx = this._wallGfx;
        gfx.clear();

        const hpRatio = this.train.wallHp / this.train.wallMaxHp;

        // 외벽 = 상단 가로 바 (기차 하단 면)
        const wallX = 100, wallY = 90, wallW = 1080, wallH = 16;

        // Wall body
        const wallColor = hpRatio > 0.6 ? 0x446644 : hpRatio > 0.3 ? 0x886622 : 0x884422;
        gfx.fillStyle(wallColor, 0.9);
        gfx.fillRect(wallX, wallY, wallW, wallH);
        gfx.lineStyle(1, 0x668866, 0.5);
        gfx.strokeRect(wallX, wallY, wallW, wallH);

        // Damage cracks
        if (hpRatio < 0.7) {
            gfx.lineStyle(1, 0xff4444, 0.3 + (1 - hpRatio) * 0.4);
            const numCracks = Math.floor((1 - hpRatio) * 10);
            for (let i = 0; i < numCracks; i++) {
                const cx = wallX + 50 + i * (wallW / numCracks);
                gfx.lineBetween(cx, wallY + 2, cx + 10, wallY + wallH - 2);
            }
        }

        // HP bar (외벽 위에 가로로 표시)
        const barX = 100, barY = 75, barW = 1080, barH = 10;
        gfx.fillStyle(0x222222, 1);
        gfx.fillRoundedRect(barX, barY, barW, barH, 2);
        const hpColor = hpRatio > 0.6 ? 0x44ff44 : hpRatio > 0.3 ? 0xffaa00 : 0xff4444;
        gfx.fillStyle(hpColor, 1);
        gfx.fillRoundedRect(barX, barY, barW * hpRatio, barH, 2);
    }

    _drawHUD() {
        // 타이머 (남은 시간)
        this.timerText = this.add.text(640, 15, '5:00', {
            fontSize: '24px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(100);

        // 층 정보
        this.add.text(640, 40, `${this.floor}층`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa8866'
        }).setOrigin(0.5).setDepth(100);

        // 외벽 HP 텍스트
        this.wallHpText = this.add.text(15, 70, '', {
            fontSize: '10px', fontFamily: 'monospace', color: '#88cc88'
        }).setDepth(100);

        // XP 바
        this.xpBarBg = this.add.rectangle(640, 55, 300, 8, 0x222222).setDepth(100);
        this.xpBarFill = this.add.rectangle(640 - 150, 55, 0, 8, 0x44aaff).setOrigin(0, 0.5).setDepth(101);
        this.levelText = this.add.text(490, 55, 'Lv.0', {
            fontSize: '10px', fontFamily: 'monospace', color: '#44aaff'
        }).setOrigin(1, 0.5).setDepth(100);

        // 킬 카운터
        this.killText = this.add.text(1250, 15, 'Kills: 0', {
            fontSize: '11px', fontFamily: 'monospace', color: '#ff8888'
        }).setOrigin(1, 0).setDepth(100);

        // 골드
        this.goldText = this.add.text(1250, 32, '💰 0G', {
            fontSize: '11px', fontFamily: 'monospace', color: '#ffcc44'
        }).setOrigin(1, 0).setDepth(100);

        // 카드 보유 수
        this.cardCountText = this.add.text(1250, 49, '🃏 0', {
            fontSize: '10px', fontFamily: 'monospace', color: '#aa8844'
        }).setOrigin(1, 0).setDepth(100);

        // 모디파이어 표시
        if (this.positiveModifier) {
            this.add.text(80, 15, `✨ ${this.positiveModifier.name}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#44ff88'
            }).setDepth(100);
        }
        if (this.negativeModifier) {
            this.add.text(80, 30, `💀 ${this.negativeModifier.name}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#ff6666'
            }).setDepth(100);
        }

        // 속도 버튼
        const speedBtn = this.add.text(1220, 690, `×${this.speedMultiplier}`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
            backgroundColor: '#331111', padding: { x: 6, y: 3 }
        }).setOrigin(0.5).setDepth(100).setInteractive();
        speedBtn.on('pointerdown', () => {
            this.speedMultiplier = this.speedMultiplier === 1 ? 2 : this.speedMultiplier === 2 ? 3 : 1;
            speedBtn.setText(`×${this.speedMultiplier}`);
            localStorage.setItem('demo9_speed', this.speedMultiplier);
        });

        // 선택 지시 텍스트
        this.instructionText = this.add.text(640, 700, '유닛 클릭 → 적 클릭으로 타겟 지정 | 바닥 클릭으로 이동', {
            fontSize: '10px', fontFamily: 'monospace', color: '#666688'
        }).setOrigin(0.5).setDepth(100);
    }

    // ═══════════════════════════════════════════════════════════════
    // SPAWN / INIT
    // ═══════════════════════════════════════════════════════════════

    _spawnAllies() {
        // 기차(상단) 바로 아래, 중앙 부근에 배치
        const positions = [
            { x: 440, y: 200 },
            { x: 580, y: 220 },
            { x: 700, y: 200 },
            { x: 840, y: 220 }
        ];

        this.party.forEach((merc, idx) => {
            if (!merc.alive) return;
            const pos = positions[idx] || { x: 400 + idx * 140, y: 210 };
            const unit = BattleUnit.fromMercenary(this, merc, pos.x, pos.y);
            unit._targetPos = null; // RTS 이동 목표
            unit._rtsTarget = null; // 수동 지정 타겟
            unit._holdPosition = false; // 이동 명령 후 제자�� 유지
            this.allies.push(unit);
            this.allUnits.push(unit);
        });
    }

    _applyModifiers() {
        const ctx = {
            allies: this.allies,
            train: this.train,
            heldCards: this.heldCards
        };

        if (this.positiveModifier && this.positiveModifier.apply) {
            this.positiveModifier.apply(ctx);
        }
        if (this.negativeModifier && this.negativeModifier.apply) {
            this.negativeModifier.apply(ctx);
        }
    }

    _setupInput() {
        // 클릭 이벤트
        this.input.on('pointerdown', (pointer) => {
            if (this.battleOver || this._levelUpActive) return;

            const x = pointer.worldX || pointer.x;
            const y = pointer.worldY || pointer.y;

            // 적 클릭 체크
            const clickedEnemy = this._getUnitAt(x, y, 'enemy');
            if (clickedEnemy && this.selectedUnit) {
                // 타겟 지정
                this.selectedUnit._rtsTarget = clickedEnemy;
                this._showTargetIndicator(clickedEnemy);
                return;
            }

            // 아군 클릭 체크
            const clickedAlly = this._getUnitAt(x, y, 'ally');
            if (clickedAlly) {
                this._selectUnit(clickedAlly);
                return;
            }

            // 빈 공간 클릭 → 선택된 유닛 이동
            if (this.selectedUnit && this.selectedUnit.alive) {
                // 맵 경계 클램핑 (전투 영역: x 40~1240, y 120~680)
                const clampX = Math.max(40, Math.min(1240, x));
                const clampY = Math.max(120, Math.min(680, y));
                this.selectedUnit._targetPos = { x: clampX, y: clampY };
                this.selectedUnit._rtsTarget = null;
                this.selectedUnit._holdPosition = true; // 이동 명령 → 도착 후 제자리 유지
                this._showMoveIndicator(clampX, clampY);
            }
        });
    }

    _getUnitAt(x, y, team) {
        const list = team === 'enemy' ? this.enemies : this.allies;
        const threshold = 30;
        for (const unit of list) {
            if (!unit.alive) continue;
            const dx = unit.container.x - x;
            const dy = unit.container.y - y;
            if (Math.sqrt(dx * dx + dy * dy) < threshold) return unit;
        }
        return null;
    }

    _selectUnit(unit) {
        // 이전 선택 해제
        if (this._selectCircle) this._selectCircle.destroy();

        this.selectedUnit = unit;
        this._selectCircle = this.add.circle(unit.container.x, unit.container.y, 22, 0x44ff88, 0.3)
            .setDepth(4);

        this.instructionText.setText(`${unit.name} 선택됨 — 적 클릭: 타겟 | 바닥 클릭: 이동`);
    }

    _showTargetIndicator(enemy) {
        const indicator = this.add.circle(enemy.container.x, enemy.container.y, 18, 0xff4444, 0.4).setDepth(4);
        this.tweens.add({ targets: indicator, alpha: 0, scale: 1.5, duration: 500, onComplete: () => indicator.destroy() });
    }

    _showMoveIndicator(x, y) {
        const indicator = this.add.circle(x, y, 8, 0x44ff88, 0.6).setDepth(4);
        this.tweens.add({ targets: indicator, alpha: 0, scale: 2, duration: 400, onComplete: () => indicator.destroy() });
    }

    _showStartAnnounce() {
        const txt = this.add.text(640, 300, `${this.floor}층 — 전투 시작!`, {
            fontSize: '32px', fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(200).setAlpha(0);
        this.tweens.add({
            targets: txt, alpha: 1, duration: 300,
            yoyo: true, hold: 800,
            onComplete: () => txt.destroy()
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // ENEMY SPAWNING
    // ═══════════════════════════════════════════════════════════════

    _spawnWave() {
        this.waveNumber++;
        const wave = generateCargoWave(this.battleTime, this.floor, this.train);

        wave.forEach((enemyDef, idx) => {
            const enemyData = ENEMY_DATA[enemyDef.type];
            if (!enemyData) return;

            // 스폰 위치: 3방향 (9시=좌, 6시=하, 3시=우)에서 진입
            const direction = idx % 3; // 0=좌, 1=하, 2=우
            let spawnX, spawnY;
            if (direction === 0) {
                // 9시 방향 (좌측)
                spawnX = -Phaser.Math.Between(20, 100);
                spawnY = Phaser.Math.Between(200, 600);
            } else if (direction === 1) {
                // 6시 방향 (하단)
                spawnX = Phaser.Math.Between(200, 1080);
                spawnY = 720 + Phaser.Math.Between(20, 100);
            } else {
                // 3시 방향 (우측)
                spawnX = 1280 + Phaser.Math.Between(20, 100);
                spawnY = Phaser.Math.Between(200, 600);
            }

            // fromEnemyData는 hp/atk/def에 scaleMult 적용
            const unit = BattleUnit.fromEnemyData(this, enemyDef.type, enemyDef.statMult, spawnX, spawnY);
            // ATK는 별도 배율 적용 (statMult이 이미 적용됨, atkMult/statMult 비율 보정)
            if (enemyDef.atkMult !== enemyDef.statMult) {
                unit.atk = Math.floor(enemyData.atk * enemyDef.atkMult);
            }
            unit.moveSpeed = Math.floor(enemyData.moveSpeed * enemyDef.speedMult);
            unit._wallTarget = true; // 벽을 향해 이동 (상단)

            this.enemies.push(unit);
            this.allUnits.push(unit);
            this.totalEnemiesSpawned++;
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // MAIN UPDATE LOOP
    // ═══════════════════════════════════════════════════════════════

    update(time, delta) {
        if (this.battleOver || this._levelUpActive) return;

        const dt = delta * this.speedMultiplier;
        this.battleTime += dt;

        // ─── 타이머 업데이트 ─────────────────────────────────────
        const remaining = Math.max(0, this.runDuration - this.battleTime);
        const sec = Math.ceil(remaining / 1000);
        const min = Math.floor(sec / 60);
        const s = sec % 60;
        this.timerText.setText(`${min}:${s.toString().padStart(2, '0')}`);
        if (remaining <= 30000) this.timerText.setColor('#ff4444');
        else if (remaining <= 60000) this.timerText.setColor('#ffaa00');

        // 5분 생존 성공
        if (remaining <= 0) {
            this._endRun(true);
            return;
        }

        // ─── 웨이브 생성 ─────────────────────────────────────────
        this.waveTimer += dt;
        if (this.waveTimer >= this.waveInterval) {
            this.waveTimer = 0;
            this._spawnWave();
            // 웨이브 간격 점차 줄어듦
            this.waveInterval = Math.max(3000, this.waveInterval - 200);
        }

        // ─── 유닛 업데이트 ───────────────────────────────────────
        for (const unit of this.allUnits) {
            if (!unit.alive) continue;

            if (unit.team === 'enemy') {
                this._updateEnemy(unit, dt);
            } else {
                this._updateAlly(unit, dt);
            }
        }

        // ─── 기차 관련 효과 ──────────────────────────────────────
        this._updateTrainEffects(dt);

        // ─── 선택 원 위치 업데이트 ───────────────────────────────
        if (this._selectCircle && this.selectedUnit && this.selectedUnit.alive) {
            this._selectCircle.setPosition(this.selectedUnit.container.x, this.selectedUnit.container.y);
        } else if (this._selectCircle && (!this.selectedUnit || !this.selectedUnit.alive)) {
            this._selectCircle.destroy();
            this._selectCircle = null;
            this.selectedUnit = null;
        }

        // ─── HUD 업데이트 ────────────────────────────────────────
        this._updateHUD();
        this._updateWallVisual();
    }

    _updateEnemy(unit, dt) {
        if (!unit.alive) return;

        // 애니메이션
        unit.animTimer += dt;
        if (unit.animTimer > 250) { unit.animTimer = 0; unit.animFrame++; unit.drawCharacter(); }

        // 스턴 처리
        if (unit._stunTimer && unit._stunTimer > 0) {
            unit._stunTimer -= dt;
            return;
        }

        const wallY = 105; // 외벽 위치 (상단 — 기차 하단 면)

        // 가장 가까운 아군 찾기
        let nearestAlly = null;
        let nearestDist = Infinity;
        for (const ally of this.allies) {
            if (!ally.alive) continue;
            const dx = unit.container.x - ally.container.x;
            const dy = unit.container.y - ally.container.y;
            const dist = Math.sqrt(dx * dx + dy * dy);
            if (dist < nearestDist) {
                nearestDist = dist;
                nearestAlly = ally;
            }
        }

        // 사거리 내 아군이 있으면 교전
        if (nearestAlly && nearestDist <= unit.range + 20) {
            unit.target = nearestAlly;
            // 직접 공격 처리
            unit.attackTimer += dt;
            if (unit.attackTimer >= unit.attackSpeed) {
                unit.attackTimer = 0;
                let dmg = Math.max(1, unit.atk - nearestAlly.def);
                const isCrit = Math.random() < unit.critRate;
                if (isCrit) dmg = Math.floor(dmg * unit.critDmg);
                nearestAlly.hp -= dmg;
                DamagePopup.show(this, nearestAlly.container.x, nearestAlly.container.y - 20, dmg, isCrit ? 0xff4444 : 0xffaaaa, isCrit);
                if (nearestAlly.hp <= 0) {
                    nearestAlly.hp = 0;
                    nearestAlly.alive = false;
                    this.tweens.add({ targets: nearestAlly.container, alpha: 0, duration: 300 });
                    if (nearestAlly.healthBar) nearestAlly.healthBar.destroy();
                    if (this.onUnitDeath) this.onUnitDeath(nearestAlly, unit);
                }
            }
        } else if (unit.container.y > wallY + 30) {
            // 벽(상단)을 향해 위로 이동
            const speed = unit.moveSpeed * (dt / 1000);
            // 목표: wallY 방향으로 직진 (약간의 X 수렴 — 벽 중앙 640으로)
            unit.container.y -= speed;
            // X축: 벽 범위(100~1180) 안으로 수렴
            const targetX = 100 + Math.random() * 1080;
            if (unit.container.x < 100) unit.container.x += speed * 0.5;
            else if (unit.container.x > 1180) unit.container.x -= speed * 0.5;
        } else {
            // 벽에 도달 → 벽 공격
            this._enemyAttackWall(unit, dt);
        }

        // 체력바 위치
        if (unit.healthBar) {
            unit.healthBar.setPosition(unit.container.x, unit.container.y - 30);
            unit.healthBar.update(unit.hp);
        }
    }

    _enemyAttackWall(unit, dt) {
        unit.attackTimer = (unit.attackTimer || 0) + dt;
        if (unit.attackTimer >= unit.attackSpeed) {
            unit.attackTimer = 0;

            let dmg = Math.max(1, unit.atk - this.train.wallDef);
            this.train.wallHp -= dmg;

            // 외벽 가시 반사
            if (this.train.thornPercent > 0) {
                const reflect = Math.floor(dmg * this.train.thornPercent);
                unit.hp -= reflect;
                if (unit.hp <= 0) {
                    this._onEnemyKill(unit);
                    return;
                }
            }

            // 외벽 스턴
            if (this.train.wallStun > 0) {
                unit._stunTimer = this.train.wallStun;
            }

            // 데미지 팝업 (벽 = 상단이므로 유닛 위치 근처에 표시)
            DamagePopup.show(this, unit.container.x, 110, dmg, 0xff4444, false);

            // 외벽 파괴 체크
            if (this.train.wallHp <= 0) {
                if (this.train.unsinkable && !this.train.unsinkableActive && this.train.unsinkableTimer <= 0) {
                    // 불침함 발동
                    this.train.wallHp = 1;
                    this.train.unsinkableActive = true;
                    this.train.unsinkableTimer = this.train.unsinkableDuration;
                    DamagePopup.show(this, 640, 300, '🛡 불침함 발동!', 0xffcc44, false);
                } else if (this.train.doubleWall && this.train.innerWallHp > 0) {
                    // 이중 장벽
                    this.train.wallHp = this.train.innerWallHp;
                    this.train.innerWallHp = 0;
                    this.train.doubleWall = false;
                    DamagePopup.show(this, 640, 300, '🛡 내부 벽 활성화!', 0x44aaff, false);
                } else {
                    this.train.wallHp = 0;
                    this._endRun(false);
                }
            }
        }
    }

    _updateAlly(unit, dt) {
        if (!unit.alive) return;

        // 맵 경계 상수
        const BOUNDS = { minX: 40, maxX: 1240, minY: 120, maxY: 680 };

        // 애니메이션
        unit.animTimer += dt;
        if (unit.animTimer > 250) { unit.animTimer = 0; unit.animFrame++; unit.drawCharacter(); }

        // RTS 이동 처리 (플레이어 명령)
        if (unit._targetPos) {
            const dx = unit._targetPos.x - unit.container.x;
            const dy = unit._targetPos.y - unit.container.y;
            const dist = Math.sqrt(dx * dx + dy * dy);
            if (dist > 5) {
                const speed = unit.moveSpeed * (dt / 1000);
                unit.container.x += (dx / dist) * speed;
                unit.container.y += (dy / dist) * speed;
            } else {
                unit._targetPos = null;
                // _holdPosition이면 도착 후 적에게 돌진하지 않음
            }
        }

        // 타겟 결정
        let target = unit._rtsTarget;
        if (target && !target.alive) {
            unit._rtsTarget = null;
            target = null;
            // 수동 타겟 사망 시 holdPosition 해제
            unit._holdPosition = false;
        }

        if (!target && !unit._holdPosition) {
            // 자동 타겟: 가장 가까운 적 (holdPosition이면 자동 타겟팅 안 함)
            let nearestDist = Infinity;
            for (const enemy of this.enemies) {
                if (!enemy.alive) continue;
                const dx = unit.container.x - enemy.container.x;
                const dy = unit.container.y - enemy.container.y;
                const dist = Math.sqrt(dx * dx + dy * dy);
                if (dist < nearestDist) {
                    nearestDist = dist;
                    target = enemy;
                }
            }
        } else if (!target && unit._holdPosition) {
            // holdPosition 상태: 사거리 내 적만 타겟 (이동은 안 함)
            let nearestDist = Infinity;
            for (const enemy of this.enemies) {
                if (!enemy.alive) continue;
                const dx = unit.container.x - enemy.container.x;
                const dy = unit.container.y - enemy.container.y;
                const dist = Math.sqrt(dx * dx + dy * dy);
                if (dist <= unit.range && dist < nearestDist) {
                    nearestDist = dist;
                    target = enemy;
                }
            }
        }

        if (!target) {
            // 경계 클램핑만 하고 리턴
            unit.container.x = Math.max(BOUNDS.minX, Math.min(BOUNDS.maxX, unit.container.x));
            unit.container.y = Math.max(BOUNDS.minY, Math.min(BOUNDS.maxY, unit.container.y));
            if (unit.healthBar) {
                unit.healthBar.setPosition(unit.container.x, unit.container.y - 30);
                unit.healthBar.update(unit.hp);
            }
            return;
        }
        unit.target = target;

        // 사거리 내 도달 시 공격
        const dx = unit.container.x - target.container.x;
        const dy = unit.container.y - target.container.y;
        const dist = Math.sqrt(dx * dx + dy * dy);

        if (dist > unit.range) {
            // 타겟을 향해 이동 — holdPosition이면 이동 안 함, 수동 타겟이면 이동
            if (!unit._holdPosition && !unit._targetPos) {
                const speed = unit.moveSpeed * (dt / 1000);
                const newX = unit.container.x + (-dx / dist) * speed;
                const newY = unit.container.y + (-dy / dist) * speed;
                // 경계 내에서만 이동
                unit.container.x = Math.max(BOUNDS.minX, Math.min(BOUNDS.maxX, newX));
                unit.container.y = Math.max(BOUNDS.minY, Math.min(BOUNDS.maxY, newY));
            } else if (unit._rtsTarget && !unit._targetPos) {
                // 수동 타겟 지정 시에는 그 적에게 이동 (경계 내)
                const speed = unit.moveSpeed * (dt / 1000);
                const newX = unit.container.x + (-dx / dist) * speed;
                const newY = unit.container.y + (-dy / dist) * speed;
                unit.container.x = Math.max(BOUNDS.minX, Math.min(BOUNDS.maxX, newX));
                unit.container.y = Math.max(BOUNDS.minY, Math.min(BOUNDS.maxY, newY));
            }
        } else {
            // 공격
            unit.attackTimer += dt;
            if (unit.attackTimer >= unit.attackSpeed) {
                unit.attackTimer = 0;

                // 데미지 계산
                let dmg = Math.max(1, unit.atk - target.def);

                // 집중 화력 보너스
                if (this.train.focusFireBonus > 0) {
                    const alliesOnSameTarget = this.allies.filter(a => a.alive && a.target === target).length;
                    if (alliesOnSameTarget >= 2) {
                        dmg = Math.floor(dmg * (1 + this.train.focusFireBonus));
                    }
                }

                // 크리티컬
                const isCrit = Math.random() < unit.critRate;
                if (isCrit) dmg = Math.floor(dmg * unit.critDmg);

                target.hp -= dmg;
                DamagePopup.show(this, target.container.x, target.container.y - 20, dmg, isCrit ? 0xffcc00 : 0xffffff, isCrit);

                // 공격 이펙트 (간단한 라인)
                const gfx = this.add.graphics().setDepth(20);
                gfx.lineStyle(1, isCrit ? 0xffcc00 : 0xffffff, 0.6);
                gfx.lineBetween(unit.container.x, unit.container.y, target.container.x, target.container.y);
                this.tweens.add({ targets: gfx, alpha: 0, duration: 150, onComplete: () => gfx.destroy() });

                if (target.hp <= 0) {
                    target.hp = 0;
                    this._onEnemyKill(target);
                }
            }
        }

        // 경계 클램핑
        unit.container.x = Math.max(BOUNDS.minX, Math.min(BOUNDS.maxX, unit.container.x));
        unit.container.y = Math.max(BOUNDS.minY, Math.min(BOUNDS.maxY, unit.container.y));

        // 체력바 위치
        if (unit.healthBar) {
            unit.healthBar.setPosition(unit.container.x, unit.container.y - 30);
            unit.healthBar.update(unit.hp);
        }
    }

    _updateTrainEffects(dt) {
        // 자동 수리
        if (this.train.autoRepairPercent > 0 && !this.train.noAutoRepair) {
            this.train.autoRepairTimer += dt;
            const interval = this.train.autoRepairInterval || 15000;
            if (this.train.autoRepairTimer >= interval) {
                this.train.autoRepairTimer = 0;
                const heal = Math.floor(this.train.wallMaxHp * this.train.autoRepairPercent);
                this.train.wallHp = Math.min(this.train.wallMaxHp, this.train.wallHp + heal);
            }
        }

        // 주기적 파티 힐
        if (this.train.periodicHeal) {
            this.train.periodicHealTimer += dt;
            if (this.train.periodicHealTimer >= this.train.periodicHealInterval) {
                this.train.periodicHealTimer = 0;
                this.allies.forEach(u => {
                    if (!u.alive) return;
                    const heal = Math.floor(u.maxHp * this.train.periodicHealPercent);
                    u.hp = Math.min(u.maxHp, u.hp + heal);
                    DamagePopup.show(this, u.container.x, u.container.y - 25, `+${heal}`, 0x44ff88, true);
                });
            }
        }

        // 피의 계약 (매 30초 파티 HP 감소)
        if (this.train.bloodPact) {
            this.train.bloodPactTimer += dt;
            if (this.train.bloodPactTimer >= this.train.bloodPactInterval) {
                this.train.bloodPactTimer = 0;
                this.allies.forEach(u => {
                    if (!u.alive) return;
                    const dmg = Math.floor(u.maxHp * this.train.bloodPactPercent);
                    u.hp = Math.max(1, u.hp - dmg);
                });
            }
        }

        // 불침함 쿨다운
        if (this.train.unsinkableActive) {
            this.train.unsinkableTimer -= dt;
            if (this.train.unsinkableTimer <= 0) {
                this.train.unsinkableActive = false;
                this.train.unsinkableTimer = this.train.unsinkableCooldown;
            }
        } else if (this.train.unsinkable && this.train.unsinkableTimer > 0) {
            this.train.unsinkableTimer -= dt;
        }

        // 자폭 코어
        if (this.train.selfDestruct && !this.train.selfDestructTriggered) {
            if (this.train.wallHp / this.train.wallMaxHp <= this.train.selfDestructThreshold) {
                this.train.selfDestructTriggered = true;
                // 모든 적에게 25% 피해
                this.enemies.forEach(e => {
                    if (!e.alive) return;
                    const dmg = Math.floor(e.maxHp * this.train.selfDestructDmg);
                    e.hp -= dmg;
                    DamagePopup.show(this, e.container.x, e.container.y - 20, `💥${dmg}`, 0xff8844, false);
                    if (e.hp <= 0) this._onEnemyKill(e);
                });
                this.cameras.main.shake(300, 0.01);
            }
        }

        // 자동 포탑
        if (this.train.turretActive) {
            this.train.turretTimer = (this.train.turretTimer || 0) + dt;
            if (this.train.turretTimer >= 2000) {
                this.train.turretTimer = 0;
                // 벽(상단)에 가장 가까운 적 공격
                let nearest = null, nearDist = Infinity;
                for (const e of this.enemies) {
                    if (!e.alive) continue;
                    const dist = e.container.y; // Y가 작을수록 벽에 가까움
                    if (dist < nearDist) { nearDist = dist; nearest = e; }
                }
                if (nearest) {
                    nearest.hp -= this.train.turretDmg;
                    DamagePopup.show(this, nearest.container.x, nearest.container.y - 20, this.train.turretDmg, 0xffaa00, false);
                    if (nearest.hp <= 0) this._onEnemyKill(nearest);
                }
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // KILL / XP / LEVEL UP
    // ═══════════════════════════════════════════════════════════════

    _onEnemyKill(unit) {
        if (!unit.alive) return;
        unit.hp = 0;
        unit.alive = false;
        this.totalKills++;

        // XP 부여
        const baseXp = unit.isElite ? 15 : unit.isBoss ? 40 : 5;
        this.xp += baseXp;

        // 골드 드랍
        let gold = Phaser.Math.Between(5, 12) + Math.floor(this.floor * 0.5);
        if (unit.isElite) gold += 15;
        if (this.train.lootBonus > 0) gold = Math.floor(gold * (1 + this.train.lootBonus));
        this.totalGold += gold;

        // 아이템 드랍
        const dropChance = unit.isElite ? 0.4 : unit.isBoss ? 1.0 : 0.15;
        if (Math.random() < dropChance) {
            const item = generateItem(this.zoneKey, this.gameState.guildLevel, unit.isElite ? 1 : 0);
            if (item) this.loot.push(item);
        }

        // 사망 연출
        this.tweens.add({ targets: unit.container, alpha: 0, duration: 300 });
        if (unit.healthBar) unit.healthBar.destroy();

        // 골드 팝업
        const gText = this.add.text(unit.container.x, unit.container.y - 10, `+${gold}G`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
            stroke: '#000', strokeThickness: 2
        }).setOrigin(0.5).setDepth(90);
        this.tweens.add({ targets: gText, y: gText.y - 25, alpha: 0, duration: 600, onComplete: () => gText.destroy() });

        // 레벨업 체크
        const xpNeeded = getCargoXpToLevel(this.level + 1);
        if (this.xp >= xpNeeded) {
            this.xp -= xpNeeded;
            this._triggerLevelUp();
        }

        // 전멸 체크 (최종 방어선)
        const aliveAllies = this.allies.filter(a => a.alive).length;
        if (aliveAllies === 0 && this.train.lastDefense && !this.train.lastDefenseUsed) {
            this.train.lastDefenseUsed = true;
            // 전원 부활
            this.allies.forEach(u => {
                u.alive = true;
                u.hp = Math.floor(u.maxHp * 0.3);
                u.container.setAlpha(1);
            });
            DamagePopup.show(this, 640, 300, '🛡 최종 방어선! 전원 부활!', 0xffcc44, false);
        }
    }

    _triggerLevelUp() {
        this.level++;
        this._levelUpActive = true;

        // 카드 선택지 생성
        const options = {
            choiceCount: 3,
            forceAllLegendary: this.train.allInNextLevel,
            legendaryMult: this.train.legendaryMult || 1
        };
        if (this.train.allInNextLevel) this.train.allInNextLevel = false;

        const choices = generateCargoLevelUpCards(this.floor, this.heldCards.map(c => c.id), options);
        this._showLevelUpUI(choices);
    }

    _showLevelUpUI(choices) {
        this._levelUpElements = [];
        const _lu = (obj) => { this._levelUpElements.push(obj); return obj; };

        // 배경 오버레이
        _lu(this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.8).setDepth(300));

        _lu(this.add.text(640, 120, `⬆ LEVEL UP! (Lv.${this.level})`, {
            fontSize: '24px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(301));

        _lu(this.add.text(640, 155, '카드 1장을 선택하세요', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa8866'
        }).setOrigin(0.5).setDepth(301));

        const cardW = 240, cardH = 150, gap = 30;
        const totalW = choices.length * cardW + (choices.length - 1) * gap;
        const startX = 640 - totalW / 2 + cardW / 2;

        choices.forEach((cardId, idx) => {
            const card = CARGO_CARDS[cardId];
            if (!card) return;

            const cx = startX + idx * (cardW + gap);
            const cy = 300;

            const rarityColors = { normal: 0x446688, rare: 0x4488ff, legendary: 0xffaa00 };
            const rarityBorder = rarityColors[card.rarity] || 0x446688;
            const rarityNames = { normal: '일반', rare: '희귀', legendary: '전설' };

            // 카드 배경
            const gfx = _lu(this.add.graphics().setDepth(302));
            gfx.fillStyle(0x1a1a2a, 1);
            gfx.fillRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
            gfx.lineStyle(2, rarityBorder, 0.8);
            gfx.strokeRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);

            // 희귀도
            _lu(this.add.text(cx, cy - cardH / 2 + 18, rarityNames[card.rarity], {
                fontSize: '10px', fontFamily: 'monospace',
                color: card.rarity === 'legendary' ? '#ffaa00' : card.rarity === 'rare' ? '#4488ff' : '#668899'
            }).setOrigin(0.5).setDepth(303));

            // 카테고리 아이콘
            const catIcons = { stat: '📊', effect: '⚡', gamble: '🎲' };
            _lu(this.add.text(cx, cy - 15, catIcons[card.category] || '📊', {
                fontSize: '20px'
            }).setOrigin(0.5).setDepth(303));

            // 이름
            _lu(this.add.text(cx, cy + 10, card.name, {
                fontSize: '13px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(303));

            // 설명
            _lu(this.add.text(cx, cy + 35, card.desc, {
                fontSize: '10px', fontFamily: 'monospace', color: '#aaaacc',
                wordWrap: { width: cardW - 20 }, align: 'center'
            }).setOrigin(0.5).setDepth(303));

            // 히트존
            const hitZone = _lu(this.add.zone(cx, cy, cardW, cardH).setInteractive({ useHandCursor: true }).setDepth(304));
            hitZone.on('pointerover', () => {
                gfx.clear();
                gfx.fillStyle(0x2a2a3a, 1);
                gfx.fillRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
                gfx.lineStyle(3, 0xffcc44, 1);
                gfx.strokeRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
            });
            hitZone.on('pointerout', () => {
                gfx.clear();
                gfx.fillStyle(0x1a1a2a, 1);
                gfx.fillRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
                gfx.lineStyle(2, rarityBorder, 0.8);
                gfx.strokeRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
            });
            hitZone.on('pointerdown', () => {
                this._selectLevelUpCard(cardId);
            });
        });
    }

    _selectLevelUpCard(cardId) {
        const card = CARGO_CARDS[cardId];
        if (!card) return;

        // 카드 효과 적용
        const ctx = {
            allies: this.allies.filter(u => u.alive),
            train: this.train,
            heldCards: this.heldCards
        };
        card.apply(ctx);

        // 보유 카드에 추가
        this.heldCards.push({ id: cardId, ...card });

        // UI 정리
        this._levelUpElements.forEach(obj => { if (obj && obj.destroy) obj.destroy(); });
        this._levelUpElements = [];
        this._levelUpActive = false;

        // 획득 표시
        const pickText = this.add.text(640, 300, `🃏 ${card.name} 획득!`, {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
            stroke: '#000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(200);
        this.tweens.add({
            targets: pickText, alpha: 0, y: 270, duration: 600, delay: 400,
            onComplete: () => pickText.destroy()
        });

        // 도박 결과 표시
        if (ctx._gambleResult) {
            const gText = this.add.text(640, 340, `🎲 ${ctx._gambleResult}`, {
                fontSize: '14px', fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(200);
            this.tweens.add({ targets: gText, alpha: 0, duration: 1500, delay: 800, onComplete: () => gText.destroy() });
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // HUD UPDATE
    // ═══════════════════════════════════════════════════════════════

    _updateHUD() {
        // 외벽 HP
        const wallPercent = Math.floor((this.train.wallHp / this.train.wallMaxHp) * 100);
        this.wallHpText.setText(`외벽 ${this.train.wallHp}/${this.train.wallMaxHp} (${wallPercent}%)`);

        // XP 바
        const xpNeeded = getCargoXpToLevel(this.level + 1);
        const xpRatio = Math.min(1, this.xp / xpNeeded);
        this.xpBarFill.setDisplaySize(300 * xpRatio, 8);
        this.levelText.setText(`Lv.${this.level}`);

        // 킬
        this.killText.setText(`Kills: ${this.totalKills}`);

        // 골드
        this.goldText.setText(`💰 ${this.totalGold}G`);

        // 카드 수
        this.cardCountText.setText(`🃏 ${this.heldCards.length}`);
    }

    // ═══════════════════════════════════════════════════════════════
    // END RUN
    // ═══════════════════════════════════════════════════════════════

    _endRun(success) {
        if (this.battleOver) return;
        this.battleOver = true;

        const wallHpPercent = this.train.wallHp / this.train.wallMaxHp;
        const killPercent = this.totalEnemiesSpawned > 0 ? this.totalKills / this.totalEnemiesSpawned : 0;

        // 성적 판정
        let floorsUnlocked = 0;
        if (success) {
            floorsUnlocked = judgeCargoPerformance(wallHpPercent, killPercent);
        }

        // 층 해금 업데이트
        const gs = this.gameState;
        if (!gs.cargoFloor) gs.cargoFloor = { maxUnlocked: 1, currentFloor: 1 };
        if (success) {
            gs.cargoFloor.maxUnlocked = Math.min(99, Math.max(gs.cargoFloor.maxUnlocked, this.floor + floorsUnlocked));
        }

        // 결과 텍스트
        const label = success ? 'CLEAR!' : 'FAILED';
        const color = success ? '#44ff88' : '#ff4444';

        const txt = this.add.text(640, 250, label, {
            fontSize: '48px', fontFamily: 'monospace', color, fontStyle: 'bold',
            stroke: '#000', strokeThickness: 5
        }).setOrigin(0.5).setDepth(400).setAlpha(0);

        this.tweens.add({ targets: txt, alpha: 1, duration: 500 });

        if (success) {
            this.add.text(640, 310, `+${floorsUnlocked}층 해금! (다음 최대: ${gs.cargoFloor.maxUnlocked}층)`, {
                fontSize: '16px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(400);

            this.add.text(640, 340, `외벽 잔량: ${Math.floor(wallHpPercent * 100)}% | 처치율: ${Math.floor(killPercent * 100)}%`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#888888'
            }).setOrigin(0.5).setDepth(400);
        }

        // 마을 복귀 버튼
        this.time.delayedCall(1500, () => {
            UIButton.create(this, 640, 420, 180, 40, '마을로 복귀', {
                color: 0x334455, hoverColor: 0x445566, textColor: '#aaccff', fontSize: 14,
                onClick: () => {
                    // 보상 적용
                    gs.gold = (gs.gold || 0) + this.totalGold;
                    // XP 분배
                    this.party.forEach(merc => {
                        if (merc.alive) {
                            merc.xp = (merc.xp || 0) + Math.floor(this.totalKills * 2);
                        }
                    });
                    // 아이템 저장
                    if (!gs.storage) gs.storage = [];
                    this.loot.forEach(item => gs.storage.push(item));

                    SaveManager.save(gs);
                    this.scene.start('TownScene', { gameState: gs });
                }
            }).setDepth(400);

            // 성공 시 재도전 버튼
            if (success) {
                UIButton.create(this, 640, 475, 160, 34, '다음 층 도전', {
                    color: 0x443322, hoverColor: 0x554433, textColor: '#ffaa44', fontSize: 12,
                    onClick: () => {
                        gs.gold = (gs.gold || 0) + this.totalGold;
                        this.party.forEach(merc => {
                            if (merc.alive) merc.xp = (merc.xp || 0) + Math.floor(this.totalKills * 2);
                        });
                        this.loot.forEach(item => { if (!gs.storage) gs.storage = []; gs.storage.push(item); });
                        SaveManager.save(gs);
                        this.scene.start('CargoFloorSelectScene', { gameState: gs, party: this.party });
                    }
                }).setDepth(400);
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    // DEATH CALLBACK (called by BattleUnit)
    // ═══════════════════════════════════════════════════════════════

    get onUnitDeath() { return this._onUnitDeath; }
    set onUnitDeath(fn) { this._onUnitDeath = fn; }

    // BattleUnit에서 호출되는 콜백 설정
    _setupDeathCallback() {
        this.onUnitDeath = (unit, killer) => {
            if (unit.team === 'enemy') {
                this._onEnemyKill(unit);
            } else {
                // 아군 사망 체크
                const aliveAllies = this.allies.filter(a => a.alive).length;
                if (aliveAllies === 0) {
                    if (this.train.lastDefense && !this.train.lastDefenseUsed) {
                        this.train.lastDefenseUsed = true;
                        this.allies.forEach(u => {
                            u.alive = true;
                            u.hp = Math.floor(u.maxHp * 0.3);
                            u.container.setAlpha(1);
                        });
                        DamagePopup.show(this, 640, 300, '🛡 최종 방어선!', 0xffcc44, false);
                    }
                    // 전멸해도 외벽이 살아있으면 계속 진행 (적이 벽만 공격)
                }
            }
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // CLEANUP
    // ═══════════════════════════════════════════════════════════════

    shutdown() {
        this.allUnits.forEach(u => { if (u && u.destroy) u.destroy(); });
        if (this._selectCircle) this._selectCircle.destroy();
        if (this._levelUpElements) this._levelUpElements.forEach(obj => { if (obj && obj.destroy) obj.destroy(); });
    }
}
