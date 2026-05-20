class SafeHouseScene extends Phaser.Scene {
    constructor() { super('SafeHouseScene'); }

    init(data) {
        this.stash = data.stash || [];
        this.gold = data.gold || 0;
        this.equipment = data.equipment || { weapon: null, armor: null, consumable1: null, consumable2: null, consumable3: null };
        this.safe = data.safe !== undefined ? data.safe : true;
        this.extracted = data.extracted || false;
        this.lootValue = data.lootValue || 0;
        this.activeTab = 'depart';
        this.MAX_STASH = 20;
    }

    create() {
        const cam = this.cameras.main;
        this.W = cam.width;
        this.H = cam.height;
        this.cameras.main.setBackgroundColor(0x0e0e1e);
        this.cameras.main.fadeIn(400);

        if (this.extracted) {
            const sold = this.stash.reduce((s, id) => s + (ITEM_DATA[id]?.value || 0), 0);
            this.gold += sold;
        }

        this.contentContainer = this.add.container(0, 0);
        this.drawChrome();
        if (this.extracted) this.showExtractResult();
        else if (!this.safe) this.showFailResult();
        this.showTab('depart');
    }

    /* ─── persistent chrome: title, gold, tabs ─── */
    drawChrome() {
        const bg = this.add.graphics();
        bg.fillStyle(0x151525, 1);
        bg.fillRoundedRect(this.W * 0.02, this.H * 0.11, this.W * 0.96, this.H * 0.87, 12);
        bg.lineStyle(1, 0x333355, 0.4);
        bg.strokeRoundedRect(this.W * 0.02, this.H * 0.11, this.W * 0.96, this.H * 0.87, 12);

        this.add.text(this.W / 2, this.H * 0.035, '\u{1F3E0} 안전가옥', {
            fontSize: `${Math.floor(this.H * 0.038)}px`, fontFamily: 'monospace', color: '#ffffff', stroke: '#000', strokeThickness: 3
        }).setOrigin(0.5);

        this.goldText = this.add.text(this.W / 2, this.H * 0.075, '', {
            fontSize: `${Math.floor(this.H * 0.02)}px`, fontFamily: 'monospace', color: '#ffcc44'
        }).setOrigin(0.5);
        this.refreshGold();

        const tabs = [
            { id: 'depart', label: '출발' }, { id: 'stash', label: '보관함' },
            { id: 'equip', label: '장비' }, { id: 'trader', label: '상인' },
            { id: 'quests', label: '의뢰' }, { id: 'craft', label: '작업대' }
        ];
        const tabW = this.W * 0.13;
        const startX = this.W / 2 - (tabs.length * tabW) / 2;
        this.tabButtons = [];
        tabs.forEach((t, i) => {
            const tx = startX + i * tabW + tabW / 2;
            const btn = this.add.text(tx, this.H * 0.125, t.label, {
                fontSize: `${Math.floor(this.H * 0.02)}px`, fontFamily: 'monospace', color: '#888',
                backgroundColor: '#1a1a2e', padding: { x: 12, y: 5 }
            }).setOrigin(0.5).setInteractive({ useHandCursor: true });
            btn.tabId = t.id;
            btn.on('pointerdown', () => this.showTab(t.id));
            this.tabButtons.push(btn);
        });
    }

    refreshGold() { this.goldText.setText(`💰 보유 골드: ${this.gold}G`); }

    showTab(id) {
        this.activeTab = id;
        this.contentContainer.removeAll(true);
        this.tabButtons.forEach(b => {
            b.setColor(b.tabId === id ? '#44aaff' : '#888');
            b.setBackgroundColor(b.tabId === id ? '#222244' : '#1a1a2e');
        });
        const area = { x: this.W * 0.04, y: this.H * 0.17, w: this.W * 0.92, h: this.H * 0.78 };
        switch (id) {
            case 'depart': this.drawDepart(area); break;
            case 'stash': this.drawStash(area); break;
            case 'equip': this.drawEquip(area); break;
            case 'trader': this.drawTrader(area); break;
            case 'quests': this.drawPlaceholder(area, '의뢰'); break;
            case 'craft': this.drawPlaceholder(area, '작업대'); break;
        }
    }

    /* helper: add to contentContainer */
    ct(obj) { this.contentContainer.add(obj); return obj; }
    makePanel(x, y, w, h) {
        const g = this.add.graphics();
        g.fillStyle(0x1a1a2e, 0.9); g.fillRoundedRect(x, y, w, h, 8);
        g.lineStyle(1, 0x333355, 0.5); g.strokeRoundedRect(x, y, w, h, 8);
        return this.ct(g);
    }
    fs(ratio) { return `${Math.floor(this.H * ratio)}px`; }

    /* ═══════════ TAB: Depart ═══════════ */
    drawDepart(a) {
        const stats = this.calcStats();
        const infoH = this.H * 0.1;
        this.makePanel(a.x, a.y, a.w, infoH);
        this.ct(this.add.text(a.x + 15, a.y + 8, '\u{1F464} 탐험가 상태', {
            fontSize: this.fs(0.018), fontFamily: 'monospace', color: '#44aaff', fontStyle: 'bold'
        }));
        const wpnName = this.equipment.weapon ? EQUIPMENT_DATA[this.equipment.weapon]?.name : '맨손';
        this.ct(this.add.text(a.x + 15, a.y + infoH * 0.45, `HP: ${stats.hp}  ATK: ${stats.atk}  DEF: ${stats.def}  무기: ${wpnName}`, {
            fontSize: this.fs(0.016), fontFamily: 'monospace', color: '#aaa'
        }));

        const zoneY = a.y + infoH + this.H * 0.02;
        const zoneH = a.h - infoH - this.H * 0.04;
        this.makePanel(a.x, zoneY, a.w, zoneH);
        this.ct(this.add.text(a.x + 15, zoneY + 8, '\u{1F5FA} 원정지 선택', {
            fontSize: this.fs(0.02), fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold'
        }));

        const zones = Object.values(ZONE_DATA);
        const gap = a.w * 0.015;
        const cardW = Math.floor((a.w - 30 - (zones.length - 1) * gap) / zones.length);
        const headerH = this.H * 0.05;
        zones.forEach((zone, i) => {
            const cx = a.x + 15 + i * (cardW + gap);
            const cy = zoneY + headerH;
            const ch = zoneH - headerH - 12;
            const card = this.add.graphics();
            card.fillStyle(0x222244, 1); card.fillRoundedRect(cx, cy, cardW, ch, 6);
            card.lineStyle(1, 0x444466, 0.5); card.strokeRoundedRect(cx, cy, cardW, ch, 6);
            this.ct(card);
            this.ct(this.add.text(cx + cardW / 2, cy + ch * 0.07, zone.name, {
                fontSize: this.fs(0.022), fontFamily: 'monospace', color: '#fff', fontStyle: 'bold'
            }).setOrigin(0.5));
            this.ct(this.add.text(cx + cardW / 2, cy + ch * 0.18, '★'.repeat(zone.difficulty) + '☆'.repeat(3 - zone.difficulty), {
                fontSize: this.fs(0.02), fontFamily: 'monospace', color: '#ffcc44'
            }).setOrigin(0.5));
            this.ct(this.add.text(cx + cardW / 2, cy + ch * 0.30, zone.desc, {
                fontSize: this.fs(0.014), fontFamily: 'monospace', color: '#999', wordWrap: { width: cardW - 16 }, align: 'center'
            }).setOrigin(0.5, 0));
            this.ct(this.add.text(cx + cardW / 2, cy + ch * 0.62, `적: ${zone.enemyCount.min}~${zone.enemyCount.max}  맵: ${zone.mapWidth}x${zone.mapHeight}`, {
                fontSize: this.fs(0.013), fontFamily: 'monospace', color: '#666'
            }).setOrigin(0.5));
            const btn = this.ct(this.add.text(cx + cardW / 2, cy + ch - ch * 0.1, '[ 출발 ]', {
                fontSize: this.fs(0.022), fontFamily: 'monospace', color: '#44ff88',
                backgroundColor: '#1a2a1a', padding: { x: 14, y: 5 }
            }).setOrigin(0.5).setInteractive({ useHandCursor: true }));
            btn.on('pointerover', () => btn.setColor('#66ff99'));
            btn.on('pointerout', () => btn.setColor('#44ff88'));
            btn.on('pointerdown', () => this.startExpedition(zone.id));
        });
    }

    /* ═══════════ TAB: Stash ═══════════ */
    drawStash(a) {
        this.makePanel(a.x, a.y, a.w, a.h);
        const counts = {}; this.stash.forEach(id => { counts[id] = (counts[id] || 0) + 1; });
        this.ct(this.add.text(a.x + 15, a.y + 8, `\u{1F4E6} 보관함  (${this.stash.length}/${this.MAX_STASH})`, {
            fontSize: this.fs(0.02), fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }));
        if (this.stash.length === 0) {
            this.ct(this.add.text(a.x + a.w / 2, a.y + a.h / 2, '비어있음 — 원정에서 전리품을 가져오세요', {
                fontSize: this.fs(0.018), fontFamily: 'monospace', color: '#555'
            }).setOrigin(0.5));
            return;
        }
        const cols = 4, cellW = Math.floor((a.w - 30) / cols), cellH = this.H * 0.08;
        let idx = 0;
        Object.entries(counts).forEach(([id, count]) => {
            const item = ITEM_DATA[id] || EQUIPMENT_DATA[id];
            if (!item) return;
            const col = idx % cols, row = Math.floor(idx / cols);
            const cx = a.x + 15 + col * cellW, cy = a.y + this.H * 0.06 + row * cellH;
            if (cy + cellH > a.y + a.h) return;
            const cell = this.ct(this.add.graphics());
            cell.fillStyle(0x222244, 1); cell.fillRoundedRect(cx, cy, cellW - 6, cellH - 4, 4);
            this.ct(this.add.text(cx + 8, cy + 4, `${item.icon || ''} ${item.name}${count > 1 ? ' x' + count : ''}`, {
                fontSize: this.fs(0.015), fontFamily: 'monospace', color: '#ccc'
            }));
            this.ct(this.add.text(cx + 8, cy + cellH * 0.55, `${item.value || 0}G`, {
                fontSize: this.fs(0.012), fontFamily: 'monospace', color: '#888'
            }));
            const hitArea = this.ct(this.add.zone(cx + (cellW - 6) / 2, cy + (cellH - 4) / 2, cellW - 6, cellH - 4).setInteractive({ useHandCursor: true }));
            hitArea.on('pointerdown', () => this.showStashItemMenu(id, cx, cy));
            idx++;
        });
    }

    showStashItemMenu(itemId, px, py) {
        if (this.itemMenu) this.itemMenu.destroy();
        const menu = this.add.container(0, 0).setDepth(100);
        this.itemMenu = menu;
        const bg = this.add.graphics(); bg.fillStyle(0x222244, 1); bg.fillRoundedRect(px, py, 160, 100, 6);
        bg.lineStyle(1, 0x44aaff, 0.6); bg.strokeRoundedRect(px, py, 160, 100, 6);
        menu.add(bg);
        const item = ITEM_DATA[itemId] || EQUIPMENT_DATA[itemId];
        const isEquipable = EQUIPMENT_DATA[itemId];
        const btns = [];
        if (isEquipable) btns.push({ label: '장착', fn: () => this.equipFromStash(itemId) });
        btns.push({ label: `판매 (${Math.floor((item?.value || 0) / 2)}G)`, fn: () => this.sellItem(itemId) });
        btns.push({ label: '삭제', fn: () => this.discardItem(itemId) });
        btns.forEach((b, i) => {
            const t = this.add.text(px + 10, py + 10 + i * 28, b.label, {
                fontSize: this.fs(0.016), fontFamily: 'monospace', color: '#44aaff', backgroundColor: '#111', padding: { x: 6, y: 3 }
            }).setInteractive({ useHandCursor: true });
            t.on('pointerdown', () => { menu.destroy(); this.itemMenu = null; b.fn(); });
            menu.add(t);
        });
        const dismiss = this.add.zone(this.W / 2, this.H / 2, this.W, this.H).setInteractive().setDepth(99);
        dismiss.on('pointerdown', () => { menu.destroy(); dismiss.destroy(); this.itemMenu = null; });
    }

    equipFromStash(itemId) {
        const eq = EQUIPMENT_DATA[itemId];
        if (!eq) return;
        const slot = eq.type === 'weapon' ? 'weapon' : eq.type === 'armor' ? 'armor' : null;
        if (!slot) return;
        const idx = this.stash.indexOf(itemId);
        if (idx === -1) return;
        if (this.equipment[slot]) this.stash.push(this.equipment[slot]);
        this.stash.splice(idx, 1);
        this.equipment[slot] = itemId;
        this.showTab('stash');
    }

    sellItem(itemId) {
        const idx = this.stash.indexOf(itemId);
        if (idx === -1) return;
        const item = ITEM_DATA[itemId] || EQUIPMENT_DATA[itemId];
        this.gold += Math.floor((item?.value || 0) / 2);
        this.stash.splice(idx, 1);
        this.refreshGold(); this.showTab('stash');
    }

    discardItem(itemId) {
        const idx = this.stash.indexOf(itemId);
        if (idx !== -1) this.stash.splice(idx, 1);
        this.showTab('stash');
    }

    /* ═══════════ TAB: Equipment ═══════════ */
    drawEquip(a) {
        const stats = this.calcStats();
        const leftW = a.w * 0.4;
        this.makePanel(a.x, a.y, leftW, a.h);
        const sq = Math.min(leftW * 0.3, a.h * 0.25);
        const sqX = a.x + leftW / 2 - sq / 2, sqY = a.y + a.h * 0.08;
        const g = this.ct(this.add.graphics());
        g.fillStyle(PLAYER_DATA.color, 1); g.fillRoundedRect(sqX, sqY, sq, sq, 6);
        this.ct(this.add.text(a.x + leftW / 2, sqY + sq + 12, PLAYER_DATA.name, {
            fontSize: this.fs(0.022), fontFamily: 'monospace', color: '#fff', fontStyle: 'bold'
        }).setOrigin(0.5));
        const statLines = [
            `HP: ${stats.hp}/${stats.maxHp}`, `ATK: ${stats.atk}`, `DEF: ${stats.def}`,
            `Range: ${stats.attackRange}`, `Cooldown: ${stats.attackCooldown}ms`
        ];
        statLines.forEach((line, i) => {
            this.ct(this.add.text(a.x + 20, sqY + sq + 42 + i * this.H * 0.035, line, {
                fontSize: this.fs(0.017), fontFamily: 'monospace', color: '#aaa'
            }));
        });

        const rightX = a.x + leftW + a.w * 0.02;
        const rightW = a.w - leftW - a.w * 0.02;
        this.makePanel(rightX, a.y, rightW, a.h);
        this.ct(this.add.text(rightX + 15, a.y + 8, '장비 슬롯', {
            fontSize: this.fs(0.02), fontFamily: 'monospace', color: '#44aaff', fontStyle: 'bold'
        }));
        const slots = [
            { key: 'weapon', label: '무기' }, { key: 'armor', label: '방어구' },
            { key: 'consumable1', label: '소모품 1' }, { key: 'consumable2', label: '소모품 2' }, { key: 'consumable3', label: '소모품 3' }
        ];
        const slotH = this.H * 0.085;
        slots.forEach((s, i) => {
            const sy = a.y + this.H * 0.06 + i * (slotH + 6);
            const slotG = this.ct(this.add.graphics());
            slotG.fillStyle(0x222244, 1); slotG.fillRoundedRect(rightX + 12, sy, rightW - 24, slotH, 5);
            const eqId = this.equipment[s.key];
            const eqData = eqId ? (EQUIPMENT_DATA[eqId] || ITEM_DATA[eqId]) : null;
            const nameStr = eqData ? `${eqData.icon || ''} ${eqData.name}` : '비어있음';
            const nameColor = eqData ? '#fff' : '#555';
            this.ct(this.add.text(rightX + 20, sy + 6, s.label, {
                fontSize: this.fs(0.013), fontFamily: 'monospace', color: '#888'
            }));
            this.ct(this.add.text(rightX + 20, sy + slotH * 0.5, nameStr, {
                fontSize: this.fs(0.017), fontFamily: 'monospace', color: nameColor
            }));
            if (eqData) {
                const unBtn = this.ct(this.add.text(rightX + rightW - 40, sy + slotH / 2, '✖', {
                    fontSize: this.fs(0.018), fontFamily: 'monospace', color: '#ff6644'
                }).setOrigin(0.5).setInteractive({ useHandCursor: true }));
                unBtn.on('pointerdown', () => this.unequip(s.key));
            }
        });
    }

    unequip(slotKey) {
        if (!this.equipment[slotKey]) return;
        if (this.stash.length >= this.MAX_STASH) return;
        this.stash.push(this.equipment[slotKey]);
        this.equipment[slotKey] = null;
        this.showTab('equip');
    }

    /* ═══════════ TAB: Trader ═══════════ */
    drawTrader(a) {
        const halfW = (a.w - a.w * 0.02) / 2;

        /* sell panel */
        this.makePanel(a.x, a.y, halfW, a.h);
        this.ct(this.add.text(a.x + 15, a.y + 8, '판매', {
            fontSize: this.fs(0.02), fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }));
        const counts = {}; this.stash.forEach(id => { counts[id] = (counts[id] || 0) + 1; });
        let row = 0;
        const rowH = this.H * 0.045;
        Object.entries(counts).forEach(([id, count]) => {
            const item = ITEM_DATA[id] || EQUIPMENT_DATA[id];
            if (!item) return;
            const ry = a.y + this.H * 0.055 + row * rowH;
            if (ry + rowH > a.y + a.h) return;
            const price = Math.floor(item.value / 2);
            const t = this.ct(this.add.text(a.x + 15, ry, `${item.icon} ${item.name}${count > 1 ? ' x' + count : ''} → ${price}G`, {
                fontSize: this.fs(0.015), fontFamily: 'monospace', color: '#ccc'
            }).setInteractive({ useHandCursor: true }));
            t.on('pointerdown', () => { this.sellItem(id); this.showTab('trader'); });
            t.on('pointerover', () => t.setColor('#ffcc44'));
            t.on('pointerout', () => t.setColor('#ccc'));
            row++;
        });
        if (row === 0) {
            this.ct(this.add.text(a.x + 15, a.y + a.h * 0.4, '판매할 아이템 없음', {
                fontSize: this.fs(0.016), fontFamily: 'monospace', color: '#555'
            }));
        }

        /* buy panel */
        const bx = a.x + halfW + a.w * 0.02;
        this.makePanel(bx, a.y, halfW, a.h);
        this.ct(this.add.text(bx + 15, a.y + 8, '구매', {
            fontSize: this.fs(0.02), fontFamily: 'monospace', color: '#44ff88', fontStyle: 'bold'
        }));
        const shopItems = ['pipe', 'bat', 'jacket', 'vest', 'bandage', 'medkit', 'painkillers'];
        shopItems.forEach((id, i) => {
            const eq = EQUIPMENT_DATA[id]; const it = ITEM_DATA[id];
            const data = eq || it; if (!data) return;
            const ry = a.y + this.H * 0.055 + i * rowH;
            if (ry + rowH > a.y + a.h) return;
            const canBuy = this.gold >= data.value && this.stash.length < this.MAX_STASH;
            const t = this.ct(this.add.text(bx + 15, ry, `${data.icon} ${data.name}  ${data.value}G`, {
                fontSize: this.fs(0.015), fontFamily: 'monospace', color: canBuy ? '#aaffaa' : '#555'
            }));
            if (canBuy) {
                t.setInteractive({ useHandCursor: true });
                t.on('pointerdown', () => { this.buyItem(id, data.value); this.showTab('trader'); });
                t.on('pointerover', () => t.setColor('#44ff88'));
                t.on('pointerout', () => t.setColor('#aaffaa'));
            }
        });
    }

    buyItem(id, price) {
        if (this.gold < price || this.stash.length >= this.MAX_STASH) return;
        this.gold -= price;
        this.stash.push(id);
        this.refreshGold();
    }

    /* ═══════════ TAB: Placeholder ═══════════ */
    drawPlaceholder(a, name) {
        this.makePanel(a.x, a.y, a.w, a.h);
        this.ct(this.add.text(a.x + a.w / 2, a.y + a.h * 0.4, `${name} — 준비 중...`, {
            fontSize: this.fs(0.03), fontFamily: 'monospace', color: '#555'
        }).setOrigin(0.5));
        this.ct(this.add.text(a.x + a.w / 2, a.y + a.h * 0.52, '추후 업데이트 예정', {
            fontSize: this.fs(0.016), fontFamily: 'monospace', color: '#444'
        }).setOrigin(0.5));
    }

    /* ═══════════ Overlays ═══════════ */
    showExtractResult() {
        const panel = this.add.container(0, 0).setDepth(200);
        const overlay = this.add.graphics();
        overlay.fillStyle(0x000000, 0.7); overlay.fillRect(0, 0, this.W, this.H);
        panel.add(overlay);
        const fs = this.H * 0.05;
        panel.add(this.add.text(this.W / 2, this.H * 0.22, '✅ 탈출 성공!', {
            fontSize: `${Math.floor(fs)}px`, fontFamily: 'monospace', color: '#44ff88', stroke: '#000', strokeThickness: 4
        }).setOrigin(0.5));
        const counts = {}; this.stash.forEach(id => { counts[id] = (counts[id] || 0) + 1; });
        const lines = Object.entries(counts).slice(0, 8).map(([id, c]) => {
            const item = ITEM_DATA[id]; return `${item?.icon || ''} ${item?.name || id} x${c}  (${(item?.value || 0) * c}G)`;
        });
        panel.add(this.add.text(this.W / 2, this.H * 0.32, '── 회수한 전리품 ──', {
            fontSize: `${Math.floor(fs * 0.4)}px`, fontFamily: 'monospace', color: '#aaa'
        }).setOrigin(0.5));
        panel.add(this.add.text(this.W / 2, this.H * 0.37, lines.join('\n') || '없음', {
            fontSize: `${Math.floor(fs * 0.35)}px`, fontFamily: 'monospace', color: '#ccc', align: 'center', lineSpacing: 4
        }).setOrigin(0.5, 0));
        panel.add(this.add.text(this.W / 2, this.H * 0.68, `총 획득: ${this.gold}G`, {
            fontSize: `${Math.floor(fs * 0.5)}px`, fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5));
        const btn = this.add.text(this.W / 2, this.H * 0.76, '[ 확인 ]', {
            fontSize: `${Math.floor(fs * 0.45)}px`, fontFamily: 'monospace', color: '#44aaff',
            backgroundColor: '#111a2a', padding: { x: 24, y: 8 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });
        panel.add(btn);
        btn.on('pointerdown', () => { this.stash = []; panel.destroy(); this.showTab('depart'); });
    }

    showFailResult() {
        const msg = this.add.text(this.W / 2, this.H * 0.95,
            '⚠ 긴급 철수 — 전리품 전부 소실', {
            fontSize: this.fs(0.02), fontFamily: 'monospace', color: '#ff6644', stroke: '#000', strokeThickness: 2
        }).setOrigin(0.5);
        this.tweens.add({ targets: msg, alpha: 0, delay: 4000, duration: 500 });
    }

    /* ═══════════ Stats / Expedition ═══════════ */
    calcStats() {
        const base = PLAYER_DATA;
        const wpn = this.equipment.weapon ? EQUIPMENT_DATA[this.equipment.weapon] : null;
        const arm = this.equipment.armor ? EQUIPMENT_DATA[this.equipment.armor] : null;
        return {
            hp: base.hp, maxHp: base.maxHp,
            atk: base.atk + (wpn?.atk || 0),
            def: base.def + (arm?.def || 0),
            attackRange: wpn?.range || base.attackRange,
            attackCooldown: wpn?.cooldown || base.attackCooldown
        };
    }

    startExpedition(zoneId) {
        const stats = this.calcStats();
        const expData = {
            zone: zoneId, inventory: [], stash: this.stash, gold: this.gold,
            playerState: { ...stats, equipment: { ...this.equipment } }
        };
        this.cameras.main.fadeOut(400, 0, 0, 0);
        this.time.delayedCall(450, () => {
            game.scene.stop('SafeHouseScene');
            game.scene.start('ExpeditionScene', expData);
        });
    }
}
