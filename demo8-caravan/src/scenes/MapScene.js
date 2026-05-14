class MapScene extends Phaser.Scene {
    constructor() {
        super({ key: 'MapScene' });
    }

    init(data) {
        this.runState = data.runState;
        this.mapManager = new MapManager();
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');
        this.drawBackground();
        this.drawRoad();
        this.drawNodes();
        this.drawCaravan();
        this.drawUI();
        this.drawPartyHUD();

        const node = this.mapManager.getNode(this.runState.currentNode);
        if (node) {
            this.time.delayedCall(800, () => this.goToNode(this.runState.currentNode));
        }
    }

    drawBackground() {
        const g = this.add.graphics();
        const progress = this.runState.currentNode / (this.mapManager.getNodeCount() - 1);

        for (let i = 0; i < 500; i++) {
            const ratio = i / 500;
            const r = Math.floor(15 + ratio * 20 + progress * 30);
            const gr = Math.floor(20 + ratio * 25 - progress * 15);
            const b = Math.floor(40 + ratio * 30 - progress * 20);
            g.fillStyle(Phaser.Display.Color.GetColor(
                Math.min(255, Math.max(0, r)),
                Math.min(255, Math.max(0, gr)),
                Math.min(255, Math.max(0, b))
            ));
            g.fillRect(0, i, 1280, 1);
        }

        g.fillStyle(Phaser.Display.Color.GetColor(
            Math.floor(30 + progress * 20),
            Math.floor(45 - progress * 20),
            Math.floor(30 - progress * 10)
        ));
        g.fillRect(0, 500, 1280, 220);
    }

    drawRoad() {
        const g = this.add.graphics();
        const nodeCount = this.mapManager.getNodeCount();
        const startX = 80;
        const endX = 1200;

        g.lineStyle(6, 0x664422, 0.8);
        g.lineBetween(startX, 350, endX, 350);
        g.lineStyle(2, 0x553311, 0.5);
        g.lineBetween(startX, 347, endX, 347);
        g.lineBetween(startX, 353, endX, 353);

        for (let i = 0; i < 30; i++) {
            const x = startX + Math.random() * (endX - startX);
            g.fillStyle(0x553311, 0.3);
            g.fillCircle(x, 350 + (Math.random() - 0.5) * 6, 1 + Math.random() * 2);
        }
    }

    drawNodes() {
        const nodeCount = this.mapManager.getNodeCount();
        const startX = 80;
        const endX = 1200;
        this.nodePositions = [];

        for (let i = 0; i < nodeCount; i++) {
            const node = this.mapManager.getNode(i);
            const x = startX + (endX - startX) * (i / (nodeCount - 1));
            const y = 350;
            this.nodePositions.push({ x, y });

            const isCompleted = i < this.runState.currentNode;
            const isCurrent = i === this.runState.currentNode;

            let color = 0x444466;
            if (node.type === 'village') color = 0x4488ff;
            else if (node.type === 'bandit') color = 0xff4444;
            else if (node.type === 'event') color = 0x44ff88;
            else if (node.type === 'boss') color = 0xaa44ff;

            if (isCompleted) color = Phaser.Display.Color.GetColor(
                Math.floor(((color >> 16) & 0xff) * 0.3),
                Math.floor(((color >> 8) & 0xff) * 0.3),
                Math.floor((color & 0xff) * 0.3)
            );

            const nodeGfx = this.add.graphics();
            if (isCurrent) {
                nodeGfx.fillStyle(color, 0.3);
                nodeGfx.fillCircle(x, y, 22);
            }
            nodeGfx.fillStyle(color);
            nodeGfx.fillCircle(x, y, 14);
            nodeGfx.fillStyle(0xffffff, 0.2);
            nodeGfx.fillCircle(x - 3, y - 3, 5);

            if (isCurrent) {
                this.tweens.add({
                    targets: nodeGfx, alpha: 0.6,
                    duration: 600, yoyo: true, repeat: -1
                });
            }

            let icon = '?';
            if (node.type === 'village') icon = '🏘️';
            else if (node.type === 'bandit') icon = '⚔️';
            else if (node.type === 'event') icon = '❓';
            else if (node.type === 'boss') icon = '💀';

            this.add.text(x, y - 32, icon, {
                fontSize: '16px'
            }).setOrigin(0.5).setDepth(10);

            this.add.text(x, y + 24, node.name, {
                fontSize: '9px', fontFamily: 'monospace',
                color: isCompleted ? '#444444' : '#aaaaaa',
                stroke: '#000000', strokeThickness: 1
            }).setOrigin(0.5).setDepth(10);

            this.add.text(x, y, String(i + 1), {
                fontSize: '10px', fontFamily: 'monospace', fontStyle: 'bold',
                color: '#ffffff', stroke: '#000000', strokeThickness: 2
            }).setOrigin(0.5).setDepth(10);
        }
    }

    drawCaravan() {
        if (!this.nodePositions || this.nodePositions.length === 0) return;
        const pos = this.nodePositions[Math.min(this.runState.currentNode, this.nodePositions.length - 1)];

        this.caravanContainer = this.add.container(pos.x - 40, pos.y - 40);
        this.caravanContainer.setDepth(15);

        const g = this.add.graphics();
        g.fillStyle(0x8B6914);
        g.fillRect(-12, -8, 24, 12);
        g.fillStyle(0xC4A035);
        g.fillRect(-10, -16, 20, 9);
        g.fillStyle(0x333333);
        g.fillCircle(-7, 6, 4);
        g.fillCircle(7, 6, 4);
        g.fillStyle(0x666666);
        g.fillCircle(-7, 6, 2);
        g.fillCircle(7, 6, 2);

        this.caravanContainer.add(g);
    }

    drawUI() {
        this.add.text(640, 30, `${this.mapManager.getNode(this.runState.currentNode).name}`, {
            fontSize: '22px', fontFamily: 'monospace', fontStyle: 'bold',
            color: '#ffffff', stroke: '#000000', strokeThickness: 3
        }).setOrigin(0.5).setDepth(20);

        this.add.text(30, 30, `🪙 ${this.runState.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace',
            color: '#ffcc44', stroke: '#000000', strokeThickness: 2
        }).setDepth(20);

        this.add.text(1250, 30, `파티: ${this.runState.party.length}/${this.runState.maxPartySize}`, {
            fontSize: '14px', fontFamily: 'monospace',
            color: '#44ff88', stroke: '#000000', strokeThickness: 2
        }).setOrigin(1, 0).setDepth(20);

        const nodeIndex = this.runState.currentNode;
        const total = this.mapManager.getNodeCount();
        this.add.text(640, 60, `여정: ${nodeIndex + 1} / ${total}`, {
            fontSize: '12px', fontFamily: 'monospace',
            color: '#888888'
        }).setOrigin(0.5).setDepth(20);
    }

    drawPartyHUD() {
        PartyHUD.draw(this, this.runState, 620);
    }

    goToNode(nodeIndex) {
        const node = this.mapManager.getNode(nodeIndex);
        if (!node) return;

        if (node.type === 'village') {
            this.scene.start('VillageScene', { runState: this.runState });
        } else if (node.type === 'bandit' || node.type === 'boss') {
            this.scene.start('BattleScene', { runState: this.runState });
        } else if (node.type === 'event') {
            this.scene.start('EventScene', { runState: this.runState });
        }
    }
}
