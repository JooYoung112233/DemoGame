# 시스템: gameState 스키마 (v2)

> 모든 시스템 통합 필드. 시스템 추가 시 이 문서에도 반영한다.

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
guildReputation: 0,

// === 파견 ===
activeExpeditions: [],
pendingResults: [],

// === 용병 상태 ===
stamina: {},  // { mercId: 0~100 }
bonds: {},    // { 'mercId1_mercId2': bondValue (0-100) }
pairSkillCooldowns: {},  // { 'mercId1_mercId2_skillKey': nextAvailableMs }

// === 친화도 / 보스 토큰 ===
affinity: {
  bloodpit: { xp: 0, level: 0, branch4: null, branch7: null, capstone: false },
  cargo:    { xp: 0, level: 0, branch4: null, branch7: null, capstone: false },
  blackout: { xp: 0, level: 0, branch4: null, branch7: null, capstone: false }
},
bossTokens: { bp: 0, cargo: 0, blackout: 0 },

// === NPC ===
npcFavor: {},      // { npcId: 0~60 }
npcInventory: {},  // { npcId: [item, ...] }

// === 정보 ===
rumors: [],

// === 자동화 ===
autoEquipMode: 'manual',  // 'manual' | 'class' | 'atk' | 'hpdef' | 'balanced'
autoSellRules: {
  sellCommon: false,
  sellUncommon: false,
  sellDuplicates: false,
  sellOnOverflow: false
},

// === 장비 잠금 ===
lockedItems: []
```

## 시스템별 매핑

| 필드 | 시스템 |
|---|---|
| `guildHall` | [guild-hall.md](guild-hall.md) |
| `guildReputation` | [reputation.md](reputation.md) |
| `activeExpeditions`, `pendingResults` | [run-simulator.md](run-simulator.md) |
| `stamina` | [stamina.md](stamina.md) |
| `bonds`, `pairSkillCooldowns` | [bonds.md](bonds.md) |
| `affinity`, `bossTokens` | [affinity-tree.md](affinity-tree.md) |
| `npcFavor`, `npcInventory` | [npc-merchants.md](npc-merchants.md) |
| `rumors` | (TBD: 소문 시스템) |
| `autoEquipMode`, `autoSellRules`, `lockedItems` | [equipment-automation.md](equipment-automation.md) |
