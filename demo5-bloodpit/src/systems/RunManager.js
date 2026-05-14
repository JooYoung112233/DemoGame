class RunManager {
    constructor() {
        this.totalRounds = 20;
        this.currentRound = 0;
        this.roundPlan = [];
        this.shopDiscount = 0;
    }

    generateRunPlan() {
        this.currentRound = 0;
        this.shopDiscount = 0;
        this.roundPlan = [];

        const fixed = {
            1: 'battle',
            5: 'shop', 10: 'shop', 15: 'shop',
            8: 'forge', 16: 'forge',
            20: 'boss'
        };

        let eventCount = 0;
        const maxEvents = 3;

        for (let i = 1; i <= this.totalRounds; i++) {
            if (fixed[i]) {
                this.roundPlan.push({
                    round: i,
                    type: fixed[i],
                    dangerLevel: this._dangerForRound(i),
                    eventId: null
                });
            } else {
                const prevType = this.roundPlan.length > 0 ? this.roundPlan[this.roundPlan.length - 1].type : 'battle';
                const canEvent = eventCount < maxEvents && prevType !== 'event';
                const isEvent = canEvent && Math.random() < 0.18;
                if (isEvent) {
                    eventCount++;
                    this.roundPlan.push({
                        round: i,
                        type: 'event',
                        dangerLevel: this._dangerForRound(i),
                        eventId: this._pickEvent()
                    });
                } else {
                    this.roundPlan.push({
                        round: i,
                        type: 'battle',
                        dangerLevel: this._dangerForRound(i),
                        eventId: null
                    });
                }
            }
        }
        return this.roundPlan;
    }

    _dangerForRound(round) {
        return round;
    }

    _pickEvent() {
        const events = ['treasure_chest', 'wounded_soldier', 'mysterious_altar',
                        'merchant_ambush', 'training_ground', 'blood_fountain'];
        return events[Math.floor(Math.random() * events.length)];
    }

    advanceRound() {
        this.currentRound++;
        return this.getCurrentRound();
    }

    getCurrentRound() {
        if (this.currentRound <= 0 || this.currentRound > this.totalRounds) return null;
        return this.roundPlan[this.currentRound - 1] || null;
    }

    getRoundPreview(count) {
        count = count || 3;
        const previews = [];
        for (let i = this.currentRound; i < this.currentRound + count && i < this.totalRounds; i++) {
            previews.push(this.roundPlan[i]);
        }
        return previews;
    }

    getRoundIcon(type) {
        const icons = { battle: '⚔️', shop: '🛒', forge: '🔨', event: '❓', boss: '💀' };
        return icons[type] || '?';
    }

    getRoundLabel(type) {
        const labels = { battle: '전투', shop: '상점', forge: '대장간', event: '이벤트', boss: '보스' };
        return labels[type] || '???';
    }

    isFinished() {
        return this.currentRound >= this.totalRounds;
    }

    getBattleRoundCount() {
        let count = 0;
        for (let i = 0; i < this.currentRound; i++) {
            if (this.roundPlan[i].type === 'battle' || this.roundPlan[i].type === 'boss') count++;
        }
        return count;
    }
}
