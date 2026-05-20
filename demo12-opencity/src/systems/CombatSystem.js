// Minimal combat system for MVP
// Player: melee attack (click near enemy) + ranged if has ammo
// Enemies: contact damage

class CombatSystem {
    constructor() {
        this.playerAttackCooldown = 0;
        this.attackCooldownTime = 0.5; // seconds between attacks
        this.meleeRange = 1.5;         // tiles
        this.meleeDamage = 15;
        this.knockbackForce = 3;       // tiles
    }

    update(delta) {
        if (this.playerAttackCooldown > 0) {
            this.playerAttackCooldown -= delta / 1000;
        }
    }

    canAttack() {
        return this.playerAttackCooldown <= 0;
    }

    // Player attacks toward a direction
    playerAttack(playerRow, playerCol, enemies) {
        if (!this.canAttack()) return null;

        this.playerAttackCooldown = this.attackCooldownTime;

        // Find closest enemy in melee range
        let closest = null;
        let closestDist = this.meleeRange;

        for (const enemy of enemies) {
            if (enemy.dead) continue;
            const dist = Math.sqrt(
                Math.pow(enemy.row - playerRow, 2) +
                Math.pow(enemy.col - playerCol, 2)
            );
            if (dist < closestDist) {
                closestDist = dist;
                closest = enemy;
            }
        }

        if (closest) {
            closest.hp -= this.meleeDamage;
            // Knockback
            const dx = closest.col - playerCol;
            const dy = closest.row - playerRow;
            const len = Math.sqrt(dx * dx + dy * dy) || 1;
            closest.knockbackX = (dx / len) * this.knockbackForce;
            closest.knockbackY = (dy / len) * this.knockbackForce;

            return { hit: true, enemy: closest, damage: this.meleeDamage };
        }

        return { hit: false };
    }

    // Check enemy contact damage
    checkEnemyContact(playerRow, playerCol, enemies) {
        for (const enemy of enemies) {
            if (enemy.dead) continue;
            const dist = Math.sqrt(
                Math.pow(enemy.row - playerRow, 2) +
                Math.pow(enemy.col - playerCol, 2)
            );
            if (dist < 0.8) {
                return {
                    damage: ENEMY_DATA[enemy.type].damage,
                    enemy
                };
            }
        }
        return null;
    }

    reset() {
        this.playerAttackCooldown = 0;
    }
}
