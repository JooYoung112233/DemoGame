class BattleScene extends Phaser.Scene {
    constructor() { super('BattleScene'); }

    init(data) {
        this.partyIds = data.party || ['scout', 'fighter', 'medic', 'marksman'];
        this.enemyIds = data.enemies || ['zombie', 'zombie', 'zombie'];
        this.zoneId = data.zone || 'suburbs';
        this.returnData = data.returnData || null;
    }

    create() {
        const cam = this.cameras.main;
        const W = cam.width, H = cam.height;
        this.GRID_COLS = 12;
        this.GRID_ROWS = 8;
        const UI_PANEL_H = 110;
        const availH = H - UI_PANEL_H - 20;
        const availW = W - 40;
        this.CELL_SIZE = Math.floor(Math.min(availW / this.GRID_COLS, availH / this.GRID_ROWS));
        this.GRID_OFFSET_X = Math.floor((W - this.GRID_COLS * this.CELL_SIZE) / 2);
        this.GRID_OFFSET_Y = Math.floor((H - UI_PANEL_H - this.GRID_ROWS * this.CELL_SIZE) / 2);
        this.UI_PANEL_H = UI_PANEL_H;

        this.cameras.main.setBackgroundColor(0x0a0a1a);
        this.cameras.main.fadeIn(400);

        this.battleGrid = MapGenerator.generateBattleGrid(this.GRID_COLS, this.GRID_ROWS);
        this.turnManager = new TurnManager();

        this.gridLayer = this.add.container(0, 0);
        this.unitLayer = this.add.container(0, 0);
        this.overlayLayer = this.add.container(0, 0);
        this.uiContainer = this.add.container(0, 0).setDepth(200);

        this.drawGrid();
        this.spawnUnits();
        this.turnManager.init(this.playerUnits, this.enemyUnits);

        this.selectedUnit = null;
        this.selectedSkill = null;
        this.mode = 'select';
        this.moveHighlights = [];
        this.attackHighlights = [];
        this.busy = false;

        this.createBattleUI();
        this.highlightCurrentUnit();

        this.input.on('pointerdown', (pointer) => this.onGridClick(pointer));
    }

    gridToWorld(gx, gy) {
        return {
            x: this.GRID_OFFSET_X + gx * this.CELL_SIZE + this.CELL_SIZE / 2,
            y: this.GRID_OFFSET_Y + gy * this.CELL_SIZE + this.CELL_SIZE / 2
        };
    }

    worldToGrid(wx, wy) {
        return {
            x: Math.floor((wx - this.GRID_OFFSET_X) / this.CELL_SIZE),
            y: Math.floor((wy - this.GRID_OFFSET_Y) / this.CELL_SIZE)
        };
    }

    drawGrid() {
        const CS = this.CELL_SIZE;
        const OX = this.GRID_OFFSET_X;
        const OY = this.GRID_OFFSET_Y;

        for (let y = 0; y < this.GRID_ROWS; y++) {
            for (let x = 0; x < this.GRID_COLS; x++) {
                const g = this.add.graphics();
                const px = OX + x * CS;
                const py = OY + y * CS;

                if (this.battleGrid[y][x] === 1) {
                    g.fillStyle(0x3a3a4a, 1);
                    g.fillRect(px, py, CS, CS);
                    g.fillStyle(0x4a4a5a, 1);
                    g.fillRect(px + 4, py + 4, CS - 8, CS - 8);
                    g.lineStyle(1, 0x5a5a6a, 0.5);
                    g.strokeRect(px, py, CS, CS);
                } else {
                    const shade = ((x + y) % 2 === 0) ? 0x1e1e2e : 0x222236;
                    g.fillStyle(shade, 1);
                    g.fillRect(px, py, CS, CS);
                    g.lineStyle(1, 0x333348, 0.4);
                    g.strokeRect(px, py, CS, CS);
                }
                this.gridLayer.add(g);
            }
        }
    }

    spawnUnits() {
        this.playerUnits = [];
        this.enemyUnits = [];
        this.allUnits = [];

        this.partyIds.forEach((id, i) => {
            const data = PARTY_DATA[id];
            if (!data) return;
            let gx = 1;
            const gy = 1 + i * 2;
            if (gy >= this.GRID_ROWS) return;
            while (this.battleGrid[gy][gx] === 1 && gx < 3) gx++;
            const unit = new BattleUnit(this, data, gx, gy, true);
            this.playerUnits.push(unit);
            this.allUnits.push(unit);
        });

        this.enemyIds.forEach((id, i) => {
            const data = ENEMY_DATA[id];
            if (!data) return;
            let gx = this.GRID_COLS - 2;
            const gy = 1 + i * 2;
            if (gy >= this.GRID_ROWS) return;
            while (this.battleGrid[gy][gx] === 1 && gx > this.GRID_COLS - 4) gx--;
            const unit = new BattleUnit(this, data, gx, gy, false);
            this.enemyUnits.push(unit);
            this.allUnits.push(unit);
        });
    }

    createBattleUI() {
        const cam = this.cameras.main;
        const w = cam.width;
        const h = cam.height;
        const panelY = h - this.UI_PANEL_H;

        const panelBg = this.add.graphics();
        panelBg.fillStyle(0x111122, 0.95);
        panelBg.fillRect(0, panelY, w, this.UI_PANEL_H);
        panelBg.lineStyle(1, 0x333366, 0.6);
        panelBg.lineBetween(0, panelY, w, panelY);
        this.uiContainer.add(panelBg);

        this.turnText = this.add.text(20, panelY + 10, '', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffffff'
        });
        this.uiContainer.add(this.turnText);

        this.unitInfoText = this.add.text(20, panelY + 32, '', {
            fontSize: '12px', fontFamily: 'monospace', color: '#aaaaaa'
        });
        this.uiContainer.add(this.unitInfoText);

        this.skillButtons = [];
        this.actionHint = this.add.text(w / 2, panelY + 100, '', {
            fontSize: '12px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);
        this.uiContainer.add(this.actionHint);

        const endTurnBtn = this.add.text(w - 20, panelY + 15, '[ 턴 종료 ]', {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff8844',
            backgroundColor: '#2a1a0a', padding: { x: 12, y: 6 }
        }).setOrigin(1, 0).setInteractive({ useHandCursor: true });
        endTurnBtn.on('pointerdown', () => this.endCurrentTurn());
        endTurnBtn.on('pointerover', () => endTurnBtn.setColor('#ffaa66'));
        endTurnBtn.on('pointerout', () => endTurnBtn.setColor('#ff8844'));
        this.uiContainer.add(endTurnBtn);
    }

    highlightCurrentUnit() {
        const unit = this.turnManager.currentUnit;
        if (!unit || !unit.alive) return;

        this.allUnits.forEach(u => u.setHighlight(false));
        unit.setHighlight(true);

        this.updateInfoPanel(unit);

        if (unit.isPlayer) {
            this.selectedUnit = unit;
            this.mode = 'select';
            this.showMoveRange(unit);
            this.createSkillButtons(unit);
            this.actionHint.setText('이동할 칸을 클릭하거나 스킬을 선택하세요');
        } else {
            this.selectedUnit = null;
            this.actionHint.setText('적 턴...');
            this.clearSkillButtons();
            this.time.delayedCall(500, () => this.executeEnemyTurn(unit));
        }
    }

    updateInfoPanel(unit) {
        this.turnText.setText(
            `턴 ${this.turnManager.turnNumber}  |  ${unit.isPlayer ? '🔵 아군' : '🔴 적군'}: ${unit.data.name}`
        );
        const apPips = '■'.repeat(unit.ap) + '□'.repeat(unit.maxAp - unit.ap);
        this.unitInfoText.setText(
            `HP: ${unit.hp}/${unit.maxHp}  |  AP: ${apPips}  |  ATK: ${unit.data.atk}  DEF: ${unit.getEffectiveDef()}  RNG: ${unit.data.range}`
        );
    }

    createSkillButtons(unit) {
        this.clearSkillButtons();
        const cam = this.cameras.main;
        const panelY = cam.height - this.UI_PANEL_H;
        const btnFS = Math.floor(cam.height * 0.017);
        const descFS = Math.floor(cam.height * 0.013);
        const btnGap = Math.floor(cam.width * 0.012);
        let curX = Math.floor(cam.width * 0.22);

        const makeBtn = (label, color, bgColor, onClick) => {
            const btn = this.add.text(curX, panelY + 12, label, {
                fontSize: `${btnFS}px`, fontFamily: 'monospace', color,
                backgroundColor: bgColor, padding: { x: 10, y: 6 }
            }).setInteractive({ useHandCursor: true });
            btn.on('pointerdown', onClick);
            btn.on('pointerover', () => btn.setAlpha(0.8));
            btn.on('pointerout', () => btn.setAlpha(1));
            this.uiContainer.add(btn);
            this.skillButtons.push(btn);
            curX += btn.width + btnGap;
            return btn;
        };

        const unitMoveRange = unit.data.moveRange || 3;
        const moveRange = Math.min(unit.ap, unitMoveRange);
        if (moveRange > 0) {
            makeBtn(`🚶 이동 (${moveRange}칸)`, '#44aaff', '#0a1a2a', () => {
                this.mode = 'select';
                this.clearHighlights();
                this.showMoveRange(unit);
                this.actionHint.setText('이동할 칸을 클릭하세요');
            });
        }

        if (unit.ap >= 1) {
            makeBtn(`⚔ 공격 AP:1`, '#ff6666', '#2a0a0a', () => {
                this.mode = 'attack';
                this.selectedSkill = null;
                this.clearHighlights();
                this.showAttackRange(unit, unit.data.range);
                this.actionHint.setText('공격할 적을 클릭하세요');
            });
        }

        if (unit.data.skills) {
            unit.data.skills.forEach((skill) => {
                const canUse = unit.ap >= skill.apCost;
                if (canUse) {
                    const btn = makeBtn(`${skill.name} AP:${skill.apCost}`, '#44aaff', '#0a1a2a', () => {
                        this.selectedSkill = skill;
                        this.clearHighlights();
                        if (skill.type === 'heal' || skill.type === 'buff') {
                            this.mode = 'skill_ally';
                            this.showAllyRange(unit, skill.range || 3);
                            this.actionHint.setText('대상 아군을 클릭하세요');
                        } else {
                            this.mode = 'skill_attack';
                            this.showAttackRange(unit, skill.range || unit.data.range);
                            this.actionHint.setText('대상 적을 클릭하세요');
                        }
                    });
                    const desc = this.add.text(btn.x, panelY + 12 + btn.height + 4, skill.desc, {
                        fontSize: `${descFS}px`, fontFamily: 'monospace', color: '#666666'
                    });
                    this.uiContainer.add(desc);
                    this.skillButtons.push(desc);
                } else {
                    const btn = this.add.text(curX, panelY + 12, `${skill.name} AP:${skill.apCost}`, {
                        fontSize: `${btnFS}px`, fontFamily: 'monospace', color: '#555555',
                        backgroundColor: '#1a1a1a', padding: { x: 10, y: 6 }
                    });
                    this.uiContainer.add(btn);
                    this.skillButtons.push(btn);
                    curX += btn.width + btnGap;
                }
            });
        }
    }

    clearSkillButtons() {
        this.skillButtons.forEach(b => b.destroy());
        this.skillButtons = [];
    }

    showMoveRange(unit) {
        this.clearHighlights();
        const range = Math.min(unit.ap, unit.data.moveRange || 3);
        for (let dy = -range; dy <= range; dy++) {
            for (let dx = -range; dx <= range; dx++) {
                if (Math.abs(dx) + Math.abs(dy) > range || (dx === 0 && dy === 0)) continue;
                const nx = unit.gridX + dx;
                const ny = unit.gridY + dy;
                if (nx < 0 || ny < 0 || nx >= this.GRID_COLS || ny >= this.GRID_ROWS) continue;
                if (this.battleGrid[ny][nx] === 1) continue;
                if (this.getUnitAt(nx, ny)) continue;

                const g = this.add.graphics();
                const pos = this.gridToWorld(nx, ny);
                g.fillStyle(0x4488ff, 0.15);
                g.fillRect(pos.x - this.CELL_SIZE / 2, pos.y - this.CELL_SIZE / 2, this.CELL_SIZE, this.CELL_SIZE);
                g.lineStyle(1, 0x4488ff, 0.4);
                g.strokeRect(pos.x - this.CELL_SIZE / 2, pos.y - this.CELL_SIZE / 2, this.CELL_SIZE, this.CELL_SIZE);
                g.setDepth(1);
                this.moveHighlights.push(g);
            }
        }
    }

    showAttackRange(unit, range) {
        for (let dy = -range; dy <= range; dy++) {
            for (let dx = -range; dx <= range; dx++) {
                if (Math.abs(dx) + Math.abs(dy) > range || (dx === 0 && dy === 0)) continue;
                const nx = unit.gridX + dx;
                const ny = unit.gridY + dy;
                if (nx < 0 || ny < 0 || nx >= this.GRID_COLS || ny >= this.GRID_ROWS) continue;

                const target = this.getUnitAt(nx, ny);
                if (target && !target.isPlayer && target.alive) {
                    const g = this.add.graphics();
                    const pos = this.gridToWorld(nx, ny);
                    g.fillStyle(0xff4444, 0.2);
                    g.fillRect(pos.x - this.CELL_SIZE / 2, pos.y - this.CELL_SIZE / 2, this.CELL_SIZE, this.CELL_SIZE);
                    g.lineStyle(2, 0xff4444, 0.6);
                    g.strokeRect(pos.x - this.CELL_SIZE / 2, pos.y - this.CELL_SIZE / 2, this.CELL_SIZE, this.CELL_SIZE);
                    g.setDepth(1);
                    this.attackHighlights.push(g);
                }
            }
        }
    }

    showAllyRange(unit, range) {
        this.playerUnits.forEach(ally => {
            if (!ally.alive) return;
            const dist = Math.abs(unit.gridX - ally.gridX) + Math.abs(unit.gridY - ally.gridY);
            if (dist <= range) {
                const g = this.add.graphics();
                const pos = this.gridToWorld(ally.gridX, ally.gridY);
                g.fillStyle(0x44ff44, 0.2);
                g.fillRect(pos.x - this.CELL_SIZE / 2, pos.y - this.CELL_SIZE / 2, this.CELL_SIZE, this.CELL_SIZE);
                g.lineStyle(2, 0x44ff44, 0.6);
                g.strokeRect(pos.x - this.CELL_SIZE / 2, pos.y - this.CELL_SIZE / 2, this.CELL_SIZE, this.CELL_SIZE);
                g.setDepth(1);
                this.attackHighlights.push(g);
            }
        });
    }

    clearHighlights() {
        this.moveHighlights.forEach(g => g.destroy());
        this.moveHighlights = [];
        this.attackHighlights.forEach(g => g.destroy());
        this.attackHighlights = [];
    }

    getUnitAt(gx, gy) {
        return this.allUnits.find(u => u.alive && u.gridX === gx && u.gridY === gy);
    }

    onGridClick(pointer) {
        if (this.busy) return;
        const unit = this.turnManager.currentUnit;
        if (!unit || !unit.isPlayer) return;

        const { x: gx, y: gy } = this.worldToGrid(pointer.x, pointer.y);
        if (gx < 0 || gy < 0 || gx >= this.GRID_COLS || gy >= this.GRID_ROWS) return;

        if (this.mode === 'select') {
            const clickedUnit = this.getUnitAt(gx, gy);
            if (clickedUnit && clickedUnit.isPlayer && clickedUnit.alive) {
                return;
            }

            if (this.isInMoveRange(unit, gx, gy) && unit.ap >= 1) {
                this.moveUnit(unit, gx, gy);
            }
        } else if (this.mode === 'attack') {
            this.doBasicAttack(unit, gx, gy);
        } else if (this.mode === 'skill_attack') {
            this.doSkillAttack(unit, gx, gy);
        } else if (this.mode === 'skill_ally') {
            this.doSkillAlly(unit, gx, gy);
        }
    }

    isInMoveRange(unit, gx, gy) {
        const range = Math.min(unit.ap, unit.data.moveRange || 3);
        const dist = Math.abs(unit.gridX - gx) + Math.abs(unit.gridY - gy);
        return dist <= range && dist > 0 &&
               this.battleGrid[gy][gx] !== 1 &&
               !this.getUnitAt(gx, gy);
    }

    moveUnit(unit, gx, gy) {
        this.busy = true;
        const dist = Math.abs(unit.gridX - gx) + Math.abs(unit.gridY - gy);
        unit.useAP(dist);
        this.clearHighlights();

        unit.animateMoveTo(gx, gy, () => {
            this.busy = false;
            this.continueOrEndTurn(unit);
        });
    }

    continueOrEndTurn(unit) {
        if (unit.ap > 0 && unit.alive) {
            this.mode = 'select';
            this.clearHighlights();
            this.showMoveRange(unit);
            this.createSkillButtons(unit);
            this.updateInfoPanel(unit);
            this.actionHint.setText('이동할 칸을 클릭하거나 스킬을 선택하세요');
        } else {
            this.nextTurn();
        }
    }

    doBasicAttack(unit, gx, gy) {
        const target = this.getUnitAt(gx, gy);
        if (!target || target.isPlayer || !target.alive) return;
        const dist = Math.abs(unit.gridX - gx) + Math.abs(unit.gridY - gy);
        if (dist > unit.data.range || unit.ap < 1) return;

        this.busy = true;
        unit.useAP(1);
        this.clearHighlights();

        target.takeDamage(unit.data.atk);
        this.updateInfoPanel(unit);

        this.time.delayedCall(300, () => {
            this.busy = false;
            if (this.checkBattleEnd()) return;
            this.continueOrEndTurn(unit);
        });
    }

    doSkillAttack(unit, gx, gy) {
        const skill = this.selectedSkill;
        if (!skill) return;
        const target = this.getUnitAt(gx, gy);
        if (!target || target.isPlayer || !target.alive) return;
        const dist = Math.abs(unit.gridX - gx) + Math.abs(unit.gridY - gy);
        if (dist > (skill.range || unit.data.range) || unit.ap < skill.apCost) return;

        this.busy = true;
        unit.useAP(skill.apCost);
        this.clearHighlights();

        target.takeDamage(skill.damage || unit.data.atk);
        DamagePopup.showText(this, target.container.x, target.container.y - 35, skill.name, '#44aaff');
        this.updateInfoPanel(unit);
        this.selectedSkill = null;

        this.time.delayedCall(400, () => {
            this.busy = false;
            if (this.checkBattleEnd()) return;
            this.continueOrEndTurn(unit);
        });
    }

    doSkillAlly(unit, gx, gy) {
        const skill = this.selectedSkill;
        if (!skill) return;
        const target = this.getUnitAt(gx, gy);
        if (!target || !target.isPlayer || !target.alive) return;
        const dist = Math.abs(unit.gridX - gx) + Math.abs(unit.gridY - gy);
        if (dist > (skill.range || 3) || unit.ap < skill.apCost) return;

        this.busy = true;
        unit.useAP(skill.apCost);
        this.clearHighlights();

        if (skill.type === 'heal') {
            target.heal(skill.value);
            DamagePopup.showText(this, target.container.x, target.container.y - 35, skill.name, '#44ff44');
        } else if (skill.type === 'buff') {
            target.addBuff({ effect: skill.effect, value: skill.value, duration: skill.duration || 1 });
            DamagePopup.showText(this, target.container.x, target.container.y - 35, skill.name, '#ffcc44');
            if (skill.effect === 'apUp') {
                target.ap += skill.value;
                target.drawApPips();
            }
        }

        this.updateInfoPanel(unit);
        this.selectedSkill = null;

        this.time.delayedCall(400, () => {
            this.busy = false;
            this.continueOrEndTurn(unit);
        });
    }

    endCurrentTurn() {
        if (this.busy) return;
        const unit = this.turnManager.currentUnit;
        if (!unit || !unit.isPlayer) return;
        this.nextTurn();
    }

    nextTurn() {
        this.clearHighlights();
        this.clearSkillButtons();
        this.allUnits.forEach(u => u.setHighlight(false));

        const next = this.turnManager.nextUnit();
        if (!next) return;

        const result = this.turnManager.isBattleOver();
        if (result) {
            this.endBattle(result);
            return;
        }

        this.time.delayedCall(200, () => this.highlightCurrentUnit());
    }

    executeEnemyTurn(unit) {
        if (!unit || !unit.alive || this.busy) {
            this.nextTurn();
            return;
        }

        this.busy = true;
        AISystem.executeEnemyTurn(unit, this.turnManager, this.battleGrid, (action, x, y, cb) => {
            if (action === 'move') {
                unit.useAP(1);
                unit.animateMoveTo(x, y, () => {
                    this.updateInfoPanel(unit);
                    if (cb) cb();
                    else {
                        this.busy = false;
                        this.time.delayedCall(300, () => this.nextTurn());
                    }
                });
            } else if (action === 'attack') {
                const target = this.getUnitAt(x, y);
                if (target) {
                    unit.useAP(1);
                    target.takeDamage(unit.data.atk);
                    this.updateInfoPanel(unit);
                }
                this.busy = false;
                this.time.delayedCall(400, () => {
                    this.checkBattleEnd();
                    this.nextTurn();
                });
            } else {
                this.busy = false;
                this.nextTurn();
            }
        });
    }

    checkBattleEnd() {
        const result = this.turnManager.isBattleOver();
        if (result) {
            this.time.delayedCall(500, () => this.endBattle(result));
            return true;
        }
        return false;
    }

    showDamage(x, y, value) {
        DamagePopup.show(this, x, y, value);
    }

    showHeal(x, y, value) {
        DamagePopup.showHeal(this, x, y, value);
    }

    endBattle(result) {
        this.clearHighlights();
        this.clearSkillButtons();

        const overlay = this.add.graphics().setDepth(300);
        overlay.fillStyle(0x000000, 0.7);
        overlay.fillRect(0, 0, this.cameras.main.width, this.cameras.main.height);

        const cam = this.cameras.main;
        const isVictory = result === 'victory';

        const title = this.add.text(cam.width / 2, cam.height * 0.35,
            isVictory ? '⚔ 승리!' : '💀 패배...', {
            fontSize: `${Math.floor(cam.height * 0.07)}px`, fontFamily: 'monospace',
            color: isVictory ? '#44ff88' : '#ff4444',
            stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(301);

        let lootText = '';
        if (isVictory) {
            const loot = [];
            this.enemyIds.forEach(id => {
                const data = ENEMY_DATA[id];
                if (data && data.loot) {
                    const item = data.loot[Math.floor(Math.random() * data.loot.length)];
                    if (Math.random() < 0.5) loot.push(item);
                }
            });
            if (loot.length > 0) {
                lootText = '전리품: ' + loot.map(id => ITEM_DATA[id]?.icon + ITEM_DATA[id]?.name || id).join(', ');
                if (this.returnData) {
                    this.returnData.inventory.push(...loot);
                }
            }
        }

        const info = this.add.text(cam.width / 2, cam.height * 0.5, lootText, {
            fontSize: `${Math.floor(cam.height * 0.025)}px`, fontFamily: 'monospace', color: '#ffffff'
        }).setOrigin(0.5).setDepth(301);

        const continueBtn = this.add.text(cam.width / 2, cam.height * 0.62, '[ 계속 ]', {
            fontSize: `${Math.floor(cam.height * 0.03)}px`, fontFamily: 'monospace', color: '#44aaff',
            backgroundColor: '#111a2a', padding: { x: 20, y: 10 }
        }).setOrigin(0.5).setDepth(301).setInteractive({ useHandCursor: true });

        continueBtn.on('pointerdown', () => {
            let nextScene, nextData;
            if (this.returnData && isVictory) {
                nextScene = 'ExpeditionScene';
                nextData = {
                    zone: this.returnData.zone,
                    party: this.returnData.party,
                    inventory: this.returnData.inventory,
                    stash: this.returnData.stash
                };
            } else {
                nextScene = 'SafeHouseScene';
                nextData = {
                    inventory: [],
                    stash: this.returnData?.stash || [],
                    party: this.partyIds,
                    safe: false
                };
            }
            this.cameras.main.fadeOut(400);
            this.time.delayedCall(450, () => {
                game.scene.stop('BattleScene');
                game.scene.start(nextScene, nextData);
            });
        });
    }
}
