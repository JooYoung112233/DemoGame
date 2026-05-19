const PARTY_DATA = {
    scout: {
        id: 'scout', name: '정찰병', role: 'scout',
        hp: 80, maxHp: 80, ap: 4, moveRange: 4,
        atk: 12, def: 4, spd: 8, range: 3,
        color: 0x44cc88,
        skills: [
            { id: 'aimed_shot', name: '조준사격', apCost: 2, damage: 18, range: 5, desc: '먼 거리 정밀 사격' },
            { id: 'quick_move', name: '신속이동', apCost: 1, type: 'buff', effect: 'extraMove', value: 2, desc: '추가 이동력 +2' }
        ]
    },
    fighter: {
        id: 'fighter', name: '전사', role: 'fighter',
        hp: 140, maxHp: 140, ap: 3, moveRange: 3,
        atk: 18, def: 10, spd: 4, range: 1,
        color: 0xcc4444,
        skills: [
            { id: 'heavy_strike', name: '강타', apCost: 2, damage: 28, range: 1, desc: '강력한 근접 일격' },
            { id: 'shield_wall', name: '방벽', apCost: 1, type: 'buff', effect: 'defUp', value: 8, duration: 2, desc: '2턴간 방어력 +8' }
        ]
    },
    medic: {
        id: 'medic', name: '의무병', role: 'medic',
        hp: 90, maxHp: 90, ap: 3, moveRange: 3,
        atk: 8, def: 5, spd: 5, range: 2,
        color: 0x44aaff,
        skills: [
            { id: 'heal', name: '치료', apCost: 2, type: 'heal', value: 30, range: 3, desc: '아군 HP 30 회복' },
            { id: 'stim', name: '각성제', apCost: 1, type: 'buff', effect: 'apUp', value: 1, range: 2, desc: '아군 AP +1 부여' }
        ]
    },
    marksman: {
        id: 'marksman', name: '저격수', role: 'marksman',
        hp: 70, maxHp: 70, ap: 3, moveRange: 2,
        atk: 22, def: 3, spd: 3, range: 5,
        color: 0xffaa44,
        skills: [
            { id: 'snipe', name: '저격', apCost: 3, damage: 40, range: 7, desc: '초장거리 고데미지 사격' },
            { id: 'overwatch', name: '감시', apCost: 2, type: 'stance', effect: 'overwatch', desc: '적 이동 시 자동 반격' }
        ]
    }
};
