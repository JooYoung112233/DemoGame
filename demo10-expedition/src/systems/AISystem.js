class AISystem {
    static executeEnemyTurn(unit, turnManager, battleGrid, onAction) {
        const enemies = turnManager.getPlayerUnits();
        if (enemies.length === 0) { onAction('end'); return; }

        const behavior = unit.data.ai || 'melee_rush';
        const target = this.findTarget(unit, enemies, behavior);
        if (!target) { onAction('end'); return; }

        const dist = Math.abs(unit.gridX - target.gridX) + Math.abs(unit.gridY - target.gridY);

        if (behavior === 'ranged_kite' && dist <= 2 && unit.ap >= 1) {
            const retreatPos = this.findRetreatPosition(unit, target, battleGrid);
            if (retreatPos) {
                onAction('move', retreatPos.x, retreatPos.y, () => {
                    this.tryAttack(unit, target, battleGrid, onAction);
                });
                return;
            }
        }

        if (dist <= unit.data.range && unit.ap >= 1) {
            onAction('attack', target.gridX, target.gridY);
            return;
        }

        if (unit.ap >= 1) {
            const moveTarget = this.findMoveToward(unit, target, battleGrid);
            if (moveTarget) {
                onAction('move', moveTarget.x, moveTarget.y, () => {
                    const newDist = Math.abs(unit.gridX - target.gridX) + Math.abs(unit.gridY - target.gridY);
                    if (newDist <= unit.data.range && unit.ap >= 1) {
                        onAction('attack', target.gridX, target.gridY);
                    } else {
                        onAction('end');
                    }
                });
                return;
            }
        }

        onAction('end');
    }

    static findTarget(unit, enemies, behavior) {
        if (behavior === 'aggressive') {
            return enemies.reduce((closest, e) => {
                const d = Math.abs(unit.gridX - e.gridX) + Math.abs(unit.gridY - e.gridY);
                const cd = closest ? Math.abs(unit.gridX - closest.gridX) + Math.abs(unit.gridY - closest.gridY) : Infinity;
                return d < cd ? e : closest;
            }, null);
        }
        return enemies.reduce((weakest, e) => (!weakest || e.hp < weakest.hp) ? e : weakest, null);
    }

    static findMoveToward(unit, target, battleGrid) {
        const range = Math.min(unit.ap, unit.data.moveRange);
        let bestPos = null;
        let bestDist = Infinity;

        for (let dy = -range; dy <= range; dy++) {
            for (let dx = -range; dx <= range; dx++) {
                if (Math.abs(dx) + Math.abs(dy) > range) continue;
                const nx = unit.gridX + dx;
                const ny = unit.gridY + dy;
                if (nx < 0 || ny < 0 || ny >= battleGrid.length || nx >= battleGrid[0].length) continue;
                if (battleGrid[ny][nx] === 1) continue;
                const dist = Math.abs(nx - target.gridX) + Math.abs(ny - target.gridY);
                if (dist < bestDist) {
                    bestDist = dist;
                    bestPos = { x: nx, y: ny };
                }
            }
        }
        return bestPos;
    }

    static findRetreatPosition(unit, threat, battleGrid) {
        const range = Math.min(unit.ap, unit.data.moveRange);
        let bestPos = null;
        let bestDist = 0;

        for (let dy = -range; dy <= range; dy++) {
            for (let dx = -range; dx <= range; dx++) {
                if (Math.abs(dx) + Math.abs(dy) > range) continue;
                const nx = unit.gridX + dx;
                const ny = unit.gridY + dy;
                if (nx < 0 || ny < 0 || ny >= battleGrid.length || nx >= battleGrid[0].length) continue;
                if (battleGrid[ny][nx] === 1) continue;
                const dist = Math.abs(nx - threat.gridX) + Math.abs(ny - threat.gridY);
                if (dist > bestDist) {
                    bestDist = dist;
                    bestPos = { x: nx, y: ny };
                }
            }
        }
        return bestPos;
    }

    static tryAttack(unit, target, battleGrid, onAction) {
        const dist = Math.abs(unit.gridX - target.gridX) + Math.abs(unit.gridY - target.gridY);
        if (dist <= unit.data.range && unit.ap >= 1) {
            onAction('attack', target.gridX, target.gridY);
        } else {
            onAction('end');
        }
    }
}
