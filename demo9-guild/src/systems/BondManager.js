/**
 * BondManager — 용병 간 친밀도(Bond) 시스템.
 *
 * gameState.bonds = { 'mercId1_mercId2': xp (0-100), ... }
 *
 * 누적: 파티 내 모든 쌍에 동반 출전 시 누적
 * 효과: 같이 출전 시 stat 보너스 (티어별)
 * 영구 보존: 한쪽 사망해도 값은 유지
 */
const BondManager = {
    /** 티어 정의 — 효과는 같이 출전했을 때만 */
    TIERS: [
        { min: 0,   max: 9,   tier: 0, name: '낯선 사이',     statMult: 1.00, atkMult: 1.00, defMult: 1.00 },
        { min: 10,  max: 29,  tier: 1, name: '동료',           statMult: 1.00, atkMult: 1.02, defMult: 1.02 },
        { min: 30,  max: 49,  tier: 2, name: '형제/자매',      statMult: 1.05, atkMult: 1.05, defMult: 1.05 },
        { min: 50,  max: 74,  tier: 3, name: '전우',           statMult: 1.08, atkMult: 1.08, defMult: 1.08 },
        { min: 75,  max: 99,  tier: 4, name: '영혼의 동반자',  statMult: 1.12, atkMult: 1.12, defMult: 1.12 },
        { min: 100, max: 100, tier: 5, name: '운명 공동체',    statMult: 1.15, atkMult: 1.15, defMult: 1.15 }
    ],

    /** 두 ID로 정렬된 key */
    getBondKey(idA, idB) {
        return idA < idB ? `${idA}_${idB}` : `${idB}_${idA}`;
    },

    /** 현재 본드 XP (0-100) */
    getBondXp(gs, idA, idB) {
        if (!gs.bonds) return 0;
        return gs.bonds[BondManager.getBondKey(idA, idB)] || 0;
    },

    /** 본드 XP → tier 객체 */
    getTier(xp) {
        for (const t of BondManager.TIERS) {
            if (xp >= t.min && xp <= t.max) return t;
        }
        return BondManager.TIERS[0];
    },

    /** 두 용병의 tier 객체 직접 조회 */
    getTierFor(gs, idA, idB) {
        return BondManager.getTier(BondManager.getBondXp(gs, idA, idB));
    },

    /**
     * 파티 출전 후 호출 — 모든 쌍에 본드 XP 누적.
     * @param gs gameState
     * @param party Mercenary[] 출전한 용병들 (사망자 포함)
     * @param success boolean 전투/파견 성공 여부
     * @param sourceType 'main' | 'sub'
     */
    updateBonds(gs, party, success, sourceType) {
        if (!gs.bonds) gs.bonds = {};
        if (!party || party.length < 2) return;

        const baseGain = success ? 4 : 2;
        const multiplier = (sourceType === 'main') ? 2 : 1;

        for (let i = 0; i < party.length; i++) {
            for (let j = i + 1; j < party.length; j++) {
                const a = party[i], b = party[j];
                if (!a || !b) continue;

                let gain = baseGain * multiplier;

                // 음성 특성 영향
                if (BondManager._hasTrait(a, 'lone_wolf') || BondManager._hasTrait(b, 'lone_wolf')) {
                    gain *= 0.3;
                }
                if (BondManager._hasTrait(a, 'jealous') || BondManager._hasTrait(b, 'jealous')) {
                    gain -= 1;
                }
                if (BondManager._hasTrait(a, 'hothead') || BondManager._hasTrait(b, 'hothead')) {
                    if (Math.random() < 0.5) gain *= 0.5;
                }

                gain = Math.max(0, Math.round(gain));
                if (gain <= 0) continue;

                const key = BondManager.getBondKey(a.id, b.id);
                const newVal = Math.min(100, (gs.bonds[key] || 0) + gain);
                const oldTier = BondManager.getTier(gs.bonds[key] || 0).tier;
                const newTier = BondManager.getTier(newVal).tier;
                gs.bonds[key] = newVal;

                // 티어 상승 메시지
                if (newTier > oldTier) {
                    if (typeof GuildManager !== 'undefined' && GuildManager.addMessage) {
                        const tierData = BondManager.getTier(newVal);
                        GuildManager.addMessage(gs, `💞 ${a.name} ↔ ${b.name} : ${tierData.name} 도달!`);
                    }
                }
            }
        }
    },

    /**
     * 파티 stat 적용 — 본드 보너스 (티어 효과).
     * 같이 출전한 모든 쌍의 효과를 누적해서 각 유닛 stat에 곱함.
     * @param gs gameState
     * @param units BattleUnit[] or {id, atk, def, ...}[] — id로 본드 조회, 직접 mutate
     * @returns 활성 본드 [{aId, bId, tier, name}, ...] (UI 표시용)
     */
    applyBondBonuses(gs, units) {
        if (!gs.bonds) return [];
        const active = [];
        const bonusMap = {}; // unitId → {atkMult, defMult, statMult}

        for (let i = 0; i < units.length; i++) {
            for (let j = i + 1; j < units.length; j++) {
                const a = units[i], b = units[j];
                if (!a.id || !b.id) continue;
                const xp = BondManager.getBondXp(gs, a.id, b.id);
                const tier = BondManager.getTier(xp);
                if (tier.tier === 0) continue;

                // lone_wolf — 본드 효과 적용 안 함
                const aSrc = a.ref || a;
                const bSrc = b.ref || b;
                if (BondManager._hasTrait(aSrc, 'lone_wolf') || BondManager._hasTrait(bSrc, 'lone_wolf')) continue;

                [a.id, b.id].forEach(id => {
                    if (!bonusMap[id]) bonusMap[id] = { atkMult: 1, defMult: 1, statMult: 1 };
                    bonusMap[id].atkMult *= tier.atkMult;
                    bonusMap[id].defMult *= tier.defMult;
                    bonusMap[id].statMult *= tier.statMult;
                });

                active.push({
                    aId: a.id, bId: b.id,
                    aName: a.name, bName: b.name,
                    tier: tier.tier, name: tier.name, xp
                });
            }
        }

        // 보너스 실제 적용 (atk, def, maxHp)
        units.forEach(u => {
            const b = bonusMap[u.id];
            if (!b) return;
            if (typeof u.atk === 'number') u.atk = Math.floor(u.atk * b.atkMult);
            if (typeof u.def === 'number') u.def = Math.floor(u.def * b.defMult);
            // statMult — HP는 maxHp에 적용
            if (typeof u.maxHp === 'number') {
                u.maxHp = Math.floor(u.maxHp * b.statMult);
                u.hp = Math.min(u.hp || u.maxHp, u.maxHp);
            }
        });

        return active;
    },

    /**
     * 특정 용병이 가진 모든 본드를 반환 (UI용).
     * @returns [{otherId, otherName, xp, tier}]
     */
    getBondsForMerc(gs, mercId, allMercs) {
        const result = [];
        if (!gs.bonds) return result;
        for (const [key, xp] of Object.entries(gs.bonds)) {
            const [idA, idB] = key.split('_').map(Number);
            if (idA !== mercId && idB !== mercId) continue;
            const otherId = idA === mercId ? idB : idA;
            const other = allMercs.find(m => m.id === otherId);
            if (!other) continue;
            const tier = BondManager.getTier(xp);
            result.push({ otherId, otherName: other.name, otherRef: other, xp, tier });
        }
        return result.sort((a, b) => b.xp - a.xp);
    },

    /** 헬퍼: 특성 보유 체크 (다양한 형태 지원) */
    _hasTrait(merc, traitKey) {
        if (!merc) return false;
        if (typeof merc.hasTrait === 'function') return merc.hasTrait(traitKey);
        if (Array.isArray(merc.traits)) {
            return merc.traits.some(t => (t.key === traitKey) || (t.id === traitKey) || (t === traitKey));
        }
        return false;
    }
};
