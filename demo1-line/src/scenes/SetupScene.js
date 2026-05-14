class SetupScene extends Phaser.Scene {
    constructor() {
        super({ key: 'SetupScene' });
    }

    create() {
        this.party = ['tank', 'rogue', 'priest', 'mage'];
        this.slots = 4;
        this.selectedSlot = -1;

        this.createBackground();
        this.createTitle();
        this.createPartySlots();
        this.createCharacterPool();
        this.createEnemyPreview();
        this.createStartButton();
    }

    createBackground() {
        const g = this.add.graphics();
        for (let i = 0; i < 720; i++) {
            const ratio = i / 720;
            const r = Math.floor(20 + ratio * 15);
            const gr = Math.floor(20 + ratio * 20);
            const b = Math.floor(45 + ratio * 30);
            g.fillStyle(Phaser.Display.Color.GetColor(r, gr, b));
            g.fillRect(0, i, 1280, 1);
        }
    }

    createTitle() {
        this.add.text(640, 40, '⚔ 라인 자동전투 데모 ⚔', {
            fontSize: '28px',
            fontFamily: 'monospace',
            color: '#ffffff',
            stroke: '#000000',
            strokeThickness: 4
        }).setOrigin(0.5);

        this.add.text(640, 80, '파티를 구성하고 전투를 시작하세요', {
            fontSize: '14px',
            fontFamily: 'monospace',
            color: '#aaaaaa'
        }).setOrigin(0.5);
    }

    createPartySlots() {
        this.add.text(640, 130, '[ 아군 파티 ]', {
            fontSize: '18px',
            fontFamily: 'monospace',
            color: '#44ff88',
            stroke: '#000000',
            strokeThickness: 2
        }).setOrigin(0.5);

        this.slotContainers = [];
        const startX = 340;
        const spacing = 160;

        for (let i = 0; i < this.slots; i++) {
            const x = startX + i * spacing;
            const y = 220;
            this.createSlot(x, y, i);
        }
    }

    createSlot(x, y, index) {
        const charKey = this.party[index];
        const data = CHAR_DATA[charKey];

        const bg = this.add.rectangle(x, y, 120, 130, 0x333355, 0.8)
            .setStrokeStyle(2, 0x5566aa)
            .setInteractive({ useHandCursor: true });

        const colorBox = this.add.rectangle(x, y - 20, 32, 32, data.color);
        const nameText = this.add.text(x, y + 15, data.name, {
            fontSize: '14px', fontFamily: 'monospace', color: '#ffffff'
        }).setOrigin(0.5);

        const roleText = this.add.text(x, y + 35, this.getRoleLabel(data.role), {
            fontSize: '11px', fontFamily: 'monospace', color: '#aaaaaa'
        }).setOrigin(0.5);

        const statsText = this.add.text(x, y + 52, `HP:${data.hp} ATK:${data.atk}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#888888'
        }).setOrigin(0.5);

        this.slotContainers.push({ bg, colorBox, nameText, roleText, statsText, x, y });

        bg.on('pointerdown', () => {
            this.selectedSlot = index;
            this.highlightSlot(index);
        });

        bg.on('pointerover', () => bg.setFillStyle(0x444466));
        bg.on('pointerout', () => bg.setFillStyle(0x333355));
    }

    createCharacterPool() {
        this.add.text(640, 330, '[ 캐릭터 선택 ] - 슬롯을 클릭한 후 아래 캐릭터를 선택', {
            fontSize: '13px',
            fontFamily: 'monospace',
            color: '#cccccc'
        }).setOrigin(0.5);

        const chars = ['tank', 'rogue', 'priest', 'mage'];
        const startX = 340;
        const spacing = 160;

        chars.forEach((charKey, i) => {
            const data = CHAR_DATA[charKey];
            const x = startX + i * spacing;
            const y = 400;

            const btn = this.add.rectangle(x, y, 120, 80, 0x222244, 0.8)
                .setStrokeStyle(2, data.color)
                .setInteractive({ useHandCursor: true });

            this.add.rectangle(x, y - 10, 24, 24, data.color);
            this.add.text(x, y + 15, data.name, {
                fontSize: '13px', fontFamily: 'monospace', color: '#ffffff'
            }).setOrigin(0.5);

            this.add.text(x, y + 32, `HP:${data.hp} ATK:${data.atk} SPD:${data.attackSpeed/1000}s`, {
                fontSize: '9px', fontFamily: 'monospace', color: '#888888'
            }).setOrigin(0.5);

            btn.on('pointerdown', () => {
                if (this.selectedSlot >= 0) {
                    this.party[this.selectedSlot] = charKey;
                    this.updateSlot(this.selectedSlot, charKey);
                }
            });
            btn.on('pointerover', () => btn.setFillStyle(0x333355));
            btn.on('pointerout', () => btn.setFillStyle(0x222244));
        });
    }

    createEnemyPreview() {
        this.add.text(640, 480, '[ 적 구성 ]', {
            fontSize: '16px',
            fontFamily: 'monospace',
            color: '#ff6644',
            stroke: '#000000',
            strokeThickness: 2
        }).setOrigin(0.5);

        const enemies = DEFAULT_ENEMIES;
        const startX = 240;
        const spacing = 140;

        enemies.forEach((key, i) => {
            const data = CHAR_DATA[key];
            const x = startX + i * spacing;
            const y = 540;

            this.add.rectangle(x, y, 100, 70, 0x332222, 0.6)
                .setStrokeStyle(1, 0x664444);
            this.add.rectangle(x, y - 8, 20, 20, data.color);
            this.add.text(x, y + 15, data.name, {
                fontSize: '12px', fontFamily: 'monospace', color: '#ccaaaa'
            }).setOrigin(0.5);
            this.add.text(x, y + 30, `HP:${data.hp}`, {
                fontSize: '10px', fontFamily: 'monospace', color: '#886666'
            }).setOrigin(0.5);
        });
    }

    createStartButton() {
        const btn = this.add.rectangle(640, 650, 240, 50, 0x44aa44)
            .setStrokeStyle(2, 0x66cc66)
            .setInteractive({ useHandCursor: true });

        const btnText = this.add.text(640, 650, '▶ 전투 시작', {
            fontSize: '20px',
            fontFamily: 'monospace',
            color: '#ffffff',
            fontStyle: 'bold'
        }).setOrigin(0.5);

        btn.on('pointerover', () => btn.setFillStyle(0x55bb55));
        btn.on('pointerout', () => btn.setFillStyle(0x44aa44));
        btn.on('pointerdown', () => {
            this.scene.start('BattleScene', {
                party: [...this.party],
                enemies: DEFAULT_ENEMIES,
                speed: 1.5
            });
        });
    }

    updateSlot(index, charKey) {
        const data = CHAR_DATA[charKey];
        const slot = this.slotContainers[index];
        slot.colorBox.setFillStyle(data.color);
        slot.nameText.setText(data.name);
        slot.roleText.setText(this.getRoleLabel(data.role));
        slot.statsText.setText(`HP:${data.hp} ATK:${data.atk}`);
    }

    highlightSlot(index) {
        this.slotContainers.forEach((slot, i) => {
            if (i === index) {
                slot.bg.setStrokeStyle(3, 0xffcc00);
            } else {
                slot.bg.setStrokeStyle(2, 0x5566aa);
            }
        });
    }

    getRoleLabel(role) {
        switch (role) {
            case 'tank': return '탱커';
            case 'dps': return '딜러';
            case 'healer': return '힐러';
            default: return role;
        }
    }
}
