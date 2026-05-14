const WEAPONS = {
    pistol: {
        id: 'pistol',
        name: '권총',
        color: '#ffcc44',
        colorHex: 0xffcc44,
        desc: '안정적이고 균형 잡힌 기본 무기',
        stamina: 100,
        maxStamina: 100,
        actions: [
            { id: 'light', name: '약공격', hitRate: 0.80, dmgMin: 8, dmgMax: 12, staminaCost: 5, desc: '안정적인 피해' },
            { id: 'heavy', name: '강공격', hitRate: 0.40, dmgMin: 20, dmgMax: 30, staminaCost: 15, desc: '높은 피해, 낮은 명중' },
            { id: 'defend', name: '방어', hitRate: 1.00, dmgMin: 0, dmgMax: 0, staminaCost: 0, desc: '받는 피해 50% 감소', special: 'defend' },
            { id: 'dodge', name: '회피', hitRate: 0.60, dmgMin: 0, dmgMax: 0, staminaCost: 5, desc: '적 공격 완전 무효', special: 'dodge' },
            { id: 'focus', name: '집중', hitRate: 0.70, dmgMin: 0, dmgMax: 0, staminaCost: 8, desc: '다음 공격 2배 피해', special: 'focus' },
            { id: 'rest', name: '대기', hitRate: 1.00, dmgMin: 0, dmgMax: 0, staminaCost: 0, desc: 'SP 15 회복, 턴 넘김', special: 'rest' }
        ]
    },
    shotgun: {
        id: 'shotgun',
        name: '샷건',
        color: '#ff6644',
        colorHex: 0xff6644,
        desc: '높은 명중률, 낮은 단일 피해\n근접 산탄 특화',
        stamina: 90,
        maxStamina: 90,
        actions: [
            { id: 'light', name: '약공격', hitRate: 0.85, dmgMin: 6, dmgMax: 10, staminaCost: 5, desc: '산탄 기본 사격' },
            { id: 'heavy', name: '강공격', hitRate: 0.50, dmgMin: 18, dmgMax: 35, staminaCost: 18, desc: '근접 집중 산탄' },
            { id: 'defend', name: '방어', hitRate: 1.00, dmgMin: 0, dmgMax: 0, staminaCost: 0, desc: '받는 피해 50% 감소', special: 'defend' },
            { id: 'dodge', name: '회피', hitRate: 0.50, dmgMin: 0, dmgMax: 0, staminaCost: 5, desc: '적 공격 완전 무효', special: 'dodge' },
            { id: 'focus', name: '집중', hitRate: 0.75, dmgMin: 0, dmgMax: 0, staminaCost: 8, desc: '다음 공격 2배 피해', special: 'focus' },
            { id: 'rest', name: '대기', hitRate: 1.00, dmgMin: 0, dmgMax: 0, staminaCost: 0, desc: 'SP 15 회복, 턴 넘김', special: 'rest' }
        ]
    },
    energy_rifle: {
        id: 'energy_rifle',
        name: '에너지 라이플',
        color: '#44ccff',
        colorHex: 0x44ccff,
        desc: '낮은 명중, 높은 피해의 저격형\n한 방에 승부',
        stamina: 80,
        maxStamina: 80,
        actions: [
            { id: 'light', name: '약공격', hitRate: 0.70, dmgMin: 12, dmgMax: 18, staminaCost: 6, desc: '에너지 사격' },
            { id: 'heavy', name: '강공격', hitRate: 0.35, dmgMin: 30, dmgMax: 50, staminaCost: 20, desc: '고출력 관통탄' },
            { id: 'defend', name: '방어', hitRate: 1.00, dmgMin: 0, dmgMax: 0, staminaCost: 0, desc: '받는 피해 50% 감소', special: 'defend' },
            { id: 'dodge', name: '회피', hitRate: 0.55, dmgMin: 0, dmgMax: 0, staminaCost: 5, desc: '적 공격 완전 무효', special: 'dodge' },
            { id: 'focus', name: '집중', hitRate: 0.65, dmgMin: 0, dmgMax: 0, staminaCost: 10, desc: '다음 공격 2배 피해', special: 'focus' },
            { id: 'rest', name: '대기', hitRate: 1.00, dmgMin: 0, dmgMax: 0, staminaCost: 0, desc: 'SP 15 회복, 턴 넘김', special: 'rest' }
        ]
    }
};
