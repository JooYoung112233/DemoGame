class CombatSystem {
    constructor(scene) {
        this.scene = scene;
        this.characters = [];
        this.battleOver = false;
        this.battleLog = [];
    }

    addCharacter(char) {
        this.characters.push(char);
    }

    update(delta) {
        if (this.battleOver) return;

        this.characters.forEach(char => {
            char.update(delta, this.characters);
        });

        this.checkBattleEnd();
    }

    checkBattleEnd() {
        const alliesAlive = this.characters.filter(c => c.team === 'ally' && c.alive);
        const enemiesAlive = this.characters.filter(c => c.team === 'enemy' && c.alive);

        if (enemiesAlive.length === 0) {
            this.battleOver = true;
            this.scene.onBattleEnd(true);
        } else if (alliesAlive.length === 0) {
            this.battleOver = true;
            this.scene.onBattleEnd(false);
        }
    }

    getStats() {
        return this.characters.map(c => ({
            name: c.name,
            team: c.team,
            alive: c.alive,
            hp: c.hp,
            maxHp: c.maxHp,
            damageDealt: c.totalDamageDealt,
            healDone: c.totalHealDone
        }));
    }

    destroy() {
        this.characters.forEach(c => c.destroy());
        this.characters = [];
    }
}
