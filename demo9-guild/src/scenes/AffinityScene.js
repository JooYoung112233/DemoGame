class AffinityScene extends Phaser.Scene {
    constructor() { super('AffinityScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.mercId = data.mercId || null;
        this.selectedZone = data.selectedZone || 'bloodpit';
    }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
        this._drawUI();
    }

    _drawUI() {
        const gs = this.gameState;
        const merc = gs.roster.find(m => m.id === this.mercId);

        this.add.text(640, 20, '구역 친화도 트리', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        UIButton.create(this, 80, 20, 100, 30, '← 로스터', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start('RosterScene', { gameState: gs })
        });

        if (!merc) {
            this.add.text(640, 360, '용병을 선택하세요', {
                fontSize: '14px', fontFamily: 'monospace', color: '#555566'
            }).setOrigin(0.5);
            return;
        }

        const base = merc.getBaseClass();
        const rarity = RARITY_DATA[merc.rarity];
        this.add.text(640, 48, `${base.icon} ${merc.name} [${rarity.name} ${base.name}] Lv.${merc.level}`, {
            fontSize: '13px', fontFamily: 'monospace', color: rarity.textColor
        }).setOrigin(0.5);

        this._drawZoneTabs(merc);
        this._drawTree(merc);
        this._drawMercAffinityInfo(merc);
    }

    _drawZoneTabs(merc) {
        let tabX = 200;
        ZONE_KEYS.forEach(key => {
            if (this.gameState.zoneLevel[key] === 0) return;
            const zone = ZONE_DATA[key];
            const tree = AFFINITY_TREES[key];
            const isSelected = this.selectedZone === key;
            const lvl = merc.affinityLevel?.[key] || 0;
            const pts = merc.affinityPoints?.[key] || 0;

            const label = `${zone.icon} ${zone.name} Lv.${lvl}${pts > 0 ? ` (${pts}P)` : ''}`;

            UIButton.create(this, tabX, 78, 180, 28, label, {
                color: isSelected ? 0x334466 : 0x222233,
                hoverColor: 0x445577,
                textColor: isSelected ? zone.textColor : '#888899',
                fontSize: 11,
                onClick: () => {
                    this.selectedZone = key;
                    this.scene.restart({ gameState: this.gameState, mercId: this.mercId, selectedZone: key });
                }
            });
            tabX += 200;
        });
    }

    _drawTree(merc) {
        const tree = AFFINITY_TREES[this.selectedZone];
        if (!tree) return;

        const nodes = tree.nodes;
        const centerX = 640;
        const startY = 140;
        const stepY = 110;
        const stepX = 160;

        Object.entries(nodes).forEach(([nodeId, node]) => {
            node.requires.forEach(reqId => {
                const req = nodes[reqId];
                if (!req) return;
                const fromX = centerX + req.x * stepX;
                const fromY = startY + req.y * stepY + 30;
                const toX = centerX + node.x * stepX;
                const toY = startY + node.y * stepY - 10;

                const line = this.add.graphics();
                const bothUnlocked = merc.hasAffinityNode(nodeId) && merc.hasAffinityNode(reqId);
                const reqUnlocked = merc.hasAffinityNode(reqId);
                line.lineStyle(2, bothUnlocked ? 0x44ff88 : reqUnlocked ? 0x446688 : 0x333344, bothUnlocked ? 0.8 : 0.4);
                line.lineBetween(fromX, fromY, toX, toY);
            });
        });

        Object.entries(nodes).forEach(([nodeId, node]) => {
            const nx = centerX + node.x * stepX;
            const ny = startY + node.y * stepY;
            this._drawNode(merc, nodeId, node, nx, ny, tree);
        });
    }

    _drawNode(merc, nodeId, node, x, y, tree) {
        const isUnlocked = merc.hasAffinityNode(nodeId);
        const canUnlock = merc.canUnlockAffinityNode(this.selectedZone, nodeId);
        const levelReached = (merc.affinityLevel?.[this.selectedZone] || 0) >= node.level;

        const w = 140;
        const h = 60;

        const bg = this.add.graphics();
        if (isUnlocked) {
            bg.fillStyle(0x1a3a1a, 1);
            bg.fillRoundedRect(x - w / 2, y - h / 2, w, h, 5);
            bg.lineStyle(2, 0x44ff88, 0.8);
            bg.strokeRoundedRect(x - w / 2, y - h / 2, w, h, 5);
        } else if (canUnlock) {
            bg.fillStyle(0x2a2a1a, 1);
            bg.fillRoundedRect(x - w / 2, y - h / 2, w, h, 5);
            bg.lineStyle(2, 0xffaa44, 0.8);
            bg.strokeRoundedRect(x - w / 2, y - h / 2, w, h, 5);
        } else {
            bg.fillStyle(levelReached ? 0x1a1a2e : 0x141418, 1);
            bg.fillRoundedRect(x - w / 2, y - h / 2, w, h, 5);
            bg.lineStyle(1, levelReached ? 0x444466 : 0x222233, 0.5);
            bg.strokeRoundedRect(x - w / 2, y - h / 2, w, h, 5);
        }

        const nameColor = isUnlocked ? '#44ff88' : canUnlock ? '#ffaa44' : levelReached ? '#aaaacc' : '#555566';
        this.add.text(x, y - 12, node.name, {
            fontSize: '11px', fontFamily: 'monospace', color: nameColor, fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(x, y + 6, node.desc, {
            fontSize: '8px', fontFamily: 'monospace', color: isUnlocked ? '#88cc88' : '#777788',
            wordWrap: { width: w - 10 }, align: 'center'
        }).setOrigin(0.5);

        this.add.text(x, y + h / 2 - 6, `Lv.${node.level}`, {
            fontSize: '8px', fontFamily: 'monospace', color: '#666677'
        }).setOrigin(0.5);

        if (canUnlock || isUnlocked) {
            const hitZone = this.add.zone(x, y, w, h).setInteractive({ useHandCursor: canUnlock });
            if (canUnlock) {
                hitZone.on('pointerdown', () => {
                    merc.unlockAffinityNode(this.selectedZone, nodeId);
                    SaveManager.save(this.gameState);
                    this.scene.restart({ gameState: this.gameState, mercId: this.mercId, selectedZone: this.selectedZone });
                });
            }
            hitZone.on('pointerover', () => {
                if (canUnlock) {
                    bg.clear();
                    bg.fillStyle(0x3a3a1a, 1);
                    bg.fillRoundedRect(x - w / 2, y - h / 2, w, h, 5);
                    bg.lineStyle(2, 0xffcc44, 1);
                    bg.strokeRoundedRect(x - w / 2, y - h / 2, w, h, 5);
                }
            });
            hitZone.on('pointerout', () => {
                bg.clear();
                if (isUnlocked) {
                    bg.fillStyle(0x1a3a1a, 1);
                    bg.fillRoundedRect(x - w / 2, y - h / 2, w, h, 5);
                    bg.lineStyle(2, 0x44ff88, 0.8);
                    bg.strokeRoundedRect(x - w / 2, y - h / 2, w, h, 5);
                } else if (canUnlock) {
                    bg.fillStyle(0x2a2a1a, 1);
                    bg.fillRoundedRect(x - w / 2, y - h / 2, w, h, 5);
                    bg.lineStyle(2, 0xffaa44, 0.8);
                    bg.strokeRoundedRect(x - w / 2, y - h / 2, w, h, 5);
                }
            });
        }
    }

    _drawMercAffinityInfo(merc) {
        const zoneKey = this.selectedZone;
        const zone = ZONE_DATA[zoneKey];
        const lvl = merc.affinityLevel?.[zoneKey] || 0;
        const xp = merc.affinityXp?.[zoneKey] || 0;
        const pts = merc.affinityPoints?.[zoneKey] || 0;
        const needed = lvl >= 5 ? 'MAX' : getAffinityXpNeeded(lvl);

        const panelX = 20;
        const panelY = 600;
        UIPanel.create(this, panelX, panelY, 500, 100, { fillColor: 0x151525, strokeColor: 0x333355 });

        this.add.text(panelX + 15, panelY + 12, `${zone.icon} ${zone.name} 친화도`, {
            fontSize: '14px', fontFamily: 'monospace', color: zone.textColor, fontStyle: 'bold'
        });

        this.add.text(panelX + 15, panelY + 35, `레벨: ${lvl}/5   XP: ${lvl >= 5 ? 'MAX' : `${xp}/${needed}`}   포인트: ${pts}`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#aaaacc'
        });

        if (lvl < 5) {
            const barX = panelX + 15;
            const barY = panelY + 58;
            const barW = 250;
            const ratio = xp / getAffinityXpNeeded(lvl);
            const bar = this.add.graphics();
            bar.fillStyle(0x222244, 1);
            bar.fillRoundedRect(barX, barY, barW, 10, 3);
            bar.fillStyle(Phaser.Display.Color.HexStringToColor(zone.textColor).color, 1);
            bar.fillRoundedRect(barX, barY, barW * ratio, 10, 3);
            bar.lineStyle(1, 0x446688, 0.5);
            bar.strokeRoundedRect(barX, barY, barW, 10, 3);
        }

        const unlockedNodes = (merc.affinityNodes || []).filter(id => {
            const tree = AFFINITY_TREES[zoneKey];
            return tree && tree.nodes[id];
        });
        if (unlockedNodes.length > 0) {
            this.add.text(panelX + 15, panelY + 76, `해금: ${unlockedNodes.map(id => AFFINITY_TREES[zoneKey].nodes[id].name).join(', ')}`, {
                fontSize: '9px', fontFamily: 'monospace', color: '#44ff88',
                wordWrap: { width: 470 }
            });
        }

        if (pts > 0) {
            this.add.text(panelX + 300, panelY + 35, '← 노드를 클릭하여 해금', {
                fontSize: '11px', fontFamily: 'monospace', color: '#ffaa44'
            });
        }

        const otherMercs = this.gameState.roster.filter(m => m.alive && m.id !== this.mercId);
        if (otherMercs.length > 0) {
            const selectPanel = UIPanel.create(this, 540, panelY, 720, 100, { fillColor: 0x151525, strokeColor: 0x333355 });
            this.add.text(555, panelY + 8, '다른 용병:', {
                fontSize: '11px', fontFamily: 'monospace', color: '#888899'
            });
            let mx = 555;
            otherMercs.forEach(m => {
                if (mx > 1200) return;
                const b = m.getBaseClass();
                const r = RARITY_DATA[m.rarity];
                const aLv = m.affinityLevel?.[zoneKey] || 0;
                const aPts = m.affinityPoints?.[zoneKey] || 0;
                const label = `${b.icon}${m.name} Lv.${aLv}${aPts > 0 ? `(${aPts}P)` : ''}`;

                UIButton.create(this, mx + 70, panelY + 50, 140, 28, label, {
                    color: 0x222233, hoverColor: 0x334455, textColor: r.textColor, fontSize: 10,
                    onClick: () => {
                        this.scene.restart({ gameState: this.gameState, mercId: m.id, selectedZone: this.selectedZone });
                    }
                });
                mx += 155;
            });
        }
    }
}
