const FACILITY_DATA = {
    recruit:      { name: '용병 모집소',   unlockLevel: 1, cost: 0,    icon: '⚔', desc: '용병을 모집합니다', scene: 'RecruitScene' },
    storage:      { name: '보관함',        unlockLevel: 1, cost: 0,    icon: '📦', desc: '아이템을 보관합니다', scene: 'StorageScene' },
    gate:         { name: '출발 게이트',   unlockLevel: 1, cost: 0,    icon: '🚪', desc: '구역으로 출발합니다', scene: 'DeployScene' },
    forge:        { name: '장비 제작소',   unlockLevel: 2, cost: 500,  icon: '🔨', desc: '소재로 장비를 제작합니다', scene: 'ForgeScene' },
    auction:      { name: '경매장',        unlockLevel: 3, cost: 800,  icon: '🏛', desc: '아이템을 경매합니다', scene: 'AuctionScene' },
    training:     { name: '훈련소',        unlockLevel: 4, cost: 1200, icon: '🏋', desc: '용병을 훈련합니다', scene: 'TrainingScene' },
    temple:       { name: '신전',          unlockLevel: 5, cost: 1500, icon: '⛪', desc: '치유 및 축복을 구매합니다', scene: 'TempleScene' },
    intel:        { name: '정보소',        unlockLevel: 6, cost: 2000, icon: '🔍', desc: '구역 정보와 의뢰를 확인합니다', scene: 'IntelScene' },
    eliteRecruit: { name: '고급 모집소',   unlockLevel: 7, cost: 3000, icon: '👑', desc: '고급 용병을 모집합니다', scene: 'EliteRecruitScene' },
    vault:        { name: '비밀 금고',     unlockLevel: 8, cost: 4000, icon: '🔒', desc: '보관함 확장 + 보안 컨테이너', scene: null }
};

const GUILD_LEVEL_XP = [0, 100, 250, 500, 800, 1200, 1800, 2500];

const ROSTER_LIMITS = {
    1: { max: 4, deploy: 2 },
    2: { max: 4, deploy: 2 },
    3: { max: 6, deploy: 3 },
    4: { max: 6, deploy: 3 },
    5: { max: 8, deploy: 4 },
    6: { max: 8, deploy: 4 },
    7: { max: 10, deploy: 5 },
    8: { max: 10, deploy: 5 }
};

const FACILITY_KEYS = Object.keys(FACILITY_DATA);
