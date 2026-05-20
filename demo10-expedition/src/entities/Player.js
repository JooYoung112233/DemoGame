// Player entity
class Player {
    constructor(row, col) {
        this.row = row;
        this.col = col;
        this.hp = 100;
        this.maxHp = 100;
        this.speed = 4;  // tiles per second
        this.facingAngle = 0; // radians, 0 = right
        this.invincibleTimer = 0;
        this.isMoving = false;
        this.moveDir = { x: 0, y: 0 };
    }

    update(delta, cursors, wasd) {
        const dt = delta / 1000;
        let dx = 0, dy = 0;

        if (cursors.left.isDown || wasd.A.isDown) dx -= 1;
        if (cursors.right.isDown || wasd.D.isDown) dx += 1;
        if (cursors.up.isDown || wasd.W.isDown) dy -= 1;
        if (cursors.down.isDown || wasd.S.isDown) dy += 1;

        this.isMoving = dx !== 0 || dy !== 0;

        if (this.isMoving) {
            // Normalize diagonal
            const len = Math.sqrt(dx * dx + dy * dy);
            dx /= len;
            dy /= len;

            this.moveDir.x = dx;
            this.moveDir.y = dy;
            this.facingAngle = Math.atan2(dy, dx);

            const newCol = this.col + dx * this.speed * dt;
            const newRow = this.row + dy * this.speed * dt;

            // Collision check — try each axis independently
            if (this._canMoveTo(this.row, newCol)) {
                this.col = newCol;
            }
            if (this._canMoveTo(newRow, this.col)) {
                this.row = newRow;
            }
        }

        // Invincibility timer
        if (this.invincibleTimer > 0) {
            this.invincibleTimer -= dt;
        }
    }

    _canMoveTo(row, col) {
        // Center-point collision only — axis-separated movement handles wall sliding
        const tr = Math.floor(row);
        const tc = Math.floor(col);
        if (tr < 0 || tr >= MAP_HEIGHT || tc < 0 || tc >= MAP_WIDTH) return false;
        return isWalkable(CITY_MAP[tr][tc]);
    }

    takeDamage(amount) {
        if (this.invincibleTimer > 0) return 0;
        this.hp -= amount;
        this.invincibleTimer = 0.5; // brief invincibility
        if (this.hp < 0) this.hp = 0;
        return amount;
    }

    heal(amount) {
        this.hp = Math.min(this.maxHp, this.hp + amount);
    }

    get isDead() {
        return this.hp <= 0;
    }

    get tileRow() {
        return Math.round(this.row);
    }

    get tileCol() {
        return Math.round(this.col);
    }
}
