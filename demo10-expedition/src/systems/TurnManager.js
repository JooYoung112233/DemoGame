class TurnManager {
    constructor() {
        this.units = [];
        this.currentIndex = 0;
        this.turnNumber = 1;
        this.phase = 'player';
        this.onTurnChange = null;
    }

    init(playerUnits, enemyUnits) {
        this.units = [...playerUnits, ...enemyUnits].sort((a, b) => b.data.spd - a.data.spd);
        this.currentIndex = 0;
        this.turnNumber = 1;
        this.updatePhase();
    }

    get currentUnit() {
        return this.units[this.currentIndex] || null;
    }

    updatePhase() {
        const unit = this.currentUnit;
        if (unit) this.phase = unit.isPlayer ? 'player' : 'enemy';
    }

    nextUnit() {
        this.units = this.units.filter(u => u.alive);
        if (this.units.length === 0) return null;

        this.currentIndex++;
        if (this.currentIndex >= this.units.length) {
            this.currentIndex = 0;
            this.turnNumber++;
            this.units.forEach(u => u.resetAP());
        }

        while (this.currentIndex < this.units.length && !this.units[this.currentIndex].alive) {
            this.currentIndex++;
        }
        if (this.currentIndex >= this.units.length) {
            this.currentIndex = 0;
            this.turnNumber++;
            this.units.forEach(u => u.resetAP());
        }

        this.updatePhase();
        if (this.onTurnChange) this.onTurnChange(this.currentUnit, this.turnNumber);
        return this.currentUnit;
    }

    isPlayerTurn() {
        return this.phase === 'player';
    }

    getPlayerUnits() {
        return this.units.filter(u => u.isPlayer && u.alive);
    }

    getEnemyUnits() {
        return this.units.filter(u => !u.isPlayer && u.alive);
    }

    isBattleOver() {
        const playersAlive = this.units.some(u => u.isPlayer && u.alive);
        const enemiesAlive = this.units.some(u => !u.isPlayer && u.alive);
        if (!playersAlive) return 'defeat';
        if (!enemiesAlive) return 'victory';
        return null;
    }
}
