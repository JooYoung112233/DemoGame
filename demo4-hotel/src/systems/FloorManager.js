class FloorManager {
    constructor() {
        this.currentFloor = 0;
        this.maxFloor = 10;
    }

    nextFloor() {
        this.currentFloor++;
        return this.currentFloor;
    }

    getFloorType(floor) {
        if (floor < 1 || floor > this.maxFloor) return 'normal';
        return FLOOR_CONFIG.floorTypes[floor - 1];
    }

    getScale() {
        return FLOOR_CONFIG.getScale(this.currentFloor);
    }

    getFloorEnemies() {
        const floor = this.currentFloor;
        const type = this.getFloorType(floor);
        const scale = this.getScale();

        if (type === 'boss') {
            return { type: 'boss', scale };
        }

        if (type === 'elite') {
            const comp = FLOOR_CONFIG.eliteFloors[floor];
            if (comp) return { type: 'elite', enemies: this.buildEnemyList(comp), scale };
        }

        if (type === 'event' || type === 'shop') {
            return { type };
        }

        const configFloor = Object.keys(FLOOR_CONFIG.normalFloors)
            .map(Number)
            .filter(f => f <= floor)
            .sort((a, b) => b - a)[0] || 1;

        return {
            type: 'normal',
            enemies: this.buildEnemyList(FLOOR_CONFIG.normalFloors[configFloor]),
            scale
        };
    }

    buildEnemyList(composition) {
        const list = [];
        for (const entry of composition) {
            const baseData = HOTEL_ENEMIES[entry.type];
            if (!baseData) continue;

            for (let i = 0; i < entry.count; i++) {
                list.push({
                    ...baseData,
                    isElite: entry.elite || false
                });
            }
        }
        return list;
    }

    getBossAdds() {
        return this.buildEnemyList(FLOOR_CONFIG.bossFloor.adds);
    }

    isLastFloor() {
        return this.currentFloor >= this.maxFloor;
    }

    getFloorLabel() {
        return FLOOR_CONFIG.getFloorLabel(this.getFloorType(this.currentFloor));
    }

    getFloorColor() {
        return FLOOR_CONFIG.getFloorColor(this.getFloorType(this.currentFloor));
    }
}
