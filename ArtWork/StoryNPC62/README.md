# 1지역 스토리 NPC 4명

2026-09-11 사용자 “걔들 만들자”로 제작 승인. 각 NPC에는 **3초 대기 루프 1개**만 포함한다. 아래는 실제 Blender 모델/모션 산출물이며 사용자 외형 최종 컨펌 및 Unity 적용은 별도다.

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

## Unity MCP 적용 시

현재 Unity Assets/씬/플레이 모드 및 기존 NPC 동작은 변경하지 않았다. 다른 전투 작업을 보존하기 위해 Assets 밖에 제작했다.

1. Unity MCP로 임포트 가능한 시점에 FBX/텍스처를 NPC 아트 폴더로 가져온다. C 루트 스케일은 파일에 이미 포함되므로 높이 1.08을 다시 적용하지 않는다.
2. Animation Type은 기존 리그에 맞춰 Generic, 각 FBX의 유일한 대기 클립에 Loop Time을 설정한다. Root Motion은 사용하지 않는다. 동일 뼈 구조를 사용하지만 전투/이동 클립은 이번 NPC 파일에 포함하지 않는다.
3. 기존 게임 재질에 BaseColor/Normal/Mask를 명시적으로 연결한다. FBX의 기본 재질 변환이 최종 GameLit 셰이더 연결을 대신하지 않는다. BaseColor는 sRGB, Mask는 Linear, Normal은 Normal Map으로 임포트한다.
4. 기존 4개 NPCData의 `npcId`와 위 표를 대응해 시각 모델만 연결한다. 마을 배치/상호작용 충돌체/Animator 연결 및 인게임 조명 확인은 아직 수행하지 않았다.
5. 62° 실제 카메라에서 발 접지, 장부·어깨끈 접촉과 그림자를 최종 확인한다. 회수꾼은 낮은 앉은 실루엣이 정상이다. 상인은 후속 가구점 정착 NPC와 같은 인물이다.

## 재생성

Blender 4.5 background로 `tools/build_story_npcs.py` → `tools/validate_story_npcs.py` → `tools/render_story_npcs.py -- all`을 실행하고 Python/Pillow로 `tools/compose_story_npcs.py`를 실행한다. 빌더는 기존 프로젝트의 읽기 전용 UV 작성 함수 `demo13-flashlight/tools/character_uv_atlas.py`를 재사용한다. 모든 출력은 이 폴더 안이다.
