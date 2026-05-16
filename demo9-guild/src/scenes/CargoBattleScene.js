class CargoBattleScene extends Phaser.Scene {
    constructor() { super('CargoBattleScene'); }

    preload() {
        if (typeof CharacterSprites !== 'undefined') {
            CharacterSprites.preload(this);
        }
    }

    init(data) {
        this.gameState = data.gameState;
        this.party = data.party;
        this.loot = data.loot || [];
        this.totalGold = data.totalGold || 0;
        this.totalXp = data.totalXp || 0;
        this.currentRound = data.currentRound || 1;
        this.casualties = data.casualties || [];
        this.partyHpState = data.partyHpState || null;

        this.zoneKey = 'cargo';
        this.zoneLevel = this.gameState.zoneLevel[this.zoneKey] || 1;
        this.maxRounds = getMaxRounds(this.zoneLevel);

        this.cardHand = data.cardHand || [];
        this.lootBonusNextRound = data.lootBonusNextRound || 0;

        // Restore train car state across rounds
        if (data.trainCars) {
            this.trainCars = data.trainCars;
        } else {
            this.trainCars = null;
        }

        this.visitedStations = data.visitedStations || [];
        this.currentStation = data.currentStation || null;
    }

    create() {
        if (typeof CharacterSprites !== 'undefined') {
            CharacterSprites.process(this);
        }

        this.allies = [];
        this.enemies = [];
        this.allUnits = [];
        this.battleOver = false;
        this.repairUIActive = false;
        this.speedMultiplier = parseInt(localStorage.getItem('demo9_speed') || '1', 10) || 1;
        this.killStreak = 0;
        this.killStreakTimer = 0;
        this.battleTime = 0;
        this._dangerVignette = null;

        this._initTrainCars();
        this._pickStation();
        this._drawBackground();
        this._drawTrainDisplay();
        this._drawHUD();
        this._spawnAllies();
        if (this.currentRound === 1) {
            this._applyBlessings();
            this._applySynergies();
        }
        this._spawnEnemies();
        this._applyCarFunctions();
        this._applyHeldCardBuffs();
        this._applyStationEffect();
        this._showRoundAnnounce();

        // Consume loot bonus after applying to this round
        if (this.lootBonusNextRound > 0) {
            this.lootBonusNextRound = 0;
        }
    }

    // ─── Train Cars ───────────────────────────────────────────────

    _initTrainCars() {
        if (this.trainCars) return; // already restored from previous round

        const hpScale = 1 + (this.zoneLevel - 1) * 0.2;
        this.trainCars = {
            generator: {
                key: 'generator', name: '발전칸', icon: '⚡',
                hp: Math.floor(220 * hpScale), maxHp: Math.floor(220 * hpScale),
                color: 0xffcc22, func: '전체 기능 활성화', alive: true
            },
            ammo: {
                key: 'ammo', name: '탄약칸', icon: '🔫',
                hp: Math.floor(180 * hpScale), maxHp: Math.floor(180 * hpScale),
                color: 0xcc8844, func: '아군 ATK +15%', alive: true
            },
            medical: {
                key: 'medical', name: '의무칸', icon: '🏥',
                hp: Math.floor(150 * hpScale), maxHp: Math.floor(150 * hpScale),
                color: 0x44aa66, func: '라운드 후 HP 15% 회복', alive: true
            },
            cargo: {
                key: 'cargo', name: '화물칸', icon: '📦',
                hp: Math.floor(200 * hpScale), maxHp: Math.floor(200 * hpScale),
                color: 0x886644, func: '골드 +25%', alive: true
            }
        };
    }

    _isCarActive(carKey) {
        const car = this.trainCars[carKey];
        if (!car || !car.alive) return false;
        // generator being alive enables all; without generator only cargo works on its own
        if (carKey === 'generator' || carKey === 'cargo') return true;
        return this.trainCars.generator.alive;
    }

    // ─── Station System ────────────────────────────────────────────

    _pickStation() {
        const stations = [
            { key: 'deck', name: '갑판', icon: '🚢', desc: '바람이 강하다 — 원거리 ATK +15%', color: '#44aaff' },
            { key: 'hold', name: '화물창', icon: '📦', desc: '물자가 쏟아진다 — 골드 +30%', color: '#ffcc44' },
            { key: 'engine', name: '기관실', icon: '⚙', desc: '열기가 오른다 — 전체 SPD +20%', color: '#ff8844' },
            { key: 'infirmary', name: '의무실', icon: '🏥', desc: '치유의 기운 — 라운드 시작 HP 10% 회복', color: '#44ff88' },
            { key: 'bridge', name: '함교', icon: '🧭', desc: '전술 지휘 — 전체 DEF +5, CRIT +3%', color: '#cc88ff' }
        ];
        if (!this.currentStation) {
            const available = stations.filter(s => !this.visitedStations.includes(s.key));
            const pool = available.length > 0 ? available : stations;
            this.currentStation = pool[Math.floor(Math.random() * pool.length)];
            this.visitedStations.push(this.currentStation.key);
        }
    }

    _applyStationEffect() {
        if (!this.currentStation) return;
        const st = this.currentStation;

        const lvlEffects = typeof getZoneLevelEffects === 'function' ? getZoneLevelEffects('cargo', this.zoneLevel) : [];
        const trainSpeedEff = lvlEffects.find(e => e.effect === 'train_speed');
        const stationMult = trainSpeedEff ? trainSpeedEff.stationBonus : 1.0;

        switch (st.key) {
            case 'deck':
                this.allies.forEach(u => { if (u.range > 100) u.atk = Math.floor(u.atk * (1 + 0.15 * stationMult)); });
                break;
            case 'hold':
                this._stationGoldBonus = 0.3 * stationMult;
                break;
            case 'engine':
                this.allies.forEach(u => { u.moveSpeed = Math.floor(u.moveSpeed * (1 + 0.2 * stationMult)); });
                break;
            case 'infirmary':
                this.allies.forEach(u => { u.hp = Math.min(u.maxHp, u.hp + Math.floor(u.maxHp * 0.1 * stationMult)); });
                break;
            case 'bridge':
                this.allies.forEach(u => {
                    u.def += Math.floor(5 * stationMult);
                    u.critRate = Math.min(0.6, u.critRate + 0.03 * stationMult);
                });
                break;
        }

        this._zoneLevelEffects = lvlEffects;
        for (const eff of lvlEffects) {
            if (eff.effect === 'storm_zone') {
                this._stormInterval = eff.interval * 1000;
                this._stormDmgPercent = eff.dmgPercent;
                this._stormTimer = this._stormInterval;
            }
        }
    }

    // ─── Background ──────────────────────────────────────────────

    _drawBackground() {
        const gfx = this.add.graphics();

        // Sky / terrain gradient
        gfx.fillGradientStyle(0x1a1510, 0x1a1510, 0x2a2015, 0x2a2015);
        gfx.fillRect(0, 0, 1280, 720);

        // Ground
        gfx.fillStyle(0x221a0a);
        gfx.fillRect(0, 460, 1280, 260);

        // Ground line
        gfx.fillStyle(0x443311);
        gfx.fillRect(0, 458, 1280, 4);

        // Railroad track at y=510
        gfx.lineStyle(3, 0x554422, 0.3);
        gfx.lineBetween(0, 530, 1280, 530);
        gfx.lineBetween(0, 545, 1280, 545);

        // Track ties
        for (let i = 0; i < 30; i++) {
            gfx.fillStyle(0x443311, 0.25);
            gfx.fillRect(20 + i * 44, 527, 8, 22);
        }

        // Background crates
        for (let i = 0; i < 5; i++) {
            gfx.fillStyle(0x332200, 0.35);
            const bx = 80 + i * 240 + Phaser.Math.Between(-30, 30);
            gfx.fillRect(bx, 470, 50, 35);
            gfx.lineStyle(1, 0x554422, 0.2);
            gfx.strokeRect(bx, 470, 50, 35);
        }

        // Subtle zone tint
        const zone = ZONE_DATA[this.zoneKey];
        gfx.fillStyle(zone.color, 0.02);
        gfx.fillRect(0, 0, 1280, 720);
    }

    // ─── HUD ─────────────────────────────────────────────────────

    _drawHUD() {
        this._hudElements = [];
        const _h = (obj) => { this._hudElements.push(obj); return obj; };

        const isBoss = this.currentRound >= this.maxRounds;

        // Round label
        const dangerLabel = isBoss ? '💀 보스 출현!' : `라운드 ${this.currentRound} / ${this.maxRounds}`;
        _h(this.add.text(640, 18, `🚂 Cargo — ${dangerLabel}`, {
            fontSize: '20px', fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(100));

        // Progress meter
        const meterX = 440, meterY = 38, meterW = 400, meterH = 6;
        _h(this.add.rectangle(meterX + meterW / 2, meterY, meterW, meterH, 0x332211).setDepth(100));
        const fillW = (this.currentRound / this.maxRounds) * meterW;
        const progress = this.currentRound / this.maxRounds;
        const fillColor = progress <= 0.4 ? 0x44aa44 : progress <= 0.7 ? 0xffaa00 : 0xff2222;
        if (fillW > 0) {
            _h(this.add.rectangle(meterX + fillW / 2, meterY, fillW, meterH, fillColor).setDepth(101));
        }

        // Timer
        this.timerText = _h(this.add.text(640, 48, '0.0초', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa6666'
        }).setOrigin(0.5).setDepth(100));

        // Zone info
        _h(this.add.text(60, 20, '🚂 Cargo', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold'
        }).setDepth(100));
        _h(this.add.text(60, 40, `구역 Lv.${this.zoneLevel}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#886666'
        }).setDepth(100));

        if (this.currentStation) {
            _h(this.add.text(60, 54, `${this.currentStation.icon} ${this.currentStation.name} 정차 — ${this.currentStation.desc}`, {
                fontSize: '9px', fontFamily: 'monospace', color: this.currentStation.color
            }).setDepth(100));
        }

        // Gold
        this.goldText = _h(this.add.text(1240, 48, `💰 ${this.totalGold}G`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44'
        }).setOrigin(1, 0).setDepth(100));

        // Enemy count
        this.enemyCountText = _h(this.add.text(1240, 20, '', {
            fontSize: '12px', fontFamily: 'monospace', color: '#ff8888'
        }).setOrigin(1, 0).setDepth(100));

        // Speed button
        const speedBtn = _h(this.add.text(1220, 690, `×${this.speedMultiplier}`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
            backgroundColor: '#331111', padding: { x: 6, y: 3 }
        }).setOrigin(0.5).setDepth(100).setInteractive());
        speedBtn.on('pointerdown', () => {
            this.speedMultiplier = this.speedMultiplier === 1 ? 2 : this.speedMultiplier === 2 ? 3 : 1;
            speedBtn.setText(`×${this.speedMultiplier}`);
            localStorage.setItem('demo9_speed', this.speedMultiplier);
        });

        // Card hand display
        this._cardHandElements = [];
        this._drawCardHand();
    }

    _drawCardHand() {
        this._cardHandElements.forEach(el => { if (el && el.destroy) el.destroy(); });
        this._cardHandElements = [];

        if (this.cardHand.length === 0) return;

        const startX = 15;
        const y = 640;
        const _ch = (obj) => { this._cardHandElements.push(obj); return obj; };

        _ch(this.add.text(startX, y - 16, `🃏 카드 ${this.cardHand.length}/${CARGO_MAX_HAND}`, {
            fontSize: '9px', fontFamily: 'monospace', color: '#aa8844'
        }).setDepth(100));

        this.cardHand.forEach((cardId, idx) => {
            const card = getCargoCard(cardId);
            if (!card) return;
            const carInfo = CARGO_CAR_CARDS[card.carKey];
            const cx = startX + idx * 75;
            const gfx = _ch(this.add.graphics().setDepth(100));
            gfx.fillStyle(0x1a1a2a, 0.9);
            gfx.fillRoundedRect(cx, y, 70, 28, 3);
            gfx.lineStyle(1, card.tier >= 3 ? 0xffaa00 : card.tier >= 2 ? 0x4488ff : 0x446688, 0.6);
            gfx.strokeRoundedRect(cx, y, 70, 28, 3);

            _ch(this.add.text(cx + 4, y + 3, `${carInfo.icon}`, {
                fontSize: '8px', fontFamily: 'monospace', color: '#cccccc'
            }).setDepth(101));
            _ch(this.add.text(cx + 16, y + 3, card.name, {
                fontSize: '8px', fontFamily: 'monospace', color: card.tier >= 3 ? '#ffaa44' : '#ccccee'
            }).setDepth(101));
            _ch(this.add.text(cx + 4, y + 15, card.desc, {
                fontSize: '7px', fontFamily: 'monospace', color: '#888899'
            }).setDepth(101));
        });
    }

    // ─── Train Visual Display ────────────────────────────────────

    _drawTrainDisplay() {
        this._trainGfx = this.add.graphics().setDepth(8);
        this._trainLabels = [];
        this._trainBars = [];
        this._trainIcons = [];
        this._updateTrainDisplay();
    }

    _updateTrainDisplay() {
        if (this._trainGfx) this._trainGfx.clear();
        this._trainLabels.forEach(l => l.destroy());
        this._trainLabels = [];
        this._trainBars.forEach(b => { if (b.destroy) b.destroy(); });
        this._trainBars = [];
        this._trainIcons.forEach(i => i.destroy());
        this._trainIcons = [];

        const carKeys = ['generator', 'ammo', 'medical', 'cargo'];
        const startX = 180;
        const carW = 220;
        const carH = 55;
        const gap = 12;
        const carY = 570;

        const gfx = this._trainGfx;

        carKeys.forEach((key, idx) => {
            const car = this.trainCars[key];
            const cx = startX + idx * (carW + gap);
            const cy = carY;

            if (!car.alive) {
                // Destroyed car — grayed out
                gfx.fillStyle(0x333333, 0.6);
                gfx.fillRoundedRect(cx, cy, carW, carH, 4);
                gfx.lineStyle(1, 0x444444, 0.4);
                gfx.strokeRoundedRect(cx, cy, carW, carH, 4);

                const destroyLabel = this.add.text(cx + carW / 2, cy + carH / 2 - 6, `💥 ${car.name}`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#666666', fontStyle: 'bold'
                }).setOrigin(0.5).setDepth(9);
                this._trainLabels.push(destroyLabel);

                const destroyedText = this.add.text(cx + carW / 2, cy + carH / 2 + 10, '파괴됨', {
                    fontSize: '10px', fontFamily: 'monospace', color: '#884444'
                }).setOrigin(0.5).setDepth(9);
                this._trainLabels.push(destroyedText);
            } else {
                // Alive car
                const hpRatio = car.hp / car.maxHp;
                const isDamaged = hpRatio < 0.5;

                gfx.fillStyle(car.color, isDamaged ? 0.5 : 0.7);
                gfx.fillRoundedRect(cx, cy, carW, carH, 4);
                gfx.lineStyle(isDamaged ? 2 : 1, isDamaged ? 0xff4444 : 0xffffff, isDamaged ? 0.6 : 0.3);
                gfx.strokeRoundedRect(cx, cy, carW, carH, 4);

                // Icon + name
                const icon = this.add.text(cx + 8, cy + 6, `${car.icon} ${car.name}`, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
                }).setDepth(9);
                this._trainLabels.push(icon);

                // Function label
                const active = this._isCarActive(key);
                const funcColor = active ? '#ccccaa' : '#666666';
                const funcLabel = this.add.text(cx + 8, cy + 22, car.func, {
                    fontSize: '9px', fontFamily: 'monospace', color: funcColor
                }).setDepth(9);
                this._trainLabels.push(funcLabel);

                if (!active && key !== 'generator' && key !== 'cargo') {
                    const offLabel = this.add.text(cx + carW - 8, cy + 6, '⚠️발전칸 필요', {
                        fontSize: '8px', fontFamily: 'monospace', color: '#ff6644'
                    }).setOrigin(1, 0).setDepth(9);
                    this._trainLabels.push(offLabel);
                }

                // HP bar
                const barX = cx + 8;
                const barY = cy + 38;
                const barW = carW - 16;
                const barH = 8;

                gfx.fillStyle(0x222222, 1);
                gfx.fillRoundedRect(barX, barY, barW, barH, 2);
                const hpColor = hpRatio > 0.6 ? 0x44ff44 : hpRatio > 0.3 ? 0xffaa00 : 0xff4444;
                gfx.fillStyle(hpColor, 1);
                gfx.fillRoundedRect(barX, barY, barW * Math.max(0, hpRatio), barH, 2);

                // HP text
                const hpText = this.add.text(cx + carW - 8, cy + 37, `${car.hp}/${car.maxHp}`, {
                    fontSize: '8px', fontFamily: 'monospace', color: '#aaaaaa'
                }).setOrigin(1, 0).setDepth(9);
                this._trainLabels.push(hpText);

                // Damage cracks visual
                if (isDamaged) {
                    gfx.lineStyle(1, 0xff4444, 0.3);
                    const crackX = cx + Phaser.Math.Between(10, carW - 10);
                    gfx.lineBetween(crackX, cy + 5, crackX + 8, cy + 20);
                    gfx.lineBetween(crackX + 8, cy + 20, crackX + 3, cy + 35);
                }
            }

            // Coupling between cars (except last)
            if (idx < carKeys.length - 1) {
                const coupleX = cx + carW;
                gfx.fillStyle(0x888888, 0.5);
                gfx.fillRect(coupleX, cy + carH / 2 - 3, gap, 6);
                gfx.fillStyle(0xaaaaaa, 0.4);
                gfx.fillCircle(coupleX + gap / 2, cy + carH / 2, 4);
            }
        });
    }

    // ─── Spawning ────────────────────────────────────────────────

    _spawnAllies() {
        const startX = 180;
        const spacing = 70;
        this.party.forEach((merc, idx) => {
            if (!merc.alive) return;
            merc._trainingRef = this.gameState.training;
            if (this.partyHpState && this.partyHpState[merc.id] !== undefined) {
                merc.currentHp = this.partyHpState[merc.id];
            }
            const x = startX + idx * spacing;
            const y = 430 + (idx % 2 === 0 ? 0 : 15);
            const unit = BattleUnit.fromMercenary(this, merc, x, y);
            this.allies.push(unit);
            this.allUnits.push(unit);
        });
    }

    _spawnEnemies() {
        const { composition, scaleMult } = getEnemyComposition(this.currentRound, this.zoneLevel, 'cargo');

        const startX = 1100;
        const spacing = 55;
        let idx = 0;
        let infiltratorCount = 0;
        const maxInfiltrators = Phaser.Math.Between(1, 2);

        composition.forEach(group => {
            for (let i = 0; i < group.count; i++) {
                const x = startX - (idx % 6) * spacing;
                const y = 420 + Math.floor(idx / 6) * 40 + (idx % 2 === 0 ? 0 : 15);
                const unit = BattleUnit.fromEnemyData(this, group.type, scaleMult, x, y);

                // Mark some enemies as infiltrators
                if (infiltratorCount < maxInfiltrators && !unit.isBoss && unit.moveSpeed > 0) {
                    unit.isInfiltrator = true;
                    unit.moveSpeed = Math.floor(unit.moveSpeed * 1.3);
                    unit._infiltratorGlow = this.add.circle(unit.container.x, unit.container.y, 16, 0xff8844, 0.2).setDepth(4);
                    unit._infiltratorIcon = this.add.text(unit.container.x, unit.container.y - 28, '⚠', {
                        fontSize: '10px'
                    }).setOrigin(0.5).setDepth(6);
                    infiltratorCount++;
                }

                this.enemies.push(unit);
                this.allUnits.push(unit);
                idx++;
            }
        });

        // Boss on final round
        if (this.currentRound >= this.maxRounds) {
            const bossType = getZoneBoss('cargo');
            const bossScale = scaleMult * getBossScaleMult(this.zoneLevel);
            const bossUnit = BattleUnit.fromEnemyData(this, bossType, bossScale, 1050, 420);
            this.enemies.push(bossUnit);
            this.allUnits.push(bossUnit);
        }

        this._updateEnemyCount();
    }

    // ─── Car Functions ───────────────────────────────────────────

    _applyCarFunctions() {
        // Ammo car: ATK +15% to allies
        if (this._isCarActive('ammo')) {
            this.allies.forEach(a => {
                a.atk = Math.floor(a.atk * 1.15);
            });
        }
    }

    _applyHeldCardBuffs() {
        // Apply combat/utility buff cards that were used during repair phase
        // (buff cards used in repair persist as stat modifiers on the living allies)
        // Note: cards in hand are passive — only "used" cards affect stats
        // This is handled by _useCard writing back to merc stats
    }

    _applyBlessings() {
        const gs = this.gameState;
        if (!gs.blessings || gs.blessings.length === 0) return;
        this.allies.forEach(a => {
            if (gs.blessings.includes('bless_atk'))   a.atk = Math.floor(a.atk * 1.1);
            if (gs.blessings.includes('bless_def'))   a.def = Math.floor(a.def * 1.1);
            if (gs.blessings.includes('bless_hp'))  { a.maxHp = Math.floor(a.maxHp * 1.1); a.hp = Math.min(a.hp + Math.floor(a.maxHp * 0.1), a.maxHp); }
            if (gs.blessings.includes('bless_crit'))  { a.critRate = Math.min(0.8, a.critRate + 0.1); a.critDmg += 0.2; }
            if (gs.blessings.includes('bless_speed')) { a.moveSpeed = Math.floor(a.moveSpeed * 1.1); a.attackSpeed = Math.floor(a.attackSpeed * 0.9); }
        });
        gs.blessings = [];
        SaveManager.save(gs);
    }

    _applySynergies() {
        const classKeys = this.party.filter(m => m.alive).map(m => m.classKey);
        const allyUnits = this.allies.filter(u => u.alive);
        allyUnits.forEach(u => {
            const merc = this.party.find(m => m.id === u.mercId);
            if (merc) u.classKey = merc.classKey;
        });
        this._activeSynergies = applySynergies(allyUnits, classKeys);
        if (this._activeSynergies.length > 0) {
            const names = this._activeSynergies.map(s => s.name).join(', ');
            const txt = this.add.text(640, 85, `⚡ 시너지: ${names}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#88ccff', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(100).setAlpha(0);
            this.tweens.add({ targets: txt, alpha: 1, duration: 500, hold: 2000, yoyo: true });
        }
    }

    // ─── Round Announce & Death Callback ─────────────────────────

    _showRoundAnnounce() {
        const isBoss = this.currentRound >= this.maxRounds;
        const label = isBoss ? '💀 보스 출현!' : `라운드 ${this.currentRound}`;
        const txt = this.add.text(640, 300, label, {
            fontSize: '38px', fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(200).setAlpha(0);
        this.tweens.add({
            targets: txt, alpha: 1, duration: 300,
            yoyo: true, hold: 600,
            onComplete: () => txt.destroy()
        });

        // Setup kill/loot callback
        this.onUnitDeath = (unit, killer) => {
            if (unit.team === 'enemy') {
                this._updateEnemyCount();

                // Clean up infiltrator visuals
                if (unit.isInfiltrator) {
                    if (unit._infiltratorGlow) unit._infiltratorGlow.destroy();
                    if (unit._infiltratorIcon) unit._infiltratorIcon.destroy();
                }

                let goldDrop = Phaser.Math.Between(8, 18) + this.currentRound * 4;
                if (unit.isElite) goldDrop += 20;
                if (unit.isBoss) goldDrop += 100;

                // Cargo car alive: +25% gold
                if (this.trainCars.cargo.alive) {
                    goldDrop = Math.floor(goldDrop * 1.25);
                }
                if (this._stationGoldBonus) {
                    goldDrop = Math.floor(goldDrop * (1 + this._stationGoldBonus));
                }

                this.totalGold += goldDrop;
                this.goldText.setText(`💰 ${this.totalGold}G`);

                const gText = this.add.text(unit.container.x, unit.container.y - 10, `+${goldDrop}G`, {
                    fontSize: '12px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
                    stroke: '#000', strokeThickness: 2
                }).setOrigin(0.5).setDepth(90);
                this.tweens.add({ targets: gText, y: gText.y - 30, alpha: 0, duration: 800, onComplete: () => gText.destroy() });

                // Loot drop (includes card bonus)
                const dropChance = unit.isBoss ? 1.0 : Math.min(0.9, 0.3 + this.currentRound * 0.04 + (this.lootBonusNextRound || 0));
                const bossRarityBonus = unit.isBoss ? 2 : (unit.isElite ? 1 : 0);
                if (Math.random() < dropChance) {
                    const item = generateItem(this.zoneKey, this.gameState.guildLevel, bossRarityBonus);
                    if (item) {
                        this.loot.push(item);
                        this._showLootPopup(unit.container.x, unit.container.y - 40, item);
                    }
                }
            }

            this._checkRoundEnd();
        };
    }

    _showLootPopup(x, y, item) {
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const bg = this.add.rectangle(x, y, 120, 24, 0x111111, 0.9).setStrokeStyle(1, rarity.color).setDepth(90);
        const txt = this.add.text(x, y, `📦 ${item.name}`, {
            fontSize: '10px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(91);
        this.tweens.add({
            targets: [bg, txt], y: y - 40, alpha: 0, duration: 1500, delay: 800,
            onComplete: () => { bg.destroy(); txt.destroy(); }
        });
    }

    _updateEnemyCount() {
        const alive = this.enemies.filter(e => e.alive).length;
        this.enemyCountText.setText(`적: ${alive}`);
    }

    // ─── Infiltrator Logic ───────────────────────────────────────

    _updateInfiltrators(dt) {
        this.enemies.forEach(unit => {
            if (!unit.alive || !unit.isInfiltrator) return;

            // Update glow/icon position
            if (unit._infiltratorGlow) {
                unit._infiltratorGlow.setPosition(unit.container.x, unit.container.y);
            }
            if (unit._infiltratorIcon) {
                unit._infiltratorIcon.setPosition(unit.container.x, unit.container.y - 28);
            }

            // Infiltrators override target: try to move left
            if (unit.target && Math.abs(unit.container.x - unit.target.container.x) > unit.range * 1.5) {
                // If no enemy is close, move towards the train
                unit.container.x -= unit.moveSpeed * (dt / 1000) * 0.8;
            }

            // Reached the train zone
            if (unit.container.x < 200) {
                const dmg = Math.floor(10 + this.currentRound * 5 + this.zoneLevel * 3);
                this._damageRandomCar(dmg);

                // Infiltrator self-destructs
                DamagePopup.show(this, unit.container.x, unit.container.y - 20, '💥 침투!', 0xff8844, false);
                this.cameras.main.shake(100, 0.003);

                // Kill infiltrator
                unit.hp = 0;
                unit.alive = false;
                if (unit._infiltratorGlow) unit._infiltratorGlow.destroy();
                if (unit._infiltratorIcon) unit._infiltratorIcon.destroy();

                // Death particles
                for (let i = 0; i < 6; i++) {
                    const p = this.add.circle(
                        unit.container.x + Phaser.Math.Between(-15, 15),
                        unit.container.y + Phaser.Math.Between(-15, 10),
                        3, 0xff8844, 0.8
                    ).setDepth(50);
                    this.tweens.add({
                        targets: p, alpha: 0, scaleX: 2, scaleY: 2,
                        duration: 400, delay: i * 40,
                        onComplete: () => p.destroy()
                    });
                }
                this.tweens.add({ targets: unit.container, alpha: 0, duration: 300 });
                unit.healthBar.destroy();

                this._updateEnemyCount();
                this._checkRoundEnd();
            }
        });
    }

    _damageRandomCar(dmg) {
        const aliveCars = Object.values(this.trainCars).filter(c => c.alive);
        if (aliveCars.length === 0) return;

        const targetCar = aliveCars[Math.floor(Math.random() * aliveCars.length)];
        targetCar.hp = Math.max(0, targetCar.hp - dmg);

        // Damage popup on the train area
        const carKeys = ['generator', 'ammo', 'medical', 'cargo'];
        const carIdx = carKeys.indexOf(targetCar.key);
        const popupX = 180 + carIdx * 232 + 110;
        const popupY = 565;

        DamagePopup.show(this, popupX, popupY, dmg, 0xff4444, false);

        if (targetCar.hp <= 0) {
            targetCar.alive = false;
            DamagePopup.show(this, popupX, popupY - 20, `${targetCar.icon} ${targetCar.name} 파괴!`, 0xff2222, false);
            this.cameras.main.shake(200, 0.005);

            // Explosion particles
            for (let i = 0; i < 8; i++) {
                const p = this.add.circle(
                    popupX + Phaser.Math.Between(-30, 30),
                    popupY + Phaser.Math.Between(-10, 20),
                    4, targetCar.color, 0.8
                ).setDepth(50);
                this.tweens.add({
                    targets: p, alpha: 0, scaleX: 3, scaleY: 3,
                    duration: 500, delay: i * 50,
                    onComplete: () => p.destroy()
                });
            }

            // Check all cars destroyed
            const allDestroyed = Object.values(this.trainCars).every(c => !c.alive);
            if (allDestroyed) {
                DamagePopup.show(this, 640, 350, '🚂 전체 열차 파괴!', 0xff0000, false);
                this.time.delayedCall(800, () => this._endRun(false));
                this.battleOver = true;
                return;
            }
        }

        this._updateTrainDisplay();
    }

    // ─── Round End / Check ───────────────────────────────────────

    _checkRoundEnd() {
        if (this.battleOver) return;

        const allyAlive = this.allies.filter(u => u.alive);
        const enemyAlive = this.enemies.filter(u => u.alive);

        // All allies dead
        if (allyAlive.length === 0) {
            this.battleOver = true;
            this.cameras.main.shake(200, 0.005);
            this.time.delayedCall(800, () => this._endRun(false));
            return;
        }

        // All enemies dead
        if (enemyAlive.length === 0) {
            this.battleOver = true;
            this.cameras.main.shake(100, 0.002);

            this._snapshotAllyState();

            const isBoss = this.currentRound >= this.maxRounds;
            const roundXp = 10 + this.currentRound * 5 + (isBoss ? 30 : 0);
            this.totalXp += roundXp;

            const roundBonus = 15 + this.currentRound * 10 + (isBoss ? 50 : 0);
            this.totalGold += roundBonus;
            this.goldText.setText(`💰 ${this.totalGold}G`);

            // Medical car: heal 15% between rounds
            if (this._isCarActive('medical')) {
                this.allies.forEach(u => {
                    if (u.alive) {
                        const healAmt = Math.floor(u.maxHp * 0.15);
                        u.hp = Math.min(u.maxHp, u.hp + healAmt);
                        DamagePopup.show(this, u.container.x, u.container.y - 30, healAmt, 0x44ff88, true);
                    }
                });
                // Update snapshot after healing
                this._snapshotAllyState();
            }

            const clearTxt = this.add.text(640, 300, 'ROUND CLEAR', {
                fontSize: '36px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold',
                stroke: '#000', strokeThickness: 4
            }).setOrigin(0.5).setDepth(300).setAlpha(0);
            this.tweens.add({
                targets: clearTxt, alpha: 1, duration: 300, hold: 800,
                onComplete: () => {
                    clearTxt.destroy();
                    if (this.currentRound >= this.maxRounds) {
                        this.cameras.main.fadeOut(400, 0, 0, 0, (cam, progress) => {
                            if (progress === 1) this._endRun(true);
                        });
                    } else {
                        // Show card reward before repair UI
                        const aliveCars = Object.keys(this.trainCars).filter(k => this.trainCars[k].alive);
                        if (aliveCars.length > 0 && this.cardHand.length < CARGO_MAX_HAND) {
                            this._showCardRewardUI(aliveCars);
                        } else {
                            this.cameras.main.fadeOut(400, 0, 0, 0, (cam, progress) => {
                                if (progress === 1) this._showRepairUI();
                            });
                        }
                    }
                }
            });
        }
    }

    _snapshotAllyState() {
        this._lastAllyHp = {};
        this.allies.forEach(u => {
            const merc = this.party.find(m => m.id === u.mercId);
            if (!merc) return;
            if (u.alive) {
                this._lastAllyHp[u.mercId] = u.hp;
                merc.currentHp = u.hp;
            } else {
                if (!this.casualties.find(c => c.id === merc.id)) {
                    this.casualties.push(merc);
                }
                merc.alive = false;
            }
        });
    }

    // ─── Card Reward UI ────────────────────────────────────────

    _showCardRewardUI(aliveCars) {
        this._cardRewardObjects = [];
        const _cr = (obj) => { this._cardRewardObjects.push(obj); return obj; };

        // Pick a random alive car to generate cards from
        const sourceCar = aliveCars[Math.floor(Math.random() * aliveCars.length)];
        const carInfo = CARGO_CAR_CARDS[sourceCar];
        if (!carInfo) {
            this.cameras.main.fadeOut(400, 0, 0, 0, (cam, progress) => {
                if (progress === 1) this._showRepairUI();
            });
            return;
        }

        const choices = generateCargoCardChoices(sourceCar, this.currentRound, this.cardHand, this.zoneLevel);
        if (choices.length === 0) {
            this.cameras.main.fadeOut(400, 0, 0, 0, (cam, progress) => {
                if (progress === 1) this._showRepairUI();
            });
            return;
        }

        // Overlay
        _cr(this.add.rectangle(640, 360, 1280, 720, 0x000000, 0.85).setDepth(400));

        _cr(this.add.text(640, 100, `${carInfo.icon} ${carInfo.name}에서 카드 획득!`, {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(401));

        _cr(this.add.text(640, 130, `카드 1장을 선택하세요 (보유: ${this.cardHand.length}/${CARGO_MAX_HAND})`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa8866'
        }).setOrigin(0.5).setDepth(401));

        const cardW = 200;
        const cardH = 160;
        const gap = 30;
        const totalW = choices.length * cardW + (choices.length - 1) * gap;
        const startX = 640 - totalW / 2 + cardW / 2;

        choices.forEach((cardId, idx) => {
            const card = carInfo.cards[cardId];
            if (!card) return;
            const cx = startX + idx * (cardW + gap);
            const cy = 280;

            const tierColors = { 1: 0x446688, 2: 0x4488ff, 3: 0xffaa00 };
            const tierBorder = tierColors[card.tier] || 0x446688;

            // Card background
            const gfx = _cr(this.add.graphics().setDepth(402));
            gfx.fillStyle(0x1a1a2a, 1);
            gfx.fillRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
            gfx.lineStyle(2, tierBorder, 0.8);
            gfx.strokeRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);

            // Tier stars
            const tierLabel = '★'.repeat(card.tier);
            _cr(this.add.text(cx, cy - cardH / 2 + 15, tierLabel, {
                fontSize: '14px', fontFamily: 'monospace', color: card.tier >= 3 ? '#ffaa00' : card.tier >= 2 ? '#4488ff' : '#668899'
            }).setOrigin(0.5).setDepth(403));

            // Card icon + name
            _cr(this.add.text(cx, cy - 20, `${carInfo.icon}`, {
                fontSize: '24px'
            }).setOrigin(0.5).setDepth(403));

            _cr(this.add.text(cx, cy + 10, card.name, {
                fontSize: '14px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(403));

            _cr(this.add.text(cx, cy + 32, card.desc, {
                fontSize: '10px', fontFamily: 'monospace', color: '#aaaacc',
                wordWrap: { width: cardW - 20 }, align: 'center'
            }).setOrigin(0.5).setDepth(403));

            // Hit zone
            const hitZone = _cr(this.add.zone(cx, cy, cardW, cardH).setInteractive({ useHandCursor: true }).setDepth(404));
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
                gfx.lineStyle(2, tierBorder, 0.8);
                gfx.strokeRoundedRect(cx - cardW / 2, cy - cardH / 2, cardW, cardH, 8);
            });
            hitZone.on('pointerdown', () => {
                this._selectCardReward(cardId);
            });
        });

        // Skip button
        _cr(UIButton.create(this, 640, 420, 120, 30, '건너뛰기', {
            color: 0x333333, hoverColor: 0x444444, textColor: '#888888', fontSize: 11,
            onClick: () => this._skipCardReward()
        }).setDepth(405));
    }

    _selectCardReward(cardId) {
        this.cardHand.push(cardId);

        // Apply immediate effects for cargo cards (gold bonus, repair)
        const card = getCargoCard(cardId);
        if (card) {
            if (card.goldBonus) {
                this.totalGold += card.goldBonus;
                this.goldText.setText(`💰 ${this.totalGold}G`);
            }
            if (card.lootBonus) {
                this.lootBonusNextRound = (this.lootBonusNextRound || 0) + card.lootBonus;
            }
            if (card.repairRatio) {
                const aliveCars = Object.values(this.trainCars).filter(c => c.alive && c.hp < c.maxHp);
                if (aliveCars.length > 0) {
                    const target = aliveCars[Math.floor(Math.random() * aliveCars.length)];
                    const heal = Math.floor(target.maxHp * card.repairRatio);
                    target.hp = Math.min(target.maxHp, target.hp + heal);
                }
            }
            if (card.bonusLoot) {
                const item = generateItem(this.zoneKey, this.gameState.guildLevel, 1);
                if (item) this.loot.push(item);
            }
        }

        // Clean up and transition
        this._cardRewardObjects.forEach(obj => { if (obj && obj.destroy) obj.destroy(); });
        this._cardRewardObjects = [];

        const pickText = this.add.text(640, 300, `🃏 ${card ? card.name : '카드'} 획득!`, {
            fontSize: '22px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
            stroke: '#000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(400);
        this.tweens.add({
            targets: pickText, alpha: 0, y: 270, duration: 800, delay: 600,
            onComplete: () => {
                pickText.destroy();
                this.cameras.main.fadeOut(400, 0, 0, 0, (cam, progress) => {
                    if (progress === 1) this._showRepairUI();
                });
            }
        });
    }

    _skipCardReward() {
        this._cardRewardObjects.forEach(obj => { if (obj && obj.destroy) obj.destroy(); });
        this._cardRewardObjects = [];
        this.cameras.main.fadeOut(400, 0, 0, 0, (cam, progress) => {
            if (progress === 1) this._showRepairUI();
        });
    }

    // ─── Repair UI (between rounds) ─────────────────────────────

    _showRepairUI() {
        this.repairUIActive = true;
        this.cameras.main.fadeIn(300);

        // Hide battle HUD
        if (this._hudElements) {
            this._hudElements.forEach(el => { if (el && el.setVisible) el.setVisible(false); });
        }

        // Clean up battle units
        this.allUnits.forEach(u => u.destroy());
        this.allUnits = [];
        this.allies = [];
        this.enemies = [];

        // Clean up train display (will re-draw in repair UI)
        this._trainLabels.forEach(l => l.destroy());
        this._trainLabels = [];
        this._trainBars.forEach(b => { if (b.destroy) b.destroy(); });
        this._trainBars = [];
        this._trainIcons.forEach(i => i.destroy());
        this._trainIcons = [];
        if (this._trainGfx) { this._trainGfx.clear(); }

        this._repairUIObjects = [];

        // Background
        const bg = this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a).setDepth(80);
        this._repairUIObjects.push(bg);

        // Title
        const title = this.add.text(640, 60, `라운드 ${this.currentRound} 클리어!`, {
            fontSize: '24px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(81);
        this._repairUIObjects.push(title);

        // Status line
        const surviving = this.party.filter(m => m.alive).length;
        this._repairGoldText = this.add.text(640, 90, `생존: ${surviving}/${this.party.length}  💰 ${this.totalGold}G`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#aa6666'
        }).setOrigin(0.5).setDepth(81);
        this._repairUIObjects.push(this._repairGoldText);

        // Section label
        const secLabel = this.add.text(640, 120, '🚂 열차 상태 및 수리', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff8844'
        }).setOrigin(0.5).setDepth(81);
        this._repairUIObjects.push(secLabel);

        this._drawRepairCarCards();

        // Bulk repair button
        this._drawBulkRepairButton();

        // Fortify button
        this._drawFortifyButton();

        // Card usage section (station stop)
        this._drawCardUsageSection();

        // Proceed / Retreat
        this._drawRepairActions();
    }

    _drawRepairCarCards() {
        const carKeys = ['generator', 'ammo', 'medical', 'cargo'];
        const startY = 170;
        const cardW = 540;
        const cardH = 55;
        const gap = 10;

        carKeys.forEach((key, idx) => {
            const car = this.trainCars[key];
            const cy = startY + idx * (cardH + gap);
            const cx = 640 - cardW / 2;

            // Card background
            const cardGfx = this.add.graphics().setDepth(82);
            const isAlive = car.alive;
            cardGfx.fillStyle(isAlive ? 0x1a1a2a : 0x111111, 1);
            cardGfx.fillRoundedRect(cx, cy, cardW, cardH, 6);
            cardGfx.lineStyle(1, isAlive ? car.color : 0x444444, 0.5);
            cardGfx.strokeRoundedRect(cx, cy, cardW, cardH, 6);
            this._repairUIObjects.push(cardGfx);

            // Icon + Name
            const nameText = this.add.text(cx + 12, cy + 8, `${car.icon} ${car.name}`, {
                fontSize: '14px', fontFamily: 'monospace', color: isAlive ? '#ffffff' : '#666666', fontStyle: 'bold'
            }).setDepth(83);
            this._repairUIObjects.push(nameText);

            // Function label
            const funcText = this.add.text(cx + 12, cy + 28, car.func, {
                fontSize: '10px', fontFamily: 'monospace', color: isAlive ? '#aaaacc' : '#555555'
            }).setDepth(83);
            this._repairUIObjects.push(funcText);

            if (!isAlive) {
                // Destroyed label
                const destLabel = this.add.text(cx + cardW - 12, cy + cardH / 2, '파괴됨', {
                    fontSize: '12px', fontFamily: 'monospace', color: '#884444'
                }).setOrigin(1, 0.5).setDepth(83);
                this._repairUIObjects.push(destLabel);
                return;
            }

            // HP bar
            const hpRatio = car.hp / car.maxHp;
            const barX = cx + 150;
            const barY = cy + 10;
            const barW = 180;
            const barH = 12;

            const barGfx = this.add.graphics().setDepth(83);
            barGfx.fillStyle(0x222222, 1);
            barGfx.fillRoundedRect(barX, barY, barW, barH, 3);
            const hpColor = hpRatio > 0.6 ? 0x44ff44 : hpRatio > 0.3 ? 0xffaa00 : 0xff4444;
            barGfx.fillStyle(hpColor, 1);
            barGfx.fillRoundedRect(barX, barY, barW * Math.max(0, hpRatio), barH, 3);
            this._repairUIObjects.push(barGfx);

            const hpLabel = this.add.text(barX + barW + 8, barY + 1, `HP:${car.hp}/${car.maxHp}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#cccccc'
            }).setDepth(83);
            this._repairUIObjects.push(hpLabel);

            // Repair button (if not full HP)
            if (car.hp < car.maxHp) {
                const repairCost = 30 + this.currentRound * 5;
                const canRepair = this.totalGold >= repairCost;

                const repairBtn = UIButton.create(this, cx + cardW - 70, cy + cardH / 2, 120, 28,
                    `수리 ${repairCost}G`, {
                    color: canRepair ? 0x334422 : 0x222222,
                    hoverColor: canRepair ? 0x445533 : 0x222222,
                    textColor: canRepair ? '#88ff44' : '#555555',
                    fontSize: 10,
                    disabled: !canRepair,
                    onClick: () => {
                        if (this.totalGold < repairCost) return;
                        this.totalGold -= repairCost;
                        const healAmt = Math.floor(car.maxHp * 0.3);
                        car.hp = Math.min(car.maxHp, car.hp + healAmt);
                        UIToast.show(this, `${car.icon} ${car.name} 수리! HP ${car.hp}/${car.maxHp}`, { color: '#88ff44' });
                        this._refreshRepairUI();
                    }
                }).setDepth(84);
                this._repairUIObjects.push(repairBtn);
            } else {
                const fullLabel = this.add.text(cx + cardW - 12, cy + cardH / 2, '만렑', {
                    fontSize: '11px', fontFamily: 'monospace', color: '#44ff44'
                }).setOrigin(1, 0.5).setDepth(83);
                this._repairUIObjects.push(fullLabel);
            }
        });
    }

    _drawBulkRepairButton() {
        const aliveCars = Object.values(this.trainCars).filter(c => c.alive && c.hp < c.maxHp);
        if (aliveCars.length === 0) return;

        const singleCost = 30 + this.currentRound * 5;
        const totalCost = Math.floor(singleCost * aliveCars.length * 0.8);
        const canAfford = this.totalGold >= totalCost;

        const btn = UIButton.create(this, 480, 450, 200, 32, `전체 수리 ${totalCost}G`, {
            color: canAfford ? 0x2a3a2a : 0x222222,
            hoverColor: canAfford ? 0x3a4a3a : 0x222222,
            textColor: canAfford ? '#88ff88' : '#555555',
            fontSize: 11,
            disabled: !canAfford,
            onClick: () => {
                if (this.totalGold < totalCost) return;
                this.totalGold -= totalCost;
                aliveCars.forEach(car => {
                    const healAmt = Math.floor(car.maxHp * 0.3);
                    car.hp = Math.min(car.maxHp, car.hp + healAmt);
                });
                UIToast.show(this, '전체 열차 수리 완료!', { color: '#88ff88' });
                this._refreshRepairUI();
            }
        }).setDepth(84);
        this._repairUIObjects.push(btn);
    }

    _drawFortifyButton() {
        const fortifyCost = 50 + this.currentRound * 10;
        const canFortify = this.totalGold >= fortifyCost;
        const aliveCars = Object.values(this.trainCars).filter(c => c.alive);
        if (aliveCars.length === 0) return;

        const btn = UIButton.create(this, 760, 450, 220, 32, `강화: 칸 HP+20 ${fortifyCost}G`, {
            color: canFortify ? 0x2a2a3a : 0x222222,
            hoverColor: canFortify ? 0x3a3a4a : 0x222222,
            textColor: canFortify ? '#8888ff' : '#555555',
            fontSize: 11,
            disabled: !canFortify,
            onClick: () => {
                if (this.totalGold < fortifyCost) return;
                this.totalGold -= fortifyCost;
                const target = aliveCars[Math.floor(Math.random() * aliveCars.length)];
                target.maxHp += 20;
                target.hp += 20;
                UIToast.show(this, `${target.icon} ${target.name} 강화! 최대 HP +20`, { color: '#8888ff' });
                this._refreshRepairUI();
            }
        }).setDepth(84);
        this._repairUIObjects.push(btn);
    }

    _drawCardUsageSection() {
        if (this.cardHand.length === 0) return;

        const secY = 490;
        const label = this.add.text(640, secY, `🃏 보유 카드 (${this.cardHand.length}/${CARGO_MAX_HAND}) — 클릭하여 사용`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#ffaa44'
        }).setOrigin(0.5).setDepth(81);
        this._repairUIObjects.push(label);

        const cardW = 150;
        const gap = 10;
        const totalW = this.cardHand.length * (cardW + gap) - gap;
        const startX = 640 - totalW / 2;

        this.cardHand.forEach((cardId, idx) => {
            const card = getCargoCard(cardId);
            if (!card) return;
            const carInfo = CARGO_CAR_CARDS[card.carKey];
            const cx = startX + idx * (cardW + gap);
            const cy = secY + 20;

            const tierColors = { 1: 0x446688, 2: 0x4488ff, 3: 0xffaa00 };
            const tierBorder = tierColors[card.tier] || 0x446688;

            const gfx = this.add.graphics().setDepth(82);
            gfx.fillStyle(0x1a1a2a, 1);
            gfx.fillRoundedRect(cx, cy, cardW, 50, 4);
            gfx.lineStyle(1, tierBorder, 0.6);
            gfx.strokeRoundedRect(cx, cy, cardW, 50, 4);
            this._repairUIObjects.push(gfx);

            this._repairUIObjects.push(this.add.text(cx + 6, cy + 5, `${carInfo.icon} ${card.name}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#ffffff', fontStyle: 'bold'
            }).setDepth(83));

            this._repairUIObjects.push(this.add.text(cx + 6, cy + 20, card.desc, {
                fontSize: '9px', fontFamily: 'monospace', color: '#aaaacc'
            }).setDepth(83));

            // Use button
            const useBtn = UIButton.create(this, cx + cardW - 25, cy + 37, 45, 18, '사용', {
                color: 0x445522, hoverColor: 0x556633, textColor: '#aaff44', fontSize: 9,
                onClick: () => this._useCard(idx)
            }).setDepth(84);
            this._repairUIObjects.push(useBtn);

            // Discard button
            const discardBtn = UIButton.create(this, cx + cardW - 70, cy + 37, 40, 18, '버림', {
                color: 0x442222, hoverColor: 0x553333, textColor: '#ff8888', fontSize: 9,
                onClick: () => this._discardCard(idx)
            }).setDepth(84);
            this._repairUIObjects.push(discardBtn);
        });
    }

    _useCard(handIdx) {
        if (handIdx < 0 || handIdx >= this.cardHand.length) return;
        const cardId = this.cardHand[handIdx];
        const card = getCargoCard(cardId);
        if (!card) return;

        // Build dummy unit list from party mercs for buff/heal application
        const livingParty = this.party.filter(m => m.alive);
        const dummyUnits = livingParty.map(merc => {
            const stats = merc.getStats();
            return {
                atk: stats.atk, def: stats.def, hp: merc.currentHp, maxHp: stats.hp,
                moveSpeed: stats.moveSpeed, critRate: stats.critRate, critDmg: stats.critDmg || 1.5,
                attackSpeed: stats.attackSpeed || 1000, lifestealOnKill: 0, lastStand: false,
                _merc: merc
            };
        });

        // Apply card effect
        card.apply(dummyUnits);

        // Write back stat changes to mercenary for next round
        dummyUnits.forEach(du => {
            du._merc.currentHp = Math.min(du.maxHp, du.hp);
        });

        // Handle special cargo card effects
        if (card.goldBonus) {
            this.totalGold += card.goldBonus;
        }
        if (card.repairRatio) {
            const aliveCars = Object.values(this.trainCars).filter(c => c.alive && c.hp < c.maxHp);
            if (aliveCars.length > 0) {
                const target = aliveCars[Math.floor(Math.random() * aliveCars.length)];
                const heal = Math.floor(target.maxHp * card.repairRatio);
                target.hp = Math.min(target.maxHp, target.hp + heal);
            }
        }
        if (card.lootBonus) {
            this.lootBonusNextRound = (this.lootBonusNextRound || 0) + card.lootBonus;
        }
        if (card.bonusLoot) {
            const item = generateItem(this.zoneKey, this.gameState.guildLevel, 1);
            if (item) this.loot.push(item);
        }

        // Remove from hand
        this.cardHand.splice(handIdx, 1);

        UIToast.show(this, `🃏 ${card.name} 사용! ${card.desc}`, { color: '#ffcc44' });
        this._refreshRepairUI();
    }

    _discardCard(handIdx) {
        if (handIdx < 0 || handIdx >= this.cardHand.length) return;
        const cardId = this.cardHand[handIdx];
        const card = getCargoCard(cardId);
        this.cardHand.splice(handIdx, 1);
        UIToast.show(this, `${card ? card.name : '카드'} 버림`, { color: '#888888' });
        this._refreshRepairUI();
    }

    _drawRepairActions() {
        const actionsY = this.cardHand.length > 0 ? 580 : 520;

        // Proceed button
        const proceedBtn = UIButton.create(this, 760, actionsY, 180, 36, '▶ 다음 라운드', {
            color: 0x225533, hoverColor: 0x336644,
            textColor: '#44ff88', fontSize: 13,
            onClick: () => this._proceedToNextRound()
        }).setDepth(84);
        this._repairUIObjects.push(proceedBtn);

        // Retreat button
        const retreatBtn = UIButton.create(this, 480, actionsY, 140, 36, '🚪 철수', {
            color: 0x332222, hoverColor: 0x443333,
            textColor: '#ff8888', fontSize: 12,
            onClick: () => this._endRun(false)
        }).setDepth(84);
        this._repairUIObjects.push(retreatBtn);
    }

    _refreshRepairUI() {
        // Clear and redraw repair UI
        this._repairUIObjects.forEach(obj => { if (obj && obj.destroy) obj.destroy(); });
        this._repairUIObjects = [];

        // Re-draw everything
        const bg = this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a).setDepth(80);
        this._repairUIObjects.push(bg);

        const title = this.add.text(640, 60, `라운드 ${this.currentRound} 클리어!`, {
            fontSize: '24px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(81);
        this._repairUIObjects.push(title);

        const surviving = this.party.filter(m => m.alive).length;
        this._repairGoldText = this.add.text(640, 90, `생존: ${surviving}/${this.party.length}  💰 ${this.totalGold}G`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#aa6666'
        }).setOrigin(0.5).setDepth(81);
        this._repairUIObjects.push(this._repairGoldText);

        const secLabel = this.add.text(640, 120, '🚂 열차 상태 및 수리', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff8844'
        }).setOrigin(0.5).setDepth(81);
        this._repairUIObjects.push(secLabel);

        this._drawRepairCarCards();
        this._drawBulkRepairButton();
        this._drawFortifyButton();
        this._drawCardUsageSection();
        this._drawRepairActions();
    }

    // ─── Next Round / End Run ────────────────────────────────────

    _proceedToNextRound() {
        const hpState = {};
        this.party.forEach(merc => {
            if (!merc.alive) return;
            const snapshotHp = this._lastAllyHp && this._lastAllyHp[merc.id] !== undefined
                ? this._lastAllyHp[merc.id] : merc.currentHp;
            const stats = merc.getStats();
            hpState[merc.id] = Math.min(stats.hp, snapshotHp);
        });

        this.scene.restart({
            gameState: this.gameState,
            party: this.party,
            currentRound: this.currentRound + 1,
            totalGold: this.totalGold,
            totalXp: this.totalXp,
            loot: this.loot,
            casualties: this.casualties,
            partyHpState: hpState,
            trainCars: this.trainCars,
            cardHand: this.cardHand,
            lootBonusNextRound: this.lootBonusNextRound,
            visitedStations: this.visitedStations,
            currentStation: null
        });
    }

    _endRun(success) {
        this._snapshotAllyState();
        const survivors = this.party.filter(m => m.alive);
        const allCasualties = [...this.casualties];

        // Cargo car survival bonus
        let cargoBonus = 0;
        if (this.trainCars.cargo.alive && success) {
            cargoBonus = Math.floor(50 + this.trainCars.cargo.hp * 1.5);
            this.totalGold += cargoBonus;
        }

        // Penalty if all cars destroyed
        const allDestroyed = Object.values(this.trainCars).every(c => !c.alive);
        if (allDestroyed) {
            this.totalGold = Math.floor(this.totalGold * 0.5);
        }

        const result = {
            success,
            zoneKey: 'cargo',
            zoneLevel: this.zoneLevel,
            rounds: this.currentRound,
            goldEarned: this.totalGold,
            xpEarned: this.totalXp,
            loot: this.loot,
            casualties: allCasualties,
            survivors,
            zoneLevelUp: success,
            trainCars: this.trainCars,
            cargoBonus
        };

        this.scene.start('RunResultScene', {
            gameState: this.gameState,
            result
        });
    }

    // ─── Main Update Loop ────────────────────────────────────────

    update(time, delta) {
        if (this.battleOver || this.repairUIActive) return;

        const dt = delta * this.speedMultiplier;
        this.battleTime += dt;
        this.timerText.setText((this.battleTime / 1000).toFixed(1) + '초');

        // Kill streak decay
        if (this.killStreakTimer > 0) {
            this.killStreakTimer -= dt;
            if (this.killStreakTimer <= 0) this.killStreak = 0;
        }

        if (this._stormTimer !== undefined) {
            this._stormTimer -= dt;
            if (this._stormTimer <= 0) {
                this._stormTimer = this._stormInterval;
                const pct = this._stormDmgPercent || 0.05;
                [...this.allies, ...this.enemies].forEach(u => {
                    if (!u.alive) return;
                    const dmg = Math.max(1, Math.floor(u.maxHp * pct));
                    u.hp -= dmg;
                    DamagePopup.show(this, u.container.x, u.container.y - 25, `폭풍 -${dmg}`, 0x4488ff, false);
                    if (u.hp <= 0) { u.hp = 0; u.die(null, this.allUnits); }
                });
                for (const car of Object.values(this.trainCars)) {
                    if (car.alive) car.hp -= Math.floor(car.maxHp * 0.03);
                    if (car.hp <= 0) { car.hp = 0; car.alive = false; }
                }
                this._drawTrainDisplay();
            }
        }

        const prevDeadCount = this.enemies.filter(e => !e.alive).length;

        // Update all units
        for (const unit of this.allUnits) {
            if (unit.alive) unit.update(dt, this.allUnits);
        }

        // Check for new kills (for kill streak)
        const newDeadCount = this.enemies.filter(e => !e.alive).length;
        if (newDeadCount > prevDeadCount) {
            const killsThisFrame = newDeadCount - prevDeadCount;
            this.killStreak += killsThisFrame;
            this.killStreakTimer = 3000;
            this._showKillStreak();
        }

        // Track ally HP
        this._lastAllyHp = {};
        this.allies.forEach(u => {
            if (u.alive && u.mercId) {
                this._lastAllyHp[u.mercId] = u.hp;
            }
        });

        // Infiltrator logic
        this._updateInfiltrators(dt);

        // Update train display (live HP bars)
        this._updateTrainDisplay();

        // Danger vignette
        const critAlly = this.allies.find(a => a.alive && a.hp / a.maxHp <= 0.25);
        if (critAlly && !this._dangerVignette) {
            this._dangerVignette = this.add.rectangle(640, 360, 1280, 720)
                .setStrokeStyle(6, 0xff0000, 0.3).setFillStyle(0x000000, 0).setDepth(200);
            this.tweens.add({ targets: this._dangerVignette, alpha: 0.15, duration: 400, yoyo: true, repeat: -1 });
        } else if (!critAlly && this._dangerVignette) {
            this._dangerVignette.destroy();
            this._dangerVignette = null;
        }

        // Also show danger for critically damaged cars
        const critCar = Object.values(this.trainCars).find(c => c.alive && c.hp / c.maxHp <= 0.25);
        if (critCar && !this._carDangerText) {
            this._carDangerText = this.add.text(640, 555, `⚠ ${critCar.icon} ${critCar.name} 위험!`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold',
                stroke: '#000', strokeThickness: 2
            }).setOrigin(0.5).setDepth(50);
            this.tweens.add({ targets: this._carDangerText, alpha: 0.3, duration: 500, yoyo: true, repeat: -1 });
        } else if (!critCar && this._carDangerText) {
            this._carDangerText.destroy();
            this._carDangerText = null;
        }
    }

    _showKillStreak() {
        const labels = { 2: 'DOUBLE KILL!', 3: 'TRIPLE KILL!', 4: 'ULTRA KILL!', 5: 'MASSACRE!' };
        const label = this.killStreak >= 5 ? 'MASSACRE!' : labels[this.killStreak];
        if (!label) return;
        const colors = { 2: '#ffcc44', 3: '#ff8844', 4: '#ff4444', 5: '#ff2222' };
        const color = this.killStreak >= 5 ? '#ff2222' : colors[this.killStreak] || '#ffcc44';
        const txt = this.add.text(640, 200, label, {
            fontSize: '28px', fontFamily: 'monospace', color, fontStyle: 'bold',
            stroke: '#000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(300).setScale(0.5);
        this.tweens.add({ targets: txt, scale: 1.2, duration: 200, yoyo: true, hold: 400 });
        this.tweens.add({ targets: txt, alpha: 0, y: 170, duration: 800, delay: 600, onComplete: () => txt.destroy() });
    }

    // ─── Cleanup ─────────────────────────────────────────────────

    shutdown() {
        this.allUnits.forEach(u => u.destroy());
        if (this._dangerVignette) { this._dangerVignette.destroy(); this._dangerVignette = null; }
        if (this._carDangerText) { this._carDangerText.destroy(); this._carDangerText = null; }
        this._trainLabels.forEach(l => l.destroy());
        this._trainBars.forEach(b => { if (b.destroy) b.destroy(); });
        this._trainIcons.forEach(i => i.destroy());
        if (this._trainGfx) this._trainGfx.destroy();
        if (this._cardHandElements) this._cardHandElements.forEach(el => { if (el && el.destroy) el.destroy(); });
        if (this._cardRewardObjects) this._cardRewardObjects.forEach(obj => { if (obj && obj.destroy) obj.destroy(); });
    }
}
