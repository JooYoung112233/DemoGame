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

        // 스크롤 상태
        this._storageScrollY = 0;
        this._storageContainer = null;

        this._drawStorageGrid(gs, 30, 75, 1220, '보관함', gs.storage);

        // 휠 이벤트
        this.input.on('wheel', this._onWheel, this);
    }

    _onWheel(pointer, gameObjects, dx, dy) {
        const wx = pointer.worldX;
        const wy = pointer.worldY;

        // 보관함 영역
        if (wx >= 30 && wx <= 1250 && wy >= 75 && wy <= 695) {
            this._storageScrollY += dy * 0.5;
            this._clampStorageScroll();
            this._updateStorageContainer();
        }
    }

    _clampStorageScroll() {
        const gs = this.gameState;
        const itemCount = gs.storage.length;
        const contentH = itemCount * 48 + 10;
        const viewH = 580;
        const maxScroll = Math.max(0, contentH - viewH);
        this._storageScrollY = Math.max(0, Math.min(this._storageScrollY, maxScroll));
    }

    _updateStorageContainer() {
        if (this._storageContainer) this._storageContainer.y = 110 - this._storageScrollY;
        this._updateScrollbar(this._storageThumb, this._storageScrollY,
            this.gameState.storage.length * 48 + 10, 580, 75 + 35, 580);
    }

    _updateScrollbar(thumb, scrollY, contentH, viewH, trackY, trackH) {
        if (!thumb) return;
        if (contentH <= viewH) {
            thumb.setVisible(false);
            return;
        }
        thumb.setVisible(true);
        const ratio = viewH / contentH;
        const thumbH = Math.max(20, trackH * ratio);
        const scrollRatio = scrollY / (contentH - viewH);
        const thumbY = trackY + scrollRatio * (trackH - thumbH);
        thumb.clear();
        thumb.fillStyle(0xffffff, 0.3);
        thumb.fillRoundedRect(0, 0, 6, thumbH, 3);
        thumb.setPosition(thumb.x, thumbY);
    }

    _drawStorageGrid(gs, x, y, w, title, items) {
        UIPanel.create(this, x, y, w, 620, { title });

        if (items.length === 0) {
            this.add.text(x + w / 2, y + 310, '비어있음', {
                fontSize: '13px', fontFamily: 'monospace', color: '#444455'
            }).setOrigin(0.5);
            return;
        }

        // 스크롤 가능한 컨테이너
        const container = this.add.container(0, y + 35);
        this._storageContainer = container;

        let cy = 0;
        items.forEach((item, idx) => {
            this._drawItemRow(container, gs, item, x + 10, cy, w - 30);
            cy += 48;
        });

        // 마스크 생성
        const maskShape = this.make.graphics({ add: false });
        maskShape.fillStyle(0xffffff);
        maskShape.fillRect(x, y + 35, w, 580);
        const mask = maskShape.createGeometryMask();
        container.setMask(mask);

        // 스크롤바
        this._storageThumb = this.add.graphics();
        this._storageThumb.setPosition(x + w - 8, y + 35);
        this._storageThumb.setDepth(10);
        this._updateStorageContainer();
    }

    _drawItemRow(container, gs, item, x, y, w) {
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;

        const bg = this.add.graphics();
        bg.fillStyle(0x1a1a2e, 1);
        bg.fillRoundedRect(x, y, w, 42, 3);
        bg.lineStyle(1, rarity.color, 0.3);
        bg.strokeRoundedRect(x, y, w, 42, 3);
        container.add(bg);

        const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
        container.add(this.add.text(x + 8, y + 5, typeIcons[item.type] || '?', { fontSize: '14px' }));

        container.add(this.add.text(x + 30, y + 5, item.name, {
            fontSize: '12px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
        }));

        container.add(this.add.text(x + 30, y + 22, `[${rarity.name}] ${item.desc || ''}`, {
            fontSize: '10px', fontFamily: 'monospace', color: '#667788'
        }));

        if (item.stats) {
            const statStr = Object.entries(item.stats).map(([k, v]) => `${k}+${v}`).join(' ');
            container.add(this.add.text(x + w - 200, y + 5, statStr, {
                fontSize: '10px', fontFamily: 'monospace', color: '#8888aa'
            }));
        }

        container.add(this.add.text(x + w - 80, y + 5, `${item.value}G`, {
            fontSize: '11px', fontFamily: 'monospace', color: '#ffcc44'
        }));

        const sellValue = Math.floor(item.value * 0.7);
        const sellBtn = UIButton.create(this, x + w - 35, y + 28, 55, 20, `판매 ${sellValue}G`, {
            color: 0x886644, hoverColor: 0xaa8866, textColor: '#ffeecc', fontSize: 9,
            onClick: () => {
                StorageManager.sellItem(gs, item.id);
                this.scene.restart({ gameState: gs });
            }
        });
        container.add(sellBtn);

        // 툴팁 히트존
        const hitZone = this.add.zone(x + w / 2 - 40, y + 21, w - 100, 42).setInteractive({ useHandCursor: true });
        hitZone.on('pointerover', () => {
            const lines = [`${item.name} [${rarity.name}]`];
            if (item.desc) lines.push(item.desc);
            if (item.stats) {
                lines.push('---');
                Object.entries(item.stats).forEach(([k, v]) => lines.push(`${k}: +${v}`));
            }
            lines.push(`가치: ${item.value}G  |  무게: ${item.weight}`);
            UITooltip.show(this, x + w / 2, y + container.y, lines);
        });
        hitZone.on('pointerout', () => UITooltip.hide(this));
        container.add(hitZone);
    }
}
