class BOMapScene extends Phaser.Scene {
    constructor() { super('BOMapScene'); }

    init(data) {
        this.party = data.party || ['scout', 'breacher', 'hacker', 'medic'];
        this.mapSystem = data.mapSystem || new MapSystem();
        this.weightSystem = data.weightSystem || new WeightSystem();
        this.tensionSystem = data.tensionSystem || new TensionSystem();
    }

    create() {
        this.cameras.main.setBackgroundColor('#0a0a1a');
        const cx = 640;

        this.add.text(cx, 25, '🔦 BLACK OUT — 시설 탐색', {
            fontSize: '20px', fontFamily: 'monospace', color: '#8844ff', fontStyle: 'bold'
        }).setOrigin(0.5);

        const tensionName = this.tensionSystem.getTensionName();
        const tensionColor = this.tensionSystem.getTensionColor();
        this.add.text(cx, 50, `긴장도: ${tensionName} (${this.tensionSystem.tension}/${this.tensionSystem.maxTension})  |  이동: ${this.tensionSystem.roomsMoved}칸`, {
            fontSize: '12px', fontFamily: 'monospace', color: tensionColor
        }).setOrigin(0.5);

        const weight = this.weightSystem.getCurrentWeight();
        const maxW = this.weightSystem.maxCapacity;
        const weightRatio = this.weightSystem.getWeightRatio();
        const weightColor = weightRatio <= 0.3 ? '#44ff88' : weightRatio <= 0.6 ? '#ffcc44' : weightRatio <= 0.8 ? '#ff8844' : '#ff4444';
        this.add.text(cx, 70, `무게: ${weight}/${maxW}  |  도주 확률: ${Math.floor(this.weightSystem.getFleeChance() * 100)}%  |  가치: ${this.weightSystem.getTotalValue()}`, {
            fontSize: '12px', fontFamily: 'monospace', color: weightColor
        }).setOrigin(0.5);

        this.drawMap();
        this.drawInventory();

        if (this.tensionSystem.shouldShuffleExit()) {
            this.mapSystem.shuffleExit();
            this.add.text(cx, 690, '⚠️ 출구 위치가 변경되었습니다!', {
                fontSize: '14px', fontFamily: 'monospace', color: '#ff4444', fontStyle: 'bold'
            }).setOrigin(0.5);
        }
    }

    drawMap() {
        const cellSize = 80;
        const gap = 6;
        const gridPx = 5 * cellSize + 4 * gap;
        const startX = 640 - gridPx / 2 + cellSize / 2;
        const startY = 120 + cellSize / 2;

        for (let y = 0; y < 5; y++) {
            for (let x = 0; x < 5; x++) {
                const px = startX + x * (cellSize + gap);
                const py = startY + y * (cellSize + gap);
                const room = this.mapSystem.grid[y][x];
                const revealed = this.mapSystem.revealed[y][x];
                const isPlayer = this.mapSystem.playerPos.x === x && this.mapSystem.playerPos.y === y;
                const adjacent = this.mapSystem.isAdjacent(x, y);

                let bgColor = 0x111122;
                let borderColor = 0x222244;
                let alpha = 0.3;
                let label = '?';
                let labelColor = '#334455';

                if (revealed || room.visited) {
                    alpha = room.visited ? 0.9 : 0.6;
                    const roomInfo = BO_ROOM_TYPES[room.type] || BO_ROOM_TYPES.empty;
                    if (room.type !== 'unknown') {
                        label = roomInfo.icon;
                        labelColor = roomInfo.color;
                        borderColor = Phaser.Display.Color.HexStringToColor(roomInfo.color).color;
                    }
                    if (room.visited) {
                        bgColor = 0x1a1a2a;
                        alpha = 0.5;
                    }
                }

                if (isPlayer) {
                    bgColor = 0x2a1a3a;
                    borderColor = 0x8844ff;
                    alpha = 1;
                    label = '👤';
                    labelColor = '#bb88ff';
                }

                const cell = this.add.rectangle(px, py, cellSize, cellSize, bgColor, alpha)
                    .setStrokeStyle(2, borderColor);

                this.add.text(px, py - 8, label, {
                    fontSize: '20px', fontFamily: 'monospace', color: labelColor
                }).setOrigin(0.5);

                if (room.visited && !isPlayer) {
                    this.add.text(px, py + 18, '탐색됨', {
                        fontSize: '9px', fontFamily: 'monospace', color: '#445566'
                    }).setOrigin(0.5);
                }

                if (adjacent && !room.visited) {
                    cell.setInteractive();
                    const highlight = this.add.rectangle(px, py, cellSize, cellSize, 0x8844ff, 0.15);
                    this.add.text(px, py + 22, '이동', {
                        fontSize: '10px', fontFamily: 'monospace', color: '#8866cc'
                    }).setOrigin(0.5);

                    cell.on('pointerover', () => highlight.setAlpha(0.3));
                    cell.on('pointerout', () => highlight.setAlpha(0.15));
                    cell.on('pointerdown', () => this.enterRoom(x, y));
                }
            }
        }
    }

    drawInventory() {
        const inv = this.weightSystem.inventory;
        if (inv.length === 0) return;

        this.add.text(130, 570, '── 인벤토리 ──', {
            fontSize: '12px', fontFamily: 'monospace', color: '#445566'
        }).setOrigin(0.5);

        const cols = 5;
        const itemW = 45, itemGap = 5;
        const sx = 130 - (cols * (itemW + itemGap)) / 2 + itemW / 2;

        inv.forEach((item, i) => {
            const row = Math.floor(i / cols);
            const col = i % cols;
            const x = sx + col * (itemW + itemGap);
            const y = 600 + row * (itemW + itemGap);

            const rarityInfo = BO_RARITIES[item.rarity] || BO_RARITIES.common;
            this.add.rectangle(x, y, itemW, itemW, 0x111122).setStrokeStyle(1, rarityInfo.borderColor);
            this.add.text(x, y - 6, item.icon || '📦', { fontSize: '14px' }).setOrigin(0.5);
            this.add.text(x, y + 12, item.weight + 'kg', {
                fontSize: '8px', fontFamily: 'monospace', color: '#888888'
            }).setOrigin(0.5);
        });
    }

    enterRoom(x, y) {
        const room = this.mapSystem.grid[y][x];

        if (room.type === 'locked') {
            const hasKey = this.weightSystem.inventory.some(it => it.id === 'keycard');
            if (!hasKey) {
                this.add.text(640, 690, '🔒 키카드가 필요합니다!', {
                    fontSize: '14px', fontFamily: 'monospace', color: '#ff8844'
                }).setOrigin(0.5);
                return;
            }
            const keyIdx = this.weightSystem.inventory.findIndex(it => it.id === 'keycard');
            this.weightSystem.removeItem(keyIdx);
            room.type = 'item';
        }

        this.tensionSystem.onRoomMove();
        const result = this.mapSystem.moveToRoom(x, y, this.tensionSystem.tension);
        if (!result) return;

        if (result.type === 'exit') {
            this.scene.start('BOEscapeScene', {
                party: this.party,
                mapSystem: this.mapSystem,
                weightSystem: this.weightSystem,
                tensionSystem: this.tensionSystem
            });
            return;
        }

        this.scene.start('BORoomScene', {
            party: this.party,
            mapSystem: this.mapSystem,
            weightSystem: this.weightSystem,
            tensionSystem: this.tensionSystem,
            roomType: result.type,
            depth: result.depth
        });
    }
}
