class VaultScene extends Phaser.Scene {
    constructor() { super('VaultScene'); }

    init(data) { this.gameState = data.gameState; }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a1a);
        const gs = this.gameState;

        this.add.text(640, 25, '🔒 비밀 금고', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ffaa44', fontStyle: 'bold'
        }).setOrigin(0.5);

        this.goldText = this.add.text(1260, 25, `${gs.gold}G`, {
            fontSize: '16px', fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(1, 0);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        const storageCap = GuildManager.getStorageCapacity(gs);
        const secureCap = GuildManager.getSecureContainerCapacity(gs);

        this.add.text(640, 55, `금고 해금! 보관함 ${storageCap}칸 | 보안 컨테이너 ${secureCap}칸`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(0.5);

        this._drawTransfer(gs);
    }

    _drawTransfer(gs) {
        if (this._objs) this._objs.forEach(o => o.destroy && o.destroy());
        this._objs = [];

        const storageCap = GuildManager.getStorageCapacity(gs);
        const secureCap = GuildManager.getSecureContainerCapacity(gs);

        this._drawPanel(gs, 30, 80, 595, '보관함', gs.storage, storageCap, false);
        this._drawPanel(gs, 655, 80, 595, `보안 컨테이너`, gs.secureContainer, secureCap, true);

        this._add(this.add.text(640, 700, '보안 컨테이너의 아이템은 용병 사망 시에도 보존됩니다', {
            fontSize: '11px', fontFamily: 'monospace', color: '#888866'
        }).setOrigin(0.5));
    }

    _add(o) { if (!this._objs) this._objs = []; this._objs.push(o); return o; }

    _drawPanel(gs, x, y, w, title, items, cap, isSecure) {
        this._add(UIPanel.create(this, x, y, w, 600, { title: `${title} (${items.length}/${cap})` }));

        if (items.length === 0) {
            this._add(this.add.text(x + w / 2, y + 300, '비어있음', {
                fontSize: '13px', fontFamily: 'monospace', color: '#444455'
            }).setOrigin(0.5));
            return;
        }

        let cy = y + 30;
        items.forEach((item, idx) => {
            if (cy > y + 575) return;
            const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;

            const bg = this._add(this.add.graphics());
            bg.fillStyle(0x1a1a2e, 1);
            bg.fillRoundedRect(x + 10, cy, w - 20, 40, 3);
            bg.lineStyle(1, rarity.color, 0.3);
            bg.strokeRoundedRect(x + 10, cy, w - 20, 40, 3);

            const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
            this._add(this.add.text(x + 20, cy + 5, typeIcons[item.type] || '?', { fontSize: '12px' }));

            this._add(this.add.text(x + 40, cy + 5, `${item.name} [${rarity.name}]`, {
                fontSize: '11px', fontFamily: 'monospace', color: rarity.textColor, fontStyle: 'bold'
            }));

            if (item.stats) {
                const statStr = Object.entries(item.stats).map(([k, v]) =>
                    typeof v === 'number' && v < 1 ? `${k}+${Math.round(v * 100)}%` : `${k}+${v}`
                ).join(' ');
                this._add(this.add.text(x + 40, cy + 22, statStr, {
                    fontSize: '9px', fontFamily: 'monospace', color: '#8888aa'
                }));
            }

            if (isSecure) {
                this._add(UIButton.create(this, x + w - 55, cy + 20, 80, 22, '꺼내기', {
                    color: 0x664444, hoverColor: 0x886666, textColor: '#ffaaaa', fontSize: 9,
                    onClick: () => {
                        const cap = GuildManager.getStorageCapacity(gs);
                        if (gs.storage.length >= cap) {
                            UIToast.show(this, '보관함이 가득 찼습니다', { color: '#ff6666' });
                            return;
                        }
                        const removed = StorageManager.removeFromSecure(gs, item.id);
                        if (removed) {
                            StorageManager.addItem(gs, removed);
                            SaveManager.save(gs);
                            UIToast.show(this, `${item.name} → 보관함`, { color: '#aaaacc' });
                            this._drawTransfer(gs);
                            this.goldText.setText(`${gs.gold}G`);
                        }
                    }
                }));
            } else {
                this._add(UIButton.create(this, x + w - 55, cy + 20, 80, 22, '보안 이동', {
                    color: 0x444466, hoverColor: 0x666688, textColor: '#aaccff', fontSize: 9,
                    onClick: () => {
                        const sCap = GuildManager.getSecureContainerCapacity(gs);
                        if (gs.secureContainer.length >= sCap) {
                            UIToast.show(this, '보안 컨테이너가 가득 찼습니다', { color: '#ff6666' });
                            return;
                        }
                        const removed = StorageManager.removeItem(gs, item.id);
                        if (removed) {
                            StorageManager.addToSecure(gs, removed);
                            SaveManager.save(gs);
                            UIToast.show(this, `${item.name} → 보안 컨테이너`, { color: '#44aaff' });
                            this._drawTransfer(gs);
                            this.goldText.setText(`${gs.gold}G`);
                        }
                    }
                }));
            }

            cy += 46;
        });
    }
}
