// ── MAP GENERATION ─────────────────────────────
// Slay the Spire-style branching tree per Act

const MAP_CONFIG = {
    acts: [
        {
            id: 1, name: 'Act 1: 어둠의 입구',
            floors: 7,
            enemyPool: ['slime', 'rat', 'bat'],
            elitePool: ['goblin'],
            boss: 'goblin_chief',
            shopFreq: 0.15,
            eventFreq: 0.12,
        },
        {
            id: 2, name: 'Act 2: 깊은 지하',
            floors: 7,
            enemyPool: ['goblin', 'skeleton', 'wolf'],
            elitePool: ['orc'],
            boss: 'lich',
            shopFreq: 0.15,
            eventFreq: 0.12,
        },
        {
            id: 3, name: 'Act 3: 드래곤의 둥지',
            floors: 7,
            enemyPool: ['orc', 'mage', 'skeleton'],
            elitePool: ['mage'],
            boss: 'dragon',
            shopFreq: 0.15,
            eventFreq: 0.12,
        },
    ]
};

const NODE_TYPES = {
    battle:   { icon: '⚔️', name: '전투',   color: 0xff4444 },
    elite:    { icon: '💀', name: '강적',   color: 0xff8800 },
    shop:     { icon: '🏪', name: '상점',   color: 0xffcc00 },
    treasure: { icon: '💎', name: '보물',   color: 0x44ccff },
    rest:     { icon: '🏕️', name: '휴식',   color: 0x44ff88 },
    event:    { icon: '❓', name: '이벤트', color: 0xcc88ff },
    boss:     { icon: '👹', name: '보스',   color: 0xff2222 },
};

class MapGenerator {
    static generate(actIndex) {
        const act = MAP_CONFIG.acts[actIndex];
        const floors = act.floors;
        const map = [];

        // floor 0: starting node (single)
        map.push([{ id: 'start', type: 'start', floor: 0, col: 1, connections: [] }]);

        // floors 1..(floors-1): branching nodes
        for (let f = 1; f < floors; f++) {
            const nodeCount = (f === 1 || f === floors - 1) ? 3 : Phaser.Math.Between(2, 4);
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
            col: 1,
            connections: [],
            visited: false
        }]);

        // connect floors
        this._connectFloors(map);

        return map;
    }

    static _pickNodeType(floor, totalFloors, act) {
        // floor 1 always battle
        if (floor === 1) return 'battle';
        // floor before boss: varied
        if (floor === totalFloors - 1) {
            const r = Math.random();
            if (r < 0.4) return 'rest';
            if (r < 0.7) return 'shop';
            return 'battle';
        }

        const r = Math.random();
        let cum = 0;
        // treasure at floor 3 or 5 sometimes
        if ((floor === 3 || floor === 5) && r < 0.2) return 'treasure';
        cum = 0.2;
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
                // single node connects to all next
                for (const n of next) {
                    curr[0].connections.push(n.id);
                }
            } else if (next.length === 1) {
                // all current connect to single next
                for (const c of curr) {
                    c.connections.push(next[0].id);
                }
            } else {
                // ensure every node has at least 1 connection going forward
                // and every next node has at least 1 incoming
                const incoming = new Set();
                for (let ci = 0; ci < curr.length; ci++) {
                    // connect to at least 1-2 next nodes
                    const minNext = Math.min(ci, next.length - 1);
                    const maxNext = Math.min(ci + 1, next.length - 1);
                    for (let ni = minNext; ni <= maxNext; ni++) {
                        if (!curr[ci].connections.includes(next[ni].id)) {
                            curr[ci].connections.push(next[ni].id);
                            incoming.add(ni);
                        }
                    }
                }
                // ensure all next nodes reachable
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
