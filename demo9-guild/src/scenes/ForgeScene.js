class ForgeScene extends Phaser.Scene {
    constructor() { super('ForgeScene'); }

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
        headerBg.lineStyle(1, T.divider || 0x5a4a2a, T.dividerAlpha || 0.5);
        headerBg.lineBetween(0, (T.headerHeight || 55), 1280, (T.headerHeight || 55));

        this.add.text(640, 27, '◈  장비 제작소  ◈', {
            fontSize: `${(T.fontSize && T.fontSize.title) || 20}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44',
            fontStyle: 'bold'
        }).setOrigin(0.5);

        this.goldText = this.add.text(1260, 20, `${gs.gold}G`, {
            fontSize: `${(T.fontSize && T.fontSize.header) || 16}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44',
            fontStyle: 'bold'
        }).setOrigin(1, 0);

        UIButton.create(this, 80, 27, 100, 30, '← 마을', {
            variant: 'ghost',
            fontSize: (T.fontSize && T.fontSize.body) || 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        this.tab = 'craft';
        this._drawTabs();
        this._drawContent();
    }

    _drawTabs() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};

        // 기존 탭 제거
        if (this._tabContainer) {
            this._tabContainer.destroy();
            this._tabContainer = null;
        }

        const tabs = [
            { key: 'craft', label: '제작', icon: '⚒' },
            { key: 'enhance', label: '강화', icon: '✦' },
            { key: 'materials', label: '소재 현황', icon: '🔧' }
        ];

        this._tabContainer = UITabs.create(this, 430, (T.headerHeight || 55) + 6, tabs, (key) => {
            this.tab = key;
            this._clearContent();
            this._drawContent();
        }, {
            activeKey: this.tab,
            tabWidth: 130,
            tabHeight: 28,
            gap: 4,
        });
    }

    _clearContent() {
        if (this._contentObjects) {
            this._contentObjects.forEach(obj => {
                if (obj && obj.destroy) obj.destroy();
            });
        }
        this._contentObjects = [];
    }

    _drawContent() {
        this._clearContent();
        switch (this.tab) {
            case 'craft': this._drawCraftTab(); break;
            case 'enhance': this._drawEnhanceTab(); break;
            case 'materials': this._drawMaterialsTab(); break;
        }
    }

    _addObj(obj) {
        this._contentObjects.push(obj);
        return obj;
    }

    _drawCraftTab() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const matCounts = this._getMaterialCounts();

        const categories = [
            { key: 'weapon', label: '⚔ 무기', x: 30 },
            { key: 'armor', label: '🛡 방어구', x: 440 },
            { key: 'accessory', label: '💍 장신구', x: 850 }
        ];

        categories.forEach(cat => {
            this._addObj(this.add.text(cat.x + 10, 95, cat.label, {
                fontSize: `${(T.fontSize && T.fontSize.header) || 14}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textPrimary || '#e8d8c0',
                fontStyle: 'bold'
            }));

            const recipes = RECIPE_DATA.filter(r => r.category === cat.key);
            recipes.forEach((recipe, idx) => {
                const ry = 125 + idx * 115;
                this._drawRecipeCard(recipe, cat.x, ry, 390, matCounts);
            });
        });
    }

    _drawRecipeCard(recipe, x, y, w, matCounts) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const canCraft = this._canCraft(recipe, matCounts);

        const bg = this._addObj(this.add.graphics());
        const cardFill = canCraft ? 0x1a2a1a : (T.cardFill || 0x231e14);
        const cardStroke = canCraft ? 0x448844 : (T.panelStroke || 0x5a4a2a);
        bg.fillStyle(cardFill, 1);
        bg.fillRoundedRect(x, y, w, 105, T.borderRadius || 6);
        bg.lineStyle(1, cardStroke, 0.5);
        bg.strokeRoundedRect(x, y, w, 105, T.borderRadius || 6);

        this._addObj(this.add.text(x + 10, y + 8, recipe.name, {
            fontSize: '13px',
            fontFamily: T.fontFamily || 'monospace',
            color: T.textPrimary || '#e8d8c0',
            fontStyle: 'bold'
        }));

        const res = recipe.result;
        const statParts = [];
        if (res.baseAtk) statParts.push(`ATK+${res.baseAtk}`);
        if (res.baseDef) statParts.push(`DEF+${res.baseDef}`);
        if (res.baseHp) statParts.push(`HP+${res.baseHp}`);
        if (res.critRate) statParts.push(`CRIT+${Math.round(res.critRate * 100)}%`);
        if (res.moveSpeed) statParts.push(`SPD+${res.moveSpeed}`);
        if (res.lifesteal) statParts.push(`흡혈+${Math.round(res.lifesteal * 100)}%`);
        this._addObj(this.add.text(x + 10, y + 28, statParts.join('  '), {
            fontSize: '10px',
            fontFamily: T.fontFamily || 'monospace',
            color: T.textSecondary || '#b8a888'
        }));

        this._addObj(this.add.text(x + 10, y + 46, `비용: ${recipe.goldCost}G`, {
            fontSize: '10px',
            fontFamily: T.fontFamily || 'monospace',
            color: T.textGold || '#ffcc44'
        }));

        let mx = x + 10;
        Object.entries(recipe.materials).forEach(([matName, need]) => {
            const have = matCounts[matName] || 0;
            const enough = have >= need;
            this._addObj(this.add.text(mx, y + 62, `${matName} ${have}/${need}`, {
                fontSize: '10px',
                fontFamily: T.fontFamily || 'monospace',
                color: enough ? (T.textSuccess || '#66bb55') : (T.textDanger || '#cc4422')
            }));
            mx += 120;
        });

        this._addObj(UIButton.create(this, x + w - 55, y + 80, 90, 26, '제작', {
            variant: 'primary',
            fontSize: 11,
            disabled: !canCraft,
            onClick: () => {
                if (!canCraft) return;
                this._craftItem(recipe);
            }
        }));
    }

    _drawEnhanceTab() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const equipment = gs.storage.filter(i => i.type === 'equipment');

        this._addObj(this.add.text(640, 95, '장비를 선택하여 등급을 올립니다 (소재 + 골드 필요)', {
            fontSize: `${(T.fontSize && T.fontSize.body) || 12}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textMuted || '#887860'
        }).setOrigin(0.5));

        if (equipment.length === 0) {
            this._addObj(this.add.text(640, 360, '강화할 장비가 없습니다', {
                fontSize: `${(T.fontSize && T.fontSize.header) || 14}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5));
            return;
        }

        const matCounts = this._getMaterialCounts();
        let cy = 120;

        equipment.forEach((item, idx) => {
            if (cy > 650) return;
            this._drawEnhanceRow(item, 80, cy, 1120, matCounts);
            cy += 68;
        });
    }

    _drawEnhanceRow(item, x, y, w, matCounts) {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const rarity = ITEM_RARITY[item.rarity] || ITEM_RARITY.common;
        const nextRar = getNextRarity(item.rarity);
        const rarityIdx = (typeof RARITY_ORDER !== 'undefined') ? RARITY_ORDER.indexOf(item.rarity) + 1 : 1;
        const cost = getEnhanceCost(rarityIdx);

        const bg = this._addObj(this.add.graphics());
        bg.fillStyle(T.cardFill || 0x231e14, 1);
        bg.fillRoundedRect(x, y, w, 58, T.borderRadius || 6);
        bg.lineStyle(1, rarity.color, 0.4);
        bg.strokeRoundedRect(x, y, w, 58, T.borderRadius || 6);

        this._addObj(this.add.text(x + 10, y + 8, `${item.name}`, {
            fontSize: '12px',
            fontFamily: T.fontFamily || 'monospace',
            color: rarity.textColor,
            fontStyle: 'bold'
        }));

        if (item.stats) {
            const statStr = Object.entries(item.stats).map(([k, v]) =>
                typeof v === 'number' && v < 1 ? `${k}+${Math.round(v * 100)}%` : `${k}+${v}`
            ).join('  ');
            this._addObj(this.add.text(x + 10, y + 28, statStr, {
                fontSize: '10px',
                fontFamily: T.fontFamily || 'monospace',
                color: T.textSecondary || '#b8a888'
            }));
        }

        this._addObj(this.add.text(x + 300, y + 8, `[${rarity.name}]`, {
            fontSize: '11px',
            fontFamily: T.fontFamily || 'monospace',
            color: rarity.textColor
        }));

        if (!nextRar || !cost) {
            this._addObj(this.add.text(x + w - 100, y + 20, '최대 등급', {
                fontSize: '11px',
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860'
            }).setOrigin(0.5));
            return;
        }

        const nextRarData = ITEM_RARITY[nextRar];
        const totalMats = this._getTotalMaterialCount();
        const canEnhance = gs.gold >= cost.gold && totalMats >= cost.materials;

        this._addObj(this.add.text(x + 400, y + 8, `→ [${nextRarData.name}]`, {
            fontSize: '11px',
            fontFamily: T.fontFamily || 'monospace',
            color: nextRarData.textColor
        }));

        this._addObj(this.add.text(x + 400, y + 28, `${cost.gold}G + 소재 ${totalMats}/${cost.materials}개`, {
            fontSize: '10px',
            fontFamily: T.fontFamily || 'monospace',
            color: canEnhance ? (T.textSuccess || '#66bb55') : (T.textDanger || '#cc4422')
        }));

        this._addObj(UIButton.create(this, x + w - 55, y + 30, 90, 26, '강화', {
            variant: 'primary',
            fontSize: 11,
            disabled: !canEnhance,
            onClick: () => {
                if (!canEnhance) return;
                this._enhanceItem(item, cost, nextRar);
            }
        }));
    }

    _drawMaterialsTab() {
        const T = (typeof UI_THEME !== 'undefined') ? UI_THEME : {};
        const gs = this.gameState;
        const matCounts = this._getMaterialCounts();
        const matNames = Object.keys(matCounts);

        this._addObj(this.add.text(640, 95, '보유 소재 현황', {
            fontSize: `${(T.fontSize && T.fontSize.header) || 14}px`,
            fontFamily: T.fontFamily || 'monospace',
            color: T.textPrimary || '#e8d8c0',
            fontStyle: 'bold'
        }).setOrigin(0.5));

        if (matNames.length === 0) {
            this._addObj(this.add.text(640, 360, '보유한 소재가 없습니다\n구역 탐사에서 소재를 획득하세요', {
                fontSize: `${(T.fontSize && T.fontSize.header) || 14}px`,
                fontFamily: T.fontFamily || 'monospace',
                color: T.textMuted || '#887860',
                align: 'center'
            }).setOrigin(0.5));
            return;
        }

        const panel = this._addObj(UIPanel.create(this, 300, 120, 680, 40 + matNames.length * 40, { title: '소재' }));

        let cy = 155;
        matNames.forEach(name => {
            const count = matCounts[name];
            const tmpl = MATERIAL_TEMPLATES.find(m => m.name === name);
            const zoneColors = { bloodpit: '#ff6666', cargo: '#6688ff', blackout: '#cc66ff', common: '#888888' };
            const zoneColor = tmpl ? (zoneColors[tmpl.zone] || '#888888') : '#888888';

            this._addObj(this.add.text(320, cy, `🔧 ${name}`, {
                fontSize: '13px',
                fontFamily: T.fontFamily || 'monospace',
                color: zoneColor,
                fontStyle: 'bold'
            }));
            this._addObj(this.add.text(520, cy, `× ${count}`, {
                fontSize: '13px',
                fontFamily: T.fontFamily || 'monospace',
                color: T.textPrimary || '#e8d8c0'
            }));
            if (tmpl) {
                this._addObj(this.add.text(580, cy, tmpl.desc, {
                    fontSize: '10px',
                    fontFamily: T.fontFamily || 'monospace',
                    color: T.textMuted || '#887860'
                }));
            }
            cy += 40;
        });
    }

    _getMaterialCounts() {
        const counts = {};
        this.gameState.storage.forEach(item => {
            if (item.type === 'material') {
                counts[item.name] = (counts[item.name] || 0) + 1;
            }
        });
        return counts;
    }

    _getTotalMaterialCount() {
        return this.gameState.storage.filter(i => i.type === 'material').length;
    }

    _canCraft(recipe, matCounts) {
        if (this.gameState.gold < recipe.goldCost) return false;
        let totalConsumed = 0;
        for (const [matName, need] of Object.entries(recipe.materials)) {
            if ((matCounts[matName] || 0) < need) return false;
            totalConsumed += need;
        }
        const cap = GuildManager.getStorageCapacity(this.gameState);
        if (this.gameState.storage.length - totalConsumed + 1 > cap) return false;
        return true;
    }

    _craftItem(recipe) {
        const gs = this.gameState;
        GuildManager.spendGold(gs, recipe.goldCost);

        for (const [matName, need] of Object.entries(recipe.materials)) {
            for (let i = 0; i < need; i++) {
                const matIdx = gs.storage.findIndex(it => it.type === 'material' && it.name === matName);
                if (matIdx !== -1) gs.storage.splice(matIdx, 1);
            }
        }

        const res = recipe.result;
        const rarity = recipe.resultRarity || 'uncommon';
        const statMult = ITEM_RARITY[rarity].statMult;
        const valueMult = ITEM_RARITY[rarity].valueMult;

        // 제작 전용 장비 체크
        const craftOnly = (typeof CRAFT_ONLY_EQUIPMENT !== 'undefined')
            ? CRAFT_ONLY_EQUIPMENT.find(c => c.id === recipe.id)
            : null;

        const item = {
            id: ++_itemIdCounter,
            type: 'equipment',
            rarity,
            baseId: recipe.id,
            name: res.name,
            slot: res.slot,
            desc: craftOnly ? craftOnly.desc : (res.special ? '' : ''),
            value: Math.floor(80 * valueMult),
            stats: {}
        };
        if (res.baseAtk) item.stats.atk = Math.floor(res.baseAtk * statMult);
        if (res.baseDef) item.stats.def = Math.floor(res.baseDef * statMult);
        if (res.baseHp)  item.stats.hp = Math.floor(res.baseHp * statMult);
        if (res.baseCrit) item.stats.critRate = +Math.min(0.40, res.baseCrit * statMult).toFixed(3);
        if (res.baseSpd) item.stats.moveSpeed = Math.floor(res.baseSpd * Math.min(statMult, 2.5));

        if (res.special || (craftOnly && craftOnly.special)) {
            item.special = res.special || craftOnly.special;
            item.specialDesc = craftOnly ? craftOnly.specialDesc : '';
        }
        if (recipe.craftOnly) item.isCraftOnly = true;

        StorageManager.addItem(gs, item);
        GuildManager.addMessage(gs, `🔨 ${item.name} [${ITEM_RARITY[rarity].name}] 제작 완료!`);
        SaveManager.save(gs);

        UIToast.show(this, `${item.name} 제작 완료!`, { color: '#44ff88' });
        this.goldText.setText(`${gs.gold}G`);
        this._clearContent();
        this._drawContent();
    }

    _enhanceItem(item, cost, nextRarity) {
        const gs = this.gameState;
        GuildManager.spendGold(gs, cost.gold);

        let removed = 0;
        for (let i = gs.storage.length - 1; i >= 0 && removed < cost.materials; i--) {
            if (gs.storage[i].type === 'material') {
                gs.storage.splice(i, 1);
                removed++;
            }
        }

        const oldRarName = ITEM_RARITY[item.rarity].name;
        const newMult = ITEM_RARITY[nextRarity].valueMult;
        const oldMult = ITEM_RARITY[item.rarity].valueMult;
        const ratio = newMult / oldMult;

        item.rarity = nextRarity;
        item.value = Math.floor(item.value * ratio);
        if (item.stats) {
            for (const key of Object.keys(item.stats)) {
                if (typeof item.stats[key] === 'number') {
                    item.stats[key] = key === 'critRate' || key === 'lifesteal'
                        ? +(item.stats[key] * ratio).toFixed(4)
                        : Math.floor(item.stats[key] * ratio);
                }
            }
        }

        GuildManager.addMessage(gs, `${item.name} ${oldRarName} → ${ITEM_RARITY[nextRarity].name} 강화!`);
        SaveManager.save(gs);

        UIToast.show(this, `${item.name} → [${ITEM_RARITY[nextRarity].name}] 강화 성공!`, { color: '#ffaacc' });
        this.goldText.setText(`${gs.gold}G`);
        this._clearContent();
        this._drawContent();
    }
}
