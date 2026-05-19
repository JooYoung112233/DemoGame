const FACILITY_DATA = {
    // ── Lv.1 (입문) ──
    recruit:      { name: '용병 모집소',   unlockLevel: 1, cost: 0,    icon: '⚔', desc: '용병을 모집합니다', scene: 'RecruitScene', tier: 1 },
    equipment:    { name: '장비 착용',     unlockLevel: 1, cost: 0,    icon: '🎽', desc: '용병에게 장비를 착용시킵니다', scene: 'EquipmentScene', tier: 1 },
    gate:         { name: '출발 게이트',   unlockLevel: 1, cost: 0,    icon: '🚪', desc: '구역으로 출발합니다', scene: 'DeployScene', tier: 1 },
    // ── Lv.2 ──
    storage:      { name: '보관함',        unlockLevel: 2, cost: 200,  icon: '📦', desc: '아이템을 보관합니다', scene: 'StorageScene', tier: 2 },
    forge:        { name: '장비 제작소',   unlockLevel: 2, cost: 500,  icon: '🔨', desc: '소재로 장비를 제작합니다', scene: 'ForgeScene', tier: 2 },
    // ── Lv.3 ──
    auction:      { name: '경매장',        unlockLevel: 3, cost: 800,  icon: '🏛', desc: '아이템을 경매합니다', scene: 'AuctionScene', tier: 3 },
    // ── Lv.5 ──
    training:     { name: '훈련소',        unlockLevel: 5, cost: 1200, icon: '🏋', desc: '용병을 훈련합니다', scene: 'TrainingScene', tier: 4 },
    temple:       { name: '신전',          unlockLevel: 5, cost: 1500, icon: '⛪', desc: '치유 및 축복을 구매합니다', scene: 'TempleScene', tier: 4 },
    // ── Lv.7 ──
    intel:        { name: '정보소',        unlockLevel: 7, cost: 2000, icon: '🔍', desc: '구역 정보와 의뢰를 확인합니다', scene: 'IntelScene', tier: 5 },
    eliteRecruit: { name: '고급 모집소',   unlockLevel: 7, cost: 3000, icon: '👑', desc: '고급 용병을 모집합니다', scene: 'EliteRecruitScene', tier: 5 },
    guildHall:    { name: '길드 회관',     unlockLevel: 7, cost: 0,    icon: '🏛', desc: '메타 업그레이드 트리', scene: 'GuildHallScene', tier: 5 },
    vault:        { name: '비밀 금고',     unlockLevel: 7, cost: 4000, icon: '🔒', desc: '보관함 확장 + 보안 컨테이너', scene: null, tier: 5 }
};

const GUILD_LEVEL_XP = [0, 100, 250, 500, 800, 1200, 1800, 2500];

const ROSTER_LIMITS = {
    // 편성(deploy)은 전 레벨 4명 고정. 로스터(max)만 레벨에 따라 증가.
    1: { max: 4, deploy: 4 },
    2: { max: 5, deploy: 4 },
    3: { max: 6, deploy: 4 },
    4: { max: 7, deploy: 4 },
    5: { max: 8, deploy: 4 },
    6: { max: 9, deploy: 4 },
    7: { max: 10, deploy: 4 },
    8: { max: 12, deploy: 4 }
};

const FACILITY_KEYS = Object.keys(FACILITY_DATA);
