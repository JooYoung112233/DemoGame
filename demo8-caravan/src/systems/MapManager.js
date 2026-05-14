class MapManager {
    constructor() {
        this.nodes = NODE_CONFIG;
    }

    getNode(index) {
        return this.nodes[index] || null;
    }

    getNodeCount() {
        return this.nodes.length;
    }

    getEnemyScaling(nodeIndex) {
        return 1 + nodeIndex * 0.15;
    }

    getScaledEnemies(nodeIndex) {
        const node = this.nodes[nodeIndex];
        if (!node || !node.enemies) return [];
        const scale = this.getEnemyScaling(nodeIndex);

        return node.enemies.map(key => {
            const base = ENEMY_DATA[key];
            if (!base) return null;
            return {
                name: base.name,
                hp: Math.floor(base.hp * scale),
                atk: Math.floor(base.atk * scale),
                def: Math.floor(base.def * scale),
                attackSpeed: base.attackSpeed,
                range: base.range,
                moveSpeed: base.moveSpeed,
                critRate: base.critRate,
                critDmg: base.critDmg,
                color: base.color,
                role: base.role,
                skills: base.skills || [],
                enemyKey: key
            };
        }).filter(Boolean);
    }
}
