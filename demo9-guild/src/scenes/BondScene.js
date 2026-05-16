/**
 * BondScene — 모든 용병 간 본드 관계 그래프.
 * 노드 = 용병 (살아있는 자 + 사망자), 엣지 = 본드 (티어 1+).
 * 호버: 티어, XP 상세.
 */
class BondScene extends Phaser.Scene {
    constructor() { super('BondScene'); }

    init(data) { this.gameState = data.gameState; }

    create() {
        this.add.rectangle(640, 360, 1280, 720, 0x0a0a14);
        this._drawUI();
    }

    _drawUI() {
        const gs = this.gameState;

        this.add.text(640, 22, '💞 용병 관계도', {
            fontSize: '20px', fontFamily: 'monospace', color: '#ff88cc', fontStyle: 'bold'
        }).setOrigin(0.5);

        UIButton.create(this, 80, 22, 100, 30, '← 마을', {
            color: 0x334455, hoverColor: 0x445566, textColor: '#aaaacc', fontSize: 12,
            onClick: () => this.scene.start('TownScene', { gameState: gs })
        });

        // 범례 (티어 색)
        this.add.text(1260, 22, '클릭: 용병 상세', {
            fontSize: '11px', fontFamily: 'monospace', color: '#888899'
        }).setOrigin(1, 0);

        const tierColors = ['#666677', '#88ccaa', '#88ddff', '#ffaa66', '#ff88cc', '#ffcc44'];
        const tierNames = ['낯선', '동료', '형제', '전우', '동반자', '운명'];
        let lx = 200;
        this.add.text(lx, 55, '티어:', { fontSize: '11px', fontFamily: 'monospace', color: '#aaaacc' });
        lx += 50;
        tierNames.forEach((name, i) => {
            if (i === 0) return; // 0티어는 표시 안 함
            this.add.text(lx, 55, `${i}${name}`, {
                fontSize: '11px', fontFamily: 'monospace', color: tierColors[i], fontStyle: 'bold'
            });
            lx += 75;
        });

        const allMercs = [...gs.roster, ...(gs.fallenMercs || [])];
        if (allMercs.length === 0) {
            this.add.text(640, 360, '용병이 없습니다', {
                fontSize: '14px', fontFamily: 'monospace', color: '#666677'
            }).setOrigin(0.5);
            return;
        }

        this._drawGraph(allMercs);
    }

    _drawGraph(mercs) {
        const gs = this.gameState;
        const cx = 640, cy = 410;
        const radius = 260;

        // 노드 위치 계산 — 원형 배치
        const nodes = mercs.map((m, i) => {
            const angle = (Math.PI * 2 * i / mercs.length) - Math.PI / 2;
            return {
                merc: m,
                x: cx + Math.cos(angle) * radius,
                y: cy + Math.sin(angle) * radius,
                angle
            };
        });

        // === 엣지 (본드) 먼저 그림 ===
        const tierColorsHex = [0x666677, 0x88ccaa, 0x88ddff, 0xffaa66, 0xff88cc, 0xffcc44];
        const edges = [];
        for (let i = 0; i < nodes.length; i++) {
            for (let j = i + 1; j < nodes.length; j++) {
                const xp = (typeof BondManager !== 'undefined')
                    ? BondManager.getBondXp(gs, nodes[i].merc.id, nodes[j].merc.id) : 0;
                if (xp <= 0) continue;
                const tier = BondManager.getTier(xp);
                edges.push({ from: nodes[i], to: nodes[j], tier, xp });
            }
        }

        // 낮은 티어부터 그려서 높은 티어가 위에 오게
        edges.sort((a, b) => a.tier.tier - b.tier.tier);
        edges.forEach(e => {
            const color = tierColorsHex[e.tier.tier] || 0x666677;
            const alpha = 0.3 + e.tier.tier * 0.15;
            const thickness = 1 + e.tier.tier;
            const line = this.add.graphics();
            line.lineStyle(thickness, color, alpha);
            line.lineBetween(e.from.x, e.from.y, e.to.x, e.to.y);

            // 중간에 XP 라벨 (티어 3+만)
            if (e.tier.tier >= 3) {
                const mx = (e.from.x + e.to.x) / 2;
                const my = (e.from.y + e.to.y) / 2;
                this.add.text(mx, my, `${e.xp}`, {
                    fontSize: '10px', fontFamily: 'monospace',
                    color: '#ffffff', fontStyle: 'bold',
                    backgroundColor: '#000000', padding: { x: 3, y: 1 }
                }).setOrigin(0.5);
            }
        });

        // === 노드 그림 ===
        nodes.forEach(n => {
            const m = n.merc;
            const base = m.getBaseClass ? m.getBaseClass() : { icon: '?', name: '' };
            const rarity = (typeof RARITY_DATA !== 'undefined' && RARITY_DATA[m.rarity]) || { color: 0x888888, textColor: '#aaaaaa' };
            const isAlive = m.alive !== false;

            // 본드 그래프 — 노드 원
            const bg = this.add.graphics();
            bg.fillStyle(isAlive ? 0x1a1a2e : 0x2a1a1a, 1);
            bg.fillCircle(n.x, n.y, 32);
            bg.lineStyle(2, rarity.color, isAlive ? 0.9 : 0.4);
            bg.strokeCircle(n.x, n.y, 32);

            // 아이콘 + 이름
            this.add.text(n.x, n.y - 6, base.icon || '?', {
                fontSize: '20px', fontFamily: 'monospace'
            }).setOrigin(0.5).setAlpha(isAlive ? 1 : 0.4);

            this.add.text(n.x, n.y + 14, m.name + (isAlive ? '' : ' ☠'), {
                fontSize: '10px', fontFamily: 'monospace',
                color: isAlive ? rarity.textColor : '#888888', fontStyle: 'bold',
                stroke: '#000', strokeThickness: 2
            }).setOrigin(0.5);

            // 클릭 — 로스터 상세로
            const hit = this.add.zone(n.x, n.y, 64, 64).setInteractive({ useHandCursor: isAlive });
            if (isAlive) {
                hit.on('pointerdown', () => {
                    this.scene.start('RosterScene', { gameState: gs, selectedMercId: m.id });
                });
            }
            hit.on('pointerover', () => this._showMercTooltip(n, m));
            hit.on('pointerout', () => this._hideTooltip());
        });

        // 본드 없으면 안내
        if (edges.length === 0) {
            this.add.text(cx, cy, '아직 본드 누적 없음\n파티로 같이 출전해보세요', {
                fontSize: '13px', fontFamily: 'monospace', color: '#666677',
                align: 'center'
            }).setOrigin(0.5);
        }
    }

    _showMercTooltip(node, merc) {
        this._hideTooltip();
        const gs = this.gameState;
        const allMercs = [...gs.roster, ...(gs.fallenMercs || [])];
        const bonds = (typeof BondManager !== 'undefined')
            ? BondManager.getBondsForMerc(gs, merc.id, allMercs).slice(0, 6)
            : [];

        const w = 280, h = 30 + bonds.length * 16 + (bonds.length === 0 ? 20 : 0);
        let tx = node.x + 50, ty = node.y;
        if (tx + w > 1270) tx = node.x - 50 - w;
        if (ty + h > 700) ty = 700 - h;

        const objs = [];
        const bg = this.add.graphics().setDepth(80);
        bg.fillStyle(0x000000, 0.92);
        bg.fillRoundedRect(tx, ty, w, h, 5);
        bg.lineStyle(2, 0xff88cc, 0.8);
        bg.strokeRoundedRect(tx, ty, w, h, 5);
        objs.push(bg);

        objs.push(this.add.text(tx + 10, ty + 8, `${merc.name} 의 본드`, {
            fontSize: '12px', fontFamily: 'monospace', color: '#ff88cc', fontStyle: 'bold'
        }).setDepth(81));

        const tierColors = ['#666677', '#88ccaa', '#88ddff', '#ffaa66', '#ff88cc', '#ffcc44'];
        if (bonds.length === 0) {
            objs.push(this.add.text(tx + 12, ty + 30, '아직 본드 없음', {
                fontSize: '10px', fontFamily: 'monospace', color: '#666677'
            }).setDepth(81));
        } else {
            bonds.forEach((b, i) => {
                const c = tierColors[b.tier.tier] || '#888888';
                objs.push(this.add.text(tx + 12, ty + 28 + i * 16, `${b.tier.name}: ${b.otherName} (${b.xp})`, {
                    fontSize: '10px', fontFamily: 'monospace', color: c
                }).setDepth(81));
            });
        }
        this._tooltipObjs = objs;
    }

    _hideTooltip() {
        if (this._tooltipObjs) {
            this._tooltipObjs.forEach(o => o.destroy && o.destroy());
            this._tooltipObjs = null;
        }
    }
}
