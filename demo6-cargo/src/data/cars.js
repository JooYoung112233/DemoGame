const LC_CARS = [
    {
        id: 'cargo', name: '화물칸', maxHp: 200, color: 0x886644,
        desc: '적 처치 시 부품/탄약 드랍',
        function: 'drop',
        lostEffect: '드랍 획득 불가'
    },
    {
        id: 'medical', name: '의료칸', maxHp: 150, color: 0x44aa66,
        desc: '매 라운드 아군 HP 15% 회복',
        function: 'heal',
        healRate: 0.15,
        lostEffect: '라운드 간 회복 불가'
    },
    {
        id: 'ammo', name: '탄약칸', maxHp: 180, color: 0xcc8844,
        desc: '원거리 공격 가능 (파괴 시 근접만 가능)',
        function: 'ammo',
        lostEffect: '원거리 아군 공격력 50% 감소'
    },
    {
        id: 'generator', name: '발전기칸', maxHp: 220, color: 0xffcc22,
        desc: '모듈 전원 공급 (파괴 시 모듈 정지)',
        function: 'power',
        lostEffect: '모든 모듈 비활성화'
    }
];
