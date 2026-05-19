class StorageScene extends Phaser.Scene {
    constructor() { super('StorageScene'); }

    init(data) { this.gameState = data.gameState; }

    create() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        this.add.rectangle(640, 360, 1280, 720, T.bg || 0x1a1510);
        const gs = this.gameState;

        // ── 헤더 배경 ──
        const headerBg = this.add.graphics();
        headerBg.fillStyle(T.headerBg || 0x1e1810, 1);
        headerBg.fillRect(0, 0, 1280, T.headerHeight || 55);
        // 하단 장식선
        headerBg.lineStyle(1, T.ornament || 0x8a7a4a, T.ornamentAlpha || 0.4);
        headerBg.lineBetween(0, (T.headerHeight || 55) - 1, 1280, (T.headerHeight || 55) - 1);
        // 상단 장식선
        headerBg.lineStyle(0.5, T.ornament || 0x8a7a4a, 0.2);
        headerBg.lineBetween(0, 1, 1280, 1);

        this.add.text(640, 25, '◈  보관함  ◈', {
            fontSize: `${(T.fontSize && T.fontSize.title) || 20}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44',
            fontStyle: 'bold'
        }).setOrigin(0.5);

        this.add.text(1260, 25, `${gs.gold}G`, {
            fontSize: `${(T.fontSize && T.fontSize.header) || 16}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44',
            fontStyle: 'bold'
        }).setOrigin(1, 0);

        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            variant: 'ghost',
            fontSize: (T.fontSize && T.fontSize.body) || 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        const cap = GuildManager.getStorageCapacity(gs);
        this.add.text(640, 50, `보관함: ${gs.storage.length}/${cap}칸`, {
            fontSize: `${(T.fontSize && T.fontSize.body) || 12}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5);

        this._drawStorageGrid(gs, 30, 75, 750, '보관함', gs.storage, false);

        const secureCap = GuildManager.getSecureContainerCapacity(gs);
        this._drawSecureContainer(gs, 810, 75, 440, secureCap);
    }

    _drawStorageGrid(gs, x, y, w, title, items, isSecure) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        UIPanel.create(this, x, y, w, 620, { title, ornament: true });

        if (items.length === 0) {
            this.add.text(x + w / 2, y + 310, '비어있음', {
                fontSize: '13px',
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
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
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;

        const bg = this.add.graphics();
        bg.fillStyle(T.cardFill || 0x231e14, 1);
        bg.fillRoundedRect(x, y, w, 42, T.borderRadius || 6);
        bg.lineStyle(1, rarity.color, 0.3);
        bg.strokeRoundedRect(x, y, w, 42, T.borderRadius || 6);

        const typeIcons = { equipment: '⚔', material: '🔧', consumable: '🧪' };
        this.add.text(x + 8, y + 5, typeIcons[item.type] || '?', { fontSize: '14px' });

        this.add.text(x + 30, y + 5, item.name, {
            fontSize: `${(T.fontSize && T.fontSize.body) || 12}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: rarity.textColor,
            fontStyle: 'bold'
        });

        this.add.text(x + 30, y + 22, `[${rarity.name}] ${item.desc || ''}`, {
            fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        });

        if (item.stats) {
            const statStr = Object.entries(item.stats).map(([k, v]) => `${k}+${v}`).join(' ');
            this.add.text(x + w - 160, y + 5, statStr, {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textSecondary || '#b8a888'
            });
        }

        this.add.text(x + w - 80, y + 5, `${item.value}G`, {
            fontSize: '11px',
            fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44'
        });

        const sellValue = Math.floor(item.value * 0.7);
        UIButton.create(this, x + w - 35, y + 28, 55, 20, `판매 ${sellValue}G`, {
            variant: 'danger',
            fontSize: (T.fontSize && T.fontSize.tiny) || 9,
            onClick: () => {
                StorageManager.sellItem(gs, item.id);
                this.scene.restart({ gameState: gs });
            }
        });

        const hitZone = this.add.zone(x + w / 2 - 40, y + 21, w - 100, 42).setInteractive({ useHandCursor: true });
        hitZone.on('pointerover', () => {
            bg.clear();
            bg.fillStyle(T.cardHover || 0x3a3020, 1);
            bg.fillRoundedRect(x, y, w, 42, T.borderRadius || 6);
            bg.lineStyle(1, rarity.color, 0.5);
            bg.strokeRoundedRect(x, y, w, 42, T.borderRadius || 6);

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
        hitZone.on('pointerout', () => {
            bg.clear();
            bg.fillStyle(T.cardFill || 0x231e14, 1);
            bg.fillRoundedRect(x, y, w, 42, T.borderRadius || 6);
            bg.lineStyle(1, rarity.color, 0.3);
            bg.strokeRoundedRect(x, y, w, 42, T.borderRadius || 6);

            UITooltip.hide(this);
        });
    }

    _drawSecureContainer(gs, x, y, w, cap) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        UIPanel.create(this, x, y, w, 620, {
            title: `보안 컨테이너 (${gs.secureContainer.length}/${cap})`,
            ornament: true
        });

        this.add.text(x + w / 2, y + 40, '사망 시에도 보존되는 아이템', {
            fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5);

        if (gs.secureContainer.length === 0) {
            this.add.text(x + w / 2, y + 310, '비어있음', {
                fontSize: '13px',
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
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
