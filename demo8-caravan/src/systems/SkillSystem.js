class SkillSystem {
    static getSkillsForUnit(unit) {
        const data = UNIT_DATA[unit.classKey];
        return data.skills || [];
    }

    static getSkillsForEnemy(enemyKey) {
        const data = ENEMY_DATA[enemyKey];
        return data.skills || [];
    }
}
