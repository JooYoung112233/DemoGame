// Tarkov-style grid inventory system
// Items occupy w x h cells in a grid

class InventorySystem {
    constructor(cols, rows, maxWeight) {
        this.cols = cols;       // 5
        this.rows = rows;       // 4
        this.maxWeight = maxWeight; // 30 kg
        this.grid = [];         // 2D array, null = empty, item ref = occupied
        this.items = [];        // list of placed items {item, col, row}
        this.currentWeight = 0;

        // Initialize empty grid
        for (let r = 0; r < rows; r++) {
            this.grid[r] = [];
            for (let c = 0; c < cols; c++) {
                this.grid[r][c] = null;
            }
        }
    }

    // Try to add item at specific position
    addItemAt(item, col, row) {
        if (!this.canPlace(item, col, row)) return false;

        const entry = { item: { ...item }, col, row, uid: Date.now() + Math.random() };
        this.items.push(entry);
        this.currentWeight += item.weight;

        // Mark grid cells
        for (let dr = 0; dr < item.h; dr++) {
            for (let dc = 0; dc < item.w; dc++) {
                this.grid[row + dr][col + dc] = entry;
            }
        }
        return true;
    }

    // Auto-place: find first available position
    autoAdd(item) {
        if (this.currentWeight + item.weight > this.maxWeight) return false;

        for (let r = 0; r < this.rows; r++) {
            for (let c = 0; c < this.cols; c++) {
                if (this.canPlace(item, c, r)) {
                    return this.addItemAt(item, c, r);
                }
            }
        }
        return false;
    }

    // Check if item can be placed at position
    canPlace(item, col, row) {
        if (this.currentWeight + item.weight > this.maxWeight) return false;
        if (col + item.w > this.cols || row + item.h > this.rows) return false;
        if (col < 0 || row < 0) return false;

        for (let dr = 0; dr < item.h; dr++) {
            for (let dc = 0; dc < item.w; dc++) {
                if (this.grid[row + dr][col + dc] !== null) return false;
            }
        }
        return true;
    }

    // Remove item
    removeItem(entry) {
        const idx = this.items.indexOf(entry);
        if (idx === -1) return false;

        // Clear grid cells
        for (let dr = 0; dr < entry.item.h; dr++) {
            for (let dc = 0; dc < entry.item.w; dc++) {
                if (this.grid[entry.row + dr] && this.grid[entry.row + dr][entry.col + dc] === entry) {
                    this.grid[entry.row + dr][entry.col + dc] = null;
                }
            }
        }

        this.items.splice(idx, 1);
        this.currentWeight -= entry.item.weight;
        return true;
    }

    // Get total value of all items
    getTotalValue() {
        return this.items.reduce((sum, e) => sum + e.item.value, 0);
    }

    // Get item count
    getItemCount() {
        return this.items.length;
    }

    // Get remaining capacity
    getRemainingWeight() {
        return this.maxWeight - this.currentWeight;
    }

    // Check if inventory is empty
    isEmpty() {
        return this.items.length === 0;
    }

    // Get all items as flat array
    getAllItems() {
        return this.items.map(e => e.item);
    }

    // Reset
    reset() {
        for (let r = 0; r < this.rows; r++) {
            for (let c = 0; c < this.cols; c++) {
                this.grid[r][c] = null;
            }
        }
        this.items = [];
        this.currentWeight = 0;
    }
}
