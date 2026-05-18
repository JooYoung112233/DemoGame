/**
 * 퀘스트 가이드 — 좌측 상단에 "다음 할 일" 표시
 * check(gs) → true면 완료된 퀘스트
 * target: 클릭 시 이동할 씬 (null이면 이동 없음)
 */
const QUEST_GUIDE = [
    // ── Lv.1 입문 ──
    { id: 'hire_first',    text: '첫 용병을 고용하세요',       target: 'RecruitScene',   tier: 1,
      check: gs => gs.roster && gs.roster.length >= 1 },

    { id: 'equip_first',   text: '장비를 착용하세요',          target: 'EquipmentScene', tier: 1,
      check: gs => gs.roster && gs.roster.some(m => m.equipment && Object.values(m.equipment).some(e => e)) },

    { id: 'deploy_first',  text: 'Blood Pit에 출전하세요',     target: 'DeployScene',    tier: 1,
      check: gs => gs.runCount >= 1 },

    { id: 'win_first',     text: '첫 전투에서 승리하세요',     target: null,             tier: 1,
      check: gs => gs.zoneClearCount && Object.values(gs.zoneClearCount).some(v => v > 0) },

    // ── Lv.2 ──
    { id: 'reach_lv2',     text: '길드 레벨 2 달성',           target: null,             tier: 2,
      check: gs => gs.guildLevel >= 2 },

    { id: 'unlock_storage', text: '보관함을 해금하세요',       target: null,             tier: 2,
      check: gs => gs.unlockedFacilities && gs.unlockedFacilities.includes('storage') },

    { id: 'unlock_forge',  text: '제작소를 해금하세요',        target: null,             tier: 2,
      check: gs => gs.unlockedFacilities && gs.unlockedFacilities.includes('forge') },

    { id: 'craft_first',   text: '장비를 제작하세요',          target: 'ForgeScene',     tier: 2,
      check: gs => gs._craftCount >= 1 },

    // ── Lv.3 ──
    { id: 'reach_lv3',     text: '길드 레벨 3 달성',           target: null,             tier: 3,
      check: gs => gs.guildLevel >= 3 },

    { id: 'unlock_auction', text: '경매장을 해금하세요',       target: null,             tier: 3,
      check: gs => gs.unlockedFacilities && gs.unlockedFacilities.includes('auction') },

    { id: 'deploy_cargo',  text: 'Cargo에 출전하세요',         target: 'DeployScene',    tier: 3,
      check: gs => gs.zoneClearCount && gs.zoneClearCount['cargo_1'] >= 1 },

    // ── Lv.5 ──
    { id: 'reach_lv5',     text: '길드 레벨 5 달성',           target: null,             tier: 4,
      check: gs => gs.guildLevel >= 5 },

    { id: 'deploy_blackout', text: 'Blackout에 도전하세요',    target: 'DeployScene',    tier: 4,
      check: gs => gs.zoneClearCount && gs.zoneClearCount['blackout_1'] >= 1 },

    // ── Lv.7 ──
    { id: 'reach_lv7',     text: '길드 레벨 7 달성',           target: null,             tier: 5,
      check: gs => gs.guildLevel >= 7 },

    { id: 'unlock_guildHall', text: '길드 회관을 해금하세요',  target: null,             tier: 5,
      check: gs => gs.unlockedFacilities && gs.unlockedFacilities.includes('guildHall') }
];

/** 현재 활성 퀘스트 (미완료 중 첫 2개) 반환 */
function getActiveQuests(gs) {
    const active = [];
    for (const q of QUEST_GUIDE) {
        if (active.length >= 2) break;
        if (!q.check(gs)) active.push(q);
    }
    return active;
}

/** 전체 완료 수 / 전체 수 */
function getQuestProgress(gs) {
    const done = QUEST_GUIDE.filter(q => q.check(gs)).length;
    return { done, total: QUEST_GUIDE.length };
}
