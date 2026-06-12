# 에디터 도구 카탈로그 (Tools ▸ TopDown)

> 유니티 상단 **Tools ▸ TopDown** 메뉴의 자체 제작 도구 목록. (Spine 등 서드파티 CONTEXT 메뉴 제외.)
> "뭐가 뭔지" 빠르게 찾기용. 파일은 `Assets/Editor/`(일부 `Assets/Scripts/`).

## 🎛️ 데이터·밸런스 편집

| 메뉴 | 무엇 | 파일 |
|------|------|------|
| 밸런스 ▸ 밸런스 에디터 | **전 CSV 격자 편집**(items·quests·region_loot·balance/*) 유니티 내 | BalanceEditorWindow |
| 데이터 ▸ 스탯 DB | 유닛/플레이어 스탯(StatDB) 편집 | StatDBEditor |
| 전투 ▸ 공격 에디터 | 히트박스 타임라인·콤보 편집 | AttackDataEditorWindow |
| 콘텐츠 ▸ NPC 메이커 | NPCData(대화·상점·퀘스트) 생성·편집 | NPCMakerWindow |
| (루트) 컨트롤 패널 | 통합 컨트롤 패널(런타임 토글 등) | GameControlPanel |

## 🏗️ 씬 빌드 (절차적 생성)

| 메뉴 | 무엇 | 파일 |
|------|------|------|
| 빌드 ▸ 시스템 씬 | Systems.unity(매니저+UI+PlayerRig) 생성 | SystemsSceneBuilder |
| 빌드 ▸ 인게임 씬 / 안전가옥 씬 | 게임플레이 씬 생성 | GameSceneBuilder |
| 빌드 ▸ 맵 편집 씬 | 맵툴 씬 생성 | MapTool2DSceneBuilder |
| 빌드 ▸ 전투 샌드박스 씬 / 적 프리팹 | 전투 테스트용 | CombatSandboxBuilder |
| 빌드 ▸ ▶ 안전구역 일괄 빌드 | 안전구역 관련 일괄 빌드 | ContentBuildAll |
| 빌드 ▸ 이상현상 구역 배치 | 씬에 이상현상 존 배치 | AnomalyTools |
| 콘텐츠 ▸ 안전가옥 NPC 빌드 | 안전가옥 NPC 자동 배치 | SafehouseNpcBuilder |

## 🗺️ 맵 제작

| 메뉴 | 무엇 | 파일 |
|------|------|------|
| 맵 ▸ 프롭 카탈로그 | Prop2D 카탈로그 편집(타일·프롭) | Prop2DCatalogEditor |
| 맵 ▸ 프롭 ID 정규화 | 프롭 itemId 정리 | Prop2DCatalogEditor |
| 맵 ▸ 그레이박스 팔레트 생성 | 색박스+라벨 그레이박스 팔레트 | GreyboxPaletteBuilder |
| 맵 ▸ 데칼 브러시 | 데칼 스캐터 배치 | DecalScatterBrush |
| 맵 ▸ 조명 쿠키 생성 / 씬 조명 셋업 | Light2D 쿠키·씬 조명 | LightCookieGenerator·SceneLightingBuilder |
| 맵 ▸ 그레이박스(안전가옥/고철시장/1구역/은신처) | 레이아웃 그레이박스 빌더 | *GreyboxLayout 4종 |
| 맵 ▸ 이상현상 구역 생성 | 현상 존 생성 | DenseAnomalyZone |
| 맵 ▸ 낮밤 라이트 드라이버 부착 | 낮↔밤 ambient 부드러운 전환(WeatherData lerp) | DayNightLightDriver |

## ⚙️ 초기설정 (1회성 셋업)

| 메뉴 | 무엇 | 파일 |
|------|------|------|
| 초기설정 ▸ 글로벌 라이트 점검 / 1개만 남기기 | Light2D 글로벌 정리 | GlobalLightDoctor |
| 초기설정 ▸ Destructible·Ceiling 레이어 보장 | 레이어/정렬레이어 셋업 | GameLayers |
| 초기설정 ▸ 플레이어 머티리얼 생성 | 플레이어 머티리얼 | PlayerMaterialSetup |
| (Assets 우클릭) Color → Alpha | 스프라이트 배경 투명화 | ColorToAlpha |

## 정리 메모 (2026-06-10)

- ✅ **불일치 수정**: `Tools/TopDown/Map/...`(영문) → `맵/...`(한글 통일) — DenseAnomalyZone.
- 💡 권장(미적용): 그레이박스 4종 + 팔레트를 **맵 ▸ 그레이박스 ▸ {이름}** 하위로 묶으면 맵 메뉴가 덜 붐빔. (MenuItem 문자열만 변경, 기능 동일)
- 카테고리 = 밸런스·데이터·전투·콘텐츠·빌드·맵·초기설정 + 루트(컨트롤 패널).
