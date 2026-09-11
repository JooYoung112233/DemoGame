# BRB 공통 참조

> **스코프**: `demo13-flashlight` 워크스페이스 전용. `docs/MASTER.md` 없으면 스킬 실행 금지.

## 프로젝트

- **제목**: 다녀올게 (BRB) — Unity 6 탑다운 2D 생존 루팅
- **루프**: 안전가옥 → 지역 선택 → 15분 레이드 → 탈출 → 정산 → 복귀
- **현재**: Stage 2 (안전가옥 맵) — 시스템 골격 연결됨, **맵 콘텐츠·전투 체감** 부족
- **인덱스**: `docs/MASTER.md` · 코드 가이드: `CLAUDE.md`

## 핵심 규칙 (필수)

1. Container 패턴 금지
2. `Start()`에서 에디터 값 덮어쓰기 금지 — 이벤트로만 변경
3. UI는 코드 생성(uGUI), 1920×1080
4. 기획 결정 즉시 `docs/*.md` 기록
5. 스탯은 `StatDB.asset` 중앙 관리

## 문서 → 주제 매핑

| 주제 키워드 | 문서 |
|------------|------|
| 전투 | `combat.md` |
| 인벤·아이템·제작 | `inventory.md`, `items.md`, `crafting.md`, `region-loot.md` |
| 경제·생존·의료 | `economy.md`, `survival.md`, `medical.md` |
| 안전가옥 | `safehouse.md`, `safehouse-asset-list.md` |
| 월드·레이드 | `world-map.md`, `raid.md`, `post-raid-event.md`, `anomaly.md` |
| 레벨 | `level-scrapmarket.md`, `level-apartment.md`, `level-tower.md` |
| 내비 | `navigation.md` |
| NPC·퀘스트 | `npc-dialogue.md`, `quest.md`, `quests-region1.md` |
| 스토리 | `story.md`, `story-script.md` |
| 렌더링 | `rendering.md`, `destructible.md`, `topdown-art-spec.md` |
| 맵툴 | `map-tool.md`, `map-tool-guide.md` |
| 아키텍처·도구 | `architecture.md`, `tooling.md`, `tuning.md` |
| 기획 | `gdd-core.md`, `gdd-progression.md`, `gdd-demo.md` |

## Unity 메뉴 (`Tools/TopDown/`)

| 용도 | 메뉴 |
|------|------|
| Systems 씬 | `빌드/시스템 씬` |
| 안전가옥·인게임 씬 | `빌드/안전가옥 씬`, `빌드/인게임 씬` |
| 그레이박스 | `맵/안전가옥 그레이박스`, `맵/고철시장 그레이박스` |
| 프롭·맵툴 | `맵/프롭 카탈로그`, `빌드/맵 편집 씬` |
| 전투·데이터 | `전투/공격 에디터`, `데이터/스탯 DB` |
| 콘텐츠 | `콘텐츠/NPC 메이커`, `컨트롤 패널` |

빌드 순서: Player Rig → Systems 씬 → 게임플레이 씬 → 맵 콘텐츠

## 응답 형식 (추가 지시 없을 때)

- **요약** (1~2문장)
- **현재 상태**
- **핵심 파일** (3~5개)
- **미완 항목** (`dev-roadmap.md`)
- **다음 작업** (1~3개)

구현 요청이 있으면 바로 작업.
