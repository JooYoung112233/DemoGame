# 1지역 스토리 NPC 4명

2026-09-11 사용자 “걔들 만들자”로 제작 승인. 각 NPC에는 **3초 대기 루프 1개**만 포함한다. 후속 “지금 만든 거 인게임에 일단 전체 적용해줘”에 따라 Unity MCP로 4명을 적용했다. 사용자 외형 최종 컨펌은 별도다.

| 기존 npcId | 모델 | 대기 모션 | 원화에서 반영한 특징 |
|---|---|---|---|
| `pawnshop` | `Models/Pawnshop.fbx` | `Idle_Observe` | 벗겨진 머리·회색 수염, 바랜 적갈색 셔츠, 앞치마. 서서 주변을 살핀다. |
| `veteran_scavenger` | `Models/VeteranScavenger.fbx` | `Idle_Crouch` | 머리띠·올리브 작업복·장갑·무릎 수선. 대본처럼 쪼그려 앉아 무릎에 손을 둔다. |
| `district_warden` | `Models/DistrictWarden.fbx` | `Idle_Ledger` | 올린 머리의 중년 여성, 갈색 겉옷·장부·열쇠. 장부를 안고 서 있다. |
| `wandering_merchant` | `Models/WanderingMerchant.fbx` | `Idle_Pack` | 챙 모자·수염·짧은 망토·배낭과 침낭·보급품. 어깨끈을 잡고 선다. |

원화: `demo13-flashlight/Assets/GPT/NPC/NPCALL4.png`, `NPCALL4_1.png`, `NPCALL7.png`, `NPC2_2.png`, `NPC3.png`, `NPC4.png` 및 `Assets/GPT/데모/전당포주인.png`. 통합 4인 원화의 역할별 실루엣을 우선하고, 픽셀 원화의 작은 주름/표정 변형은 생략했다. 본명·시설 해금·새 NPCData·게임 수치는 추가하지 않았다.

## 산출물과 검수

- `BlenderSource~/`: 편집 가능한 원본. 모델별 기존 `SimpleHero_Rig` 23본, `NPC_Body` 스킨 메시 1개, 재질 1개. 메시 안 `Part_*` 그룹으로 의상·머리·소품을 분리할 수 있다.
- `Models/`: FBX 4개. 각 대기 클립 하나, 30fps 1~91프레임. 91은 1과 같은 루프 경계다.
- `Textures/<모델>/`: 2048² BaseColor / Normal / Mask, UV 배치 SVG 및 검사 JSON. Mask R=금속성, A=매끄러움(1−거칠기); Normal은 미세한 천결만 표현하며 무광 큰 면을 유지한다.
- `NPCOverview.jpg`: 위 62° 탑다운 / 아래 형태 확인용 낮은 시점. `QuarterReview.jpg`, `Rear62Review.jpg`, `SideReview.jpg`: 실제 모델 여러 방향 확인.
- `Idle62.mp4`, `IdleFront.mp4`: 실제 클립의 62° 및 낮은 시점 영상. 미리보기는 원본 키를 2프레임 간격으로 샘플링해 15fps로 인코딩하므로 3초 주기 속도가 유지된다.
- `BuildValidation.json`: 전체 364프레임의 유한한 스킨 변형, 가중치 정규화, 발 본 행렬 고정, 시작/끝 정점 일치, 원본 23본 rest pose 보존.
- `RigAndFBXValidation.json`: 4개 FBX를 깨끗한 Blender 씬에 다시 임포트해 메시/뼈/클립/텍스처와 크기·루프 일치를 확인한다. UV 내부 겹침은 0이다. 텍스처 래스터 집계의 중복 픽셀은 공유 경계에서 생기며, 별도 삼각형 내부 면적 검사로 실제 겹침과 구분한다.
- `SourcePreservation.json`: 승인된 `CharacterProportionC/BlenderSource~/Player_C.blend` 해시 불변.

자체 수정: 첫 렌더의 머리카락-두피 관통을 실제 두상 단면에 맞춘 외피로 교정했다. 돌출 수염은 얼굴 UV 표현으로 바꾸고, 관리인의 손은 장부 앞/아래에, 상인의 손은 어깨끈에 맞췄다. 상인의 망토 앞단과 회수꾼의 앉은 높이도 보완했다. 눈을 보이게 하려고 머리를 돌리거나 카메라 기준을 바꾸지 않았다.

## Unity MCP 적용 — 2026-09-11

- `Assets/ChibiSurvivor/NPC/StoryNPC62`에 FBX/텍스처/재질/단일 상태 Animator/시각 프리팹 4세트를 생성했다. 기존 `Assets/Resources/NPC` 프리팹 4개 및 `Safehouse`의 기존 4명에 연결했다. NPCData·대화·퀘스트·상호작용 설정, 씬 NPC 위치와 충돌체를 보존했다.
- Generic, Loop Time, Root Motion 끔. C 높이 1.08을 추가 적용하지 않았다. 기존 씬 캡슐 스케일은 시각 자식에서 상쇄하고 발은 지면에 맞췄다. 월드 높이는 전당포 1.751m, 회수꾼 1.416m, 관리인 1.904m, 상인 1.865m다. 회수꾼의 낮은 높이는 앉은 포즈다.
- BRB/GameLit에 BaseColor(sRGB)와 Normal(Normal Map)을 연결했다. Normal strength 0.35, Smoothness 0.08, Metallic 0으로 무광 표면을 유지한다. **현재 GameLit은 Mask 텍스처 슬롯이 없어 Mask는 Linear로 임포트만 했고 연결하지 않았다.**
- 재질·스킨·실행 애니메이션 확인 자료는 `UnityIntegration/`. 초기 BakeMesh 출력에 FBX scale이 중복 적용되는 측정 오류를 발견해 bone world matrix × bindpose로 실제 정점을 계산하고 접지/마커 높이를 교정했다.
- 실행 검수에서 발견한 마을 5개 건물의 끊어진 지붕 참조를 현재 `Roof_Art02`로 복구했다. 전당포 실내 조명도 기존 내부 조명에 연결했다. 새 기능을 추가한 것이 아니라 기존 진입 시 지붕 숨김 동작의 참조를 복원했다.
- Unity 변경은 MCP로만 수행했으며 컴퓨터 화면 조작은 사용하지 않았다. 기존 전투 테스트는 종료된 상태였으며 검증용 플레이를 새로 시작했다.

## 재생성

Blender 4.5 background로 `tools/build_story_npcs.py` → `tools/validate_story_npcs.py` → `tools/render_story_npcs.py -- all`을 실행하고 Python/Pillow로 `tools/compose_story_npcs.py`를 실행한다. 빌더는 기존 프로젝트의 읽기 전용 UV 작성 함수 `demo13-flashlight/tools/character_uv_atlas.py`를 재사용한다. 모든 출력은 이 폴더 안이다.
