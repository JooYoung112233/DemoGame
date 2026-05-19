class SafeHouseScene extends Phaser.Scene {
    constructor() { super('SafeHouseScene'); }

    init(data) {
        this.stash = data.stash || [];
        this.party = data.party || ['scout','fighter','medic','marksman'];
        this.safe = data.safe !== undefined ? data.safe : true;
        this.lootValue = data.lootValue || 0;
        this.extracted = data.extracted || false;
        this.gold = data.gold || 0;
    }

    create() {
        const cam = this.cameras.main;
        this.W = cam.width;
        this.H = cam.height;
        this.PAD = Math.floor(this.W * 0.03);
        this.cameras.main.setBackgroundColor(0x0e0e1e);
        this.cameras.main.fadeIn(400);

        if (this.extracted) {
            const sold = this.stash.reduce((s, id) => s + (ITEM_DATA[id]?.value || 0), 0);
            this.gold += sold;
        }

        this.drawBackground();

        this.add.text(this.W / 2, Math.floor(this.H * 0.04), '🏠 안전가옥', {
            fontSize: `${Math.floor(this.H * 0.04)}px`, fontFamily: 'monospace', color: '#ffffff', stroke: '#000', strokeThickness: 3
        }).setOrigin(0.5);

        this.goldText = this.add.text(this.W / 2, Math.floor(this.H * 0.08), `💰 보유 골드: ${this.gold}G`, {
            fontSize: `${Math.floor(this.H * 0.02)}px`, fontFamily: 'monospace', color: '#ffcc44'
        }).setOrigin(0.5);

        if (this.extracted) this.showExtractResult();
        else if (!this.safe) this.showFailResult();

        const topY = Math.floor(this.H * 0.13);
        const panelH = Math.floor(this.H * 0.32);
        const halfW = Math.floor((this.W - this.PAD * 3) / 2);
        this.drawPartyPanel(this.PAD, topY, halfW, panelH);
        this.drawStashPanel(this.PAD * 2 + halfW, topY, halfW, panelH);

        const zoneY = topY + panelH + Math.floor(this.H * 0.02);
        const zoneH = Math.floor(this.H * 0.30);
        this.drawZoneSelection(this.PAD, zoneY, this.W - this.PAD * 2, zoneH);

        const tipY = zoneY + zoneH + Math.floor(this.H * 0.015);
        const tipH = Math.floor(this.H * 0.09);
        this.drawTips(this.PAD, tipY, this.W - this.PAD * 2, tipH);
    }

    drawBackground() {
        const g = this.add.graphics();
        const topY = Math.floor(this.H * 0.11);
        g.fillStyle(0x151525, 1);
        g.fillRoundedRect(this.PAD - 5, topY, this.W - (this.PAD - 5) * 2, this.H - topY - Math.floor(this.H * 0.02), 12);
        g.lineStyle(1, 0x333355, 0.4);
        g.strokeRoundedRect(this.PAD - 5, topY, this.W - (this.PAD - 5) * 2, this.H - topY - Math.floor(this.H * 0.02), 12);
    }

    showExtractResult() {
        const panel = this.add.container(0, 0).setDepth(200);
        const overlay = this.add.graphics();
        overlay.fillStyle(0x000000, 0.7);
        overlay.fillRect(0, 0, this.W, this.H);
        panel.add(overlay);

        const fs = Math.floor(this.H * 0.05);
        panel.add(this.add.text(this.W / 2, this.H * 0.25, '✅ 탈출 성공!', {
            fontSize: `${fs}px`, fontFamily: 'monospace', color: '#44ff88', stroke: '#000', strokeThickness: 4
        }).setOrigin(0.5));

        const stashCounts = {};
        this.stash.forEach(id => { stashCounts[id] = (stashCounts[id] || 0) + 1; });
        const lines = Object.entries(stashCounts).slice(0, 8).map(([id, count]) => {
            const item = ITEM_DATA[id];
            return `${item?.icon || ''} ${item?.name || id} x${count}  (${(item?.value || 0) * count}G)`;
        });

        panel.add(this.add.text(this.W / 2, this.H * 0.35, '── 회수한 전리품 ──', {
            fontSize: `${Math.floor(fs * 0.4)}px`, fontFamily: 'monospace', color: '#aaa'
        }).setOrigin(0.5));

        panel.add(this.add.text(this.W / 2, this.H * 0.40, lines.join('\n') || '없음', {
            fontSize: `${Math.floor(fs * 0.35)}px`, fontFamily: 'monospace', color: '#ccc', align: 'center', lineSpacing: 4
        }).setOrigin(0.5, 0));

        panel.add(this.add.text(this.W / 2, this.H * 0.70, `총 획득: ${this.gold}G`, {
            fontSize: `${Math.floor(fs * 0.5)}px`, fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold'
        }).setOrigin(0.5));

        const btn = this.add.text(this.W / 2, this.H * 0.78, '[ 확인 ]', {
            fontSize: `${Math.floor(fs * 0.45)}px`, fontFamily: 'monospace', color: '#44aaff', backgroundColor: '#111a2a', padding: { x: 24, y: 8 }
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });
        panel.add(btn);
        btn.on('pointerdown', () => { this.stash = []; panel.destroy(); });
    }

    showFailResult() {
        const msg = this.add.text(this.W / 2, this.H * 0.95,
            '⚠ 긴급 철수 — 전리품 전부 소실', {
            fontSize: `${Math.floor(this.H * 0.02)}px`, fontFamily: 'monospace', color: '#ff6644', stroke: '#000', strokeThickness: 2
        }).setOrigin(0.5);
        this.tweens.add({ targets: msg, alpha: 0, delay: 4000, duration: 500 });
    }

    drawPartyPanel(x, y, w, h) {
        const g = this.add.graphics();
        g.fillStyle(0x1a1a2e, 0.8);
        g.fillRoundedRect(x, y, w, h, 8);
        g.lineStyle(1, 0x333355, 0.4);
        g.strokeRoundedRect(x, y, w, h, 8);

        const fs = Math.floor(this.H * 0.02);
        this.add.text(x + 15, y + 10, '🎖 원정대', { fontSize: `${fs}px`, fontFamily: 'monospace', color: '#44aaff', fontStyle: 'bold' });

        const slotH = Math.floor((h - 45) / Math.max(this.party.length, 1));
        this.party.forEach((id, i) => {
            const data = PARTY_DATA[id];
            if (!data) return;
            const py = y + 38 + i * slotH;
            const iconS = Math.floor(slotH * 0.55);

            const icon = this.add.graphics();
            icon.fillStyle(data.color, 1);
            icon.fillRoundedRect(x + 15, py, iconS, iconS, 3);

            this.add.text(x + 20 + iconS + 8, py + 2, data.name, { fontSize: `${Math.floor(fs * 0.85)}px`, fontFamily: 'monospace', color: '#fff' });
            this.add.text(x + 20 + iconS + 8, py + Math.floor(slotH * 0.4), `HP:${data.hp} ATK:${data.atk} DEF:${data.def} SPD:${data.spd} RNG:${data.range}`, {
                fontSize: `${Math.floor(fs * 0.65)}px`, fontFamily: 'monospace', color: '#777'
            });
        });
    }

    drawStashPanel(x, y, w, h) {
        const g = this.add.graphics();
        g.fillStyle(0x1a1a2e, 0.8);
        g.fillRoundedRect(x, y, w, h, 8);
        g.lineStyle(1, 0x333355, 0.4);
        g.strokeRoundedRect(x, y, w, h, 8);

        const fs = Math.floor(this.H * 0.02);
        this.add.text(x + 15, y + 10, '📦 보관함', { fontSize: `${fs}px`, fontFamily: 'monospace', color: '#ffcc44', fontStyle: 'bold' });

        const counts = {};
        this.stash.forEach(id => { counts[id] = (counts[id] || 0) + 1; });

        const rowH = Math.floor(fs * 1.5);
        const maxRows = Math.floor((h - 45) / rowH);
        let row = 0;
        Object.entries(counts).forEach(([id, count]) => {
            if (row >= maxRows) return;
            const item = ITEM_DATA[id];
            if (!item) return;
            this.add.text(x + 15, y + 38 + row * rowH, `${item.icon} ${item.name} x${count}`, {
                fontSize: `${Math.floor(fs * 0.8)}px`, fontFamily: 'monospace', color: '#ccc'
            });
            row++;
        });

        if (this.stash.length === 0) {
            this.add.text(x + 15, y + 45, '비어있음 — 원정에서 전리품을 가져오세요', {
                fontSize: `${Math.floor(fs * 0.7)}px`, fontFamily: 'monospace', color: '#555'
            });
        }
    }

    drawZoneSelection(x, y, w, h) {
        const g = this.add.graphics();
        g.fillStyle(0x1a1a2e, 0.8);
        g.fillRoundedRect(x, y, w, h, 8);
        g.lineStyle(1, 0x333355, 0.4);
        g.strokeRoundedRect(x, y, w, h, 8);

        const fs = Math.floor(this.H * 0.02);
        this.add.text(x + 15, y + 10, '🗺 원정지 선택', { fontSize: `${fs}px`, fontFamily: 'monospace', color: '#ff8844', fontStyle: 'bold' });

        const zones = Object.values(ZONE_DATA);
        const gap = Math.floor(w * 0.015);
        const cardW = Math.floor((w - 30 - (zones.length - 1) * gap) / zones.length);
        const headerH = Math.floor(fs * 1.8);

        zones.forEach((zone, i) => {
            const cx = x + 15 + i * (cardW + gap);
            const cy = y + headerH;
            const ch = h - headerH - 12;

            const card = this.add.graphics();
            card.fillStyle(0x222244, 1);
            card.fillRoundedRect(cx, cy, cardW, ch, 6);
            card.lineStyle(1, 0x444466, 0.5);
            card.strokeRoundedRect(cx, cy, cardW, ch, 6);

            this.add.text(cx + cardW / 2, cy + Math.floor(ch * 0.08), zone.name, {
                fontSize: `${Math.floor(fs * 0.9)}px`, fontFamily: 'monospace', color: '#fff', fontStyle: 'bold'
            }).setOrigin(0.5);

            this.add.text(cx + cardW / 2, cy + Math.floor(ch * 0.22), '★'.repeat(zone.difficulty) + '☆'.repeat(3 - zone.difficulty), {
                fontSize: `${Math.floor(fs * 0.85)}px`, fontFamily: 'monospace', color: '#ffcc44'
            }).setOrigin(0.5);

            this.add.text(cx + cardW / 2, cy + Math.floor(ch * 0.35), zone.desc, {
                fontSize: `${Math.floor(fs * 0.6)}px`, fontFamily: 'monospace', color: '#999', wordWrap: { width: cardW - 16 }, align: 'center'
            }).setOrigin(0.5, 0);

            this.add.text(cx + cardW / 2, cy + Math.floor(ch * 0.65), `맵: ${zone.mapWidth}x${zone.mapHeight}  적: ${zone.enemyCount.min}~${zone.enemyCount.max}`, {
                fontSize: `${Math.floor(fs * 0.6)}px`, fontFamily: 'monospace', color: '#666'
            }).setOrigin(0.5);

            const btn = this.add.text(cx + cardW / 2, cy + ch - Math.floor(ch * 0.12), '[ 출발 ]', {
                fontSize: `${Math.floor(fs * 0.9)}px`, fontFamily: 'monospace', color: '#44ff88',
                backgroundColor: '#1a2a1a', padding: { x: 14, y: 5 }
            }).setOrigin(0.5).setInteractive({ useHandCursor: true });

            btn.on('pointerover', () => { btn.setColor('#66ff99'); card.clear(); card.fillStyle(0x2a2a55, 1); card.fillRoundedRect(cx, cy, cardW, ch, 6); card.lineStyle(2, 0x44ff88, 0.5); card.strokeRoundedRect(cx, cy, cardW, ch, 6); });
            btn.on('pointerout', () => { btn.setColor('#44ff88'); card.clear(); card.fillStyle(0x222244, 1); card.fillRoundedRect(cx, cy, cardW, ch, 6); card.lineStyle(1, 0x444466, 0.5); card.strokeRoundedRect(cx, cy, cardW, ch, 6); });
            btn.on('pointerdown', () => this.startExpedition(zone.id));
        });
    }

    drawTips(x, y, w, h) {
        const g = this.add.graphics();
        g.fillStyle(0x1a1a2e, 0.5);
        g.fillRoundedRect(x, y, w, h, 8);

        const fs = Math.floor(this.H * 0.015);
        this.add.text(x + 15, y + Math.floor(h * 0.2), '💡 팁: WASD로 이동, E키로 컨테이너 수색/탈출, TAB으로 인벤토리, M으로 전체맵. 탈출 지점(🚁)에 도착해야 전리품을 가져올 수 있습니다!', {
            fontSize: `${fs}px`, fontFamily: 'monospace', color: '#666', wordWrap: { width: w - 30 }
        });
    }

    startExpedition(zoneId) {
        this.cameras.main.fadeOut(400, 0, 0, 0);
        const self = this;
        setTimeout(() => {
            self.scene.start('ExpeditionScene', {
                zone: zoneId, party: self.party, inventory: [], stash: self.stash
            });
        }, 450);
    }
}
