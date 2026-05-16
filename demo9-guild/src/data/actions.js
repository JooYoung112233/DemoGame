/**
 * 다키스트 스타일 액션 데이터.
 * 각 클래스 = 공격 3 + 스킬 1 = 4행동.
 * 행동 정의:
 *  - id, name, type ('attack' | 'skill' | 'support')
 *  - casterPositions: [1-4] 사용 가능한 캐스터 위치
 *  - targetType: 'enemy' | 'ally' | 'self' | 'enemy_all' | 'ally_all'
 *  - targetPositions: [1-4] 적/아군 어느 포지션을 노릴 수 있는지
 *  - targetCount: 1 (단일) | 2 (2명) | 'multi' (지정 수만큼) | 'all' (전체)
 *  - cooldown: 0 (매턴 가능) | N (N라운드 쿨다운, 스킬)
 *  - effects: { atkMult, statusEffect, statusDuration, shift, heal, debuff, etc }
 */
const ACTION_DATA = {
    // === WARRIOR ===
    warrior_atk1: {
        name: '정면 베기', type: 'attack', icon: '⚔',
        casterPositions: [1, 2], targetType: 'enemy', targetPositions: [1, 2], targetCount: 1,
        cooldown: 0, effects: { atkMult: 1.0 },
        desc: '근접 단일 공격'
    },
    warrior_atk2: {
        name: '회전 베기', type: 'attack', icon: '🌀',
        casterPositions: [1, 2], targetType: 'enemy', targetPositions: [1, 2], targetCount: 2,
        cooldown: 0, effects: { atkMult: 0.7 },
        desc: '전열 2명 동시 타격 (AoE)'
    },
    warrior_atk3: {
        name: '방패 밀치기', type: 'attack', icon: '🛡',
        casterPositions: [1, 2], targetType: 'enemy', targetPositions: [1], targetCount: 1,
        cooldown: 0, effects: { atkMult: 0.8, shiftTarget: 1 },
        desc: '공격 + 적을 1칸 뒤로'
    },
    warrior_skill: {
        name: '방패의 맹세', type: 'skill', icon: '✨',
        casterPositions: [1, 2], targetType: 'ally', targetPositions: [1, 2], targetCount: 2,
        cooldown: 3, effects: { defBuff: 0.4, duration: 1, taunt: true },
        desc: '자신+인접 아군 피해 -40% (1R) + 도발'
    },

    // === ROGUE ===
    rogue_atk1: {
        name: '단검 찌르기', type: 'attack', icon: '🗡',
        casterPositions: [1, 2], targetType: 'enemy', targetPositions: [1, 2], targetCount: 1,
        cooldown: 0, effects: { atkMult: 1.0 },
        desc: '빠른 근접 공격'
    },
    rogue_atk2: {
        name: '투척 단검', type: 'attack', icon: '🎯',
        casterPositions: [2, 3, 4], targetType: 'enemy', targetPositions: [1, 2, 3, 4], targetCount: 1,
        cooldown: 0, effects: { atkMult: 0.8 },
        desc: '원거리 단일'
    },
    rogue_atk3: {
        name: '측면 이동', type: 'attack', icon: '💨',
        casterPositions: [1, 2, 3], targetType: 'enemy', targetPositions: [1, 2], targetCount: 1,
        cooldown: 0, effects: { atkMult: 0.7, shiftSelf: 1 },
        desc: '공격 + 자신 1칸 뒤로 (회피)'
    },
    rogue_skill: {
        name: '급소 찌르기', type: 'skill', icon: '✨',
        casterPositions: [1, 2], targetType: 'enemy', targetPositions: [1, 2], targetCount: 1,
        cooldown: 3, effects: { atkMult: 1.2, guaranteedCrit: true, statusEffect: 'bleed', statusDuration: 3 },
        desc: '확정 크리 + 출혈 (3R)'
    },

    // === MAGE ===
    mage_atk1: {
        name: '마력 화살', type: 'attack', icon: '✨',
        casterPositions: [3, 4], targetType: 'enemy', targetPositions: [1, 2, 3, 4], targetCount: 1,
        cooldown: 0, effects: { atkMult: 1.0 },
        desc: '원거리 단일'
    },
    mage_atk2: {
        name: '화염구', type: 'attack', icon: '🔥',
        casterPositions: [3, 4], targetType: 'enemy', targetPositions: [1, 2], targetCount: 1,
        cooldown: 0, effects: { atkMult: 0.9, statusEffect: 'burn', statusDuration: 2 },
        desc: '전열 공격 + 화상 (2R)'
    },
    mage_atk3: {
        name: '냉기파', type: 'attack', icon: '❄',
        casterPositions: [3, 4], targetType: 'enemy', targetPositions: [1, 2], targetCount: 1,
        cooldown: 0, effects: { atkMult: 0.8, statusEffect: 'slow', statusDuration: 2 },
        desc: '전열 공격 + 둔화'
    },
    mage_skill: {
        name: '마력 폭발', type: 'skill', icon: '💥',
        casterPositions: [3, 4], targetType: 'enemy', targetPositions: [1, 2, 3, 4], targetCount: 'all',
        cooldown: 4, effects: { atkMult: 1.5, statusEffect: 'burn', statusDuration: 2 },
        desc: '적 전체 AoE + 화상'
    },

    // === ARCHER ===
    archer_atk1: {
        name: '활쏘기', type: 'attack', icon: '🏹',
        casterPositions: [3, 4], targetType: 'enemy', targetPositions: [1, 2, 3, 4], targetCount: 1,
        cooldown: 0, effects: { atkMult: 1.0 },
        desc: '원거리 단일'
    },
    archer_atk2: {
        name: '다중 사격', type: 'attack', icon: '🎯',
        casterPositions: [3, 4], targetType: 'enemy', targetPositions: [1, 2, 3], targetCount: 3,
        cooldown: 0, effects: { atkMult: 0.7 },
        desc: '3타깃 동시 (각 0.7배)'
    },
    archer_atk3: {
        name: '저격', type: 'attack', icon: '🎯',
        casterPositions: [4], targetType: 'enemy', targetPositions: [3, 4], targetCount: 1,
        cooldown: 0, effects: { atkMult: 1.5 },
        desc: '후열 한정 (적 3-4) 강타'
    },
    archer_skill: {
        name: '관통 사격', type: 'skill', icon: '✨',
        casterPositions: [3, 4], targetType: 'enemy_line', targetPositions: [1, 2, 3, 4], targetCount: 'line',
        cooldown: 3, effects: { atkMult: 1.4 },
        desc: '일렬 관통 (선택한 적부터 뒤 모두)'
    },

    // === PRIEST ===
    priest_atk1: {
        name: '신성 빛', type: 'attack', icon: '✨',
        casterPositions: [2, 3, 4], targetType: 'enemy', targetPositions: [1, 2, 3, 4], targetCount: 1,
        cooldown: 0, effects: { atkMult: 0.7 },
        desc: '약한 원거리'
    },
    priest_atk2: {
        name: '정화 일격', type: 'attack', icon: '⚒',
        casterPositions: [1, 2], targetType: 'enemy', targetPositions: [1], targetCount: 1,
        cooldown: 0, effects: { atkMult: 0.8, dispelEnemy: true },
        desc: '근접 + 적 버프 1개 제거'
    },
    priest_atk3: {
        name: '치유', type: 'support', icon: '💚',
        casterPositions: [2, 3, 4], targetType: 'ally', targetPositions: [1, 2, 3, 4], targetCount: 1,
        cooldown: 0, effects: { healPct: 0.15 },
        desc: '아군 1명 HP 15% 회복'
    },
    priest_skill: {
        name: '신성 치유', type: 'skill', icon: '✨',
        casterPositions: [2, 3, 4], targetType: 'ally', targetPositions: [1, 2, 3, 4], targetCount: 'all',
        cooldown: 3, effects: { healPct: 0.25, dispelAlly: true },
        desc: '아군 전체 HP 25% + 디버프 해제'
    },

    // === ALCHEMIST ===
    alchemist_atk1: {
        name: '산성 투척', type: 'attack', icon: '🧪',
        casterPositions: [2, 3], targetType: 'enemy', targetPositions: [1, 2, 3], targetCount: 1,
        cooldown: 0, effects: { atkMult: 0.9, debuffStat: 'def', debuffAmt: 3, duration: 2 },
        desc: '공격 + DEF -3 (2R)'
    },
    alchemist_atk2: {
        name: '폭탄', type: 'attack', icon: '💣',
        casterPositions: [2, 3], targetType: 'enemy_pair', targetPositions: [1, 2, 3, 4], targetCount: 2,
        cooldown: 0, effects: { atkMult: 1.0 },
        desc: '인접한 적 2명 동시'
    },
    alchemist_atk3: {
        name: '강화 물약', type: 'support', icon: '🧪',
        casterPositions: [2, 3, 4], targetType: 'ally', targetPositions: [1, 2, 3, 4], targetCount: 1,
        cooldown: 0, effects: { atkBuff: 0.2, duration: 2 },
        desc: '아군 1명 ATK +20% (2R)'
    },
    alchemist_skill: {
        name: '화염병', type: 'skill', icon: '🔥',
        casterPositions: [2, 3], targetType: 'enemy', targetPositions: [1, 2, 3, 4], targetCount: 'all',
        cooldown: 4, effects: { atkMult: 1.0, statusEffect: 'burn', statusDuration: 2 },
        desc: '적 전체 AoE + 화상'
    }
};

/**
 * 클래스별 4행동 ID 매핑.
 * @returns {object} { atk1, atk2, atk3, skill }
 */
const CLASS_ACTIONS = {
    warrior:   { atk1: 'warrior_atk1',   atk2: 'warrior_atk2',   atk3: 'warrior_atk3',   skill: 'warrior_skill' },
    rogue:     { atk1: 'rogue_atk1',     atk2: 'rogue_atk2',     atk3: 'rogue_atk3',     skill: 'rogue_skill' },
    mage:      { atk1: 'mage_atk1',      atk2: 'mage_atk2',      atk3: 'mage_atk3',      skill: 'mage_skill' },
    archer:    { atk1: 'archer_atk1',    atk2: 'archer_atk2',    atk3: 'archer_atk3',    skill: 'archer_skill' },
    priest:    { atk1: 'priest_atk1',    atk2: 'priest_atk2',    atk3: 'priest_atk3',    skill: 'priest_skill' },
    alchemist: { atk1: 'alchemist_atk1', atk2: 'alchemist_atk2', atk3: 'alchemist_atk3', skill: 'alchemist_skill' }
};

function getClassActions(classKey) {
    const map = CLASS_ACTIONS[classKey];
    if (!map) return [];
    return [
        { id: map.atk1, slot: 'atk1', ...ACTION_DATA[map.atk1] },
        { id: map.atk2, slot: 'atk2', ...ACTION_DATA[map.atk2] },
        { id: map.atk3, slot: 'atk3', ...ACTION_DATA[map.atk3] },
        { id: map.skill, slot: 'skill', ...ACTION_DATA[map.skill] }
    ];
}

/** 캐스터 위치에서 사용 가능한 행동만 필터 */
function canUseAction(action, casterPosition) {
    return action.casterPositions.includes(casterPosition);
}

/** 타겟 위치에 적/아군이 있을 때 사용 가능한지 */
function canTargetPosition(action, targetPos) {
    return action.targetPositions.includes(targetPos);
}
