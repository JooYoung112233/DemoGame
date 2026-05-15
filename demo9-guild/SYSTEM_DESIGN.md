# Demo9 — 길드 경영 로그라이크 시스템 설계서

> 최종 업데이트: 2026-05-15  
> 밸런스 수치 상세는 [BALANCE_DESIGN.md](BALANCE_DESIGN.md) 참조

---

## 목차

1. [프로젝트 개요](#1-프로젝트-개요)
2. [폴더 구조](#2-폴더-구조)
3. [씬 흐름 & 네비게이션](#3-씬-흐름--네비게이션)
4. [데이터 파일 레퍼런스](#4-데이터-파일-레퍼런스)
5. [엔티티 시스템](#5-엔티티-시스템)
6. [전투 시스템](#6-전투-시스템)
7. [경제 시스템](#7-경제-시스템)
8. [성장 시스템](#8-성장-시스템)
9. [시설 & 씬 상세](#9-시설--씬-상세)
10. [UI 컴포넌트](#10-ui-컴포넌트)
11. [시스템 매니저](#11-시스템-매니저)
12. [저장 시스템](#12-저장-시스템)

---

## 1. 프로젝트 개요

### 기술 스택
- **엔진**: Phaser 3 (Canvas 렌더링, Arcade Physics)
- **언어**: JavaScript (바닐라, 번들러 없음, 모듈 미사용 — 전역 클래스)
- **해상도**: 1280×720
- **서버**: `npx http-server -p 8080`
- **진입점**: `demo9-guild/index.html`

### 게임 컨셉
판타지 로그라이크 길드 경영. 용병 길드를 운영하며 3개 구역(Blood Pit, Cargo, Blackout)에 용병을 파견해 전투/파밍을 반복하고 길드를 성장시킨다. 목표 플레이타임: 6시간 (약 45런).

### 파일 통계
- **총 44개 JS 소스 파일** + index.html
- 19 씬 / 12 데이터 파일 / 2 엔티티 / 5 시스템 / 6 UI 컴포넌트

---

## 2. 폴더 구조

```
demo9-guild/
├── index.html                          # Phaser 로드 + 스크립트 태그
└── src/
    ├── main.js                         # Phaser 설정 (1280×720, CANVAS)
    ├── data/
    │   ├── units.js                    # CLASS_DATA, RARITY_DATA, RARITY_POOL
    │   ├── traits.js                   # 양성/음성/전설 특성
    │   ├── facilities.js               # 시설 정의, 길드 레벨 XP 테이블
    │   ├── items.js                    # 장비/소재/소비 아이템, 저주 장비
    │   ├── zones.js                    # 3개 구역 + 친화도 트리
    │   ├── names.js                    # 이름 생성 (30×30)
    │   ├── enemies.js                  # 적 스탯 + 웨이브 구성 함수
    │   ├── cards.js                    # 전투 카드 (3 티어)
    │   ├── events.js                   # 마을 이벤트 (3 티어, 15개)
    │   ├── synergies.js                # 시너지 (2인/3인/5인)
    │   ├── recipes.js                  # 제작 레시피 + 강화 비용
    │   └── merchants.js                # 상인 4명 + 호감도 시스템
    ├── entities/
    │   ├── Mercenary.js                # 용병 클래스 (스탯, 특성, 장비)
    │   └── BattleUnit.js               # 전투 유닛 (1012줄, 프로시저럴 렌더링)
    ├── scenes/
    │   ├── TitleScene.js               # 타이틀 (새 게임/이어하기)
    │   ├── TownScene.js                # 메인 허브
    │   ├── RecruitScene.js             # 용병 모집
    │   ├── RosterScene.js              # 로스터 관리
    │   ├── StorageScene.js             # 보관함
    │   ├── ForgeScene.js               # 제작소
    │   ├── AuctionScene.js             # 경매장
    │   ├── TrainingScene.js            # 훈련소
    │   ├── TempleScene.js              # 신전
    │   ├── IntelScene.js               # 정보소
    │   ├── EliteRecruitScene.js        # 고급 모집소
    │   ├── AffinityScene.js            # 친화도 트리
    │   ├── DeployScene.js              # 파견 (구역 선택 + 파티 편성)
    │   ├── PlaceholderBattleScene.js   # 간이 전투 시뮬
    │   ├── BattleScene.js              # Blood Pit 전투
    │   ├── CargoBattleScene.js         # Cargo 열차 방어전
    │   ├── BlackoutBattleScene.js      # Blackout 그리드 탐험
    │   ├── EventScene.js               # 마을 이벤트
    │   └── RunResultScene.js           # 런 결과
    ├── systems/
    │   ├── GuildManager.js             # 길드 레벨/XP/골드/시설
    │   ├── MercenaryManager.js         # 모집 풀 생성, 고용, 해고
    │   ├── StorageManager.js           # 보관함/보안 컨테이너
    │   ├── SaveManager.js              # localStorage 저장/불러오기
    │   └── RunSimulator.js             # 간이 전투 시뮬 로직
    └── ui/
        ├── Button.js                   # 범용 버튼
        ├── Panel.js                    # 테두리 패널
        ├── Tooltip.js                  # 호버 툴팁
        ├── Toast.js                    # 자동 페이드 메시지
        ├── BattleHealthBar.js          # 전투용 HP 바
        └── DamagePopup.js              # 플로팅 데미지 숫자
```

---

## 3. 씬 흐름 & 네비게이션

```
TitleScene
  ├─ 새 게임 → TownScene (초기 상태)
  └─ 이어하기 → TownScene (저장 복원)

TownScene (메인 허브)
  ├─ 모집소 → RecruitScene → TownScene
  ├─ 보관함 → StorageScene → TownScene
  ├─ 로스터 → RosterScene → TownScene
  ├─ 제작소 → ForgeScene → TownScene
  ├─ 경매장 → AuctionScene → TownScene
  ├─ 훈련소 → TrainingScene → TownScene
  ├─ 신전 → TempleScene → TownScene
  ├─ 정보소 → IntelScene → TownScene
  ├─ 고급모집 → EliteRecruitScene → TownScene
  ├─ 친화도 → AffinityScene → TownScene
  ├─ 출발 게이트 → DeployScene
  │   └─ 출발 → BattleScene / CargoBattleScene / BlackoutBattleScene
  │       └─ 전투 종료 → RunResultScene → TownScene
  └─ 랜덤 이벤트 (런 복귀 시 10% 확률) → EventScene → TownScene
```

### 씬 전환 데이터
모든 씬은 `init(data)`로 `gameState` 객체를 전달받는다. `gameState`는 단일 상태 객체로 전 씬에서 공유.

---

## 4. 데이터 파일 레퍼런스

### 4.1 units.js — 클래스 & 희귀도

**6개 클래스:**

| 클래스 | HP | ATK | DEF | 공속(ms) | 사거리 | 이동속도 | 크리율 | 크리뎀 | 스킬 |
|--------|-----|-----|-----|----------|--------|----------|--------|--------|------|
| warrior | 150 | 18 | 10 | 1500 | 50 | 80 | 0.05 | 1.5 | 도발 |
| rogue | 90 | 25 | 4 | 900 | 45 | 120 | 0.20 | 2.0 | 출혈 |
| mage | 70 | 30 | 2 | 2000 | 250 | 50 | 0.10 | 1.8 | 메테오 |
| archer | 80 | 22 | 3 | 1200 | 280 | 60 | 0.15 | 1.8 | 관통 |
| priest | 85 | 10 | 5 | 1800 | 200 | 55 | 0.05 | 1.3 | 힐 |
| alchemist | 75 | 20 | 3 | 1600 | 180 | 55 | 0.08 | 1.6 | 산성병 |

**5개 희귀도:**

| 등급 | 스탯배율 | 성장배율 | 양성특성 | 음성특성 | 고용비배율 |
|------|----------|----------|----------|----------|------------|
| common | 1.0 | 1.0 | 0-1 | 0-1 | 1.0 |
| uncommon | 1.1 | 1.1 | 1 | 0-1 | 1.3 |
| rare | 1.2 | 1.2 | 1-2 | 0-1 | 1.8 |
| epic | 1.35 | 1.3 | 2 | 0-1 | 2.5 |
| legendary | 1.5 | 1.5 | 2-3 | 1 | 4.0 |

**희귀도 풀 (길드 레벨별):**

| 길드 Lv | common | uncommon | rare | epic | legendary |
|---------|--------|----------|------|------|-----------|
| 1 | 70% | 25% | 5% | 0% | 0% |
| 2 | 60% | 30% | 8% | 2% | 0% |
| 3 | 50% | 30% | 15% | 4% | 1% |
| 4 | 40% | 30% | 20% | 8% | 2% |
| 5 | 30% | 30% | 25% | 12% | 3% |
| 6 | 20% | 30% | 28% | 17% | 5% |
| 7 | 15% | 25% | 30% | 22% | 8% |
| 8 | 10% | 20% | 30% | 28% | 12% |

**기본 고용비**: 80G × 희귀도배율

### 4.2 traits.js — 특성

**양성 특성 (20개):**
- 전투: 강인(HP+15%), 맹공(ATK+12%), 철벽(DEF+20%), 신속(공속-10%), 사거리+(사거리+30), 기동력(이동+15%), 크리강화(크리율+10%), 치명(크리뎀+0.3), 흡혈(피해3%회복)
- 생존: 재생(초당HP1%), 회피(15%회피), 근성(HP<30%→DEF2배), 결사(HP<20%→ATK1.5배)
- 경제: 행운(드롭+15%), 수집(소재+25%), 절약(수리비-30%)
- 성장: 빠른학습(XP+20%), 적응(친화XP+25%), 다재(스킬쿨-15%)
- 특수: 선봉(첫 전투 ATK+20%)

**음성 특성 (15개):**
- 허약(HP-10%), 둔감(공속+15%), 겁쟁이(HP<50%→ATK-20%), 욕심(드롭뺏기), 고집(힐-30%), 취약(DEF-15%), 단명(10%즉사), 낭비(수리비+50%), 둔중(이동-20%), 유리몸(크리피해+30%), 부주의(함정뎀+50%), 저주체질(저주장비확률+), 고독(시너지 미적용), 소심(보스전 ATK-15%), 게으름(XP-15%)

**전설 특성 (6개, 클래스 전용):**
- warrior: 불굴의 방패 — HP<10% 시 5초 무적
- rogue: 그림자 습격 — 전투 시작 5초 은신+크리확정
- mage: 마력 폭주 — 스킬이 적 3체 동시 타격
- archer: 꿰뚫는 화살 — 공격이 2체 관통 (뒤쪽 70%)
- priest: 기적의 손길 — 아군 1회 부활 (HP30%)
- alchemist: 금단의 비약 — 아군 전체 ATK+30%/DEF-20% 30초

### 4.3 facilities.js — 시설 & 길드 레벨

**10개 시설:**

| 시설 | 해금 레벨 | 비용 | 기능 |
|------|-----------|------|------|
| recruit (모집소) | 1 | 0G | 용병 모집 |
| storage (보관함) | 1 | 0G | 아이템 관리 |
| gate (출발 게이트) | 1 | 0G | 구역 파견 |
| forge (제작소) | 2 | 500G | 장비 제작/강화 |
| auction (경매장) | 3 | 800G | 아이템 매매 |
| training (훈련소) | 4 | 1200G | 전체 용병 버프 |
| temple (신전) | 5 | 1500G | 치유/부활/축복 |
| intel (정보소) | 6 | 2000G | 구역 정보/임무 |
| eliteRecruit (고급 모집소) | 7 | 3000G | rare+ 용병 모집 |
| vault (금고) | 8 | 4000G | 보안 컨테이너 확장 |

**길드 레벨 XP 테이블:**

| 레벨 | 필요 XP | 누적 XP |
|------|---------|---------|
| 1→2 | 100 | 100 |
| 2→3 | 250 | 350 |
| 3→4 | 500 | 850 |
| 4→5 | 800 | 1,650 |
| 5→6 | 1,200 | 2,850 |
| 6→7 | 1,800 | 4,650 |
| 7→8 | 2,500 | 7,150 |

**로스터 제한:**

| 길드 Lv | 최대 로스터 | 최대 파견 |
|---------|-------------|-----------|
| 1-2 | 4 | 2 |
| 3-4 | 6 | 3 |
| 5-6 | 8 | 4 |
| 7-8 | 10 | 5 |

### 4.4 items.js — 아이템

**아이템 타입 분포:** 장비 35% / 소재 30% / 소비 35%

**장비 종류:** weapon(ATK+), armor(DEF+/HP+), accessory(특수 효과)

**아이템 희귀도 가치 배율:**
- common: 1.0x
- uncommon: 1.5x
- rare: 2.5x
- epic: 5.0x
- legendary: 10.0x

**저주 장비 (6종):**
1. 영혼포식자 (weapon) — ATK+25, HP-15% 영구
2. 고통의 갑옷 (armor) — DEF+20/HP+100, 힐-50%
3. 탐욕의 반지 (accessory) — 골드+30%, 매 전투 HP-10%
4. 피의 검 (weapon) — ATK+30/흡혈8%, 공속+20% 느려짐
5. 저주받은 방패 (armor) — DEF+25, 이동-30%
6. 광기의 목걸이 (accessory) — 크리율+20%/크리뎀+0.5, DEF=0

저주 장비는 장착 해제 불가, 전투 중 debuff 적용.

### 4.5 zones.js — 구역 & 친화도 트리

**3개 구역:**

| 구역 | 해금 | 기본 골드 | 기본 XP | 특징 |
|------|------|-----------|---------|------|
| Blood Pit | Lv1 | 60G | 30 XP | 웨이브 생존 전투 |
| Cargo | Lv3 | 80G | 35 XP | 열차 방어전 + 스테이션 |
| Blackout | Lv5 | 100G | 40 XP | 그리드 탐험 + 저주 |

**구역 레벨 효과 (Lv5/7/10 달성 시):**
- Blood Pit: 희귀드롭+, 체력회복+, 보스 추가보상
- Cargo: 화물보너스+, 장비드롭+, 스테이션 버프+
- Blackout: 저주저항+, 비밀방확률+, 저주장비확률+

**친화도 트리 (구역당 8-10 노드):**
각 구역별 분기형 스킬 트리. 친화도 레벨(0→5)에 따라 해금. 효과: ATK/DEF 보너스, 특수 능력 해금, 구역별 고유 버프.

### 4.6 enemies.js — 적 데이터

**Blood Pit 적:**

| 적 | 역할 | HP | ATK | DEF | 공속 | 사거리 | 이동 | 특수 |
|----|------|-----|-----|-----|------|--------|------|------|
| runner | melee | 70 | 10 | 2 | 1300 | 50 | 110 | — |
| bruiser | melee | 220 | 20 | 10 | 2200 | 55 | 50 | — |
| spitter | ranged | 60 | 14 | 2 | 1600 | 250 | 60 | 출혈15% |
| summoner | ranged | 90 | 8 | 3 | 2000 | 200 | 40 | — |
| elite_runner | melee | 280 | 25 | 7 | 1000 | 50 | 130 | 엘리트 |
| elite_bruiser | melee | 600 | 38 | 16 | 2000 | 55 | 55 | 엘리트 |
| pitlord (보스) | melee | 1500 | 45 | 15 | 1800 | 60 | 70 | 3페이즈 |

**Cargo 적:**

| 적 | 역할 | HP | ATK | DEF | 공속 | 사거리 | 이동 | 특수 |
|----|------|-----|-----|-----|------|--------|------|------|
| mechling | melee | 90 | 12 | 6 | 1500 | 50 | 80 | — |
| turret | ranged | 120 | 18 | 12 | 1800 | 300 | 0 | 고정 |
| shielder | melee | 300 | 10 | 20 | 2200 | 50 | 45 | — |
| bomber | ranged | 50 | 30 | 1 | 2500 | 200 | 90 | 크리20% |
| elite_shielder | melee | 650 | 22 | 28 | 2000 | 55 | 50 | 엘리트 |
| elite_turret | ranged | 350 | 35 | 18 | 1400 | 350 | 0 | 엘리트 |
| ironclad (보스) | melee | 1800 | 35 | 25 | 2200 | 60 | 40 | 3페이즈 |

**Blackout 적:**

| 적 | 역할 | HP | ATK | DEF | 공속 | 사거리 | 이동 | 특수 |
|----|------|-----|-----|-----|------|--------|------|------|
| wraith | melee | 55 | 16 | 1 | 1000 | 50 | 140 | 크리15% |
| cursed_mage | ranged | 70 | 22 | 3 | 2000 | 280 | 50 | 출혈25% |
| bone_golem | melee | 400 | 18 | 8 | 2500 | 55 | 35 | — |
| shade | melee | 40 | 25 | 0 | 800 | 50 | 160 | 크리25% |
| elite_wraith | melee | 350 | 35 | 5 | 900 | 50 | 150 | 엘리트 |
| elite_cursed | ranged | 250 | 42 | 8 | 1600 | 300 | 55 | 출혈35% |
| lich_king (보스) | ranged | 1200 | 50 | 10 | 1600 | 250 | 60 | 3페이즈, 출혈30% |
| curse_avatar | melee | 800 | 45 | 12 | 1100 | 55 | 120 | 엘리트 |
| mansion_lord (보스) | melee | 2000 | 55 | 18 | 1800 | 60 | 50 | 3페이즈, 출혈20% |

**웨이브 구성 공식:**
- `getMaxRounds(zoneLevel) = min(10, 4 + floor((zoneLevel - 1) * 1.5))`
- `scaleMult = 1 + (zoneLevel - 1) * 0.1`
- 보스 스케일: `getBossScaleMult()` — Lv1: 0.5x, Lv2: 0.65x, Lv3: 0.8x, Lv4: 0.9x, Lv5: 1.0x

### 4.7 cards.js — 전투 카드

**3 티어 시스템:**

| 티어 | 카드 수 | 예시 |
|------|---------|------|
| 1 (기본) | 6 | 응급처치, 방어태세, 격려, 함정설치, 집중, 철수준비 |
| 2 (중급) | 10 | 화염진, 치유의빛, 강화방벽, 독안개, 전장분석, ... |
| 3 (고급) | 7 | 궁극기폭발, 생명의축복, 최후의방벽, ... |

전투 중 라운드 시작 시 3장 제시, 1장 선택. 즉시 효과 적용.

### 4.8 events.js — 마을 이벤트

**15개 이벤트, 3 티어:**

| 티어 | 조건 | 이벤트 수 | 예시 |
|------|------|-----------|------|
| 1 | Lv1+ | 5 | 방랑상인, 부상병, 도둑, 축제, 의뢰 |
| 2 | Lv3+ | 5 | 밀수업자, 저주술사, 대장장이, 정보원, 상인조합 |
| 3 | Lv5+ | 5 | 고대유적, 어둠거래, 전설용병, 길드전쟁, 왕실칙령 |

각 이벤트는 2-3개 선택지 제공. 선택에 따라 골드/아이템/특성/용병 획득 또는 손실.

런 복귀 시 10% 확률로 이벤트 발생.

### 4.9 synergies.js — 시너지

**2인 시너지 (7종):**
- 방패+검 (warrior+rogue): ATK+10%, DEF+10%
- 마법진 (mage+priest): 스킬 효과+15%
- 저격조 (archer+rogue): 크리율+10%
- 등등

**3인 시너지 (5종):**
- 전선 (warrior+rogue+archer): 전원 ATK+15%
- 마법군단 (mage+priest+alchemist): 스킬쿨-20%
- 등등

**5인 시너지 (2종):**
- 길드 결속 (5인 모두 다른 클래스): 전체 스탯+10%
- 군단 (5인 모두 같은 클래스): 해당 클래스 특화 대폭 버프

### 4.10 recipes.js — 제작 & 강화

**12개 레시피:** 장비 제작에 소재 + 골드 필요

**강화 비용 (희귀도 승급):**

| 변환 | 비용 | 소재 |
|------|------|------|
| common → uncommon | 200G | 소재 2개 |
| uncommon → rare | 500G | 소재 3개 |
| rare → epic | 1200G | 소재 5개 |
| epic → legendary | 3000G | 소재 8개 |

### 4.11 merchants.js — 상인

**4명의 상인:**

| 상인 | 전문 분야 | 기본 가격배율 |
|------|-----------|---------------|
| 무기상인 | 무기/소재 | 1.0x |
| 방어구상인 | 방어구/액세서리 | 1.0x |
| 약제상인 | 소비/연금술 | 0.9x |
| 암시장상인 | 희귀/저주 | 1.2x |

**호감도 4단계:**

| 단계 | 필요 거래횟수 | 할인율 | 특전 |
|------|-------------|--------|------|
| 낯선 | 0 | 0% | — |
| 친근한 | 5 | 5% | — |
| 신뢰하는 | 15 | 10% | 특별 물품 |
| 동맹 | 30 | 15% | 독점 물품 + 위탁판매 할인 |

---

## 5. 엔티티 시스템

### 5.1 Mercenary (용병)

**구조:**
```
id, classKey, rarity, name, level(1-10), xp
traits[], equipment: {weapon, armor, accessory}
currentHp, alive, zoneAffinity: {bloodpit, cargo, blackout}
```

**스탯 공식:**
```
stat = (baseStat + growth * (level - 1) * growthMult) * rarityStatMult
```
- `growth`: 클래스별 레벨당 성장값 (HP+12, ATK+3, DEF+1.5)
- `growthMult`: 희귀도 성장배율
- `rarityStatMult`: 희귀도 스탯배율

**레벨업 XP 커브:**
```
필요 XP = 40 + level * 20
Lv1→10 총 필요: 1,260 XP
```

**친화도 XP 커브:**
```
필요 XP = 30 + affinityLevel * 20
Lv0→5 총 필요: 350 XP
```

**훈련 버프 (TrainingScene에서 포인트 투자):**
- HP: +3% per point
- ATK: +3% per point
- DEF: +2 per point
- 쿨다운: -3% per point
- 각 카테고리 최대 10포인트

**직렬화:** `toJSON()` / `fromJSON()` — localStorage 저장/복원 지원

### 5.2 BattleUnit (전투 유닛)

**1012줄의 핵심 전투 엔티티.** 프로시저럴 픽셀 아트 렌더링.

**생성:**
- `BattleUnit.fromMercenary(scene, merc, x, y, team)` — 아군
- `BattleUnit.fromEnemyData(scene, enemyKey, x, y, scaleMult)` — 적군

**프로시저럴 렌더링:**
- 클래스별 고유 외형 (warrior: 무거운 갑옷, rogue: 후드, mage: 로브+지팡이, ...)
- 적 유닛별 고유 외형 (runner: 날렵, turret: 포탑, wraith: 유령, ...)
- 보스: 더 크고 화려한 이펙트
- 애니메이션: idle, attack, hit, die

**전투 메커니즘 (update 루프):**
1. 타겟 선택 (가장 가까운 적, 도발 우선)
2. 사거리 내 → 공격 / 밖 → 이동
3. 공격 → 데미지 계산 (ATK - DEF, 최소 1) → 크리티컬 → 출혈
4. 힐 (priest) → 가장 HP 비율 낮은 아군
5. AoE (mage meteor) → 범위 내 전체
6. 실드 부여 → 힐 시 최대HP 10% 실드

**보스 페이즈 시스템 (3페이즈):**
- Phase 1 (100%-60%): 기본 패턴
- Phase 2 (60%-30%): 강화 패턴 (공속+, ATK+)
- Phase 3 (30%-0%): 광폭화 (대폭 강화, 특수 능력)

| 보스 | Phase 2 | Phase 3 |
|------|---------|---------|
| pitlord | ATK+20%, 공속-200ms | 전체 AoE, 흡혈 |
| ironclad | DEF+50%, 실드 | 돌진, 방어무시 |
| lich_king | 소환, 저주 강화 | 전체 저주, 즉사 시도 |
| mansion_lord | 은신, 함정 | 분열, 다중 공격 |

**오버킬 스플래시:**
적 처치 시 초과 데미지의 50%가 근처 적 최대 3체에게 전파.

**전설 특성 전투 효과:**
각 클래스의 전설 특성이 전투 중 자동 발동 (위 traits.js 참조).

**사망 구제:**
- 시너지 사망구제: 특정 시너지 활성 시 1회 HP 1로 생존
- 부활 토큰: 신전에서 구매 (500G), 1회 부활 HP30%

---

## 6. 전투 시스템

### 6.1 Blood Pit (BattleScene.js) — 웨이브 생존

**레이아웃:** 1280×720, 지면 Y=480
- 아군: 좌측 (X=100~300)
- 적군: 우측 (X=900~1100)

**라운드 진행:**
1. 라운드 시작 → 카드 3장 제시 (1장 선택)
2. `getEnemyComposition(round, zoneLevel, 'bloodpit')` → 적 스폰
3. 실시간 자동 전투 (BattleUnit.update())
4. 적 전멸 → 다음 라운드 / 아군 전멸 → 패배
5. 최종 라운드 → 보스 등장

**보상:**
- 라운드 XP: `10 + currentRound * 5 + (보스 ? 30 : 0)`
- 라운드 골드: `15 + currentRound * 10 + (보스 ? 50 : 0)`
- 적 처치 골드: `random(8,18) + currentRound * 4 + (엘리트 ? 20 : 0) + (보스 ? 100 : 0)`
- 드롭 확률: `0.20 + (엘리트 ? 0.30 : 0) + (보스 ? 0.80 : 0)`

**HUD:**
- 상단: 라운드 번호, 적 남은 수
- 하단: 용병 HP 바, 카드 슬롯
- 철수 버튼 (진행 보상 유지, 보스 보상 없음)

### 6.2 Cargo (CargoBattleScene.js) — 열차 방어전

**레이아웃:** 5개 스테이션 (deck/hold/engine/infirmary/bridge)

**고유 메커니즘:**
- **화물 HP**: 보호 대상 (화물 파괴 시 패배)
- **스테이션 이동**: 용병을 스테이션 간 배치/이동
- **폭풍 구간**: 10초 간격 전체 AoE 데미지
- **보급 이벤트**: 랜덤 스테이션에 보급 크레이트 생성

**보상:**
- 화물 생존 보너스: `50 + cargoHp * 1.5`
- 스테이션 점령 보너스: 스테이션당 추가 골드
- 기본 보상은 Blood Pit과 동일 공식

### 6.3 Blackout (BlackoutBattleScene.js) — 그리드 탐험

**레이아웃:** 5×4 ~ 6×5 그리드 (층수에 따라 확대)

**방 타입:**

| 타입 | 확률 | 내용 |
|------|------|------|
| combat | 35% | 적과 전투 |
| trap | 15% | 함정 (HP 피해) |
| treasure | 10% | 보물 (아이템/골드) |
| shop | 8% | 상점 (구매 가능) |
| secret | 5% | 비밀방 (고급 보상) |
| clue | 7% | 단서 (보스 약점 정보) |
| empty | 15% | 빈 방 |
| boss | 5% | 보스방 |

**저주 시스템:**
- `curseLevel`: 0에서 시작, 탐험할수록 상승
- 효과: 적 강화, 함정 강화, 저주 장비 드롭 확률 증가
- 보스 스케일: `0.4 + currentFloor * 0.2 + curseLevel * 0.05`

**저주 장비 드롭:** `cursedChance = 0.08 + curseLevel * 0.04`

**보상:**
- 적 처치 골드: `random(8,18) + depth * 4 + currentFloor * 5`
- 비밀방: 고급 장비/소재 확정 드롭
- 보스 처치: 대량 보상 + 다음 층 해금

---

## 7. 경제 시스템

### 7.1 골드 수입원

| 출처 | 평균 수입/런 | 비고 |
|------|-------------|------|
| 라운드 보상 | 100-200G | 라운드 수에 비례 |
| 적 처치 | 50-150G | 엘리트/보스 추가 |
| 보스 처치 | 100-150G | 보스 기본 + 추가 |
| 아이템 판매 | 30-100G | 경매장 70% 가치 |
| 화물 보너스 (Cargo) | 50-200G | 화물 HP에 비례 |
| 이벤트 보상 | 0-100G | 랜덤 |

**런당 평균 수입: 약 200-400G** (초반 200G, 후반 400G+)

### 7.2 골드 지출처

| 지출 | 비용 | 빈도 |
|------|------|------|
| 용병 고용 | 80-320G | 필요 시 |
| 시설 해금 | 500-4000G | 1회 |
| 장비 제작 | 200-3000G | 수시 |
| 장비 강화 | 200-3000G | 수시 |
| 훈련 | 50-200G/pt | 수시 |
| 힐/부활 | 20-500G | 전투 후 |
| 축복 | 200-300G | 선택 |
| 경매 구매 | 가변 | 수시 |

### 7.3 아이템 경제

**드롭률:**
- 일반 적: 20%
- 엘리트: 50% (20% + 30%)
- 보스: 100% (20% + 80%)

**판매:** 경매장에서 아이템 가치의 70%로 즉시 판매, 또는 위탁 판매(시간 소요, 더 높은 가격)

**시장 트렌드:** 3 이벤트마다 카테고리별 수요/공급 변동 (0.5x~1.6x)

### 7.4 경매장 (AuctionScene.js)

**5개 탭:**
1. **구매**: NPC 판매 물품 목록
2. **판매**: 즉시 판매 (가치 70%)
3. **위탁**: 시간 경과 후 판매 (가치 80-120%)
4. **입찰**: 경매 참여
5. **기록**: 거래 내역

---

## 8. 성장 시스템

### 8.1 길드 레벨

- **XP 소스**: 전투 런 (라운드 XP + 보스 XP)
- **레벨업 효과**: 시설 해금, 로스터 확장, 희귀도 풀 개선, 훈련 포인트+1
- **총 필요 XP**: 7,150 (Lv1→8)
- **예상 도달**: 약 45런 (6시간)

### 8.2 용병 레벨

- **XP 소스**: `floor(guildXp * 0.5 + rounds * 5 + (success ? 15 : 0))` (RunResultScene)
- **레벨업 효과**: 스탯 성장 (growth × growthMult × rarityMult)
- **총 필요 XP**: 1,260 (Lv1→10)
- **예상 도달**: 약 20런 (common), 15런 (legendary)

### 8.3 친화도

- **XP 소스**: `floor(10 + rounds * 3 + (success ? 10 : 0))` (RunResultScene)
- **구역별 독립**: 해당 구역 출격 시만 획득
- **레벨업 효과**: 친화도 트리 노드 해금 가능
- **총 필요 XP**: 350 (Lv0→5)

### 8.4 훈련소

- **훈련 포인트**: 길드 레벨업 시 1포인트 획득
- **4 카테고리**: HP, ATK, DEF, 쿨다운 (각 최대 10)
- **전 용병 적용**: 로스터 전체에 영구 버프

### 8.5 장비 성장

- **획득**: 전투 드롭, 제작, 경매 구매
- **강화**: 레시피 소재 + 골드로 희귀도 승급
- **저주 장비**: 강력하지만 영구 디버프, 해제 불가

---

## 9. 시설 & 씬 상세

### 9.1 TownScene (메인 허브)

**레이아웃 (1280×720):**
```
┌────────────┬──────────────────────────┬──────────────────┐
│ 로스터      │   시설 그리드 (3×3+1)     │ 최근 소식        │
│ (x:0-280)  │   (x:280-980)            │ (x:980-1280)    │
│            │   120×90 셀, 20px 간격     │                 │
│ 용병 카드   │   ⚔모집소 📦보관함 🚪게이트│ 이벤트 로그      │
│ 클릭→상세  │   🔨제작소 🏛경매장 🏋훈련소│                 │
│            │   ⛪신전  🔍정보소 👑고급   │                 │
│            │        🔒금고              │                 │
└────────────┴──────────────────────────┴──────────────────┘
```

**색상 팔레트:**
- 배경: `#0a0a1a`
- 패널: `#151525` / `0x333355`
- 잠긴 시설: `0x222233`
- 해금 시설: `0x1a2a3a` / `0x446688`
- 골드: `#ffcc44`
- 길드 레벨: `#44aaff`
- 버튼: `#ffaa44`

### 9.2 RecruitScene (용병 모집)

- 모집 풀: 3-5명 (길드 레벨 기반 희귀도)
- 용병 카드: 클래스, 희귀도, 스탯, 특성 표시
- 고용 비용: `BASE_HIRE_COST(80) × rarityHireCostMult`
- 리롤: 50G (풀 새로 생성)

### 9.3 RosterScene (로스터 관리)

- 용병 상세 정보: 레벨, XP, 스탯, 특성, 장비
- 장비 장착/교체
- 해고: 확인 후 영구 제거
- 치유: 30G (부상 용병)

### 9.4 StorageScene (보관함)

- 일반 보관함: 아이템 목록, 판매 (가치 70%)
- 보안 컨테이너: 용병 사망 시 보존되는 아이템
- 금고 해금 시 보안 컨테이너 용량 확장

### 9.5 ForgeScene (제작소)

**3개 탭:**
1. **제작**: 레시피 목록, 소재+골드로 장비 생산
2. **강화**: 기존 장비 희귀도 승급
3. **소재**: 보유 소재 목록

### 9.6 AuctionScene (경매장)

위 7.4 참조. 5탭 구조, 시장 트렌드 표시, 위탁 판매 시스템.

### 9.7 TrainingScene (훈련소)

- 4개 카테고리 슬라이더
- 포인트 투자/회수
- 현재 효과 실시간 표시

### 9.8 TempleScene (신전)

**3가지 서비스:**
1. **치유**: 20G/용병 (HP 완전 회복)
2. **부활**: 500G (사망 용병 1회 부활, HP 30%)
3. **축복**: 200-300G (임시 버프, 다음 런 적용)

### 9.9 IntelScene (정보소)

- 구역별 정보 표시 (적 구성, 보스 패턴)
- 특수 임무 목록 (추가 보상 조건)
- 구역 레벨 효과 확인

### 9.10 EliteRecruitScene (고급 모집소)

- rare+ 전용 모집 풀
- 고용비 2배
- 리롤: 300G
- 전설 용병 등장 확률 상승

### 9.11 AffinityScene (친화도 트리)

- 구역별 분기형 스킬 트리 시각화
- 노드 클릭 → 해금 (친화도 레벨 조건)
- 효과: 구역별 전투 버프, 특수 능력

### 9.12 EventScene (마을 이벤트)

- 이벤트 텍스트 + 삽화
- 2-3개 선택지 버튼
- 선택 결과 즉시 적용 (골드, 아이템, 특성 등)

### 9.13 DeployScene (파견)

- 구역 3개 선택 (해금 조건 표시)
- 파티 편성 (최대 인원은 길드 레벨에 따라)
- 시너지 미리보기
- 출발 버튼 → 해당 구역 전투 씬으로 전환

### 9.14 RunResultScene (런 결과)

**표시 내용:**
- 클리어/실패 여부
- 획득 골드/XP
- 아이템 획득 목록
- 용병별 XP 획득, 레벨업 표시
- 친화도 XP 획득
- 사망 용병 표시 (영구 퇴장)
- 위탁 판매 결과, 시장 트렌드 변동

**보상 공식:**
- 용병 XP: `floor(guildXp * 0.5 + rounds * 5 + (success ? 15 : 0))`
- 친화도 XP: `floor(10 + rounds * 3 + (success ? 10 : 0))`

---

## 10. UI 컴포넌트

### 10.1 Button.js
범용 버튼. 호버 시 밝기 변화, 비활성화 상태, 텍스트 라벨.
```
new Button(scene, x, y, width, height, text, callback, options)
options: { color, hoverColor, disabledColor, fontSize, fontColor }
```

### 10.2 Panel.js
테두리 패널. 배경색 + 테두리 색상.
```
new Panel(scene, x, y, width, height, options)
options: { fillColor, borderColor, borderWidth, alpha }
```

### 10.3 Tooltip.js
호버 시 표시되는 정보 툴팁. 자동 위치 조정.
```
Tooltip.show(scene, x, y, text, options)
Tooltip.hide(scene)
```

### 10.4 Toast.js
화면 하단 자동 페이드 메시지. 골드 획득, 레벨업 등 알림.
```
Toast.show(scene, message, duration)
```

### 10.5 BattleHealthBar.js
전투 중 유닛 위 HP 바. 실드 별도 표시 (파란색).
```
new BattleHealthBar(scene, unit)
update() — HP/실드 비율에 따라 바 길이 조정
```

### 10.6 DamagePopup.js
플로팅 데미지 숫자. 위로 떠오르며 페이드아웃.
```
DamagePopup.show(scene, x, y, damage, options)
options: { color, fontSize, isCrit, isHeal }
```

---

## 11. 시스템 매니저

### 11.1 GuildManager.js

| 메서드 | 기능 |
|--------|------|
| `addXp(state, amount)` | 길드 XP 추가, 레벨업 체크 → 훈련포인트+1 |
| `addGold(state, amount)` | 골드 추가 |
| `spendGold(state, amount)` | 골드 차감 (부족 시 false) |
| `unlockFacility(state, key)` | 시설 해금 (비용 차감) |
| `getMaxRoster(state)` | 현재 최대 로스터 수 |
| `getMaxDeploy(state)` | 현재 최대 파견 수 |
| `canUnlock(state, key)` | 해금 가능 여부 (레벨+골드) |

### 11.2 MercenaryManager.js

| 메서드 | 기능 |
|--------|------|
| `generateRecruitPool(state)` | 길드레벨 기반 3-5명 생성 |
| `hire(state, mercId)` | 골드 차감 후 pool→roster 이동 |
| `dismiss(state, mercId)` | roster에서 제거 |
| `generateName()` | 랜덤 이름 (prefix + suffix) |
| `rollRarity(guildLevel)` | RARITY_POOL 기반 희귀도 결정 |

### 11.3 StorageManager.js

| 메서드 | 기능 |
|--------|------|
| `addItem(state, item)` | 보관함에 아이템 추가 |
| `removeItem(state, itemId)` | 아이템 제거 |
| `moveToSecure(state, itemId)` | 보안 컨테이너로 이동 |
| `getStorageCount(state)` | 현재 보관함 아이템 수 |

### 11.4 SaveManager.js

| 메서드 | 기능 |
|--------|------|
| `save(state)` | localStorage `'demo9-guild-save'`에 저장 |
| `load()` | 저장 데이터 로드 + Mercenary.fromJSON() 복원 |
| `hasSave()` | 저장 데이터 존재 여부 |
| `deleteSave()` | 저장 데이터 삭제 |

**저장 시점:** 고용, 해고, 시설 해금, 런 복귀, 출발 직전

### 11.5 RunSimulator.js (간이 전투)

PlaceholderBattleScene에서 사용. 3초간 텍스트 이벤트 애니메이션.
```
simulate(state, zone, party) → {
  success: boolean,
  rounds: number,
  gold: number,
  xp: number,
  loot: Item[],
  casualties: mercId[]
}
```
성공률: `60% + party.length * 5%`, 사망확률: 15%/용병

---

## 12. 저장 시스템

### gameState 구조

```javascript
{
  guildLevel: 1,
  guildXp: 0,
  gold: 200,
  roster: [],               // Mercenary[]
  storage: [],              // Item[]
  secureContainer: [],      // Item[]
  unlockedFacilities: ['recruit','storage','gate'],
  recruitPool: [],          // Mercenary[]
  runCount: 0,
  zoneLevel: { bloodpit: 1, cargo: 0, blackout: 0 },
  trainingPoints: 0,
  training: { hp: 0, atk: 0, survival: 0, recovery: 0 },
  merchantFavor: { weapon: 0, armor: 0, potion: 0, black: 0 },
  consignments: [],         // 위탁 판매 목록
  marketTrend: {},          // 카테고리별 시세 변동
  affinityTrees: {},        // 구역별 해금된 노드
  blessings: [],            // 활성 축복 목록
  missions: []              // 활성 임무 목록
}
```

### localStorage 키
`'demo9-guild-save'` — JSON.stringify된 전체 gameState

### 복원 프로세스
1. `SaveManager.load()` → JSON 파싱
2. `roster` 배열의 각 항목을 `Mercenary.fromJSON()`으로 복원
3. 나머지 원시값/배열은 그대로 복원

---

## 부록: Phaser 설정 (main.js)

```javascript
// 1280×720, Canvas 렌더링
// 19개 씬 등록
// Arcade Physics (기본 설정)
// 배경색: #0a0a1a
```

모든 클래스는 `<script>` 태그로 순서대로 로드 (모듈 미사용).
스크립트 로드 순서: data → entities → systems → ui → scenes → main
