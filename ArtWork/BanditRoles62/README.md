# 밴딧 역할 비주얼 — 62° 탑다운

2026-09-11 사용자 결정: 탑다운 pitch 62°, 현재 밴딧을 근접형으로 두고 같은 리그의 원거리형 1종 추가. 코드에서 확인한 yaw는 0°다.

## 두 역할

- **근접:** 승인된 `ArtWork/CharacterProportionC/BlenderSource~/Bandit_C.blend`를 유지한다. 큰 청회색 후드·갈색 상의·팔 붕대가 표식이다. 이번 작업에서 해당 파일은 수정하지 않았다.
- **원거리:** `BlenderSource~/Bandit_Ranged_C.blend`, `Models/Bandit_Ranged_C.fbx`. 낮고 둥근 헬멧·올리브 상의·어두운 조끼·가슴 탄창 파우치·장갑. 권총과 돌격소총이 공용으로 쓸 본체 1종이다. 등 중앙은 큰 가방 없이 수납 공간을 남겼다.

참고 원화: `Assets/GPT/밴딧/ChatGPT Image 2026년 6월 11일 오후 02_57_47.png`, `ChatGPT Image 2026년 6월 11일 오후 06_33_07.png`. 군용 헬멧·올리브 상의·가슴 장구류를 참고했다. 실제 무기 종류나 AI를 신규 구현한 작업은 아니다.

## 호환성

기존 `SimpleHero_Rig`의 뼈 23개와 rest pose, 모든 원본 액션 키를 그대로 사용한다. `Part_Bandit_Hood`로 식별한 후드만 제거하고, 나머지 본체 정점 1,090개의 가중치와 기존 UV를 보존했다. 신규 장비는 Head/Chest/Spine/Hips 등 기존 뼈만 사용하며, 조끼는 몸통 가중치를 따른다. C 비례의 모델 루트 높이 스케일 1.08도 유지한다.

내보낸 몸은 `Bandit_Body` 스킨 렌더러 1개, 재질 슬롯 8개다. 원본 아틀라스는 얼굴·팔다리 등에 재사용하고 새 장구류는 `BuildValidation.json/newMaterials`의 색·거칠기·금속성으로 구분한다. 새 재질을 GameLit에 연결할 때 해당 값을 반영해야 하며, FBX 기본 재질만으로 최종 조명을 판단하지 않는다.

`RigAndFBXValidation.json`: 202프레임의 유한한 스킨 변형, 표면/UV, 정규화 가중치, 원본 뼈/액션 보존, FBX 재임포트의 뼈 23개·액션 4개·크기를 확인했다. 포함된 BatSwing은 원본 호환성 보존을 위해 남겨 둔 것이며 원거리 공격 모션이 아니다. 기존 Unity 권총/소총 클립을 연결해야 한다.

Unity 플레이와 실행 원본을 보존하기 위해 Assets 밖에 준비했다. 전투 테스트 이후 Unity MCP로 원거리 Resource 프리팹(`Characters/BanditPistol`, `Characters/BanditRifle`)에 연결하고, 무기별 조준/발사/걷기/수납과 월드 공간 IK·관통을 확인한다. 근접 `Characters/Bandit01`은 기존 C 후드 본체를 사용한다. 기존 뼈 경로를 재사용할 수 있다는 검사와 실제 총기 그립 검증은 별개다.

비교 이미지는 모두 62°/yaw 0 기준 Blender 미리보기다. 인게임 적용 화면이 아니다.
