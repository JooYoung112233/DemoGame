// ── THE LAST THEATER — MAP GENERATION ─────────────────────
// 저주받은 극장의 막(Act)을 탐험하는 트리 맵 (Horizontal layout)

const MAP_CONFIG = {
    acts: [
        {
            id: 1, name: '제1막: 버려진 객석',
            floors: 7,
            enemyPool: ['audience', 'maskless', 'stage_spider', 'shadow_dancer'],
            elitePool: ['shadow_dancer'],
            boss: 'stage_director',
            shopFreq: 0.15,
            eventFreq: 0.12,
        },
        {
            id: 2, name: '제2막: 왕의 비극',
            floors: 7,
            enemyPool: ['mad_king', 'laughing_soldier', 'singing_skull', 'clapping_ghost'],
            elitePool: ['mad_king'],
            boss: 'tyrant',
            shopFreq: 0.15,
            eventFreq: 0.12,
        },
    ]
};

const NODE_TYPES = {
    battle:   { icon: '⚔️', name: '공연',   color: 0xff4444 },
    elite:    { icon: '💀', name: '비극',   color: 0xff8800 },
    shop:     { icon: '🏪', name: '소품실', color: 0xffcc00 },
    treasure: { icon: '💎', name: '유물',   color: 0x44ccff },
    rest:     { icon: '🕯️', name: '분장실', color: 0x44ff88 },
    event:    { icon: '❓', name: '즉흥극', color: 0xcc88ff },
    boss:     { icon: '👹', name: '주연',   color: 0xff2222 },
};

class MapGenerator {
    static generate(actIndex) {
        const act = MAP_CONFIG.acts[actIndex];
        const floors = act.floors;
        const map = [];

        // floor 0: starting node (single)
        map.push([{ id: 'start', type: 'start', floor: 0, col: 0, connections: [] }]);

        // floors 1..(floors-1): branching nodes, max 3 per floor
        for (let f = 1; f < floors; f++) {
            const nodeCount = (f === 1 || f === floors - 1) ? 3 : Phaser.Math.Between(2, 3);
            const row = [];
            for (let c = 0; c < nodeCount; c++) {
                const type = this._pickNodeType(f, floors, act);
                row.push({
                    id: `${actIndex}_${f}_${c}`,
                    type: type,
                    floor: f,
                    col: c,
                    connections: [],
                    visited: false
                });
            }
            map.push(row);
        }

        // last floor: boss
        map.push([{
            id: `${actIndex}_boss`,
            type: 'boss',
            floor: floors,
            col: 0,
            connections: [],
            visited: false
        }]);

        this._connectFloors(map);
        return map;
    }

    static _pickNodeType(floor, totalFloors, act) {
        if (floor === 1) return 'battle';
        if (floor === totalFloors - 1) {
            const r = Math.random();
            if (r < 0.4) return 'rest';
            if (r < 0.7) return 'shop';
            return 'battle';
        }

        const r = Math.random();
        if ((floor === 3 || floor === 5) && r < 0.2) return 'treasure';
        let cum = 0.2;
        if (r < cum + act.shopFreq) return 'shop';
        cum += act.shopFreq;
        if (r < cum + act.eventFreq) return 'event';
        cum += act.eventFreq;
        if (r < cum + 0.12) return 'elite';
        cum += 0.12;
        if (r < cum + 0.08) return 'rest';
        return 'battle';
    }

    static _connectFloors(map) {
        for (let f = 0; f < map.length - 1; f++) {
            const curr = map[f];
            const next = map[f + 1];

            if (curr.length === 1) {
                for (const n of next) curr[0].connections.push(n.id);
            } else if (next.length === 1) {
                for (const c of curr) c.connections.push(next[0].id);
            } else {
                const incoming = new Set();
                for (let ci = 0; ci < curr.length; ci++) {
                    const minNext = Math.min(ci, next.length - 1);
                    const maxNext = Math.min(ci + 1, next.length - 1);
                    for (let ni = minNext; ni <= maxNext; ni++) {
                        if (!curr[ci].connections.includes(next[ni].id)) {
                            curr[ci].connections.push(next[ni].id);
                            incoming.add(ni);
                        }
                    }
                }
                for (let ni = 0; ni < next.length; ni++) {
                    if (!incoming.has(ni)) {
                        const ci = Math.min(ni, curr.length - 1);
                        curr[ci].connections.push(next[ni].id);
                    }
                }
            }
        }
    }

    static findNode(map, nodeId) {
        for (const row of map) {
            for (const node of row) {
                if (node.id === nodeId) return node;
            }
        }
        return null;
    }
}
