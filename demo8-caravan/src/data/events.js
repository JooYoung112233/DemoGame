const EVENT_DATA = [
    {
        title: '떠돌이 상인',
        desc: '"좋은 물건 있수다~" 낡은 마차 옆에 상인이 서 있다.',
        choices: [
            {
                text: '물약 구매 (-25G)',
                desc: '파티 전원 HP 완전 회복',
                canApply: rs => rs.gold >= 25,
                apply: rs => {
                    rs.gold -= 25;
                    rs.party.forEach(u => { u.currentHp = u.getStats().hp; });
                    return '전원 HP 회복! (-25G)';
                }
            },
            {
                text: '거절',
                desc: '그냥 지나간다',
                canApply: () => true,
                apply: () => '조용히 지나갔다.'
            }
        ]
    },
    {
        title: '금화 상자',
        desc: '길가에 금화가 든 상자가 놓여 있다. 함정일 수도...',
        choices: [
            {
                text: '열어본다',
                desc: '70%: 60G 획득 / 30%: 랜덤 유닛 HP -30%',
                canApply: () => true,
                apply: rs => {
                    if (Math.random() < 0.7) {
                        rs.gold += 60;
                        rs.totalGoldEarned += 60;
                        return '금화 60G 발견! 행운이다!';
                    } else {
                        const u = rs.party[Math.floor(Math.random() * rs.party.length)];
                        const stats = u.getStats();
                        u.currentHp = Math.max(1, u.currentHp - Math.floor(stats.hp * 0.3));
                        return `함정! ${u.getName()}이(가) 부상!`;
                    }
                }
            },
            {
                text: '지나간다',
                desc: '안전하게 무시한다',
                canApply: () => true,
                apply: () => '현명한 판단이다.'
            }
        ]
    },
    {
        title: '길 막힌 도로',
        desc: '거대한 바위가 길을 막고 있다.',
        choices: [
            {
                text: '돌파한다',
                desc: '랜덤 유닛 HP -20%',
                canApply: () => true,
                apply: rs => {
                    const u = rs.party[Math.floor(Math.random() * rs.party.length)];
                    const stats = u.getStats();
                    u.currentHp = Math.max(1, u.currentHp - Math.floor(stats.hp * 0.2));
                    return `${u.getName()}이(가) 바위를 치우다 부상당했다.`;
                }
            },
            {
                text: '우회한다 (-20G)',
                desc: '골드를 내고 안전하게 우회',
                canApply: rs => rs.gold >= 20,
                apply: rs => {
                    rs.gold -= 20;
                    return '안전하게 우회했다. (-20G)';
                }
            }
        ]
    },
    {
        title: '떠돌이 대장장이',
        desc: '"무기를 벼려줄까?" 대장장이가 모루 앞에 서 있다.',
        choices: [
            {
                text: '강화 의뢰 (-50G)',
                desc: '랜덤 유닛 ATK +8 영구',
                canApply: rs => rs.gold >= 50,
                apply: rs => {
                    rs.gold -= 50;
                    const u = rs.party[Math.floor(Math.random() * rs.party.length)];
                    u.bonusAtk = (u.bonusAtk || 0) + 8;
                    return `${u.getName()}의 무기를 강화했다! ATK +8 (-50G)`;
                }
            },
            {
                text: '거절',
                desc: '골드가 아깝다',
                canApply: () => true,
                apply: () => '대장장이에게 감사를 전하고 떠났다.'
            }
        ]
    },
    {
        title: '야영지 발견',
        desc: '버려진 야영지를 발견했다. 쉬어갈 수도 있고, 수색할 수도 있다.',
        choices: [
            {
                text: '휴식',
                desc: '전원 HP 40% 회복',
                canApply: () => true,
                apply: rs => {
                    rs.party.forEach(u => {
                        const stats = u.getStats();
                        u.currentHp = Math.min(stats.hp, u.currentHp + Math.floor(stats.hp * 0.4));
                    });
                    return '충분히 쉬었다. 전원 HP 40% 회복!';
                }
            },
            {
                text: '수색',
                desc: '60%: 30G / 40%: 전원 HP -15%',
                canApply: () => true,
                apply: rs => {
                    if (Math.random() < 0.6) {
                        rs.gold += 30;
                        rs.totalGoldEarned += 30;
                        return '숨겨진 금화 30G 발견!';
                    } else {
                        rs.party.forEach(u => {
                            const stats = u.getStats();
                            u.currentHp = Math.max(1, u.currentHp - Math.floor(stats.hp * 0.15));
                        });
                        return '기습! 전원 HP -15%!';
                    }
                }
            }
        ]
    },
    {
        title: '부상병 구출',
        desc: '길가에 쓰러진 부상병이 있다.',
        choices: [
            {
                text: '도와준다',
                desc: '무료 병사 1명 합류 (파티<6일 때)',
                canApply: rs => rs.party.length < rs.maxPartySize,
                apply: rs => {
                    const unit = new Unit('soldier');
                    unit.currentHp = Math.floor(unit.getStats().hp * 0.5);
                    rs.party.push(unit);
                    rs.totalRecruits++;
                    return '부상병이 감사하며 합류했다! (HP 50%)';
                }
            },
            {
                text: '무시한다 (+20G)',
                desc: '주머니를 뒤져 골드를 얻는다',
                canApply: () => true,
                apply: rs => {
                    rs.gold += 20;
                    rs.totalGoldEarned += 20;
                    return '...20G를 얻었다.';
                }
            }
        ]
    },
    {
        title: '수상한 수도사',
        desc: '"축복을 내려줄까, 아니면 치유를 해줄까?"',
        choices: [
            {
                text: '축복',
                desc: '랜덤 유닛에게 좋은 특성 +1',
                canApply: () => true,
                apply: rs => {
                    const u = rs.party[Math.floor(Math.random() * rs.party.length)];
                    const trait = GOOD_TRAITS[Math.floor(Math.random() * GOOD_TRAITS.length)];
                    u.traits.push(trait);
                    return `${u.getName()}에게 [${trait.name}] 특성 부여!`;
                }
            },
            {
                text: '치유',
                desc: '전원 HP 20% 회복',
                canApply: () => true,
                apply: rs => {
                    rs.party.forEach(u => {
                        const stats = u.getStats();
                        u.currentHp = Math.min(stats.hp, u.currentHp + Math.floor(stats.hp * 0.2));
                    });
                    return '따뜻한 빛이 감싼다. 전원 HP 20% 회복!';
                }
            }
        ]
    },
    {
        title: '도박꾼의 제안',
        desc: '"주사위 한 판 어때? 잃으면 내 걸 줄게~"',
        choices: [
            {
                text: '도박! (-30G)',
                desc: '50%: 90G 획득 / 50%: 0G',
                canApply: rs => rs.gold >= 30,
                apply: rs => {
                    rs.gold -= 30;
                    if (Math.random() < 0.5) {
                        rs.gold += 90;
                        rs.totalGoldEarned += 90;
                        return '대박! 90G 획득! (+60G 순이익)';
                    } else {
                        return '졌다... 30G를 잃었다.';
                    }
                }
            },
            {
                text: '거절',
                desc: '도박은 안 한다',
                canApply: () => true,
                apply: () => '도박꾼이 아쉬워한다.'
            }
        ]
    },
    {
        title: '폐허의 훈련장',
        desc: '옛 기사단의 훈련장이다. 아직 쓸 만하다.',
        choices: [
            {
                text: '훈련',
                desc: '랜덤 유닛 HP/ATK/DEF 중 하나 +5',
                canApply: () => true,
                apply: rs => {
                    const u = rs.party[Math.floor(Math.random() * rs.party.length)];
                    const stat = ['bonusHp', 'bonusAtk', 'bonusDef'][Math.floor(Math.random() * 3)];
                    const label = { bonusHp: 'HP', bonusAtk: 'ATK', bonusDef: 'DEF' }[stat];
                    u[stat] = (u[stat] || 0) + 5;
                    if (stat === 'bonusHp') u.currentHp += 5;
                    return `${u.getName()}의 ${label} +5!`;
                }
            },
            {
                text: '지나간다',
                desc: '시간이 아깝다',
                canApply: () => true,
                apply: () => '훈련장을 뒤로하고 떠났다.'
            }
        ]
    },
    {
        title: '마왕군 정찰대',
        desc: '마왕군 정찰 소대가 보인다. 숨을까, 기습할까?',
        choices: [
            {
                text: '매복 공격',
                desc: '소규모 전투, 승리시 50G',
                canApply: () => true,
                apply: rs => {
                    rs._ambushBattle = true;
                    return null;
                }
            },
            {
                text: '숨는다',
                desc: '안전하게 통과',
                canApply: () => true,
                apply: () => '조용히 숨어서 지나갔다.'
            }
        ]
    }
];
