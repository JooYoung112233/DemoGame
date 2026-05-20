class MapScene extends Phaser.Scene {
    constructor() {
        super('MapScene');
    }

    init(data) {
        this.act = data.act || 0;
        this.playerState = data.playerState || {
            hp: 80, maxHp: 80, block: 0, gold: 10,
            symbolPool: ['knight', 'knight', 'blackout', 'blackout', 'priest', 'jester', 'dagger', 'rage', 'festival', 'dead'],
            codex: {}, comboUpgrades: {}, relics: [], currentFloor: 0, visitedNodes: []
        };
        if (!this.playerState.relics) this.playerState.relics = [];
        if (!this.playerState.visitedNodes) this.playerState.visitedNodes = [];
        if (!this.playerState.codex) this.playerState.codex = {};
        if (!this.playerState.comboUpgrades) this.playerState.comboUpgrades = {};
        this.playerState.currentFloor = this.playerState.currentFloor || 0;

        if (data.map) {
            this.map = data.map;
        } else {
            this.map = MapGenerator.generate(this.act);
            this.playerState.currentFloor = 0;
            this.playerState.visitedNodes = [];
        }
    }

    create() {
        const W = 1280, H = 720;
        this.cameras.main.setBackgroundColor('#0a0a0a');

        const actCfg = MAP_CONFIG.acts[this.act];

        // Top bar
        this.add.graphics().fillStyle(0x111128, 1).fillRect(0, 0, W, 50);
        this.add.text(W / 2, 25, actCfg.name, {
            fontSize: '22px', fontFamily: 'monospace', color: '#ffcc00', fontStyle: 'bold'
        }).setOrigin(0.5);

        // Player info top-right
        this.add.text(W - 20, 14, `❤️ ${this.playerState.hp}/${this.playerState.maxHp}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ff6666'
        }).setOrigin(1, 0);
        this.add.text(W - 20, 32, `🪙 ${this.playerState.gold}`, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffcc00'
        }).setOrigin(1, 0);

        // Relics display (with stacking counts)
        if (this.playerState.relics.length > 0) {
            const relicCounts = {};
            for (const r of this.playerState.relics) {
                relicCounts[r] = (relicCounts[r] || 0) + 1;
            }
            const relicStr = Object.entries(relicCounts).map(([id, cnt]) => {
                const rd = RELIC_DATA[id];
                return rd ? (cnt > 1 ? `${rd.icon}×${cnt}` : rd.icon) : '?';
            }).join(' ');
            this.add.text(20, 14, relicStr, { fontSize: '18px' });
        }

        this._drawMap();
        this._drawDeckInfo();
    }

    _drawMap() {
        const W = 1280, H = 720;
        const mapW = W - 100;
        const mapX = 60;
        const centerY = (H - 50) / 2 + 50; // center of playable area
        const totalFloors = this.map.length;
        const colSpacing = mapW / (totalFloors + 1);

        // Calculate node positions (horizontal: floors go left→right)
        this.nodePositions = {};
        for (let f = 0; f < this.map.length; f++) {
            const row = this.map[f];
            const x = mapX + (f + 1) * colSpacing;
            const rowHeight = row.length * 90;
            const startY = centerY - rowHeight / 2 + 45;

            for (let c = 0; c < row.length; c++) {
                const y = startY + c * 90;
                this.nodePositions[row[c].id] = { x, y };
            }
        }

        // Draw connections (behind nodes)
        const gfx = this.add.graphics();
        for (let f = 0; f < this.map.length; f++) {
            for (const node of this.map[f]) {
                const from = this.nodePositions[node.id];
                for (const connId of node.connections) {
                    const to = this.nodePositions[connId];
                    if (!to) continue;
                    const isPath = this.playerState.visitedNodes.includes(node.id);
                    gfx.lineStyle(isPath ? 3 : 2, isPath ? 0x888888 : 0x333344, isPath ? 0.8 : 0.4);
                    // Draw curved bezier path for cleaner look
                    const midX = (from.x + to.x) / 2;
                    gfx.beginPath();
                    gfx.moveTo(from.x, from.y);
                    gfx.lineTo(to.x, to.y);
                    gfx.strokePath();
                }
            }
        }

        // Draw nodes
        for (let f = 0; f < this.map.length; f++) {
            for (const node of this.map[f]) {
                this._drawNode(node);
            }
        }
    }

    _drawNode(node) {
        const pos = this.nodePositions[node.id];
        if (!pos) return;
        const { x, y } = pos;

        const typeInfo = NODE_TYPES[node.type] || NODE_TYPES.battle;
        const visited = this.playerState.visitedNodes.includes(node.id);
        const clickable = this._isClickable(node.id);

        const bg = this.add.graphics();
        if (visited) {
            bg.fillStyle(0x222233, 0.5);
            bg.lineStyle(2, 0x444455, 0.5);
        } else if (clickable) {
            bg.fillStyle(typeInfo.color, 0.25);
            bg.lineStyle(3, typeInfo.color, 1);
        } else {
            bg.fillStyle(0x1a1a2a, 0.6);
            bg.lineStyle(2, 0x333355, 0.5);
        }
        bg.fillCircle(x, y, 26);
        bg.strokeCircle(x, y, 26);

        const icon = this.add.text(x, y - 2, node.type === 'start' ? '🎭' : typeInfo.icon, {
            fontSize: '20px'
        }).setOrigin(0.5).setAlpha(visited ? 0.3 : 1);

        if (!visited) {
            this.add.text(x, y + 30, node.type === 'start' ? '개막' : typeInfo.name, {
                fontSize: '9px', fontFamily: 'monospace',
                color: clickable ? '#ffffff' : '#666666'
            }).setOrigin(0.5);
        }

        if (clickable) {
            this.tweens.add({
                targets: bg, alpha: 0.7, duration: 600, yoyo: true, repeat: -1
            });

            const hitArea = this.add.circle(x, y, 28, 0x000000, 0)
                .setInteractive({ useHandCursor: true });

            hitArea.on('pointerover', () => {
                bg.clear();
                bg.fillStyle(typeInfo.color, 0.45);
                bg.lineStyle(3, 0xffffff, 1);
                bg.fillCircle(x, y, 28);
                bg.strokeCircle(x, y, 28);
            });
            hitArea.on('pointerout', () => {
                bg.clear();
                bg.fillStyle(typeInfo.color, 0.25);
                bg.lineStyle(3, typeInfo.color, 1);
                bg.fillCircle(x, y, 26);
                bg.strokeCircle(x, y, 26);
            });
            hitArea.on('pointerdown', () => this._selectNode(node));
        }
    }

    _isClickable(nodeId) {
        const currentFloor = this.playerState.currentFloor;
        if (currentFloor === 0 && this.playerState.visitedNodes.length === 0) {
            const startNode = this.map[0][0];
            return startNode.connections.includes(nodeId);
        }

        const lastVisited = this.playerState.visitedNodes[this.playerState.visitedNodes.length - 1];
        if (!lastVisited) {
            const startNode = this.map[0][0];
            return startNode.connections.includes(nodeId);
        }

        const lastNode = MapGenerator.findNode(this.map, lastVisited);
        if (!lastNode) return false;
        return lastNode.connections.includes(nodeId);
    }

    _selectNode(node) {
        if (this.playerState.visitedNodes.length === 0) {
            this.playerState.visitedNodes.push(this.map[0][0].id);
        }
        this.playerState.visitedNodes.push(node.id);
        this.playerState.currentFloor = node.floor;

        const passData = {
            act: this.act,
            map: this.map,
            playerState: this.playerState,
            nodeType: node.type,
        };

        switch (node.type) {
            case 'battle': this._startBattle(passData, 'battle'); break;
            case 'elite': this._startBattle(passData, 'elite'); break;
            case 'boss': this._startBossBattle(passData); break;
            case 'shop': this.scene.start('ShopScene', passData); break;
            case 'treasure': this.scene.start('TreasureScene', passData); break;
            case 'rest': this.scene.start('RestScene', passData); break;
            case 'event': this.scene.start('EventScene', passData); break;
        }
    }

    _startBattle(passData, encounterType) {
        const actId = this.act + 1;
        const encounters = ACT_ENCOUNTERS[actId];
        const pool = encounters[encounterType] || encounters.battle;
        const encounter = pool[Phaser.Math.Between(0, pool.length - 1)];
        passData.encounter = encounter;
        passData.encounterType = encounterType;
        this.scene.start('BattleScene', passData);
    }

    _startBossBattle(passData) {
        const actCfg = MAP_CONFIG.acts[this.act];
        passData.encounter = { pool: [actCfg.boss], count: 1 };
        passData.encounterType = 'boss';
        this.scene.start('BattleScene', passData);
    }

    _drawDeckInfo() {
        const W = 1280;
        this.add.graphics().fillStyle(0x111128, 1).fillRect(0, 680, W, 40);
        const icons = this.playerState.symbolPool.map(id => {
            const s = SYMBOL_DATA[id];
            return s ? s.icon : '?';
        }).join('');
        this.add.text(20, 692, `대본: ${icons}`, {
            fontSize: '13px', fontFamily: 'monospace', color: '#666666'
        }).setOrigin(0, 0.5);
    }
}
