# 시스템: gameState 스키마 (v2)

> 모든 시스템 통합 필드. 시스템 추가/변경 시 이 문서에도 반영한다.

## 전체 추가 필드

```javascript
// === 길드 회관 ===
guildHall: {
  operations: 0,       // A (0-12)
  infrastructure: 0,   // B
  recovery: 0,         // C
  automation: 0,       // D
  intel: 0,            // E
  pit_control: 0,      // F (BP 첫 클리어 후 해금)
  cargo_control: 0,    // G (Cargo 첫 클리어 후 해금)
  dark_control: 0      // H (BO 첫 클리어 후 해금)
},

// === 길드 평판 ===
guildReputation: 0,        // 0-100

// === 파견 (서브) ===
activeExpeditions: [],     // 실시간 진행 중인 파견
pendingResults: [],        // 완료 후 미수령 결과

// === 용병 상태 ===
stamina: {},               // { mercId: 0~100 }
bonds: {},                 // { 'mercId1_mercId2': bondValue (0-100) }
pairSkillCooldowns: {},    // { 'mercId1_mercId2_skillKey': nextAvailableMs }

// === 친화도 / 보스 토큰 ===
// 구역 친화도는 용병 개인 (Mercenary.zoneAffinity, 별도)
// 공통 친화도는 길드 전체 (이 gameState)
bossTokens: { bp: 0, cargo: 0, blackout: 0 },
commonAffinityNodes: [],   // 공통 트리 해금 노드 키 배열
commonAffinityPoints: 0,   // 공통 트리 사용 가능 포인트 (길드 레벨업 시 +1)

// === NPC ===
npcFavor: {},              // { npcId: 0~100 }
npcInventory: {},          // { npcId: [{ item, isBarter, requiredMaterials }] }
npcQuests: {},             // { npcId: { stage: 1-4, completed: [], activeQuest: questKey } }
npcRotationAt: {},         // { npcId: nextRotationMs }

// === 의뢰 ===
quests: [],                // 활성 의뢰 (NPC 의뢰 라인 + 마을 이벤트 의뢰)

// === 정보 ===
rumors: [],                // [{ key, grade, acquiredAt, expiresAt, used }]

// === 자동화 ===
autoEquipMode: 'manual',   // 'manual' | 'class' | 'atk' | 'hpdef' | 'balanced'
autoSellRules: {
  sellCommon: false,
  sellUncommon: false,
  sellDuplicates: false,
  sellOnOverflow: false
},

// === 장비 ===
lockedItems: [],

// === 제작 ===
materials: {},             // { materialKey: amount }
activeCrafting: [],        // [{ recipeKey, startedAt, completesAt }]

// === 시간 추적 ===
lastActiveAt: 0,           // 마지막 접속 종료 (epoch ms)
totalPlayTime: 0,          // 누적 접속 시간 (ms, 게임 내 시간)
totalRealTime: 0,          // 누적 실시간 (ms, 오프라인 포함)

// === 마을 이벤트 ===
activeEvent: null,         // 현재 표시 중 (즉시 선택 필요)
recentEventKeys: [],       // 최근 10개 (반복 방지)
eventLastTriggeredAt: 0,   // 마지막 이벤트 발동 시각
completedEvents: [],       // 완료된 단발 이벤트 키 (NPC 의뢰 라인 등)

// === 구역 진행 ===
zoneLevel: { bloodpit: 0, cargo: 0, blackout: 0 },  // 메인으로 클리어한 최고 레벨
unlockedZoneLevels: {},    // { 'bloodpit_3': { autoUnlocked: true } } — 자동전투 잠금해제
bossDefeated: { bp: false, cargo: false, blackout: false },

// === BP 전용 (v4) ===
// 파티 공용 수동 아이템 슬롯 (길드회관 F2 해금 시 2칸 활성).
// BP 입장 전 마을에서 사전에 채움. BP 쉬는 곳에서만 사용. 사용 시 비움.
pocketSlots: [null, null],  // [{ key, name } | null, ...]

// === 사망/은퇴 ===
fallenMercs: [],           // 사망 용병 기록
retiredMercs: [],          // 은퇴 용병 기록
permadeathMode: false,     // 영구사망 모드 선택 여부
memorialMoraleBoostUsedAt: 0,  // 추모비 사기 버프 마지막 사용 시각

// === 메타 진행 (영구, NG+ 간 유지) ===
metaProgress: {
  honorPoints: 0,
  cycleCount: 0,
  permanentUpgrades: {},   // { upgradeKey: stackCount }
  achievements: [],
  titles: [],
  unlockedContent: []      // ['nightmare_mode', 'fourth_zone', ...]
},

// === 현재 사이클 ===
currentCycle: {
  startedAt: 0,
  difficulty: 'normal',    // 'normal' | 'challenge_1~3' | 'nightmare' | 'custom'
  challenges: []           // 진행 중인 챌린지
}
```

## 시스템별 매핑

| 필드 | 시스템 |
|---|---|
| `guildHall` | [guild-hall.md](guild-hall.md) |
| `guildReputation` | [reputation.md](reputation.md) |
| `activeExpeditions`, `pendingResults` | [run-simulator.md](run-simulator.md) |
| `stamina` | [stamina.md](stamina.md) |
| `bonds`, `pairSkillCooldowns` | [bonds.md](bonds.md) |
| `commonAffinityNodes`, `commonAffinityPoints`, `bossTokens` | [affinity-tree.md](affinity-tree.md) |
| `npcFavor`, `npcInventory`, `npcQuests`, `npcRotationAt` | [npc-merchants.md](npc-merchants.md) |
| `quests` | [town-events.md](town-events.md) §7 (의뢰 시스템) |
| `rumors` | [rumors.md](rumors.md) |
| `autoEquipMode`, `autoSellRules`, `lockedItems` | [equipment-automation.md](equipment-automation.md) |
| `materials`, `activeCrafting` | [crafting.md](crafting.md) |
| `lastActiveAt`, `totalPlayTime`, `totalRealTime` | [time-system.md](time-system.md) |
| `activeEvent`, `recentEventKeys`, `eventLastTriggeredAt`, `completedEvents` | [town-events.md](town-events.md) |
| `zoneLevel`, `unlockedZoneLevels`, `bossDefeated` | [zone-bloodpit.md](zone-bloodpit.md) / [zone-cargo.md](zone-cargo.md) / [zone-blackout.md](zone-blackout.md) |
| `pocketSlots` | [zone-bloodpit.md §8](zone-bloodpit.md) — BP 수동 아이템 슬롯 |
| `fallenMercs`, `retiredMercs`, `permadeathMode`, `memorialMoraleBoostUsedAt` | [death-retirement.md](death-retirement.md) |
| `metaProgress`, `currentCycle` | [newgame-plus.md](newgame-plus.md) |

## Mercenary 객체 내 필드 (gameState 외)

용병 개인 데이터는 `Mercenary` 객체 내 보관 (직렬화는 SaveManager 처리):

```javascript
{
  id, name, classKey, rarity, level, xp,
  hp, maxHp, atk, def, spd, critRate, critDmg,
  equipment: { weapon, armor, accessory, consumable },
  traits: { positive: [], negative: [], legendary: null },
  zoneAffinity: {           // 구역별 친화도 (용병 개인)
    bloodpit:  { xp: 0, level: 0, nodes: [], points: 0 },
    cargo:     { xp: 0, level: 0, nodes: [], points: 0 },
    blackout:  { xp: 0, level: 0, nodes: [], points: 0 }
  }
}
```

## 기존 코드 호환

- 기존 `affinity` 필드는 제거 (용병 개인 `zoneAffinity`로 이전)
- 기존 `runCount`, `trainingPoints` 등 유지
- 마이그레이션은 [SaveManager.js] 진행 (코드 작업에서 처리)
