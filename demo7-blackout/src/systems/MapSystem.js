class MapSystem {
    constructor() {
        this.gridSize = 5;
        this.grid = [];
        this.revealed = [];
        this.playerPos = { x: 0, y: 2 };
        this.exitPos = { x: 4, y: 2 };
        this.roomsVisited = 0;
        this.generateMap();
    }

    generateMap() {
        for (let y = 0; y < this.gridSize; y++) {
            this.grid[y] = [];
            this.revealed[y] = [];
            for (let x = 0; x < this.gridSize; x++) {
                this.grid[y][x] = { type: 'unknown', visited: false, depth: x };
                this.revealed[y][x] = false;
            }
        }

        this.grid[2][0] = { type: 'entrance', visited: true, depth: 0 };
        this.revealed[2][0] = true;

        this.grid[this.exitPos.y][this.exitPos.x] = { type: 'exit', visited: false, depth: 4 };

        const lockedX = 2 + Math.floor(Math.random() * 2);
        const lockedY = Math.floor(Math.random() * this.gridSize);
        if (!(lockedX === this.exitPos.x && lockedY === this.exitPos.y)) {
            this.grid[lockedY][lockedX] = { type: 'locked', visited: false, depth: lockedX };
        }

        this.revealAdjacent(this.playerPos.x, this.playerPos.y);
    }

    revealAdjacent(px, py) {
        const dirs = [[-1,0],[1,0],[0,-1],[0,1]];
        dirs.forEach(([dx, dy]) => {
            const nx = px + dx, ny = py + dy;
            if (nx >= 0 && nx < this.gridSize && ny >= 0 && ny < this.gridSize) {
                this.revealed[ny][nx] = true;
            }
        });
    }

    isAdjacent(x, y) {
        const dx = Math.abs(x - this.playerPos.x);
        const dy = Math.abs(y - this.playerPos.y);
        return (dx + dy) === 1;
    }

    moveToRoom(x, y, tension) {
        if (!this.isAdjacent(x, y)) return null;

        this.playerPos = { x, y };
        this.roomsVisited++;
        const room = this.grid[y][x];
        room.visited = true;
        this.revealAdjacent(x, y);

        if (room.type === 'unknown') {
            room.type = rollRoomEvent(x, tension);
        }

        return room;
    }

    getDepth() {
        return this.playerPos.x;
    }

    isAtExit() {
        return this.playerPos.x === this.exitPos.x && this.playerPos.y === this.exitPos.y;
    }

    shuffleExit() {
        let newY;
        do { newY = Math.floor(Math.random() * this.gridSize); } while (newY === this.exitPos.y);
        this.grid[this.exitPos.y][this.exitPos.x] = { type: 'empty', visited: this.grid[this.exitPos.y][this.exitPos.x].visited, depth: this.exitPos.x };
        this.exitPos = { x: 4, y: newY };
        this.grid[newY][4] = { type: 'exit', visited: false, depth: 4 };
    }
}
