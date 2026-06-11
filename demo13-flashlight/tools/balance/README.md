# 밸런스 에디터 — 단일 컨트롤 (tools/balance/)

> **게임의 모든 자원·밸류를 한 곳에서.** 엑셀/텍스트로 아래 CSV 숫자만 만지면 전 도메인 밸런스 제어.
> 워크플로: **CSV 편집 → `ApplyBalance.ps1`(예정) → Resources/SO 반영 → 런타임 로드.** (시스템 구현 후 자동 적용; 그 전엔 설계 단일 진실원.)

## 도메인별 파일

| 파일 | 제어 대상 | 핵심 컬럼 |
|------|----------|----------|
| `hideout_modules.csv` | **시설 레벨업 비용·재료** | module·level·scrap_cost·materials |
| `barter.csv` | **물물교환 레시피** | npc·in1/in2·out·unlock |
| `quest_rewards.csv` | 의뢰 보상 등급(스크랩) | grade·scrap_min/max |
| `reputation.csv` | 평판 적립량(행동별) | action·rep |
| `reputation_tiers.csv` | 평판 구간·해금·자릿세할인 | tier·min/max·unlock·rent_discount |
| `trust.csv` | 신뢰도 적립·게이트·일일상한 | source·trust |
| `dispatch.csv` | 파견 유형(시간·성공률·보상) | type·time·base_success |
| `dispatch_modifiers.csv` | 파견 성공률 보정값 | factor·value |
| `upkeep.csv` | 자릿세·연료·상점 할인·매입가 배수 | key·value |

## 외부 연동(이미 존재 — 같은 밸런스 컨트롤군)

| 파일 | 제어 대상 |
|------|----------|
| `../items.csv` + `../ItemPrices.ps1`(Get-ItemPricesScaled) | **아이템 가격**(sell/buy·루디 5만·잭팟 20만~500만) → `../UpdateItemPrices.ps1` |
| `../region_loot.csv` | **지역×티어 루트**(weight·roll_count·min/max) → `../GenerateRegionItems.ps1` |

→ 이 폴더 + 위 둘 = **전체 경제 단일 진실원.**

## 재료 표기 규칙
`materials` / `in*` 컬럼 = `itemId:qty` (복수는 `;`). 예: `tool_part:3;circuit_board:1`. `junk_*`·`val_*` = 와일드카드(카테고리/접두 묶음).

## 적용 상태
- ✅ **아이템 가격**: ItemPrices.ps1 + UpdateItemPrices.ps1로 SO 적용 완료(2026-06-10).
- ✅ **루트**: region_loot.csv 편집 가능(생성기 존재).
- 🟡 **나머지(모듈·바터·의뢰·평판·신뢰·파견·자릿세)**: CSV=설계 진실원. **런타임 로더 + `ApplyBalance.ps1`은 backlog-impl.csv 참조**(해당 시스템 구현 시 연결).

## 변경 로그
| 날짜 | 내용 |
|---|---|
| 2026-06-10 | 밸런스 폴더 신설. 9 CSV(모듈비용·바터·의뢰보상·평판·신뢰·파견·자릿세) + 외부 연동(가격·루트) 인덱스. 단일 밸런스 컨트롤. GUI 에디터 창은 backlog(BalanceEditor). |
