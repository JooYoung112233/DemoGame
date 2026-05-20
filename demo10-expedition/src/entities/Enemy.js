// Enemy entity with simple patrol/chase AI
class Enemy {
    constructor(type, row, col) {
        this.type = type;
        this.row = row;
        this.col = col;
        const data = ENEMY_DATA[type];
        this.hp = data.hp;
        this.maxHp = data.hp;
        this.speed = data.speed;
        this.sightRange = data.sightRange;
        this.chaseRange = data.chaseRange;
        this.color = data.color;
        this.dead = false;

        // AI state
        this.state = 'patrol'; // patrol, chase, return
        this.patrolTarget = { row, col };
        this.patrolOrigin = { row, col };
        this.patrolTimer = 0;
        this.facingAngle = Math.random() * Math.PI * 2;

        // Knockback
        this.knockbackX = 0;
        this.knockbackY = 0;

        // Alert indicator
        this.alertTimer = 0;
    }

    update(delta, playerRow, playerCol, playerVisible) {
        if (this.dead) return;

        const dt = delta / 1000;

        // Apply knockback
        if (Math.abs(this.knockbackX) > 0.1 || Math.abs(this.knockbackY) > 0.1) {
            const kbSpeed = 8 * dt;
            const newCol = this.col + this.knockbackX * kbSpeed;
            const newRow = this.row + this.knockbackY * kbSpeed;
            if (this._canMoveTo(newRow, newCol)) {
                this.col = newCol;
                this.row = newRow;
            }
            this.knockbackX *= 0.85;
            this.knockbackY *= 0.85;
            return;
        }

        const distToPlayer = Math.sqrt(
            Math.pow(this.row - playerRow, 2) +
            Math.pow(this.col - playerCol, 2)
        );

        // State transitions
        switch (this.state) {
            case 'patrol':
                if (distToPlayer <= this.sightRange && playerVisible) {
                    this.state = 'chase';
                    this.alertTimer = 0.5;
                }
                break;
            case 'chase':
                if (distToPlayer > this.chaseRange) {
                    this.state = 'return';
                }
                break;
            case 'return':
                const distToOrigin = Math.sqrt(
                    Math.pow(this.row - this.patrolOrigin.row, 2) +
                    Math.pow(this.col - this.patrolOrigin.col, 2)
                );
                if (distToOrigin < 1) {
                    this.state = 'patrol';
                }
                if (distToPlayer <= this.sightRange && playerVisible) {
                    this.state = 'chase';
                    this.alertTimer = 0.5;
                }
                break;
        }

        // Alert timer
        if (this.alertTimer > 0) {
            this.alertTimer -= dt;
            return; // Brief pause when first spotting player
        }

        // Movement based on state
        switch (this.state) {
            case 'patrol':
                this._doPatrol(dt);
                break;
            case 'chase':
                this._doChase(dt, playerRow, playerCol);
                break;
            case 'return':
                this._moveToward(dt, this.patrolOrigin.row, this.patrolOrigin.col, this.speed * 0.6);
                break;
        }
    }

    _doPatrol(dt) {
        this.patrolTimer -= dt;
        if (this.patrolTimer <= 0) {
            // Pick new random patrol point near origin
            this.patrolTarget = {
                row: this.patrolOrigin.row + (Math.random() - 0.5) * 6,
                col: this.patrolOrigin.col + (Math.random() - 0.5) * 6,
            };
            this.patrolTimer = 2 + Math.random() * 3;
        }
        this._moveToward(dt, this.patrolTarget.row, this.patrolTarget.col, this.speed * 0.4);
    }

    _doChase(dt, playerRow, playerCol) {
        this._moveToward(dt, playerRow, playerCol, this.speed);
    }

    _moveToward(dt, targetRow, targetCol, speed) {
        const dx = targetCol - this.col;
        const dy = targetRow - this.row;
        const dist = Math.sqrt(dx * dx + dy * dy);
        if (dist < 0.1) return;

        const nx = dx / dist;
        const ny = dy / dist;
        this.facingAngle = Math.atan2(ny, nx);

        const newCol = this.col + nx * speed * dt;
        const newRow = this.row + ny * speed * dt;

        if (this._canMoveTo(newRow, newCol)) {
            this.col = newCol;
            this.row = newRow;
        } else if (this._canMoveTo(this.row, newCol)) {
            this.col = newCol;
        } else if (this._canMoveTo(newRow, this.col)) {
            this.row = newRow;
        }
    }

    _canMoveTo(row, col) {
        const tr = Math.floor(row);
        const tc = Math.floor(col);
        if (tr < 0 || tr >= MAP_HEIGHT || tc < 0 || tc >= MAP_WIDTH) return false;
        return isWalkable(CITY_MAP[tr][tc]);
    }

    takeDamage(amount) {
        this.hp -= amount;
        if (this.hp <= 0) {
            this.hp = 0;
            this.dead = true;
        }
    }
}
