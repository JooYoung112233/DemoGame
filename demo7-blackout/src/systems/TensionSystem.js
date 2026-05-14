class TensionSystem {
    constructor() {
        this.tension = 0;
        this.maxTension = 5;
        this.roomsMoved = 0;
        this.exitShuffled = false;
    }

    onRoomMove() {
        this.roomsMoved++;
        if (this.roomsMoved % 4 === 0 && this.tension < this.maxTension) {
            this.tension++;
        }
    }

    reduceTension(amount) {
        this.tension = Math.max(0, this.tension - amount);
    }

    getEnemyScale() {
        return 1 + this.tension * 0.12;
    }

    getEnemyChanceBonus() {
        return this.tension * 0.05;
    }

    shouldShuffleExit() {
        if (this.exitShuffled) return false;
        if (this.tension >= 4) {
            this.exitShuffled = true;
            return true;
        }
        return false;
    }

    getTensionName() {
        if (this.tension <= 1) return '평온';
        if (this.tension <= 2) return '불안';
        if (this.tension <= 3) return '위험';
        if (this.tension <= 4) return '극도 위험';
        return '공포';
    }

    getTensionColor() {
        if (this.tension <= 1) return '#44ff88';
        if (this.tension <= 2) return '#ffcc44';
        if (this.tension <= 3) return '#ff8844';
        if (this.tension <= 4) return '#ff4444';
        return '#ff0000';
    }
}
