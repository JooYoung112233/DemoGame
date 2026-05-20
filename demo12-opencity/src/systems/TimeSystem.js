// Day/Night cycle system
class TimeSystem {
    constructor() {
        this.dayDuration = 60;     // seconds of daytime
        this.nightDuration = 120;  // seconds of nighttime
        this.elapsed = 0;          // total elapsed seconds
        this.isNight = false;
        this.nightWave = 0;        // which wave of enemies has spawned
        this.transitionAlpha = 0;  // 0=day, 1=full night (for visual lerp)
        this.paused = false;
    }

    get totalCycleDuration() {
        return this.dayDuration + this.nightDuration;
    }

    get cycleProgress() {
        const cycleTime = this.elapsed % this.totalCycleDuration;
        if (cycleTime < this.dayDuration) {
            return { phase: 'day', remaining: this.dayDuration - cycleTime, progress: cycleTime / this.dayDuration };
        } else {
            const nightElapsed = cycleTime - this.dayDuration;
            return { phase: 'night', remaining: this.nightDuration - nightElapsed, progress: nightElapsed / this.nightDuration };
        }
    }

    update(delta) {
        if (this.paused) return;

        this.elapsed += delta / 1000;
        const cycle = this.cycleProgress;
        const wasNight = this.isNight;
        this.isNight = cycle.phase === 'night';

        // Smooth transition
        if (this.isNight) {
            this.transitionAlpha = Math.min(1, this.transitionAlpha + delta / 2000);
        } else {
            this.transitionAlpha = Math.max(0, this.transitionAlpha - delta / 2000);
        }

        // Detect night start
        if (!wasNight && this.isNight) {
            this.nightWave = 0;
            return 'night_start';
        }
        // Detect day start
        if (wasNight && !this.isNight) {
            this.nightWave = 0;
            return 'day_start';
        }

        // Night wave triggers
        if (this.isNight) {
            const progress = cycle.progress;
            if (this.nightWave === 0 && progress > 0.05) {
                this.nightWave = 1;
                return 'spawn_wave1';
            }
            if (this.nightWave === 1 && progress > 0.35) {
                this.nightWave = 2;
                return 'spawn_wave2';
            }
            if (this.nightWave === 2 && progress > 0.65) {
                this.nightWave = 3;
                return 'spawn_wave3';
            }
        }

        return null;
    }

    getTimeString() {
        const cycle = this.cycleProgress;
        const remaining = Math.ceil(cycle.remaining);
        const min = Math.floor(remaining / 60);
        const sec = remaining % 60;
        const timeStr = `${min}:${sec.toString().padStart(2, '0')}`;
        return cycle.phase === 'day'
            ? `☀️ 낮 ${timeStr}`
            : `🌙 밤 ${timeStr}`;
    }

    reset() {
        this.elapsed = 0;
        this.isNight = false;
        this.nightWave = 0;
        this.transitionAlpha = 0;
        this.paused = false;
    }
}
