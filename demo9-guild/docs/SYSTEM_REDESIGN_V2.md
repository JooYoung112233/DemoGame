# Demo9 — 길드 경영 v2 통합 시스템 재설계

> ⚠️ **이 문서는 아카이브입니다.**
> 본 통합 문서는 시스템별 파일로 분리되었습니다. 신규 변경은 분리된 파일에 반영하세요.
>
> - 진입점: [INDEX.md](INDEX.md)
> - 전반 개요: [OVERVIEW.md](OVERVIEW.md)
> - 시스템별: [systems/](systems/)
>
> 이 파일은 분리 이전 상태의 스냅샷으로만 유지합니다. **편집 금지.**

> **문서 목적**: SYSTEM_DESIGN.md + BALANCE_DESIGN.md 간 불일치 해소 및 신규 시스템 추가를 위한 재설계 문서.
> **플레이타임 목표**: 12시간 (능동 플레이, PC)
> **핵심 원칙**: "쉴 틈 없이 뭔가 계속 하는 게임"

---

## 목차

1. [디자인 철학](#1-디자인-철학)
2. [문서 간 불일치 정리](#2-문서-간-불일치-정리)
3. [메인 파티 vs 서브 파견](#3-메인-파티-vs-서브-파견)
4. [길드 회관 (Guild Hall)](#4-길드-회관-guild-hall)
5. [RunSimulator v2 (서브 파견)](#5-runsimulator-v2-서브-파견)
6. [피로도 시스템 (Stamina)](#6-피로도-시스템-stamina)
7. [장비 시스템 + 자동화](#7-장비-시스템--자동화)
8. [특성 제거/변환 시스템](#8-특성-제거변환-시스템)
9. [NPC 상인 시스템](#9-npc-상인-시스템)
10. [마을 이벤트 확장](#10-마을-이벤트-확장)
11. [구역 특수 업그레이드 (친화도 트리와 별개)](#11-구역-특수-업그레이드)
12. [12시간 페이싱 가이드](#12-12시간-페이싱-가이드)
13. [남은 작업 목록 (TODO)](#13-남은-작업-목록)

---

## 1. 디자인 철학

### 1.1 핵심 루프 변경

```
[기존 6시간]
  1런 = 직접 전투 5-12분 → 결과 → 마을 → 다시 출발
  45런 × 8분 = 6시간

[v2 12시간]
  메인 파티: 최전선 구역 도전 (직접 전투, BattleScene)
  서브 파티들: 이미 깬 구역에 자동 파견 (시간 경과형)
  마을: 시설 운영, 파견 결과 처리, 장비 정리, NPC 거래 등
  → 다키스트 던전처럼 여러 파티를 동시에 굴리는 구조
```

### 1.2 "쉴 틈 없음" 보장 장치

- 동시 파견 슬롯이 항상 차 있어야 함
- 파견 회전이 짧음 (가장 짧은 게 42초~2분)
- 결과 처리 → 즉시 재파견 사이클
- 마을에서 시설/장비/경매/NPC 일거리가 끊기지 않음
- 자동화를 투자해서 잡일을 줄이되, 투자 자체가 게임 목표

### 1.3 "쉴 틈 없음" 공식

```
분당 이벤트 수 = 동시파견슬롯 / 평균파견시간(분)
목표: 항상 ≥ 1 이벤트/분

예시 (중반, 서브 5슬롯):
  BP Lv1 × 2: 1.5분 회전 → 분당 1.3건
  Cargo Lv1: 4분 → 분당 0.25건
  BP Lv3: 3분 → 분당 0.33건
  BO Lv1: 8분 → 분당 0.13건
  합계: ~분당 2건 = 30초마다 뭔가 일어남
```

---

## 2. 문서 간 불일치 정리

### 2.1 클래스 베이스 스탯

**결정**: 12시간 플레이타임에 맞게 새로 산출 (통합 시뮬 시 확정)

**현황 비교** (참고용):

| 클래스 | SYSTEM HP/ATK/DEF | BALANCE HP/ATK/DEF | 차이 |
|---|---|---|---|
| warrior | 150 / 18 / 10 | 250 / 20 / 18 | BALANCE +66% HP |
| rogue | 90 / 25 / 4 | 160 / 35 / 8 | BALANCE +78% HP |
| mage | 70 / 30 / 2 | 120 / 45 / 5 | BALANCE +71% HP |
| archer | 80 / 22 / 3 | 150 / 30 / 7 | BALANCE +88% HP |
| priest | 85 / 10 / 5 | 180 / 12 / 10 | BALANCE +112% HP |
| alchemist | 75 / 20 / 3 | 170 / 15 / 12 | BALANCE HP↑ ATK↓ |

공속/사거리/이동속도도 다름. **통합 시뮬(섹션 12) 후 확정 예정.**

### 2.2 희귀도 풀

**결정**: 12시간 플레이용으로 새로 짜기

| 길드 Lv | SYSTEM (1레벨 단위) | BALANCE (2레벨 묶음) |
|---|---|---|
| 1 | 70/25/5/0/0 | 60/35/5/0/0 |
| 3 | 50/30/15/4/1 | 40/40/18/2/0 |
| 5 | 30/30/25/12/3 | 25/35/30/9/1 |
| 7 | 15/25/30/22/8 | 15/30/30/20/5 |
| 8 | 10/20/30/28/12 | — |

**새 풀은 통합 시뮬(섹션 12) 후 확정 예정.**

### 2.3 고용 비용

| 희귀도 | SYSTEM (배율) | BALANCE (실수치) | 일치 |
|---|---|---|---|
| common | 80G × 1.0 = 80G | 80G | ✓ |
| uncommon | 80G × 1.3 = 104G | 120G | ✗ |
| rare | 80G × 1.8 = 144G | 200G | ✗ |
| epic | 80G × 2.5 = 200G | 320G | ✗ |
| legendary | 80G × 4.0 = 320G | 480G | ✗ |

**결정**: 통합 시뮬에서 골드 커브에 맞춰 재산출.

### 2.4 특성 목록 불일치

- 양성: SYSTEM 19개 vs BALANCE 20개
- 음성: 라인업 다름 (저주체질/단명/고독 vs greedy/stubborn/lone_wolf)
- 전설: "기적의 손길(부활)" vs "성스러운 희생(죽기 직전 힐)"

**결정**: 통합 특성 테이블 재정의 (섹션 8에서).

### 2.5 적 스탯

- Blood Pit / Cargo 주요 적 수치: 대체로 일치
- BALANCE에 elite_runner, pitlord, ironclad, lich_king, mansion_lord 누락
- **결정**: SYSTEM_DESIGN을 기준으로 하되, 통합 시뮬 시 재조정

---

## 3. 메인 파티 vs 서브 파견

### 3.1 메인 파티 (직접 도전)

- **언제**: 새 구역 첫 도전 / 보스전 / 구역 레벨 갱신 도전
- **인원**: 길드회관 A 트리에 따라 3 → 최대 6-7명
- **방식**: 기존 BattleScene 직접 전투 (카드 선택 능동 개입)
- **소요**: 5-10분 능동 플레이
- **보상**: 서브 파견의 1.5-2배 (위험 프리미엄)
- **목적**: **구역 레벨업** (서브 파견 효율 끌어올리기 + 다음 구역 해금)

### 3.2 서브 파견 (자동 회전)

- **언제**: 메인이 깬 구역 레벨까지 자동 파밍
- **인원**: 2-3명 (메인보다 적음)
- **방식**: 시간 경과형, 시뮬 결과만 반환
- **소요**: 42초~30분 (구역/레벨/파티/업그레이드에 따라)
- **보상**: 기본치 (꾸준한 자원 흐름)
- **목적**: 골드/장비/소재/XP/친화도 누적

### 3.3 메인 파티 "출전 의미" 보장 장치

서브로만 굴려도 클리어가 안 되도록:

- **구역 레벨업은 메인만 가능** — 서브 파견으로 구역 레벨 못 올림
- **보스 처치는 메인만** — 다음 구역 해금에 필수
- **메인 전용 보상**: 보스 드롭 전설장비, 친화도 트리 핵심 노드 해금 토큰
- **서브 파견은 클리어된 레벨까지만** — 미클리어 구역엔 못 보냄

### 3.4 구역 진행 흐름

```
Blood Pit Lv1 (메인) → 클리어 → Lv1 서브 파밍 가능
Blood Pit Lv2 (메인) → 클리어 → Lv1-2 서브 파밍 가능
  ...
Blood Pit Lv10 (메인) → 보스 처치 → Cargo 해금
  동시에 BP Lv1-10 전부 서브 파밍 가능
Cargo Lv1 (메인) → 클리어 → Cargo Lv1 서브 + BP 계속
  ...
```

---

## 4. 길드 회관 (Guild Hall) — 확정 v3

### 4.1 컨셉

길드 회관 = 게임 시작부터 보유. 모든 시스템의 **메타 업그레이드 트리**.

- 기존 시설 해금(제작소/경매장 등) = 수평 확장 (새 활동 추가)
- 길드 회관 = 수직 확장 (기존 활동 효율 강화)

### 4.2 구조

**8 카테고리 × 12 단계 = 96 업그레이드**

| 카테고리 | 코드 | 컨셉 | 해금 조건 |
|---|---|---|---|
| A. 운영 | `operations` | 파견 슬롯, 메인 파티 인원 | 기본 |
| B. 인프라 | `infrastructure` | 로스터, 보관함, 보안 컨테이너 | 기본 |
| C. 휴식 | `recovery` | 스테미너 회복, 부상, 사망 구제 | 기본 |
| D. 자동화 | `automation` | 자동 장착/판매/회수/재파견 | 기본 |
| E. 정보 | `intel` | 미리보기, 소문, 평판 가속 | 기본 |
| F. 핏 통제 | `pit_control` | BP 핏게이지 메커니즘 변경 | BP 첫 클리어 |
| G. 화물 통제 | `cargo_control` | Cargo 열차 칸 메커니즘 변경 | Cargo 첫 클리어 |
| H. 어둠 통제 | `dark_control` | BO 저주 트레이드오프 변경 | BO 첫 클리어 |

### 4.3 비용 구조

```
cost(stage) = round(baseCost × 1.5^(stage-1))

예시 (baseCost = 300):
  Lv1:   300G      Lv5:  1,519G     Lv9:   7,695G
  Lv2:   450G      Lv6:  2,278G     Lv10: 11,543G
  Lv3:   675G      Lv7:  3,418G     Lv11: 17,314G
  Lv4: 1,013G      Lv8:  5,127G     Lv12: 25,972G
```

### 4.4 게이트 조건

| 단계 | 길드 평판 | 길드 Lv |
|---|---|---|
| 1-3 | — | 1 |
| 4-6 | — | 2-3 |
| 7-9 | 15 | 4-5 |
| 10-12 | 35 | 6-8 |

### 4.5 A. 운영 (Operations) — base 300G

| 단계 | 효과 |
|---|---|
| 1 | 서브 슬롯 +1 (총 2) |
| 2 | 서브 슬롯 +1 (총 3) |
| 3 | 메인 파티 +1 (3→4) |
| 4 | 서브 슬롯 +1 (총 4) |
| 5 | 서브 슬롯 +1 (총 5) |
| 6 | 메인 파티 +1 (4→5) |
| 7 | 동일구역 중복 파견 (같은 곳 2팀) |
| 8 | 서브 슬롯 +1 (총 6) |
| 9 | 서브 슬롯 +1 (총 7) |
| 10 | 메인 파티 +1 (5→6) |
| 11 | 동일구역 중복 +1 (3팀) + 서브 슬롯 +1 (총 8) |
| 12 | **🏆 총사령관**: 파견 중 파티 교체 가능 + 서브 슬롯 +2 (총 10) |

누적: 서브 1→10, 메인 3→6

### 4.6 B. 인프라 (Infrastructure) — base 400G

| 단계 | 효과 |
|---|---|
| 1 | 로스터 +2 (4→6) |
| 2 | 보관함 +4 (12→16) |
| 3 | 로스터 +2 (6→8) |
| 4 | 보안 컨테이너 +1 (2→3) |
| 5 | 보관함 +6 (16→22) |
| 6 | 로스터 +3 (8→11) |
| 7 | 보관함 카테고리 탭 (장비/소재/소비 분리) |
| 8 | 보안 컨테이너 +2 (3→5) |
| 9 | 로스터 +4 (11→15) |
| 10 | 보관함 +8 (22→30) + 장비 전용 보관함 (20칸) |
| 11 | 사망 시 인벤 보존 50%→80% |
| 12 | **🏆 차원 창고**: 보관함 +30 (총 60) + 보안 컨테이너 무제한 |

누적: 로스터 4→15, 보관함 12→60

### 4.7 C. 휴식 (Recovery) — base 250G

| 단계 | 효과 |
|---|---|
| 1 | 마을 스테미너 +0.5/분 (1→1.5) |
| 2 | 마을 스테미너 +0.5/분 (→2) |
| 3 | 부상 회복 시간 -30% |
| 4 | 파견 후 스테미너 -10 페널티 면제 |
| 5 | 마을 스테미너 +1/분 (→3) |
| 6 | 사망 위기 자동 생존 1회 (HP1, 쿨다운 5런) |
| 7 | 마을 복귀 시 +20 스테미너 즉시 |
| 8 | 마을 스테미너 +2/분 (→5) |
| 9 | 부상 회복 시간 -50% (누적 -80%) |
| 10 | 사망 위기 쿨다운 5런→2런 |
| 11 | 부상자도 서브 파견 가능 (보상 50%) |
| 12 | **🏆 불사의 길드**: 사망 위기 자동 생존 2회/전투 + 마을 스테미너 즉시 풀회복 |

### 4.8 D. 자동화 (Automation) — base 500G

| 단계 | 효과 |
|---|---|
| 1 | 자동 장착 해금 (수동 버튼) |
| 2 | 자동 판매 해금 (common) |
| 3 | 새 장비 획득 시 자동 장착 옵션 |
| 4 | 자동 판매 (uncommon 포함) |
| 5 | 파견 출발 시 자동 최적화 |
| 6 | 자동 회수 (귀환 즉시 보상 처리) |
| 7 | 자동 재파견 (같은 구역 자동 출발) |
| 8 | 자동 판매 (rare 이하 잠금 안된 중복) |
| 9 | 자동 위탁 (잡템 자동 위탁 등록) |
| 10 | 자동 치유/스테미너 관리 |
| 11 | 자동 모집 (조건 매칭 용병 자동 고용) |
| 12 | **🏆 완전 자동화**: 풀 자동 (장비강화 + NPC거래 + 카드선택) |

### 4.9 E. 정보 (Intel) — base 600G

| 단계 | 효과 |
|---|---|
| 1 | 파견 결과 미리보기 (예상 보상/위험) |
| 2 | 소문 슬롯 +1 (1→2) |
| 3 | NPC 매물 갱신 시간 -30% |
| 4 | 적 능력치/약점 표시 |
| 5 | 마을 이벤트 알림 우선순위 |
| 6 | 소문 슬롯 +1 (→3) |
| 7 | 길드 평판 누적 +30% 가속 |
| 8 | 모든 NPC 호감도 +25% 가속 |
| 9 | 소문 슬롯 +1 (→4) |
| 10 | 시장 트렌드 예측 (다음 변동 미리보기) |
| 11 | 음성 특성 위험도 표시 (모집 풀) |
| 12 | **🏆 전능의 시야**: 모든 정보 공개 + 보스 약점 자동 입수 + 소문 +2 (→6) |

### 4.10 F. 핏 통제 (Pit Control) — base 800G

**해금**: Blood Pit Lv1 첫 클리어. 핏 게이지 메커니즘 변경에 집중.

| 단계 | 효과 |
|---|---|
| 1 | 핏 게이지 최대치 +50% |
| 2 | 핏 게이지 MAX 시 드롭률 +20% → +35% |
| 3 | 핏 게이지 감소 속도 -30% |
| 4 | 엘리트 처치 시 핏 게이지 +50% 즉시 충전 |
| 5 | **핏 오버플로우**: MAX 초과 충전 → 라운드 클리어 골드 +1%/초과 |
| 6 | BP 라운드 간 아군 HP 회복 +10% (사망률 -10%) |
| 7 | 엘리트 출현 확률 2배 + 엘리트 드롭 등급 +1 |
| 8 | **핏 게이지 연쇄**: 적 처치 시 인접 적에게 "출혈" 부여 |
| 9 | BP 보스전 시작 시 핏 게이지 50% 사전 충전 |
| 10 | **광전사의 전장**: 핏 게이지 MAX 시 ATK +25% / DEF -15% |
| 11 | BP 보스 처치 시 전설 드롭 확률 +15% |
| 12 | **🏆 피의 군주**: 핏 게이지 영구 MAX + 효과 2배 + 보스 확정 전설 |

### 4.11 G. 화물 통제 (Cargo Control) — base 1,000G

**해금**: Cargo Lv1 첫 클리어. 열차 칸 시스템 변경에 집중.

| 단계 | 효과 |
|---|---|
| 1 | 열차 칸 HP +25% |
| 2 | **보급 크레이트 자석**: 아군 가까운 칸에 우선 생성 |
| 3 | 역 정차 시 수리량 2배 |
| 4 | **6번째 칸 해금**: 무기고 (ATK +10%, 파괴 시 -15%) |
| 5 | 침입자 사전 경고 (다음 라운드 침입 위치 표시) |
| 6 | **폭풍 차폐**: 폭풍 데미지 -50% + 폭풍 중 골드 +30% |
| 7 | 열차 칸 1회 자동 수리 (HP 0 → 30%, 전투당 1회) |
| 8 | **화물 증축**: 화물 보너스 HP × 2 적용 |
| 9 | 역 정차 시 NPC 상인 등장 (랜덤 매물, 할인) |
| 10 | **7번째 칸 해금**: 통신실 (보스 약점 노출, 서브 파견 -20%) |
| 11 | 침입자 자동 처치 (칸 HP 손실 없음) |
| 12 | **🏆 황금 열차**: 보상 2배 + 칸 파괴 불가 + 추가 정차역 2개 |

### 4.12 H. 어둠 통제 (Dark Control) — base 1,200G

**해금**: Blackout Lv1 첫 클리어. 저주 트레이드오프 심화에 집중.

| 단계 | 효과 |
|---|---|
| 1 | 저주 레벨 상승 속도 -25% |
| 2 | **저주 시야**: 저주 Lv3+ 시 인접 방 타입 1개 미리보기 |
| 3 | 비밀방 확률 +5% |
| 4 | **저주 흡수**: 저주 Lv1당 ATK +3% (양날의 검) |
| 5 | 함정방 데미지 -40% + 적에게 반사 20% |
| 6 | **안전한 후퇴**: 방 진입 전 취소 가능 (1층당 2회) |
| 7 | 저주 장비 드롭 시 음성 효과 -30% |
| 8 | **저주 정화의 샘**: 3층마다 정화방 확정 등장 (음성 특성 1개 제거) |
| 9 | 보스 약점 단서 발견 확률 2배 |
| 10 | **이중 탐색**: 같은 층 2갈래 동시 탐색 (방문 방 +50%) |
| 11 | 저주 Lv7+ 시 적 동족상잔 (20%) |
| 12 | **🏆 그림자 군주**: 저주 → 양성 전환 (저주Lv10=양성 2개 영구) + 비밀방 3배 |

### 4.13 피니쉬 명소 요약

| 카테고리 | 12단계 | 효과 |
|---|---|---|
| A 운영 | 총사령관 | 파견 중 교체 + 서브 10슬롯 |
| B 인프라 | 차원 창고 | 보관함 60칸 + 보안 무제한 |
| C 휴식 | 불사의 길드 | 자동 생존 2회 + 스테미너 즉시 풀 |
| D 자동화 | 완전 자동화 | 모든 마을 활동 자동 |
| E 정보 | 전능의 시야 | 모든 정보 공개 + 소문 6슬롯 |
| F 핏 | 피의 군주 | 핏 게이지 영구 MAX + 보스 확정 전설 |
| G 화물 | 황금 열차 | 보상 2배 + 칸 파괴 불가 |
| H 어둠 | 그림자 군주 | 저주→양성 전환 + 비밀방 3배 |

### 4.14 비용 시뮬

| 카테고리 | base | 1-12 누적 |
|---|---|---|
| A 운영 | 300 | ~77,000G |
| B 인프라 | 400 | ~103,000G |
| C 휴식 | 250 | ~64,000G |
| D 자동화 | 500 | ~129,000G |
| E 정보 | 600 | ~154,000G |
| F 핏 | 800 | ~206,000G |
| G 화물 | 1,000 | ~257,000G |
| H 어둠 | 1,200 | ~309,000G |
| **총합** | | **~1,299,000G** |

12시간 내 길드회관에 쓸 수 있는 골드 ≈ 200,000-300,000G

→ 2-3개 카테고리 풀업 or 모든 카테고리 7-8단계. 한 카테고리 밀면 다른 데 못 함 (트레이드오프).

### 4.15 gameState

```javascript
guildHall: {
  operations: 0,      // A (0-12)
  infrastructure: 0,  // B
  recovery: 0,        // C
  automation: 0,      // D
  intel: 0,           // E
  pit_control: 0,     // F (BP 첫 클리어 후 해금)
  cargo_control: 0,   // G (Cargo 첫 클리어 후 해금)
  dark_control: 0     // H (BO 첫 클리어 후 해금)
}
guildReputation: 0  // 0-100
```

---

## 5. RunSimulator v2 (서브 파견)

### 5.1 파티 파워 (Power Rating)

```javascript
function calcPartyPower(party) {
  let totalPower = 0;

  for (const merc of party) {
    const stats = merc.getStats();  // 장비/훈련 반영 최종 스탯

    // 개인 파워 = 공격력 × 생존력의 기하평균
    const offense = stats.atk * (1 + stats.critRate * stats.critDmg);
    const defense = stats.hp * (1 + stats.def * 0.02);
    let power = Math.sqrt(offense * defense);

    // 레벨 보정
    power *= (1 + (merc.level - 1) * 0.05);

    // 희귀도 보정
    const rarityBonus = {
      common: 1.0, uncommon: 1.05, rare: 1.1,
      epic: 1.2, legendary: 1.35
    };
    power *= rarityBonus[merc.rarity];

    // 피로도 페널티
    if (merc.stamina < 30) power *= 0.7;
    else if (merc.stamina < 50) power *= 0.85;
    else if (merc.stamina < 80) power *= 0.95;

    totalPower += power;
  }

  return totalPower;
}
```

### 5.2 구역 권장 파워

```javascript
function getZoneRecommendedPower(zone, zoneLevel) {
  const basePower = {
    bloodpit: 200,
    cargo: 300,
    blackout: 450
  };

  return basePower[zone]
    * (1 + (zoneLevel - 1) * 0.25)
    * (1 + (zoneLevel - 1) * 0.03);

  // BP:  Lv1=200, Lv3=320, Lv5=480, Lv8=760, Lv10=1000
  // Cargo: Lv1=300, Lv5=720, Lv10=1500
  // BO:  Lv1=450, Lv5=1080, Lv10=2250
}
```

### 5.3 파워 비율 해석

```
powerRatio = partyPower / recommendedPower

0.3 이하 → 무모 (거의 확정 실패, 높은 사망)
0.5      → 위험 (40% 성공, 중간 사망)
0.7      → 도전 (60% 성공)
1.0      → 적정 (80% 성공)
1.3      → 여유 (95% 성공, 사망 거의 없음)
1.5+     → 안전 (99% 성공, 사망 0%, 보상 약간↓)
```

### 5.4 성공률 공식

```javascript
function calcSuccessRate(powerRatio, party, zone, zoneLevel, gameState) {
  // 시그모이드 커브
  let baseRate = 1 / (1 + Math.exp(-6 * (powerRatio - 0.7)));
  // ratio 0.3→8%, 0.5→23%, 0.7→50%, 1.0→86%, 1.3→97%

  // 시너지 보너스 (0 ~ +0.15)
  baseRate += calcSynergyBonus(party);

  // 친밀도 보너스 (0 ~ +0.05)
  baseRate += calcAvgBondBonus(party) * 0.05;

  // 친화도 보너스 (0 ~ +0.10)
  baseRate += getPartyAvgAffinity(party, zone) * 0.02;

  // 구역업 보너스
  baseRate += getZoneControlBonus(gameState, zone, 'successRate');

  // 음성 특성 페널티 (0 ~ -0.15)
  baseRate += calcNegativeTraitPenalty(party);

  // 피로도 페널티
  const avgStamina = party.reduce((s, m) => s + m.stamina, 0) / party.length;
  if (avgStamina < 30) baseRate -= 0.15;
  else if (avgStamina < 50) baseRate -= 0.08;

  return Math.max(0.05, Math.min(0.99, baseRate));
}
```

### 5.5 라운드 진행 (부분 성공)

```javascript
function calcRoundsCompleted(successRate, maxRounds) {
  if (Math.random() < successRate) {
    return maxRounds;  // 전 라운드 클리어
  }

  // 실패: 부분 진행
  const progressRatio = successRate * (0.6 + Math.random() * 0.3);
  return Math.max(1, Math.floor(maxRounds * progressRatio));
}

// maxRounds = min(10, 4 + floor((zoneLevel-1) * 1.5))
// Lv1: 4, Lv3: 7, Lv5: 10
```

→ 실패해도 3/7까지 갔으면 3/7만큼 보상.

### 5.6 사망률

```javascript
function calcDeathChance(merc, powerRatio, zone, roundsCompleted, maxRounds, gameState) {
  // 기본 사망률
  let deathRate;
  if (powerRatio < 0.5) deathRate = 0.30;
  else if (powerRatio < 0.7) deathRate = 0.15;
  else if (powerRatio < 1.0) deathRate = 0.08;
  else if (powerRatio < 1.3) deathRate = 0.03;
  else deathRate = 0.01;

  // 실패 시 사망률 증가
  if (roundsCompleted < maxRounds) deathRate *= 1.5;

  // 클래스 보정
  const classDeathMod = {
    warrior: 1.3, rogue: 1.1, mage: 0.7,
    archer: 0.7, priest: 0.8, alchemist: 0.9
  };
  deathRate *= classDeathMod[merc.classKey];

  // 피로도
  if (merc.stamina < 30) deathRate *= 2.0;
  else if (merc.stamina < 50) deathRate *= 1.3;

  // 음성 특성
  if (merc.hasTrait('weak_body')) deathRate *= 1.3;
  if (merc.hasTrait('coward')) deathRate *= 1.2;

  // 양성 특성
  if (merc.hasTrait('veteran')) deathRate *= 0.7;

  // 구역업 사망률 감소
  deathRate *= (1 - getZoneControlBonus(gameState, zone, 'deathReduction'));

  return Math.min(0.50, Math.max(0.005, deathRate));
}
```

### 5.7 보상 산출

```javascript
function calcRewards(zone, zoneLevel, roundsCompleted, maxRounds, party, gameState) {
  const progress = roundsCompleted / maxRounds;
  const success = roundsCompleted === maxRounds;

  // === 골드 ===
  const baseGold = { bloodpit: 60, cargo: 80, blackout: 100 };
  let gold = baseGold[zone];
  for (let r = 1; r <= roundsCompleted; r++) {
    gold += 15 + r * 10;
  }
  if (success) gold += 50 + zoneLevel * 20;
  const enemyGoldPerRound = 12 + zoneLevel * 4;
  gold += roundsCompleted * enemyGoldPerRound;
  if (zone === 'cargo' && success) gold += 50 + 100 * progress;
  if (zone === 'blackout') gold += roundsCompleted * 5;
  gold *= (1 + getZoneControlBonus(gameState, zone, 'goldBonus'));

  // === 길드 XP ===
  let guildXp = 0;
  for (let r = 1; r <= roundsCompleted; r++) {
    guildXp += 10 + r * 5;
  }
  if (success) guildXp += 30;
  guildXp *= (1 + getZoneControlBonus(gameState, zone, 'xpBonus'));

  // === 용병 XP ===
  const mercXp = Math.floor(guildXp * 0.5 + roundsCompleted * 5 + (success ? 15 : 0));

  // === 친화도 XP ===
  const affinityXp = Math.floor(10 + roundsCompleted * 3 + (success ? 10 : 0));

  // === 아이템 드롭 ===
  const loot = generateLoot(zone, zoneLevel, roundsCompleted, maxRounds, gameState);

  // === 피로도 소모 ===
  const staminaCost = {
    bloodpit: 12 + roundsCompleted * 1,
    cargo: 15 + roundsCompleted * 1.5,
    blackout: 20 + roundsCompleted * 2
  };

  return {
    success, roundsCompleted, maxRounds, progress,
    gold: Math.floor(gold),
    guildXp: Math.floor(guildXp),
    mercXp: Math.floor(mercXp),
    affinityXp: Math.floor(affinityXp),
    loot,
    staminaCost: Math.floor(staminaCost[zone])
  };
}
```

### 5.8 파견 시간 공식

```javascript
function calcExpeditionTime(zone, zoneLevel, party, gameState) {
  const baseMinutes = { bloodpit: 2, cargo: 5, blackout: 10 };
  let time = baseMinutes[zone];

  // 구역 레벨 (깊을수록 오래)
  time *= (1 + zoneLevel * 0.12);

  // 파티 전투력 (강하면 빨리)
  const powerRatio = calcPartyPower(party) / getZoneRecommendedPower(zone, zoneLevel);
  const powerCoef = Math.max(0.6, Math.min(1.4, 1.5 - powerRatio * 0.5));
  time *= powerCoef;

  // 친화도 (Lv5: -20%)
  const avgAffinity = getPartyAvgAffinity(party, zone);
  time *= (1 - avgAffinity * 0.04);

  // 피로도
  const avgStamina = party.reduce((s, m) => s + m.stamina, 0) / party.length;
  if (avgStamina < 30) time *= 1.4;
  else if (avgStamina < 50) time *= 1.15;

  // 길드회관 F/G/H 구역별 파견 시간 감소
  const zoneTimeReduction = getZoneControlBonus(gameState, zone, 'timeReduction');
  time *= (1 - zoneTimeReduction);

  return Math.max(0.5, time);  // 최소 30초
}
```

### 5.9 파견 시간 예시

| 상황 | BP Lv1 | BP Lv5 | Cargo Lv3 | BO Lv5 |
|---|---|---|---|---|
| 베이스 | 2분 | 2분 | 5분 | 10분 |
| ×구역레벨 | 2.2분 | 3.2분 | 6.8분 | 16분 |
| ×파워1.0 | 2.2분 | 3.2분 | 6.8분 | 16분 |
| ×파워1.5 | 1.5분 | 2.2분 | 4.8분 | 11분 |
| ×친화도Lv5 | 1.2분 | 1.8분 | 3.8분 | 9분 |
| +구역업 풀 (-45%) | **0.7분(42초)** | **1.0분** | **2.1분** | **5분** |

### 5.10 파견 중 이벤트

25% 확률로 진행 중 알림 팝업. 선택에 따라 결과 보정.

**Blood Pit 이벤트 풀:**
- `pit_ambush`: 매복 → 정면 돌파 (+골드, +사망↑) / 우회 (+시간, 안전)
- `pit_treasure`: 숨겨진 보물 → 즉시 회수 (+아이템) / 무시 (+XP)
- `pit_elite_challenge`: 엘리트 도발 → 도전 (전설소재/사망↑) / 무시

**Cargo 이벤트 풀:**
- `cargo_storm`: 폭풍 진입 → 화물칸/무기고/의무실 보강 선택
- `cargo_stowaway`: 밀항자 → 추방/고용(50G)/심문(+소문)

**Blackout 이벤트 풀:**
- `bo_curse_altar`: 저주 제단 → 기도(저주+2, 보상2배) / 파괴(저주-1) / 무시
- `bo_ghost`: 과거 용병 영혼 → 위로(ATK+10%) / 정보(보스약점) / 무시(저주+1)

### 5.11 친밀도 누적

```javascript
function updateBonds(party, success) {
  for (let i = 0; i < party.length; i++) {
    for (let j = i + 1; j < party.length; j++) {
      const key = getBondKey(party[i].id, party[j].id);
      const gain = success ? 3 : 1;
      gameState.bonds[key] = (gameState.bonds[key] || 0) + gain;
    }
  }
}
// Bond 50: 같이 출전 시 +5% 스탯
// Bond 100: 전용 페어 스킬 또는 사망 시 광폭화
```

### 5.12 결과 로그

파견 완료 시 텍스트 로그 생성. 라운드별 진행, 이벤트, 사망, 보상 요약.

### 5.13 gameState 추가

```javascript
activeExpeditions: [
  {
    id: 'exp_001',
    zone: 'bloodpit',
    zoneLevel: 3,
    partyIds: ['merc_01', 'merc_05', 'merc_08'],
    startTime: Date.now(),
    estimatedDuration: 180000,  // ms
    midEvent: null,
    autoRedeploy: true
  }
],
pendingResults: [],
bonds: {}  // { 'merc01_merc05': 45, ... }
```

---

## 6. 피로도 시스템 (Stamina)

### 6.1 핵심 룰

- 모든 용병: stamina 0-100
- 시작 100, 파견/전투 후 감소
- 마을 시설에서 회복
- 같은 용병만 굴리면 자연히 피로도 누적 → **로스터 회전 강제**

### 6.2 피로도 차감

| 활동 | 차감 |
|---|---|
| 메인 직접 전투 (Blood Pit) | -25 |
| 메인 직접 전투 (Cargo) | -30 |
| 메인 직접 전투 (Blackout) | -40 |
| 서브 파견 (1런) | -12 ~ -20 (구역별) |
| 서브 파견 사망 위기 | -25 (생존 시 추가) |
| 메인에서 부상/HP낮음 종료 | +10 추가 차감 |

### 6.3 피로도 효과

| Stamina | 효과 |
|---|---|
| 80-100 | 컨디션 최상 (+5% ATK) |
| 50-79 | 정상 |
| 30-49 | 피곤 (-10% ATK, -10% 명중) |
| 10-29 | 탈진 (-25% ATK, 파견 시간 +25%, 사망률 +50%) |
| 0-9 | 강제 휴식 (파견 불가) |

### 6.4 피로도 회복

| 방법 | 회복량 | 비용 |
|---|---|---|
| 마을 자동 (기본) | +1/분 | 무료 |
| 마을 자동 (길드회관 C 풀) | +5/분 | 투자 필요 |
| 술집 휴식 | +30 즉시 | 20G |
| 신전 명상 | +50 즉시 | 50G |
| 술집 풀 휴식 | +100 즉시 | 100G |

---

## 7. 장비 시스템 + 자동화

### 7.1 장비 슬롯 (4슬롯)

| 슬롯 | 주 효과 |
|---|---|
| weapon | ATK, CRIT |
| armor | DEF, HP |
| trinket | 특수 (CRIT/SPD/특성 강화) |
| consumable (1회용) | 전투 시작 시 자동 소비 (포션 등) |

### 7.2 자동 장착 모드

- 클래스 추천 (warrior=HP/DEF, mage=ATK/CRIT...)
- 최고 ATK 우선
- 최고 HP+DEF 우선
- 균형
- 수동

발동 시점:
- 새 장비 획득 즉시 (옵션)
- 파견 출발 직전 (체크박스)
- 수동 "로스터 최적화" 버튼

### 7.3 장비 잠금

자물쇠 표시 → 자동 처분/교체에서 제외.

### 7.4 자동 처분

규칙 (체크박스):
- common 자동 판매
- uncommon 자동 판매
- 잠금 안된 중복 자동 판매
- 보관함 80% 차면 common부터

처분 방식: 즉시 판매 (70% 가치) 또는 위탁 등록

### 7.5 일괄 작업 패널

```
[전체 최적화] [잡템 일괄판매] [전원 휴식] [전원 치유]
[자동장착 ON/OFF] [자동판매 ON/OFF]
```

### 7.6 비교 툴팁

장비 호버 시 현재 장착과 스탯 +/- 색상 비교.

---

## 8. 특성 제거/변환 시스템

### 8.1 음성 특성 제거

**신전 — 정화 의식**
- 음성 특성 1개 제거
- 비용: 길드Lv × 500G + 희귀도 배율
- 쿨다운: 용병당 5런

**고급 모집소 — 특성 재추첨**
- 음성 전체 재추첨 (다른 음성으로 바뀔 수도)
- 비용: 800G
- 확률: 30% 모두 제거 / 50% 1개 남음 / 20% 그대로

**Blackout 비밀방 — 정화의 샘**
- 무작위 발견, 음성 1개 무료 제거

### 8.2 양성 특성 추가

**훈련소 — 특훈**
- 양성 1개 부여 (무작위, 클래스 가중치)
- 비용: 1500G + 훈련 포인트 3
- 슬롯 제한: 최대 3개 양성

**전설 특성 부여**
- legendary 등급 + 80레벨 친화도 → 자동 부여
- 또는 보스 처치 후 토큰으로 부여

### 8.3 특성 슬롯

- 양성: 최대 3슬롯
- 음성: 최대 2슬롯
- 전설: 1슬롯 (legendary만)

---

## 9. NPC 상인 시스템

### 9.1 상인 목록 (8명)

| NPC | 컨셉 | 전문 매물 | 특수 |
|---|---|---|---|
| 크로그 (무기상) | 거친 대장장이 | weapon 전체 | 호감 Lv4: 전설무기 |
| 헤르타 (방어구상) | 노련한 갑옷장이 | armor | 호감 Lv4: 전설갑옷 |
| 미라 (약초상) | 비밀스러운 연금술사 | 소비 (포션/폭탄) | 음성 정화 -50% 할인 |
| 골든 (경매사) | 경매장 사회자 | 위탁/매물 | 위탁 성공률 향상 |
| 스피드카 (정보상) | 회색 망토의 정보꾼 | 구역 정보, 단서 | 보스 약점 정보 |
| 아이언 (대장장이) | 강화 전문가 | 강화/제작 할인 | 전설 제작 |
| 그림자 (암시장) | 위험한 거래상 | 저주 장비, 음성 거래 | 비밀 매물 |
| 사제 (신전) | 정화의 사제 | 특성 정화, 피로도 회복 | 부활, 축복 |

### 9.2 호감도 5티어

| 티어 | 거래수 | 할인 | 매물 |
|---|---|---|---|
| 낯선 | 0 | 0% | 기본 (common) |
| 친근한 | 5 | 5% | + uncommon |
| 신뢰하는 | 15 | 10% | + rare |
| 동맹 | 30 | 15% | + epic |
| 절친 | 60 | 20% | + legendary (확률) |

### 9.3 매물 로테이션

- 각 NPC 개별 인벤토리
- 갱신: 마을 시간 기반 (길드회관 E3 "-30%" 적용)
- 호감도 ↑ = 매물 슬롯 증가 (3 → 8)

---

## 10. 마을 이벤트 확장

### 10.1 이벤트 카테고리

| 카테고리 | 비중 | 예시 |
|---|---|---|
| NPC 의뢰 | 25% | 무기상이 특수 소재 의뢰 |
| 길드 내부 | 15% | 용병 간 갈등, 우정, 결투 |
| 외부 방문 | 15% | 떠돌이 상인, 모험가 길드 사절 |
| 정치/세력 | 10% | 왕실 칙령, 영주 의뢰, 도적단 협박 |
| 자연 재해 | 10% | 역병, 폭풍, 시장 변동 |
| 발견/사건 | 15% | 비밀 통로 발견, 유적 단서 |
| 길드원 사건 | 10% | 용병의 과거, 비밀 노출 |

### 10.2 발생 트리거

- 시간 경과 기반: 마을 실시간 30분마다 1회
- 조건 트리거: NPC 호감도, 특정 아이템 보유, 음성특성 등

### 10.3 이벤트 구조

```
이벤트:
  - 이름, 일러스트 placeholder
  - 본문 (4-8줄)
  - 선택지 2-4개
  - 선택지별 결과:
    * 즉시 효과 (골드, 아이템, 특성 변경)
    * 지연 효과 (NPC 호감도 변동)
    * 분기 이벤트 (후속 이벤트)
```

### 10.4 목표: 40개 이상

기존 15개 → 40개+ (카테고리별 5-7개). **상세 이벤트 내용은 TODO.**

---

## 11. 구역 특수 업그레이드

기존 친화도 트리와 별개. **길드회관 F/G/H로 통합됨.**

- 친화도 트리: 해당 구역 **전투 시** 적용되는 전투력 버프
- 길드회관 F/G/H: **구역 메커니즘 자체** 변경 (핏 게이지, 열차 칸, 저주)

친화도 트리 24-30 노드 정의는 **TODO** (다음 작업 항목).

---

## 12. 12시간 페이싱 가이드 (초안)

| 시간 | 메인 런 | 서브 파견 | 길드 Lv | 핵심 이벤트 |
|---|---|---|---|---|
| 0:00-1:00 | 3-4 | 3-5 | 1→2 | 튜토리얼, BP Lv1-2 |
| 1:00-2:30 | 6-8 | 15-20 | 2→3 | 제작소, BP Lv3-5 |
| 2:30-4:00 | 10-12 | 30-40 | 3→4 | 경매장, Cargo 첫 도전 |
| 4:00-6:00 | 14-18 | 50-70 | 4→5 | 훈련소, BP Lv8, Cargo Lv3 |
| 6:00-8:00 | 20-25 | 80-100 | 5→6 | 신전, Blackout 첫 도전, BP 보스 |
| 8:00-10:00 | 28-32 | 110-140 | 6→7 | 정보소, 3구역 풀가동, Cargo 보스 |
| 10:00-11:30 | 35-40 | 160-200 | 7→8 | 고급모집, Blackout 보스 |
| 11:30-12:00 | 42-50 | 220+ | 8 | 금고, 엔드게임 |

**정밀 시뮬은 모든 시스템 확정 후 수행 예정.**

---

## 13. 남은 작업 목록 (TODO)

### P0 — 핵심 (게임 동작에 필수)

| # | 항목 | 상태 | 비고 |
|---|---|---|---|
| 1 | 길드 회관 시스템 | ✅ 확정 | 섹션 4 |
| 2 | RunSimulator v2 | ✅ 확정 | 섹션 5 |
| 3 | 친화도 트리 24-30 노드 정의 | ⬜ | 구역별 8-10 노드, 분기형 |
| 4 | 친밀도/Bond 시스템 | ⬜ | 용병 간 관계, 시너지 |
| 5 | 카드 23장 정의 (이름/효과/조건) | ⬜ | 메인 전투 핵심 |
| 6 | 시너지 14종 정의 | ⬜ | 2인 7종, 3인 5종, 5인 2종 |

### P1 — 중요 (게임 풍성함)

| # | 항목 | 상태 | 비고 |
|---|---|---|---|
| 7 | 술집 + 피로도 회복 시설 | ⬜ | 다키스트 던전 town activity |
| 8 | 마을 이벤트 40개 + 소문 시스템 | ⬜ | 카테고리별 5-7개 |
| 9 | NPC 8명 + 호감도 + 매물 구체화 | ⬜ | 각 NPC 인벤/대화/의뢰 |
| 10 | 길드 평판 시스템 상세 | ⬜ | 0-100, 획득원/마일스톤 |
| 11 | 제작 레시피 12개 + 소재 드롭표 | ⬜ | 6종 소재, 구역별 드롭 |
| 12 | 시간 시스템 (실시간 처리) | ⬜ | Date.now() 기반, 오프라인 처리 |

### P2 — 깊이 (게임 깊이/재미)

| # | 항목 | 상태 | 비고 |
|---|---|---|---|
| 13 | 보스 페이즈 패턴 (4종 × 3페이즈) | ⬜ | 특수 능력/쿨다운 |
| 14 | 클래스 스킬 수치 | ⬜ | 6클래스 × 각 스킬 |
| 15 | 사망/은퇴 시스템 | 🔒 보류 | 나중에 |
| 16 | 소문 시스템 상세 | ⬜ | NPC/술집 연동 |

### P3 — 후순위 (1회차 완성 후)

| # | 항목 | 상태 | 비고 |
|---|---|---|---|
| 17 | 영구 진행 / 뉴게임+ | ⬜ | 12시간 클리어 후 |
| 18 | 적 호위병 Lv4-10 구성 | ⬜ | 수치 보충 |
| 19 | 음성 특성 상세 (발동 타이밍 등) | ⬜ | 단명 10% 즉사 등 |

### 통합 — 전체 수치 재산출

| # | 항목 | 상태 | 비고 |
|---|---|---|---|
| 20 | 클래스 베이스 스탯 재산출 | ⬜ | 12시간 페이싱 기반 |
| 21 | 희귀도 풀 재산출 | ⬜ | 길드 Lv 1-8 |
| 22 | 골드/XP 커브 재산출 | ⬜ | 길드회관 비용 감안 |
| 23 | 12시간 페이싱 정밀 시뮬 | ⬜ | 모든 시스템 반영 |

---

## 부록: 길드 평판 (간이 정의)

```
길드 평판 (0-100)

획득원:
  - 메인 도전 성공: +0.5
  - 보스 처치: +5
  - NPC 호감도 누적: +0.2 / 거래
  - 마을 이벤트 (선택지에 따라): -3 ~ +5
  - 길드회관 E7, E18: 가속

마일스톤:
  15: "이름 있는 길드"
  35: "신뢰받는 길드"
  60: "전설의 길드"
  100: "왕실 인정"

용도:
  - 길드회관 게이트 (단계 7-9: 평판 15, 단계 10-12: 평판 35)
  - NPC 특수 의뢰 해금
  - 마을 이벤트 분기
```

---

## 부록: gameState 전체 추가 필드

```javascript
// 길드 회관
guildHall: {
  operations: 0,
  infrastructure: 0,
  recovery: 0,
  automation: 0,
  intel: 0,
  pit_control: 0,
  cargo_control: 0,
  dark_control: 0
},

// 길드 평판
guildReputation: 0,

// 파견
activeExpeditions: [],
pendingResults: [],

// 용병 피로도 (mercId → stamina)
stamina: {},

// 친밀도 (mercId1_mercId2 → bond value)
bonds: {},

// NPC 호감도 (npcId → favor value)
npcFavor: {},

// NPC 매물 (npcId → inventory array)
npcInventory: {},

// 소문 슬롯
rumors: [],

// 자동화 설정
autoEquipMode: 'manual',  // 'manual' | 'class' | 'atk' | 'hpdef' | 'balanced'
autoSellRules: {
  sellCommon: false,
  sellUncommon: false,
  sellDuplicates: false,
  sellOnOverflow: false
},

// 장비 잠금
lockedItems: []
```

---

> **다음 작업**: TODO #3 친화도 트리 → #4 친밀도/Bond → #5 카드 23장 → #6 시너지 14종 → ... 순서대로 진행.
