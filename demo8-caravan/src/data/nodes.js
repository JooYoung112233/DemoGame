const NODE_CONFIG = [
    { type: 'village', name: '시작 마을', desc: '여정의 시작점. 동료를 모집하자.' },
    { type: 'bandit', name: '산적 출몰', enemies: ['bandit', 'bandit', 'bandit'], gold: 40 },
    { type: 'event',  name: '갈림길', desc: '길 위에서 무언가를 발견했다.' },
    { type: 'village', name: '강변 마을', desc: '강가의 작은 마을. 보급이 가능하다.' },
    { type: 'bandit', name: '산적 야영지', enemies: ['bandit', 'bandit', 'bandit_archer'], gold: 55 },
    { type: 'event',  name: '폐허', desc: '오래된 폐허가 보인다.' },
    { type: 'village', name: '산맥 마을', desc: '산맥 기슭의 마을. 강한 전사들이 있다.' },
    { type: 'bandit', name: '산적 요새', enemies: ['bandit', 'bandit', 'bandit_archer', 'bandit_chief'], gold: 75 },
    { type: 'event',  name: '어둠의 숲', desc: '어두운 숲 속에서 이상한 기운이 느껴진다.' },
    { type: 'village', name: '최후의 마을', desc: '마왕성 앞 마지막 마을. 최후의 준비를 하자.' },
    { type: 'bandit', name: '마족 정찰대', enemies: ['demon_soldier', 'demon_soldier', 'demon_mage', 'bandit_chief'], gold: 100 },
    { type: 'boss',   name: '마왕성', enemies: ['demon_king', 'demon_soldier', 'demon_soldier', 'demon_mage'], gold: 0 }
];
