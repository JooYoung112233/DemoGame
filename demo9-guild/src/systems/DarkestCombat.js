/**
 * DarkestCombat — 다키스트 던전 스타일 전투 엔진.
 * 4명 일렬 + SPD 턴제 + 위치 기반 액션.
 *
 * unit 객체 구조 (전투용):
 *  - id, name, classKey, alive, position (1-4), team ('ally' | 'enemy')
 *  - hp, maxHp, atk, def, spd, critRate, critDmg
 *  - cooldowns: { 'warrior_skill': 0 }  // 0 = ready
 *  - statusEffects: [{ type, duration, value }]
 *  - actions: [actionId, ...]  (아군만)
 */
class DarkestCombat {
    /**
     * 전투 상태 생성.
     */
    static createCombat(allies, enemies) {
        return {
            allies: allies.map((a, i) => DarkestCombat._makeUnit(a, i + 1, 'ally')),
            enemies: enemies.map((e, i) => DarkestCombat._makeUnit(e, i + 1, 'enemy')),
            round: 1,
            turnQueue: [],   // 라운드 시작 시 생성
            currentTurnIdx: 0
        };
    }

    static _makeUnit(source, position, team) {
        const stats = source.getStats ? source.getStats() : source;
        const hp = source.currentHp !== undefined ? source.currentHp : (stats.hp || 100);
        const maxHp = stats.hp || hp;
        const classKey = source.classKey || source.type || 'warrior';

        return {
            ref: source,
            id: source.id || `${team}_${position}`,
            name: source.name || `${team}${position}`,
            classKey,
            team,
            position,
            alive: true,
            hp, maxHp,
            atk: stats.atk || 10,
            def: stats.def || 0,
            spd: stats.moveSpeed || stats.spd || 100,
            critRate: stats.critRate || 0.05,
            critDmg: stats.critDmg || 1.5,
            cooldowns: {},
            statusEffects: [],
            actions: team === 'ally' ? (typeof getClassActions === 'function' ? getClassActions(classKey).map(a => a.id) : []) : null
        };
    }

    /**
     * 라운드 시작 — 턴 큐 생성 (SPD 순).
     */
    static startRound(combat) {
        const all = [...combat.allies, ...combat.enemies].filter(u => u.alive);
        // SPD 높은 순. 같은 SPD는 랜덤
        all.sort((a, b) => (b.spd - a.spd) + (Math.random() - 0.5));
        combat.turnQueue = all;
        combat.currentTurnIdx = 0;

        // 상태 효과 라운드 처리
        all.forEach(u => DarkestCombat._tickStatusEffectsStartOfRound(u));

        // 쿨다운 감소
        all.forEach(u => {
            Object.keys(u.cooldowns).forEach(k => {
                if (u.cooldowns[k] > 0) u.cooldowns[k]--;
            });
        });
    }

    static getCurrentTurnUnit(combat) {
        while (combat.currentTurnIdx < combat.turnQueue.length) {
            const u = combat.turnQueue[combat.currentTurnIdx];
            if (u.alive) return u;
            combat.currentTurnIdx++;
        }
        return null;
    }

    static advanceTurn(combat) {
        combat.currentTurnIdx++;
        return DarkestCombat.getCurrentTurnUnit(combat);
    }

    static isRoundDone(combat) {
        return combat.currentTurnIdx >= combat.turnQueue.length;
    }

    /**
     * 액션 실행. caster가 action을 targets에 사용.
     * targets: position number[] (적/아군 포지션)
     */
    static executeAction(combat, casterUnit, actionId, targetPositions) {
        const action = ACTION_DATA[actionId];
        if (!action) return { error: 'unknown_action' };

        // 캐스터 위치 체크
        if (!action.casterPositions.includes(casterUnit.position)) {
            return { error: 'invalid_caster_pos' };
        }

        // 쿨다운 체크
        if (casterUnit.cooldowns[actionId] && casterUnit.cooldowns[actionId] > 0) {
            return { error: 'on_cooldown' };
        }

        // 타겟 결정
        const targets = DarkestCombat._resolveTargets(combat, casterUnit, action, targetPositions);
        if (!targets || targets.length === 0) {
            return { error: 'no_targets' };
        }

        // 효과 적용
        const results = [];
        for (const target of targets) {
            const r = DarkestCombat._applyEffect(casterUnit, target, action);
            results.push(r);
        }

        // 쿨다운 설정
        if (action.cooldown > 0) {
            casterUnit.cooldowns[actionId] = action.cooldown;
        }

        // 위치 변경 (shiftSelf/shiftTarget)
        if (action.effects.shiftSelf) {
            DarkestCombat._shiftUnit(combat, casterUnit, action.effects.shiftSelf);
        }
        if (action.effects.shiftTarget && targets.length > 0) {
            DarkestCombat._shiftUnit(combat, targets[0], action.effects.shiftTarget);
        }

        return { success: true, action, targets, results };
    }

    static _resolveTargets(combat, caster, action, requestedPositions) {
        const opponentTeam = caster.team === 'ally' ? combat.enemies : combat.allies;
        const allyTeam = caster.team === 'ally' ? combat.allies : combat.enemies;

        let pool;
        if (action.targetType === 'enemy' || action.targetType === 'enemy_pair' || action.targetType === 'enemy_line') {
            pool = opponentTeam.filter(u => u.alive);
        } else if (action.targetType === 'ally') {
            pool = allyTeam.filter(u => u.alive);
        } else if (action.targetType === 'self') {
            return [caster];
        } else {
            pool = opponentTeam.filter(u => u.alive);
        }

        // 전체
        if (action.targetCount === 'all') return pool;

        // 일렬 관통 (선택 위치부터 끝까지)
        if (action.targetCount === 'line') {
            if (!requestedPositions || requestedPositions.length === 0) return [];
            const startPos = requestedPositions[0];
            return pool.filter(u => u.position >= startPos);
        }

        // 인접 쌍 (예: 폭탄)
        if (action.targetType === 'enemy_pair') {
            if (!requestedPositions || requestedPositions.length === 0) return [];
            const startPos = requestedPositions[0];
            const adjacent = pool.filter(u => u.position === startPos || u.position === startPos + 1);
            return adjacent.slice(0, 2);
        }

        // 다중 (예: 회전 베기 = 2, 다중 사격 = 3)
        if (typeof action.targetCount === 'number' && action.targetCount > 1) {
            // 허용된 위치 풀 중 N개
            const valid = pool.filter(u => action.targetPositions.includes(u.position));
            return valid.slice(0, action.targetCount);
        }

        // 단일
        if (requestedPositions && requestedPositions.length > 0) {
            const target = pool.find(u => u.position === requestedPositions[0]);
            return target ? [target] : [];
        }

        // 기본: 첫 번째 유효 타겟
        const target = pool.find(u => action.targetPositions.includes(u.position));
        return target ? [target] : [];
    }

    static _applyEffect(caster, target, action) {
        const fx = action.effects;
        const result = { target: target.id, casterId: caster.id };

        // 공격 (데미지)
        if (fx.atkMult) {
            const isCrit = fx.guaranteedCrit || (Math.random() < caster.critRate);
            const critMult = isCrit ? caster.critDmg : 1.0;
            const rawDmg = caster.atk * fx.atkMult * critMult;
            // DEF percent: damage * (1 - def/(def+100))
            const finalDmg = Math.floor(rawDmg * (1 - target.def / (target.def + 100)));
            target.hp -= finalDmg;
            result.damage = finalDmg;
            result.isCrit = isCrit;
            if (target.hp <= 0) {
                target.alive = false;
                target.hp = 0;
                result.killed = true;
            }
        }

        // 회복 (heal)
        if (fx.healPct) {
            const heal = Math.floor(target.maxHp * fx.healPct);
            target.hp = Math.min(target.maxHp, target.hp + heal);
            result.heal = heal;
        }

        // 상태 효과
        if (fx.statusEffect) {
            target.statusEffects.push({
                type: fx.statusEffect,
                duration: fx.statusDuration || 2,
                value: fx.atkMult || 0.2
            });
            result.status = fx.statusEffect;
        }

        // 디버프
        if (fx.debuffStat) {
            target.statusEffects.push({
                type: 'debuff_' + fx.debuffStat,
                duration: fx.duration || 2,
                stat: fx.debuffStat,
                amount: -fx.debuffAmt
            });
        }

        // 버프 (atkBuff, defBuff)
        if (fx.atkBuff) {
            target.statusEffects.push({
                type: 'buff_atk', duration: fx.duration || 2, value: fx.atkBuff
            });
        }
        if (fx.defBuff) {
            target.statusEffects.push({
                type: 'buff_def', duration: fx.duration || 1, value: fx.defBuff
            });
        }

        // 도발
        if (fx.taunt) {
            caster.statusEffects.push({ type: 'taunt_active', duration: 1 });
        }

        // 디스펠
        if (fx.dispelEnemy) {
            target.statusEffects = target.statusEffects.filter(e => e.type.startsWith('buff_'));
        }
        if (fx.dispelAlly) {
            target.statusEffects = target.statusEffects.filter(e => !e.type.startsWith('debuff_') && e.type !== 'bleed' && e.type !== 'burn' && e.type !== 'slow');
        }

        return result;
    }

    static _shiftUnit(combat, unit, amount) {
        const newPos = Math.max(1, Math.min(4, unit.position + amount));
        if (newPos === unit.position) return;

        // 같은 팀 내 위치 스왑
        const team = unit.team === 'ally' ? combat.allies : combat.enemies;
        const other = team.find(u => u.position === newPos && u.alive);
        if (other) {
            other.position = unit.position;
        }
        unit.position = newPos;
    }

    /**
     * 라운드 시작 시 상태 효과 처리 (출혈/화상 데미지, 만료 카운트)
     */
    static _tickStatusEffectsStartOfRound(unit) {
        if (!unit.statusEffects || unit.statusEffects.length === 0) return;
        const remaining = [];
        for (const e of unit.statusEffects) {
            // 데미지 효과
            if (e.type === 'bleed') {
                const dmg = Math.floor(unit.maxHp * 0.05);
                unit.hp -= dmg;
            } else if (e.type === 'burn') {
                const dmg = Math.floor(unit.maxHp * 0.05);
                unit.hp -= dmg;
            }
            if (unit.hp <= 0) { unit.hp = 0; unit.alive = false; }

            e.duration--;
            if (e.duration > 0) remaining.push(e);
        }
        unit.statusEffects = remaining;
    }

    /**
     * 사망 시 위치 시프트 — 뒤 유닛이 1칸씩 앞으로
     */
    static compactPositions(combat) {
        ['allies', 'enemies'].forEach(team => {
            const alive = combat[team].filter(u => u.alive).sort((a, b) => a.position - b.position);
            alive.forEach((u, i) => { u.position = i + 1; });
        });
    }

    /**
     * 전투 종료 체크.
     */
    static checkBattleEnd(combat) {
        const allyAlive = combat.allies.filter(u => u.alive).length;
        const enemyAlive = combat.enemies.filter(u => u.alive).length;
        if (allyAlive === 0) return { ended: true, winner: 'enemy' };
        if (enemyAlive === 0) return { ended: true, winner: 'ally' };
        return { ended: false };
    }

    /**
     * 적 AI — 사용 가능한 액션 중 단순 선택.
     * 일반 적은 atk1만 사용. 엘리트는 다양화.
     */
    static aiChooseAction(combat, enemy) {
        // 일반 적: 단순 공격 (atk1 효과로 가정)
        // 임시 행동: ATK ×1.0 단일 적 (포지션 1-2 우선)
        const enemyAlives = combat.allies.filter(u => u.alive);
        if (enemyAlives.length === 0) return null;

        // 도발된 아군 우선
        const tauntTarget = enemyAlives.find(u => u.statusEffects.some(e => e.type === 'taunt_active'));
        let target = tauntTarget;
        if (!target) {
            // HP% 낮은 우선
            enemyAlives.sort((a, b) => (a.hp / a.maxHp) - (b.hp / b.maxHp));
            target = enemyAlives[0];
        }

        return {
            action: { name: '공격', effects: { atkMult: 1.0 }, casterPositions: [1, 2, 3, 4], targetType: 'enemy', targetCount: 1 },
            target
        };
    }

    /**
     * 적 AI 실행 — 단순 공격 적용.
     */
    static executeAiAction(combat, enemy) {
        const choice = DarkestCombat.aiChooseAction(combat, enemy);
        if (!choice || !choice.target) return null;

        const fx = choice.action.effects;
        const isCrit = Math.random() < (enemy.critRate || 0.05);
        const critMult = isCrit ? (enemy.critDmg || 1.5) : 1.0;
        const rawDmg = enemy.atk * (fx.atkMult || 1.0) * critMult;
        const finalDmg = Math.floor(rawDmg * (1 - choice.target.def / (choice.target.def + 100)));
        choice.target.hp -= finalDmg;
        const killed = choice.target.hp <= 0;
        if (killed) { choice.target.hp = 0; choice.target.alive = false; }

        return {
            casterId: enemy.id,
            target: choice.target.id,
            damage: finalDmg,
            isCrit,
            killed,
            action: choice.action
        };
    }
}
