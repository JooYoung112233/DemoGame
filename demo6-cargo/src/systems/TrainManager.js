class TrainManager {
    constructor() {
        this.cars = LC_CARS.map((data, i) => new TrainCar(data, i));
        this.parts = 0;
        this.ammo = 10;
        this.medkits = 2;
        this.collectedDrops = [];
    }

    isGameOver() {
        return this.cars.every(c => c.destroyed);
    }

    getOperationalCount() {
        return this.cars.filter(c => c.isOperational()).length;
    }

    isPowerOn() {
        const gen = this.cars.find(c => c.id === 'generator');
        return gen && gen.isOperational();
    }

    isAmmoAvailable() {
        const ammoCar = this.cars.find(c => c.id === 'ammo');
        return ammoCar && ammoCar.isOperational();
    }

    isMedicalAvailable() {
        const med = this.cars.find(c => c.id === 'medical');
        return med && med.isOperational();
    }

    applyAutoDefense(invasionData) {
        const results = [];
        const powerOn = this.isPowerOn();

        invasionData.forEach(inv => {
            if (inv.enemies.length === 0) {
                results.push({ carIndex: inv.carIndex, defended: true, method: 'none', damage: 0 });
                return;
            }

            const car = this.cars[inv.carIndex];
            const defenseRate = powerOn ? car.getAutoDefenseRate() : 0;

            let defended = false;
            let method = '무방비';
            let damage = 0;

            if (defenseRate > 0 && Math.random() < defenseRate) {
                defended = true;
                method = car.module ? car.module.name : '자동 방어';
            } else {
                const totalAtk = inv.enemies.reduce((sum, e) => sum + e.atk, 0);
                damage = car.takeDamage(totalAtk);
                method = inv.entryType;

                inv.enemies.forEach(e => {
                    if (e.type === 'tanker' && Math.random() < 0.4) {
                        car.doorBroken = true;
                    }
                });
            }

            results.push({ carIndex: inv.carIndex, defended, method, damage, enemies: inv.enemies });
        });

        return results;
    }

    autoRepairAll() {
        let totalRepaired = 0;
        this.cars.forEach(car => {
            totalRepaired += car.autoRepairTick();
        });
        return totalRepaired;
    }

    healAllies(allies) {
        if (!this.isMedicalAvailable()) return 0;
        const med = this.cars.find(c => c.id === 'medical');
        let totalHealed = 0;
        allies.forEach(a => {
            if (!a.alive) return;
            const heal = Math.floor(a.maxHp * med.healRate);
            const actual = Math.min(heal, a.maxHp - a.hp);
            a.hp += actual;
            totalHealed += actual;
        });
        return totalHealed;
    }

    generateDrops(enemiesDefeated) {
        const drops = [];
        const cargoOk = this.cars.find(c => c.id === 'cargo')?.isOperational();
        if (!cargoOk) return drops;

        for (let i = 0; i < enemiesDefeated; i++) {
            const r = Math.random();
            if (r < 0.3) drops.push({ type: 'parts', name: '부품', amount: 1 });
            else if (r < 0.5) drops.push({ type: 'ammo', name: '탄약', amount: 2 });
            else if (r < 0.6) drops.push({ type: 'medkit', name: '의료킷', amount: 1 });
        }

        drops.forEach(d => {
            if (d.type === 'parts') this.parts += d.amount;
            else if (d.type === 'ammo') this.ammo += d.amount;
            else if (d.type === 'medkit') this.medkits += d.amount;
            this.collectedDrops.push(d);
        });

        return drops;
    }

    repairCar(carIndex) {
        if (this.parts < 2) return false;
        const car = this.cars[carIndex];
        const healed = car.repair(Math.floor(car.maxHp * 0.4));
        if (healed > 0) {
            this.parts -= 2;
            return true;
        }
        return false;
    }
}
