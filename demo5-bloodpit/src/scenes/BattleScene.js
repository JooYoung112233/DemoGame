class BPBattleScene extends Phaser.Scene {
    constructor() { super('BPBattleScene'); }

    init(data) {
        this.partyIds = data.party || [];
        this.dangerSystem = data.dangerSystem || new DangerSystem();
        this.dropSystem = data.dropSystem || new DropSystem();
        this.runManager = data.runManager;
        this.appliedDrops = data.appliedDrops || [];
        this.prevAllies = data.allies || [];
        this.battleTime = data.time || 0;
    }

    create() {
        this.cameras.main.setBackgroundColor('#1a0a0a');
        this.drawBackground();

        this.allies = [];
        this.enemies = [];
        this.combatSystem = new BPCombatSystem(this);
        this.momentum = new MomentumSystem();
        this.battleOver = false;
        this.speedMultiplier = parseInt(localStorage.getItem('bp_speed') || '1', 10) || 1;
        this.farmDropShown = false;
        this.killStreak = 0;
        this.killStreakTimer = 0;

        const round = this.runManager ? this.runManager.getCurrentRound() : null;
        const roundNum = round ? round.round : this.dangerSystem.level;
        const isBoss = round ? round.type === 'boss' : this.dangerSystem.isBossRound();

        if (this.dangerSystem.level === 0 && round) {
            this.dangerSystem.level = round.dangerLevel;
        }

        const dangerLabel = isBoss ? '💀 보스 출현!' : `라운드 ${roundNum} / 20  —  위험도 ${this.dangerSystem.level}`;
        this.dangerText = this.add.text(640, 25, dangerLabel, {
            fontSize: '20px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
        }).setOrigin(0.5).setDepth(100);

        this.drawDangerMeter();

        this.timerText = this.add.text(640, 50, '0.0초', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aa6666'
        }).setOrigin(0.5).setDepth(100);

        // round preview
        if (this.runManager) {
            const preview = this.runManager.getRoundPreview(3);
            if (preview.length > 0) {
                const icons = preview.map(r => this.runManager.getRoundIcon(r.type)).join(' ');
                this.add.text(640, 68, `다음: ${icons}`, {
                    fontSize: '11px', fontFamily: 'monospace', color: '#666688'
                }).setOrigin(0.5).setDepth(100);
            }
        }

        const syns = this.dropSystem.getSynergyInfo();
        if (syns.length > 0) {
            const synText = syns.map(s => `${s.name}(${s.count})`).join(' | ');
            this.add.text(640, 84, synText, {
                fontSize: '11px', fontFamily: 'monospace', color: '#ffaa44'
            }).setOrigin(0.5).setDepth(100);
        }

        this.spawnAllies();
        this.applyMilestones();
        this.spawnEnemies();
        this.applyCollectedDrops();
        this.applyEncounterModifier();

        this.bagText = this.add.text(60, 25, '', {
            fontSize: '12px', fontFamily: 'monospace', color: '#ffaa44'
        }).setDepth(100);
        this.updateBagCount();

        this.showRoundAnnounce(roundNum, isBoss);

        const speedBtn = this.add.text(1220, 30, `×${this.speedMultiplier}`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold',
            backgroundColor: '#331111', padding: { x: 6, y: 3 }
        }).setOrigin(0.5).setDepth(100).setInteractive();
        speedBtn.on('pointerdown', () => {
            this.speedMultiplier = this.speedMultiplier === 1 ? 2 : this.speedMultiplier === 2 ? 3 : 1;
            speedBtn.setText(`×${this.speedMultiplier}`);
            localStorage.setItem('bp_speed', this.speedMultiplier);
        });

        // Auto-use consumables toggle
        this.autoUseConsumables = localStorage.getItem('bp_auto_consumable') === 'true';
        this._autoUsedWarPotion = false;
        const autoLabel = () => this.autoUseConsumables ? '자동 ON' : '자동 OFF';
        const autoColor = () => this.autoUseConsumables ? '#44ff88' : '#886644';
        this.autoBtn = this.add.text(1220, 58, autoLabel(), {
            fontSize: '12px', fontFamily: 'monospace', color: autoColor(), fontStyle: 'bold',
            backgroundColor: '#331111', padding: { x: 4, y: 2 }
        }).setOrigin(0.5).setDepth(100).setInteractive();
        this.autoBtn.on('pointerdown', () => {
            this.autoUseConsumables = !this.autoUseConsumables;
            localStorage.setItem('bp_auto_consumable', this.autoUseConsumables);
            this.autoBtn.setText(autoLabel());
            this.autoBtn.setColor(autoColor());
        });

        // Consumable bar
        this._consumableBarObjects = [];
        this._drawConsumableBar();
    }

    drawBackground() {
        const gfx = this.add.graphics();
        gfx.fillGradientStyle(0x1a0a0a, 0x1a0a0a, 0x2a1515, 0x2a1515);
        gfx.fillRect(0, 0, 1280, 720);
        gfx.fillStyle(0x221111);
        gfx.fillRect(0, 450, 1280, 270);
        gfx.fillStyle(0x331818);
        gfx.fillRect(0, 448, 1280, 4);
        for (let i = 0; i < 8; i++) {
            const x = Phaser.Math.Between(50, 1230);
            const y = Phaser.Math.Between(460, 700);
            gfx.fillStyle(0x330000, 0.3);
            gfx.fillCircle(x, y, Phaser.Math.Between(5, 15));
        }
    }

    drawDangerMeter() {
        const meterX = 440, meterY = 42, meterW = 400, meterH = 6;
        this.add.rectangle(meterX + meterW / 2, meterY, meterW, meterH, 0x331111).setDepth(100);
        const fillW = (this.dangerSystem.level / this.dangerSystem.maxLevel) * meterW;
        const fillColor = this.dangerSystem.level <= 6 ? 0x44aa44 : this.dangerSystem.level <= 13 ? 0xffaa00 : 0xff2222;
        if (fillW > 0) {
            this.add.rectangle(meterX + fillW / 2, meterY, fillW, meterH, fillColor).setDepth(101);
        }
    }

    spawnAllies() {
        const startX = 180;
        const spacing = 70;
        this.partyIds.forEach((charId, i) => {
            const rosterChar = BP_ROSTER.getById(charId);
            if (!rosterChar) return;
            const baseData = BP_ALLIES[rosterChar.classKey];
            if (!baseData) return;
            const eStats = BP_ROSTER.getEffectiveStats(rosterChar);
            const data = {
                ...baseData,
                hp: eStats.hp,
                atk: eStats.atk,
                def: eStats.def,
                attackSpeed: eStats.attackSpeed,
                range: eStats.range,
                moveSpeed: eStats.moveSpeed,
                critRate: eStats.critRate,
                critDmg: eStats.critDmg,
                bleedChance: eStats.bleedChance || 0,
                dodgeRate: eStats.dodgeRate || 0,
                lifesteal: eStats.lifesteal || 0,
                thorns: eStats.thorns || 0,
                name: `${baseData.name} ${rosterChar.name}`,
                charId: charId
            };
            const x = startX + i * spacing;
            const y = 430 + (i % 2 === 0 ? 0 : 15);
            const char = new BPCharacter(this, data, 'ally', x, y);
            char.charId = charId;
            if (BP_SKILLS[rosterChar.classKey]) {
                char.skill = BP_SKILLS[rosterChar.classKey];
                char.skillCooldown = BP_SKILLS[rosterChar.classKey].cooldown;
                char.skillTimer = 0;
            }
            this.allies.push(char);
        });
    }

    spawnEnemies() {
        const enemyData = this.dangerSystem.getEnemyComposition();
        const startX = 1100;
        const spacing = 55;
        enemyData.forEach((data, i) => {
            const x = startX - (i % 6) * spacing;
            const y = 420 + Math.floor(i / 6) * 40 + (i % 2 === 0 ? 0 : 15);
            const char = new BPCharacter(this, data, 'enemy', x, y);
            this.enemies.push(char);
        });
    }

    applyCollectedDrops() {
        const aliveAllies = this.allies.filter(a => a.alive);
        this.appliedDrops.forEach(drop => {
            if (drop.type === 'passive' && drop.apply) {
                drop.apply(aliveAllies);
            } else if (drop.type === 'weapon') {
                aliveAllies.forEach(a => {
                    if (a.role === 'dps' || a.role === 'tank') {
                        a.atk += drop.atkBonus;
                        if (drop.spdBonus !== 0) a.attackSpeed = Math.floor(a.attackSpeed * (1 - drop.spdBonus));
                        if (drop.rangeBonus) a.range += drop.rangeBonus;
                    }
                });
            }
        });
    }

    applyEncounterModifier() {
        const mod = this.dangerSystem.getEncounterModifier();
        if (!mod) return;
        if (mod.apply) mod.apply(this.enemies);
        if (mod.applyAllies) mod.applyAllies(this.allies);
        this.add.text(640, 100, `⚠️ ${mod.name}: ${mod.desc}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#ff6644'
        }).setOrigin(0.5).setDepth(100);
    }

    showRoundAnnounce(roundNum, isBoss) {
        const label = isBoss ? '💀 보스 출현!' : `라운드 ${roundNum}`;
        const txt = this.add.text(640, 300, label, {
            fontSize: '38px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold',
            stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(200).setAlpha(0);
        this.tweens.add({
            targets: txt, alpha: 1, duration: 300,
            yoyo: true, hold: 600,
            onComplete: () => txt.destroy()
        });
    }

    updateBagCount() {
        const inv = BP_FARMING.getRaidInventory();
        this.bagText.setText(`🎒 ${inv.length}`);
    }

    _drawConsumableBar() {
        // Clear previous bar
        this._consumableBarObjects.forEach(obj => obj.destroy());
        this._consumableBarObjects = [];

        if (this.battleOver) return;

        const raidInv = BP_FARMING.getRaidInventory();
        const consumables = [];
        raidInv.forEach((item, idx) => {
            const reg = ItemRegistry.get(item.itemId);
            if (reg && reg.category === 'consumable' && item.itemId !== 'escape_kit') {
                consumables.push({ item, reg, actualIndex: idx });
            }
        });

        if (consumables.length === 0) {
            const emptyTxt = this.add.text(640, 695, '소비 아이템 없음', {
                fontSize: '10px', fontFamily: 'monospace', color: '#444444'
            }).setOrigin(0.5).setDepth(200);
            this._consumableBarObjects.push(emptyTxt);
            return;
        }

        // bar background
        const maxShow = Math.min(consumables.length, 6);
        const btnW = 150, btnH = 36, gap = 8;
        const totalW = maxShow * btnW + (maxShow - 1) * gap;
        const startX = 640 - totalW / 2;
        const y = 692;

        // bar label
        const barLabel = this.add.text(startX + totalW / 2, y - 26, `🧪 소비 아이템 (${consumables.length})`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#886644'
        }).setOrigin(0.5).setDepth(200);
        this._consumableBarObjects.push(barLabel);

        for (let i = 0; i < maxShow; i++) {
            const c = consumables[i];
            const bx = startX + i * (btnW + gap) + btnW / 2;

            const bg = this.add.rectangle(bx, y, btnW, btnH, 0x332211, 0.9)
                .setStrokeStyle(2, 0x886633).setDepth(200).setInteractive();
            this._consumableBarObjects.push(bg);

            const label = this.add.text(bx, y, `${c.reg.icon} ${c.reg.name}`, {
                fontSize: '12px', fontFamily: 'monospace', color: '#ffddaa', fontStyle: 'bold'
            }).setOrigin(0.5).setDepth(201);
            this._consumableBarObjects.push(label);

            const actualIdx = c.actualIndex;
            bg.on('pointerdown', () => {
                if (this.battleOver) return;
                BP_FARMING.useConsumable(actualIdx, this.allies, this.enemies, this);
                this.updateBagCount();
                this._drawConsumableBar();
            });
            bg.on('pointerover', () => { bg.setFillStyle(0x554422, 1); bg.setStrokeStyle(2, 0xffaa44); });
            bg.on('pointerout', () => { bg.setFillStyle(0x332211, 0.9); bg.setStrokeStyle(2, 0x886633); });
        }
    }

    _checkAutoConsumables() {
        if (this.battleOver) return;
        const raidInv = BP_FARMING.getRaidInventory();
        let used = false;

        // war_potion: auto-use at battle start (first frame)
        if (!this._autoUsedWarPotion) {
            this._autoUsedWarPotion = true;
            const wpIdx = raidInv.findIndex(item => item.itemId === 'war_potion');
            if (wpIdx !== -1) {
                BP_FARMING.useConsumable(wpIdx, this.allies, this.enemies, this);
                used = true;
            }
        }

        // pit_bandage: auto-use when any ally HP < 30%
        if (!used) {
            const needHeal = this.allies.some(a => a.alive && a.hp / a.maxHp < 0.3);
            if (needHeal) {
                const freshInv = BP_FARMING.getRaidInventory();
                const pbIdx = freshInv.findIndex(item => item.itemId === 'pit_bandage');
                if (pbIdx !== -1) {
                    BP_FARMING.useConsumable(pbIdx, this.allies, this.enemies, this);
                    used = true;
                }
            }
        }

        // fire_bomb: auto-use when 4+ enemies alive
        if (!used) {
            const aliveEnemies = this.enemies.filter(e => e.alive).length;
            if (aliveEnemies >= 4) {
                const freshInv = BP_FARMING.getRaidInventory();
                const fbIdx = freshInv.findIndex(item => item.itemId === 'fire_bomb');
                if (fbIdx !== -1) {
                    BP_FARMING.useConsumable(fbIdx, this.allies, this.enemies, this);
                    used = true;
                }
            }
        }

        if (used) {
            this.updateBagCount();
            this._drawConsumableBar();
        }
    }

    update(time, delta) {
        if (this.battleOver) return;
        const dt = delta * this.speedMultiplier;
        this.battleTime += dt;
        this.timerText.setText((this.battleTime / 1000).toFixed(1) + '초');

        if (this.autoUseConsumables) {
            this._checkAutoConsumables();
        }

        // kill streak timer
        if (this.killStreakTimer > 0) {
            this.killStreakTimer -= dt;
            if (this.killStreakTimer <= 0) this.killStreak = 0;
        }

        const prevDeadCount = this.enemies.filter(e => !e.alive).length;
        this.combatSystem.update(dt, this.allies, this.enemies);
        const newDeadCount = this.enemies.filter(e => !e.alive).length;

        this.momentum.update(dt);
        this._drawMomentumBar();

        if (newDeadCount > prevDeadCount) {
            const killsThisFrame = newDeadCount - prevDeadCount;
            this.killStreak += killsThisFrame;
            this.killStreakTimer = 3000;
            this._showKillStreak();
            for (let k = 0; k < killsThisFrame; k++) this.momentum.onKill();

            for (let k = 0; k < killsThisFrame; k++) {
                if (Math.random() < 0.4 + this.dangerSystem.level * 0.03) {
                    const drop = BP_FARMING.rollFarmingDrop(this.dangerSystem.level);
                    if (drop) {
                        const deadEnemy = this.enemies.filter(e => !e.alive)[prevDeadCount + k];
                        const dx = deadEnemy ? deadEnemy.container.x : 800;
                        const dy = deadEnemy ? deadEnemy.container.y - 40 : 400;
                        FarmingUI.drawLootPopup(this, drop, dx, dy);
                        this.updateBagCount();
                    }
                }
            }
        }

        // danger: vignette when ally HP low
        const critAlly = this.allies.find(a => a.alive && a.hp / a.maxHp <= 0.25);
        if (critAlly && !this._dangerVignette) {
            this._dangerVignette = this.add.rectangle(640, 360, 1280, 720)
                .setStrokeStyle(6, 0xff0000, 0.3).setFillStyle(0x000000, 0).setDepth(200);
            this.tweens.add({ targets: this._dangerVignette, alpha: 0.15, duration: 400, yoyo: true, repeat: -1 });
        } else if (!critAlly && this._dangerVignette) {
            this._dangerVignette.destroy();
            this._dangerVignette = null;
        }

        const alliesAlive = this.allies.some(a => a.alive);
        const enemiesAlive = this.enemies.some(e => e.alive);

        if (!enemiesAlive) {
            this.battleOver = true;
            this.cameras.main.shake(100, 0.002);
            this.time.delayedCall(800, () => this.onRoundCleared());
        } else if (!alliesAlive) {
            this.battleOver = true;
            this.cameras.main.shake(200, 0.005);
            this.time.delayedCall(800, () => this.onDefeat());
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

    onRoundCleared() {
        // add gold reward
        const goldReward = 15 + this.dangerSystem.level * 8;
        let totalGold = goldReward;
        if (this.dangerSystem.isBossRound()) totalGold += 300;
        else if (this.dangerSystem.isMiniBossRound()) totalGold += 100;
        StashManager.addGold(totalGold);

        // add EXP to surviving characters
        const expPerChar = 30 + this.dangerSystem.level * 5;
        this.allies.forEach(a => {
            if (a.alive && a.charId) {
                BP_ROSTER.addExp(a.charId, expPerChar);
            }
        });

        // mark dead characters
        this.allies.forEach(a => {
            if (!a.alive && a.charId) {
                BP_ROSTER.markDead(a.charId);
            }
        });

        // tick recovery for all roster
        BP_ROSTER.tickRecovery();

        // show clear banner
        const txt = this.add.text(640, 300, 'ROUND CLEAR', {
            fontSize: '36px', fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold',
            stroke: '#000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(300).setAlpha(0);
        this.tweens.add({
            targets: txt, alpha: 1, duration: 300, hold: 800,
            onComplete: () => {
                this.cameras.main.fadeOut(400, 0, 0, 0, (cam, progress) => {
                    if (progress === 1) this._routeNext();
                });
            }
        });
    }

    onDefeat() {
        // mark all dead
        this.allies.forEach(a => {
            if (!a.alive && a.charId) {
                BP_ROSTER.markDead(a.charId);
            }
        });

        // lose half of raid inventory
        const raidInv = BP_FARMING.getRaidInventory();
        const halfIdx = Math.ceil(raidInv.length / 2);
        const kept = raidInv.slice(0, halfIdx);
        kept.forEach(item => StashManager.addToStash(item));
        BP_FARMING.raidInventory = [];

        StashManager.recordRaid({ extracted: false, totalValue: 0 });

        this.scene.start('BPResultScene', {
            victory: false,
            dangerLevel: this.dangerSystem.level,
            drops: this.dropSystem.collectedDrops,
            allies: this.allies,
            party: this.partyIds,
            time: this.battleTime
        });
    }

    _routeNext() {
        if (!this.runManager) {
            this.scene.start('BPDropScene', {
                party: this.partyIds, dangerSystem: this.dangerSystem,
                dropSystem: this.dropSystem, runManager: this.runManager,
                appliedDrops: [...this.appliedDrops], allies: this.allies, time: this.battleTime
            });
            return;
        }

        // go to DropScene for in-game drop selection
        this.scene.start('BPDropScene', {
            party: this.partyIds,
            dangerSystem: this.dangerSystem,
            dropSystem: this.dropSystem,
            runManager: this.runManager,
            appliedDrops: [...this.appliedDrops],
            allies: this.allies,
            time: this.battleTime
        });
    }

    applyMilestones() {
        this.allies.forEach(ally => {
            if (!ally.charId) return;
            const rChar = BP_ROSTER.getById(ally.charId);
            if (!rChar) return;
            const role = (typeof BP_MILESTONE_ROLE_MAP !== 'undefined' && BP_MILESTONE_ROLE_MAP[rChar.classKey]) || null;
            if (!role || typeof BP_MILESTONES === 'undefined') return;
            const tree = BP_MILESTONES[role];
            if (!tree) return;
            const lvls = [5, 10, 15, 20];
            lvls.forEach(lv => {
                if (rChar.level >= lv && tree[lv]) {
                    if (tree[lv].apply) tree[lv].apply(ally);
                    if (tree[lv].applyParty) tree[lv].applyParty(this.allies);
                }
            });
        });
    }

    _drawMomentumBar() {
        if (this._momBarObjects) this._momBarObjects.forEach(o => o.destroy());
        this._momBarObjects = [];
        const m = this.momentum;
        const x = 440, y = 55, w = 400, h = 5;
        const bg = this.add.rectangle(x + w / 2, y, w, h, 0x222222).setDepth(100);
        const fillW = Math.max(0, (m.value / m.max) * w);
        const fill = this.add.rectangle(x + fillW / 2, y, fillW, h, m.getTierColor()).setDepth(101);
        this._momBarObjects.push(bg, fill);
        const label = m.getTierLabel();
        if (label) {
            const txt = this.add.text(x + w + 8, y, label, {
                fontSize: '10px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
            }).setOrigin(0, 0.5).setDepth(101);
            this._momBarObjects.push(txt);
        }
    }

    shutdown() {
        this.allies.forEach(a => a.destroy());
        this.enemies.forEach(e => e.destroy());
        if (this._momBarObjects) { this._momBarObjects.forEach(o => o.destroy()); this._momBarObjects = []; }
        if (this._dangerVignette) { this._dangerVignette.destroy(); this._dangerVignette = null; }
        if (this._consumableBarObjects) {
            this._consumableBarObjects.forEach(obj => obj.destroy());
            this._consumableBarObjects = [];
        }
    }
}
