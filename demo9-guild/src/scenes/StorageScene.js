class StorageScene extends Phaser.Scene {
    constructor() { super('StorageScene'); }

    init(data) { this.gameState = data.gameState; }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
        const gs = this.gameState;

        this.add.text(640, 25, '보관함', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(1260, 25, `${gs.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        const cap = GuildManager.getStorageCapacity(gs);
        this.add.text(640, 50, `보관함: ${gs.storage.length}/${cap}칸`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5);

        this._drawStorageGrid(gs, 30, 75, 750, '보관함', gs.storage, false);

        const secureCap = GuildManager.getSecureContainerCapacity(gs);
        this._drawSecureContainer(gs, 810, 75, 440, secureCap);
    }

    _drawStorageGrid(gs, x, y, w, title, items, isSecure) {
        UIPanel.create(this, x, y, w, 620, { title });

        if (items.length === 0) {
            this.add.text(x + w / 2, y + 310, '비어있음', {
                fontSize: '13px', fontFamily: 'monospace', color: '#444455'
            }).setOrigin(0.5);
            return;
        }

        let cy = y + 35;
        items.forEach((item, idx) => {
            if (cy > y + 600) return;
            this._drawItemRow(gs, item, x + 10, cy, w - 20, isSecure);
            cy += 48;
        });
    }

    _drawItemRow(gs, item, x, y, w, isSecure) {
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;

        const bg = this.add.graphics();
        bg.fillStyle(0x1a1a2e, 1);
        bg.fillRoundedRect(x, y, w, 42, 3);
        bg.lineStyle(1, rarity.color, 0.3);
        bg.strokeRoundedRect(x, y, w, 42, 3);

        const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
        this.add.text(x + 8, y + 5, typeIcons[item.type] || '?', { fontSize: '14px' });

        this.add.text(x + 30, y + 5, item.name, {
            fontSize: '12px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        });

        this.add.text(x + 30, y + 22, `[${rarity.name}] ${item.desc || ''}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#667788'
        });

        if (item.stats) {
            const statStr = Object.entries(item.stats).map(([k, v]) => `${k}+${v}`).join(' ');
            this.add.text(x + w - 160, y + 5, statStr, {
                fontSize: '10px', fontFamily: 'monospace', color: '#8888aa'
            });
        }

        this.add.text(x + w - 80, y + 5, `${item.value}G`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#ffcc44'
        });

        const sellValue = Math.floor(item.value * 0.7);
        UIButton.create(this, x + w - 35, y + 28, 55, 20, `판매 ${sellValue}G`, {
            color: 0x886644, hoverColor: 0xaa8866, textColor: '#ffeecc', fontSize: 9,
            onClick: () => {
                StorageManager.sellItem(gs, item.id);
                this.scene.restart({ gameState: gs });
            }
        });

        const hitZone = this.add.zone(x + w / 2 - 40, y + 21, w - 100, 42).setInteractive({ useHandCursor: true });
        hitZone.on('pointerover', () => {
            const lines = [`${item.name} [${rarity.name}]`];
            if (item.flavor) lines.push(item.flavor);
            if (item.desc && item.desc !== item.flavor) lines.push(item.desc);
            if (item.stats) {
                lines.push('---');
                Object.entries(item.stats).forEach(([k, v]) => {
                    const label = { atk: 'ATK', def: 'DEF', hp: 'HP', critRate: 'CRIT', moveSpeed: 'SPD' }[k] || k;
                    const display = k === 'critRate' ? `${Math.round(v * 100)}%` : `+${v}`;
                    lines.push(`  ${label}: ${display}`);
                });
            }
            if (item.penalty && Object.keys(item.penalty).length > 0) {
                lines.push('--- 페널티 ---');
                Object.entries(item.penalty).forEach(([k, v]) => {
                    const label = { atk: 'ATK', def: 'DEF', hp: 'HP', spd: 'SPD' }[k] || k;
                    lines.push(`  ${label}: ${v}`);
                });
            }
            if (item.specialDesc) lines.push(`★ ${item.specialDesc}`);
            if (item.zoneBonus) lines.push(`🌍 ${item.zoneBonus}`);
            if (item.cursed) lines.push(`⚠ ${item.curseDebuffDesc}`);
            lines.push(`가치: ${item.value}G`);
            UITooltip.show(this, x + w / 2, y, lines);
        });
        hitZone.on('pointerout', () => UITooltip.hide(this));
    }

    _drawSecureContainer(gs, x, y, w, cap) {
        UIPanel.create(this, x, y, w, 620, { title: `보안 컨테이너 (${gs.secureContainer.length}/${cap})` });

        this.add.text(x + w / 2, y + 40, '사망 시에도 보존되는 아이템', {
            fontSize: '10px', fontFamily: 'monospace', color: '#667788'
        }).setOrigin(0.5);

        if (gs.secureContainer.length === 0) {
            this.add.text(x + w / 2, y + 310, '비어있음', {
                fontSize: '13px', fontFamily: 'monospace', color: '#444455'
            }).setOrigin(0.5);
        } else {
            let cy = y + 55;
            gs.secureContainer.forEach(item => {
                if (cy > y + 600) return;
                this._drawItemRow(gs, item, x + 10, cy, w - 20, true);
                cy += 48;
            });
        }
    }
}
