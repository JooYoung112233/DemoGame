/**
 * PitGauge — BP 핏 게이지 시스템 (v4).
 * 라운드 중 충전 + 33%/66% 임계점 관중 난입 트리거.
 *
 * 사용:
 *   const pg = new PitGauge(gameState);
 *   pg.onCharge(type, value?)   // 전투 중 이벤트 발생 시 호출
 *   pg.getMultiplier()          // 보상 배율 (1.0~2.0)
 */
class PitGauge {
    constructor(gameState) {
        this.gs = gameState;
        const fLevel = (gameState.guildHall && gameState.guildHall.pit_control) || 0;
        this.fLevel = fLevel;

        this.gauge = 0;
        this.max = 100;

        // F 해금 상태
        this.hasPocket    = fLevel >= 2;
        this.hasRestRoom  = fLevel >= 4;
        this.hasCrowdRush = fLevel >= 6;

        // 충전 배율 (F3 = +20%)
        this.chargeMultiplier = fLevel >= 3 ? 1.2 : 1.0;

        // F5: 엘리트 킬 추가 충전 (+10%)
        this.eliteKillBonus = fLevel >= 5 ? 0.1 : 0;

        // 관중 난입 임계점 — 한 라운드에서 같은 임계점은 1회만
        this._thresholds = [33, 66];
        this._triggeredThresholds = new Set();

        // 쉬는 곳 관련
        this._restRoomChance = 0;
        this._restRoomAppearCount = 0;
        this._maxRestRoomPerRun = 2;

        // 도파밍 난입 확률 (F7 = 10%)
        this.dopamineChance = fLevel >= 7 ? 0.10 : 0.05;
        this.dopamineBonus  = fLevel >= 7 ? 1.5 : 1.0;

        // 보스전 난입 확률 (F9 = ×2)
        this.bossRushChance = fLevel >= 9 ? 0.30 : 0.15;

        // F10: 광전사 전장
        this.berserkerField = fLevel >= 10;

        // F11: 쉬는곳 HP 회복 강화
        this.restHealPct = fLevel >= 11 ? 0.40 : 0.20;
        this.restReviveHpPct = fLevel >= 11 ? 0.25 : 0.01;

        // F12: 피의 군주
        if (fLevel >= 12) {
            this.gauge = 50;
            this.rushRewardMult = 2.0;
        } else {
            this.rushRewardMult = 1.0;
        }

        // 관중 난입 결과 추적
        this.rushKills = 0;
        this.rushMisses = 0;
    }

    // --- 충전 이벤트 ---

    static CHARGE_TABLE = {
        crit:           3,
        bleed_apply:    1,
        burn_apply:     1,
        debuff_apply:   0.5,
        skill_use:      5,
        kill_normal:    5,
        kill_elite:     10,
        kill_boss:      20,
        round_clear:    10
    };

    onCharge(type, _value) {
        const base = PitGauge.CHARGE_TABLE[type] || 0;
        if (base <= 0) return null;
        let amount = base * this.chargeMultiplier;
        if ((type === 'kill_elite' || type === 'kill_boss') && this.eliteKillBonus > 0) {
            amount += this.max * this.eliteKillBonus;
        }
        const prev = this.gauge;
        this.gauge = Math.min(this.max, this.gauge + amount);

        // 임계점 체크
        let triggered = null;
        if (this.hasCrowdRush) {
            for (const t of this._thresholds) {
                if (prev < t && this.gauge >= t && !this._triggeredThresholds.has(t)) {
                    this._triggeredThresholds.add(t);
                    triggered = t;
                    break;
                }
            }
        }
        return triggered;
    }

    resetThresholdsForNewRound() {
        this._triggeredThresholds.clear();
    }

    // --- 보상 배율 ---

    getMultiplier() {
        return 1.0 + (this.gauge / 100);
    }

    // --- 관중 난입 적 결정 ---

    decideCrowdRush(zoneLevel, currentRound, isBossRound) {
        if (!this.hasCrowdRush) return null;

        if (isBossRound) {
            if (Math.random() >= this.bossRushChance) return null;
        }

        const isDopamine = Math.random() < this.dopamineChance;
        const threshold = [...this._triggeredThresholds].pop() || 33;
        const is66 = threshold >= 66;

        if (isDopamine) {
            return {
                type: 'dopamine',
                enemies: [{ key: 'crowd_fodder', scaleMult: 0.3 + Math.random() * 0.2 }],
                lootBonus: 3.0 * this.dopamineBonus
            };
        }

        // 도전적 난입 — 구역 레벨별 스케일링
        let enemies;
        if (zoneLevel <= 3) {
            enemies = is66
                ? [{ key: 'crowd_fodder', scaleMult: 0.5 }, { key: 'crowd_fodder', scaleMult: 0.5 }]
                : [{ key: 'crowd_fodder', scaleMult: 0.7 }];
        } else if (zoneLevel <= 6) {
            enemies = is66
                ? [{ key: 'crowd_fodder', scaleMult: 0.5 }, { key: 'crowd_assassin', scaleMult: 0.7 }]
                : [{ key: 'crowd_fodder', scaleMult: 0.5 }, { key: 'crowd_fodder', scaleMult: 0.5 }];
        } else if (zoneLevel <= 9) {
            enemies = is66
                ? [{ key: 'crowd_assassin', scaleMult: 0.8 }, { key: 'crowd_challenger', scaleMult: 1.0 }]
                : [{ key: 'crowd_assassin', scaleMult: 0.8 }];
        } else {
            enemies = is66
                ? [{ key: 'crowd_challenger', scaleMult: 1.1 }]
                : [{ key: 'crowd_assassin', scaleMult: 0.8 }, { key: 'crowd_assassin', scaleMult: 0.8 }];
        }
        return { type: 'challenge', enemies, lootBonus: 0 };
    }

    // 난입 적 처치/미처치 결과 반영
    onRushKill(count) {
        const bonus = 5 * count * this.rushRewardMult;
        this.gauge = Math.min(this.max, this.gauge + bonus);
        this.rushKills += count;
    }

    onRushMiss(count) {
        this.gauge = Math.max(0, this.gauge - 10 * count);
        this.rushMisses += count;
    }

    // --- 쉬는 곳 ---

    shouldShowRestRoom(zoneLevel) {
        if (!this.hasRestRoom) return false;
        if (this._restRoomAppearCount >= this._maxRestRoomPerRun) return false;

        const baseRate = zoneLevel <= 4 ? 0.10 : zoneLevel <= 7 ? 0.07 : 0.05;
        const incRate  = zoneLevel <= 4 ? 0.05 : zoneLevel <= 7 ? 0.04 : 0.03;

        if (this._restRoomChance === 0) this._restRoomChance = baseRate;

        if (Math.random() < this._restRoomChance) {
            this._restRoomAppearCount++;
            const zl = zoneLevel;
            this._restRoomChance = zl <= 4 ? 0.10 : zl <= 7 ? 0.07 : 0.05;
            return true;
        }

        this._restRoomChance = Math.min(0.50, this._restRoomChance + incRate);
        return false;
    }

    // --- 광전사 전장 (F10) ---

    getBerserkerBonus() {
        if (!this.berserkerField || this.gauge < 70) return null;
        return { atkMult: 1.25, defMult: 0.85 };
    }
}
