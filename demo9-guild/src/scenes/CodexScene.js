class CodexScene extends Phaser.Scene {
    constructor() { super('CodexScene'); }

    init(data) {
        this.gameState = data.gameState;
        this.category = data.category || 'weapon';
        this.page = data.page || 0;
    }

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
        headerBg.lineStyle(0.5, T.divider || 0x5a4a2a, T.dividerAlpha || 0.5);
        headerBg.lineBetween(0, (T.headerHeight || 55), 1280, (T.headerHeight || 55));

        // ── 타이틀 (골드 + ◈ 장식) ──
        this.add.text(640, 25, '◈  📖 아이템 도감  ◈', {
            fontSize: `${(T.fontSize && T.fontSize.title) || 20}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44',
            fontStyle: 'bold'
        }).setOrigin(0.5);

        // 타이틀 좌우 장식 라인
        const ornLine = this.add.graphics();
        ornLine.lineStyle(1, T.ornament || 0x8a7a4a, T.ornamentAlpha || 0.4);
        ornLine.lineBetween(340, 25, 480, 25);
        ornLine.lineBetween(800, 25, 940, 25);

        // ── 뒤로가기 버튼 (ghost) ──
        UIButton.create(this, 80, 25, 100, 30, '← 마을', {
            variant: 'ghost',
            fontSize: (T.fontSize && T.fontSize.body) || 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        // ── 카테고리 탭 ──
        const categories = [
            { key: 'weapon', label: '⚔ 무기' },
            { key: 'armor', label: '🛡 방어구' },
            { key: 'accessory', label: '💍 장신구' },
            { key: 'zone_bp', label: '🩸 핏구역' },
            { key: 'zone_cargo', label: '🚂 화물' },
            { key: 'zone_bo', label: '🌑 정전' },
            { key: 'auction', label: '🏛 경매전용' },
            { key: 'craft', label: '🔨 제작전용' },
            { key: 'cursed', label: '⚠ 저주' },
            { key: 'material', label: '🔧 소재' },
            { key: 'consumable', label: '🧪 소모품' }
        ];

        let tabX = 30;
        categories.forEach(cat => {
            const isActive = cat.key === this.category;
            const btnW = cat.label.length * 8 + 20;
            UIButton.create(this, tabX + btnW / 2, 60, btnW, 24, cat.label, {
                color: isActive ? (T.buttonPrimary || 0x8a6a2a) : (T.panelFill || 0x2a2218),
                hoverColor: T.cardHover || 0x3a3020,
                textColor: isActive ? (T.buttonText || '#f0e8d0') : (T.textMuted || '#887860'),
                fontSize: (T.fontSize && T.fontSize.caption) || 10,
                onClick: () => this.scene.restart({ gameState: gs, category: cat.key, page: 0 })
            });
            tabX += btnW + 4;
        });

        // ── 아이템 목록 ──
        const items = this._getItemsForCategory(this.category);
        const perPage = 8;
        const totalPages = Math.max(1, Math.ceil(items.length / perPage));
        const page = Math.min(this.page, totalPages - 1);
        const pageItems = items.slice(page * perPage, (page + 1) * perPage);

        this._drawItemList(pageItems, 30, 90, 1220, 580);

        // ── 페이지네이션 ──
        if (totalPages > 1) {
            this.add.text(640, 695, `${page + 1} / ${totalPages}`, {
                fontSize: `${(T.fontSize && T.fontSize.body) || 12}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5);

            if (page > 0) {
                UIButton.create(this, 560, 695, 60, 24, '← 이전', {
                    variant: 'ghost',
                    fontSize: (T.fontSize && T.fontSize.body) || 11,
                    onClick: () => this.scene.restart({ gameState: gs, category: this.category, page: page - 1 })
                });
            }
            if (page < totalPages - 1) {
                UIButton.create(this, 720, 695, 60, 24, '다음 →', {
                    variant: 'ghost',
                    fontSize: (T.fontSize && T.fontSize.body) || 11,
                    onClick: () => this.scene.restart({ gameState: gs, category: this.category, page: page + 1 })
                });
            }
        }

        this.add.text(1250, 695, `총 ${items.length}종`, {
            fontSize: `${(T.fontSize && T.fontSize.body) || 11}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(1, 0.5);
    }

    _getItemsForCategory(category) {
        switch (category) {
            case 'weapon': return EQUIPMENT_TEMPLATES.weapon.map(t => this._templateToDisplay(t, 'weapon'));
            case 'armor': return EQUIPMENT_TEMPLATES.armor.map(t => this._templateToDisplay(t, 'armor'));
            case 'accessory': return EQUIPMENT_TEMPLATES.accessory.map(t => this._templateToDisplay(t, 'accessory'));
            case 'zone_bp': return this._flattenZone('bloodpit');
            case 'zone_cargo': return this._flattenZone('cargo');
            case 'zone_bo': return this._flattenZone('blackout');
            case 'auction': return AUCTION_EXCLUSIVE.map(a => ({
                name: a.name, slot: a.slot, rarity: a.rarity,
                stats: a.stats, penalty: a.penalty,
                special: a.special, specialDesc: a.specialDesc,
                flavor: a.flavor, source: '경매장 전용'
            }));
            case 'craft': return CRAFT_ONLY_EQUIPMENT.map(c => ({
                name: c.name, slot: c.slot, rarity: c.rarity,
                stats: c.stats, penalty: {},
                special: c.special, specialDesc: c.specialDesc,
                flavor: c.desc, source: '제작 전용'
            }));
            case 'cursed': return CURSED_EQUIPMENT.map(c => ({
                name: c.name, slot: c.slot, rarity: c.rarity,
                stats: c.stats, penalty: c.penalty || {},
                special: c.curseDebuff, specialDesc: c.curseDebuffDesc,
                flavor: c.desc, cursed: true, source: '저주 장비'
            }));
            case 'material': return MATERIAL_TEMPLATES.map(m => ({
                name: m.name, rarity: m.rarity, flavor: m.desc,
                source: this._zoneLabel(m.zone), isMaterial: true
            }));
            case 'consumable': return CONSUMABLE_TEMPLATES.map(c => ({
                name: c.name, flavor: c.desc, tier: c.tier,
                source: c.zone ? this._zoneLabel(c.zone) : `Tier ${c.tier}`, isConsumable: true
            }));
            default: return [];
        }
    }

    _templateToDisplay(t, slot) {
        const stats = {};
        if (t.baseAtk) stats.atk = t.baseAtk;
        if (t.baseDef) stats.def = t.baseDef;
        if (t.baseHp) stats.hp = t.baseHp;
        if (t.baseCrit) stats.critRate = t.baseCrit;
        if (t.baseSpd) stats.moveSpeed = t.baseSpd;
        return {
            name: t.name, slot, tier: t.tier, rarity: 'base',
            stats, penalty: {}, flavor: t.flavor,
            source: `Tier ${t.tier}`
        };
    }

    _flattenZone(zone) {
        const pool = ZONE_EQUIPMENT[zone];
        if (!pool) return [];
        const result = [];
        for (const slot of ['weapon', 'armor', 'accessory']) {
            if (!pool[slot]) continue;
            for (const t of pool[slot]) {
                result.push({
                    name: t.name, slot, rarity: t.minRarity,
                    stats: t.stats, penalty: t.penalty || {},
                    special: t.special, specialDesc: t.specialDesc,
                    zoneBonus: t.zoneBonus, flavor: t.flavor,
                    source: `${this._zoneLabel(zone)} / ${t.minRarity}+`
                });
            }
        }
        return result;
    }

    _zoneLabel(zone) {
        return { bloodpit: '핏빛 구덩이', cargo: '화물 열차', blackout: '정전 구역', common: '공통' }[zone] || zone;
    }

    _drawItemList(items, x, y, w, h) {
        UIPanel.create(this, x, y, w, h, { title: '', ornament: true });

        if (items.length === 0) {
            const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
            this.add.text(x + w / 2, y + h / 2, '항목 없음', {
                fontSize: `${(T.fontSize && T.fontSize.header) || 14}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5);
            return;
        }

        let cy = y + 10;
        items.forEach(item => {
            this._drawCodexRow(item, x + 10, cy, w - 20);
            cy += 68;
        });
    }

    _drawCodexRow(item, x, y, w) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        const rarity = item.rarity && ITEM_RARITY[item.rarity]
            ? ITEM_RARITY[item.rarity]
            : { name: '기본', color: 0x555555, textColor: '#aaaaaa' };

        const bg = this.add.graphics();
        const cardFill = item.cursed ? 0x2a1a18 : (T.cardFill || 0x231e14);
        bg.fillStyle(cardFill, 1);
        bg.fillRoundedRect(x, y, w, 62, T.borderRadius || 4);
        bg.lineStyle(1, rarity.color, 0.5);
        bg.strokeRoundedRect(x, y, w, 62, T.borderRadius || 4);

        const slotIcons = { weapon: '⚔', armor: '🛡', accessory: '💍' };
        const slotIcon = item.slot ? (slotIcons[item.slot] || '') : (item.isMaterial ? '🔧' : item.isConsumable ? '🧪' : '');

        this.add.text(x + 10, y + 6, `${slotIcon} ${item.name}`, {
            fontSize: '13px', fontFamily: T.fontFamily || 'monospace',
            color: rarity.textColor, fontStyle: 'bold'
        });

        const tags = [];
        if (item.rarity && ITEM_RARITY[item.rarity]) tags.push(`[${rarity.name}]`);
        if (item.tier) tags.push(`T${item.tier}`);
        if (item.source) tags.push(item.source);
        if (item.cursed) tags.push('⚠저주');

        this.add.text(x + 10, y + 24, tags.join(' · '), {
            fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        });

        if (item.stats && Object.keys(item.stats).length > 0) {
            const statStr = Object.entries(item.stats).map(([k, v]) => {
                if (k === 'critRate') return `CRIT+${Math.round(v * 100)}%`;
                if (k === 'moveSpeed') return `SPD+${v}`;
                return `${k.toUpperCase()}+${v}`;
            }).join('  ');
            let displayStr = statStr;
            if (item.penalty && Object.keys(item.penalty).length > 0) {
                const penStr = Object.entries(item.penalty).map(([k, v]) => `${k.toUpperCase()}${v}`).join(' ');
                displayStr += `  | ${penStr}`;
            }
            this.add.text(x + 10, y + 40, displayStr, {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textBlue || '#6699cc'
            });
        }

        if (item.specialDesc) {
            this.add.text(x + 500, y + 8, `★ ${item.specialDesc}`, {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textSuccess || '#66bb55'
            });
        }

        if (item.zoneBonus) {
            this.add.text(x + 500, y + 24, `🌍 ${item.zoneBonus}`, {
                fontSize: `${(T.fontSize && T.fontSize.caption) || 10}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textBlue || '#6699cc'
            });
        }

        if (item.flavor) {
            this.add.text(x + w - 10, y + 46, item.flavor, {
                fontSize: `${(T.fontSize && T.fontSize.tiny) || 9}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860',
                fontStyle: 'italic'
            }).setOrigin(1, 0);
        }
    }
}
